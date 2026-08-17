using UnityEngine;

public class PortableWorkshopRelic : Relic
{
    private bool triggered;

    public PortableWorkshopRelic()
    {
        rarity = RelicRarity.Common;
        name = "Atelier";
        description = "Au début du combat, gagnez 4 d'Armure.";
    }

    public override void OnCombatStart(Character player)
    {
        triggered = false;
    }

    public override void OnTurnStart(Character player)
    {
        // Grant here (not OnCombatStart) so it survives the first turn's armor reset.
        if (triggered)
        {
            return;
        }

        triggered = true;
        player.AddArmor(4);
    }
}