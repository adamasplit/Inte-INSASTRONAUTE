public class ActionDispelledModifier : StatModifier
{
    public int perDispel = 2;
    public ActionDispelledModifier(StatType type, int amount)
    {
        this.type = type;
        this.perDispel = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx==null||ctx.state == null|| ctx.state.effectsDispelled == 0)
            return value;
        return value + ctx.state.effectsDispelled * perDispel;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perDispel,modifierType)} par effet dissipé avec cette carte";
    }
}