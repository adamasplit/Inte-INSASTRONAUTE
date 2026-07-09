using System.Collections.Generic;
public class AIRelic:BaseRelic
{
    // Personnage basé sur la manipulation de status (dispels, transfert, etc.)
    public AIRelic():base()
    {
        namesByStage[0] = "Intemporalité";
        descriptionsByStage[0] = "Les effets appliqués durent 2 tours de plus.";
        Upgrade(0);
    }
    public override bool CanApplyStatus(StatusEffect status,Character target)
    {
        if (target.isPlayer&&status.buff)
        {
            status.Extend(2); 
        }
        else if (!target.isPlayer&&status.debuff)
        {
            status.Extend(2); // Les effets appliqués sur les ennemis durent 2 tours de plus
        }
        return true;
    }
}