using UnityEngine;
public abstract class StatModifier
{
    public StatType type;
    public ModifierType modifierType;
    public virtual bool AppliesTo(StatType stat,EffectContext ctx)
    {
        return type == stat || (type == StatType.Any&&(stat!=StatType.Cost&&stat!=StatType.ReplayCount));
    }
    public abstract int Modify(int value, EffectContext ctx);
    public abstract string Describe();
    public StatModifier Clone()
    {
        return (StatModifier)this.MemberwiseClone();
    }
}