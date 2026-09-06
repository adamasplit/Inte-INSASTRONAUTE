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
        bool serverBacked = !string.IsNullOrWhiteSpace(reward.serverRewardId);
        if (manager != null && !(await manager.TryClaimServerRewardAsync(reward)).Accepted)
        {
            return;
        }

        if (serverBacked)
        {
            // The server has already recorded the relic and its OnAcquire effect.
            // Keep the client inventory in sync without applying that effect twice.
            RunManager.Instance.relics.Add(reward.relic);
            reward.claimed = true;
        }
        else
        {
            reward.Claim();
        }

        StartCoroutine(Collapse());
    }
}