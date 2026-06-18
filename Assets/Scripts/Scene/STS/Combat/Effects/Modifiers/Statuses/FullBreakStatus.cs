using UnityEngine;
public class FullBreakStatus : StatusEffect
{
    float power = 0.2f;
    public FullBreakStatus(int duration)
    {
        this.Name="Déchéance";
        this.Duration = duration;
        this.modifierType = ModifierType.Multiplicative;
        debuff=true;
        framed=true;
        generic=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (ctx.target == null) return false;
        return true;
    }

    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target.statusEffects.Contains(this))
        {
            return Mathf.CeilToInt(value * (1+power));
        }
        if (ctx.source.statusEffects.Contains(this))
        {
            return Mathf.CeilToInt(value * (1-power));
        }
        return value;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Augmente les dégâts reçus de 20% et réduit tous les effets appliqués de 20%";
    }
}