using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RunManager : MonoBehaviour
{
    public static RunManager Instance;

    public Player player;
    public int currentFloor;
    public List<STSCardData> deck = new();
    public List<Relic> relics = new();
    public Reward pendingReward;
    public bool eliteEncounter;
    public bool bossEncounter;
    public List<MapNode> map=null;
    public MapNode currentNode;
    public bool RegenerateMap = false;
    public int lastActEndFloor = 0;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void AddRelic(Relic relic)
    {
        relics.Add(relic);
        relic.OnAcquire(player);
    }
    public void StartRun(string character,int maxHP, List<Relic> startingRelics,bool startOnMap=true)
    {
        STSCardDatabase.Load();
        var run = RunManager.Instance;

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
            run.deck.Add(attackCard);
            run.deck.Add(blockCard);
        }
        if (startOnMap)
        {
            SceneManager.LoadScene("STS_Map");
        }
    }
}