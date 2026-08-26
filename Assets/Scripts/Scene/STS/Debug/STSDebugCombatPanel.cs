using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug menu that builds an arbitrary STS battle — enemies, character, deck and relics — and
/// launches it against the backend combat engine.
/// </summary>
/// <remarks>
/// Requires <c>app.sts.debug.combat.enabled=true</c> on the backend: launching rewrites the
/// caller's own run (player, deck, relics, active encounter) with the chosen configuration.
/// </remarks>
public class STSDebugCombatPanel : MonoBehaviour
{
    [Serializable]
    public class ListSection
    {
        public TMP_InputField input;
        public Button addButton;
        public Button removeButton;
        public Button clearButton;
        public TMP_Text entriesText;
        [Tooltip("Parent under which one clickable button per suggestion is created.")]
        public Transform suggestionsContainer;
        public Button suggestionButtonPrefab;
    }

    [Header("Enemies (required)")]
    public ListSection enemies;

    [Header("Cards (empty = character starting deck)")]
    public ListSection cards;

    [Header("Relics (empty = none)")]
    public ListSection relics;

    [Header("Character")]
    public TMP_InputField characterInput;
    public TMP_InputField maxHpInput;

    [Header("Actions")]
    public Button launchButton;
    public Button clearAllButton;
    public TMP_Text statusText;

    [Header("Scenes")]
    public string combatSceneName = "STS_Combat";
    [Tooltip("Scene loaded again once the debug battle ends. Defaults to the scene holding this panel.")]
    public string returnSceneName;

    [Tooltip("How many catalog names to show while typing.")]
    public int suggestionCount = 8;

    readonly List<string> enemyIds = new();
    readonly List<string> cardIds = new();
    readonly List<string> relicIds = new();

    bool catalogsReady;
    bool launching;

    void Start()
    {
        if (string.IsNullOrWhiteSpace(returnSceneName))
            returnSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        BindSection(enemies, enemyIds, ResolveEnemyId, SuggestEnemies);
        BindSection(cards, cardIds, ResolveCardId, SuggestCards);
        BindSection(relics, relicIds, ResolveRelicId, SuggestRelics);

        if (launchButton != null)
        {
            launchButton.onClick.AddListener(() => _ = LaunchAsync());
            launchButton.interactable = false;
        }
        if (clearAllButton != null)
            clearAllButton.onClick.AddListener(ClearAll);

        _ = LoadCatalogsAsync();
    }

    void BindSection(ListSection section, List<string> entries, Func<string, string> resolve, Func<string, List<string>> suggest)
    {
        if (section == null)
            return;

        if (section.addButton != null)
            section.addButton.onClick.AddListener(() => Add(section, entries, resolve));
        if (section.removeButton != null)
            section.removeButton.onClick.AddListener(() => Remove(section, entries, resolve));
        if (section.clearButton != null)
            section.clearButton.onClick.AddListener(() =>
            {
                entries.Clear();
                RefreshEntries(section, entries);
            });
        if (section.input != null)
        {
            // Enter in the field is the same as pressing Add, so a list can be typed without
            // reaching for the mouse between every entry.
            section.input.onSubmit.AddListener(_ => Add(section, entries, resolve));
            if (section.suggestionsContainer != null)
                section.input.onValueChanged.AddListener(value => RefreshSuggestions(section, entries, resolve, suggest(value)));
        }

        RefreshEntries(section, entries);
    }

    async Task LoadCatalogsAsync()
    {
        SetStatus("Chargement des catalogues...");
        try
        {
            await STSCardDatabase.LoadAsync();
            await PlayersDatabase.LoadAsync();
            await EnemyDataDatabase.LoadAsync();
            await EnemyPoolDatabase.LoadAsync();
            catalogsReady = true;
            SetStatus("Prêt.");
        }
        catch (Exception ex)
        {
            SetStatus($"Échec du chargement des catalogues : {ex.Message}", true);
        }

        if (launchButton != null)
            launchButton.interactable = catalogsReady;
    }

    void Add(ListSection section, List<string> entries, Func<string, string> resolve)
    {
        if (section == null || section.input == null)
            return;

        TryAdd(section, entries, resolve, section.input.text?.Trim());
    }

    void TryAdd(ListSection section, List<string> entries, Func<string, string> resolve, string typed)
    {
        if (string.IsNullOrEmpty(typed))
            return;

        string resolved = resolve(typed);
        if (resolved == null)
        {
            SetStatus($"Inconnu : '{typed}'.", true);
            return;
        }

        entries.Add(resolved);
        if (section.input != null)
        {
            section.input.text = string.Empty;
            section.input.ActivateInputField();
        }
        RefreshEntries(section, entries);
        SetStatus($"Ajouté : {resolved}.");
    }

