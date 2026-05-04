public abstract class StatModifier
{
    public StatType type;
    public ModifierType modifierType;
    public abstract bool AppliesTo(StatType stat,EffectContext ctx);
    public abstract int Modify(int value, EffectContext ctx);
    public abstract string Describe();
    public StatModifier Clone()
    {
        return (StatModifier)this.MemberwiseClone();
    }
}