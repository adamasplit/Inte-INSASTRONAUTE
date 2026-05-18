using UnityEngine;
public class EPRelic:Relic
{
    public EPRelic()
    {
        name = "Moteur spatial";
        description = "Au début de chaque tour, gagnez 1 énergie et piochez une carte.";
        rarity=RelicRarity.Base;
    }
    public override int EnergyOnTurnStart(int previousEnergy,Character character)
    {
        return 1;
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        player.GetCombatManager().deck.Draw(1);
    }
}