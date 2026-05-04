public class MRIERelic:Relic
{
    public MRIERelic()
    {
        name="Coeur galactique";
        description="Au début de chaque tour, ajoute 1 Météorite à votre main.";
    }
    public override void OnTurnStart(Character character)
    {
        base.OnTurnStart(character);
        if (character is Player player)
        {
            player.GetCombatManager().deck.AddToHand(new CardInstance(STSCardDatabase.Get("Météorite")));
        }
    }
}