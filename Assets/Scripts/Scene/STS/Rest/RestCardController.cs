using UnityEngine;
public class RestCardController : MonoBehaviour
{
    public CardView view;
    CardInstance card;
    RestManager manager;

    public void Init(CardInstance card, RestManager mgr)
    {
        this.card = card;
        manager = mgr;

        view.SetCard(card);
    }

    public void OnClick()
    {
        manager.OnCardSelected(card);
    }
}