public class PlayedModifier : StatModifier
{
    public int perCard = 1;
    public PlayedModifier(StatType type, int amount)
    {
        this.type = type;
        perCard = amount;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == type && ctx.state.cardsPlayedThisTurn > 0;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.state==null)
            return value;
        return value + ctx.state.cardsPlayedThisTurn * perCard;
    }
    public override string Describe()
    {
        return $"+{perCard} {type} par carte jouée ce tour";
    }
}