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
        float trailLifetime = 0.18f
    )
    {
        float t = 0f;
        Quaternion startRotation = rect.localRotation;
        Quaternion targetRotation = endRotation ?? (forceRotation ? Quaternion.identity : startRotation);
        bool shouldRotate = forceRotation || endRotation.HasValue;
        Vector3 initialScale = startScale ?? rect.localScale;
        Vector3 finalScale = endScale ?? initialScale;
        float trailTimer = 0f;


        Vector3 control =
            (start + end) / 2
            + Vector3.up * curveHeight;

        if (!curved)
            control = (start + end) / 2;
        while (t < 1f)
        {
            t += Time.deltaTime / duration * speedMultiplier;
            trailTimer += Time.deltaTime;

            float eased =
                Mathf.SmoothStep(0, 1, t);

            Vector3 pos =
                Mathf.Pow(1 - eased, 2) * start
                + 2 * (1 - eased) * eased * control
                + Mathf.Pow(eased, 2) * end;

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