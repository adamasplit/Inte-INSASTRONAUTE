using UnityEngine;

public class OverclockRelic : Relic
{
    private int turns;

    public OverclockRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Surcadençage";
        description = "Au début des 2 premiers tours d'un combat, gagnez 1 énergie.";
    }

    public override void OnCombatStart(Character player)
    {
        turns = 0;
    }

    public override void OnTurnStart(Character player)
    {
        if (turns < 2)
        {
            player.GainEnergy(1);
            turns++;
        }
    }
}