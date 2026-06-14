using UnityEngine;
using System.Collections.Generic;
public class FollowUpStatus:StatusEffect
{
    public FollowUpStatus(int value,string effectInfo="")
    {
        Duration = -1;
        Name = effectInfo;
        Value = value;
        debuff=false;
        buff=true;
        framed=true;
        STSCardData data = STSCardDatabase.Get(Name);
        if (data == null) 
        {
            mustExpire=true;
        }
        else
        {
            if (data.HasTag(CardTag.Status) || data.HasTag(CardTag.Curse))
            {
                buff=false;
                debuff=true;
            }
        }
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        StatusEffect other = list.Find(s => s.GetType() == this.GetType()&&s.Name==this.Name&&s.Value==this.Value);
        if (other != null)
        {
        }
        else
        {
            list.Add(this);
        }
    }
    public override string Desc()
    {
        return $"Lorsque vous jouez une carte "+(Value!=0?cardType().ToString():"")+$", déclenchez {Name}.";
    }
    public override string CardDesc(bool targetSelf)
    {
        if (targetSelf)
        {
            return Desc();
        }
        return $"Lorsque la cible joue une carte "+(Value!=0?cardType().ToString():"")+$", elle déclenche {Name}.";
    }
    public override void OnCardPlayed(Character source,Character target,CardInstance card)
    {
        if ((Value==0||card.data.type == cardType())&&!card.HasTag(CardTag.FollowUp))
        {
            STSCardData data = STSCardDatabase.Get(Name);
            if (data == null)
            {
                Debug.LogWarning($"Carte de suivi introuvable : {Name}");
                return;
            }
            CardInstance crystalCard = new CardInstance(data);
            source.GetCombatManager().PlayCard(source,crystalCard,source.GetCombatManager().AutoCardTargets(crystalCard.targetingMode,source,target),false,true);
        }
    }
    private CardType cardType()
    {
        return Value switch
        {
            1 => CardType.Attaque,
            2 => CardType.Compétence,
            3 => CardType.Pouvoir,
            _ => CardType.Attaque
        };
    }
}