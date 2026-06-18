using UnityEngine;
public class EchoStatus : StatusEffect
{
    public EchoStatus(int value)
    {
        Value = value;
        Name = "Écho";
        modifierType = ModifierType.Additive;
        buff=true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.ReplayCount && ctx.source.statusEffects.Contains(this);
    }
    public override int Modify(int replayCount, EffectContext ctx)
    {
        int res = replayCount + Value;
        if (!ctx.isPreview)
        {
            mustExpire = true;
        }
        return res;
    }
    public override string Desc(bool isPlayer)
    {
        return $"La prochaine carte jouée se rejoue {Value} fois.";
    }
}