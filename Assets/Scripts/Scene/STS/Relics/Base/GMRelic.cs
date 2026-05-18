public class GMRelic:Relic
{
    public GMRelic()
    {
        name = "Oeil impitoyable";
        description="Lorsque vous brisez l'Armure d'un ennemi, son tour est retardé.";
        rarity=RelicRarity.Base;
    }

    public override void OnTargetArmorBroken(Character source, Character target)
    {
        source.combat.turnSystem.ApplyDelayAllTurns(target, 10);
    }
}