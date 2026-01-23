using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public List<Column> columns = new List<Column>();
    public GameObject columnPrefab;

    [Range(0f, 0.5f)]
    public float horizontalMarginPercent = 0.05f;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        LayoutColumns();
    }

    public void LayoutColumns()
    {
        // Supprimer les anciennes colonnes
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        columns.Clear();

        int columnCount = GameManager.Instance.columnCount;
        float canvasWidth = rectTransform.rect.width;
        float hMargin = canvasWidth * horizontalMarginPercent;
        float usableWidth = canvasWidth - 2 * hMargin;
        float colWidth = usableWidth / columnCount;

        for (int i = 0; i < columnCount; i++)
        {
            GameObject colObj = Instantiate(columnPrefab, transform);
            Column col = colObj.GetComponent<Column>();
            columns.Add(col);

            RectTransform colRect = colObj.GetComponent<RectTransform>();

            // Stretch verticalement
            colRect.anchorMin = new Vector2(0, 0);
            colRect.anchorMax = new Vector2(0, 1);
            colRect.pivot = new Vector2(0.5f, 0.5f);

            // Position horizontale et largeur
            float x = -usableWidth / 2f + colWidth / 2f + i * colWidth;
            colRect.anchoredPosition = new Vector2(x, 0);
            colRect.sizeDelta = new Vector2(colWidth, 0); // 0 car stretch vertical

            // Optionnel : centrer la tour à l’intérieur
            if (col.tower != null)
            {
                RectTransform towerRect = col.tower.GetComponent<RectTransform>();
                towerRect.anchorMin = towerRect.anchorMax = new Vector2(0.5f, 0.5f);
                towerRect.pivot = new Vector2(0.5f, 0.5f);
                towerRect.anchoredPosition = Vector2.zero;
            }
        }
    }
}
