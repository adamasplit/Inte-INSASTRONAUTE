using UnityEngine;
public class VulnStatus : StatusEffect
{
    public VulnStatus(int duration)
    {
        this.Name="Vulnérable";
        this.Duration = duration;
        this.modifierType = ModifierType.Multiplicative;
        debuff=true;
        generic=true;
    }
    public override void OnTurnEnd(Character target)
    {
        // Implementation for when the turn ends
    }
    public override void OnTurnStart(Character target)
    {
        Tick(target);
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (ctx.target == null) return false;
        return stat == StatType.Damage && ctx.target.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return Mathf.CeilToInt(damage * 1.5f);
    }
    public override string Desc(bool isPlayer)
    {
        return $"Reçoit 50% de dégâts en plus";
    }
}