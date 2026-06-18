public class RelicRewardEntryView : RewardEntryView
{
    public TMPro.TextMeshProUGUI relicName;

    RelicReward reward;

    public override void Init(RewardItem rewardItem, IRewardFlowHost mgr)
    {
        base.Init(rewardItem, mgr);

        reward = rewardItem as RelicReward;

        relicName.text = "Équipement: " + reward.relic.name+"\n<size=20>"+reward.relic.description+"</size>";
    }

    public void ClaimRelic()
    {
        reward.Claim();

        StartCoroutine(Collapse());
    }
}