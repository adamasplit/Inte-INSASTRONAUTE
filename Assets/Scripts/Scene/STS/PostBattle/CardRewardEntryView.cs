using System.Collections.Generic;
using UnityEngine;
public class CardRewardEntryView : RewardEntryView
{
    public Transform cardsContainer;
    public GameObject cardPrefab;
    public GameObject cardPanel;

    CardReward reward;
    bool cardsLayoutFrozen;

    public override void Init(RewardItem rewardItem, IRewardFlowHost mgr)
    {
        base.Init(rewardItem, mgr);

        reward = rewardItem as CardReward;

        if (reward == null || reward.choices == null || reward.choices.Count == 0)
        {
            reward?.Claim();
            StartCoroutine(Collapse());
            return;
        }

        foreach (var card in reward.choices)
        {
            var obj = Instantiate(cardPrefab, cardsContainer);

            var ctrl = obj.GetComponent<RewardCardController>();
            ctrl.Init(card, this);
        }

        cardsLayoutFrozen = false;
        UILayoutHelper.RebuildAfterFrame(this, cardsContainer as RectTransform);
    }
    public void ToggleCardPanel(bool show)
    {
        cardPanel.SetActive(show);
    }

    public void SelectCard(CardInstance card, RewardCardController sourceController)
    {
        RunManager.Instance.deck.Add(card);

        reward.Claim();

        StartCoroutine(Collapse());
    }

    public void DisableCardsLayout()
    {
        UILayoutHelper.DisableLayoutHierarchy(cardsContainer as RectTransform);
    }

    public void FreezeCardsLayoutOnce()
    {
        if (cardsLayoutFrozen)
            return;

        cardsLayoutFrozen = true;
        UILayoutHelper.DisableLayoutLocal(cardsContainer as RectTransform);
    }
}