public class PercentModifier : StatModifier
{
    public int addedPercent;

    public PercentModifier(StatType type, int amount)
    {
        this.type = type;
        addedPercent = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + (value * addedPercent / 100);
    }

    public override string Describe()
    {
        return $"+ {addedPercent}% {StatTypeString.ToFrench(type)}";
    }
}