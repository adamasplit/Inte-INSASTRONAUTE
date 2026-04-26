using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
public class CardDrag : MonoBehaviour,
IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rect;
    Canvas canvas;
    CanvasGroup group;

    Vector2 startPos;
    public CardView cardView;
    public UIManager ui;
    public TurnSystem turnSystem;
    public TimelineUI timelineUI;
    public CombatManager combat;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        group = GetComponent<CanvasGroup>();
        ui = FindFirstObjectByType<UIManager>();
        turnSystem = FindFirstObjectByType<TurnSystem>();
        timelineUI = FindFirstObjectByType<TimelineUI>();
        combat = FindFirstObjectByType<CombatManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = rect.anchoredPosition;
        transform.localScale = Vector3.one * 1.1f;
        group.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.position = eventData.position;
        if (cardView.cardInstance == null)
        {
            Debug.LogError("CardView has no card instance");
            return;
        }
        var future = turnSystem.GetFuture(10);
        var target = GetHoveredTarget();
        var sim = turnSystem.SimulateCard(future, cardView.cardInstance, target);
        ui.HighlightTargets(cardView.cardInstance.data.targetingMode, target);
        timelineUI.Display(sim,true);
        cardView.RefreshDescription(target);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        group.blocksRaycasts = true;
        rect.anchoredPosition = startPos;
        timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
        var cardView = GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.RefreshDescription(null);
        }
        transform.localScale = Vector3.one;
    }
    Character GetHoveredTarget()
    {
        return DropZone.hoveredCharacter;
    }

}