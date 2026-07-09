public class GMRelic:BaseRelic
{
    public GMRelic():base()
    {
        namesByStage[0] = "Oeil impitoyable";
        descriptionsByStage[0] = "Lorsque vous brisez l'Armure d'un ennemi, son tour est retardé.";
        namesByStage[1] = "Oeil destructeur";
        descriptionsByStage[1] = "Lorsque vous brisez l'Armure d'un ennemi, il perd 10 PV.";
        namesByStage[2] = "Oeil impitoyable+";
        descriptionsByStage[2] = "Lorsque vous brisez l'Armure d'un ennemi, son tour est supprimé.";
        namesByStage[3] = "Oeil de la mort";
        descriptionsByStage[3] = "Lorsque vous brisez l'Armure d'un ennemi, il meurt instantanément.";
        Upgrade(0);
    }

    public override void OnTargetArmorBroken(Character source, Character target)
    {
        VFXManager.Instance.PlayEffect("Shatter", target);
        switch (stage)
        {
            case 0:
                source.combat.turnSystem.ApplyDelayAllTurns(target, 10);
                break;
            case 1:
                target.TakeDamage(10);
                break;
            case 2:
                source.combat.turnSystem.ApplyDelayAllTurns(target, 25);
                break;
            case 3:
                target.TakeDamage(99999);
                break;
        }
        
    }
}