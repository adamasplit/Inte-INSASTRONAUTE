using UnityEngine;
public class EPRelic:BaseRelic
{
    public EPRelic():base()
    {
        namesByStage[0] = "Moteur spatial";
        namesByStage[1] = "Moteur spatial - Hyperpropulseur";
        namesByStage[2] = "Moteur spatial - Énergétique";
        descriptionsByStage[0] = "Au début de chaque tour, gagnez 1 d'énergie et piochez une carte.";
        descriptionsByStage[1] = "Au début de chaque tour, gagnez 3 d'énergie et piochez une carte";
        descriptionsByStage[2] = "Vous conservez votre énergie d'un tour à l'autre et piochez une carte au début de chaque tour.";
        rarity=RelicRarity.Base;
        Upgrade(0);
    }
    public override int EnergyOnTurnStart(int previousEnergy,Character character)
    {
        switch (stage)
        {
            case 0:
                return  1;
            case 1:
                return 3;
            case 2:
                return previousEnergy;
            default:
                return 0;
        }
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        VFXManager.Instance.PlayEffect("EPRelicActivate", player);
        player.GetCombatManager().deck.Draw(1);
    }
}