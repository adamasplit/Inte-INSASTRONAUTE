public class UltimateStrengthRelic : Relic
{
    public UltimateStrengthRelic()
    {
        name = "Force ultime";
        description = "Donne 1 de Force à chaque début de tour pendant les 3 premiers tours d'un combat.";
        rarity=RelicRarity.Rare;
    }
    private int turnCount = 0;
    public override void OnCombatStart(Character player)
    {
        turnCount = 0;
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        if (turnCount < 3)
        {
            player.AddStatus(StatusEffect.Factory(StatusType.Strength,1,-1));
            turnCount++;
        }
    }
}