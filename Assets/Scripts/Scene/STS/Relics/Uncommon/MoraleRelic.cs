public class MoraleRelic:Relic
{
    public MoraleRelic()
    {
        name = "Moral en hausse";
        description = "Rend 1 PV à chaque début de tour si vous avez moins de 50% de votre santé.";
        rarity=RelicRarity.Uncommon;
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        if (player.currentHP < player.maxHP / 2)
        {
            player.Heal(1);
        }
    }
}