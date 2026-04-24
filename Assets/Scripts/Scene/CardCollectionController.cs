using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CollectionDisplayMode
{
    Digital = 0,
    Physical = 1
}

public enum OwnershipFilter
{
    All = 0,
    Owned = 1,
    Unowned = 2
}

public enum CollectionSortBy
{
    Name = 0,
    Rarity = 1,
    Quantity = 2
}

public class CardCollectionController : MonoBehaviour
{
    [Header("Grid")]
    public Transform cardContainer;
    public CardUI cardPrefab;

    [Header("Datasets")]
    [SerializeField] private CardCollectionDataset digitalDataset;
    [SerializeField] private CardCollectionDataset physicalDataset;

    [Header("Display")]
    [SerializeField] private CollectionDisplayMode displayMode = CollectionDisplayMode.Digital;
    [SerializeField] private OwnershipFilter ownershipFilter = OwnershipFilter.All;
    [SerializeField] private CollectionSortBy sortBy = CollectionSortBy.Name;
    [SerializeField] private bool sortDescending;

    [Header("Extra Filters")]
    [SerializeField] private string categoryFilter = "";
    [SerializeField] private string elementFilter = "";

    // Backward-compatible flags used by existing UI wiring.
    private bool inCollection;
    private bool getInfos;

    public CollectionDisplayMode CurrentDisplayMode => displayMode;

    private void OnEnable()
    {
        RefreshCollection();
    }

    public void SetMode(bool inCollection = false, bool GetInfos = false)
    {
        this.inCollection = inCollection;
        this.getInfos = GetInfos;
    }

    public void ShowDigitalCollection()
    {
        displayMode = CollectionDisplayMode.Digital;
        RefreshCollection();
    }

    public void ShowPhysicalCollection()
    {
        displayMode = CollectionDisplayMode.Physical;
        RefreshCollection();
    }

    public void SetDisplayMode(CollectionDisplayMode mode)
    {
        displayMode = mode;
    }

    public void SetOwnershipFilter(OwnershipFilter filter)
    {
        ownershipFilter = filter;
    }

    public void SetOwnershipFilterFromInt(int filter)
    {
        ownershipFilter = (OwnershipFilter)Mathf.Clamp(filter, 0, 2);
        RefreshCollection();
    }

    public void SetSortBy(CollectionSortBy mode)
    {
        sortBy = mode;
    }

    public void SetSortByFromInt(int mode)
    {
        sortBy = (CollectionSortBy)Mathf.Clamp(mode, 0, 2);
        RefreshCollection();
    }

    public void SetSortDescending(bool descending)
    {
        sortDescending = descending;
    }

    public void SetCategoryFilter(string filter)
    {
        categoryFilter = filter ?? string.Empty;
    }

    public void SetCategoryFilterAndRefresh(string filter)
    {
        SetCategoryFilter(filter);
        RefreshCollection();
    }

    public void SetElementFilter(string filter)
    {
        elementFilter = filter ?? string.Empty;
    }

    public void SetElementFilterAndRefresh(string filter)
    {
        SetElementFilter(filter);
        RefreshCollection();
    }

    public void ClearExtraFilters()
    {
        categoryFilter = string.Empty;
        elementFilter = string.Empty;
    }

    public void ClearExtraFiltersAndRefresh()
    {
        ClearExtraFilters();
        RefreshCollection();
    }

    public List<string> GetAvailableCategoryTags()
    {
        return GetSourceCards()
            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.categoryTag))
            .Select(c => c.categoryTag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToList();
    }

    public List<string> GetAvailableElementTags()
    {
        return GetSourceCards()
            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.elementTag))
            .Select(c => c.elementTag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToList();
    }

    public void OpenScanInterface()
    {
        if (displayMode != CollectionDisplayMode.Physical)
        {
            NotificationSystem.Instance?.ShowNotification("Le scan est reserve a la collection physique.");
            return;
        }

        NotificationSystem.Instance?.ShowNotification("Scanner une carte (interface) arrive bientot.");
    }

    public void RefreshCollection(bool? inCollection = null, bool? GetInfos = null)
    {
        if (inCollection.HasValue) this.inCollection = inCollection.Value;
        if (GetInfos.HasValue) this.getInfos = GetInfos.Value;

        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        var source = GetSourceCards();
        var filtered = ApplyFilters(source);

        foreach (var card in filtered)
        {
            int qty = GetOwnedQuantity(card);
            if (qty == 0 && !getInfos)
                continue;

            var item = Instantiate(cardPrefab, cardContainer);
            item.SetCardData(
                qty,
                card.sprite,
                card,
                inCollection: this.inCollection,
                GetInfos: this.getInfos,
                canAddToDeck: displayMode == CollectionDisplayMode.Digital,
                displayMode: displayMode
            );
        }
    }

    private List<CardData> GetSourceCards()
    {
        var dataset = displayMode == CollectionDisplayMode.Physical ? physicalDataset : digitalDataset;

        if (dataset != null && dataset.cards != null && dataset.cards.Count > 0)
            return dataset.cards;

        return CardDatabase.Instance.cards
            .Where(c => c != null)
            .ToList();
    }

    private IEnumerable<CardData> ApplyFilters(IEnumerable<CardData> source)
    {
        var query = source.Where(c => c != null);

        if (!string.IsNullOrWhiteSpace(categoryFilter))
            query = query.Where(c => string.Equals(c.categoryTag, categoryFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(elementFilter))
            query = query.Where(c => string.Equals(c.elementTag, elementFilter, StringComparison.OrdinalIgnoreCase));

        if (ownershipFilter == OwnershipFilter.Owned)
            query = query.Where(c => GetOwnedQuantity(c) > 0);
        else if (ownershipFilter == OwnershipFilter.Unowned)
            query = query.Where(c => GetOwnedQuantity(c) == 0);

        return sortBy switch
        {
            CollectionSortBy.Rarity => sortDescending
                ? query.OrderByDescending(c => c.rarity).ThenBy(c => c.cardName)
                : query.OrderBy(c => c.rarity).ThenBy(c => c.cardName),
            CollectionSortBy.Quantity => sortDescending
                ? query.OrderByDescending(GetOwnedQuantity).ThenBy(c => c.cardName)
                : query.OrderBy(GetOwnedQuantity).ThenBy(c => c.cardName),
            _ => sortDescending
                ? query.OrderByDescending(c => c.cardName)
                : query.OrderBy(c => c.cardName)
        };
    }

    private int GetOwnedQuantity(CardData card)
    {
        if (card == null || string.IsNullOrEmpty(card.cardId)) return 0;

        return displayMode == CollectionDisplayMode.Physical
            ? PlayerProfileStore.GetPhysicalCardQuantity(card.cardId)
            : PlayerProfileStore.GetCardQuantity(card.cardId);
    }
}
