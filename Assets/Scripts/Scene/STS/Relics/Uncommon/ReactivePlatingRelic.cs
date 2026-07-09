using UnityEngine;

public class ReactivePlatingRelic : Relic
{
    private bool used;

    public ReactivePlatingRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Blindage réactif";
        description = "La première fois que vous subissez des dégâts à chaque combat, gagnez 5 d'Armure.";
    }

    public override void OnCombatStart(Character player)
    {
        used = false;
    }

    public override void OnDamageTaken(Character source, Character target, int amount)
    {
        if (used || target == null || !target.isPlayer || amount <= 0)
        {
            return;
        }

        used = true;
        target.AddArmor(5);
    }
}