public static class EventActionFactory
{
    public static System.Action GetAction(
        PanelOptionData option,
        EventManager manager)
    {
        return () => ExecuteEntry(0, option, manager);
    }

    private static void ExecuteEntry(int index, PanelOptionData option, EventManager manager)
    {
        if (option == null || option.entries == null || index >= option.entries.Count)
        {
            manager.ReturnToMap();
            return;
        }

        var entry = option.entries[index];

        switch (System.Enum.Parse<EventOptionType>(entry.type))
        {
            case EventOptionType.Heal:
                RunManager.Instance.player.Heal(entry.value);
                ExecuteEntry(index + 1, option, manager);
                break;

            case EventOptionType.UpgradeCard:
                DeckSelectionPanel.Instance.Open(
                    "Choose a card to upgrade",
                    entry.value,
                    cards =>
                    {
                        foreach (var card in cards)
                            EnchantManager.ApplyEnchant(card, UnityEngine.Random.Range(1, 4)); // Example: apply a random enchantment with random level

                        ExecuteEntry(index + 1, option, manager);
                    });
                break;

            case EventOptionType.RemoveCard:
                DeckSelectionPanel.Instance.Open(
                    "Choose a card to remove",
                    entry.value,
                    cards =>
                    {
                        foreach (var card in cards)
                            RunManager.Instance.deck.Remove(card);

                        ExecuteEntry(index + 1, option, manager);
                    });
                break;

            case EventOptionType.Damage:
                RunManager.Instance.player.TakeDamage(entry.value);
                ExecuteEntry(index + 1, option, manager);
                break;

            case EventOptionType.MaxHpGain:
                RunManager.Instance.player.GainMaxHP(entry.value);
                ExecuteEntry(index + 1, option, manager);
                break;

            case EventOptionType.MaxHpLoss:
                RunManager.Instance.player.LoseMaxHP(entry.value);
                ExecuteEntry(index + 1, option, manager);
                break;

            default:
                ExecuteEntry(index + 1, option, manager);
                break;
        }
    }
}