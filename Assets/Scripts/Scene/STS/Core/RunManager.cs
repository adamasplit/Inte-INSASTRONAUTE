using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RunManager : MonoBehaviour
{
    public static RunManager Instance;

    public Player player;
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
        run.player = new Player(character, maxHP);
        run.relics = startingRelics;
        run.currentFloor = 1;

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
        if (startOnMap)
        {
            SceneManager.LoadScene("STS_Map");
        }
    }
}