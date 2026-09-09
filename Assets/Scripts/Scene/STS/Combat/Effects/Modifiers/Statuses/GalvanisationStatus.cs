using UnityEngine;
public class GalvanisationStatus : StatusEffect
{
    public GalvanisationStatus()
    {
        Duration = -1;
        Name = "Galvanisation";
        modifierType = ModifierType.Multiplicative;
        buff = true;
        framed = true;
        generic = true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return ctx.source != null && ctx.source.statusEffects.Contains(this) && StatTypeChecker.IsValid(stat);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.source == null || ctx.source.maxHP <= 0)
        {
            return value;
        }
        int missingPercent = Mathf.Max(0,
            (ctx.source.maxHP - ctx.source.currentHP) * 100 / ctx.source.maxHP);
        return value + (value * missingPercent / 100);
    }
    public override string Desc(bool isPlayer)
    {
        return "Tous les effets que vous appliquez sont augmentés du pourcentage de PV que vous avez perdus.";
    }
}
