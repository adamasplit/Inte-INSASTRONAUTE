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
        other.Value = Mathf.Min(other.Value, this.Value);
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (ctx.target == null) return false;
        return stat == StatType.Damage && ctx.target.statusEffects.Contains(this);
    }
    public override string Desc()
    {
        return $"\nVous ne pouvez pas perdre plus de {Value} PV en un seul coup.";
    }
    public override void OnDamageTaken(Character source,Character target, ref int damage)
    {
        if (damage > Value)
        {
            damage = Value;
        }
    }
}