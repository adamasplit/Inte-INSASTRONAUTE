using UnityEngine;

public class FortifiedMomentumRelic : Relic
{
    public FortifiedMomentumRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Élan victorieux";
        description = "La première fois que vous brisez l'Armure d'un ennemi, gagnez 1 de Force et 1 de Dextérité.";
    }
    private bool triggered = false;
    public override void OnCombatStart(Character player)
    {
        triggered = false;
    }
    public override void OnTargetArmorBroken(Character source, Character target)
    {
        if (source == null || target == null || !source.isPlayer || target.isPlayer|| triggered)
        {
            return;
        }

        triggered = true;
        source.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        source.AddStatus(StatusEffect.Factory(StatusType.Dexterity, 1, -1));
    }
}