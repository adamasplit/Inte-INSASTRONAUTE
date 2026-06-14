using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
public class RunManager : MonoBehaviour
{
    public static RunManager Instance;

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
    public async void StartRun(string character,int maxHP, List<Relic> startingRelics,bool startOnMap=true,bool forceTutorial=false,int tutorialStage=0)
    {
        // First end other executions of StartRun to prevent multiple runs from starting at the same time
        if (startingRun)
        {
            Debug.LogWarning("A run is already starting. Ignoring this StartRun call.");
            return;
        }
        startingRun = true;
        OnRunEnd();
        forceTutorial=forceTutorial;
        act=tutorialStage;
        ui.gameObject.SetActive(true);
        
        await STSCardDatabase.LoadAsync();
        gold=0;
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
            if (card.startingCount > 0 && (card.favoredCharacter == SelectableCharacter.Starting || card.favoredCharacter == selectedCharacter|| card.favoredCharacter == SelectableCharacter.Aucun))
            {
                for (int i = 0; i < card.startingCount; i++)
                {
                    Debug.Log($"Adding starting card: {card.name}");
                    deck.Add(new CardInstance(card));
                }
            }
        }
        if (startOnMap)
        {
            STSSceneLoader.Instance.LoadScene("STS_Map");
        }
        startingRun = false;
    }

    public void OnRunEnd()
    {
        gold=0;
        ui.gameObject.SetActive(false);
        player = null;
        deck.Clear();
        relics.Clear();
        pendingReward = null;
        currentNode = null;
        map = null;
    }
    public void StartTutorialRun(int stage)
    {
        StartRun("", 50, new List<Relic>(), false,false,stage);
        forceTutorial = true;
        act = stage;
        STSSceneLoader.Instance.LoadScene("STS_Combat");
    }
}