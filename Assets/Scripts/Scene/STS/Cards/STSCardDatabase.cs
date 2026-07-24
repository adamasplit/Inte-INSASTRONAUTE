using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;

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
        try
        {
            await loadTask;
        }
        finally
        {
            loadTask = null;
        }
    }

    static async Task LoadInternalAsync()
    {
        if (isLoaded)
            return;

        cardDict = new();
        allCards = new();
        collectionCardSpriteById = new();
        collectionSpritesInitialized = false;

        Debug.Log("STSCardDatabase loading remote card catalog through React bridge first.");
        if (!await TryLoadFromRemoteApiAsync())
        {
            await LoadFromStreamingAssetsAsync();
        }

        isLoaded = allCards.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("STSCardDatabase loaded zero cards. Check the remote card API and StreamingAssets/STSCardData contents.");
            cardDict = null;
            allCards = null;
        }
        else
        {
            await EnsureCollectionCardSpritesLoadedAsync();
        }
    }

    static async Task LoadFromStreamingAssetsAsync()
    {
        Debug.Log("STSCardDatabase falling back to StreamingAssets/STSCardData.");

        List<string> files = await StreamingAssetsLoader.ListJsonFilesAsync("STSCardData");
        Debug.Log($"STSCardDatabase found {files.Count} card JSON files.");

        int perFileLoaded = 0;
        foreach (string file in files)
        {
            if (string.Equals(file, "STSCardData/cards.json", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith("/cards.json", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith("\\cards.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                string json = await StreamingAssetsLoader.ReadAllTextAsync(file);
                if (string.IsNullOrEmpty(json))
                    continue;

                STSCardDataDTO dto = JsonConvert.DeserializeObject<STSCardDataDTO>(json);

                if (dto == null)
                {
                    Debug.LogWarning($"Invalid card JSON in '{file}'.");
                    continue;
                }

                STSCardData card = STSCardData.FromDTO(dto);
                if (UpsertCard(card, true))
                {
                    perFileLoaded++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load card '{file}': {ex}");
            }
        }

        Debug.Log($"STSCardDatabase per-file StreamingAssets load completed with {perFileLoaded} cards.");

        string combinedJson = await StreamingAssetsLoader.ReadAllTextAsync("STSCardData/cards.json");
        if (!string.IsNullOrEmpty(combinedJson))
        {
            try
            {
                CardDatabaseWrapper wrapper = JsonConvert.DeserializeObject<CardDatabaseWrapper>(combinedJson);
                if (wrapper != null && wrapper.cards != null)
                {
                    int combinedAdded = 0;
                    int combinedSkipped = 0;
                    foreach (STSCardDataDTO dto in wrapper.cards)
                    {
                        if (dto == null)
                        {
                            continue;
                        }

                        STSCardData card = STSCardData.FromDTO(dto);
                        if (UpsertCard(card, false))
                        {
                            combinedAdded++;
                        }
                        else
                        {
                            combinedSkipped++;
                        }
                    }

                    Debug.Log($"STSCardDatabase merged cards.json as fallback: added={combinedAdded}, skipped_existing={combinedSkipped}, total={allCards.Count}.");
                }
                else
                {
                    Debug.LogWarning("STSCardDatabase combined cards.json did not contain a cards array.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load combined cards.json: {ex}");
            }
        }
        else
        {
            Debug.LogWarning("STSCardDatabase could not read StreamingAssets/STSCardData/cards.json.");
        }

        Debug.Log($"STSCardDatabase StreamingAssets load completed with {allCards.Count} cards.");
    }

    static async Task<bool> TryLoadFromRemoteApiAsync()
    {
        Debug.Log("STSCardDatabase requesting card catalog (api/sts/catalog/cards) through React bridge.");
        string json = await ReactApiBridge.RequestStsCatalogCardsAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("STSCardDatabase did not receive a card catalog payload from the React bridge.");
            return false;
        }

        Debug.Log($"STSCardDatabase received card catalog payload from React bridge ({json.Length} chars).");

        try
        {
            List<STSCardDataDTO> remoteCards = ParseRemoteCards(json);
            if (remoteCards == null || remoteCards.Count == 0)
            {
                Debug.LogWarning("STSCardDatabase could not find a cards array in the React bridge payload.");
                return false;
            }

            Debug.Log($"STSCardDatabase loaded {remoteCards.Count} cards from remote API.");
            foreach (STSCardDataDTO dto in remoteCards)
            {
                if (dto == null)
                {
                    Debug.LogWarning("STSCardDatabase encountered a null card DTO in the remote payload.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dto.id) && string.IsNullOrWhiteSpace(dto.cardName))
                {
                    Debug.LogWarning("STSCardDatabase encountered a card DTO without id or cardName in the remote payload.");
                }

                try
                {
                    STSCardData card = STSCardData.FromDTO(dto);
                    UpsertCard(card, true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"STSCardDatabase skipped remote card '{dto.id ?? dto.cardName ?? "<unknown>"}' because it could not be converted: {ex.Message}");
                }
            }

            Debug.Log($"STSCardDatabase remote load completed with {allCards.Count} cards.");
            return allCards.Count > 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to load cards through the React bridge: {ex}");
            return false;
        }
    }

    static List<STSCardDataDTO> ParseRemoteCards(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        JToken root = JToken.Parse(json);
        return ParseRemoteCards(root);
    }

    static List<STSCardDataDTO> ParseRemoteCards(JToken token)
    {
        if (token == null)
            return null;

        if (token.Type == JTokenType.Array)
        {
            return token.ToObject<List<STSCardDataDTO>>();
        }

        if (token.Type != JTokenType.Object)
            return null;

        JObject rootObject = (JObject)token;
        string[] candidateKeys = new[] { "cards", "data", "items", "result", "payload" };

        foreach (string key in candidateKeys)
        {
            if (rootObject.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nestedToken))
            {
                List<STSCardDataDTO> nestedCards = ParseRemoteCards(nestedToken);
                if (nestedCards != null && nestedCards.Count > 0)
                {
                    return nestedCards;
                }
            }
        }

        foreach (JProperty property in rootObject.Properties())
        {
            if (property.Value.Type != JTokenType.Object && property.Value.Type != JTokenType.Array)
                continue;

            List<STSCardDataDTO> nestedCards = ParseRemoteCards(property.Value);
            if (nestedCards != null && nestedCards.Count > 0)
            {
                return nestedCards;
            }
        }

        return null;
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

    static bool UpsertCard(STSCardData card, bool overwriteExisting)
    {
        if (card == null)
            return false;

        string key = GetCardKey(card);
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("STSCardDatabase encountered a card without id/cardName and skipped it.");
            return false;
        }

        int existingIndex = allCards.FindIndex(existing => IsSameCard(existing, key));
        if (existingIndex >= 0)
        {
            if (!overwriteExisting)
                return false;

            allCards[existingIndex] = card;
            RegisterCard(card);
            return true;
        }

        RegisterCard(card);
        allCards.Add(card);
        return true;
    }

    static bool IsSameCard(STSCardData card, string key)
    {
        if (card == null || string.IsNullOrWhiteSpace(key))
            return false;

        return string.Equals(card.id, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(card.cardName, key, StringComparison.OrdinalIgnoreCase);
    }

    static string GetCardKey(STSCardData card)
    {
        if (card == null)
            return null;

        if (!string.IsNullOrWhiteSpace(card.id))
            return card.id.Trim();

        if (!string.IsNullOrWhiteSpace(card.cardName))
            return card.cardName.Trim();

        return null;
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