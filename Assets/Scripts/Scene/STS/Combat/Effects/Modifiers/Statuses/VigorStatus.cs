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
        generic=true;
    }
    private bool isApplied = false;
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this) && !ctx.card.HasTag(CardTag.FollowUp);
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
    public override string Desc(bool isPlayer)
    {
        return $"Le prochain effet de dégâts appliqué est augmenté de {Value}.";
    }
}