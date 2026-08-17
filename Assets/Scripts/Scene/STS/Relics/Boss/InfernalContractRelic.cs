using UnityEngine;

public class InfernalContractRelic : Relic
{
    private bool triggered;

    public InfernalContractRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Contrat d'études";
        description = "Au début du combat, gagnez 2 énergie et piochez 2 cartes. Vous commencez aussi avec 4 Poison.";
    }

    public override void OnCombatStart(Character player)
    {
        triggered = false;
        player.AddStatus(StatusEffect.Factory(StatusType.Poison, 6, 6));
    }

    public override void OnTurnStart(Character player)
    {
        // Grant here (not OnCombatStart) so it survives the first turn's energy/armor reset.
        if (triggered)
        {
            return;
        }

        triggered = true;
        player.GainEnergy(2);
        player.DrawCard();
        player.DrawCard();
    }
}