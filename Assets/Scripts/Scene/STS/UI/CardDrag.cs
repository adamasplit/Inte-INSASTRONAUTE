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
    bool cardPlayedByDrop;
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
        if (cardView == null || cardView.isAnimating)
        {
            return;
        }

        cardPlayedByDrop = false;
        DropZone.hoveredCharacter = null;

        cardView.isDragging = true;
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
        if (cardView == null || cardView.isAnimating)
        {
            return;
        }

        if (cardView.cardInstance == null)
        {
            Debug.LogError("CardView has no card instance");
            return;
        }

        var target = GetHoveredTarget(eventData);

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

        var sim = turnSystem.SimulateCard(turnSystem.timeline, cardView.cardInstance, GetDisplayTargets(target));
        var future = turnSystem.GetFuture(sim,10);
        ui.HighlightTargets(cardView.cardInstance.targetingMode, target);
        timelineUI.Display(future,true,GetDisplayTargets(target));
        cardView.RefreshDescription(target, false, GetDisplayTargets(target));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (cardView == null)
        {
            return;
        }

        group.blocksRaycasts = true;

        if (!cardPlayedByDrop && cardView.cardInstance != null)
        {
            Character target = GetHoveredTarget(eventData);
            TargetingMode mode = cardView.cardInstance.targetingMode;
            bool canPlayFromDropArea = IsInAllowedDropArea(mode, eventData);

            if (!canPlayFromDropArea)
            {
                // Released outside an allowed drop area for this targeting mode.
            }
            else if (RequiresExplicitTarget(mode) && target == null)
            {
                // No valid selected target at release time: do not auto-play.
            }
            else
            {
                List<Character> targets = combat.GetDisplayTargets(mode, target);
                if (targets.Count > 0)
                {
                    combat.PlayCard(combat.player, cardView.cardInstance, targets);
                    cardPlayedByDrop = true;
                }
            }
        }

        // If the card was played, it may already be animating in the animation layer.
        // In that case, avoid snapping it back to the hand position.
        if (!cardView.isAnimating)
        {
            rect.anchoredPosition = startPos;
        }

        timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
        if (cardView != null)
        {
            cardView.Deselect();
            cardView.RefreshDescription(null, false, null);
            cardView.isDragging = false;
        }
        transform.localScale = Vector3.one;
        if (arrow != null) Destroy(arrow.gameObject);
        ui.Deselect();
    }

    public void Destroy()
    {
        if (arrow != null) Destroy(arrow.gameObject);
    }

    public void NotifyCardPlayedFromDrop()
    {
        cardPlayedByDrop = true;
    }

    Character GetHoveredTarget(PointerEventData eventData)
    {
        if (eventData == null || cardView == null || cardView.cardInstance == null)
            return DropZone.GetCurrentHoveredCharacter();

        Character autoTarget = DropZone.GetAutoTarget(eventData.position, cardView.cardInstance.targetingMode, eventData.pressEventCamera);
        if (autoTarget != null)
            return autoTarget;

        return DropZone.GetCurrentHoveredCharacter();
    }

    static bool RequiresExplicitTarget(TargetingMode mode)
    {
        return mode == TargetingMode.Player ||
               mode == TargetingMode.Enemy;
    }

    static bool IsInAllowedDropArea(TargetingMode mode, PointerEventData eventData)
    {
        if (eventData == null || Screen.height <= 0)
            return false;

        float normalizedHeight = eventData.position.y / Screen.height;
        bool inEnemyThreshold = normalizedHeight >= 0.5f;
        bool inPlayerThreshold = normalizedHeight <= 0.3f;

        if (mode == TargetingMode.AllEnemies)
            return inEnemyThreshold;

        if (mode == TargetingMode.AllCharacters)
            return inEnemyThreshold || inPlayerThreshold;

        return true;
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