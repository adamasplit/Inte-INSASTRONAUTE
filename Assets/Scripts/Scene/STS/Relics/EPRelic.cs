using UnityEngine;
public class EPRelic:Relic
{
    public EPRelic()
    {
        name = "Moteur spatial";
        description = "Au début de chaque tour, gagne 1 énergie supplémentaire.";
    }
    public override void OnTurnStart(Character player)
    {
        player.resources.energy++;
    }
}