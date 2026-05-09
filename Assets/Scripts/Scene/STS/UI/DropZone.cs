using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CombatManager combat;
    public Character target;

    public Image highlight;
    public Image image;

    public bool isHovered = false;

    public bool acceptsEnemyCards = false;
    public static Character hoveredCharacter;

    public void Init(CombatManager cm, Character t, bool acceptsEnemy)
    {
        combat = cm;
        target = t;
        Sprite sprite=Resources.Load<Sprite>("STS/Characters/" + target.name);
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        else
            image.gameObject.SetActive(false);
        acceptsEnemyCards = acceptsEnemy;

        highlight.color = acceptsEnemy ? 
            new Color(1, 0, 0, 0f) : 
            new Color(0, 1, 0, 0f);

        isHovered = false;
    }
    void Update()
    {
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.x);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsValidTarget(eventData))
            return;

        SetHighlight(true);
        hoveredCharacter = target;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
        hoveredCharacter = null;
        isHovered = false;
    }

    public void SetHighlight(bool highlight)
    {
        this.highlight.color = highlight ? 
            (acceptsEnemyCards ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f)) :
            (acceptsEnemyCards ? new Color(1, 0, 0, 0f) : new Color(0, 1, 0, 0f));
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isHovered) return;

        isHovered = false;

        var drag = eventData.pointerDrag?.GetComponentInParent<CardDrag>();
        drag.Destroy(); // Clean up the drag visuals
        var cardView = drag?.GetComponentInChildren<CardView>();

        if (cardView?.cardInstance == null)
        {
            Debug.LogWarning("Invalid drop");
            return;
        }

        var mode = cardView.cardInstance.data.targetingMode;

        var targets = combat.GetTargets(mode, target);

        if (targets.Count == 0)
            return;

        Vector2 discardPos = combat.animator.animationLayer.InverseTransformPoint(combat.ui.discardAnchor.position);
        combat.PlayCard(combat.player, cardView.cardInstance, targets);
    }

    bool IsValidTarget(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return false;

        var cardView = eventData.pointerDrag
            .GetComponent<CardDrag>()?
            .GetComponentInChildren<CardView>();

        if (cardView?.cardInstance?.data == null)
            return false;

        if (acceptsEnemyCards)
            return cardView.cardInstance.data.targetingMode == TargetingMode.Enemy ||
                   cardView.cardInstance.data.targetingMode == TargetingMode.AllEnemies ||
                   cardView.cardInstance.data.targetingMode == TargetingMode.AllCharacters;
        else
            return cardView.cardInstance.data.targetingMode == TargetingMode.Player ||
                   cardView.cardInstance.data.targetingMode == TargetingMode.AllCharacters;
    }
}