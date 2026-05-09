using System.Collections.Generic;
using UnityEngine;
using System;
public class DeckManager
{
    public List<CardInstance> drawPile = new();
    public List<CardInstance> hand = new();
    public List<CardInstance> discardPile = new();
    public List<CardInstance> exhaustPile = new();
    public CombatManager combatManager;
    public event Action<CardInstance> OnCardDrawn;
    public event Action<CardInstance> OnCardDiscarded;
    public event Action<CardInstance> OnCardExhausted;
    public event Action<CardInstance> OnCardPlayed;
    public void AddToHand(CardInstance card)
    {
        hand.Add(card);
    }

    public void Draw(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0)
                Reshuffle();

            if (drawPile.Count == 0)
                return;

            CardInstance card = drawPile[0];

            drawPile.RemoveAt(0);

            hand.Add(card);

            OnCardDrawn?.Invoke(card);
        }
    }

    public void RemoveFromHand(CardInstance card)
    {
        hand.Remove(card);
    }

    public void SendToDiscard(CardInstance card)
    {
        discardPile.Add(card);
    }

    public void Exhaust(CardInstance card)
    {
        exhaustPile.Add(card);
    }

    public void DiscardHand()
    {
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            CardInstance card = hand[i];

            if (card.data.retain)
                continue;

            hand.RemoveAt(i);

            discardPile.Add(card);

            OnCardDiscarded?.Invoke(card);
        }
    }

    void Reshuffle()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }

    public void Shuffle(List<CardInstance> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}