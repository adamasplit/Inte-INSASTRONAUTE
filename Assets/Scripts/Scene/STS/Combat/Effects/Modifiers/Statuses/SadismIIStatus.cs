using UnityEngine;
public class SadismIIStatus:StatusEffect
{
    public SadismIIStatus()
    {
        Value = 30;
        Duration = -1;
        Name = "Préparation...";
        debuff=false;
        buff=true;
        framed=true;
        modifierType = ModifierType.Multiplicative;
    }
    public override void Update(Character target)
    {
        foreach (Character character in target.GetCombatManager().enemies)
        if (character.currentHP <= character.maxHP *0.5f)
        {
            target.AddStatus(new SadismIIIStatus());
            this.mustExpire=true;
        }
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return ((stat == StatType.Damage||stat==StatType.Armor) && ctx.source.statusEffects.Contains(this));
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return Mathf.FloorToInt(damage + (damage * Value) / 100);
    }
    public override string Desc()
    {
        return $"{Value}% dégâts supplémentaires subis et +{Value} d'Armure gagnée. Cet effet évolue lorsqu'un ennemi passe en-dessous de 50% de sa vie.";
    }
}