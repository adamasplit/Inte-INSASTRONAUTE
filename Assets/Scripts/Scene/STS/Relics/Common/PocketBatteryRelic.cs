using UnityEngine;

public class PocketBatteryRelic : Relic
{
    private int turns;

    public PocketBatteryRelic()
    {
        rarity = RelicRarity.Common;
        name = "Batterie de poche";
        description = "Au début du premier tour d'un combat, gagnez 1 énergie.";
    }

    public override void OnCombatStart(Character player)
    {
        turns = 0;
    }

    public override void OnTurnStart(Character player)
    {
        if (turns < 1)
        {
            player.GainEnergy(1);
            turns++;
        }
    }
}