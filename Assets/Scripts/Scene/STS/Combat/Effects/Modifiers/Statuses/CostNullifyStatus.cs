using UnityEngine;
public class CostNullifyStatus : StatusEffect
{
    public CostNullifyStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Coût nul";
        modifierType = ModifierType.Override;
        buff=true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Cost && ctx.source.statusEffects.Contains(this);
    }
    public override int Modify(int cost, EffectContext ctx)
    {
        int res = 0;
        if (!ctx.isPreview)
        {
            if (Value<=0)
            {
                mustExpire = true;
            }
        }
        return res;
    }
    public override string Desc()
    {
        if (Value>1)
        {
            return $"Les {Value} prochaines cartes jouées coûtent 0.";
        }
        return $"La prochaine carte jouée coûte 0.";
    }
}