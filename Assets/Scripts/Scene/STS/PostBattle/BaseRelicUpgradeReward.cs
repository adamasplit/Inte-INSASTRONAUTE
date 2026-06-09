public class BaseRelicUpgradeReward : RewardItem
{
    public BaseRelic relic;
    public int stage;
    public override void Claim()
    {
        relic.Upgrade(stage);
    }
}