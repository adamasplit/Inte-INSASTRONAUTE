using UnityEngine;
public class EPRelic:Relic
{
    public EPRelic()
    {
        name = "Moteur spatial";
        description = "Au début de chaque tour, donne 1 énergie supplémentaire.";
        rarity=RelicRarity.Base;
    }
    public override void OnTurnStart(Character player)
    {
        player.resources.energy++;
    }
}