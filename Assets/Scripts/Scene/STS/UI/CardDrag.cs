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
        group.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.position = eventData.position;
        var cardView = GetComponent<CardView>();
        if (cardView?.cardInstance == null)
            return;
        var future = turnSystem.GetFuture(10);
        var target = GetHoveredTarget();
        var sim = turnSystem.SimulateCard(future, cardView.cardInstance, target);
        Debug.Log("[OnDrag] SIM CONTAINS ADVANCED: " + sim.Any(t => t.visualType == TurnVisualType.Advanced));
        ui.HighlightTargets(cardView.cardInstance.data.targetingMode, target);
        timelineUI.Display(sim);
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
    }
    Character GetHoveredTarget()
    {
        return DropZone.hoveredCharacter;
    }

}