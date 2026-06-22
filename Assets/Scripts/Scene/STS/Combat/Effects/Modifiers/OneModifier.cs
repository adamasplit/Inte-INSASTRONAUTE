using UnityEngine;
public class OneModifier : StatModifier
{
    public int overrideValue;

    public OneModifier(StatType type)
    {
        this.type = type;
        this.modifierType = ModifierType.Override;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return Mathf.Clamp(value, -1, 1);
    }

    public override string Describe()
    {
        return $"Set {StatTypeString.ToFrench(type)} to 1, keeping the sign of the original value";
    }
}