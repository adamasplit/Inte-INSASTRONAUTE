public abstract class RewardItem
{
    public string serverRewardId;
    public bool claimed;

    public abstract void Claim();
}