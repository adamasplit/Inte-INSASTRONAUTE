using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public static class STSCollectionCardApi
{
    private const string SameOriginCardsEndpoint = "/api/admin/cards";
    private const string ExternalCardsEndpoint = "https://api.beraud.dev/api/admin/cards";
    private const string LocalManifestPath = "STSCollectionCards/cards.json";
    private const string AccessTokenPlayerPrefsKey = "STS_COLLECTION_CARD_API_TOKEN";

    [Serializable]
    public class CollectionCardApiEntry
    {
        public string id;
        public string name;
        public string imageUrl;
        public string thumbnailUrl;
    }

    private static readonly Dictionary<string, CollectionCardApiEntry> cardsById = new();
    private static readonly Dictionary<string, Sprite> spriteCacheByUrl = new();
    private static readonly Dictionary<string, Task<Sprite>> spriteLoadTasksByUrl = new();
    private static Task loadTask;
    private static string accessToken;

    public static void SetAccessToken(string token)
    {
        accessToken = token;
    }

    public static async Task EnsureLoadedAsync()
    {
        if (cardsById.Count > 0)
            return;

        if (loadTask != null)
        {
            await loadTask;
            return;
        }

        loadTask = LoadInternalAsync();
        try
        {
            await loadTask;
        }
        finally
        {
            loadTask = null;
        }
    }

    private static async Task LoadInternalAsync()
    {
        if (cardsById.Count > 0)
            return;

        if (await TryLoadRemoteAsync())
            return;

        if (await TryLoadLocalManifestAsync())
            return;

        Debug.LogError("Failed to load collection cards from the API and no local fallback manifest was available.");
    }

    private static async Task<bool> TryLoadRemoteAsync()
    {
        string[] endpoints = GetRemoteEndpoints();
        for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
        {
            string endpoint = endpoints[endpointIndex];
            using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
            {
                request.SetRequestHeader("Accept", "application/json");
                string token = GetAccessToken();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
                }

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

#if UNITY_2020_2_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isHttpError || request.isNetworkError)
#endif
                {
                    Debug.LogWarning($"Failed to load collection cards from '{endpoint}': {request.error}");
                    continue;
                }

                List<CollectionCardApiEntry> cards = JsonConvert.DeserializeObject<List<CollectionCardApiEntry>>(request.downloadHandler.text);
                if (cards == null)
                {
                    Debug.LogWarning($"Collection card API response from '{endpoint}' could not be parsed.");
                    continue;
                }

                foreach (CollectionCardApiEntry card in cards)
                {
                    if (card == null || string.IsNullOrWhiteSpace(card.id))
                        continue;

                    cardsById[card.id] = card;
                }

                if (cardsById.Count > 0)
                {
                    Debug.Log($"Loaded {cardsById.Count} collection cards from API endpoint '{endpoint}'.");
                    return true;
                }
            }
        }

        return false;
    }

    private static string[] GetRemoteEndpoints()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new[] { SameOriginCardsEndpoint };
#else
        return new[] { ExternalCardsEndpoint, SameOriginCardsEndpoint };
#endif
    }

    private static async Task<bool> TryLoadLocalManifestAsync()
    {
        string localManifest = await StreamingAssetsLoader.ReadAllTextAsync(LocalManifestPath);
        if (string.IsNullOrWhiteSpace(localManifest))
            return false;

        List<CollectionCardApiEntry> cards;
        try
        {
            cards = JsonConvert.DeserializeObject<List<CollectionCardApiEntry>>(localManifest);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse local collection-card manifest '{LocalManifestPath}': {ex.Message}");
            return false;
        }

        if (cards == null)
            return false;

        foreach (CollectionCardApiEntry card in cards)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.id))
                continue;

            cardsById[card.id] = card;
        }

        if (cardsById.Count > 0)
        {
            Debug.Log($"Loaded {cardsById.Count} collection cards from local manifest fallback.");
            return true;
        }

        return false;
    }

    private static string GetAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
            return accessToken;

        if (PlayerPrefs.HasKey(AccessTokenPlayerPrefsKey))
            return PlayerPrefs.GetString(AccessTokenPlayerPrefsKey);

        return null;
    }

    public static bool TryGetCard(string cardId, out CollectionCardApiEntry card)
    {
        card = null;
        if (string.IsNullOrWhiteSpace(cardId))
            return false;

        return cardsById.TryGetValue(cardId, out card);
    }

    public static async Task<CollectionCardApiEntry> GetCardAsync(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        await EnsureLoadedAsync();
        if (cardsById.TryGetValue(cardId, out var card))
            return card;

        Debug.LogWarning($"Collection card '{cardId}' was not found in the API response.");
        return null;
    }

    public static bool TryGetSprite(string cardId, out Sprite sprite)
    {
        sprite = null;
        if (!TryGetCard(cardId, out var card))
            return false;

        string imageUrl = GetPreferredImageUrl(card);
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        return spriteCacheByUrl.TryGetValue(imageUrl, out sprite);
    }

    public static async Task<Sprite> LoadSpriteAsync(CollectionCardApiEntry card)
    {
        if (card == null)
            return null;

        string imageUrl = GetPreferredImageUrl(card);
        if (string.IsNullOrWhiteSpace(imageUrl))
            return LoadSpriteFromResources(card.id);

        if (spriteCacheByUrl.TryGetValue(imageUrl, out var cachedSprite))
            return cachedSprite;

        if (spriteLoadTasksByUrl.TryGetValue(imageUrl, out var existingTask))
            return await existingTask;

        Task<Sprite> loadTask = LoadSpriteInternalAsync(imageUrl);
        spriteLoadTasksByUrl[imageUrl] = loadTask;

        try
        {
            Sprite sprite = await loadTask;
            if (sprite != null)
                return sprite;

            return LoadSpriteFromResources(card.id);
        }
        finally
        {
            spriteLoadTasksByUrl.Remove(imageUrl);
        }
    }

    public static async Task<Sprite> LoadSpriteAsync(string cardId)
    {
        CollectionCardApiEntry card = await GetCardAsync(cardId);
        if (card != null)
            return await LoadSpriteAsync(card);

        return LoadSpriteFromResources(cardId);
    }

    private static async Task<Sprite> LoadSpriteInternalAsync(string imageUrl)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isHttpError || request.isNetworkError)
#endif
            {
                Debug.LogError($"Failed to load collection card sprite from '{imageUrl}': {request.error}");
                return null;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                Debug.LogError($"Collection card texture download returned null for '{imageUrl}'.");
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            spriteCacheByUrl[imageUrl] = sprite;
            return sprite;
        }
    }

    private static Sprite LoadSpriteFromResources(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        Sprite sprite = Resources.Load<Sprite>($"Sprites/Cartes/{cardId}");
        if (sprite == null)
        {
            Debug.LogWarning($"Collection card sprite fallback not found in Resources/Sprites/Cartes/{cardId}.");
        }

        return sprite;
    }

    private static string GetPreferredImageUrl(CollectionCardApiEntry card)
    {
        if (card == null)
            return null;

        if (!string.IsNullOrWhiteSpace(card.imageUrl))
            return card.imageUrl;

        if (!string.IsNullOrWhiteSpace(card.thumbnailUrl))
            return card.thumbnailUrl;

        return null;
    }
}