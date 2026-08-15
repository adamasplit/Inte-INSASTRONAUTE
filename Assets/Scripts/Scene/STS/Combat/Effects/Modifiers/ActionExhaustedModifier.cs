public class ActionExhaustedModifier : StatModifier
{
    public int perExhaust = 2;
    public ActionExhaustedModifier(StatType type, int amount)
    {
        this.type = type;
        this.perExhaust = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx==null||ctx.state == null|| ctx.state.cardsExhausted == 0)
            return value;
        return value + ctx.state.cardsExhausted * perExhaust;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perExhaust,modifierType)} par carte épuisée avec cette carte";
    }
}