using UnityEngine;
public class FreezeStatus:StatusEffect
{
    public FreezeStatus(int duration)
    {
        Duration = duration;
        Name = "Congélation";
        modifierType = ModifierType.Multiplicative;
        debuff=true;
        framed=true;
        generic=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Tous les effets appliqués sont réduits de moitié.";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return ctx.source.statusEffects.Contains(this)&&(StatTypeChecker.IsValid(stat));
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return Mathf.RoundToInt(value * 0.5f);
    }
}