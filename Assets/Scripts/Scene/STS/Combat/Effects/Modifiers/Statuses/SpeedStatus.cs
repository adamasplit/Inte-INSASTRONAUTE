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
            return Mathf.Clamp(Mathf.RoundToInt(50f * Mathf.Log10(Value + 1f)), 0, 99);
        }
        else
        {
            return -Mathf.Clamp(Mathf.RoundToInt(50f * Mathf.Log10(-Value + 1f)), 0, 99);
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
    public override string Desc(bool isPlayer)
    {
        if (Value > 0)
        {
            return $"Réduit le délai des tours de {SpeedValue()}%";
        }
        else if (Value < 0)
        {
            return $"Augmente le délai des tours de {-SpeedValue()}%";
        }
        else
        {
            return "Aucun effet";
        }
    }
}