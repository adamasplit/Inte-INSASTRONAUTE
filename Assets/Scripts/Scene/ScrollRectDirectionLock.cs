using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectDirectionLock :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public ScrollRect scrollRect;
    private bool baseHorizontal,baseVertical;

    private Vector2 startPos;
    private bool decided = false;

    private float directionThreshold = 20f;
    public Transform content;

    void Awake()
    {
        if (!scrollRect)
            scrollRect = GetComponent<ScrollRect>();
        baseHorizontal = scrollRect.horizontal;
        baseVertical = scrollRect.vertical;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
        decided = false;
        scrollRect.enabled = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (decided) return;

        
        Vector2 delta = eventData.position - startPos;
        bool outsideContentBounds = false;
        if (content != null)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
            Vector3 localPoint = content.InverseTransformPoint(worldPoint);
            outsideContentBounds = !content.GetComponent<RectTransform>().rect.Contains(localPoint);
        }
        if (outsideContentBounds)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
            Debug.Log("[ScrollRectDirectionLock] Horizontal swipe detected, disabling ScrollRect.");
            decided = true;
        }
        else if (Mathf.Abs(delta.y) > directionThreshold)
        {
            scrollRect.horizontal = baseHorizontal;
            scrollRect.vertical = baseVertical;
            decided = true;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        scrollRect.horizontal = baseHorizontal;
        scrollRect.vertical = baseVertical;
        decided = false;
    }
}
