using UnityEngine;
using System.Collections.Generic;

public class HandLayoutController : MonoBehaviour
{
    public float spacing = 150f;
    public float arcHeight = 50f;
    public float maxAngle = 25f;

    public void Arrange(List<RectTransform> cards)
    {
        int count = cards.Count;
        if (count == 0) return;

        float center = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];

            float offset = i - center;

            // POSITION (centrée)
            float x = offset * spacing;
            float y = -Mathf.Abs(offset) * arcHeight;

            // ANGLE (symétrique)
            float angle = -offset * (maxAngle / Mathf.Max(1, center));

            card.anchoredPosition = new Vector2(x, y);
            card.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}