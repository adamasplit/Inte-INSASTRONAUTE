using UnityEngine;
public class AreaDmgReductionStatus : StatusEffect
{
    public AreaDmgReductionStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Réduction de dégâts de zone";
        modifierType = ModifierType.Multiplicative;
        buff=true;
        framed=true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.targets != null && ctx.targets.Count > 1 && ctx.target!=null && ctx.target.statusEffects.Contains(this);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.targets == null||ctx.targets.Count<=1)
            return value;
        return value - (value * Value / 100);
    }
    public override string Desc(bool isPlayer)
    {
        return $"Réduit les dégâts de zone subis de {Value}% pendant {Duration} tour"+(Duration>1?"s":"");
    }
}