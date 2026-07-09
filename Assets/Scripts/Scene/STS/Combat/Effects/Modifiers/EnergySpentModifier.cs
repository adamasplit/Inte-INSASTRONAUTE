public class EnergySpentModifier : StatModifier
{
    public int perEnergySpent = 1;
    public EnergySpentModifier(StatType type, int amount)
    {
        this.type = type;
        perEnergySpent = amount;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return base.AppliesTo(stat, ctx) && (ctx!=null && ctx.state!=null && ctx.state.energySpentThisTurn > 0);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.state==null)
            return value;
        return value + ctx.state.energySpentThisTurn * perEnergySpent;
    }
    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perEnergySpent,modifierType)} par point d'énergie dépensé ce tour";
    }
}