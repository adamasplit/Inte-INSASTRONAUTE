using UnityEngine;
using System.Collections.Generic;
public class FollowUpStatus:StatusEffect
{
    public FollowUpStatus()
    {
        Duration = -1;
        Name = "Demi-volée";
        debuff=false;
        buff=true;
        framed=true;
    }
    public override string Desc()
    {
        return $"Lorsque vous attaquez un ennemi, déclenchez une attaque supplémentaire.";
    }
    public override void OnCardPlayed(Character source,Character target,CardInstance card)
    {
        if (card.data.type == CardType.Attaque&& card.data.name != "Tir cadré"&&source.GetCombatManager().currentCardName!="Tir cadré")
        {
            CardInstance crystalCard = new CardInstance(STSCardDatabase.Get("Tir cadré"));
            source.GetCombatManager().PlayCard(source,crystalCard,new List<Character>(){target},false,true);
        }
    }
}