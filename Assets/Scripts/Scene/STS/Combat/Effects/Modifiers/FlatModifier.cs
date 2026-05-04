public class FlatModifier : StatModifier
{
    public int flatAmount;

    public FlatModifier(StatType type, int amount)
    {
        this.type = type;
        flatAmount = amount;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == type;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + flatAmount;
    }

    public override string Describe()
    {
        return $"+ {flatAmount} {type}";
    }
}