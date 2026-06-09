using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HealthBar : MonoBehaviour
{
    public RectTransform fill;
    public Image fillImage;
    public TextMeshProUGUI text;
    [Tooltip("Override the maximum width of the health bar. If 0, use container width.")]
    public float maxWidthOverride = 0f;
    float maxWidth;
    float lastRatio = 1f;

    void Awake()
    {
        if (fill == null) return;

        fill.anchorMin = new Vector2(0f, fill.anchorMin.y);
        fill.anchorMax = new Vector2(0f, fill.anchorMax.y);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = new Vector2(0f, fill.anchoredPosition.y);

        CacheMaxWidth();
    }

    void OnRectTransformDimensionsChange()
    {
        CacheMaxWidth();
        ApplyWidth();
    }

    void CacheMaxWidth()
    {
        if (maxWidthOverride > 0f)
        {
            maxWidth = maxWidthOverride;
            return;
        }
        if (fill != null)
        {
            var parentRect = fill.parent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;
                if (parentWidth > 0f)
                {
                    maxWidth = parentWidth;
                    return;
                }
            }

            float fillWidth = fill.rect.width;
            if (fillWidth > 0f)
            {
                maxWidth = fillWidth;
            }
        }
    }

    void ApplyWidth()
    {
        if (fill == null || maxWidth <= 0f) return;
        float targetWidth = maxWidth * lastRatio;
        if (Mathf.Approximately(fill.sizeDelta.x, targetWidth)) return;

        fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
    }

    public void SetHealth(int current, int max)
    {
        text.text = $"{current}/{max}";
        lastRatio = max <= 0 ? 0f : Mathf.Clamp01(current / (float)max);
        CacheMaxWidth();
        ApplyWidth();
    }
}