using UnityEngine;

public static class UIAdaptiveScale
{
    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;
    const float MaxScale = 1.75f;

    public static float GetScreenScale()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return 1f;

        float widthScale = Screen.width / ReferenceWidth;
        float heightScale = Screen.height / ReferenceHeight;
        return Mathf.Clamp(Mathf.Max(widthScale, heightScale), 1f, MaxScale);
    }
}