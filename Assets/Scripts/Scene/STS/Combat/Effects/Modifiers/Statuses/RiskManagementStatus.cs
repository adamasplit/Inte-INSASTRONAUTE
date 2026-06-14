using UnityEngine;
public class RiskManagementStatus : StatusEffect
{
    public RiskManagementStatus(int value)    
    {
        Name="Gestion du risque";
        Value = value;
        Duration = -1;
        buff=true;
        framed=true;
        modifierType = ModifierType.Override;
    }
    public override void Merge(StatusEffect other)
    {
        other.Value = Mathf.Max(other.Value, this.Value);
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (ctx.target == null) return false;
        return stat == StatType.Damage && ctx.target.statusEffects.Contains(this);
    }
    public override string Desc()
    {
        return $"Vous ne pouvez pas perdre plus de {maxDamage()} PV en un seul coup.";
    }
    private int maxDamage()
    {
        return Mathf.Max(5,30-Value);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        if (damage>maxDamage())
        {
            damage=maxDamage();
        }
        return damage;
    }
}