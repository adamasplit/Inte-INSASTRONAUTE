public class RelicUpgradeRewardEntryView : RewardEntryView
{
    public TMPro.TextMeshProUGUI relicName;
    public TMPro.TextMeshProUGUI relicDescription;
    BaseRelicUpgradeReward reward;
    public override void Init(RewardItem item, IRewardFlowHost mgr)
    {
        base.Init(item,mgr);
        BaseRelicUpgradeReward relicUpgradeReward = item as BaseRelicUpgradeReward;
        reward = relicUpgradeReward;
        if (relicUpgradeReward != null)
        {
            relicName.text = relicUpgradeReward.relic.namesByStage[relicUpgradeReward.stage];
            relicDescription.text = relicUpgradeReward.relic.descriptionsByStage[relicUpgradeReward.stage];
        }

        UILayoutHelper.ApplyChildActualSizeAfterFrame(this, transform as UnityEngine.RectTransform, extraWidth: 20f, extraHeight: 12f);
    }
    public async void ClaimRelicUpgrade()
    {
        if (manager != null && !await manager.TryClaimServerRewardAsync(reward))
        {
            return;
        }

        reward.Claim();
        StartCoroutine(Collapse());
        foreach (var entry in FindObjectsOfType<RelicUpgradeRewardEntryView>())
        {
            if (entry != this)
                entry.StartCoroutine(entry.Collapse());
        }
    }
}