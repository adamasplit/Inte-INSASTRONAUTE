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
    private bool isApplied = false;
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
            isApplied = true;
        }
        return res;
    }
    public override void OnCardPlayed(Character source, Character target, CardInstance card)
    {
        if (isApplied)
        {
            mustExpire = true;
        }
    }
    public override string Desc()
    {
        return $"Le prochain effet de dégâts que vous appliquez est augmenté de {Value}.";
    }
}