using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Lean.Gui;

public class DragOrScrollRouter : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    public ScrollRect scrollRect; // Assign in inspector
    public RectTransform viewport; // Assign in inspector (scrollRect.viewport)
    public LeanDrag leanDrag; // Assign in inspector

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsPointerOverViewport(eventData))
        {
            // Forward to ScrollRect
            ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.pointerDownHandler);
        }
        else
        {
            // Forward to LeanDrag
            leanDrag.OnPointerDown(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsPointerOverViewport(eventData))
        {
            ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
        }
        else
        {
            leanDrag.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsPointerOverViewport(eventData))
        {
            ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
        }
        else
        {
            leanDrag.OnEndDrag(eventData);
        }
    }

    private bool IsPointerOverViewport(PointerEventData eventData)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(viewport, eventData.position, eventData.pressEventCamera);
    }
}