    // One clickable button per suggestion; clicking adds it straight to the list.
    void RefreshSuggestions(ListSection section, List<string> entries, Func<string, string> resolve, List<string> suggestions)
    {
        if (section == null || section.suggestionsContainer == null || section.suggestionButtonPrefab == null)
            return;

        foreach (Transform child in section.suggestionsContainer)
            Destroy(child.gameObject);

        foreach (string suggestion in suggestions)
        {
            string captured = suggestion;
            Button button = Instantiate(section.suggestionButtonPrefab, section.suggestionsContainer);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = captured;
            button.onClick.AddListener(() => TryAdd(section, entries, resolve, captured));
        }
    }

    // Removes the typed entry, or the last one added when the field is empty: repeatedly
    // pressing Remove then walks the list back.
    void Remove(ListSection section, List<string> entries, Func<string, string> resolve)
    {
        if (entries.Count == 0)
            return;

        string typed = section != null && section.input != null ? section.input.text?.Trim() : null;
        if (string.IsNullOrEmpty(typed))
        {
            entries.RemoveAt(entries.Count - 1);
        }
        else
        {
            string resolved = resolve(typed) ?? typed;
            int index = entries.FindLastIndex(entry => string.Equals(entry, resolved, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                SetStatus($"'{typed}' n'est pas dans la liste.", true);
                return;
            }
            entries.RemoveAt(index);
        }

        RefreshEntries(section, entries);
    }

    void RefreshEntries(ListSection section, List<string> entries)
    {
        if (section == null || section.entriesText == null)
            return;

        if (entries.Count == 0)
        {
            section.entriesText.text = "<i>(vide)</i>";
            return;
        }

        // Repeats are meaningful here (three copies of a card, two of the same enemy), so they
        // are counted rather than collapsed.
        var counts = new List<string>();
        foreach (var group in entries.GroupBy(entry => entry, StringComparer.Ordinal))
        {
            counts.Add(group.Count() > 1 ? $"{group.Key} x{group.Count()}" : group.Key);
        }
        section.entriesText.text = string.Join("\n", counts);
    }

    void ClearAll()
    {
        enemyIds.Clear();
        cardIds.Clear();
        relicIds.Clear();
        RefreshEntries(enemies, enemyIds);
        RefreshEntries(cards, cardIds);
        RefreshEntries(relics, relicIds);
        SetStatus("Configuration vidée.");
    }

    string ResolveEnemyId(string typed)
    {
        EnemyData data = EnemyDataDatabase.Get(typed);
        if (data == null && EnemyDataDatabase.allEnemies != null)
        {
            data = EnemyDataDatabase.allEnemies.FirstOrDefault(enemy => enemy != null
                && (Matches(enemy.id, typed) || Matches(enemy.enemyName, typed) || Matches(enemy.displayName, typed)));
        }
        if (data == null)
            return null;

        return !string.IsNullOrWhiteSpace(data.id) ? data.id : data.enemyName;
    }

    // Not STSCardDatabase.Get: it logs an error for every miss, and a debug field that is still
    // being typed into misses constantly.
    string ResolveCardId(string typed)
    {
        if (STSCardDatabase.allCards == null)
            return null;

        STSCardData card = STSCardDatabase.allCards.FirstOrDefault(candidate => candidate != null
            && (Matches(candidate.id, typed) || Matches(candidate.cardName, typed)));
        if (card == null)
            return null;

        return !string.IsNullOrWhiteSpace(card.id) ? card.id : card.cardName;
    }

    // A relic's backend id is its C# class name, which is also what STSApiClient rebuilds from.
    string ResolveRelicId(string typed)
    {
        Relic relic = RelicDatabase.All?.FirstOrDefault(candidate => candidate != null
            && (Matches(candidate.GetType().Name, typed) || Matches(candidate.name, typed)));
        if (relic != null)
            return relic.GetType().Name;

        return STSApiClient.CreateRelicFromId(typed) != null ? typed : null;
    }

    List<string> SuggestEnemies(string typed)
    {
        return Suggest(EnemyDataDatabase.allEnemies?.Select(enemy => enemy != null
            ? (!string.IsNullOrWhiteSpace(enemy.displayName) ? enemy.displayName : enemy.id)
            : null), typed);
    }

    List<string> SuggestCards(string typed)
    {
        return Suggest(STSCardDatabase.allCards?.Select(card => card != null
            ? (!string.IsNullOrWhiteSpace(card.cardName) ? card.cardName : card.id)
            : null), typed);
    }

    List<string> SuggestRelics(string typed)
    {
        return Suggest(RelicDatabase.All?.Select(relic => relic != null ? relic.GetType().Name : null), typed);
    }

    List<string> Suggest(IEnumerable<string> names, string typed)
    {
        if (names == null || string.IsNullOrWhiteSpace(typed))
            return new List<string>();

        return names
            .Where(name => !string.IsNullOrWhiteSpace(name)
                && name.IndexOf(typed.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Mathf.Max(1, suggestionCount))
            .ToList();
    }

    static bool Matches(string candidate, string typed)
    {
        return !string.IsNullOrWhiteSpace(candidate)
            && string.Equals(candidate.Trim(), typed, StringComparison.OrdinalIgnoreCase);
    }

    async Task LaunchAsync()
    {
        if (launching)
            return;

        if (enemyIds.Count == 0)
        {
            SetStatus("Ajoute au moins un ennemi.", true);
            return;
        }

        if (RunManager.Instance == null)
        {
            SetStatus("Aucun RunManager : lance cette scène depuis STS_Boot.", true);
            return;
        }

        launching = true;
        if (launchButton != null)
            launchButton.interactable = false;

        try
        {
            string character = ResolveCharacter();
            SetStatus("Préparation de la run...");
            string runId = await EnsureRunIdAsync(character);
            if (string.IsNullOrWhiteSpace(runId))
            {
                SetStatus("Impossible d'obtenir une run active.", true);
                return;
            }

            int maxHp = 0;
            if (maxHpInput != null && int.TryParse(maxHpInput.text, out int parsedHp) && parsedHp > 0)
                maxHp = parsedHp;

            SetStatus("Création du combat côté serveur...");
            STSApiDebugCombatResponse response = await STSApiClient.StartDebugCombatAsync(runId, new STSApiDebugCombatRequest
            {
                character = character,
                maxHp = maxHp,
                enemyIds = new List<string>(enemyIds),
                cardIds = new List<string>(cardIds),
                relicIds = new List<string>(relicIds)
            });

            if (response == null || !response.accepted || response.activeCombat == null || response.activeEncounter == null)
            {
                SetStatus("Le serveur a refusé le combat de debug (endpoint désactivé ou configuration invalide).", true);
                return;
            }

            ApplyToRunManager(runId, character, response);
            SetStatus("Lancement du combat...");
            STSSceneLoader.Instance?.LoadScene(combatSceneName);
        }
        catch (Exception ex)
        {
            SetStatus($"Échec du lancement : {ex.Message}", true);
        }
        finally
        {
            launching = false;
            if (launchButton != null)
                launchButton.interactable = catalogsReady;
        }
    }

    string ResolveCharacter()
    {
        string typed = characterInput != null ? characterInput.text?.Trim() : null;
        if (!string.IsNullOrEmpty(typed) && Enum.TryParse(typed, true, out SelectableCharacter parsed))
            return parsed.ToString();

        if (RunManager.Instance != null
            && RunManager.Instance.selectedCharacter != SelectableCharacter.Aucun)
        {
            return RunManager.Instance.selectedCharacter.ToString();
        }

        return SelectableCharacter.EP.ToString();
    }

    async Task<string> EnsureRunIdAsync(string character)
    {
        if (!string.IsNullOrWhiteSpace(RunManager.Instance.runId))
            return RunManager.Instance.runId;

        STSApiCurrentRunResponse current = await STSApiClient.CurrentRunAsync();
        if (current != null && current.hasRun && current.run != null && !string.IsNullOrWhiteSpace(current.run.runId))
        {
            RunManager.Instance.ApplyRemoteRunIfAvailable(current.run);
            return RunManager.Instance.runId;
        }

        STSApiRunCreateResponse created = await STSApiClient.CreateRunAsync(character, Application.version);
        if (created == null || string.IsNullOrWhiteSpace(created.runId))
            return null;

        RunManager.Instance.ApplyRemoteRunIfAvailable(created);
        return RunManager.Instance.runId;
    }

    void ApplyToRunManager(string runId, string character, STSApiDebugCombatResponse response)
    {
        RunManager run = RunManager.Instance;
        run.runId = runId;
        run.debugCombat = true;
        run.debugCombatReturnScene = returnSceneName;
        run.forceTutorial = false;
        run.eliteEncounter = false;
        run.bossEncounter = false;
        run.pendingReward = null;

        if (Enum.TryParse(character, true, out SelectableCharacter parsed))
            run.selectedCharacter = parsed;

        int playerMaxHp = response.player != null ? Mathf.Max(1, response.player.maxHp) : 100;
        run.player = new Player(character, playerMaxHp)
        {
            currentHP = response.player != null ? Mathf.Max(1, response.player.currentHp) : playerMaxHp
        };

        run.deck = response.runInventory != null
            ? STSApiClient.ConvertDeck(response.runInventory.deck)
            : new List<CardInstance>();
        run.relics = response.runInventory != null
            ? STSApiClient.ConvertRelics(response.runInventory.relics)
            : new List<Relic>();

        run.activeEncounter = response.activeEncounter;
        run.activeCombat = STSApiClient.NormalizeOptionalToken(response.activeCombat);
        run.activeEvent = null;
        if (run.ui != null)
            run.ui.gameObject.SetActive(true);
    }

    void SetStatus(string message, bool isError = false)
    {
        if (isError)
            Debug.LogWarning($"[STS-DEBUG] {message}");
        else
            Debug.Log($"[STS-DEBUG] {message}");

        if (statusText == null)
            return;

        statusText.color = isError ? new Color(1f, 0.45f, 0.45f) : Color.white;
        statusText.text = message;
    }
}
