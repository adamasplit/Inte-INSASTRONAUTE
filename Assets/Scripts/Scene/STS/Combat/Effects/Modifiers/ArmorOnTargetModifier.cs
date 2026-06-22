public class ArmorOnTargetModifier : StatModifier
{
    public int addedValue;

    public ArmorOnTargetModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Additive;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + addedValue * ctx.target.armor;
    }

    public override string Describe()
    {
        return $"+ {addedValue} {StatTypeString.ToFrench(type)} par Armure de la cible";
    }
}