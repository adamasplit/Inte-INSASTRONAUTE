using UnityEngine;

public class PortableWorkshopRelic : Relic
{
    public PortableWorkshopRelic()
    {
        rarity = RelicRarity.Common;
        name = "Atelier";
        description = "Au début du combat, gagnez 4 d'Armure.";
    }

    public override void OnCombatStart(Character player)
    {
        player.AddArmor(4);
    }
}