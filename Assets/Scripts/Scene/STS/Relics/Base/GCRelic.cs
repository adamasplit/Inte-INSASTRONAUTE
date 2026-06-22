public class GCRelic:BaseRelic
{
    public GCRelic():base()
    {
        namesByStage[0] = "Fondation solide";
        descriptionsByStage[0] = "À la fin de chaque tour (ennemi ou allié), donne 3 d'Armure. L'Armure n'est pas perdue quand vous jouez plusieurs tours à la suite.";
        rarity=RelicRarity.Base;
        Upgrade(0);
    }
    public override void OnFieldTurnEnd(Character character)
    {
        if (character.isPlayer)
        {
            RunManager.Instance.player.AddArmor(3);
            VFXManager.Instance.PlayEffect("GCRelicActivate", character);
        }
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