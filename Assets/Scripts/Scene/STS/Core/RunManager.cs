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
        ui.gameObject.SetActive(true);
        STSCardDatabase.Load();
        var run = RunManager.Instance;
        run.act=0;
        run.selectedCharacter = Enum.Parse<SelectableCharacter>(character);
        run.player = new Player(character, maxHP);
        run.relics = startingRelics;
        run.currentFloor = 1;
        run.RegenerateMap = true;

        run.deck.Clear();
        STSCardData attackCard = STSCardDatabase.Get("Katana");
        STSCardData blockCard = STSCardDatabase.Get("Révision Model Text");
        if (attackCard == null || blockCard == null)
        {
            Debug.LogError("Cards not found in database!");
            return;
        }
        for(int i = 0; i < 5; i++)
        {
            run.deck.Add(new CardInstance(attackCard));
            run.deck.Add(new CardInstance(blockCard));
        }
        List<STSCardData> characterCards = STSCardDatabase.CardForCollectionCard(character);
        foreach (var card in characterCards)
        {
            run.deck.Add(new CardInstance(card));
        }
        if (startOnMap)
        {
            SceneManager.LoadScene("STS_Map");
        }
    }

    public void OnRunEnd()
    {
        ui.gameObject.SetActive(false);
        player = null;
        deck.Clear();
        relics.Clear();
        pendingReward = null;
        currentNode = null;
        map = null;
    }
}