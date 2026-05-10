using UnityEngine;
public class RewardCardController : MonoBehaviour
{
    CardView view;
    CardInstance instance;
    RewardManager rewardManager;
    private bool chosen = false;
    void Awake()
    {
        view = GetComponentInChildren<CardView>();
    }

    public void Init(CardInstance card, RewardManager manager)
    {
        rewardManager = manager;
        instance = card;
        view.SetCard(instance);
        view.enabled=false;
    }

    public void OnClick()
    {
        if (!chosen)
        {
            chosen = true;
            rewardManager.OnCardSelected(instance,this);
        }
    }
}