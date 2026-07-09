using UnityEngine;

public class ImmutableSpineRelic : Relic
{
    private bool blocked;

    public ImmutableSpineRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Colonne immuable";
        description = "La première fois que vous devriez recevoir un debuff à chaque combat, il est annulé.";
    }

    public override void OnCombatStart(Character player)
    {
        blocked = false;
    }

    public override bool CanApplyStatus(StatusEffect status, Character target)
    {
        if (!blocked && target != null && target.isPlayer && status.debuff)
        {
            blocked = true;
            return false;
        }

        return true;
    }
}