public class ArmorModifier : StatModifier
{
    public int addedValue;

    public ArmorModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Multiplicative;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + value*addedValue*ctx.target.armor/100;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} par Armure de la cible";
    }
}