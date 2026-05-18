public class CFIRelic:Relic
{
    public CFIRelic()
    {
        name="Analyseur de combat";
        description="Lorsqu'un ennemi gagne de l'armure, votre prochaine attaque inflige autant de dégâts supplémentaires.";
        rarity=RelicRarity.Base;
    }
    public override void OnAnyArmorGain(Character target, int amount)
    {
        if (!target.isPlayer)
        {
            RunManager.Instance.player.AddStatus(new VigorStatus(amount));
        }
    }
}