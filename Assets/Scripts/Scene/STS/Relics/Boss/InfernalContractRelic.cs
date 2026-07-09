using UnityEngine;

public class InfernalContractRelic : Relic
{
    public InfernalContractRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Contrat d'études";
        description = "Au début du combat, gagnez 2 énergie et piochez 2 cartes. Vous commencez aussi avec 4 Poison.";
    }

    public override void OnCombatStart(Character player)
    {
        player.GainEnergy(2);
        player.DrawCard();
        player.DrawCard();
        player.AddStatus(StatusEffect.Factory(StatusType.Poison, 6, 6));
    }
}