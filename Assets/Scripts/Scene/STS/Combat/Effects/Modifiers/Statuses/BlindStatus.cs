using UnityEngine;
public class BlindStatus : StatusEffect
{
    public BlindStatus(int duration)
    {
        Duration = duration;
        Name = "Aveuglé";
        modifierType = ModifierType.Override;
        debuff=true;
        generic=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return 0;
    }
    public override string Desc()
    {
        return "Les attaques infligent 0 dégâts";
    }
}