using UnityEngine;

public class RunicAegisRelic : Relic
{
    public RunicAegisRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Égide runique";
        description = "Au début du combat, gagnez 15 d'Armure et 1 d'Artéfact. Vous commencez le combat avec 1 énergie en moins.";
    }
    private bool blocked = false;
    private bool armorGranted = false;
    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Artifact, 1, -1));
        blocked = true;
        armorGranted = false;
    }

    public override void OnTurnStart(Character player)
    {
        // Grant here (not OnCombatStart) so it survives the first turn's armor reset.
        if (armorGranted)
        {
            return;
        }

        armorGranted = true;
        player.AddArmor(15);
    }

    public override int EnergyOnTurnStart(int previousEnergy, Character character)
    {
        if (blocked)
        {
            blocked = false;
            return -1;
        }
        return 0;
    }
}