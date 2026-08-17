using UnityEngine;

public class SolarCoreRelic : Relic
{
    private bool triggered;

    public SolarCoreRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Panneau solaire";
        description = "Au début du combat, gagnez 2 énergie et piochez 2 cartes.";
    }

    public override void OnCombatStart(Character player)
    {
        triggered = false;
    }

    public override void OnTurnStart(Character player)
    {
        // Grant here (not OnCombatStart) so it survives the first turn's energy reset.
        if (triggered)
        {
            return;
        }

        triggered = true;
        player.GainEnergy(2);
        player.DrawCard();
        player.DrawCard();
    }
}