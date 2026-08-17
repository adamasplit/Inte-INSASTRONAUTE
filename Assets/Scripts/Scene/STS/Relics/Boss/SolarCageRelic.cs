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
        triggered = false;
        grantedThisCombat = false;
    }
    private bool triggered = false;
    private bool grantedThisCombat = false;

    public override void OnTurnStart(Character player)
    {
        // Grant here (not OnCombatStart) so it survives the first turn's energy reset.
        if (grantedThisCombat)
        {
            return;
        }

        grantedThisCombat = true;
        player.GainEnergy(1);
        player.DrawCard();
        player.DrawCard();
        player.DrawCard();
    }

    public override void OnTurnEnd(Character player)
    {
        if (player != null && player.isPlayer && !triggered)
        {
            player.TakeDamage(10, true);
            triggered = true;
        }
    }
}