using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
public class RunManager : MonoBehaviour
{
    public static RunManager Instance;

    public string runId;

    /// Le serveur fait autorité dès qu'une run lui appartient.

    public bool IsServerAuthoritative => STSServerAuthority.Decides(runId);
    public string apiStatus;
    public string dataVersion;
    public Player player;
    public SelectableCharacter selectedCharacter;
    public int currentFloor;
    public List<CardInstance> deck = new();
    public List<Relic> relics = new();
    public Reward pendingReward;
    public bool eliteEncounter;
    public bool bossEncounter;
    public List<MapNode> map=null;
    public MapNode currentNode;
    public int? enteredNodeId;
    public bool RegenerateMap = false;
    public int act=0;
    public int restCharges=3;
    public int maxRestCharges=15;
    public RunManagerUI ui;
    public int gold=0;
    public bool forceTutorial=false;
    public bool addAllCardsToDeck=false;//Debug option to add all cards to the deck for testing purposes
    public List<string> debugCards=new List<string>();//Debug option to specify which cards to add to the deck when addAllCardsToDeck is true
    [HideInInspector] public bool inCombat=false;
    public STSApiActiveEncounterState activeEncounter;
    public JToken activeCombat;
    public JToken activeEvent;
    public JToken serverRunInventoryPatch;
    public JToken serverAccountInventoryPatch;
    public List<JToken> serverPendingRewards = new();
    public STSApiMapPatchState serverMapPatch;
    public bool backendRewardClaimUnavailable;
    public bool completedFinalAct;
    public bool unrestrictedMode;
    public string unrestrictedModeReason;
    // Set by STSDebugCombatPanel: this combat belongs to no map node, so it must not be reported
    // as a node completion and it returns to the debug scene instead of the reward screen.
    [HideInInspector] public bool debugCombat;
    [HideInInspector] public string debugCombatReturnScene;
    public string pvpLocalUserId;
    public string pvpBattleId;

    /// La bataille effectivement en train d'etre jouee.
    ///
    /// Distinct de `pvpBattleId`, qui ne fait que retenir la derniere bataille annoncee
    /// par le matchmaking et n'est efface qu'en fin de run. Tout ce qui doit se comporter
    /// autrement en duel s'appuie sur ce champ-ci, de sorte qu'un matchmaking passe ne
    /// puisse jamais faire croire a une rencontre PvE qu'elle est un duel.
    public string activePvpBattleId;

