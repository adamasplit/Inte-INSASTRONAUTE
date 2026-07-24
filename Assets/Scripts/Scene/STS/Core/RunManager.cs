using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
public class RunManager : MonoBehaviour
{
    public static RunManager Instance;

    public string runId;
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
    public JToken serverRunInventoryPatch;
    public JToken serverAccountInventoryPatch;
    public List<JToken> serverPendingRewards = new();
    public STSApiMapPatchState serverMapPatch;
    public bool backendRewardClaimUnavailable;
    public bool completedFinalAct;
    public bool unrestrictedMode;
    public string unrestrictedModeReason;
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
                    STSSceneLoader.Instance?.LoadScene("STS_Map");
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

    public void OnRunEnd(bool clearSave, bool resetRemoteRun)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[STS-RUN] OnRunEnd(clearSave={clearSave}, resetRemoteRun={resetRemoteRun}, runId={runId}, scene={currentScene}, completedFinalAct={completedFinalAct})");
        STSRunAuditSystem.RecordRunEnded(this, clearSave ? "clear_save" : "preserve_save");

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
        completedFinalAct = false;
        backendRewardClaimUnavailable = false;
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
        currentNode = map != null ? map.Find(n => n != null && n.id == remoteState.currentNodeId) : null;
        if (currentNode == null && map != null && map.Count > 0)
        {
            currentNode = map[0];
        }

        RegenerateMap = false;
        activeEncounter = remoteState.activeEncounter;
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
        currentNode = map != null ? map.Find(n => n != null && n.id == remoteState.currentNodeId) : null;
        if (currentNode == null && map != null && map.Count > 0)
        {
            currentNode = map[0];
        }

        RegenerateMap = false;
        activeEncounter = remoteState.activeEncounter;
        pendingReward = null;
        serverPendingRewards = pendingRewards != null
            ? new List<JToken>(pendingRewards)
            : new List<JToken>();
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

        if (map != null)
        {
            MapNode entered = map.Find(n => n != null && n.id == response.nodeId);
            if (entered != null)
            {
                entered.visited = true;
            }
        }
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
        }

        serverRunInventoryPatch = response.runInventoryPatch;
        serverAccountInventoryPatch = response.accountInventoryPatch;
        serverPendingRewards = response.pendingRewards ?? new List<JToken>();
        serverMapPatch = response.mapPatch;
        activeEncounter = null;

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
}