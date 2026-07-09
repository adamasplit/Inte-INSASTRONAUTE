using System.Collections.Generic;
using UnityEngine;

public class TempoHourglassRelic : Relic
{
    private int cardsPlayedThisTurn;

    public TempoHourglassRelic()
    {
        rarity = RelicRarity.Rare;
        name = "200 BPM";
        description = "Après avoir joué 5 cartes dans un tour, gagnez 2 de Célérité.";
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
        if (cardsPlayedThisTurn == 4)
        {
            player.AddStatus(StatusEffect.Factory(StatusType.Haste, 0, 2));
        }
    }
}