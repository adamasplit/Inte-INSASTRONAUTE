using UnityEngine;

public class FrenziedIdolRelic : Relic
{
    public FrenziedIdolRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Courroie rouillée";
        description = "Quand vous subissez des dégâts, gagnez 1 de Force. Vous commencez chaque combat avec 6 Brûlure.";
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Burn, 0, 6));
    }

    public override void OnDamageTaken(Character source, Character target, int amount)
    {
        if (target != null && target.isPlayer && amount > 0)
        {
            target.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        }
    }
}