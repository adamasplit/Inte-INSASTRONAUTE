using UnityEngine;
public class MurOffensifStatus : StatusEffect
{
    public MurOffensifStatus()
    {
        Duration = -1;
        Name = "Mur offensif";
        modifierType = ModifierType.Additive;
        buff = true;
        framed = true;
        generic = true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null && ctx.source.statusEffects.Contains(this)
            && ctx.card != null && ctx.card.Type == CardType.Attaque;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return damage + Mathf.CeilToInt(ctx.source.armor * 0.2f);
    }
    public override string Desc(bool isPlayer)
    {
        return "Vos attaques infligent 20% de votre Armure en dégâts supplémentaires.";
    }
}
