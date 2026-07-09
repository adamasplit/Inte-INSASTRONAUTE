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
    private float GetHandScaleMultiplier(int cardCount)
    {
        if (cardCount < 7)
            return 1f;

        if (cardCount >= 10)
            return 0.75f;

        float t = (cardCount - 7f) / 3f;
        return Mathf.Lerp(1f, 0.75f, t);
    }
    private float selectedYOffset = 100f;
    private float pushStrength = 100f;
    public float smooth = 12f;

    float GetAdaptiveScale()
    {
        float screenScale = UIAdaptiveScale.GetScreenScale();
        return Mathf.Lerp(1f, screenScale, 0.9f);
    }

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

        float adaptiveScale = GetAdaptiveScale();
        float compactFactor = count > 5 ? 1f / (1f + (count - 5) * 0.18f) : 1f;
        float layoutSpacing = spacing * compactFactor * adaptiveScale;
        float layoutArcHeight = arcHeight * compactFactor * adaptiveScale;
        float layoutMaxAngle = maxAngle * compactFactor * adaptiveScale;
        float handScaleMultiplier = GetHandScaleMultiplier(count);
        float layoutCardScale = Mathf.Lerp(1f, adaptiveScale, 0.85f);

        float center = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            var cardView = cards[i];
            RectTransform card = cardView.rootRect;
            if (cardView.isAnimating) continue;

            if (!states.ContainsKey(cardView))
                states[cardView] = new CardLayoutState();

            float offset = i - center;

            float x = offset * layoutSpacing;
            float y = -Mathf.Abs(offset) * layoutArcHeight;
            float angle = -offset * (layoutMaxAngle / Mathf.Max(1, center));

            if (cardView.isDragging)
            {
                states[cardView].targetPos = card.anchoredPosition;
                states[cardView].targetAngle = 0f;
                states[cardView].targetScale = selectedScale * handScaleMultiplier * layoutCardScale;
                card.SetAsLastSibling();
                continue;
            }

            // sélection
            bool highlighted =
                cardView == selectedCard
                || cardView.selectionPreview;

            if(highlighted)
            {
                y += selectedYOffset;

                states[cardView].targetScale =
                    selectedScale * handScaleMultiplier * layoutCardScale;
                card.SetAsLastSibling();
                if (cardView.selectionPreview)
                {
                    angle = 0f;
                    y+=selectedYOffset*1f;
                }
            }
            else
            {
                states[cardView].targetScale = handScaleMultiplier * layoutCardScale;

                if (selectedCard != null)
                {
                    int selectedIndex = cards.IndexOf(selectedCard);
                    float dir = Mathf.Sign(i - selectedIndex);
                    x += dir * pushStrength * adaptiveScale * 1.1f;
                }
            }

            states[cardView].targetPos = new Vector2(x, y);
            states[cardView].targetAngle = angle;
        }
    }
    public bool cardSide(CardView card)
    {
        if (!states.ContainsKey(card)) return true;
        return states[card].targetPos.x >= 0;
    }

    void Update()
    {
        foreach (var kvp in states)
        {
            var card = kvp.Key;
            var state = kvp.Value;

            if (card == null || card.isAnimating) continue;

            RectTransform rt = card.rootRect;

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

            card.gameObject.transform.localScale = Vector3.Lerp(
                card.gameObject.transform.localScale,
                targetScale,
                Time.deltaTime * smooth
            );
        }
    }
    public Vector2 GetTargetPosition(CardView card)
    {
        if (states.TryGetValue(card, out var state))
            return state.targetPos;

        return Vector2.zero;
    }
    public CardView GetCardWithType(CardType type)
    {
        foreach (var kvp in states)
        {
            if (kvp.Key.cardInstance.data.type == type)
                return kvp.Key;
        }
        return null;
    }
}