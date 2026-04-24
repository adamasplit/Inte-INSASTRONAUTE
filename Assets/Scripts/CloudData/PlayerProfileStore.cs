using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.VisualScripting;
using UnityEngine;

public static class PlayerProfileStore
{
    public const string DisplayNameKey   = "displayName";
    public const string CardCollectionKey = "cardCollection";
    public const string PhysicalCardCollectionKey = "physicalCardCollection";
    public const string PackCollectionKey = "packCollection";
    public const string DeckSelectionKey = "deckSelection";

    // Events — toute UI dans n'importe quelle scène peut s'abonner
    public static event System.Action<long>   OnTokenChanged;
    public static event System.Action<long>   OnPCChanged;
    public static event System.Action<string> OnDisplayNameChanged;
    public static event System.Action         OnCardCollectionChanged;
    public static event System.Action         OnPhysicalCardCollectionChanged;
    public static event System.Action         OnPackCollectionChanged;
    public static event System.Action         OnDeckSelectionChanged;

    // Propriétés — le setter notifie automatiquement les abonnés
    private static long _token;
    public static long TOKEN
    {
        get => _token;
        set { _token = value; OnTokenChanged?.Invoke(value); }
    }

    private static long _pc;
    public static long PC
    {
        get => _pc;
        set { _pc = value; OnPCChanged?.Invoke(value); }
    }

    private static string _displayName = "Guest";
    public static string DISPLAY_NAME
    {
        get => _displayName;
        set { _displayName = value; OnDisplayNameChanged?.Invoke(value); }
    }

    public static Dictionary<string, int> CARD_COLLECTION = new();
    public static Dictionary<string, int> PHYSICAL_CARD_COLLECTION = new();
    public static Dictionary<string, int> PACK_COLLECTION = new();
    public static List<string> DECK_SELECTION = new();

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

    public static async Task SavePhysicalCardCollectionAsync()
    {
        var data = new Dictionary<string, object>
        {
            { PhysicalCardCollectionKey, PHYSICAL_CARD_COLLECTION }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public static async Task LoadPhysicalCardCollectionAsync()
    {
        var keys = new HashSet<string> { PhysicalCardCollectionKey };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        PHYSICAL_CARD_COLLECTION = result.TryGetValue(PhysicalCardCollectionKey, out var item)
            ? item.Value.GetAs<Dictionary<string, int>>()
            : new Dictionary<string, int>();

        OnPhysicalCardCollectionChanged?.Invoke();
    }

    public static async Task UpdatePC()
    {
        await CollectionPointsClient.UpdatePCAsync((int)PC);
        Debug.Log($"[UpdatePC] PC mis à jour dans Economy : {PC}");
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
        //Debug.Log($"[AddCardAsync] Adding card {cardId} x{amount}");
        if (string.IsNullOrEmpty(cardId))
        {
            Debug.LogError("CardId invalide");
            return;
        }

        if (!CARD_COLLECTION.ContainsKey(cardId))
            CARD_COLLECTION[cardId] = 0;

        CARD_COLLECTION[cardId] += amount;
        //Debug.Log($"[AddCardAsync] Carte {cardId} ajoutée x{amount}. Nouvelle quantité : {CARD_COLLECTION[cardId]}");

        await SaveCardCollectionAsync();
        await ComputePC();
        OnCardCollectionChanged?.Invoke();
    }

    public static async Task AddPhysicalCardAsync(string cardId, int amount = 1)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            Debug.LogError("Physical CardId invalide");
            return;
        }

        if (!PHYSICAL_CARD_COLLECTION.ContainsKey(cardId))
            PHYSICAL_CARD_COLLECTION[cardId] = 0;

        PHYSICAL_CARD_COLLECTION[cardId] += amount;

        await SavePhysicalCardCollectionAsync();
        OnPhysicalCardCollectionChanged?.Invoke();
    }

    public static async Task AddCards(CardData[] cards)
    {
        foreach (var card in cards)
        {
            if (card != null)
            {
                if (!CARD_COLLECTION.ContainsKey(card.cardId))
                    CARD_COLLECTION[card.cardId] = 0;
                CARD_COLLECTION[card.cardId] += 1;
            }
        }
        await SaveCardCollectionAsync();
        await ComputePC();
        OnCardCollectionChanged?.Invoke();
    }

    public static long GetPCReward(CardData[] cards,bool forceSubsequent = false)
    {
        long pcReward = 0;
        foreach (var card in cards)
        {
            if (card != null)
            {
                int owned = GetCardQuantity(card.cardId);
                if (owned == 0)
                    pcReward += forceSubsequent ? card.SubsequentValue:card.FirstTimeValue;
                else
                    pcReward += card.SubsequentValue;
            }
        }
        //Debug.Log($"[GetPCReward] Calculated PC reward for given cards: {pcReward}");
        return pcReward;
    }

    public static async Task ComputePC()
    {
        int totalPC = 0;
        if (CardDatabase.Instance.cards == null || CardDatabase.Instance.cards.Count == 0)
        {
            Debug.LogWarning("CardDatabase non initialisée ou vide lors du calcul du PC.");
            CardDatabase.Instance.Init();
        }
        foreach (var card in CardDatabase.Instance.cards)
        {
            if (CARD_COLLECTION.TryGetValue(card.cardId, out int qty))
            {
                if (qty > 0) totalPC += card.FirstTimeValue + Mathf.Max(0, qty - 1) * card.SubsequentValue;
            }
        }
        PC = totalPC;
        Debug.Log($"[ComputePC] Total PC recalculé : {PC}");
        await UpdatePC();
    }

