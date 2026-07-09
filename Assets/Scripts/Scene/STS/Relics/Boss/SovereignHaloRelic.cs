using UnityEngine;

public class SovereignHaloRelic : Relic
{
    public SovereignHaloRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Halo souverain";
        description = "Perdez 15 PV max. Au début du combat, gagnez 3 d'Artifact.";
    }

    public override void OnAcquire(Character player)
    {
        player.LoseMaxHP(15);
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Artifact, 3, -1));
    }
}