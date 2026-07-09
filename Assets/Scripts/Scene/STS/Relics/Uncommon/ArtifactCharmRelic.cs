using UnityEngine;

public class ArtifactCharmRelic : Relic
{
    public ArtifactCharmRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Plaque d'essai";
        description = "Au début du combat, gagnez 1 d'Artéfact.";
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Artifact, 1, -1));
    }
}