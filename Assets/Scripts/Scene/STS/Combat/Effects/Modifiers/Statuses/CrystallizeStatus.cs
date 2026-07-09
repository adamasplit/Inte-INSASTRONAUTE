using System.Collections.Generic;
using UnityEngine;
public class CrystallizeStatus : StatusEffect
{
    bool followUp=false;
    public CrystallizeStatus(int duration)
    {
        Value = 1;
        maxValue = 3;
        Duration = duration;
        Name = "Cristallisation";
        debuff=true;
        generic=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (Value==maxValue)
        {
            return $"La prochaine fois que la cible subira des dégâts, elle subira une attaque supplémentaire et perdra 1 effet positif.";
        }
        return $"Une fois que la cible aura subi des dégâts d'Attaque {maxValue-Value} fois, elle subira une attaque supplémentaire et perdra 1 effet positif.";
    }
    public override void OnTargetedByCard(Character source, Character target, CardInstance card)
    {
        if (card.data.type == CardType.Attaque&&followUp&&!card.HasTag(CardTag.FollowUp))
        {
            CardInstance crystalCard = new CardInstance(STSCardDatabase.Get("Recristallisation"));
            source.GetCombatManager().PlayCard(source,crystalCard,new List<Character>(){target},false,true);
            Value = 1; // Réinitialisation du compteur
            followUp=false;
        }
    }
    public override void OnDamageTaken(Character source,Character target, ref int damage)
    {
        if (source.GetCombatManager().currentCard.displayName=="Recristallisation")
        {
            return; // Ignore damage from Recristallisation to prevent infinite loop
        }
        if (Value >= maxValue)
        {
            followUp=true;
        }
        else
        {
            Value++;
        }
    }
}