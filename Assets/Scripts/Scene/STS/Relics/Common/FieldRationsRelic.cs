using UnityEngine;

public class FieldRationsRelic : Relic
{
    public FieldRationsRelic()
    {
        rarity = RelicRarity.Common;
        name = "Rations de campagne";
        description = "Vous regagnez 4 PV lorsque vous entrez dans un site de repos.";
    }

    public override void OnEnterRestSite(Character player)
    {
        player.Heal(4);
    }
}