using UnityEngine;

public class CrownOfAshesRelic : Relic
{
    private int turns;

    public CrownOfAshesRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Réveil brutal";
        description = "Perdez 12 PV max. Au début des 3 premiers tours d'un combat, gagnez 1 énergie.";
    }

    public override void OnAcquire(Character player)
    {
        player.LoseMaxHP(12);
    }

    public override void OnCombatStart(Character player)
    {
        turns = 0;
    }

    public override int EnergyOnTurnStart(int previousEnergy, Character character)
    {
        if (turns < 3)
        {
            turns++;
            return 1;
        }

        return 0;
    }
}