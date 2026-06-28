using System.Collections.Generic;
using UnityEngine;
public class ImitateStatus : StatusEffect
{
    bool followUp=false;
    private CardInstance lastPlayedCard;
    public ImitateStatus()
    {
        Value = 0;
        Duration = -1;
        Name = "Imitation";
        buff=true;
        generic=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Quand vous êtes ciblé par une carte, une copie de celle-ci est ajoutée à votre main.";
        }
        return $"À son tour, l'ennemi jouera une copie de la dernière carte que vous lui avez jouée.";
    }
    public override void OnTargetedByCard(Character source, Character target, CardInstance card)
    {
        if (owner == target)
        {
            lastPlayedCard = card;
            if (target.isPlayer)
            {
                CardInstance copyCard = new CardInstance(card.data);
                source.GetCombatManager().deck.AddToHand(copyCard);
            }
            else
            {
                Enemy enemy = target as Enemy;
                if (enemy != null && lastPlayedCard != null)
                {
                    enemy.ForceNextAction(lastPlayedCard.data);
                }
            }
        }
    }
}