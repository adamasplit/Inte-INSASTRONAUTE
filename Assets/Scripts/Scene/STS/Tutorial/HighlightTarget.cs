using UnityEngine;

public struct HighlightTarget
{
    public Rect screenRect;
    public bool valid;


    public static HighlightTarget FromRectTransform(RectTransform rt, Canvas canvas)
    {
        if (rt == null || canvas == null)
        {
            return default;
        }

        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = min;

        for (int i = 1; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        return new HighlightTarget
        {
            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y),
            valid = true
        };
    }
}