using UnityEngine;

public class GlassCannonRelic : Relic
{
    public GlassCannonRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Canon de verre";
        description = "Perdez 20 PV max. Gagnez 5 de Vitesse durant chaque combat.";
    }

    public override void OnAcquire(Character player)
    {
        player.LoseMaxHP(20);
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Speed, 5, -1));
    }
}