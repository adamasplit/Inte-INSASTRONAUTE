using UnityEngine;
using System.Collections.Generic;
public class CardFollowUpStatus:FollowUpStatus
{
    public CardFollowUpStatus(int value,int duration,string effectInfo="",int index=0):base(value,duration,effectInfo,index)
    {
    }
    public override string Desc(bool isPlayer)
    {
        string nameStr = randomCard?"une carte aléatoire":$"<color=green>{Name}</color>";
        string maxValueStr = maxValue > 1 ? $"{maxValue}" : "";
        string baseStr=$"Toutes les {maxValueStr} cartes "+(moveIndex!=0?cardType().ToString():"");
        string endStr=Duration>0?$" (s'active {Duration} fois)":"";
        if (maxValue==1)
        {
            if (isPlayer)
            {
                return baseStr+$" que vous jouez déclenchent {nameStr}{endStr}.";
            }
            return baseStr+$" jouées par l'ennemi activent {nameStr}{endStr}.";
        }
        if (isPlayer)
        {
            return baseStr+$" jouées, déclenchez {nameStr}{endStr}.";
        }
        return baseStr+$" jouées, l'ennemi joue aussi {nameStr}{endStr}.";
    }
    public override void OnCardPlayed(Character source,Character target,CardInstance card)
    {
        if ((moveIndex==0||card.data.type == cardType())&&!card.HasTag(CardTag.FollowUp))
        {
            IncrementFollowUp(source,target);
        }
    }
    public override void OnTurnEnd(Character character)
    {
        if (Duration>0)
        {
            Duration--;
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