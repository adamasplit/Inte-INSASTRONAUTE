using System.Collections.Generic;

public class NineSkillsBatteryRelic : Relic
{
    private int skillCount;

    public NineSkillsBatteryRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Batterie méthodique";
        description = "Toutes les 9 Compétences jouées, gagnez 1 énergie.";
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card == null || card.data == null || card.data.type != CardType.Compétence)
        {
            return;
        }

        skillCount++;
        if (skillCount % 9 == 0)
        {
            player.GainEnergy(1);
        }
    }
}