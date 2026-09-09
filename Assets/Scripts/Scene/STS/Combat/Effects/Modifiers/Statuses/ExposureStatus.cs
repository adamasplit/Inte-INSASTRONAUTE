using UnityEngine;
public class ExposureStatus : StatusEffect
{
    public ExposureStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Exposition";
        debuff = true;
        modifierType = ModifierType.Multiplicative;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (stat != StatType.Damage || ctx.target == null || !ctx.target.statusEffects.Contains(this)
            || ctx.card == null)
            return false;
        return index == 0 || (index == 1 && ctx.card.Type == CardType.Attaque)
            || (index == 2 && ctx.card.Type == CardType.Compétence)
            || (index == 3 && ctx.card.Type == CardType.Pouvoir);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return AppliesTo(StatType.Damage, ctx)
            ? Mathf.CeilToInt(damage * (100 + Value) / 100f)
            : damage;
    }

    public override string Desc(bool isPlayer)
    {
        return $"La cible subit {Value}% de dégâts supplémentaires de la famille de cartes choisie pendant {Duration} tour(s).";
    }
}
