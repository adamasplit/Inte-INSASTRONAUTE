using UnityEngine;

public class SafetyNetRelic : Relic
{
    private bool used;

    public SafetyNetRelic()
    {
        rarity = RelicRarity.Common;
        name = "Batterie de réserve";
        description = "Une fois par combat, si vos PV passent sous la moitié, regagnez 4 PV.";
    }

    public override void OnCombatStart(Character player)
    {
        used = false;
    }

    public override void OnDamageTaken(Character source, Character target, int amount)
    {
        if (target == null || !target.isPlayer || used || amount <= 0)
        {
            return;
        }

        if (target.currentHP > 0 && target.currentHP <= target.maxHP / 2)
        {
            used = true;
            target.Heal(4);
        }
    }
}