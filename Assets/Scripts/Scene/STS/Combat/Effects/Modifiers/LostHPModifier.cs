public class LostHPModifier : StatModifier
{
    public int addedValue;

    public LostHPModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Multiplicative;
    }

    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.source == null)
            return value;

        return value * (1 + addedValue * (ctx.source.maxHP - ctx.source.currentHP) / 100);
    }

    public override string Describe()
    {
        return $"+ {addedValue}% {StatTypeString.ToFrench(type)} par % de PV perdus du lanceur";
    }
}