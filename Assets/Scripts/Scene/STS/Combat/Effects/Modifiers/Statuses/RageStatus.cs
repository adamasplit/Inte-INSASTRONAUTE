using UnityEngine;

/// <summary>
/// Rage : le porteur inflige <c>Value</c> dégâts de plus à ce qui n'a pas d'Armure.
/// </summary>
public class RageStatus : StatusEffect
{
    public RageStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Rage";
        modifierType = ModifierType.Additive;
        buff = true;
        framed = true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage
            && ctx.source != null
            && ctx.source.statusEffects.Contains(this)
            && ctx.target != null
            && ctx.target.armor <= 0;
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        if (!AppliesTo(StatType.Damage, ctx))
            return damage;
        return damage + Value;
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vous infligez {Value} dégâts supplémentaires aux ennemis sans Armure.";
        }
        return $"Ce personnage inflige {Value} dégâts supplémentaires aux ennemis sans Armure.";
    }
}
