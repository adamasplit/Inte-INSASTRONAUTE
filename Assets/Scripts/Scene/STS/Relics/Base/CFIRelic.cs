public class CFIRelic:BaseRelic
{
    public CFIRelic():base()
    {
        namesByStage[0] = "Analyseur de combat";
        descriptionsByStage[0] = "Lorsqu'un ennemi gagne de l'armure, votre prochaine attaque inflige autant de dégâts supplémentaires.";
        namesByStage[1] = "Analyseur de combat - Chaos";
        descriptionsByStage[1] = "Lorsqu'un ennemi gagne de l'armure, vous gagnez un statut positif au hasard.";
        Upgrade(0);
    }
    public override void OnAnyArmorGain(Character target, int amount)
    {
        if (!target.isPlayer)
        {
            RunManager.Instance.player.AddStatus(new VigorStatus(amount));
        }
    }
}