using UnityEngine;

public class ReinforcedBuckleRelic : Relic
{
    private bool triggered;

    public ReinforcedBuckleRelic()
    {
        rarity = RelicRarity.Common;
        name = "Gants de protection";
        description = "La première fois par combat que vous gagnez de l'Armure, gagnez 5 d'Armure supplémentaire.";
    }

    public override void OnCombatStart(Character player)
    {
        triggered = false;
    }

    public override void OnAnyArmorGain(Character target, int amount)
    {
        if (triggered || target == null || !target.isPlayer || amount <= 0)
        {
            return;
        }

        triggered = true;
        target.AddArmor(5);
    }
}