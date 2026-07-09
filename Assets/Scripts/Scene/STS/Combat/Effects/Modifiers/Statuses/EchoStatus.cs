using UnityEngine;
public class EchoStatus : StatusEffect
{
    private int moveIndex;
    public EchoStatus(int value,int duration,int moveIndex)
    {
        Value = value;
        Duration = duration;
        moveIndex = moveIndex;
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
        if (ctx.card.data.type!=cardType()&&moveIndex!=0||ctx.card.HasTag(CardTag.FollowUp)) return replayCount;
        int res = replayCount + Value;
        if (!ctx.isPreview)
        {
            Duration--;
        }
        return res;
    }
    public override void OnTurnEnd(Character character)
    {
    }
    public override string Desc(bool isPlayer)
    {
        bool singular = Duration==1;
        return $"{(singular?"La prochaine carte":$"Les {Duration} prochaines cartes")} {(moveIndex==0?"":" "+cardType().ToString())} jouée{(singular?"":"s")} se rejoue{(singular?"":"nt")} {Value} fois supplémentaire{(Value>1?"s":"")}.";
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