using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CardAnimator : MonoBehaviour
{
    public float duration = 0.15f;
    private float curveHeight = 4f;
    public RectTransform animationLayer;

    public IEnumerator MoveCard(
        RectTransform rect,
        Vector3 start,
        Vector3 end,
        float speedMultiplier = 1f,
        bool curved = true,
        bool forceRotation = false,
        Vector3? startScale = null,
        Vector3? endScale = null,
        Quaternion? endRotation = null,
        GameObject trailSource = null,
        float trailSpawnInterval = 0.03f,
        float trailAlpha = 0.35f,
        float trailLifetime = 0.18f,
        bool arcAwayFromTarget = false,
        float arcAwayDistance = 150f
    )
    {
        CardView cardView = rect != null ? rect.GetComponent<CardView>() : null;
        CanvasGroup canvasGroup = rect != null ? rect.GetComponent<CanvasGroup>() : null;
        bool restoreBlocksRaycasts = false;
        bool restoreInteractable = false;

        if (cardView != null)
        {
            cardView.isAnimating = true;
        }

        if (canvasGroup != null)
        {
            restoreBlocksRaycasts = canvasGroup.blocksRaycasts;
            restoreInteractable = canvasGroup.interactable;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        try
        {
            float t = 0f;
            Quaternion startRotation = rect.localRotation;
            Quaternion targetRotation = endRotation ?? (forceRotation ? Quaternion.identity : startRotation);
            bool shouldRotate = forceRotation || endRotation.HasValue;
            Vector3 initialScale = startScale ?? rect.localScale;
            Vector3 finalScale = endScale ?? initialScale;
            float trailTimer = 0f;


            Vector3 control = start;
            Vector3 bezierStart = start;
            Vector3 bezierEnd = end;
            
            if (arcAwayFromTarget)
            {
                // Calculate direction away from target for arc
                Vector3 towardTarget = end - start;
                Vector3 awayDirection = towardTarget.sqrMagnitude > 0.001f ? -towardTarget.normalized : Vector3.left;
                
                // Control point placed away from target to create the arc
                // Bezier starts at actual start position, curves through control point, ends at target
                control = start + awayDirection * arcAwayDistance;
            }
            else
            {
                control = (start + end) / 2 + Vector3.up * curveHeight;
                if (!curved)
                    control = (start + end) / 2;
            }
            
            while (t < 1f)
            {
                t += Time.deltaTime / duration * speedMultiplier;
                trailTimer += Time.deltaTime;

                float eased;
                if (arcAwayFromTarget)
                {
                    // SmoothStep: starts average, slows at the away point, accelerates back
                    eased = Mathf.SmoothStep(0, 1, t);
                }
                else
                {
                    eased = Mathf.SmoothStep(0, 1, t);
                }

                Vector3 pos =
                    Mathf.Pow(1 - eased, 2) * bezierStart
                    + 2 * (1 - eased) * eased * control
                    + Mathf.Pow(eased, 2) * bezierEnd;

                rect.position = pos;
                rect.localScale = Vector3.Lerp(initialScale, finalScale, eased);
                if (shouldRotate)
                {
                    rect.localRotation = Quaternion.Lerp(
                        startRotation,
                        targetRotation,
                        eased
                    );
                }

                if (trailSource != null && trailTimer >= trailSpawnInterval)
                {
                    trailTimer = 0f;
                    SpawnTrailGhost(trailSource, rect.position, rect.localRotation, rect.localScale, trailAlpha, trailLifetime);
                }

                yield return null;
            }

            rect.position = end;
            rect.localScale = finalScale;
            if (shouldRotate)
            {
                rect.localRotation = targetRotation;
            }
        }
        finally
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = restoreBlocksRaycasts;
                canvasGroup.interactable = restoreInteractable;
            }

            if (cardView != null)
            {
                cardView.isAnimating = false;
            }
        }
    }

    void SpawnTrailGhost(
        GameObject source,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        float alpha,
        float lifetime
    )
    {
        if (source == null || animationLayer == null)
            return;

        GameObject ghost = Instantiate(source, animationLayer, false);
        ghost.transform.SetAsLastSibling();

        RectTransform ghostRect = ghost.GetComponent<RectTransform>();
        if (ghostRect != null)
        {
            ghostRect.position = position;
            ghostRect.rotation = rotation;
            ghostRect.localScale = Vector3.one * 0.96f;
        }

        CanvasGroup group = ghost.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = ghost.AddComponent<CanvasGroup>();
        }

        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = false;

        CardView cardView = ghost.GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.enabled = false;
            cardView.transform.localScale = Vector3.one;
        }

        CardDrag cardDrag = ghost.GetComponent<CardDrag>();
        if (cardDrag != null)
        {
            cardDrag.enabled = false;
            cardDrag.transform.localScale = Vector3.one;
        }

        StartCoroutine(FadeAndDestroyTrailGhost(ghost, group, lifetime));
    }

    IEnumerator FadeAndDestroyTrailGhost(GameObject ghost, CanvasGroup group, float lifetime)
    {
        float elapsed = 0f;
        float startAlpha = group != null ? group.alpha : 1f;

        while (elapsed < lifetime && ghost != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            if (group != null)
            {
                group.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }

            yield return null;
        }

        if (ghost != null)
        {
            Destroy(ghost);
        }
    }
}