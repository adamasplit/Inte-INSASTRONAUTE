using UnityEngine;
public class EchoStatus : StatusEffect
{
    private int moveIndex;
    public EchoStatus(int value,int duration)
    {
        Value = duration;
        moveIndex = value;
        Duration = -1;
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
        if (ctx.card == null) return replayCount;
        if (ctx.card.data.type!=cardType()) return replayCount;
        int res = replayCount + Value;
        if (!ctx.isPreview)
        {
            mustExpire = true;
        }
        return res;
    }
    public override string Desc(bool isPlayer)
    {
        return $"La prochaine carte{(moveIndex==0?"":" "+cardType().ToString())} jouée se rejoue {Value} fois supplémentaire{(Value>1?"s":"")}.";
    }
    public CardType cardType()
    {
        return moveIndex switch
        {
            1 => CardType.Attaque,
            2 => CardType.Compétence,
            3 => CardType.Pouvoir,
            _ => CardType.Rien
        };
    }
}