public class DSRelic : Relic
{
    public DSRelic()
    {
        rarity = RelicRarity.Common;
        name = "Annales des DS";
        description = "Au deuxième tour d'un combat, gagnez 16 d'Armure.";
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
        if (turnCounter == 2)
        {
            player.AddArmor(16);
        }
    }
}