using UnityEngine;

public class LastStandRelic : Relic
{
    public LastStandRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Mur cassé";
        description = "Au début de votre tour, si vous avez moins de la moitié de vos PV, gagnez 5 d'Armure et 1 de Force.";
    }

    public override void OnTurnStart(Character player)
    {
        if (player.currentHP < player.maxHP / 2)
        {
            player.AddArmor(5);
            player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        }
    }
}