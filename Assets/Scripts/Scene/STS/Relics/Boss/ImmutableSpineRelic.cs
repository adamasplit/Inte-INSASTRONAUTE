using UnityEngine;

public class ImmutableSpineRelic : Relic
{
    private bool blocked;

    public ImmutableSpineRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Apéro";
        description = "La première fois que vous devriez recevoir un debuff à chaque combat, il est annulé et vous récupérez 2 PV.";
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
            target.Heal(2);
            return false;
        }

        return true;
    }
}