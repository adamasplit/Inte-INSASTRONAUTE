using System;
public class PlayedModifier : StatModifier
{
    public int perCard = 1;
    CardType info;
    public PlayedModifier(StatType type, int amount,string info)
    {
        this.type = type;
        perCard = amount;
        if (string.IsNullOrEmpty(info))
        {
            this.info = CardType.Rien;
        }
        else
        {
            this.info = Enum.Parse<CardType>(info);
        }
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return base.AppliesTo(stat, ctx) && (ctx!=null && ctx.state!=null && ctx.state.cardsPlayedThisTurn.Count > 0);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.state==null)
            return value;
        return value + VerifyCards(ctx.state.cardsPlayedThisTurn) * perCard;
    }
    private int VerifyCards(System.Collections.Generic.List<CardInstance> cards)
    {
        if (info == CardType.Rien)
            return cards.Count;
        int count = 0;
        foreach (CardInstance card in cards)
        {
            if (card.data.type == info)
                count++;
        }
        return count;
    }
    public override string Describe()
    {
        string str = info==CardType.Rien?"carte":info.ToString().ToLower();
        return $"{StatTypeString.ToFrench(type, perCard,modifierType)} par {str} jouée ce tour";
    }
}