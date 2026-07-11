using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

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

    private static readonly Dictionary<string, CollectionCardApiEntry> cardsById = new();
    private static Task loadTask;

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

        return TryParseCardsJson(json, "React bridge");
#else
        return false;
#endif
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
            if (card == null || string.IsNullOrWhiteSpace(card.id))
                continue;

            cardsById[card.id] = card;
        }

        if (cardsById.Count > 0)
        {
            Debug.Log($"Loaded {cardsById.Count} collection cards from '{sourceName}'.");
            return true;
        }

        return false;
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

        Debug.LogWarning($"Collection card '{cardId}' was not found in the loaded collection-card data.");
        return null;
    }

    public static async Task<Sprite> LoadSpriteAsync(CollectionCardApiEntry card)
    {
        if (card == null)
            return null;

        return await LoadSpriteAsync(card.id);
    }

    public static async Task<Sprite> LoadSpriteAsync(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        await EnsureLoadedAsync();

        //Sprite sprite = Resources.Load<Sprite>($"Sprites/Cartes/{cardId}");
        //if (sprite == null)
        //{
        //    Debug.LogWarning($"Collection card sprite not found in Resources/Sprites/Cartes/{cardId}.");
        //}

        //return sprite;
        return null;
    }
}