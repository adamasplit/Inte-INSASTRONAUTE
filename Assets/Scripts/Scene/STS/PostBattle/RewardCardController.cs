using UnityEngine;
public class RewardCardController : MonoBehaviour
{
    CardView view;
    STSCardData data;
    CardInstance instance;
    RewardManager rewardManager;

    void Awake()
    {
        view = GetComponentInChildren<CardView>();
    }

    public void Init(STSCardData card, RewardManager manager)
    {
        data = card;
        rewardManager = manager;
        instance = new CardInstance(data);
        view.SetCard(instance);
    }

    public void OnClick()
    {
        rewardManager.OnCardSelected(data);
    }
}