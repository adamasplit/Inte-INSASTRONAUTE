using UnityEngine;
public class VigorStatus : StatusEffect
{
    public VigorStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Vigueur";
        modifierType = ModifierType.Additive;
        buff=true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        if (!AppliesTo(StatType.Damage, ctx))
            return damage;
        int res = damage + Value;
        if (!ctx.isPreview)
        {
            mustExpire = true;
        }
        return res;
    }
    public override string Desc()
    {
        return $"\nLe prochain effet de dégâts que vous appliquez est augmenté de {Value}.";
    }
}