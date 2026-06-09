using System.Collections.Generic;
public class CardReward : RewardItem
{
    public List<CardInstance> choices;

    public override void Claim()
    {
        claimed = true;
    }
}