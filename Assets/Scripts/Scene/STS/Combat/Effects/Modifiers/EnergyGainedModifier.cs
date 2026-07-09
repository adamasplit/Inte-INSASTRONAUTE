public class EnergyGainedModifier : StatModifier
{
    public int perEnergyGained = 1;
    public EnergyGainedModifier(StatType type, int amount)
    {
        this.type = type;
        perEnergyGained = amount;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return base.AppliesTo(stat, ctx) && (ctx!=null && ctx.state!=null && ctx.state.energyGainedThisTurn > 0);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.state==null)
            return value;
        return value + ctx.state.energyGainedThisTurn * perEnergyGained;
    }
    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perEnergyGained,modifierType)} par point d'énergie gagné ce tour";
    }
}