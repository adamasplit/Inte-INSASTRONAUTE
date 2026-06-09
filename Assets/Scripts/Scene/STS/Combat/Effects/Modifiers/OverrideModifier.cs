using UnityEngine;
public class OverrideModifier : StatModifier
{
    public int overrideValue;

    public OverrideModifier(StatType type, int value)
    {
        this.type = type;
        overrideValue = value;
        this.modifierType = ModifierType.Override;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return overrideValue;
    }

    public override string Describe()
    {
        return $"Set {StatTypeString.ToFrench(type)} to {overrideValue}";
    }
}