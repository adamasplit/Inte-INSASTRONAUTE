public class ISRelic : Relic
{
    public ISRelic()
    {
        rarity = RelicRarity.Common;
        name = "Annales des IS";
        description = "Au premier tour d'un combat, gagnez 10 d'Armure.";
    }
    private int turnCounter = 0;
    public override void OnCombatStart(Character player)
    {
        turnCounter = 0;
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        turnCounter++;
        if (turnCounter == 1)
        {
            player.AddArmor(10);
        }
    }
}