public static class EventActionFactory
{
    public static System.Action GetAction(
        PanelOptionData option,
        EventManager manager)
    {
        return () =>
        {
            manager.HideEventPanel();
            ExecuteEntry(0, option, manager);
            manager.description.text = option.completionMessage; // Display the completion message if provided
        };
    }

    private static void ExecuteEntry(int index, PanelOptionData option, EventManager manager)
    {
        if (option == null || option.entries == null || index >= option.entries.Count)
        {
            manager.ShowEventContinue(manager.ReturnToMap);
            return;
        }

        var entry = option.entries[index];

        switch (System.Enum.Parse<EventOptionType>(entry.type))
        {
            case EventOptionType.CardReward:
            {
                Reward reward = new Reward();
                reward.items.Add(RewardGenerator.GenerateCardReward());
                manager.PresentReward(reward, () => ExecuteEntry(index + 1, option, manager));
                break;
            }

            case EventOptionType.RelicReward:
            {
                Reward reward = new Reward();
                reward.items.Add(RewardGenerator.GenerateRelicReward());
                manager.PresentReward(reward, () => ExecuteEntry(index + 1, option, manager));
                break;
            }

            case EventOptionType.GoldReward:
                RunManager.Instance.gold += entry.value;
                ExecuteEntry(index + 1, option, manager);
                break;

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

            case EventOptionType.TransformCard:
                DeckSelectionPanel.Instance.Open(
                    "Choose cards to transform",
                    entry.value,
                    cards =>
                    {
                        foreach (var card in cards)
                        {
                            TransformCard(card, STSCardDatabase.GetRandomCard(RunManager.Instance.selectedCharacter));
                        }

                        ExecuteEntry(index + 1, option, manager);
                    });
                break;

            case EventOptionType.AddCard:
                for (int i = 0; i < entry.value; i++)
                {
                    STSCardData cardData = string.IsNullOrEmpty(entry.id)
                        ? STSCardDatabase.GetRandomCard(RunManager.Instance.selectedCharacter)
                        : STSCardDatabase.Get(entry.id);

                    if (cardData != null)
                    {
                        RunManager.Instance.deck.Add(new CardInstance(cardData));
                    }
                }

                ExecuteEntry(index + 1, option, manager);
                break;

            default:
                ExecuteEntry(index + 1, option, manager);
                break;
        }
    }

    private static void TransformCard(CardInstance oldCard, STSCardData newCardData)
    {
        if (oldCard == null || newCardData == null)
        {
            return;
        }

        CardInstance newCard = new CardInstance(newCardData);
        oldCard.data = newCard.data;
        oldCard.targetingMode = newCard.targetingMode;
        oldCard.baseModifiers.Clear();
        oldCard.addedModifiers.Clear();
        oldCard.enchantments.Clear();
        oldCard.addedEffects.Clear();
    }
}