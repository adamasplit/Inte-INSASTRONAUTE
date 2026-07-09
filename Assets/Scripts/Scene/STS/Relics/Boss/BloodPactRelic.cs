using UnityEngine;

public class BloodPactRelic : Relic
{
    public BloodPactRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Don du sang";
        description = "Perdez 10 PV max. Lorsque vous êtes soigné en combat, gagnez 1 d'énergie.";
    }

    public override void OnAcquire(Character player)
    {
        player.LoseMaxHP(10);
    }

    public override int OnHeal(Character target, int amount)
    {
        if (target != null && target.isPlayer && amount > 0)
        {
            target.GainEnergy(1);
        }
        return amount;
    }
}