using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CombatManager combat;
    public Character target;

    public Image highlight;
    public Image image;

    public bool isHovered = false;

    public bool acceptsEnemyCards = false;
    public static Character hoveredCharacter;
    bool deathAnimationPlayed;

    public void Init(CombatManager cm, Character t, bool acceptsEnemy)
    {
        combat = cm;
        target = t;
        Sprite sprite=Resources.Load<Sprite>("STS/Characters/" + target.name);
        if (sprite != null)
        {
            image.gameObject.SetActive(true);
            image.sprite = sprite;
        }
        else
        {
            image.gameObject.SetActive(false);
        }
        acceptsEnemyCards = acceptsEnemy;
        deathAnimationPlayed = false;

        highlight.color = acceptsEnemy ? 
            new Color(1, 0, 0, 0f) : 
            new Color(0, 1, 0, 0f);

        isHovered = false;
    }
    void Update()
    {
        float targetSize=Mathf.Min(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.y, 500);
        if (target.isPlayer)
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 800);
        else
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSize, targetSize);
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

        var mode = cardView.cardInstance.targetingMode;

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
            return cardView.cardInstance.targetingMode == TargetingMode.Enemy ||
                   cardView.cardInstance.targetingMode == TargetingMode.AllEnemies ||
                   cardView.cardInstance.targetingMode == TargetingMode.AllCharacters||
                   cardView.cardInstance.targetingMode == TargetingMode.RandomEnemy;
        else
            return cardView.cardInstance.targetingMode == TargetingMode.Player ||
                   cardView.cardInstance.targetingMode == TargetingMode.AllCharacters;
    }
    public IEnumerator FlashWhite()
    {
        Color originalColor = image.color;
        RectTransform imageRect = image.rectTransform;
        Vector2 originalPosition = imageRect.anchoredPosition;
        float moveDistance = 50f;
        Vector2 targetPosition = originalPosition + (target != null && target.isPlayer ? Vector2.zero : Vector2.down) * moveDistance;
        float duration = 0.2f;
        float punchDuration = duration * 0.35f;

        image.color = Color.white;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float colorT = Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(Color.white, originalColor, colorT);

            float positionT;
            if (elapsed <= punchDuration)
            {
                positionT = Mathf.Clamp01(elapsed / punchDuration);
                imageRect.anchoredPosition = Vector2.Lerp(originalPosition, targetPosition, positionT);
            }
            else
            {
                positionT = Mathf.Clamp01((elapsed - punchDuration) / (duration - punchDuration));
                imageRect.anchoredPosition = Vector2.Lerp(targetPosition, originalPosition, positionT);
            }
            yield return null;
        }
        image.color = originalColor;
        imageRect.anchoredPosition = originalPosition;
    }

    public IEnumerator PlayDeathAnimation(float duration = 0.65f)
    {
        if (deathAnimationPlayed || image == null || !image.gameObject.activeInHierarchy)
            yield break;

        deathAnimationPlayed = true;

        RectTransform imageRect = image.rectTransform;
        Vector2 originalPosition = imageRect.anchoredPosition;
        Vector3 originalScale = imageRect.localScale;
        Color originalColor = image.color;

        float elapsed = 0f;
        float flashDuration = Mathf.Min(0.18f, duration * 0.35f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fadeT = Mathf.SmoothStep(0f, 1f, t);
            float flashT = flashDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / flashDuration);
            float flash = flashT < 0.5f ? flashT * 2f : (1f - flashT) * 2f;

            imageRect.localScale = Vector3.Lerp(originalScale, originalScale * 0.1f, t);
            imageRect.anchoredPosition = originalPosition + new Vector2(0f, -24f * fadeT);

            Color tinted = Color.Lerp(originalColor, Color.white, flash * 0.75f);
            tinted.a = originalColor.a * (1f - fadeT);
            image.color = tinted;

            yield return null;
        }

        imageRect.localScale = originalScale * 0.1f;
        imageRect.anchoredPosition = originalPosition + new Vector2(0f, -24f);
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }
}