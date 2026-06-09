using UnityEngine;
public class SpeedStatus : StatusEffect
{
    public SpeedStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Vitesse";
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

    int SpeedValue()
    {
        if (Value>=0)
        {
            return Mathf.RoundToInt(50f * Mathf.Log10(Value + 1f));
        }
        else
        {
            return -Mathf.RoundToInt(50f * Mathf.Log10(-Value + 1f));
        }
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.TurnDelay && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int turnDelay, EffectContext ctx)
    {
        return turnDelay * (100 - SpeedValue()) / 100;
    }
    public override string Desc()
    {
        if (Value > 0)
        {
            return $"Réduit le délai de vos tours de {SpeedValue()}%";
        }
        else if (Value < 0)
        {
            return $"Augmente le délai de vos tours de {-SpeedValue()}%";
        }
        else
        {
            return "Aucun effet";
        }
    }
}