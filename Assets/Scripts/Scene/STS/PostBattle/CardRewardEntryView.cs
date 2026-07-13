using System.Collections.Generic;
using System.Collections;
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
        StartCoroutine(SelectCardRoutine(card, sourceController));
    }

    private IEnumerator SelectCardRoutine(CardInstance card, RewardCardController sourceController)
    {
        RunManager.Instance.deck.Add(card);

        reward.Claim();

        if (sourceController != null && sourceController.view != null)
        {
            Canvas canvas = sourceController.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

            if (canvasRect != null)
            {
                sourceController.view.HideCardTooltips();

                Vector2 startScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 endScreenPosition = new Vector2(Screen.width - 64f, Screen.height - 64f);

                GameObject animatedCardObject = new GameObject("RewardSelectCardClone", typeof(RectTransform), typeof(CanvasGroup));
                animatedCardObject.transform.SetParent(canvasRect, false);
                animatedCardObject.transform.SetAsLastSibling();

                RectTransform animatedRoot = animatedCardObject.GetComponent<RectTransform>();
                animatedRoot.anchorMin = new Vector2(0.5f, 0.5f);
                animatedRoot.anchorMax = new Vector2(0.5f, 0.5f);
                animatedRoot.pivot = new Vector2(0.5f, 0.5f);
                animatedRoot.sizeDelta = new Vector2(200f, 300f);

                GameObject animatedCardViewObject = Instantiate(sourceController.view.gameObject, animatedRoot);
                animatedCardViewObject.transform.SetAsLastSibling();
                RectTransform animatedCardViewRect = animatedCardViewObject.GetComponent<RectTransform>();
                if (animatedCardViewRect != null)
                {
                    animatedCardViewRect.anchorMin = Vector2.zero;
                    animatedCardViewRect.anchorMax = Vector2.one;
                    animatedCardViewRect.offsetMin = Vector2.zero;
                    animatedCardViewRect.offsetMax = Vector2.zero;
                    animatedCardViewRect.localScale = Vector3.one;
                }

                CardView animatedCardView = animatedCardViewObject.GetComponent<CardView>();
                if (animatedCardView != null)
                {
                    animatedCardView.SetCard(card);
                }

                CanvasGroup animatedGroup = animatedCardObject.GetComponent<CanvasGroup>();
                if (animatedGroup != null)
                {
                    animatedGroup.alpha = 1f;
                }

                sourceController.SetVisualVisible(false);

                yield return StartCoroutine(sourceController.PlayRewardSelectionAnimation(0.1f, 0.5f, startScreenPosition, endScreenPosition, animatedRoot));
                Destroy(animatedCardObject);
            }
            else
            {
                sourceController.SetVisualVisible(false);
            }
        }

        yield return StartCoroutine(Collapse());
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