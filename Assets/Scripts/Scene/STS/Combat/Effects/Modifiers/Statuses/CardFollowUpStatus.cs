using UnityEngine;
using System.Collections.Generic;
public class CardFollowUpStatus:StatusEffect
{
    private int moveIndex=0;
    public CardFollowUpStatus(int value,int duration,string effectInfo="")
    {
        Duration = -1;
        Name = effectInfo;
        Value = 1;
        moveIndex = value;
        maxValue=Mathf.Max(1, duration);
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
        StatusEffect other = list.Find(s => s.GetType() == this.GetType()&&s.Name==this.Name&&((CardFollowUpStatus)s).cardType()==this.cardType());
        if (other != null)
        {
        }
        else
        {
            list.Add(this);
        }
    }
    public override string Desc(bool isPlayer)
    {
        string maxValueStr = maxValue > 1 ? $"{maxValue}" : "";
        if (isPlayer)
        {
            return $"Toutes les {maxValueStr} carte "+(Value!=0?cardType().ToString():"")+$", déclenchez {Name}.";
        }
        return $"Toutes les {maxValueStr} carte "+(Value!=0?cardType().ToString():"")+$", l'ennemi joue aussi {Name}.";
    }
    public override void OnCardPlayed(Character source,Character target,CardInstance card)
    {
        if ((Value==0||card.data.type == cardType())&&!card.HasTag(CardTag.FollowUp))
        {
            Value++;
            if (Value>maxValue)
            {
                Value=1;
                STSCardData data = STSCardDatabase.Get(Name);
                if (data == null)
                {
                    Debug.LogWarning($"Carte de suivi introuvable : {Name}");
                    return;
                }
                CardInstance crystalCard = new CardInstance(data);
                source.GetCombatManager().PlayCard(source,crystalCard,source.GetCombatManager().AutoCardTargets(crystalCard.targetingMode,source,target),true,true);
            }
        }
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