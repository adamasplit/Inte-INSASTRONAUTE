using UnityEngine;

public class LeviathanCoreRelic : Relic
{
    private bool triggered;

    public LeviathanCoreRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Noyau d'étoile";
        description = "Au début du combat, gagnez 2 énergie et 1 de Force.";
    }
    public override void OnCombatStart(Character player)
    {
        triggered = false;
        player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
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
    }
}