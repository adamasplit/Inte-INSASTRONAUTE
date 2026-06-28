using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class RewardManager : MonoBehaviour, IRewardFlowHost
{
    public Transform rewardList;

    public GameObject cardRewardPrefab;
    public GameObject relicRewardPrefab;
    public GameObject goldRewardPrefab;
    public GameObject baseRelicUpgradeRewardPrefab;

    public GameObject continueButton;

    List<RewardEntryView> activeEntries = new();
    bool goingToMap = true;
    void Start()
    {
        Reward reward;
        if (RunManager.Instance!=null &&RunManager.Instance.pendingReward != null)
        {
            reward = RunManager.Instance.pendingReward;
            goingToMap = !RunManager.Instance.bossEncounter;
        }
        else
        {
            
            CombatResult result = new CombatResult
            {
                floor = 1,
                elite = true,
                boss = true
            };
            reward = RewardGenerator.GenerateReward(result);
        }
        
        foreach (var item in reward.items)
        {
            SpawnReward(item);
        }

        STSSceneLoader.Instance?.SceneReady();
    }

    void SpawnReward(RewardItem item)
    {
        GameObject prefab = null;

        if (item is CardReward)
        {
            prefab = cardRewardPrefab;
        }
        else if (item is RelicReward)
        {   
            Debug.Log("Spawning relic reward: " + ((RelicReward)item).relic.name);
            prefab = relicRewardPrefab;
        }
        else if (item is GoldReward)
        {
            prefab = goldRewardPrefab;
        }
        else if (item is BaseRelicUpgradeReward)
        {
            prefab = baseRelicUpgradeRewardPrefab;
        }
        if (prefab == null)
            return;

        var obj = Instantiate(prefab, rewardList);

        var view = obj.GetComponent<RewardEntryView>();
        view.Init(item, this);

        UILayoutHelper.ApplyPreferredSizeAfterFrame(this, obj.transform as RectTransform, fitWidth: true, fitHeight: true, extraWidth: 20f, extraHeight: 12f);

        activeEntries.Add(view);
    }

    public void NotifyClaimed(RewardEntryView entry)
    {
        activeEntries.Remove(entry);

        if (activeEntries.Count == 0)
        {
            continueButton.SetActive(true);
        }
    }

    public void Continue()
    {
        RunManager.Instance.pendingReward = null;
        if (goingToMap)
        {
            STSSceneLoader.Instance.LoadScene("STS_Map");
        }
        else
        {
            STSSceneLoader.Instance.LoadScene("STS_Retreat");
        }
    }
}