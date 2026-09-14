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

    /// <summary>
    /// Mégacristal demande un coup de moins : le compteur s'arrête alors à 2, comme côté
    /// serveur (StatusReaction.crystallizeThreshold). La valeur dorée de l'icône suit.
    /// </summary>
    public override void OnOwnerStatusesChanged()
    {
        maxValue = owner != null && owner.statusEffects.Exists(s => s is MegacristalStatus) ? 2 : 3;
    }

    public override string Desc(bool isPlayer)
    {
        OnOwnerStatusesChanged();
        // Le coup qui remplit le compteur est celui qui déclenche l'attaque supplémentaire :
        // arrivé au maximum, il reste un coup, pas zéro.
        int hitsLeft = Mathf.Max(1, maxValue - Value + 1);
        if (hitsLeft == 1)
        {
            return $"La prochaine fois que la cible subira des dégâts d'Attaque, elle subira une attaque supplémentaire et perdra 1 effet positif.";
        }
        return $"Une fois que la cible aura subi des dégâts d'Attaque {hitsLeft} fois, elle subira une attaque supplémentaire et perdra 1 effet positif.";
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
