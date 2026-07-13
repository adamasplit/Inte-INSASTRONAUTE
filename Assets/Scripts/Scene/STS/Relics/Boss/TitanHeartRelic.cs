using UnityEngine;

public class TitanHeartRelic : Relic
{
    public TitanHeartRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Ceinture d'astéroïdes";
        description = "Au début du combat, gagnez 20 d'Armure, 1 de Force et 1 de Dextérité, mais vous êtes Fragilisé pendant tout le combat.";
    }

    public override void OnCombatStart(Character player)
    {
        player.AddArmor(20);
        player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        player.AddStatus(StatusEffect.Factory(StatusType.Dexterity, 1, -1));
        StatusEffect status = StatusEffect.Factory(StatusType.Fragile, 0, 99);
        status.framed = true;
        status.goldFrame = true;
        player.AddStatus(status);
    }
}