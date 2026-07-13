using System.Collections.Generic;
using System.Linq;

public class PowerCompletionRelic : Relic
{
    private int totalPowersAtCombatStart;
    private int powersPlayedThisCombat;
    private bool triggered;

    public PowerCompletionRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Dossier complet";
        description = "Pendant un combat, si vous jouez tous les Pouvoirs de votre deck, gagnez autant de Vitesse.";
    }

    public override void OnCombatStart(Character player)
    {
        powersPlayedThisCombat = 0;
        triggered = false;
        totalPowersAtCombatStart = 0;

        DeckManager deck = player.GetCombatManager().deck;
        if (deck != null)
        {
            totalPowersAtCombatStart = deck.drawPile.Count(c => c != null && c.data != null && c.data.type == CardType.Pouvoir)
                + deck.hand.Count(c => c != null && c.data != null && c.data.type == CardType.Pouvoir)
                + deck.discardPile.Count(c => c != null && c.data != null && c.data.type == CardType.Pouvoir)
                + deck.exhaustPile.Count(c => c != null && c.data != null && c.data.type == CardType.Pouvoir);
        }
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (triggered || totalPowersAtCombatStart <= 0 || card == null || card.data == null || card.data.type != CardType.Pouvoir)
        {
            return;
        }

        powersPlayedThisCombat++;
        if (powersPlayedThisCombat >= totalPowersAtCombatStart)
        {
            triggered = true;
            player.AddStatus(StatusEffect.Factory(StatusType.Speed, totalPowersAtCombatStart, -1));
        }
    }
}