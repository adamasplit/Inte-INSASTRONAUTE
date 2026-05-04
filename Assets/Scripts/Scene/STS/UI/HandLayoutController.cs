using UnityEngine;
using System.Collections.Generic;

public class HandLayoutController : MonoBehaviour
{
    public float spacing = 200f;
    public float arcHeight = 50f;
    public float maxAngle = 25f;
    public CardView selectedCard;
    // 1.3f pour 150f de spacing
    private float selectedScale => (spacing / 150f) * 1.3f;
    private float selectedYOffset = 100f;
    private float pushStrength = 100f;
    public float smooth = 12f;

    [System.Serializable]
    public class CardLayoutState
    {
        public Vector2 targetPos;
        public float targetAngle;
        public float targetScale = 1f;
    }
    private Dictionary<CardView, CardLayoutState> states = new();

    public void Arrange(List<CardView> cards)
    {
        int count = cards.Count;
        if (count == 0) return;

        float center = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            var cardView = cards[i];
            RectTransform card = cardView.GetComponent<RectTransform>();

            if (!states.ContainsKey(cardView))
                states[cardView] = new CardLayoutState();

            float offset = i - center;

            float x = offset * spacing;
            float y = -Mathf.Abs(offset) * arcHeight;
            float angle = -offset * (maxAngle / Mathf.Max(1, center));

            // sélection
            if (cardView == selectedCard)
            {
                y += selectedYOffset;
                states[cardView].targetScale = selectedScale;
            }
            else
            {
                states[cardView].targetScale = 1f;

                if (selectedCard != null)
                {
                    int selectedIndex = cards.IndexOf(selectedCard);
                    float dir = Mathf.Sign(i - selectedIndex);
                    x += dir * pushStrength;
                }
            }

            states[cardView].targetPos = new Vector2(x, y);
            states[cardView].targetAngle = angle;
        }
    }

    void Update()
    {
        foreach (var kvp in states)
        {
            var card = kvp.Key;
            var state = kvp.Value;

            if (card == null) continue;

            RectTransform rt = card.GetComponent<RectTransform>();

            rt.anchoredPosition = Vector2.Lerp(
                rt.anchoredPosition,
                state.targetPos,
                Time.deltaTime * smooth
            );

            rt.localRotation = Quaternion.Lerp(
                rt.localRotation,
                Quaternion.Euler(0, 0, state.targetAngle),
                Time.deltaTime * smooth
            );

            Vector3 targetScale = Vector3.one * state.targetScale;

            rt.localScale = Vector3.Lerp(
                rt.localScale,
                targetScale,
                Time.deltaTime * smooth
            );
        }
    }
}