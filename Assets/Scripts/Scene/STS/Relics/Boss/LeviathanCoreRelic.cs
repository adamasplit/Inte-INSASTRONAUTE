using UnityEngine;

public class LeviathanCoreRelic : Relic
{
    public LeviathanCoreRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Noyau d'étoile";
        description = "Au début du combat, gagnez 2 énergie et 1 de Force.";
    }
    public override void OnCombatStart(Character player)
    {
        player.GainEnergy(2);
        player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
    }
}