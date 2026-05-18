public class GCRelic:Relic
{
    public GCRelic()
    {
        name="Fondation solide";
        description="À la fin de chaque tour (ennemi ou allié), donne 2 d'Armure. L'Armure n'est pas perdue en cas de tours consécutifs.";
        rarity=RelicRarity.Base;
    }
    public override void OnAnyTurnEnd(Character character)
    {
        RunManager.Instance.player.AddArmor(2);
    }
    public override int ArmorOnTurnStart(int previousArmor,Character character)
    {
        if (character.GetCombatManager().state.playerLastTurn)
        {
            return previousArmor;
        }
        else
        {
            return 0;
        }
    }
}