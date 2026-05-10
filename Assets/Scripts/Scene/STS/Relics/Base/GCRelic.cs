public class GCRelic:Relic
{
    public GCRelic()
    {
        name="Fondation solide";
        description="À la fin de chaque tour, donne 2 d'Armure par énergie, par carte en main et par ennemi présent.";
        rarity=RelicRarity.Base;
    }
    public override void OnTurnEnd(Character character)
    {
        base.OnTurnEnd(character);
        if (character is Player player)
        {
            int armorGain = (player.resources.energy*2 + player.GetCombatManager().deck.hand.Count + player.GetCombatManager().enemies.Count);
            player.AddArmor(armorGain);
        }
    }

}