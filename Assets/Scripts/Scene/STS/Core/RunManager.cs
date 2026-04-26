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
    public void StartRun(string character,int maxHP, List<Relic> startingRelics)
    {
        STSCardDatabase.Load();
        var run = RunManager.Instance;

        run.player = new Player(character, maxHP);
        run.relics = startingRelics;
        run.currentFloor = 1;

        run.deck.Clear();
        STSCardData attackCard = STSCardDatabase.Get("Attaque");
        STSCardData blockCard = STSCardDatabase.Get("Défense");
        for(int i = 0; i < 5; i++)
        {
            run.deck.Add(attackCard);
            run.deck.Add(blockCard);
        }
        SceneManager.LoadScene("STS_Map");
    }
}