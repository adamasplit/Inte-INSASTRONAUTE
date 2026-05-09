using UnityEngine;
public class StrengthStatus : StatusEffect
{
    public StrengthStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Force";
        modifierType = ModifierType.Additive;
        buff=true;
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