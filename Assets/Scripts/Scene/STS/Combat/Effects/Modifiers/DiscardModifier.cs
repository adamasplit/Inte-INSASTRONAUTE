public class DiscardModifier : StatModifier
{
    public int perDiscard = 2;
    public DiscardModifier(StatType type, int amount)
    {
        this.type = type;
        perDiscard = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + ctx.state.cardsDiscardedThisCombat * perDiscard;
    }

    public override string Describe()
    {
        return $"+{perDiscard} {StatTypeString.ToFrench(type)} par carte défaussée ce combat";
    }
}