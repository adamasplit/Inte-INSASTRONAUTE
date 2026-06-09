public class MRIERelic:BaseRelic
{
    public MRIERelic():base()
    {
        namesByStage[0] = "Coeur galactique";
        descriptionsByStage[0] = "Au début de chaque combat, ajoute 1 Météorite à votre pioche et une Galaxie à votre défausse.";
        Upgrade(0);
    }
    public override void OnCombatStart(Character character)
    {
        base.OnCombatStart(character);
        if (character is Player player)
        {
            player.GetCombatManager().deck.drawPile.Add(new CardInstance(STSCardDatabase.Get("Météorite")));
            player.GetCombatManager().deck.discardPile.Add(new CardInstance(STSCardDatabase.Get("Galaxie")));
        }
    }
}