using UnityEngine;

public class TitanHeartRelic : Relic
{
    private bool triggered;

    public TitanHeartRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Ceinture d'astéroïdes";
        description = "Au début du combat, gagnez 20 d'Armure, 1 de Force et 1 de Dextérité, mais vous êtes Fragilisé pendant tout le combat.";
    }

    public override void OnCombatStart(Character player)
    {
        triggered = false;
        player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        player.AddStatus(StatusEffect.Factory(StatusType.Dexterity, 1, -1));
        StatusEffect status = StatusEffect.Factory(StatusType.Fragile, 0, 99);
        status.framed = true;
        status.goldFrame = true;
        player.AddStatus(status);
    }

    public override void OnTurnStart(Character player)
    {
        // Grant here (not OnCombatStart) so it survives the first turn's armor reset.
        if (triggered)
        {
            return;
        }

        triggered = true;
        player.AddArmor(20);
    }
}