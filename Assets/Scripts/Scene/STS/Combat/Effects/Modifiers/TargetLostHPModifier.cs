public class TargetLostHPModifier : StatModifier
{
    public int addedValue;

    public TargetLostHPModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Multiplicative;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value * (1 + addedValue * (ctx.target.maxHP - ctx.target.currentHP) / 100);
    }

    public override string Describe()
    {
        return $"+ {addedValue}% {StatTypeString.ToFrench(type)} par % de PV perdus de la cible";
    }
}