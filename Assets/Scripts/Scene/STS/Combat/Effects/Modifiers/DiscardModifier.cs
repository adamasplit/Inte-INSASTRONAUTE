public class DiscardModifier : StatModifier
{
    public int perDiscard = 2;
    public DiscardModifier(StatType type, int amount)
    {
        this.type = type;
        perDiscard = amount;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == type;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + ctx.state.cardsDiscardedThisCombat * perDiscard;
    }

    public override string Describe()
    {
        return $"+{perDiscard} {type} par carte défaussée ce combat";
    }
}