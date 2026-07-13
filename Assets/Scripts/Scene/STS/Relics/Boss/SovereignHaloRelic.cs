using UnityEngine;

public class SovereignHaloRelic : Relic
{
    public SovereignHaloRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Raspberry Pi";
        description = "Perdez 5 PV max. Au début du combat, gagnez 3 d'Artéfact.";
    }

    public override void OnAcquire(Character player)
    {
        player.LoseMaxHP(5);
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Artifact, 3, -1));
    }
}