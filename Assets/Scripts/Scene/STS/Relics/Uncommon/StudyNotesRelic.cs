using System.Collections.Generic;
using UnityEngine;

public class StudyNotesRelic : Relic
{
    private int cardsPlayedThisTurn;

    public StudyNotesRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Notes de cours";
        description = "La première fois que vous jouez 3 cartes dans un tour, piochez 1 carte.";
    }

    public override void OnCombatStart(Character player)
    {
        cardsPlayedThisTurn = 0;
    }

    public override void OnTurnStart(Character player)
    {
        cardsPlayedThisTurn = 0;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        cardsPlayedThisTurn++;
        if (cardsPlayedThisTurn == 3)
        {
            player.GetCombatManager().deck.Draw(1);
        }
    }
}