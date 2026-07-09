using System.Collections.Generic;
using UnityEngine;

public class WarpedChronometerRelic : Relic
{
    private int cardsPlayedThisTurn;

    public WarpedChronometerRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Chronomètre spatial";
        description = "La troisième carte que vous jouez à chaque tour vous fait piocher 2 cartes, gagner 1 d'énergie et perdre 1 PV.";
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
            player.TakeDamage(1, true);
            player.DrawCard();
            player.DrawCard();
            player.GainEnergy(1);
        }
    }
}