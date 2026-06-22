using UnityEngine;
public abstract class StatModifier
{
    public StatType type;
    public ModifierType modifierType;
    public string description;
    public bool temporary=false;
    public virtual bool AppliesTo(StatType stat,EffectContext ctx)
    {
        return type == stat || (type == StatType.Any&&(StatTypeChecker.IsValid(stat)));
    }
    public abstract int Modify(int value, EffectContext ctx);
    public abstract string Describe();
    public StatModifier Clone()
    {
        return (StatModifier)this.MemberwiseClone();
    }
}