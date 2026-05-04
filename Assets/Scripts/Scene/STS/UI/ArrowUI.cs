using UnityEngine;
using System.Collections.Generic;
public class ArrowUI : MonoBehaviour
{
    public RectTransform dashPrefab;
    public RectTransform head;

    List<RectTransform> dashes = new();

    public int dashCount = 20;
    public float curveHeight = 150f;
    Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        return Mathf.Pow(1 - t, 2) * a
            + 2 * (1 - t) * t * b
            + Mathf.Pow(t, 2) * c;
    }

    public RectTransform container;

    public void UpdateArrow(Vector2 screenStart, Vector2 screenEnd)
    {
        if (container == null)
            container = transform as RectTransform;

        // conversion écran -> local UI
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container,
            screenStart,
            null,
            out Vector2 start
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container,
            screenEnd,
            null,
            out Vector2 end
        );

        while (dashes.Count < dashCount)
        {
            var dash = Instantiate(dashPrefab, container);
            dashes.Add(dash);
        }

        Vector2 mid = (start + end) * 0.5f;

        // courbure PERPENDICULAIRE
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        float usedCurveHeight = Mathf.Min(curveHeight, Vector2.Distance(start, end) * 0.5f);

        Vector2 control = mid + Vector2.up * usedCurveHeight;

        for (int i = 0; i < dashCount; i++)
        {
            float t = i / (float)(dashCount - 1);

            Vector2 pos = QuadraticBezier(start, control, end, t);

            dashes[i].anchoredPosition = pos;

            // orientation
            float nextT = Mathf.Min(t + 0.02f, 1f);

            Vector2 nextPos =
                QuadraticBezier(start, control, end, nextT);

            Vector2 tangent = (nextPos - pos).normalized;

            float angle =
                Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            dashes[i].localRotation =
                Quaternion.Euler(0, 0, angle);
        }

        // HEAD
        head.anchoredPosition = end;

        Vector2 beforeEnd =
            QuadraticBezier(start, control, end, 0.95f);

        Vector2 finalDir = (end - beforeEnd).normalized;

        float finalAngle =
            Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;

        head.localRotation =
            Quaternion.Euler(0, 0, finalAngle+90f);
    }
}