using UnityEngine;

public class MirrorShellRelic : Relic
{
    private bool used;

    public MirrorShellRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Carapace miroir";
        description = "La première fois que vous subissez des dégâts à chaque combat, renvoyez-en la moitié.";
    }

    public override void OnCombatStart(Character player)
    {
        used = false;
    }

    public override void OnDamageTaken(Character source, Character target, int amount)
    {
        if (used || target == null || !target.isPlayer || amount <= 0 || source == null)
        {
            return;
        }

        used = true;
        source.TakeDamage(Mathf.Max(1, amount / 2));
    }
}