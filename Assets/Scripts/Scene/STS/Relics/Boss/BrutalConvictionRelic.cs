using System.Collections.Generic;
using UnityEngine;

public class BrutalConvictionRelic : Relic
{
    private bool triggeredThisTurn;

    public BrutalConvictionRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Rage de vaincre";
        description = "La première carte d'Attaque que vous jouez chaque tour vous donne 1 de Force. Toutes les 3 cartes Attaque, perdez 1 PV.";
    }
    private int counter = 0;
    public override void OnCombatStart(Character player)
    {
        triggeredThisTurn = false;
    }

    public override void OnTurnStart(Character player)
    {
        triggeredThisTurn = false;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card == null || card.data == null || card.data.type != CardType.Attaque)
        {
            return;
        }
        counter++;
        if (counter>3)
        {
            player.TakeDamage(1, true);
            counter = 0;
        }

        if (!triggeredThisTurn)
        {
            triggeredThisTurn = true;
            player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        }
    }
}