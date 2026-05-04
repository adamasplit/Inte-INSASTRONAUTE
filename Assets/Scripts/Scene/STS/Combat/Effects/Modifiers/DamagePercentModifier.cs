public class DamagePercentModifier : StatModifier
{
    public int addedPercent;

    public DamagePercentModifier(StatType type, int amount)
    {
        this.type = type;
        addedPercent = amount;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == type;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + (value * addedPercent / 100);
    }

    public override string Describe()
    {
        return $"+ {addedPercent}% {type}";
    }
}