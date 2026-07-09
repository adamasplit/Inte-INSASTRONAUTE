using UnityEngine;

public class SolarCoreRelic : Relic
{
    public SolarCoreRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Panneau solaire";
        description = "Au début du combat, gagnez 2 énergie et piochez 2 cartes.";
    }

    public override void OnCombatStart(Character player)
    {
        player.GainEnergy(2);
        player.DrawCard();
        player.DrawCard();
    }
}