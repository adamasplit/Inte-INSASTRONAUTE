using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
public class RunManager : MonoBehaviour
{
    public static RunManager Instance;

    public string runId;
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
            OnRunEnd();
            this.forceTutorial = forceTutorial;
            act = tutorialStage;
            ui.gameObject.SetActive(true);

            await STSCardDatabase.LoadAsync();
            await EnemyDataDatabase.LoadAsync();
            await EnemyPoolDatabase.LoadAsync();
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
        OnRunEnd(true);
    }

    public void OnRunEnd(bool clearSave)
    {
        STSRunAuditSystem.RecordRunEnded(this, clearSave ? "clear_save" : "preserve_save");

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