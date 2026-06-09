public class RelicReward : RewardItem
{
    public Relic relic;

    public override void Claim()
    {
        RunManager.Instance.AddRelic(relic);
        claimed = true;
    }
}