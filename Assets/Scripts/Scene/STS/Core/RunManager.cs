using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
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
    public void StartRun(string character,int maxHP, List<Relic> startingRelics,bool startOnMap=true)
    {
        forceTutorial=false;
        gold=0;
        ui.gameObject.SetActive(true);
        STSCardDatabase.Load();
        var run = RunManager.Instance;
        run.act=0;
        if (Enum.TryParse(character, out SelectableCharacter parsedCharacter))
        {
            run.selectedCharacter = parsedCharacter;
        }
        else
        {
            Debug.LogError($"Invalid character: {character}. No character selected.");
            run.selectedCharacter = SelectableCharacter.Aucun;
        }
        run.player = new Player(character, maxHP);
        run.relics = startingRelics;
        run.currentFloor = 1;
        run.RegenerateMap = true;

        run.deck.Clear();
        foreach (STSCardData card in STSCardDatabase.allCards)
        {
            if (card.startingCount > 0 && (card.favoredCharacter == SelectableCharacter.Starting || card.favoredCharacter == run.selectedCharacter))
            {
                for (int i = 0; i < card.startingCount; i++)
                {
                    run.deck.Add(new CardInstance(card));
                }
            }
        }
        if (startOnMap)
        {
            SceneManager.LoadScene("STS_Map");
        }
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
        StartRun("", 50, new List<Relic>(), false);
        forceTutorial = true;
        act = stage;
        SceneManager.LoadScene("STS_Combat");
    }
}