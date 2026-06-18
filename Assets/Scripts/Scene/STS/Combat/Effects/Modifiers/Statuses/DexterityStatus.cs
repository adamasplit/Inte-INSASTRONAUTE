using UnityEngine;
public class DexterityStatus : StatusEffect
{
    public DexterityStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Dextérité";
        modifierType = ModifierType.Additive;
        Update(null);
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
        return stat == StatType.Armor && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int armor, EffectContext ctx)
    {
        return armor + Value;
    }
    public override string Desc(bool isPlayer)
    {
        if (Value > 0)
        {
            return $"Augmente l'Armure obtenue de {Value}.";
        }
        else if (Value < 0)
        {
            return $"Réduit l'Armure obtenue de {-Value}.";
        }
        else
        {
            return $"Aucun effet.";
        }
    }
}