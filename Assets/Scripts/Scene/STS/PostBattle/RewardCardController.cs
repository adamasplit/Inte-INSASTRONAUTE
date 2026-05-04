using UnityEngine;
public class RewardCardController : MonoBehaviour
{
    CardView view;
    STSCardData data;
    CardInstance instance;
    RewardManager rewardManager;
    private bool chosen = false;
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
        view.enabled=false;
    }

    public void OnClick()
    {
        if (!chosen)
        {
            chosen = true;
            rewardManager.OnCardSelected(data,this);
        }
    }
}