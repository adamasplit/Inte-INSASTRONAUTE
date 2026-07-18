using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class RestCardController : MonoBehaviour
{
    public CardView view;
    CardInstance card;
    RestManager manager;

    public CardInstance Card => card;

    public void Init(CardInstance card, RestManager mgr)
    {
        this.card = card;
        manager = mgr;

        view.SetCard(card);
    }

    public void OnClick()
    {
        manager.OnCardSelected(this);
    }

    public void SetSelected(bool selected)
    {
        if (view != null && view.selectionHighlight != null)
            view.selectionHighlight.SetActive(selected);
    }

    public void SetVisualVisible(bool visible)
    {
        if (view != null)
            view.gameObject.SetActive(visible);
    }

    public void RefreshView()
    {
        if (view != null)
            view.SetCard(card);
    }

    private void PlayPresentationCue(string cueName, Vector3 position)
    {
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayEffect(cueName, position);

        SFXManager.Instance?.PlaySound(cueName);
    }

    public IEnumerator PlayEnchantExitAnimation(
        float holdDuration,
        float duration,
        Vector2 startScreenPosition,
        Vector2 endScreenPosition,
        RectTransform animatedRoot
    )
    {
        if (animatedRoot == null)
            yield break;

        CanvasGroup canvasGroup = animatedRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = animatedRoot.gameObject.AddComponent<CanvasGroup>();

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
            yield break;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPosition, uiCamera, out Vector2 startPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreenPosition, uiCamera, out Vector2 endPosition);

        Vector3 startScale = Vector3.one*3f;
        Vector3 endScale = Vector3.one;
        float startAlpha = canvasGroup.alpha;
        float rand = Random.Range(-10f, 10f);
        Quaternion startRotation = Quaternion.Euler(0f, 0f, rand);
        Quaternion endRotation = Quaternion.Euler(0f, 0f, -rand*2f);

        Vector2 curveDirection = endPosition - startPosition;
        Vector2 perpendicular = new Vector2(-curveDirection.y, curveDirection.x).normalized;
        Vector2 controlPoint = (startPosition + endPosition) * 0.5f + perpendicular * 90f + Vector2.up * 40f;

        animatedRoot.anchoredPosition = startPosition;
        animatedRoot.localScale = startScale;
        animatedRoot.localRotation = startRotation;

        PlayPresentationCue("Enchant", animatedRoot.position);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t;

            Vector2 position = Mathf.Pow(1f - eased, 2f) * startPosition
                + 2f * (1f - eased) * eased * controlPoint
                + Mathf.Pow(eased, 2f) * endPosition;

            animatedRoot.anchoredPosition = position;
            animatedRoot.localScale = Vector3.Lerp(startScale, endScale, eased);
            animatedRoot.localRotation = Quaternion.Lerp(startRotation, endRotation, eased);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        animatedRoot.anchoredPosition = endPosition;
        animatedRoot.localScale = endScale;
        animatedRoot.localRotation = endRotation;
        canvasGroup.alpha = 0f;
    }
}