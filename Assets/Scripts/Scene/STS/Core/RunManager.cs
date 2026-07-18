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
    public async Task StartRunAsync(string character, int maxHP, List<Relic> startingRelics, bool startOnMap = true, bool forceTutorial = false, int tutorialStage = 0, string nextSceneName = null)
    {
        // First end other executions of StartRun to prevent multiple runs from starting at the same time
        if (startingRun)
        {
            Debug.LogWarning("A run is already starting. Ignoring this StartRun call.");
            return;
        }
        startingRun = true;
        STSSceneLoader.Instance?.BeginLoading();

        bool loadedScene = false;

        try
        {
            OnRunEnd(true, false);
            this.forceTutorial = forceTutorial;
            act = tutorialStage;
            if (ui != null)
            {
                ui.gameObject.SetActive(true);
            }

            await STSCardDatabase.LoadAsync();
            await EnemyDataDatabase.LoadAsync();
            await EnemyPoolDatabase.LoadAsync();

            STSApiRunCreateResponse remoteRun = null;
            if (!forceTutorial)
            {
                try
                {
                    remoteRun = await STSApiClient.CreateRunAsync(character, Application.version);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Remote STS run creation failed, falling back to local run setup: {ex.Message}");
                }
            }

            if (!forceTutorial && ApplyRemoteRunIfAvailable(remoteRun))
            {
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

    public async void StartRun(string character, int maxHP, List<Relic> startingRelics, bool startOnMap = true, bool forceTutorial = false, int tutorialStage = 0)
    {
        await StartRunAsync(character, maxHP, startingRelics, startOnMap, forceTutorial, tutorialStage);
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
        STSRunAuditSystem.RecordRunEnded(this, clearSave ? "clear_save" : "preserve_save");

        if (resetRemoteRun && clearSave && !string.IsNullOrWhiteSpace(runId))
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
        return true;
    }

    public void ApplyNodeEnterResponse(STSApiNodeEnterResponse response)
    {
        if (response == null)
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
        if (response == null)
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

            if (response.mapPatch.currentNodeId >= 0)
            {
                currentNode = map.Find(n => n != null && n.id == response.mapPatch.currentNodeId) ?? currentNode;
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

    private async Task StartTutorialRunAsync()
    {
        await StartRunAsync("", 50, new List<Relic>(), false, true, 0, "STS_Combat");
        forceTutorial = true;
        act = 0;
    }
}