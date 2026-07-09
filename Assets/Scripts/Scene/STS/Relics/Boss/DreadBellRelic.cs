using UnityEngine;

public class DreadBellRelic : Relic
{
    public DreadBellRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Glas de la cathédrale";
        description = "Quand vous infligez des dégâts à un ennemi, gagnez 1 de Force. Vous commencez chaque combat avec 4 Affaibli indissipable.";
    }

    public override void OnCombatStart(Character player)
    {
        StatusEffect status=StatusEffect.Factory(StatusType.Weakness, 0, 4);
        status.framed=true;
        status.goldFrame=true;
        player.AddStatus(status);
    }

    public override void OnDamageDealt(Character source, Character target, int amount)
    {
        if (source != null && source.isPlayer && target != null && !target.isPlayer && amount > 0)
        {
            source.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        }
    }
}