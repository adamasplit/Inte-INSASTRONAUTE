using UnityEngine;
using System.Collections.Generic;

public class CardDatabase : MonoBehaviour
{
    public List<CardData> cards;
    
    public static CardDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CardDatabase>();
                if (_instance == null)
                {
                    Debug.Log("[CardDatabase] Creating CardDatabase instance programmatically");
                    GameObject go = new GameObject("CardDatabase");
                    _instance = go.AddComponent<CardDatabase>();
                    DontDestroyOnLoad(go);
                    _instance.Init();
                }
            }
            return _instance;
        }
    }
    
    private static CardDatabase _instance;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
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
    
    public Color GetRarityColor(int rarity)
    {
        return rarity switch
        {
            0 => Color.white,
            1 => new Color(0.6f, 0.7f, 1f),
            2 => new Color(1f, 0.85f, 0.2f),
            3 => Color.magenta,
            4 => Color.red,
            5 => new Color(1f, 0.5f, 0f),
            _ => Color.white
        };
    }
    
    public CardData Get(string cardId)
    {
        if (_byId == null) Init();
        _byId.TryGetValue(cardId, out var card);
        return card;
    }
}
