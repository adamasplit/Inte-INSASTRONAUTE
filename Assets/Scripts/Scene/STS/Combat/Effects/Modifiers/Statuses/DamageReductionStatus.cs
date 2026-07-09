using UnityEngine;
using System.Collections.Generic;
public class DamageReductionStatus : StatusEffect
{
    public DamageReductionStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Réduction de dégâts";
        modifierType = ModifierType.Multiplicative;
        buff=true;
        framed=true;
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        list.Add(this);
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage&& ctx.target!=null && ctx.target.statusEffects.Contains(this);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target==null || !ctx.target.statusEffects.Contains(this))
            return value;
        return value - (value * Value / 100);
    }
    public override string Desc(bool isPlayer)
    {
        return $"Réduit les dégâts subis de {Value}% pendant {Duration} tour"+(Duration>1?"s":"");
    }
}