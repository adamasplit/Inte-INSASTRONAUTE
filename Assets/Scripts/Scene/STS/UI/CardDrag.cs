using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
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
    public GameObject arrowPrefab;
    private ArrowUI arrow;
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
        cardView.OnPointerClick(eventData); // Ensure the card is selected 
        startPos = rect.anchoredPosition;
        transform.localScale = Vector3.one * 1.1f;
        group.blocksRaycasts = false;
        Canvas canvas =  GameObject.Find("ArrowCanvas").GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No canvas found for arrow");
            return;
        }
        GameObject arrowObject = Instantiate(arrowPrefab, canvas.transform);
        if (arrowObject == null)
        {
            Debug.LogError("Arrow prefab not found!");
            return;
        }
        arrow = arrowObject.GetComponent<ArrowUI>();
        if (arrow == null)
        {
            Debug.LogError("ArrowUI component not found on arrow prefab!");
            return;
        }
        else
        {
            arrow.Init(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 start = RectTransformUtility.WorldToScreenPoint(
            eventData.pressEventCamera,
            cardView.GetComponent<RectTransform>().TransformPoint(cardView.GetComponent<RectTransform>().rect.center)
        );
        Vector2 end = eventData.position;
        if (arrow == null)
        {
            Debug.LogError("ArrowUI component not found!");
            return;
        }
        arrow.UpdateArrow(start, end);
        if (cardView.cardInstance == null)
        {
            Debug.LogError("CardView has no card instance");
            return;
        }
        
        var target = GetHoveredTarget();
        var sim = turnSystem.SimulateCard(turnSystem.timeline, cardView.cardInstance, GetDisplayTargets(target));
        var future = turnSystem.GetFuture(sim,10);
        ui.HighlightTargets(cardView.cardInstance.targetingMode, target);
        timelineUI.Display(future,true,GetDisplayTargets(target));
        cardView.RefreshDescription(target, false, GetDisplayTargets(target));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        group.blocksRaycasts = true;
        rect.anchoredPosition = startPos;
        timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
        if (cardView != null)
        {
            Debug.Log("Deselecting card and refreshing description");
            cardView.Deselect();
            cardView.RefreshDescription(null, false, null);
        }
        transform.localScale = Vector3.one;
        if (arrow != null) Destroy(arrow.gameObject);
        ui.Deselect();
    }

    public void Destroy()
    {
        if (arrow != null) Destroy(arrow.gameObject);
    }
    Character GetHoveredTarget()
    {
        return DropZone.hoveredCharacter;
    }
    List<Character> GetDisplayTargets(Character target)
    {
        if (cardView.cardInstance == null)
        {
            Debug.LogError("CardView has no card instance");
            return new List<Character>();
        }
        var mode = cardView.cardInstance.targetingMode;
        return combat.GetDisplayTargets(mode, target);
    }
    void Update()
    {
        if (cardView == null) Destroy(gameObject);
    }

}