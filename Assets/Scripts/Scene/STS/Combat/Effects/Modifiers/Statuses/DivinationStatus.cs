using UnityEngine;
public class DivinationStatus : StatusEffect
{

    public DivinationStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Divination";
        buff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Annule les dégâts d'une attaque ennemie. Se déclenche {Value} fois.";
        }
        return $"Annule les dégâts d'une de vos attaques. Se déclenche {Value} fois.";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage &&ctx.target!=null&& ctx.target.statusEffects.Contains(this) && ctx.source != ctx.target;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        if (damage<=ctx.target.armor)
        {
            return damage;
        }
        if (!ctx.isPreview) Value--;
        if (Value <= 0)
        {
            mustExpire=true;
        }
        return 0;
    }
}