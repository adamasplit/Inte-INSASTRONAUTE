public class MECARelic:Relic
{
    public MECARelic()
    {
        name = "Autoréparation";
        description = "Rend 10 PV à la fin d'un combat.";
        rarity=RelicRarity.Base;
    }
    public override void OnCombatEnd(Character player)
    {
        player.Heal(10);
    }
}