    /// Le menu multijoueur doit relancer une recherche des son ouverture. Ecrit par le
    /// bouton « Revanche » de l'ecran de fin de duel, et consomme une seule fois : il
    /// n'existe pas d'endpoint de revanche cote serveur, c'est un nouveau matchmaking.
    public bool requestPvpQuickMatch;
    public List<STSApiClient.StsPvpParticipantSnapshot> pvpParticipants = new();
    void Update()
    {
        if (SceneManager.GetActiveScene().name != "STS_Combat" && player != null && player.currentHP <= 0)
        {
            player.currentHP = 1;
        }
    }
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ui=GetComponentInChildren<RunManagerUI>();
        if (ui!=null)
        {
            ui.gameObject.SetActive(false);
        }
        // Ensure this canvas is always on top
        var canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 1500; // Set to a very high value to guarantee it's above all others
        }
    }
    public void AddRelic(Relic relic)
    {
        relics.Add(relic);
        relic.OnAcquire(player);
    }
    private bool startingRun = false;
    public async Task StartRunAsync(string character, int maxHP, List<Relic> startingRelics, bool startOnMap = true, bool forceTutorial = false, int tutorialStage = 0, string nextSceneName = null, bool preferFreshRun = false)
    {
        Debug.Log($"[STS-RUN] StartRunAsync requested character={character} forceTutorial={forceTutorial} startOnMap={startOnMap} existingRunId={runId}");
        // First end other executions of StartRun to prevent multiple runs from starting at the same time
        if (startingRun)
        {
            Debug.LogWarning("A run is already starting. Ignoring this StartRun call.");
            return;
        }
        startingRun = true;
        STSSceneLoader.Instance?.BeginLoading();
        STSSceneLoader.Instance?.SetBackgroundProgress(0.05f);

        bool loadedScene = false;

        try
        {
            OnRunEnd(true, false);
            SetUnrestrictedMode(false, null);
            this.forceTutorial = forceTutorial;
            completedFinalAct = false;
            act = tutorialStage;
            if (ui != null)
            {
                ui.gameObject.SetActive(true);
            }

            STSSceneLoader.Instance?.SetBackgroundProgress(0.12f);
            await STSCardDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.36f);
            await PlayersDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.44f);
            await EnemyDataDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.56f);
            await EnemyPoolDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.68f);

            STSApiRunCreateResponse remoteRun = null;
            if (!forceTutorial)
            {
                try
                {
                    STSSceneLoader.Instance?.SetBackgroundProgress(0.76f);
                    remoteRun = await STSApiClient.CreateRunAsync(character, Application.version);
                    Debug.Log($"[STS-RUN] CreateRunAsync returned runId={remoteRun?.runId} status={remoteRun?.status}");

                    if (preferFreshRun
                        && remoteRun != null
                        && !string.IsNullOrWhiteSpace(remoteRun.runId)
                        && ShouldRestartForFreshCharacter(remoteRun, character))
                    {
                        Debug.LogWarning($"[STS-RUN] CreateRunAsync resumed an existing run while a fresh run was requested. Resetting runId={remoteRun.runId} and recreating.");
                        try
                        {
                            await STSApiClient.ResetRunAsync(remoteRun.runId);
                        }
                        catch (Exception resetEx)
                        {
                            Debug.LogWarning($"[STS-RUN] Failed to reset resumed run before fresh start: {resetEx.Message}");
                        }

                        remoteRun = await STSApiClient.CreateRunAsync(character, Application.version);
                        Debug.Log($"[STS-RUN] Recreate after reset returned runId={remoteRun?.runId} status={remoteRun?.status}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Remote STS run creation failed, attempting active-run recovery before local fallback: {ex.Message}");

                    try
                    {
                        STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
                        if (currentRun != null && currentRun.hasRun && currentRun.run != null)
                        {
                            remoteRun = currentRun.run;
                            Debug.Log($"[STS-RUN] Recovered active run after create failure runId={remoteRun.runId} status={remoteRun.status}");
                        }
                        else
                        {
                            EnableUnrestrictedMode($"run creation failed: {ex.Message}");
                        }
                    }
                    catch (Exception recoveryEx)
                    {
                        Debug.LogWarning($"[STS-RUN] Active-run recovery also failed: {recoveryEx.Message}");
                        EnableUnrestrictedMode($"run creation failed: {ex.Message}");
                    }
                }
            }
            STSSceneLoader.Instance?.SetBackgroundProgress(0.84f);

            if (!forceTutorial && ApplyRemoteRunIfAvailable(remoteRun))
            {
                STSSceneLoader.Instance?.SetBackgroundProgress(0.90f);
                if (startOnMap)
                {
                    STSSceneLoader.Instance?.LoadScene(ResolveRemoteResumeScene());
                    loadedScene = true;
                }
                else if (!string.IsNullOrEmpty(nextSceneName))
                {
                    STSSceneLoader.Instance?.LoadScene(nextSceneName);
                    loadedScene = true;
                }

                STSRunAuditSystem.RecordRunStarted(this);
                return;
            }

            gold = 0;
            if (!forceTutorial && !unrestrictedMode)
            {
                EnableUnrestrictedMode("remote run could not be initialized");
            }
            if (Enum.TryParse(character, out SelectableCharacter parsedCharacter))
            {
                selectedCharacter = parsedCharacter;
            }
            else
            {
                Debug.LogError($"Invalid character: {character}. No character selected.");
                selectedCharacter = SelectableCharacter.Aucun;
            }

            player = new Player(character, maxHP);
            relics = startingRelics;
            currentFloor = 1;
            RegenerateMap = true;

            deck.Clear();
            foreach (STSCardData card in STSCardDatabase.allCards)
            {
                if ((addAllCardsToDeck&&(debugCards.Contains(card.cardName)||(debugCards.Count == 0&&card.favoredCharacter==selectedCharacter))) 
                || (card.startingCount > 0 
                && (card.favoredCharacter == SelectableCharacter.Starting || card.favoredCharacter == selectedCharacter || card.favoredCharacter == SelectableCharacter.Aucun)))
                {
                    for (int i = 0; i < (addAllCardsToDeck ? 1 : card.startingCount); i++)
                    {
                        deck.Add(new CardInstance(card));
                    }
                }
            }

            STSSceneLoader.Instance?.SetBackgroundProgress(0.92f);

            if (startOnMap)
            {
                STSSceneLoader.Instance?.LoadScene("STS_Map");
                loadedScene = true;
            }
            else if (!string.IsNullOrEmpty(nextSceneName))
            {
                STSSceneLoader.Instance?.LoadScene(nextSceneName);
                loadedScene = true;
            }

            STSRunAuditSystem.RecordRunStarted(this);
        }
        finally
        {
            if (loadedScene)
            {
                STSSceneLoader.Instance?.EndLoading();
            }
            startingRun = false;
        }
    }

    public async void StartRun(string character, int maxHP, List<Relic> startingRelics, bool startOnMap = true, bool forceTutorial = false, int tutorialStage = 0, bool preferFreshRun = false)
    {
        await StartRunAsync(character, maxHP, startingRelics, startOnMap, forceTutorial, tutorialStage, null, preferFreshRun);
    }

    public void OnRunEnd()
    {
        OnRunEnd(true, true);
    }

    public void OnRunEnd(bool clearSave)
    {
        OnRunEnd(clearSave, true);
    }

    private bool runEndUnlocksGranted;
    public List<STSCardData> lastRunEndUnlockedCards = new();

    public List<STSCardData> GrantRunEndUnlocks(bool wasRetreat)
    {
        if (runEndUnlocksGranted || deck == null || deck.Count == 0)
        {
            return new List<STSCardData>();
        }

        if (selectedCharacter == SelectableCharacter.Aucun
            || selectedCharacter == SelectableCharacter.Starting
            || selectedCharacter == SelectableCharacter.Impossible)
        {
            return new List<STSCardData>();
        }

        List<STSCardData> unlocked = STSPlayerProfileStore.UnlockCardsFromDeck(deck, selectedCharacter, wasRetreat, act);
        runEndUnlocksGranted = true;
        lastRunEndUnlockedCards = unlocked;
        Debug.Log($"[STS-RUN] Granted end-of-run unlocks for {selectedCharacter} (retreat={wasRetreat}, act={act}, deckSize={deck.Count}, unlockedCount={unlocked.Count}).");

        if (unlocked.Count > 0)
        {
            List<string> collectionCardNames = unlocked
                .Select(card => card.GetCollectionCardId() ?? card.cardName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
            if (collectionCardNames.Count > 0)
            {
                _ = STSApiClient.UnlockPvpCardsAsync(runId, collectionCardNames);
            }
        }

        return unlocked;
    }

    public void OnRunEnd(bool clearSave, bool resetRemoteRun)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[STS-RUN] OnRunEnd(clearSave={clearSave}, resetRemoteRun={resetRemoteRun}, runId={runId}, scene={currentScene}, completedFinalAct={completedFinalAct})");
        STSRunAuditSystem.RecordRunEnded(this, clearSave ? "clear_save" : "preserve_save");

        if (!runEndUnlocksGranted)
        {
            GrantRunEndUnlocks(currentScene == "STS_Retreat");
        }

        if (resetRemoteRun && clearSave && !string.IsNullOrWhiteSpace(runId) && !unrestrictedMode)
        {
            _ = STSApiClient.ResetRunAsync(runId);
        }

        if (clearSave)
        {
            STSRunSaveSystem.ClearSave();
        }

        gold=0;
        if (ui != null)
        {
            ui.gameObject.SetActive(false);
        }
        player = null;
        deck.Clear();
        relics.Clear();
        pendingReward = null;
        currentNode = null;
        map = null;
        activeEncounter = null;
        activeCombat = null;
        activeEvent = null;
        enteredNodeId = null;
        completedFinalAct = false;
        backendRewardClaimUnavailable = false;
        debugCombat = false;
        debugCombatReturnScene = null;
        pvpLocalUserId = null;
        activePvpBattleId = null;
        requestPvpQuickMatch = false;
        ClearPvpBattleParticipants();
        SetUnrestrictedMode(false, null);
        if (clearSave)
        {
            runId = null;
            apiStatus = null;
            dataVersion = null;
            serverRunInventoryPatch = null;
            serverAccountInventoryPatch = null;
            serverPendingRewards.Clear();
            serverMapPatch = null;
        }
    }

    public bool SaveRunState()
    {
        return STSRunSaveSystem.SaveRun(this);
    }

    public bool LoadSavedRun()
    {
        bool loaded = STSRunSaveSystem.LoadRun(this);
        if (loaded && ui != null)
        {
            ui.gameObject.SetActive(true);
        }

        if (loaded)
        {
            STSRunAuditSystem.EnsureRunId(this);
        }

        return loaded;
    }

    public bool ApplyRemoteRunIfAvailable(STSApiRunCreateResponse remoteRun)
    {
        if (remoteRun == null || string.IsNullOrWhiteSpace(remoteRun.runId))
            return false;

        STSApiRunState remoteState = STSApiClient.ConvertToRunState(remoteRun);
        if (remoteState == null)
            return false;

        runId = remoteState.runId;
        apiStatus = remoteState.status;
        dataVersion = remoteState.dataVersion;

        if (Enum.TryParse(remoteState.selectedCharacter, out SelectableCharacter parsedCharacter))
        {
            selectedCharacter = parsedCharacter;
        }

        act = remoteState.act;
        currentFloor = remoteState.currentFloor;
        gold = remoteState.gold;
        player = new Player(remoteState.selectedCharacter, Mathf.Max(1, remoteState.playerMaxHp))
        {
            currentHP = remoteState.playerCurrentHp
        };
        deck = remoteState.deck ?? new List<CardInstance>();
        relics = remoteState.relics ?? new List<Relic>();
        map = remoteState.map ?? new List<MapNode>();
        enteredNodeId = remoteState.enteredNodeId;
        int resumeNodeId = enteredNodeId ?? remoteState.currentNodeId;
        currentNode = map != null ? map.Find(n => n != null && n.id == resumeNodeId) : null;
        if (currentNode == null && map != null && map.Count > 0)
        {
            currentNode = map[0];
        }

        RegenerateMap = false;
        activeEncounter = remoteState.activeEncounter;
        activeCombat = STSApiClient.NormalizeOptionalToken(remoteState.activeCombat);
        activeEvent = STSApiClient.NormalizeOptionalToken(remoteState.activeEvent);
        pendingReward = null;
        serverPendingRewards = remoteRun.pendingRewards != null
            ? new List<JToken>(remoteRun.pendingRewards)
            : new List<JToken>();
        return true;
    }

    public void ApplyRemoteRunState(STSApiRunState remoteState, List<JToken> pendingRewards = null)
    {
        if (remoteState == null)
            return;

        runId = remoteState.runId;
        apiStatus = remoteState.status;
        dataVersion = remoteState.dataVersion;

        if (Enum.TryParse(remoteState.selectedCharacter, out SelectableCharacter parsedCharacter))
        {
            selectedCharacter = parsedCharacter;
        }

        act = remoteState.act;
        currentFloor = remoteState.currentFloor;
        gold = remoteState.gold;
        player = new Player(remoteState.selectedCharacter, Mathf.Max(1, remoteState.playerMaxHp))
        {
            currentHP = remoteState.playerCurrentHp
        };
        deck = remoteState.deck ?? new List<CardInstance>();
        relics = remoteState.relics ?? new List<Relic>();
        map = remoteState.map ?? new List<MapNode>();
        enteredNodeId = remoteState.enteredNodeId;
        int resumeNodeId = enteredNodeId ?? remoteState.currentNodeId;
        currentNode = map != null ? map.Find(n => n != null && n.id == resumeNodeId) : null;
        if (currentNode == null && map != null && map.Count > 0)
        {
            currentNode = map[0];
        }

        RegenerateMap = false;
        activeEncounter = remoteState.activeEncounter;
        activeCombat = STSApiClient.NormalizeOptionalToken(remoteState.activeCombat);
        activeEvent = STSApiClient.NormalizeOptionalToken(remoteState.activeEvent);
        pendingReward = null;
        serverPendingRewards = pendingRewards != null
            ? new List<JToken>(pendingRewards)
            : new List<JToken>();
    }

    public string ResolveRemoteResumeScene()
    {
        string enteredNodeType = enteredNodeId.HasValue && currentNode != null
            ? currentNode.type.ToString()
            : null;
        string currentNodeType = currentNode != null ? currentNode.type.ToString() : null;
        bool hasPendingRewards = serverPendingRewards != null && serverPendingRewards.Count > 0;

        STSRunResumePhase phase = STSRunResumeResolver.Resolve(
            activeEncounter != null,
            activeEvent != null,
            enteredNodeType,
            hasPendingRewards,
            currentNodeType,
            currentNode != null && currentNode.completed);

        bossEncounter = currentNode != null && currentNode.type == NodeType.Boss;
        eliteEncounter = currentNode != null && currentNode.type == NodeType.Elite;
        completedFinalAct = bossEncounter
            && currentNode.completed
            && EnemyPoolDatabase.IsLastAct(act);

        return phase switch
        {
            STSRunResumePhase.Combat => "STS_Combat",
            STSRunResumePhase.Event => "STS_Event",
            STSRunResumePhase.Rest => "STS_Rest",
            STSRunResumePhase.Reward => "STS_Reward",
            STSRunResumePhase.Retreat => "STS_Retreat",
            _ => "STS_Map"
        };
    }

    public List<JToken> ConsumeServerPendingRewards()
    {
        if (serverPendingRewards == null || serverPendingRewards.Count == 0)
        {
            return new List<JToken>();
        }

        List<JToken> consumed = new List<JToken>(serverPendingRewards);
        serverPendingRewards.Clear();
        return consumed;
    }

    public void ActAndRegenerateLocally()
    {
        RegenerateMap = true;
        act++;
    }

    public void EnableUnrestrictedMode(string reason)
    {
        SetUnrestrictedMode(true, reason);
    }

    public void SetUnrestrictedMode(bool enabled, string reason)
    {
        unrestrictedMode = enabled;
        unrestrictedModeReason = enabled ? reason : null;

        if (enabled)
        {
            apiStatus = "Unrestricted";
            Debug.LogWarning(string.IsNullOrWhiteSpace(reason)
                ? "[STS-RUN] Switching to unrestricted mode."
                : $"[STS-RUN] Switching to unrestricted mode: {reason}");
        }

        if (ui != null)
        {
            ui.SetUnrestrictedMode(enabled);
        }
    }

    bool ShouldRestartForFreshCharacter(STSApiRunCreateResponse remoteRun, string requestedCharacter)
    {
        if (remoteRun == null)
            return false;

        if (!string.IsNullOrWhiteSpace(remoteRun.selectedCharacter)
            && !string.IsNullOrWhiteSpace(requestedCharacter)
            && !string.Equals(remoteRun.selectedCharacter, requestedCharacter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // If run already progressed beyond the entry node, this is clearly not a fresh run.
        if (remoteRun.currentFloor > 0 || remoteRun.currentNodeId > 0)
        {
            return true;
        }

        return false;
    }

    public void ApplyNodeEnterResponse(STSApiNodeEnterResponse response)
    {
        if (response == null)
            return;

        if (!response.accepted)
            return;

        if (!string.IsNullOrWhiteSpace(response.runId))
        {
            runId = response.runId;
        }

        if (response.activeEncounter != null)
        {
            activeEncounter = response.activeEncounter;
        }
        activeCombat = STSApiClient.NormalizeOptionalToken(response.activeCombat);
        activeEvent = STSApiClient.NormalizeOptionalToken(response.activeEvent);
        enteredNodeId = response.nodeId;

        // Un feu de camp pose les charges et fait jouer les reliques de repos côté
        // serveur : sans cette reprise, la scène s'ouvrirait sur les charges d'avant
        // et ignorerait le soin qu'on vient de recevoir.
        if (response.player != null && player != null)
        {
            player.maxHP = response.player.maxHp;
            player.currentHP = response.player.currentHp;
        }
        ApplyServerRestCharges(response.player);

        if (map != null)
        {
            MapNode entered = map.Find(n => n != null && n.id == response.nodeId);
            if (entered != null)
            {
                entered.visited = true;
            }
        }
    }

    /// <summary>
    /// Applique ce que le serveur vient de changer dans l'inventaire.
    ///
    /// <para>Sans ça, le patch était rangé et jamais ouvert : le joueur ne voyait ni
    /// l'or ni les cartes gagnés avant un resynchro complet. L'état serveur était bon,
    /// c'est l'affichage qui restait en arrière — et rien ne le signalait.</para>
    /// </summary>
    /// <summary>
    /// Reprend les charges de feu de camp que le serveur annonce.
    /// </summary>
    /// <remarks>
    /// Zéro y est ambigu : c'est aussi ce que vaut le champ quand la réponse ne le
    /// porte pas, faute de nullable dans les DTO. On ne l'accepte donc que si le
    /// serveur annonce un maximum, seul cas où il a réellement parlé de charges.
    /// </remarks>
    private void ApplyServerRestCharges(STSApiPlayerState serverPlayer)
    {
        if (serverPlayer == null || serverPlayer.maxRestCharges <= 0)
            return;

        maxRestCharges = serverPlayer.maxRestCharges;
        restCharges = serverPlayer.restCharges;
    }

    public void ApplyRunInventoryPatch(JToken rawPatch)
    {
        STSInventoryPatch patch = STSInventoryPatch.Read(rawPatch);

        gold += patch.GoldDelta;

        if (deck != null && patch.RemovedCardInstanceIds.Count > 0)
        {
            var removed = new HashSet<string>(patch.RemovedCardInstanceIds);
            deck.RemoveAll(card => card != null && removed.Contains(card.instanceId));
        }

        foreach (JToken cardToken in patch.AddedCards)
        {
            CardInstance card = STSApiClient.ConvertCard(cardToken.ToObject<STSApiCardState>());
            if (card != null)
                deck?.Add(card);
        }

        // Une carte enchantée existe déjà : on remplace la sienne plutôt que d'en
        // ajouter une seconde portant le même instanceId.
        foreach (JToken cardToken in patch.EnchantedCards)
        {
            CardInstance updated = STSApiClient.ConvertCard(cardToken.ToObject<STSApiCardState>());
            if (updated == null || deck == null)
                continue;

            int index = deck.FindIndex(card => card != null && card.instanceId == updated.instanceId);
            if (index >= 0)
                deck[index] = updated;
            else
                deck.Add(updated);
        }

        foreach (JToken relicToken in patch.AddedRelics)
        {
            Relic relic = STSApiClient.CreateRelicFromId(relicToken["relicId"]?.ToString());
            if (relic != null)
                relics.Add(relic);
        }
    }

    /// <summary>
    /// Le serveur vient de résoudre un événement : ses PV, son inventaire, ses
    /// récompenses et sa carte font foi. Rien n'est recalculé localement.
    /// </summary>
    /// <summary>
    /// Applique ce que le serveur a décidé au feu de camp : points de vie, cartes
    /// enchantées, charges restantes.
    /// </summary>
    public void ApplyRestResponse(STSApiRestActionResponse response)
    {
        if (response == null || !response.accepted)
            return;

        if (response.player != null && player != null)
        {
            player.maxHP = response.player.maxHp;
            player.currentHP = response.player.currentHp;
            ApplyServerRestCharges(response.player);
        }

        ApplyRunInventoryPatch(response.runInventoryPatch);
        restCharges = response.restCharges;
        if (response.maxRestCharges > 0)
            maxRestCharges = response.maxRestCharges;
    }

    public void ApplyEventChoiceResponse(STSApiChooseEventOptionResponse response)
    {
        if (response == null || !response.accepted)
            return;

        // Une option peut en ouvrir d'autres : l'événement continue, le nœud n'est pas
        // terminé. Déléguer à la fin de nœud effacerait activeEvent et enteredNodeId,
        // et le joueur perdrait le choix qu'on vient de lui présenter.
        if (!response.eventCompleted)
        {
            if (response.player != null && player != null)
            {
                player.maxHP = response.player.maxHp;
                player.currentHP = response.player.currentHp;
                ApplyServerRestCharges(response.player);
            }
            serverRunInventoryPatch = response.runInventoryPatch;
            serverAccountInventoryPatch = response.accountInventoryPatch;
            serverPendingRewards = response.pendingRewards ?? new List<JToken>();
            ApplyRunInventoryPatch(response.runInventoryPatch);
            activeEvent = STSApiClient.NormalizeOptionalToken(response.activeEvent);
            return;
        }

        // La réponse porte alors exactement la même forme d'état qu'une fin de nœud —
        // PV, patchs d'inventaire, récompenses, patch de carte. La déléguer évite de
        // dupliquer quarante lignes qui finiraient par diverger.
        ApplyNodeCompleteResponse(new STSApiNodeCompleteResponse
        {
            accepted = true,
            runId = runId,
            player = response.player,
            runInventoryPatch = response.runInventoryPatch,
            accountInventoryPatch = response.accountInventoryPatch,
            pendingRewards = response.pendingRewards,
            mapPatch = response.mapPatch
        });
    }

    public void ApplyNodeCompleteResponse(STSApiNodeCompleteResponse response)
    {
        if (response == null || !response.accepted)
            return;

        if (!string.IsNullOrWhiteSpace(response.runId))
        {
            runId = response.runId;
        }

        if (response.player != null && player != null)
        {
            player.maxHP = response.player.maxHp;
            player.currentHP = response.player.currentHp;
            ApplyServerRestCharges(response.player);
        }

        serverRunInventoryPatch = response.runInventoryPatch;
        serverAccountInventoryPatch = response.accountInventoryPatch;
        serverPendingRewards = response.pendingRewards ?? new List<JToken>();
        ApplyRunInventoryPatch(response.runInventoryPatch);
        serverMapPatch = response.mapPatch;
        activeEncounter = null;
        activeCombat = null;
        activeEvent = null;
        enteredNodeId = response.mapPatch != null ? response.mapPatch.enteredNodeId : null;

        if (response.mapPatch != null && map != null)
        {
            foreach (int visitedId in response.mapPatch.visitedNodeIds ?? new List<int>())
            {
                MapNode node = map.Find(n => n != null && n.id == visitedId);
                if (node != null)
                {
                    node.visited = true;
                }
            }

            foreach (int completedId in response.mapPatch.completedNodeIds ?? new List<int>())
            {
                MapNode node = map.Find(n => n != null && n.id == completedId);
                if (node != null)
                {
                    node.completed = true;
                }
            }

            int authoritativeNodeId = response.mapPatch.currentNodeId;

            if (authoritativeNodeId >= 0)
            {
                MapNode serverCurrent = map.Find(n => n != null && n.id == authoritativeNodeId);
                if (serverCurrent != null)
                {
                    bool regressing = currentNode != null && serverCurrent.floor < currentNode.floor;
                    if (!regressing)
                    {
                        currentNode = serverCurrent;
                    }
                    else
                    {
                        Debug.LogWarning($"Ignoring regressive mapPatch authoritativeNodeId={authoritativeNodeId} (floor {serverCurrent.floor}) while local node is floor {currentNode.floor}.");
                    }
                }
            }
        }
    }
    public void StartTutorialRun()
    {
        _ = StartTutorialRunAsync();
    }

    public void StartTutorialRun(int stage)
    {
        StartTutorialRun();
    }
    public void HideUI()
    {
        if (ui != null)
        {
            ui.gameObject.SetActive(false);
        }
    }

    private async Task StartTutorialRunAsync()
    {
        await StartRunAsync("", 50, new List<Relic>(), false, true, 0, "STS_Combat");
        forceTutorial = true;
        act = 0;
    }

    public void BeginPvpBattle(string battleId)
    {
        activePvpBattleId = string.IsNullOrWhiteSpace(battleId) ? null : battleId.Trim();
    }

    /// Referme la session, et rien d'autre : une run PvE mise en pause pendant le duel
    /// doit se retrouver exactement comme elle etait.
    public void EndPvpBattle()
    {
        activePvpBattleId = null;
        inCombat = false;
    }

    /// Rend, et efface, la demande de revanche. Une fois lue, elle ne doit plus valoir :
    /// sinon revenir au menu par un autre chemin relancerait une recherche non demandee.
    public bool ConsumePvpQuickMatchRequest()
    {
        bool requested = requestPvpQuickMatch;
        requestPvpQuickMatch = false;
        return requested;
    }

    public void CachePvpBattleParticipants(string battleId, List<STSApiClient.StsPvpParticipantSnapshot> participants)
    {
        pvpBattleId = string.IsNullOrWhiteSpace(battleId) ? null : battleId.Trim();
        pvpParticipants = participants != null
            ? new List<STSApiClient.StsPvpParticipantSnapshot>(participants)
            : new List<STSApiClient.StsPvpParticipantSnapshot>();
    }

    public void ClearPvpBattleParticipants()
    {
        pvpBattleId = null;
        pvpParticipants = new List<STSApiClient.StsPvpParticipantSnapshot>();
    }

    /// Le participant que ce client pilote, reconnu par l'identifiant d'utilisateur que
    /// le profil PVP a donne. A defaut, le premier de la premiere equipe — un repli qui
    /// n'est juste que pour l'hote, et qui ne sert qu'a ne pas afficher un ecran vide.
    public STSApiClient.StsPvpParticipantSnapshot LocalPvpParticipant()
    {
        if (pvpParticipants == null || pvpParticipants.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(pvpLocalUserId))
        {
            STSApiClient.StsPvpParticipantSnapshot mine = pvpParticipants.Find(p =>
                p != null
                && !string.IsNullOrWhiteSpace(p.userId)
                && string.Equals(p.userId, pvpLocalUserId, StringComparison.OrdinalIgnoreCase));
            if (mine != null)
                return mine;
        }

        return pvpParticipants.Find(p => p != null && p.teamIndex == 0 && p.slotIndex == 0)
            ?? pvpParticipants.Find(p => p != null);
    }

    /// Le premier participant d'une autre equipe que la notre. En 1v1 il n'y en a qu'un ;
    /// la formulation par equipe est ce qui la laissera vraie en 2v2.
    public STSApiClient.StsPvpParticipantSnapshot OpponentPvpParticipant()
    {
        if (pvpParticipants == null || pvpParticipants.Count == 0)
            return null;

        STSApiClient.StsPvpParticipantSnapshot local = LocalPvpParticipant();
        return pvpParticipants.Find(p =>
                   p != null && p != local
                   && (local == null || p.teamIndex != local.teamIndex))
               ?? pvpParticipants.Find(p => p != null && p != local);
    }

    public void ApplyPvpParticipantDisplayNames(List<Player> allies, List<Character> enemies)
    {
        // Sur la bataille en cours, et sur elle seule. La garde precedente lisait
        // `pvpBattleId`, qui retient la derniere bataille annoncee par le matchmaking et
        // survit a tout : la rencontre PvE jouee apres un matchmaking affichait donc le
        // pseudo de l'adversaire PvP sur son premier ennemi.
        if (string.IsNullOrWhiteSpace(activePvpBattleId) || pvpParticipants == null || pvpParticipants.Count == 0)
            return;

        STSApiClient.StsPvpParticipantSnapshot localParticipant = LocalPvpParticipant();
        STSApiClient.StsPvpParticipantSnapshot opponentParticipant = OpponentPvpParticipant();

        if (allies != null && allies.Count > 0)
        {
            Player ally = allies[0];
            if (ally != null && localParticipant != null)
            {
                if (!string.IsNullOrWhiteSpace(localParticipant.displayName))
                    ally.playerDisplayName = localParticipant.displayName;
                if (!string.IsNullOrWhiteSpace(localParticipant.userId))
                    ally.playerUserId = localParticipant.userId;
            }
        }

        if (enemies != null && enemies.Count > 0)
        {
            Character enemy = enemies[0];
            if (enemy != null && opponentParticipant != null)
            {
                if (!string.IsNullOrWhiteSpace(opponentParticipant.displayName))
                    enemy.playerDisplayName = opponentParticipant.displayName;
                if (!string.IsNullOrWhiteSpace(opponentParticipant.userId))
                    enemy.playerUserId = opponentParticipant.userId;
            }
        }
    }
}
