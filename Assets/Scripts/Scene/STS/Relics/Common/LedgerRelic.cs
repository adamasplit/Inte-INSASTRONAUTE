using UnityEngine;

public class LedgerRelic : Relic
{
    private bool triggered;

    public LedgerRelic()
    {
        rarity = RelicRarity.Common;
        name = "Registre";
        description = "Piochez 1 carte au début de votre premier tour.";
    }

    public override void OnCombatStart(Character player)
    {
        triggered = false;
    }

    public override void OnTurnStart(Character player)
    {
        if (triggered)
        {
            return;
        }

        triggered = true;
        player.DrawCard();
    }
}