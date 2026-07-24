using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class CardRewardEntryView : RewardEntryView
{
    public Transform cardsContainer;
    public GameObject cardPrefab;
    public GameObject cardPanel;
    public RectTransform spawnOrigin;
    private float holdDuration = 0.3f;
    private float exitDuration = 0.8f;
    private float spawnDuration = 0.42f;

    CardReward reward;
    bool cardsLayoutFrozen;
    bool spawnAnimationStarted;
    private readonly List<RewardCardController> controllers = new();
    private RewardCardController selectedController;
    private bool selectionLocked;

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

        controllers.Clear();
        selectedController = null;
        selectionLocked = false;
        spawnAnimationStarted = false;

        foreach (var card in reward.choices)
        {
            var obj = Instantiate(cardPrefab, cardsContainer);

            var ctrl = obj.GetComponent<RewardCardController>();
            ctrl.Init(card, this);
            controllers.Add(ctrl);
        }

        cardsLayoutFrozen = false;
    }
    public void ToggleCardPanel(bool show)
    {
        if (cardPanel != null)
            cardPanel.SetActive(show);

        if (show && !spawnAnimationStarted)
        {
            spawnAnimationStarted = true;
            StartCoroutine(AnimateSpawnAfterLayout());
        }
    }

    public void SelectCard(CardInstance card, RewardCardController sourceController)
    {
        if (selectionLocked && sourceController != selectedController)
            return;

        selectionLocked = true;
        selectedController = sourceController;

        foreach (var controller in controllers)
        {
            if (controller != null)
                controller.SetSelectable(controller == sourceController);
        }

        StartCoroutine(SelectCardRoutine(card, sourceController));
    }

    private IEnumerator SelectCardRoutine(CardInstance card, RewardCardController sourceController)
    {
        if (manager != null)
        {
            string selectedCardId = card != null && card.data != null ? card.data.id : null;
            var claimTask = manager.TryClaimServerRewardAsync(reward, selectedCardId);
            while (!claimTask.IsCompleted)
            {
                yield return null;
            }

            if (claimTask.IsFaulted || claimTask.IsCanceled || !claimTask.Result)
            {
                selectionLocked = false;
                selectedController = null;
                foreach (var controller in controllers)
                {
                    if (controller != null)
                        controller.SetSelectable(true);
                }
                yield break;
            }
        }

        RunManager.Instance.deck.Add(card);

        reward.Claim();

        if (sourceController != null && sourceController.view != null)
        {
            Canvas canvas = sourceController.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

            if (canvasRect != null)
            {
                sourceController.view.HideCardTooltips();

                // Start the selection animation from the center of the screen so the
                // launch visually originates from the middle instead of near the end position.
                Vector2 startScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 endScreenPosition = new Vector2(Screen.width - 64f, Screen.height - 64f);

                GameObject animatedCardObject = new GameObject("RewardSelectCardClone", typeof(RectTransform), typeof(CanvasGroup));
                animatedCardObject.transform.SetParent(canvasRect, false);
                animatedCardObject.transform.SetAsLastSibling();

                RectTransform animatedRoot = animatedCardObject.GetComponent<RectTransform>();
                // For selection clones, use the holder's native size and its world (lossy) scale
                // so the clone matches the live holder pixel-perfectly, just like spawn clones do.
                RectTransform holderRect = sourceController.transform as RectTransform;
                if (holderRect != null)
                {
                    RectTransform sourceViewRectForSize = sourceController.view != null && sourceController.view.rootRect != null
                        ? sourceController.view.rootRect
                        : sourceController.view.GetComponent<RectTransform>();
                    
                    if (sourceViewRectForSize != null)
                    {
                        GetRectCenterAndSizeInCanvas(sourceViewRectForSize, canvas, out Vector2 centerLocal, out Vector2 sizeLocal);
                        animatedRoot.sizeDelta = sourceViewRectForSize.rect.size;
                        animatedRoot.localScale = sourceViewRectForSize.localScale;
                        animatedRoot.anchoredPosition = centerLocal;
                        animatedRoot.pivot = sourceViewRectForSize.pivot;
                    }
                }
                // Log selection clone sizing before animation starts
                RectTransform srcRect = sourceController.transform as RectTransform;
                RectTransform srcViewRect = sourceController.view != null && sourceController.view.rootRect != null
                    ? sourceController.view.rootRect
                    : sourceController.view.GetComponent<RectTransform>();
                GameObject animatedCardViewObject = Instantiate(sourceController.view.gameObject, animatedRoot);
                animatedCardViewObject.transform.SetAsLastSibling();
                RectTransform animatedCardViewRect = animatedCardViewObject.GetComponent<RectTransform>();
                if (animatedCardViewRect != null)
                {
                    RectTransform sourceViewRect = sourceController.view != null && sourceController.view.rootRect != null
                        ? sourceController.view.rootRect
                        : sourceController.view.GetComponent<RectTransform>();
                        animatedCardViewRect.pivot = sourceViewRect != null ? sourceViewRect.pivot : new Vector2(0.5f, 0.5f);
                        animatedCardViewRect.anchorMin = new Vector2(0.5f, 0.5f);
                        animatedCardViewRect.anchorMax = new Vector2(0.5f, 0.5f);
                        animatedCardViewRect.anchoredPosition = Vector2.zero;
                        // Size the inner view to its native rect and leave scale to the animated root
                        // which has been set to render at the same on-screen size as the live holder.
                        RectTransform srcViewRectLocal = sourceController.view != null && sourceController.view.rootRect != null
                            ? sourceController.view.rootRect
                            : sourceController.view.GetComponent<RectTransform>();
                        Vector2 nativeSizeLocal = srcViewRectLocal != null ? srcViewRectLocal.rect.size : animatedRoot.sizeDelta;
                        animatedCardViewRect.sizeDelta = nativeSizeLocal;
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

                Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPosition, uiCamera, out Vector2 startLocalPos);
                // Place the animated root at the start position immediately so it doesn't jump when the selection animation begins.
                animatedRoot.anchoredPosition = startLocalPos;
                sourceController.SetVisualVisible(false);

                yield return StartCoroutine(sourceController.PlayRewardSelectionAnimation(holdDuration, exitDuration, startScreenPosition, endScreenPosition, animatedRoot));
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

    private IEnumerator AnimateSpawnAfterLayout()
    {
        yield return null;

        RectTransform containerRect = cardsContainer as RectTransform;
        if (containerRect == null)
            yield break;

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
            yield break;

        if (cardPanel != null && !cardPanel.activeSelf)
            cardPanel.SetActive(true);

        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        Canvas.ForceUpdateCanvases();

        List<(RewardCardController controller, Vector2 targetPosition, float delay)> spawnCards = new();

        foreach (Transform child in cardsContainer)
        {
            RewardCardController controller = child.GetComponent<RewardCardController>();
            if (controller == null)
                continue;

            RectTransform cardRect = controller.transform as RectTransform;
            if (cardRect == null)
                continue;

            // Log controller size/scale while still in layout (before hiding)
            RectTransform sourceViewRect = controller.view != null && controller.view.rootRect != null
                ? controller.view.rootRect
                : controller.view != null ? controller.view.GetComponent<RectTransform>() : null;
            Vector2 sourceViewSize = sourceViewRect != null ? sourceViewRect.rect.size : Vector2.zero;
            // Compute the controller's center in canvas-local coordinates so the clone lands exactly on it.
            GetRectCenterAndSizeInCanvas(cardRect, canvas, out Vector2 targetLocalPosition, out Vector2 _);
            spawnCards.Add((controller, targetLocalPosition, Random.Range(0f, 0.12f)));
            controller.SetVisualVisible(false);
        }
        foreach (var spawnCard in spawnCards)
        {
            Camera uiCamera = canvas != null ? canvas.worldCamera : null;
                StartCoroutine(PlaySpawnCloneRoutine(spawnCard.controller, canvas, uiCamera, spawnCard.targetPosition, spawnDuration, spawnCard.delay));
        }
    }

    private IEnumerator PlaySpawnCloneRoutine(
        RewardCardController controller,
        Canvas canvas,
        Camera uiCamera,
        Vector2 targetPosition,
        float duration,
        float delay)
    {
        if (controller == null || controller.view == null)
            yield break;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        GameObject animatedCardObject = new GameObject("RewardSpawnCardClone", typeof(RectTransform), typeof(CanvasGroup));
        animatedCardObject.transform.SetParent(canvasRect, false);
        animatedCardObject.transform.SetAsLastSibling();

        RectTransform animatedRoot = animatedCardObject.GetComponent<RectTransform>();
        ConfigureAnimationRoot(animatedRoot, controller.transform as RectTransform, canvas);
        // Compute native and canvas-local sizes, and set root size to native with scale ratio
        RectTransform sourceViewRect2 = controller.view != null && controller.view.rootRect != null
            ? controller.view.rootRect
            : controller.view != null ? controller.view.GetComponent<RectTransform>() : null;
        if (sourceViewRect2 != null)
        {
            GetRectCenterAndSizeInCanvas(sourceViewRect2, canvas, out Vector2 centerLocalForView2, out Vector2 sizeLocalForView2);
            Vector2 nativeSize2 = sourceViewRect2.rect.size;
            animatedRoot.pivot = sourceViewRect2.pivot;
            animatedRoot.sizeDelta = nativeSize2;
            Vector3 scaleRatio2 = Vector3.one;
            if (nativeSize2.x > 0f) scaleRatio2.x = sizeLocalForView2.x / nativeSize2.x;
            if (nativeSize2.y > 0f) scaleRatio2.y = sizeLocalForView2.y / nativeSize2.y;
            animatedRoot.localScale = new Vector3(scaleRatio2.x, scaleRatio2.y, 1f);
            animatedRoot.anchoredPosition = centerLocalForView2;
        }
        // Log computed animated root vs source sizes/scales at animation start
        RectTransform sourceRect = controller.transform as RectTransform;
        GameObject animatedCardViewObject = Instantiate(controller.view.gameObject, animatedRoot);
        animatedCardViewObject.transform.SetAsLastSibling();
            RectTransform animatedCardViewRect = animatedCardViewObject.GetComponent<RectTransform>();
            if (animatedCardViewRect != null)
            {
                // Size the instantiated CardView to its native rect and leave scaling to the animated root
                // so the rendered on-screen size matches the live holder exactly.
                RectTransform sourceViewRect = controller.view != null && controller.view.rootRect != null
                    ? controller.view.rootRect
                    : controller.view.GetComponent<RectTransform>();

                animatedCardViewRect.pivot = sourceViewRect != null ? sourceViewRect.pivot : new Vector2(0.5f, 0.5f);
                animatedCardViewRect.anchorMin = new Vector2(0.5f, 0.5f);
                animatedCardViewRect.anchorMax = new Vector2(0.5f, 0.5f);
                animatedCardViewRect.anchoredPosition = Vector2.zero;
                animatedCardViewRect.sizeDelta = sourceViewRect != null ? sourceViewRect.rect.size : animatedRoot.sizeDelta;
                animatedCardViewRect.localScale = Vector3.one;
            }

        CardView animatedCardView = animatedCardViewObject.GetComponent<CardView>();
        if (animatedCardView != null)
        {
            animatedCardView.SetCard(controller.instance);
        }
        CanvasGroup animatedGroup = animatedCardObject.GetComponent<CanvasGroup>();
        if (animatedGroup != null)
        {
            animatedGroup.alpha = 1f;
            animatedGroup.interactable = false;
            animatedGroup.blocksRaycasts = false;
        }

        Vector3[] corners = new Vector3[4];
        animatedRoot.GetWorldCorners(corners);
        Vector2 startLocalPosition;
        Vector3 finalScale = animatedRoot.localScale;
        Vector3 startScale;

        if (spawnOrigin != null)
        {
            // Start from the provided spawn origin (e.g., the button position) at a smaller scale relative to final.
            Vector3[] originCorners = new Vector3[4];
            spawnOrigin.GetWorldCorners(originCorners);
            Vector3 originWorldCenter = (originCorners[0] + originCorners[2]) * 0.5f;
            Camera uiCamera2 = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 originScreen = RectTransformUtility.WorldToScreenPoint(uiCamera2, originWorldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, originScreen, uiCamera2, out startLocalPosition);
            startScale = finalScale * 0.6f;
        }
        else
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero)
                randomDirection = Vector2.left;

            float travelDistance = Random.Range(180f, 320f);
            Vector2 startOffset = randomDirection * travelDistance;
            startLocalPosition = targetPosition + startOffset;
            startScale = finalScale * 0.92f;
        }

        Vector2 travelDirection = (targetPosition - startLocalPosition).normalized;
        Vector2 perpendicular = new Vector2(-travelDirection.y, travelDirection.x);
        float arcHeight = Random.Range(90f, 190f) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 controlPoint = (startLocalPosition + targetPosition) * 0.5f
            + perpendicular * arcHeight
            + travelDirection * Random.Range(-40f, 30f);

        animatedRoot.anchoredPosition = startLocalPosition;
        animatedRoot.localScale = startScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            Vector2 position = Mathf.Pow(1f - eased, 2f) * startLocalPosition
                + 2f * (1f - eased) * eased * controlPoint
                + Mathf.Pow(eased, 2f) * targetPosition;

            animatedRoot.anchoredPosition = position;
            animatedRoot.localScale = Vector3.Lerp(startScale, finalScale, eased);
            if (animatedGroup != null)
                animatedGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        animatedRoot.anchoredPosition = targetPosition;
        animatedRoot.localScale = finalScale;

        controller.SetVisualVisible(true);
        Destroy(animatedCardObject);
    }

    private static void ConfigureAnimationRoot(RectTransform targetRect, RectTransform sourceRect, Canvas canvas)
    {
        if (targetRect == null)
            return;

        targetRect.anchorMin = new Vector2(0.5f, 0.5f);
        targetRect.anchorMax = new Vector2(0.5f, 0.5f);
        targetRect.pivot = new Vector2(0.5f, 0.5f);
        if (sourceRect == null)
        {
            targetRect.sizeDelta = new Vector2(200f, 300f);
            targetRect.localScale = Vector3.one;
            targetRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            // Compute the source rect's center and size in canvas-local coordinates so the
            // animated root exactly matches the visual placement and size of the source.
            GetRectCenterAndSizeInCanvas(sourceRect, canvas, out Vector2 centerLocal, out Vector2 sizeLocal);
            // Use the computed canvas-local size for the animated root and keep its
            // localScale at 1 so the root's rect space matches canvas pixels exactly.
            targetRect.sizeDelta = sizeLocal;
            targetRect.localScale = Vector3.one;
            targetRect.anchoredPosition = centerLocal;
        }
        targetRect.localRotation = Quaternion.identity;
    }

    private static void GetRectCenterAndSizeInCanvas(RectTransform sourceRect, Canvas canvas, out Vector2 centerLocal, out Vector2 sizeLocal)
    {
        centerLocal = Vector2.zero;
        sizeLocal = Vector2.zero;
        if (sourceRect == null || canvas == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector3[] worldCorners = new Vector3[4];
        sourceRect.GetWorldCorners(worldCorners);

        Vector2 blScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[0]);
        Vector2 trScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, blScreen, uiCamera, out Vector2 blLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, trScreen, uiCamera, out Vector2 trLocal);

        sizeLocal = trLocal - blLocal;
        sizeLocal.x = Mathf.Abs(sizeLocal.x);
        sizeLocal.y = Mathf.Abs(sizeLocal.y);
        centerLocal = (blLocal + trLocal) * 0.5f;
    }

    private static Vector2 GetCardScreenCenter(RectTransform cardRect, Canvas canvas)
    {
        if (cardRect == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector3[] corners = new Vector3[4];
        cardRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
    }
}