using System.Collections;
using System.Collections.Generic;
public static class EventActionFactory
{
    public static System.Action GetAction(
        PanelOptionData option,
        EventManager manager)
    {
        switch (System.Enum.Parse<EventOptionType>(option.type))
        {
            case EventOptionType.Heal:
                return () =>
                {
                    RunManager.Instance.player.Heal(option.value);

                    manager.ReturnToMap();
                };

            case EventOptionType.UpgradeCard:
                return () =>
                {
                    DeckSelectionPanel.Instance.Open(
                        "Choose a card to upgrade",
                        option.value,
                        cards =>
                        {
                            foreach (var card in cards)
                                EnchantManager.ApplyEnchant(card, UnityEngine.Random.Range(1, 4)); // Example: apply a random enchantment with random level

                            manager.ReturnToMap();
                        });
                };

            case EventOptionType.RemoveCard:
                return () =>
                {
                    DeckSelectionPanel.Instance.Open(
                        "Choose a card to remove",
                        option.value,
                        cards =>
                        {
                            foreach (var card in cards)
                                RunManager.Instance.deck.Remove(card);

                            manager.ReturnToMap();
                        });
                };
            case EventOptionType.Damage:
                return () =>
                {
                    RunManager.Instance.player.TakeDamage(option.value);

                    manager.ReturnToMap();
                };
            case EventOptionType.MaxHpGain:
                return () =>
                {
                    RunManager.Instance.player.GainMaxHP(option.value);

                    manager.ReturnToMap();
                };
            case EventOptionType.MaxHpLoss:
                return () =>
                {
                    RunManager.Instance.player.LoseMaxHP(option.value);

                    manager.ReturnToMap();
                };

            default:
                return () =>
                {
                    manager.ReturnToMap();
                };
        }
    }
}