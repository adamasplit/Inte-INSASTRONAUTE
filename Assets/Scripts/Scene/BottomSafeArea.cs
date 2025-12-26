using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BottomSafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private float originalBottomOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalBottomOffset = rectTransform.offsetMin.y;
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // Distance entre le bas de l'écran et le bas de la safe area
        float bottomInset = safeArea.yMin;

        // 👉 On applique UNIQUEMENT si c'est significatif
        if (bottomInset > 1f)
        {
            rectTransform.offsetMin = new Vector2(
                rectTransform.offsetMin.x,
                originalBottomOffset + bottomInset
            );
        }
        else
        {
            // Aucun device bloquant → position normale
            rectTransform.offsetMin = new Vector2(
                rectTransform.offsetMin.x,
                originalBottomOffset
            );
        }
    }
}
