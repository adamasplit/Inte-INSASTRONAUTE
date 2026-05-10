public class MRIERelic:Relic
{
    public MRIERelic()
    {
        name="Coeur galactique";
        description="Au début de chaque combat, ajoute 1 Météorite à votre main.";
        rarity=RelicRarity.Base;
    }
    public override void OnCombatStart(Character character)
    {
        base.OnCombatStart(character);
        if (character is Player player)
        {
            player.GetCombatManager().deck.AddToHand(new CardInstance(STSCardDatabase.Get("Météorite")));
        }
    }
}