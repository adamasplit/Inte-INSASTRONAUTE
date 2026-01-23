using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameCardUI : MonoBehaviour
{
    public CardData data;
    public Image image;

    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    private Transform originalParent;
    private Vector3 startLocalPos;
    private bool isDragging;
    // Hover
    private Tower hoveredTower;
    private Vector3 originalTowerScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        originalParent = transform.parent;
        startLocalPos = rectTransform.localPosition;
    }

    public void Initialize(CardData cardData)
    {
        data = cardData;
        image.sprite = data.sprite;
    }

    // --- LeanGUI OnBegin ---
    public void OnBeginDrag()
    {
        isDragging = true;
        rectTransform.localScale = Vector3.one * 0.4f;

        // Centrer pivot et anchors
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Libérer LayoutGroup
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;

        // Déplacer dans DragLayer
        rectTransform.SetParent(DragLayerProvider.Instance, true);

        // Position initiale sous le curseur
        UpdatePositionToPointer();
    }

    // --- LeanGUI OnEnd ---
    public void OnEndDrag()
    {
        isDragging = false;
        rectTransform.localScale = Vector3.one;

        bool dropped = TryDropOnTower();

        if (!dropped)
        {
            // Retour au LayoutGroup
            rectTransform.SetParent(originalParent, false);
            if (layoutElement != null)
                layoutElement.ignoreLayout = false;

            rectTransform.localPosition = startLocalPos; // layout repositionne correctement
        }
    }

    void Update()
    {
        if (isDragging)
        {
            UpdatePositionToPointer();
            HandleHover();
        }
    }

    private void UpdatePositionToPointer()
    {
        Vector2 pointerScreen = Pointer.current.position.ReadValue();
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rootCanvas.transform as RectTransform,
                pointerScreen,
                rootCanvas.worldCamera,
                out Vector3 worldPoint))
        {
            rectTransform.position = worldPoint;
        }
    }

    void HandleHover()
    {
        Vector2 pointerPos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tower tower = hit.collider.GetComponent<Tower>();
            if (tower != null)
            {
                if (hoveredTower != tower)
                {
                    ResetHoveredTower();
                    hoveredTower = tower;
                    originalTowerScale = tower.transform.localScale;
                    tower.transform.localScale = originalTowerScale * 1.2f;
                }
                return;
            }
        }

        ResetHoveredTower();
    }

    void ResetHoveredTower()
    {
        if (hoveredTower != null)
        {
            hoveredTower.transform.localScale = originalTowerScale;
            hoveredTower = null;
        }
    }

    // --- drop sur tour ---
    private bool TryDropOnTower()
    {
        Vector2 pointerPos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tower tower = hit.collider.GetComponent<Tower>();
            if (tower != null)
            {
                tower.Activate(data);
                Destroy(gameObject);
                return true;
            }
        }
        return false;
    }
}
