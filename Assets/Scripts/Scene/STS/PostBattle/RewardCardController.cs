using UnityEngine;
public class RewardCardController : MonoBehaviour
{
    public CardView view;
    CardInstance instance;
    CardRewardEntryView rewardManager;
    private bool chosen = false;
    void Awake()
    {
        if (view == null)
        {
            view = GetComponentInChildren<CardView>();
        }
    }

    public void Init(CardInstance card, CardRewardEntryView manager)
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
            rewardManager.SelectCard(instance);
        }
    }
}