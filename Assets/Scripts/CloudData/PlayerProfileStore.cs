using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public static class PlayerProfileStore
{
    //Static Data
    public static long TOKEN = 0;
    public static long PACK = 0;
    public static string DISPLAY_NAME = "Guest";
    //Key for the display name in Cloud Save
    public const string DisplayNameKey = "displayName";
    // 👉 Collection de cartes
    public static Dictionary<string, int> CARD_COLLECTION = new();

    public const string CardCollectionKey = "cardCollection";
    public static Dictionary<string, int> PACK_COLLECTION = new();
    public const string PackCollectionKey = "packCollection";
    public static System.Action OnPackCollectionChanged;

    public static async Task SaveDisplayNameAsync(string displayName)
    {
        var data = new Dictionary<string, object>
        {
            { DisplayNameKey, displayName }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }
    public static async Task SaveCardCollectionAsync()
    {
    var data = new Dictionary<string, object>
        {
            { CardCollectionKey, CARD_COLLECTION }
        };

    await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public static async Task LoadCardCollectionAsync()
    {
        var keys = new HashSet<string> { CardCollectionKey };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (result.TryGetValue(CardCollectionKey, out var item))
        {
            CARD_COLLECTION = item.Value.GetAs<Dictionary<string, int>>();
        }
        else
        {
            CARD_COLLECTION = new Dictionary<string, int>();
        }
    }

    public static async Task<string> LoadDisplayNameAsync()
    {
        var keys = new HashSet<string> { DisplayNameKey };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (result.TryGetValue(DisplayNameKey, out var item))
            return item.Value.GetAs<string>();

        return null;
    }

    public static async Task AddCardAsync(string cardId, int amount = 1)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            Debug.LogError("CardId invalide");
            return;
        }

        if (!CARD_COLLECTION.ContainsKey(cardId))
            CARD_COLLECTION[cardId] = 0;

        CARD_COLLECTION[cardId] += amount;

        await SaveCardCollectionAsync();
    }
    public static async Task AddPackAsync(PackData pack, int amount = 1)
    {
        if (pack == null)
        {
            Debug.LogError("AddPackAsync: PackData null");
            return;
        }
        Debug.Log($"[AddPackAsync] Adding pack {pack.packId} x{amount}");
        if (!PACK_COLLECTION.ContainsKey(pack.packId))
            PACK_COLLECTION[pack.packId] = 0;
        PACK_COLLECTION[pack.packId] += amount;
        Debug.Log($"[AddPackAsync] New amount: {PACK_COLLECTION[pack.packId]}");
        await SavePackCollectionAsync();
        Debug.Log("[AddPackAsync] Pack collection saved");
        OnPackCollectionChanged?.Invoke();
    }

    public static async Task SavePackCollectionAsync()
    {
        var data = new Dictionary<string, object>
        {
            { PackCollectionKey, PACK_COLLECTION }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public static async Task LoadPackCollectionAsync()
    {
        var keys = new HashSet<string> { PackCollectionKey };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (result.TryGetValue(PackCollectionKey, out var item))
        {
            PACK_COLLECTION = item.Value.GetAs<Dictionary<string, int>>();
        }
        else
        {
            PACK_COLLECTION = new Dictionary<string, int>();
        }
    }

    public static async Task OpenPackAsync(PackData packData)
    {
        if (!PACK_COLLECTION.ContainsKey(packData.packId) ||
            PACK_COLLECTION[packData.packId] <= 0)
        {
            Debug.LogWarning("Aucun pack disponible");
            return;
        }

        PACK_COLLECTION[packData.packId]--;

        for (int i = 0; i < packData.cardCount; i++)
        {
            var entry = packData.possibleCards[
                Random.Range(0, packData.possibleCards.Count)
            ];

            await AddCardAsync(entry.cardId);
        }

        await SavePackCollectionAsync();
        OnPackCollectionChanged?.Invoke();
    }

}
