using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GlowEffect : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform glowTransform;

    public float pulseSpeed = 2f;
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    public float minScale = 1f;
    public float maxScale = 1.15f;

    bool active;
    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        glowTransform.localScale = Vector3.one * scale;
    }
}