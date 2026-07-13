using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public static class STSCollectionCardApi
{
    private const string LocalManifestPath = "STSCollectionCards/cards.json";

    [Serializable]
    public class CollectionCardApiEntry
    {
        public string id;
        public string name;
        public string imageUrl;
        public string thumbnailUrl;
    }

    [Serializable]
    private class ReactBridgeError
    {
        public string code;
        public string message;
    }

    [Serializable]
    private class ReactBridgeResponse
    {
        public string id;
        public bool ok;
        public JToken data;
        public ReactBridgeError error;
    }

    private static readonly Dictionary<string, CollectionCardApiEntry> cardsByName = new();
    private static readonly Dictionary<string, Sprite> spritesByName = new();
    private static readonly Dictionary<string, Task<Sprite>> spriteLoadTasksByName = new();
    private static Task loadTask;

    public static async Task EnsureLoadedAsync()
    {
        if (cardsByName.Count > 0)
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
        if (cardsByName.Count > 0)
            return;

        if (await TryLoadFromReactBridgeAsync())
            return;

        if (await TryLoadLocalManifestAsync())
            return;

        Debug.LogError("Failed to load collection cards from the React bridge and no local fallback manifest was available.");
    }

    private static async Task<bool> TryLoadFromReactBridgeAsync()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string json = await ReactApiBridge.RequestAdminCardsAsync();
        if (string.IsNullOrWhiteSpace(json))
            return false;

        return TryParseReactBridgeCardsJson(json);
#else
        return false;
#endif
    }

    private static bool TryParseReactBridgeCardsJson(string json)
    {
        Debug.Log($"Parsing React bridge card payload ({json.Length} chars).");

        JToken root;
        try
        {
            root = JToken.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse React bridge response JSON: {ex.Message}");
            return false;
        }

        JToken payload = root;
        if (root.Type == JTokenType.Object)
        {
            ReactBridgeResponse response;
            try
            {
                response = root.ToObject<ReactBridgeResponse>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize React bridge wrapper: {ex.Message}");
                return false;
            }

            if (response == null)
                return false;

            if (!response.ok)
            {
                string message = response.error != null
                    ? $"{response.error.code}: {response.error.message}"
                    : "Unknown React bridge error";
                Debug.LogWarning($"React bridge returned an error while loading cards: {message}");
                return false;
            }

            if (response.data == null || response.data.Type == JTokenType.Null)
            {
                Debug.LogWarning("React bridge returned an empty card payload.");
                return false;
            }

            payload = response.data;
        }

        if (payload.Type != JTokenType.Array)
        {
            Debug.LogWarning($"React bridge payload was not an array. Actual type: {payload.Type}");
            return false;
        }

        string cardsJson = payload.ToString(Formatting.None);
        bool parsed = TryParseCardsJson(cardsJson, "React bridge");
        Debug.Log(parsed
            ? $"React bridge payload parsed successfully with {cardsByName.Count} cards loaded."
            : "React bridge payload parsed, but no cards were loaded.");
        return parsed;
    }

    private static async Task<bool> TryLoadLocalManifestAsync()
    {
        string localManifest = await StreamingAssetsLoader.ReadAllTextAsync(LocalManifestPath);
        if (string.IsNullOrWhiteSpace(localManifest))
            return false;

        return TryParseCardsJson(localManifest, LocalManifestPath);
    }

    private static bool TryParseCardsJson(string json, string sourceName)
    {
        List<CollectionCardApiEntry> cards;
        try
        {
            cards = JsonConvert.DeserializeObject<List<CollectionCardApiEntry>>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse collection-card JSON from '{sourceName}': {ex.Message}");
            return false;
        }

        if (cards == null)
            return false;

        foreach (CollectionCardApiEntry card in cards)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.name))
                continue;

            cardsByName[card.name] = card;
        }

        if (cardsByName.Count > 0)
        {
            Debug.Log($"Loaded {cardsByName.Count} collection cards from '{sourceName}'.");
            return true;
        }

        return false;
    }

    public static bool TryGetCard(string cardId, out CollectionCardApiEntry card)
    {
        card = null;
        if (string.IsNullOrWhiteSpace(cardId))
            return false;

        return cardsByName.TryGetValue(cardId, out card);
    }

    public static async Task<CollectionCardApiEntry> GetCardAsync(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        await EnsureLoadedAsync();
        if (cardsByName.TryGetValue(cardId, out var card))
            return card;

        Debug.LogWarning($"Collection card '{cardId}' was not found in the loaded collection-card data.");
        return null;
    }

    public static async Task<Sprite> LoadSpriteAsync(CollectionCardApiEntry card)
    {
        if (card == null)
            return null;

        return await LoadSpriteAsync(card.name, card);
    }

    public static async Task<Sprite> LoadSpriteAsync(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        await EnsureLoadedAsync();
        if (cardsByName.TryGetValue(cardId, out var card))
            return await LoadSpriteAsync(cardId, card);

        Debug.LogWarning($"Collection card '{cardId}' was not found in the loaded collection-card data.");
        return null;
    }

    private static async Task<Sprite> LoadSpriteAsync(string spriteKey, CollectionCardApiEntry card)
    {
        if (string.IsNullOrWhiteSpace(spriteKey))
            return null;

        if (spritesByName.TryGetValue(spriteKey, out var cachedSprite))
            return cachedSprite;

        if (spriteLoadTasksByName.TryGetValue(spriteKey, out var existingTask))
            return await existingTask;

        Task<Sprite> loadTask = LoadSpriteInternalAsync(card);
        spriteLoadTasksByName[spriteKey] = loadTask;

        try
        {
            Sprite sprite = await loadTask;
            if (sprite != null)
            {
                spritesByName[spriteKey] = sprite;
            }

            return sprite;
        }
        finally
        {
            spriteLoadTasksByName.Remove(spriteKey);
        }
    }

    private static async Task<Sprite> LoadSpriteInternalAsync(CollectionCardApiEntry card)
    {
        string imageUrl = GetPreferredImageUrl(card);
        if (!string.IsNullOrWhiteSpace(imageUrl))
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
                    Debug.LogWarning($"Failed to load collection card sprite from '{imageUrl}': {request.error}");
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    }

                    Debug.LogWarning($"Collection card texture download returned null for '{imageUrl}'.");
                }
            }
        }
        else
        {
            Debug.LogWarning($"Collection card '{card.name}' has no imageUrl or thumbnailUrl.");
        }

        Sprite fallbackSprite = LoadSpriteFromResources(card.name);
        if (fallbackSprite != null)
        {
            Debug.Log($"Loaded collection card sprite from Resources fallback for '{card.name}'.");
        }

        return fallbackSprite;
    }

    private static Sprite LoadSpriteFromResources(string spriteKey)
    {
        if (string.IsNullOrWhiteSpace(spriteKey))
            return null;

        string resourcePath = $"Sprites/Cartes/{spriteKey}";
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Failed to load collection card sprite from Resources path '{resourcePath}'.");
        }

        return sprite;
    }

    public static int LoadedCardCount => cardsByName.Count;
    public static int CachedSpriteCount => spritesByName.Count;

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