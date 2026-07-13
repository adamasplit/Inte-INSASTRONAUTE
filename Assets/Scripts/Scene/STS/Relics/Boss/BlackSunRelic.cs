using UnityEngine;

public class BlackSunRelic : Relic
{
    public BlackSunRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Canicule";
        description = "Au début du combat, gagnez 3 de Force et 3 de Dextérité, mais aussi 1 Affaibli et 1 Vulnérable.";
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Strength, 3, -1));
        player.AddStatus(StatusEffect.Factory(StatusType.Dexterity, 3, -1));
        player.AddStatus(StatusEffect.Factory(StatusType.Weakness, 0, 1));
        player.AddStatus(StatusEffect.Factory(StatusType.Vuln, 0, 1));
    }
}