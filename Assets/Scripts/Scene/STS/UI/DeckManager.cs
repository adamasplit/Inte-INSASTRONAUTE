using System.Collections.Generic;
using UnityEngine;
public class DeckManager
{
    public List<CardInstance> drawPile = new();
    public List<CardInstance> hand = new();
    public List<CardInstance> discardPile = new();
    public List<CardInstance> exhaustPile = new();

    public void Draw(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0)
                Reshuffle();

            if (drawPile.Count == 0)
                return;

            hand.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }
    }

    public void DiscardHand()
    {
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            if (hand[i].data.retain)
                continue;
            discardPile.Add(hand[i]);
            hand.RemoveAt(i);
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
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}