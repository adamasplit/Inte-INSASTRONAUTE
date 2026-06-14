using System.Collections.Generic;
using UnityEngine;
public class CardRewardEntryView : RewardEntryView
{
    public Transform cardsContainer;
    public GameObject cardPrefab;
    public GameObject cardPanel;

    CardReward reward;

    public override void Init(RewardItem rewardItem, IRewardFlowHost mgr)
    {
        base.Init(rewardItem, mgr);

        reward = rewardItem as CardReward;
        foreach (var card in reward.choices)
        {
            var obj = Instantiate(cardPrefab, cardsContainer);

            var ctrl = obj.GetComponent<RewardCardController>();
            ctrl.Init(card, this);
        }
    }
    public void ToggleCardPanel(bool show)
    {
        cardPanel.SetActive(show);
    }

    public void SelectCard(CardInstance card)
    {
        RunManager.Instance.deck.Add(card);

        reward.Claim();

        StartCoroutine(Collapse());
    }
}