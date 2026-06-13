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
    public event Action<CardInstance> OnCardAddedToHand;
    public void AddToHand(CardInstance card)
    {
        hand.Add(card);
        OnCardAddedToHand?.Invoke(card);
    }

    public void Draw(int amount = 1, bool firstTurn=false)
    {
        for (int i = 0; i < amount; i++)
        {
            if (hand.Count >= 10)
            {
                Debug.Log("Hand is full, cannot draw more cards.");
                return;
            }
            if (drawPile.Count == 0)
                Reshuffle();

            if (drawPile.Count == 0)
            {
                if (hand.Count<=1)
                {
                    AddCardToHand("Désespoir");
                }
                return;
            }
            if (firstTurn)
            {
                //Draw innate cards first
                CardInstance innateCard = drawPile.Find(c => c.data.innate);
                if (innateCard != null)
                {
                    drawPile.Remove(innateCard);
                    hand.Add(innateCard);
                    OnCardDrawn?.Invoke(innateCard);
                    continue;
                }
            }
            CardInstance card = drawPile[0];

            drawPile.RemoveAt(0);

            hand.Add(card);

            OnCardDrawn?.Invoke(card);
        }
    }
    public void Discard()
    {
        if (hand.Count == 0)
            return;
        int index = UnityEngine.Random.Range(0, hand.Count);
        CardInstance card = hand[index];
        hand.RemoveAt(index);
        discardPile.Add(card);
        OnCardDiscarded?.Invoke(card);
    }
    public void Discard(CardInstance card)
    {
        if (hand.Contains(card))
        {
            hand.Remove(card);
            discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
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

    public void Delete(CardInstance card)
    {
        //Just remove the card from all piles without sending it to discard or exhaust
        if (hand.Contains(card))
        {
            OnCardExhausted?.Invoke(card);
        }
        hand.Remove(card);
        drawPile.Remove(card);
        discardPile.Remove(card);
        exhaustPile.Remove(card);
    }

    public void DiscardHand()
    {
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            CardInstance card = hand[i];

            if (card.HasEnchantment("Mécanique"))
            {
                Debug.Log("Card has Mécanique enchantment, playing instead of discarding.");
                combatManager.PlayCard(combatManager.player, card, new List<Character> { combatManager.enemies[UnityEngine.Random.Range(0, combatManager.enemies.Count)] },true);
                continue;
            }
            if (card.data.retain)
                continue;

            hand.RemoveAt(i);

            discardPile.Add(card);

            OnCardDiscarded?.Invoke(card);
        }
    }
    public void AddCardToHand(string cardID)
    {
        STSCardData cardToAdd = STSCardDatabase.Get(cardID);
        if (cardToAdd != null)
        {
            CardInstance instance = new CardInstance(cardToAdd);
            hand.Add(instance);
            OnCardAddedToHand?.Invoke(instance);
        }
    }
    public CardInstance GetAndRemoveTopCard()
    {
        if (drawPile.Count == 0)
            Reshuffle();

        if (drawPile.Count == 0)
            return null;

        CardInstance card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }

    void Reshuffle()
    {
        drawPile.AddRange(discardPile);
        //Also add cards with Mending enchantment from exhaust pile back to draw pile
        List<CardInstance> mendingCards = exhaustPile.FindAll(c => c.HasEnchantment("Mending"));
        drawPile.AddRange(mendingCards);
        foreach (var card in mendingCards)
        {
            exhaustPile.Remove(card);
        }
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