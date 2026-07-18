using UnityEngine;
public class RegulatorStatus : StatusEffect
{
    private int damageThisTurn = 0;
    
    public RegulatorStatus(int value)    
    {
        Name="Régulateur";
        Value = 0;
        maxValue=maxDamage(value);
        Duration = -1;
        buff=true;
        framed=true;
        modifierType = ModifierType.Override;
    }
    public override void Merge(StatusEffect other)
    {
        other.maxValue = Mathf.Min(other.maxValue, this.Value);
    }
    public override void OnTurnStart(Character target)
    {
        damageThisTurn = 0;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (ctx.target == null) return false;
        return stat == StatType.Damage && ctx.target.statusEffects.Contains(this);
    }
    public override string Desc(bool isPlayer)
    {
        return (isPlayer ? "Vous ne pouvez pas subir plus de " : "L'ennemi ne peut pas subir plus de ") + $"{maxValue} dégâts en un seul tour.";
    }
    private int maxDamage(int value)
    {
        return Mathf.Max(5,30-value);
    }
    public override void OnHPLoss(Character target,int damage)
    {
        damageThisTurn += damage;
    }
    public override int ValidateHPLoss(int damage, Character target)
    {
        if (damageThisTurn + damage > maxValue)
        {
            return Mathf.Max(0, maxValue - damageThisTurn);
        }
        return damage;
    }
}