using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class STSPlayerProfileStore
{
    private const string UnlockPrefix = "STS.PlayerProfile.UnlockedCards.";

    public static List<STSCardData> UnlockCardsFromDeck(List<CardInstance> deck, SelectableCharacter selectedCharacter, bool wasRetreat, int act)
    {
        List<STSCardData> unlocked = new();

        if (deck == null || deck.Count == 0)
        {
            return unlocked;
        }

        if (selectedCharacter == SelectableCharacter.Aucun
            || selectedCharacter == SelectableCharacter.Starting
            || selectedCharacter == SelectableCharacter.Impossible)
        {
            return unlocked;
        }

        List<STSCardData> candidates = new();
        foreach (CardInstance card in deck)
        {
            if (card == null || card.data == null)
            {
                continue;
            }

            if (card.data.favoredCharacter != selectedCharacter)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(card.data.id))
            {
                continue;
            }

            if (HasUnlockedCard(card.data.id, selectedCharacter))
            {
                continue;
            }

            candidates.Add(card.data);
        }

        if (candidates.Count == 0)
        {
            return unlocked;
        }

        int unlockCount = RunEndUnlockCount(wasRetreat, act);
        int countToUnlock = Mathf.Min(unlockCount, candidates.Count);

        HashSet<string> toUnlock = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < countToUnlock; i++)
        {
            toUnlock.Add(candidates[i].id);
            unlocked.Add(candidates[i]);
        }

        if (toUnlock.Count == 0)
        {
            return unlocked;
        }

        HashSet<string> current = LoadUnlockedCardIds(selectedCharacter);
        foreach (string cardId in toUnlock)
        {
            current.Add(cardId);
        }

        SaveUnlockedCardIds(selectedCharacter, current);
        Debug.Log($"[STS-PROFILE] Unlocked {current.Count} cards for {selectedCharacter} after run end (retreat={wasRetreat}, act={act}).");
        return unlocked;
    }

    // Mirrors the server-authoritative formula in StsPvpService so the client preview matches what actually gets granted.
    // Retreat: 0/1/3/6/... unlocks for 0/1/2/3/... bosses defeated. Game over: 1 unlock if any boss was defeated, else 0.
    private static int RunEndUnlockCount(bool wasRetreat, int act)
    {
        int normalizedAct = Mathf.Max(0, act);
        if (wasRetreat)
        {
            int bossesDefeated = normalizedAct;
            return bossesDefeated * (bossesDefeated + 1) / 2;
        }

        return normalizedAct > 0 ? 1 : 0;
    }

    public static bool HasUnlockedCard(string cardId, SelectableCharacter selectedCharacter)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        return LoadUnlockedCardIds(selectedCharacter).Contains(cardId);
    }

    public static List<string> GetUnlockedCardIds(SelectableCharacter selectedCharacter)
    {
        return LoadUnlockedCardIds(selectedCharacter).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static HashSet<string> LoadUnlockedCardIds(SelectableCharacter selectedCharacter)
    {
        string key = BuildKey(selectedCharacter);
        string raw = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (string entry in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!string.IsNullOrWhiteSpace(entry))
            {
                ids.Add(entry.Trim());
            }
        }

        return ids;
    }

    private static void SaveUnlockedCardIds(SelectableCharacter selectedCharacter, HashSet<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            PlayerPrefs.DeleteKey(BuildKey(selectedCharacter));
            return;
        }

        string serialized = string.Join("|", ids.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
        PlayerPrefs.SetString(BuildKey(selectedCharacter), serialized);
        PlayerPrefs.Save();
    }

    private static string BuildKey(SelectableCharacter selectedCharacter)
    {
        return UnlockPrefix + selectedCharacter.ToString();
    }
}
