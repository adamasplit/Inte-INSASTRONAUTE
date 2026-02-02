using UnityEngine;
using System.Collections.Generic;

public class CardDatabase : MonoBehaviour
{
    public List<CardData> cards;
    public static CardDatabase Instance;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }
    private Dictionary<string, CardData> _byId;
    public void Init()
    {
        cards = new List<CardData>(Resources.LoadAll<CardData>("Cards"));
        _byId = new Dictionary<string, CardData>();
        foreach (var card in cards)
        {
            if (card != null && !string.IsNullOrEmpty(card.cardId))
                _byId[card.cardId] = card;
        }
    }

    public CardData Get(string cardId)
    {
        if (_byId == null) Init();
        _byId.TryGetValue(cardId, out var card);
        return card;
    }
}
