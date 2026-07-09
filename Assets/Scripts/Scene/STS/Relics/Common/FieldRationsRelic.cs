using UnityEngine;

public class FieldRationsRelic : Relic
{
    public FieldRationsRelic()
    {
        rarity = RelicRarity.Common;
        name = "Rations de campagne";
        description = "Vous soigne de 6 PV lorsque vous entrez dans un site de repos.";
    }

    public override void OnEnterRestSite(Character player)
    {
        player.Heal(6);
    }
}