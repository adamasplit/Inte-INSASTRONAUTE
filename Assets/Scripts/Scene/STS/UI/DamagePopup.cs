using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float riseDistance = 42f;
    [SerializeField] private float duration = 0.75f;
    [SerializeField] private float fadeOutStart = 0.1f;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Play(int amount, bool healing, bool blocked)
    {
        if (text == null)
            return;

        text.text = amount.ToString();
        text.color = healing
            ? new Color32(96, 220, 120, 255)
            : blocked
                ? new Color32(90, 170, 255, 255)
                : Color.white;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        if (rect == null)
        {
            yield break;
        }

        Vector3 startPosition = rect.localPosition;
        Vector3 endPosition = startPosition + Vector3.up * riseDistance;
        float elapsed = 0f;

        canvasGroup.alpha = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.localPosition = Vector3.Lerp(startPosition, endPosition, t);

            if (t >= fadeOutStart)
            {
                float fadeT = Mathf.InverseLerp(fadeOutStart, 1f, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        Destroy(gameObject);
    }
}
