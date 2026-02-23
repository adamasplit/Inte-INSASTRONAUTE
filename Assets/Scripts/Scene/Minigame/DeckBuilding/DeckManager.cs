using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    public int maxDeckSize = 12;
    public List<CardData> deck = new List<CardData>();

    public void Init()
    {
        Debug.Log("Forcing DeckManager initialization");
        if (Instance == null)
            Instance = this;
        if (Instance == null)
            Debug.LogError("Failed to initialize DeckManager instance");
        LoadDeck();
    }

    public void TryAddCard(CardData card)
    {
        Debug.Log($"[DeckManager] Trying to add card {card.cardId} to deck...");
        if (deck.Count >= maxDeckSize)
            return;

        int owned = PlayerProfileStore.GetCardQuantity(card.cardId);
        int alreadyInDeck = CountInDeck(card);

        if (alreadyInDeck >= owned)
            return;

        deck.Add(card);
        DeckBuilderUI.Instance?.Refresh();
        return ;
    }

    public void RemoveCard(CardData card)
    {
        Debug.Log($"[DeckManager] Trying to remove card {card.cardId} from deck...");
        if (deck.Remove(card))
            DeckBuilderUI.Instance?.Refresh();
    }

    public int CountInDeck(CardData card)
    {
        int count = 0;
        foreach (var c in deck)
            if (c == card)
                count++;
        return count;
    }

    public bool IsDeckValid()
    {
        return deck.Count == maxDeckSize;
    }

    public void SaveDeck()
    {
        List<string> ids = new List<string>();

        foreach (var card in deck)
            ids.Add(card.cardId);

        string json = JsonUtility.ToJson(new DeckSaveData { cards = ids });
        PlayerPrefs.SetString("PLAYER_DECK", json);
        PlayerPrefs.Save();
    }

    public void LoadDeck()
    {
        deck.Clear();

        if (!PlayerPrefs.HasKey("PLAYER_DECK"))
            return;

        string json = PlayerPrefs.GetString("PLAYER_DECK");
        DeckSaveData data = JsonUtility.FromJson<DeckSaveData>(json);

        foreach (string id in data.cards)
        {
            CardData card = CardDatabase.Instance.Get(id);
            if (card != null)
                deck.Add(card);
        }
    }

    [System.Serializable]
    private class DeckSaveData
    {
        public List<string> cards;
    }
}