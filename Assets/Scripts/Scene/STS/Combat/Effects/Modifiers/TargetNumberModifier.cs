public class TargetNumberModifier : StatModifier
{
    public int addedValue;

    public TargetNumberModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Multiplicative;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.targets == null)
            return value;
        return value + value*addedValue*ctx.targets.Count/100;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} par cible touchée";
    }
}