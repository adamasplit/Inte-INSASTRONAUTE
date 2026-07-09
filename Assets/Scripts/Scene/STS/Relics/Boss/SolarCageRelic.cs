using UnityEngine;

public class SolarCageRelic : Relic
{
    public SolarCageRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Cage solaire";
        description = "Au début du combat, gagnez 1 énergie et piochez 3 cartes. À la fin du premier tour, subissez 10 dégâts.";
    }

    public override void OnCombatStart(Character player)
    {
        player.GainEnergy(1);
        player.DrawCard();
        player.DrawCard();
        player.DrawCard();
        triggered = false;
    }
    private bool triggered = false;

    public override void OnTurnEnd(Character player)
    {
        if (player != null && player.isPlayer && !triggered)
        {
            player.TakeDamage(10, true);
            triggered = true;
        }
    }
}