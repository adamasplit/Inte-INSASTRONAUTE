using System.Collections.Generic;
using UnityEngine;
public class PERFRelic:BaseRelic
{
    public PERFRelic():base()
    {
        namesByStage[0] = "Alternateur de performance";
        descriptionsByStage[0] = "Gagne 1 énergie en moins par tour. Quand vous subissez des dégâts, jouez la carte du dessus de votre pioche.";
        Upgrade(0);
    }
    public override int EnergyOnTurnStart(int previousEnergy, Character character)
    {
        return -1;
    }
    public override void OnDamageTaken(Character source, Character target, int amount)
    {
        var combat = target.GetCombatManager();
        if (combat == null) return;
        var card = combat.deck.GetAndRemoveTopCard();
        if (card == null)
        {
            return;
        }
        combat.PlayCard(combat.player, card, new List<Character> {source},true,true);
    }
}