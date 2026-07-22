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

    public void ClaimRelic()
    {
        reward.Claim();

        StartCoroutine(Collapse());
    }
}