using UnityEngine;
using System.Collections.Generic;
public class CardFollowUpStatus:FollowUpStatus
{
    public CardFollowUpStatus(int value,int duration,string effectInfo=""):base(value,duration,effectInfo)
    {
    }
    public override string Desc(bool isPlayer)
    {
        string nameStr = randomCard?"une carte aléatoire":$"<color=green>{Name}</color>";
        string maxValueStr = maxValue > 1 ? $"{maxValue}" : "";
        string baseStr=$"Toutes les {maxValueStr} cartes "+(moveIndex!=0?cardType().ToString():"");
        if (maxValue==1)
        {
            if (isPlayer)
            {
                return baseStr+$" que vous jouez déclenchent {nameStr}.";
            }
            return baseStr+$" jouées par l'ennemi activent {nameStr}.";
        }
        if (isPlayer)
        {
            return baseStr+$" jouées, déclenchez {nameStr}.";
        }
        return baseStr+$" jouées, l'ennemi joue aussi {nameStr}.";
    }
    public override void OnCardPlayed(Character source,Character target,CardInstance card)
    {
        if ((moveIndex==0||card.data.type == cardType())&&!card.HasTag(CardTag.FollowUp))
        {
            IncrementFollowUp(source,target);
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