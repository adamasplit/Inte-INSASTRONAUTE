using UnityEngine;

public class MeditativeFocusRelic : Relic
{
    private bool used;

    public MeditativeFocusRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Eco-cup";
        description = "La première fois que vous êtes soigné à chaque combat, gagnez 2 d'énergie.";
    }

    public override void OnCombatStart(Character player)
    {
        used = false;
    }

    public override int OnHeal(Character target, int amount)
    {
        if (!used && target != null && target.isPlayer && amount > 0)
        {
            used = true;
            target.GainEnergy(2);
        }

        return amount;
    }
}