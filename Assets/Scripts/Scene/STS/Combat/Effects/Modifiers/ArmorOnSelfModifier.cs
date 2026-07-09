public class ArmorOnSelfModifier : StatModifier
{
    public int addedValue;

    public ArmorOnSelfModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Additive;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.source == null)
            return value;
        return value + addedValue * ctx.source.armor;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} par Armure sur vous";
    }
}