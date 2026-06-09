public class GoldReward : RewardItem
{
    public int amount;

    public override void Claim()
    {
        RunManager.Instance.gold += amount;
        claimed = true;
    }
}