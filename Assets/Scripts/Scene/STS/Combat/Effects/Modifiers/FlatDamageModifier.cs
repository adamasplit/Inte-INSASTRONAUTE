public class FlatDamageModifier : StatModifier
{
    public int flatAmount;

    public FlatDamageModifier(Character owner, int amount)
    {
        Owner = owner;
        type = StatType.Damage;
        flatAmount = amount;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source == Owner;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + flatAmount;
    }

    public override string Describe()
    {
        return $"Flat Damage: {flatAmount}";
    }
}