    public static async Task AddPackAsync(PackData pack, int amount = 1)
    {
        if (pack == null)
        {
            Debug.LogError("AddPackAsync: PackData null");
            return;
        }
        //Debug.Log($"[AddPackAsync] Adding pack {pack.packId} x{amount}");
        if (!PACK_COLLECTION.ContainsKey(pack.packId))
            PACK_COLLECTION[pack.packId] = 0;
        PACK_COLLECTION[pack.packId] += amount;
        //Debug.Log($"[AddPackAsync] New amount: {PACK_COLLECTION[pack.packId]}");
        await SavePackCollectionAsync();
        //Debug.Log("[AddPackAsync] Pack collection saved");
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

        PACK_COLLECTION = result.TryGetValue(PackCollectionKey, out var item)
            ? item.Value.GetAs<Dictionary<string, int>>()
            : new Dictionary<string, int>();

        OnPackCollectionChanged?.Invoke();
    }

    public static async Task RemovePackAsync(string packId, int amount = 1)
    {
        //Debug.Log($"[RemovePackAsync] Removing pack {packId} x{amount}");
        if (string.IsNullOrEmpty(packId))
        {
            Debug.LogError("RemovePackAsync: packId invalide");
            return;
        }

        if (!PACK_COLLECTION.ContainsKey(packId) || PACK_COLLECTION[packId] < amount)
        {
            Debug.LogError($"RemovePackAsync: Pas assez de packs {packId} à retirer");
            return;
        }

        PACK_COLLECTION[packId] -= amount;
        //Debug.Log($"[RemovePackAsync] Pack {packId} retiré x{amount}. Nouvelle quantité : {PACK_COLLECTION[packId]}");

        await SavePackCollectionAsync();
        OnPackCollectionChanged?.Invoke();
    }
    public static int GetCardQuantity(string cardId)
    {
        if (CARD_COLLECTION.TryGetValue(cardId, out int qty))
            return qty;
        return 0;
    }

    public static int GetPhysicalCardQuantity(string cardId)
    {
        if (PHYSICAL_CARD_COLLECTION.TryGetValue(cardId, out int qty))
            return qty;
        return 0;
    }

    public static bool HasCardForCollection(CardData card)
    {
        if (card == null || string.IsNullOrEmpty(card.cardId)) return false;
        return GetPhysicalCardQuantity(card.cardId) > 0 || GetCardQuantity(card.cardId) > 0;
    }

    public static async Task SaveDeckSelectionAsync()
    {
        var data = new Dictionary<string, object>
        {
            { DeckSelectionKey, DECK_SELECTION }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public static async Task LoadDeckSelectionAsync()
    {
        var keys = new HashSet<string> { DeckSelectionKey };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        DECK_SELECTION = result.TryGetValue(DeckSelectionKey, out var item)
            ? item.Value.GetAs<List<string>>()
            : new List<string>();

        OnDeckSelectionChanged?.Invoke();
    }

    public static int GetDeckCopies(string cardId)
    {
        return DECK_SELECTION.Count(id => id == cardId);
    }

    public static bool TryAddCardToDeck(string cardId, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrEmpty(cardId))
        {
            reason = "Carte invalide.";
            return false;
        }

        if (DECK_SELECTION.Count >= GameConstants.MaxDeckSize)
        {
            reason = $"Deck plein ({GameConstants.MaxDeckSize} cartes).";
            return false;
        }

        if (GetDeckCopies(cardId) >= GameConstants.MaxCopiesPerCard)
        {
            reason = $"Maximum {GameConstants.MaxCopiesPerCard} exemplaires par carte.";
            return false;
        }

        if (GetCardQuantity(cardId) <= GetDeckCopies(cardId))
        {
            reason = "Pas assez d'exemplaires possedes.";
            return false;
        }

        DECK_SELECTION.Add(cardId);
        OnDeckSelectionChanged?.Invoke();
        return true;
    }

    public static bool TryRemoveCardFromDeck(string cardId)
    {
        var index = DECK_SELECTION.FindIndex(id => id == cardId);
        if (index < 0) return false;

        DECK_SELECTION.RemoveAt(index);
        OnDeckSelectionChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Efface toutes les données du joueur stockées dans Cloud Save
    /// </summary>
    public static async Task ClearAllDataAsync()
    {
        try
        {
            Debug.Log("[PlayerProfileStore] Suppression de toutes les données Cloud Save...");
            
            // Effacer les données locales
            CARD_COLLECTION.Clear();
            PHYSICAL_CARD_COLLECTION.Clear();
            PACK_COLLECTION.Clear();
            DECK_SELECTION.Clear();
            TOKEN = 0;
            PC = 0;
            DISPLAY_NAME = "Guest";

            // Effacer toutes les clés dans Cloud Save (une par une)
            var keysToDelete = new List<string>
            {
                DisplayNameKey,
                CardCollectionKey,
                PhysicalCardCollectionKey,
                DeckSelectionKey,
                PackCollectionKey
            };

            foreach (var key in keysToDelete)
            {
                try
                {
                    await CloudSaveService.Instance.Data.Player.DeleteAsync(key, new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PlayerProfileStore] Impossible de supprimer la clé '{key}': {ex.Message}");
                }
            }
            
            Debug.Log("[PlayerProfileStore] Toutes les données Cloud Save ont été supprimées");

            // Notifier les listeners
            OnCardCollectionChanged?.Invoke();
            OnPhysicalCardCollectionChanged?.Invoke();
            OnPackCollectionChanged?.Invoke();
            OnDeckSelectionChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerProfileStore] Erreur lors de la suppression des données: {e.Message}");
            throw;
        }
    }

}
