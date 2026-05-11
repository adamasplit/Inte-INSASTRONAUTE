public class UltimateDexterityRelic : Relic
{
    public UltimateDexterityRelic()
    {
        name = "Dextérité ultime";
        description = "Donne 1 de Dextérité à chaque début de tour.";
        rarity=RelicRarity.Rare;
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        player.AddStatus(StatusEffect.Factory(StatusType.Dexterity,1,-1));
    }
}