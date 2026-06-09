using System.Collections.Generic;
public class AIRelic:BaseRelic
{
    // Personnage basé sur la manipulation de status (dispels, transfert, etc.)
    public AIRelic():base()
    {
        namesByStage[0] = "Intemporalité";
        descriptionsByStage[0] = "Jouer une Attaque étend les debuffs de la cible, et jouer une Compétence étend vos buffs.";
        Upgrade(0);
    }
    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card.data.type == CardType.Attaque)
        {
            foreach (var target in targets)
            {
                foreach (var status in target.statusEffects)
                {
                    if (status.debuff)
                    {
                        status.Extend(1);
                    }
                }
            }
        }
        else if (card.data.type == CardType.Compétence)
        {
            foreach (var status in player.statusEffects)
            {
                if (status.buff)
                {
                    status.Extend(1);
                }
            }
        }
    }
}