using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public static class STSCardDatabase
{
    [System.Serializable]
    private class CardDatabaseWrapper
    {
        public List<STSCardDataDTO> cards;
    }

    static Dictionary<string, STSCardData> cardDict;
    static Dictionary<string, Sprite> collectionCardSpriteById;
    static bool isLoaded;
    static Task loadTask;
    static Task collectionCardSpriteLoadTask;
    static bool collectionSpritesInitialized;

    public static List<STSCardData> allCards;

    public static async Task LoadAsync()
    {
        if (loadTask != null)
        {
            await loadTask;
            return;
        }

        loadTask = LoadInternalAsync();
        await loadTask;
    }

    static async Task LoadInternalAsync()
    {
        if (isLoaded)
            return;

        cardDict = new();
        allCards = new();
        collectionCardSpriteById = new();
        collectionSpritesInitialized = false;

        string combinedJson = await StreamingAssetsLoader.ReadAllTextAsync("STSCardData/cards.json");
        if (!string.IsNullOrEmpty(combinedJson))
        {
            try
            {
                CardDatabaseWrapper wrapper = JsonConvert.DeserializeObject<CardDatabaseWrapper>(combinedJson);
                if (wrapper != null && wrapper.cards != null)
                {
                    Debug.Log($"STSCardDatabase loaded {wrapper.cards.Count} cards from cards.json.");
                    foreach (STSCardDataDTO dto in wrapper.cards)
                    {
                        if (dto == null)
                        {
                            continue;
                        }

                        STSCardData card = STSCardData.FromDTO(dto);
                        RegisterCard(card);
                        allCards.Add(card);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load combined cards.json: {ex}");
            }
        }
        else
        {
            List<string> files = await StreamingAssetsLoader.ListJsonFilesAsync("STSCardData");
            Debug.Log($"STSCardDatabase found {files.Count} card JSON files.");

            foreach (string file in files)
            {
                try
                {
                    string json = await StreamingAssetsLoader.ReadAllTextAsync(file);
                    if (string.IsNullOrEmpty(json))
                        continue;

                    STSCardDataDTO dto =
                        JsonConvert.DeserializeObject<STSCardDataDTO>(json);

                    if (dto == null)
                    {
                        Debug.LogWarning($"Invalid card JSON in '{file}'.");
                        continue;
                    }

                    STSCardData card =
                        STSCardData.FromDTO(dto);

                    RegisterCard(card);

                    allCards.Add(card);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load card '{file}': {ex}");
                }
            }
        }

        isLoaded = allCards.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("STSCardDatabase loaded zero cards. Check StreamingAssets/STSCardData and its JSON contents.");
            cardDict = null;
            allCards = null;
        }
        else
        {
            await EnsureCollectionCardSpritesLoadedAsync();
        }

        loadTask = null;
    }

    static void RegisterCard(STSCardData card)
    {
        if (card == null)
            return;

        if (!string.IsNullOrEmpty(card.cardName))
        {
            cardDict[card.cardName] = card;
        }

        if (!string.IsNullOrEmpty(card.id) && !string.Equals(card.id, card.cardName, System.StringComparison.Ordinal))
        {
            cardDict[card.id] = card;
        }
    }

    public static void Load()
    {
        #if UNITY_ANDROID || UNITY_WEBGL
                Debug.LogError("STSCardDatabase.Load() is not supported on Android/WebGL. Use LoadAsync() and await it.");
        #else
                LoadAsync().GetAwaiter().GetResult();
        #endif
    }

    public static async Task EnsureLoadedAsync()
    {
        if (allCards != null && allCards.Count > 0)
            return;

        isLoaded = false;
        await LoadAsync();
    }

    public static bool TryGetCollectionCardSprite(string collectionCardId, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(collectionCardId) || collectionCardSpriteById == null)
            return false;

        return collectionCardSpriteById.TryGetValue(collectionCardId, out sprite);
    }

    public static async Task<Sprite> GetCollectionCardSpriteAsync(string collectionCardId)
    {
        if (string.IsNullOrWhiteSpace(collectionCardId))
            return null;

        if (TryGetCollectionCardSprite(collectionCardId, out Sprite cached))
            return cached;

        await EnsureCollectionCardSpritesLoadedAsync();
        if (TryGetCollectionCardSprite(collectionCardId, out cached))
            return cached;

        Sprite sprite = await STSCollectionCardApi.LoadSpriteAsync(collectionCardId);
        if (sprite != null)
        {
            collectionCardSpriteById ??= new Dictionary<string, Sprite>();
            collectionCardSpriteById[collectionCardId] = sprite;
        }

        return sprite;
    }

    public static async Task EnsureCollectionCardSpritesLoadedAsync()
    {
        if (collectionSpritesInitialized)
            return;

        if (allCards == null || allCards.Count == 0)
            return;

        if (collectionCardSpriteLoadTask != null)
        {
            await collectionCardSpriteLoadTask;
            return;
        }

        collectionCardSpriteLoadTask = LoadCollectionCardSpritesInternalAsync();
        try
        {
            await collectionCardSpriteLoadTask;
        }
        finally
        {
            collectionCardSpriteLoadTask = null;
        }
    }

    static async Task LoadCollectionCardSpritesInternalAsync()
    {
        collectionCardSpriteById ??= new Dictionary<string, Sprite>();

        HashSet<string> uniqueCollectionCardIds = new HashSet<string>();
        foreach (STSCardData card in allCards)
        {
            if (card == null)
                continue;

            string collectionCardId = card.GetCollectionCardId();
            if (!string.IsNullOrWhiteSpace(collectionCardId))
            {
                uniqueCollectionCardIds.Add(collectionCardId);
            }
        }

        foreach (string collectionCardId in uniqueCollectionCardIds)
        {
            if (collectionCardSpriteById.ContainsKey(collectionCardId))
                continue;

            Sprite sprite = await STSCollectionCardApi.LoadSpriteAsync(collectionCardId);
            if (sprite != null)
            {
                collectionCardSpriteById[collectionCardId] = sprite;
            }
        }

        collectionSpritesInitialized = true;
        if (collectionCardSpriteById.Count == uniqueCollectionCardIds.Count)
        {
            Debug.Log($"STSCardDatabase cached all {collectionCardSpriteById.Count} collection card sprites.");
        }
        else
        {
            List<string> missingSprites = new List<string>();
            foreach (string collectionCardId in uniqueCollectionCardIds)
            {
                if (!collectionCardSpriteById.ContainsKey(collectionCardId))
                    missingSprites.Add(collectionCardId);
            }

            Debug.LogWarning($"STSCardDatabase cached {collectionCardSpriteById.Count}/{uniqueCollectionCardIds.Count} collection card sprites. Missing: {string.Join(", ", missingSprites)}");
        }
    }

    public static STSCardData Get(string id)
    {
        if (cardDict.TryGetValue(id, out var card))
            return card;

        Debug.LogError($"Card {id} not found!");

        return null;
    }
    public static List<STSCardData> CardForCollectionCard(string collectionCardId)
    {
        List<STSCardData> cards = new List<STSCardData>();

        foreach (var card in allCards)
        {
            if (card.collectionCardId == collectionCardId)
            {
                cards.Add(card);
            }
        }

        return cards;
    }
    public static STSCardData GetRandomCard()
    {
        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogError("No cards loaded in STSCardDatabase!");
            return null;
        }

        int index = UnityEngine.Random.Range(0, allCards.Count);
        return allCards[index];
    }
    public static STSCardData GetRandomCard(SelectableCharacter character)
    {
        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogError("No cards loaded in STSCardDatabase!");
            return null;
        }

        List<STSCardData> favoredCards = allCards.FindAll(c => c.favoredCharacter == character&&c.HasTag(CardTag.Created));
        if (favoredCards.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, favoredCards.Count);
            return favoredCards[index];
        }
        else
        {
            return GetRandomCard();
        }
    }
}