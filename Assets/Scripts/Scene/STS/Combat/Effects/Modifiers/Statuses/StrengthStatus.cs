using UnityEngine;
public class StrengthStatus : StatusEffect
{
    public StrengthStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Force";
        modifierType = ModifierType.Additive;
        Update(null);
        inextendable=true;
    }
    public override void Update(Character target)
    {
        if (Value < 0)
        {
            buff = false;
            debuff=true;
        }
        else if (Value > 0)
        {
            buff = true;
            debuff=false;
        }
        else
        {
            mustExpire = true;
        }
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return damage + Value;
    }
    public override string Desc(bool isPlayer)
    {
        if (Value > 0)
        {
            return $"Augmente les dégâts de {Value}.";
        }
        else if (Value < 0)
        {
            return $"Réduit les dégâts de {-Value}.";
        }
        else
        {
            return $"Aucun effet.";
        }
    }
}