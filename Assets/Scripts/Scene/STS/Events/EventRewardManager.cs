using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EventRewardManager : MonoBehaviour, IRewardFlowHost
{
    public Transform rewardList;
    public GameObject cardRewardPrefab;
    public GameObject relicRewardPrefab;
    public GameObject goldRewardPrefab;
    public GameObject baseRelicUpgradeRewardPrefab;
    public GameObject continueButton;
    [SerializeField] float continueButtonFailSafeDelay = 2f;

    private readonly List<RewardEntryView> activeEntries = new();
    private Action onComplete;
    private Coroutine continueButtonFailSafeRoutine;

    public void ShowReward(Reward reward, Action completion)
    {
        gameObject.SetActive(true);
        rewardList.gameObject.SetActive(true);

        onComplete = completion;

        StopContinueButtonFailSafe();

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
        gameObject.SetActive(true);
        onComplete = completion;

        rewardList.gameObject.SetActive(false);

        StopContinueButtonFailSafe();

        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }

        ArmContinueButtonFailSafe();
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
        UILayoutHelper.ApplyPreferredSizeAfterFrame(this, obj.transform as RectTransform, fitWidth: true, fitHeight: true, extraWidth: 20f, extraHeight: 12f);
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
        StopContinueButtonFailSafe();
        gameObject.SetActive(false);

        onComplete?.Invoke();
        onComplete = null;
    }

    public Task<bool> TryClaimServerRewardAsync(RewardItem rewardItem, string selectedCardId = null)
    {
        return Task.FromResult(true);
    }

    private void ArmContinueButtonFailSafe()
    {
        StopContinueButtonFailSafe();
        continueButtonFailSafeRoutine = StartCoroutine(ContinueButtonFailSafe());
    }

    private void StopContinueButtonFailSafe()
    {
        if (continueButtonFailSafeRoutine != null)
        {
            StopCoroutine(continueButtonFailSafeRoutine);
            continueButtonFailSafeRoutine = null;
        }
    }

    private IEnumerator ContinueButtonFailSafe()
    {
        yield return new WaitForSeconds(continueButtonFailSafeDelay);

        if (continueButton == null || !continueButton.activeInHierarchy)
        {
            Debug.LogWarning("Event continue button did not appear. Triggering fail-safe completion.");
            Continue();
        }

        continueButtonFailSafeRoutine = null;
    }
}