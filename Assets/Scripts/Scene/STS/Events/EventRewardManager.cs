using System;
using System.Collections.Generic;
using UnityEngine;

public class EventRewardManager : MonoBehaviour, IRewardFlowHost
{
    public Transform rewardList;
    public GameObject cardRewardPrefab;
    public GameObject relicRewardPrefab;
    public GameObject goldRewardPrefab;
    public GameObject baseRelicUpgradeRewardPrefab;
    public GameObject continueButton;

    private readonly List<RewardEntryView> activeEntries = new();
    private Action onComplete;

    public void ShowReward(Reward reward, Action completion)
    {
        rewardList.gameObject.SetActive(true);

        onComplete = completion;

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        foreach (Transform child in rewardList)
        {
            Destroy(child.gameObject);
        }

        activeEntries.Clear();

        foreach (var item in reward.items)
        {
            SpawnReward(item);
        }

        rewardList.gameObject.SetActive(activeEntries.Count > 0);

        if (activeEntries.Count == 0 && continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    public void ShowContinue(Action completion)
    {
        onComplete = completion;

        rewardList.gameObject.SetActive(false);

        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    private void SpawnReward(RewardItem item)
    {
        GameObject prefab = null;

        if (item is CardReward)
        {
            prefab = cardRewardPrefab;
        }
        else if (item is RelicReward)
        {
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
        {
            return;
        }

        GameObject obj = Instantiate(prefab, rewardList);
        RewardEntryView view = obj.GetComponent<RewardEntryView>();
        view.Init(item, this);
        activeEntries.Add(view);
    }

    public void NotifyClaimed(RewardEntryView entry)
    {
        activeEntries.Remove(entry);

        if (activeEntries.Count == 0 && continueButton != null)
        {
            rewardList.gameObject.SetActive(false);
            continueButton.SetActive(true);
        }
    }

    public void Continue()
    {
        gameObject.SetActive(false);

        onComplete?.Invoke();
        onComplete = null;
    }
}