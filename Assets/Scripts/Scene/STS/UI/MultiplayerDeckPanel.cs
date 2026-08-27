using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerDeckPanel : MonoBehaviour
{
    [Serializable]
    private class DeckViewModel
    {
        public string id;
        public string name;
        public List<string> cardIds = new();
    }

    private sealed class CardEntry
    {
        public STSCardData card;
        public string key;
        public bool owned;
        public bool unlocked;
        public bool characterCompatible;
    }

    [Header("Data")]
    [SerializeField] private int minDeckSize = 10;
    [SerializeField] private int maxDeckSize = 30;

    [Header("Grid")]
    [SerializeField] private Transform gridContainer;
    [SerializeField] private GameObject cardItemPrefab;

    [Header("Editor Preview")]
    [SerializeField] private bool spawnEditorPlaceholdersWhenApiUnavailable = true;
    [SerializeField] private int editorPlaceholderMinCount = 12;
    [SerializeField] private int editorPlaceholderMaxCount = 28;

    [Header("Filters")]
    [SerializeField] private Transform filtersContainer;
    [SerializeField] private GameObject filterToggleItemPrefab;
    [SerializeField] private TMP_InputField searchInput;

    [Header("Deck Actions")]
    [SerializeField] private Button addAllButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;

    [Header("Deck Presets")]
    [SerializeField] private TMP_Dropdown savedDecksDropdown;
    [SerializeField] private TMP_InputField deckNameInput;
    [SerializeField] private Button saveDeckButton;
    [SerializeField] private Button loadDeckButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<CardEntry> allEntries = new();
    private readonly List<CardEntry> visibleEntries = new();
    private readonly HashSet<string> selectedCardKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DeckViewModel> savedDecks = new();
    private readonly HashSet<string> ownedCardIds = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<CardRarity> selectedRarities = new();
    private readonly HashSet<SelectableCharacter> selectedCharacterFilters = new();
    private readonly HashSet<UnlockFilterState> selectedUnlockStates = new();

    private enum UnlockFilterState { Locked, Unlocked }

    private MultiplayerMenuController host;
    private SelectableCharacter selectedCharacter = SelectableCharacter.EP;
    private int selectedCharacterLevel;
    private bool missingApiResponses;

    public void SetHost(MultiplayerMenuController controller)
    {
        host = controller;
    }

    private void Awake()
    {
        WireListeners();
        BuildFilterCheckboxes();
        SetStatus(string.Empty);
        RefreshDeckCounter();
    }

    public void OpenForCharacter(SelectableCharacter character)
    {
        selectedCharacter = character;
        BuildFilterCheckboxes();
        gameObject.SetActive(true);
        _ = RefreshAsync();
    }

    private void WireListeners()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveAllListeners();
            searchInput.onValueChanged.AddListener(_ => RefreshGrid());
        }

        if (addAllButton != null)
        {
            addAllButton.onClick.RemoveAllListeners();
            addAllButton.onClick.AddListener(AddAllVisibleCards);
        }

        if (validateButton != null)
        {
            validateButton.onClick.RemoveAllListeners();
            validateButton.onClick.AddListener(() => _ = ValidateDeckAsync());
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => _ = RefreshAsync());
        }

        if (saveDeckButton != null)
        {
            saveDeckButton.onClick.RemoveAllListeners();
            saveDeckButton.onClick.AddListener(() => _ = SaveDeckPresetAsync());
        }

        if (loadDeckButton != null)
        {
            loadDeckButton.onClick.RemoveAllListeners();
            loadDeckButton.onClick.AddListener(() => _ = LoadSelectedDeckPresetAsync());
        }
    }

    private async Task RefreshAsync()
    {
        SetStatus("Chargement des cartes...");
        missingApiResponses = false;

        await STSCardDatabase.EnsureLoadedAsync();

        JToken profile = await STSApiClient.GetPvpProfileAsync();
        if (profile == null)
        {
            missingApiResponses = true;
        }
        selectedCharacterLevel = ResolveCharacterLevel(profile, selectedCharacter);

        ownedCardIds.Clear();

        // Both sources are merged: the PVP collection can answer with ids that match nothing usable here.
        JToken pvpCollection = await STSApiClient.GetPvpCollectionAsync();
        if (pvpCollection != null)
        {
            CollectOwnedCardIds(pvpCollection, ownedCardIds);
        }

        JToken virtualDeck = await STSApiClient.GetVirtualCollectionDeckAsync();
        if (virtualDeck != null)
        {
            CollectOwnedCardIds(virtualDeck, ownedCardIds);
        }

        if (ownedCardIds.Count == 0)
        {
            missingApiResponses = true;
        }

        Debug.Log($"[STS-PVP] Owned collection entries: {ownedCardIds.Count} -> {string.Join(", ", ownedCardIds.Take(30))}");

        RebuildEntries();
        Debug.Log($"[STS-PVP] Deck builder entries: {allEntries.Count}, unlocked: {allEntries.Count(entry => entry.unlocked)}");
        await RefreshSavedDecksAsync();
        RefreshGrid();
        UpdateStatusWithCounts();
    }

    private void BuildFilterCheckboxes()
    {
        if (filtersContainer == null || filterToggleItemPrefab == null)
            return;

        foreach (Transform child in filtersContainer)
        {
            Destroy(child.gameObject);
        }

        selectedRarities.Clear();
        selectedCharacterFilters.Clear();
        selectedUnlockStates.Clear();

        List<(CardRarity value, string iconName, string label)> rarityOptions = new();
        foreach (CardRarity rarity in Enum.GetValues(typeof(CardRarity)))
        {
            rarityOptions.Add((rarity, RarityIconName(rarity), rarity.ToString()));
        }
        BuildCheckboxCategory(rarityOptions, selectedRarities);

        // Cards for other characters are never selectable here, so only "Aucun" and the current character are worth filtering by.
        List<(SelectableCharacter value, string iconName, string label)> characterOptions = new()
        {
            (SelectableCharacter.Aucun, CharacterIconName(SelectableCharacter.Aucun), SelectableCharacter.Aucun.ToString())
        };
        if (selectedCharacter != SelectableCharacter.Aucun)
        {
            characterOptions.Add((selectedCharacter, CharacterIconName(selectedCharacter), selectedCharacter.ToString()));
        }
        BuildCheckboxCategory(characterOptions, selectedCharacterFilters);

        List<(UnlockFilterState value, string iconName, string label)> unlockOptions = new()
        {
            (UnlockFilterState.Unlocked, "debloque", "Débloquées"),
            (UnlockFilterState.Locked, "verrouille", "Non débloquées")
        };
        BuildCheckboxCategory(unlockOptions, selectedUnlockStates);
    }

    private void BuildCheckboxCategory<T>(
        List<(T value, string iconName, string label)> options,
        HashSet<T> selectedSet)
    {
        GameObject row = new GameObject($"FilterRow_{typeof(T).Name}", typeof(RectTransform));
        row.transform.SetParent(filtersContainer, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        // Items keep their own authored size; flexible spacer objects between them absorb the leftover width evenly.
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 24f;
        ContentSizeFitter fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        for (int i = 0; i < options.Count; i++)
        {
            (T value, string iconName, string label) = options[i];

            GameObject itemObject = Instantiate(filterToggleItemPrefab, row.transform);
            FilterToggleItem item = itemObject.GetComponent<FilterToggleItem>();
            if (item == null)
            {
                item = itemObject.GetComponentInChildren<FilterToggleItem>();
            }

            if (item == null)
            {
                Debug.LogWarning("FilterToggleItem component missing on filter toggle item prefab.");
                continue;
            }

            RectTransform itemRect = itemObject.transform as RectTransform;
            LayoutElement itemLayout = itemObject.GetComponent<LayoutElement>();
            if (itemLayout == null)
            {
                itemLayout = itemObject.AddComponent<LayoutElement>();
            }
            float lockedWidth = itemRect != null ? itemRect.rect.width : 0f;
            itemLayout.minWidth = lockedWidth;
            itemLayout.preferredWidth = lockedWidth;
            itemLayout.flexibleWidth = 0f;

            if (item.icon != null)
            {
                Sprite sprite = Resources.Load<Sprite>($"STS/Icons/PVP/{iconName}");
                item.icon.sprite = sprite;
                item.icon.enabled = sprite != null;
            }

            if (item.label != null)
            {
                item.label.text = label;
            }

            if (item.toggle != null)
            {
                item.toggle.isOn = false;
                item.toggle.onValueChanged.RemoveAllListeners();
                item.toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        selectedSet.Add(value);
                    else
                        selectedSet.Remove(value);
                    RefreshGrid();
                });
            }

            if (i < options.Count - 1)
            {
                GameObject spacer = new GameObject("FilterSpacer", typeof(RectTransform));
                spacer.transform.SetParent(row.transform, false);
                LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
                spacerLayout.minWidth = 0f;
                spacerLayout.preferredWidth = 0f;
                spacerLayout.flexibleWidth = 1f;
            }
        }
    }

    private static string RarityIconName(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => "rarity1",
            CardRarity.Uncommon => "rarity2",
            CardRarity.Rare => "rarity3",
            CardRarity.Epic => "rarity4",
            CardRarity.Legendary => "rarity5",
            CardRarity.Special => "rarity6",
            _ => rarity.ToString().ToLowerInvariant()
        };
    }

    private static string CharacterIconName(SelectableCharacter character)
    {
        return character.ToString().ToLowerInvariant();
    }

    private void RebuildEntries()
    {
        allEntries.Clear();

        if (STSCardDatabase.allCards == null)
        {
            return;
        }

        foreach (STSCardData card in STSCardDatabase.allCards)
        {
            if (card == null)
                continue;

            if (IsHiddenFromDeckBuilder(card))
                continue;

            string key = GetCardKey(card);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            bool owned = IsOwnedCard(card);
            bool unlocked = IsUnlocked(card, owned);
            bool compatible = IsCompatibleWithCurrentCharacter(card);

            allEntries.Add(new CardEntry
            {
                card = card,
                key = key,
                owned = owned,
                unlocked = unlocked,
                characterCompatible = compatible
            });
        }

        allEntries.Sort((a, b) => string.Compare(a.card.cardName, b.card.cardName, StringComparison.OrdinalIgnoreCase));

        selectedCardKeys.RemoveWhere(key => allEntries.All(entry => !string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase)));
    }

    // Never selectable in PVP, so they are dropped before any filter runs.
    private bool IsHiddenFromDeckBuilder(STSCardData card)
    {
        return !IsCompatibleWithCurrentCharacter(card)
            || card.HasTag(CardTag.Unobtainable)
            || card.HasTag(CardTag.Created)
            || card.HasTag(CardTag.FollowUp);
    }

    private bool IsOwnedCard(STSCardData card)
    {
        return OwnsIdentifier(card.GetCollectionCardId())
            || OwnsIdentifier(GetCardKey(card))
            || OwnsIdentifier(card.cardName);
    }

    private bool OwnsIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && ownedCardIds.Contains(identifier.Trim());
    }

    /// <summary>
    /// Délègue à la règle du serveur, au lieu de la redire ici.
    /// </summary>
    /// <remarks>
    /// Ce code exigeait de posséder toute carte non exclusive. Le serveur, lui, ne
    /// restreint que celles liées à une carte de collection — quarante-deux sur trois
    /// cent cinquante. Un joueur sans collection voyait donc une grille vide, ne
    /// pouvait composer aucun deck, et se voyait refuser toute recherche de combat
    /// faute d'en avoir un. Le multijoueur était fermé à qui n'avait jamais scanné.
    /// </remarks>
    private bool IsUnlocked(STSCardData card, bool owned)
    {
        return PvpDeckEligibility.IsUsable(
            card.GetCollectionCardId(),
            owned,
            card.multiplayerExclusive,
            card.characterLevel,
            selectedCharacterLevel);
    }

    private bool IsCompatibleWithCurrentCharacter(STSCardData card)
    {
        if (card == null)
            return false;

        return PvpDeckEligibility.BelongsToPool(
            card.favoredCharacter.ToString(), selectedCharacter.ToString());
    }

    private void RefreshGrid()
    {
        visibleEntries.Clear();
        ClearGrid();

        foreach (CardEntry entry in allEntries)
        {
            if (!PassesFilters(entry))
                continue;

            visibleEntries.Add(entry);

            GameObject itemObject = Instantiate(cardItemPrefab, gridContainer);
            MultiplayerDeckCardItem item = itemObject.GetComponent<MultiplayerDeckCardItem>();
            if (item == null)
            {
                item = itemObject.GetComponentInChildren<MultiplayerDeckCardItem>();
            }

            if (item == null)
            {
                Debug.LogWarning("MultiplayerDeckCardItem component missing on deck item prefab.");
                continue;
            }

            bool isSelected = selectedCardKeys.Contains(entry.key);
            bool canSelect = entry.unlocked && entry.characterCompatible;
            string lockReason = BuildLockReason(entry);

            item.Bind(entry.card, entry.key, isSelected, canSelect, lockReason, HandleCardToggleChanged);
        }

        if (visibleEntries.Count == 0 && ShouldSpawnEditorPlaceholders())
        {
            SpawnEditorPlaceholders();
            SetStatus("Aucune réponse API en éditeur: aperçu de layout avec placeholders.");
        }

        RefreshDeckCounter();
        UpdateStatusWithCounts();
    }

    private void ClearGrid()
    {
        if (gridContainer == null)
            return;

        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private bool ShouldSpawnEditorPlaceholders()
    {
        if (!Application.isEditor)
            return false;

        if (!spawnEditorPlaceholdersWhenApiUnavailable)
            return false;

        if (!missingApiResponses)
            return false;

        if (gridContainer == null || cardItemPrefab == null)
            return false;

        return true;
    }

    private void SpawnEditorPlaceholders()
    {
        int min = Mathf.Max(1, editorPlaceholderMinCount);
        int max = Mathf.Max(min, editorPlaceholderMaxCount);
        int count = UnityEngine.Random.Range(min, max + 1);

        for (int i = 0; i < count; i++)
        {
            Instantiate(cardItemPrefab, gridContainer);
        }
    }

    private bool PassesFilters(CardEntry entry)
    {
        if (entry == null || entry.card == null)
            return false;

        if (!PassesRarityFilter(entry.card))
            return false;

        if (!PassesCharacterFilter(entry.card))
            return false;

        if (!PassesUnlockFilter(entry))
            return false;

        if (!PassesSearchFilter(entry.card))
            return false;

        return true;
    }

    private bool PassesRarityFilter(STSCardData card)
    {
        if (selectedRarities.Count == 0)
            return true;

        return selectedRarities.Contains(card.rarity);
    }

    private bool PassesCharacterFilter(STSCardData card)
    {
        if (selectedCharacterFilters.Count == 0)
            return true;

        return card.favoredCharacter == SelectableCharacter.Starting
            || selectedCharacterFilters.Contains(card.favoredCharacter);
    }

    private bool PassesUnlockFilter(CardEntry entry)
    {
        if (selectedUnlockStates.Count == 0)
            return true;

        bool wantUnlocked = selectedUnlockStates.Contains(UnlockFilterState.Unlocked);
        bool wantLocked = selectedUnlockStates.Contains(UnlockFilterState.Locked);
        return (wantUnlocked && entry.unlocked) || (wantLocked && !entry.unlocked);
    }

    private bool PassesSearchFilter(STSCardData card)
    {
        if (searchInput == null || string.IsNullOrWhiteSpace(searchInput.text))
            return true;

        string term = searchInput.text.Trim();
        return (!string.IsNullOrWhiteSpace(card.cardName) && card.cardName.Contains(term, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(card.id) && card.id.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private string BuildLockReason(CardEntry entry)
    {
        if (!entry.unlocked)
        {
            if (entry.card.multiplayerExclusive)
            {
                return $"Niv. requis: {entry.card.characterLevel}";
            }

            return "Carte non obtenue";
        }

        if (!entry.characterCompatible)
        {
            return $"Incompatible ({selectedCharacter})";
        }

        return string.Empty;
    }

    private void HandleCardToggleChanged(string cardKey, bool selected)
    {
        if (string.IsNullOrWhiteSpace(cardKey))
            return;

        if (selected)
        {
            if (selectedCardKeys.Count >= maxDeckSize)
            {
                Notify($"Le deck est limité à {maxDeckSize} cartes.");
                RefreshGrid();
                return;
            }

            CardEntry entry = allEntries.FirstOrDefault(c => string.Equals(c.key, cardKey, StringComparison.OrdinalIgnoreCase));
            if (entry == null || !entry.unlocked || !entry.characterCompatible)
            {
                RefreshGrid();
                return;
            }

            selectedCardKeys.Add(cardKey);
        }
        else
        {
            selectedCardKeys.Remove(cardKey);
        }

        RefreshDeckCounter();
    }

    private void AddAllVisibleCards()
    {
        int added = 0;
        foreach (CardEntry entry in visibleEntries)
        {
            if (selectedCardKeys.Count >= maxDeckSize)
                break;

            if (!entry.unlocked || !entry.characterCompatible)
                continue;

            if (selectedCardKeys.Add(entry.key))
            {
                added++;
            }
        }

        RefreshGrid();
        Notify(added > 0
            ? $"{added} carte(s) ajoutée(s) au deck."
            : "Aucune carte visible valide à ajouter.");
    }

    private async Task ValidateDeckAsync()
    {
        if (selectedCardKeys.Count < minDeckSize)
        {
            Notify($"Le deck doit contenir au moins {minDeckSize} cartes.");
            return;
        }

        if (selectedCardKeys.Count > maxDeckSize)
        {
            Notify($"Le deck dépasse la limite de {maxDeckSize} cartes.");
            return;
        }

        JObject payload = new JObject
        {
            ["name"] = string.IsNullOrWhiteSpace(deckNameInput != null ? deckNameInput.text : null) ? "Deck Actif" : deckNameInput.text.Trim(),
            ["selectedCharacter"] = selectedCharacter.ToString(),
            ["isActive"] = true,
            ["cardIds"] = new JArray(selectedCardKeys.ToArray())
        };

        JToken response = await STSApiClient.SavePvpDeckAsync(payload);
        if (response == null)
        {
            Notify("Impossible de sauvegarder le deck actif.");
            return;
        }

        Notify("Deck validé et sauvegardé.");
        await RefreshSavedDecksAsync();
    }

    private async Task SaveDeckPresetAsync()
    {
        string name = deckNameInput != null ? deckNameInput.text?.Trim() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            Notify("Nom de deck requis pour la sauvegarde.");
            return;
        }

        JObject payload = new JObject
        {
            ["name"] = name,
            ["selectedCharacter"] = selectedCharacter.ToString(),
            ["isActive"] = false,
            ["cardIds"] = new JArray(selectedCardKeys.ToArray())
        };

        JToken response = await STSApiClient.SavePvpDeckAsync(payload);
        if (response == null)
        {
            Notify("Échec de sauvegarde du deck.");
            return;
        }

        Notify("Deck sauvegardé.");
        await RefreshSavedDecksAsync();
    }

    private async Task LoadSelectedDeckPresetAsync()
    {
        if (savedDecksDropdown == null || savedDecksDropdown.value < 0 || savedDecksDropdown.value >= savedDecks.Count)
        {
            Notify("Aucun deck sauvegardé sélectionné.");
            return;
        }

        DeckViewModel selectedDeck = savedDecks[savedDecksDropdown.value];
        if (selectedDeck == null || string.IsNullOrWhiteSpace(selectedDeck.id))
        {
            Notify("Deck sélectionné invalide.");
            return;
        }

        JToken loadedDeck = await STSApiClient.LoadPvpDeckAsync(selectedDeck.id);
        List<string> loadedCardIds = ExtractCardIdListFromDeckToken(loadedDeck);

        selectedCardKeys.Clear();

        int skipped = 0;
        foreach (string cardId in loadedCardIds)
        {
            CardEntry entry = allEntries.FirstOrDefault(e =>
                string.Equals(e.key, cardId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.card.cardName, cardId, StringComparison.OrdinalIgnoreCase));

            if (entry == null || !entry.unlocked || !entry.characterCompatible)
            {
                skipped++;
                continue;
            }

            if (selectedCardKeys.Count >= maxDeckSize)
            {
                skipped++;
                continue;
            }

            selectedCardKeys.Add(entry.key);
        }

        RefreshGrid();

        if (skipped > 0)
        {
            Notify($"Deck chargé avec {skipped} carte(s) ignorée(s) car incompatibles/non débloquées.");
        }
        else
        {
            Notify("Deck chargé.");
        }
    }

    private async Task RefreshSavedDecksAsync()
    {
        savedDecks.Clear();

        JToken decksToken = await STSApiClient.ListPvpDecksAsync();
        if (decksToken != null)
        {
            foreach (JToken deckToken in ExtractDeckTokens(decksToken))
            {
                DeckViewModel model = ParseDeckModel(deckToken);
                if (model != null)
                {
                    savedDecks.Add(model);
                }
            }
        }

        if (savedDecksDropdown != null)
        {
            savedDecksDropdown.ClearOptions();
            List<string> names = savedDecks.Count == 0
                ? new List<string> { "Aucun deck" }
                : savedDecks.Select(d => d.name).ToList();
            savedDecksDropdown.AddOptions(names);
            savedDecksDropdown.value = 0;
            savedDecksDropdown.interactable = savedDecks.Count > 0;
        }

        if (loadDeckButton != null)
        {
            loadDeckButton.interactable = savedDecks.Count > 0;
        }
    }

    private IEnumerable<JToken> ExtractDeckTokens(JToken token)
    {
        if (token == null)
            yield break;

        if (token.Type == JTokenType.Array)
        {
            foreach (JToken item in token)
            {
                if (item != null)
                    yield return item;
            }

            yield break;
        }

        if (token.Type != JTokenType.Object)
            yield break;

        JObject obj = (JObject)token;
        foreach (string key in new[] { "decks", "items", "data", "result", "payload" })
        {
            if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nested) && nested != null)
            {
                foreach (JToken item in ExtractDeckTokens(nested))
                {
                    yield return item;
                }
                yield break;
            }
        }

        yield return token;
    }

    private DeckViewModel ParseDeckModel(JToken token)
    {
        if (token == null || token.Type != JTokenType.Object)
            return null;

        JObject obj = (JObject)token;
        string id = obj.Value<string>("id")
            ?? obj.Value<string>("deckId")
            ?? obj.Value<string>("uuid");
        string name = obj.Value<string>("name")
            ?? obj.Value<string>("deckName")
            ?? "Deck";

        List<string> cardIds = ExtractCardIdListFromDeckToken(obj);

        if (string.IsNullOrWhiteSpace(id))
            return null;

        return new DeckViewModel
        {
            id = id,
            name = name,
            cardIds = cardIds
        };
    }

    private List<string> ExtractCardIdListFromDeckToken(JToken token)
    {
        List<string> ids = new();
        if (token == null)
            return ids;

        if (token.Type == JTokenType.Object)
        {
            JObject obj = (JObject)token;
            foreach (string key in new[] { "cardIds", "cards", "deck", "deckCards" })
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nested))
                {
                    ExtractCardIdsFromToken(nested, ids);
                }
            }
        }
        else
        {
            ExtractCardIdsFromToken(token, ids);
        }

        return ids;
    }

    private void ExtractCardIdsFromToken(JToken token, List<string> target)
    {
        if (token == null)
            return;

        if (token.Type == JTokenType.String)
        {
            string value = token.Value<string>();
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value);
            }
            return;
        }

        if (token.Type == JTokenType.Array)
        {
            foreach (JToken child in token)
            {
                ExtractCardIdsFromToken(child, target);
            }
            return;
        }

        if (token.Type != JTokenType.Object)
            return;

        JObject obj = (JObject)token;
        string cardId = obj.Value<string>("cardId")
            ?? obj.Value<string>("id")
            ?? obj.Value<string>("collectionCardId");

        if (!string.IsNullOrWhiteSpace(cardId))
        {
            target.Add(cardId);
        }

        foreach (JProperty property in obj.Properties())
        {
            if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array)
            {
                ExtractCardIdsFromToken(property.Value, target);
            }
        }
    }

    private void CollectOwnedCardIds(JToken token, HashSet<string> target)
    {
        if (token == null)
            return;

        if (token.Type == JTokenType.String)
        {
            string value = token.Value<string>();
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value.Trim());
            }
            return;
        }

        if (token.Type == JTokenType.Array)
        {
            foreach (JToken child in token)
            {
                CollectOwnedCardIds(child, target);
            }
            return;
        }

        if (token.Type != JTokenType.Object)
            return;

        JObject obj = (JObject)token;

        string cardId = obj.Value<string>("cardId")
            ?? obj.Value<string>("collectionCardId")
            ?? obj.Value<string>("id");

        string cardName = obj.Value<string>("cardName")
            ?? obj.Value<string>("name");

        if (LooksLikeCardPayload(obj) && HasOwnedQuantity(obj))
        {
            if (!string.IsNullOrWhiteSpace(cardId))
            {
                target.Add(cardId.Trim());
            }

            // /api/cards/deck identifies cards by display name, which maps to collectionCardId here.
            if (!string.IsNullOrWhiteSpace(cardName))
            {
                target.Add(cardName.Trim());
            }
        }

        foreach (string key in new[] { "ownedCardIds", "unlockedCardIds", "cardIds", "cards", "deck", "items", "data", "result", "payload" })
        {
            if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nested) && nested != null)
            {
                CollectOwnedCardIds(nested, target);
            }
        }

        foreach (JProperty property in obj.Properties())
        {
            if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array)
            {
                CollectOwnedCardIds(property.Value, target);
            }
        }
    }

    // The deck endpoint also lists cards the player does not own, with quantity 0.
    private bool HasOwnedQuantity(JObject obj)
    {
        if (!obj.TryGetValue("quantity", StringComparison.OrdinalIgnoreCase, out JToken quantity) || quantity == null)
            return true;

        return (quantity.Value<int?>() ?? 0) > 0;
    }

    private bool LooksLikeCardPayload(JObject obj)
    {
        if (obj == null)
            return false;

        if (obj.TryGetValue("cardId", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("collectionCardId", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("collectionId", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("cardName", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("quantity", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("rarity", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("targetingMode", StringComparison.OrdinalIgnoreCase, out _)
            || obj.TryGetValue("id", StringComparison.OrdinalIgnoreCase, out _))
        {
            return true;
        }

        if (obj.TryGetValue("collectionType", StringComparison.OrdinalIgnoreCase, out JToken collectionType)
            && string.Equals(collectionType?.Value<string>(), "VIRTUAL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private int ResolveCharacterLevel(JToken profile, SelectableCharacter character)
    {
        if (profile == null)
            return 0;

        int direct = profile.Value<int?>("characterLevel") ?? 0;
        if (direct > 0)
            return direct;

        JToken levels = profile["characterLevels"];
        if (levels is JObject levelsObject)
        {
            int level = levelsObject.Value<int?>(character.ToString()) ?? 0;
            return Math.Max(0, level);
        }

        return 0;
    }

    private string GetCardKey(STSCardData card)
    {
        if (card == null)
            return null;

        if (!string.IsNullOrWhiteSpace(card.id))
            return card.id;

        return card.cardName;
    }

    private void RefreshDeckCounter()
    {
        if (counterText == null)
            return;

        counterText.text = $"{selectedCardKeys.Count}/{maxDeckSize}";
    }

    private void UpdateStatusWithCounts()
    {
        if (statusText == null)
            return;

        int shown = visibleEntries.Count;
        int total = allEntries.Count;
        if (missingApiResponses)
        {
            statusText.text = $"Cartes affichées: {shown}/{total} (API indisponible)";
            return;
        }

        statusText.text = $"Cartes affichées: {shown}/{total}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void Notify(string message)
    {
        host?.ShowNotification(message);
        SetStatus(message);
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
        host?.ShowConfigurationPanel();
    }
}
