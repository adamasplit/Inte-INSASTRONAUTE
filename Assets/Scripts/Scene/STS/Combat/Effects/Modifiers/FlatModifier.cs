public class FlatModifier : StatModifier
{
    public int flatAmount;

    public FlatModifier(StatType type, int amount)
    {
        this.type = type;
        flatAmount = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + flatAmount;
    }

    public override string Describe()
    {
        return $"+ {flatAmount} {StatTypeString.ToFrench(type)}";
    }
}