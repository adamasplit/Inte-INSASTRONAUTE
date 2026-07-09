using UnityEngine;

public class ShatterPulseRelic : Relic
{
    public ShatterPulseRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Réactions en chaîne";
        description = "Lorsque l'Armure d'un ennemi se brise, infligez 3 dégâts aux autres ennemis.";
    }

    public override void OnTargetArmorBroken(Character source, Character target)
    {
        if (source == null || target == null || !source.isPlayer || target.isPlayer)
        {
            return;
        }

        var combat = source.GetCombatManager();
        foreach (var enemy in combat.enemies)
        {
            if (enemy != null && enemy.IsAlive && enemy != target)
            {
                enemy.TakeDamage(3);
            }
        }
    }
}