using UnityEngine;
public class WeaknessStatus : StatusEffect
{
    public WeaknessStatus(int duration)
    {
        Duration = duration;
        Name = "Affaibli";
        modifierType = ModifierType.Multiplicative;
        debuff=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return Mathf.FloorToInt(damage * 0.75f);
    }
    public override string Desc()
    {
        return "\nInflige 25% de dégâts en moins";
    }
}