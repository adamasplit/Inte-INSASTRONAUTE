using UnityEngine;
public class RelicRewardEntryView : RewardEntryView
{
    public TMPro.TextMeshProUGUI relicName;

    RelicReward reward;

    public override void Init(RewardItem rewardItem, IRewardFlowHost mgr)
    {
        base.Init(rewardItem, mgr);

        reward = rewardItem as RelicReward;

        relicName.text = "Équipement: " + reward.relic.name+$"\n<size={Mathf.RoundToInt(relicName.fontSize/1.5f)}>"+reward.relic.description+"</size>";

        UILayoutHelper.ApplyChildActualSizeAfterFrame(this, transform as UnityEngine.RectTransform, extraWidth: 20f, extraHeight: 12f);
    }

    public async void ClaimRelic()
    {
        if (manager != null && !await manager.TryClaimServerRewardAsync(reward))
        {
            return;
        }

        reward.Claim();

        StartCoroutine(Collapse());
    }
}