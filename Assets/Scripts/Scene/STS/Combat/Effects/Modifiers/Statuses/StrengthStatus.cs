using UnityEngine;
public class StrengthStatus : StatusEffect
{
    public StrengthStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Force";
        modifierType = ModifierType.Additive;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return damage + Value;
    }
    public override string Describe()
    {
        return $"{Value} dégâts supplémentaires";
    }
}