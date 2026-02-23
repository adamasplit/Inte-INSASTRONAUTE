[System.Serializable]
public class CardStack
{
    public CardData card;
    public int count;

    public CardStack(CardData card, int count)
    {
        this.card = card;
        this.count = count;
    }
}