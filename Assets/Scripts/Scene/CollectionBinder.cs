using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionBinder : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private CardCollectionController collectionController;

    [Header("Tabs")]
    [SerializeField] private Button digitalTabButton;
    [SerializeField] private Button physicalTabButton;
    [SerializeField] private Graphic digitalTabHighlight;
    [SerializeField] private Graphic physicalTabHighlight;

    [Header("Actions")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button clearFiltersButton;

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown ownershipDropdown;
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private Toggle sortDescendingToggle;
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_Dropdown elementDropdown;

    private bool suppressUIEvents;

    private void Start()
    {
        if (collectionController == null)
            collectionController = GetComponentInChildren<CardCollectionController>(true);

        WireButtons();
        WireFilters();

        if (collectionController == null)
        {
            Debug.LogError("[CollectionBinder] CardCollectionController non assigne.");
            return;
        }

        collectionController.SetMode(inCollection: false, GetInfos: true);
        SelectDigitalTab();

        PlayerProfileStore.OnCardCollectionChanged += RefreshCurrentView;
        PlayerProfileStore.OnPhysicalCardCollectionChanged += RefreshCurrentView;
        PlayerProfileStore.OnDeckSelectionChanged += RefreshCurrentView;
    }

    private void OnDestroy()
    {
        UnwireButtons();
        UnwireFilters();

        PlayerProfileStore.OnCardCollectionChanged -= RefreshCurrentView;
        PlayerProfileStore.OnPhysicalCardCollectionChanged -= RefreshCurrentView;
        PlayerProfileStore.OnDeckSelectionChanged -= RefreshCurrentView;
    }

    private void WireButtons()
    {
        if (digitalTabButton != null) digitalTabButton.onClick.AddListener(SelectDigitalTab);
        if (physicalTabButton != null) physicalTabButton.onClick.AddListener(SelectPhysicalTab);
        if (refreshButton != null) refreshButton.onClick.AddListener(RefreshCurrentView);
        if (scanButton != null) scanButton.onClick.AddListener(OnScanClicked);
        if (clearFiltersButton != null) clearFiltersButton.onClick.AddListener(ClearFilters);
    }

    private void UnwireButtons()
    {
        if (digitalTabButton != null) digitalTabButton.onClick.RemoveAllListeners();
        if (physicalTabButton != null) physicalTabButton.onClick.RemoveAllListeners();
        if (refreshButton != null) refreshButton.onClick.RemoveAllListeners();
        if (scanButton != null) scanButton.onClick.RemoveAllListeners();
        if (clearFiltersButton != null) clearFiltersButton.onClick.RemoveAllListeners();
    }

    private void WireFilters()
    {
        if (ownershipDropdown != null) ownershipDropdown.onValueChanged.AddListener(OnOwnershipChanged);
        if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(OnSortChanged);
        if (sortDescendingToggle != null) sortDescendingToggle.onValueChanged.AddListener(OnSortDescendingChanged);
        if (categoryDropdown != null) categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        if (elementDropdown != null) elementDropdown.onValueChanged.AddListener(OnElementChanged);
    }

    private void UnwireFilters()
    {
        if (ownershipDropdown != null) ownershipDropdown.onValueChanged.RemoveAllListeners();
        if (sortDropdown != null) sortDropdown.onValueChanged.RemoveAllListeners();
        if (sortDescendingToggle != null) sortDescendingToggle.onValueChanged.RemoveAllListeners();
        if (categoryDropdown != null) categoryDropdown.onValueChanged.RemoveAllListeners();
        if (elementDropdown != null) elementDropdown.onValueChanged.RemoveAllListeners();
    }

    public void SelectDigitalTab()
    {
        if (collectionController == null) return;

        collectionController.ShowDigitalCollection();
        UpdateTabVisuals(isDigital: true);
        RebuildDynamicFilters();
        UpdateScanButtonVisibility();
    }

    public void SelectPhysicalTab()
    {
        if (collectionController == null) return;

        collectionController.ShowPhysicalCollection();
        UpdateTabVisuals(isDigital: false);
        RebuildDynamicFilters();
        UpdateScanButtonVisibility();
    }

    public void RefreshCurrentView()
    {
        if (collectionController == null) return;
        collectionController.RefreshCollection();
    }

    private void ClearFilters()
    {
        suppressUIEvents = true;

        if (ownershipDropdown != null) ownershipDropdown.value = 0;
        if (sortDropdown != null) sortDropdown.value = 0;
        if (sortDescendingToggle != null) sortDescendingToggle.isOn = false;
        if (categoryDropdown != null) categoryDropdown.value = 0;
        if (elementDropdown != null) elementDropdown.value = 0;

        suppressUIEvents = false;

        collectionController.SetOwnershipFilter(OwnershipFilter.All);
        collectionController.SetSortBy(CollectionSortBy.Name);
        collectionController.SetSortDescending(false);
        collectionController.ClearExtraFiltersAndRefresh();
    }

    private void OnOwnershipChanged(int index)
    {
        if (suppressUIEvents || collectionController == null) return;
        collectionController.SetOwnershipFilterFromInt(index);
    }

    private void OnSortChanged(int index)
    {
        if (suppressUIEvents || collectionController == null) return;
        collectionController.SetSortByFromInt(index);
    }

    private void OnSortDescendingChanged(bool descending)
    {
        if (suppressUIEvents || collectionController == null) return;
        collectionController.SetSortDescending(descending);
        collectionController.RefreshCollection();
    }

    private void OnCategoryChanged(int index)
    {
        if (suppressUIEvents || collectionController == null || categoryDropdown == null) return;

        string value = index <= 0 ? string.Empty : categoryDropdown.options[index].text;
        collectionController.SetCategoryFilterAndRefresh(value);
    }

    private void OnElementChanged(int index)
    {
        if (suppressUIEvents || collectionController == null || elementDropdown == null) return;

        string value = index <= 0 ? string.Empty : elementDropdown.options[index].text;
        collectionController.SetElementFilterAndRefresh(value);
    }

    private void OnScanClicked()
    {
        if (collectionController == null) return;
        collectionController.OpenScanInterface();
    }

    private void RebuildDynamicFilters()
    {
        if (collectionController == null) return;

        suppressUIEvents = true;

        PopulateDropdown(categoryDropdown, collectionController.GetAvailableCategoryTags(), "Toutes categories");
        PopulateDropdown(elementDropdown, collectionController.GetAvailableElementTags(), "Tous elements");

        suppressUIEvents = false;
    }

    private static void PopulateDropdown(TMP_Dropdown dropdown, List<string> values, string allLabel)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();

        var options = new List<string> { allLabel };
        options.AddRange(values);

        dropdown.AddOptions(options);
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    private void UpdateTabVisuals(bool isDigital)
    {
        SetGraphicAlpha(digitalTabButton != null ? digitalTabButton.targetGraphic : null, isDigital ? 1f : 0.45f);
        SetGraphicAlpha(physicalTabButton != null ? physicalTabButton.targetGraphic : null, isDigital ? 0.45f : 1f);

        SetHighlightState(digitalTabHighlight, isDigital);
        SetHighlightState(physicalTabHighlight, !isDigital);
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private static void SetHighlightState(Graphic highlight, bool active)
    {
        if (highlight == null) return;

        Color color = active
            ? new Color(1f, 0.92f, 0.35f, 1f)
            : new Color(1f, 1f, 1f, 0f);

        highlight.color = color;
    }

    private void UpdateScanButtonVisibility()
    {
        if (scanButton == null || collectionController == null) return;
        scanButton.gameObject.SetActive(collectionController.CurrentDisplayMode == CollectionDisplayMode.Physical);
    }
}
