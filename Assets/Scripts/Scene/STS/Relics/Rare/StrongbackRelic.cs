using UnityEngine;

public class StrongbackRelic : Relic
{
    public StrongbackRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Dos solide";
        description = "À la fin de votre tour, si vous avez au moins 15 d'Armure, vous piocherez 1 carte et gagnez 1 de Dextérité au tour suivant.";
    }

    private bool triggered;
    public override void OnCombatStart(Character player)
    {
        triggered = false;
    }
    public override void OnTurnEnd(Character player)
    {
        if (player.armor >= 15)
        {
            triggered = true;
        }
    }

    public override void OnTurnStart(Character player)
    {
        if (triggered)
        {
            player.DrawCard();
            player.AddStatus(StatusEffect.Factory(StatusType.Dexterity, 1, -1));
            triggered = false;
        }
    }
}