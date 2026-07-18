using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public CombatManager combat;

    public TextMeshProUGUI energyText;

    public Transform playerRoot;
    public Transform enemyRoot;

    public GameObject characterUIPrefab;
    public GameObject enemyPrefab;
    public GameObject playerPrefab;

    List<CharacterUI> characterUIs = new();
    private List<CardView> currentHandViews = new();

    public Transform handPanel;
    public GameObject cardButtonPrefab;
    public GameObject DamagePopupPrefab;
    public HandLayoutController handLayout;
    public List<DropZone> allZones = new();
    public CardView selectedCard;
    private GameObject combatPreviewCardObject;
    public GameOverController gameOverController;
    public RectTransform discardAnchor;
    public RectTransform deckAnchor;
    public CardAnimator animator;
    public TextMeshProUGUI discardCountText;
    public TextMeshProUGUI deckCountText;
    public CardSelectionController selectionController;
    private int pendingDrawAnimations = 0;
    private int pendingPlayedCardAnimations = 0;
    public Image backgroundImage;
    public bool IsSelectingCards()
    {
        return selectionController.Active;
    }
    public IEnumerator RequestCardSelection(
        CardSelectionRequest request,
        System.Action<List<CardInstance>> onConfirm
    )
    {
        List<CardView> hand = currentHandViews
            .FindAll(v => v != null && request.filter(v.cardInstance));

        if (hand.Count <= request.amount)
        {
            request.selectedCards = hand
                .ConvertAll(v => v.cardInstance);

            onConfirm?.Invoke(request.selectedCards);

            ResetSelectionVisuals();
            yield break;
        }

        // UI flow normal
        selectionController.Open(request);

        yield return selectionController.WaitForSelection();

        onConfirm?.Invoke(request.selectedCards);

        ResetSelectionVisuals();
    }
    private void ResetSelectionVisuals()
    {
        foreach (var view in currentHandViews)
        {
            if (view != null)
                view.selectionPreview = false;
        }

        RefreshHandLayout();
    }
    public void HideAllTooltips()
    {
        foreach (var view in currentHandViews)
        {
            if (view != null)
                view.HideCardTooltips();
        }

        if (selectedCard != null)
            selectedCard.HideCardTooltips();

        TooltipManager.Instance?.HideTooltip();
    }
    public void SelectCard(CardView card, bool force = false)
    {
        if (!force && selectedCard == card&&!card.isDragging)
        {
            Deselect();
            return;
        }
        HideAllTooltips();
        HideCombatCardPreview();
        if (selectedCard != null&&!selectedCard.isDragging) selectedCard.Deselect();
        selectedCard = card;
        card.Select(handLayout.cardSide(card));
        RefreshHandLayout();
    }

    public void ShowCombatCardPreview(CardView sourceCard)
    {
        if (sourceCard == null || sourceCard.cardInstance == null || animator == null || animator.animationLayer == null)
            return;

        HideCombatCardPreview();

        CardView previewView = CreateCardView(sourceCard.cardInstance, false, sourceCard.rootRect.position);
        if (previewView == null)
            return;

        combatPreviewCardObject = previewView.gameObject;

        RectTransform previewRect = previewView.rootRect;
        if (previewRect == null)
            previewRect = previewView.GetComponent<RectTransform>();

        if (previewRect == null)
            return;

        previewRect.SetAsLastSibling();

        CanvasGroup previewGroup = combatPreviewCardObject.GetComponent<CanvasGroup>();
        if (previewGroup == null)
            previewGroup = combatPreviewCardObject.AddComponent<CanvasGroup>();
        previewGroup.interactable = false;
        previewGroup.blocksRaycasts = false;

        CardDrag previewDrag = combatPreviewCardObject.GetComponent<CardDrag>();
        if (previewDrag != null)
            previewDrag.enabled = false;

        Vector3 center = animator.animationLayer.TransformPoint(Vector3.zero);
        Vector3 startScale = previewRect.localScale;
        Vector3 targetScale = sourceCard.rootRect != null ? sourceCard.rootRect.localScale * 3f : startScale * 3f;

        previewView.isAnimating = true;
        StartCoroutine(AnimateCombatCardPreview(previewRect, center, startScale, targetScale, previewView));
    }

    private IEnumerator AnimateCombatCardPreview(RectTransform previewRect, Vector3 center, Vector3 startScale, Vector3 targetScale, CardView previewView)
    {
        if (animator == null)
            yield break;

        yield return animator.MoveCard(
            previewRect,
            previewRect.position,
            center,
            1f,
            true,
            true,
            startScale: startScale,
            endScale: targetScale,
            endRotation: Quaternion.identity
        );

        if (previewView != null)
            previewView.isAnimating = false;
    }

    public void HideCombatCardPreview()
    {
        if (combatPreviewCardObject != null)
        {
            Destroy(combatPreviewCardObject);
            combatPreviewCardObject = null;
        }
    }

    public CardView GetView(CardInstance card)
    {
        foreach (var view in currentHandViews)
        {
            if (view.cardInstance == card)
                return view;
        }
        // Also check if the card is in the animation layer (e.g., during draw or discard animations)
        foreach (Transform child in animator.animationLayer)
        {
            CardView view = child.GetComponentInChildren<CardView>();
            if (view != null && view.cardInstance == card)
                return view;
        }

        return null;
    }
    public CardView CreateCardView(CardInstance card, bool addToHand = true, Vector3? startWorldPosition = null)
    {
        Transform parent = addToHand ? handPanel : animator.animationLayer;
        GameObject obj = Instantiate(cardButtonPrefab, parent);
        if (!addToHand)
            obj.SetActive(false);

        CardView view = obj.GetComponentInChildren<CardView>();

        if (!addToHand && startWorldPosition.HasValue)
            view.rootRect.position = startWorldPosition.Value;

        view.SetCard(card);

        if (addToHand)
            currentHandViews.Add(view);

        if (!addToHand)
            obj.SetActive(true);

        return view;
    }

    public void Deselect()
    {
        Debug.Log("Deselecting card {" + selectedCard?.cardInstance?.displayName + "}");
        HideAllTooltips();
        if (selectedCard != null)
        {
            selectedCard.Deselect();
            selectedCard = null;
            HideCombatCardPreview();
            RefreshHandLayout();
        }
    }
    public void Init(CombatManager cm)
    {
        combat = cm;
        int act = Mathf.Min(RunManager.Instance != null ? RunManager.Instance.act+1 : 1,4);
        backgroundImage.sprite = Resources.Load<Sprite>($"STS/Backgrounds/BG{act}");
        InitCharacters();
        combat.deck.OnCardDrawn -= DrawCardAnimated;
        combat.deck.OnCardDiscarded -= DiscardCardAnimated;
        combat.deck.OnCardExhausted -= ExhaustCardAnimated;
        combat.deck.OnCardAddedToHand -= AddCardAnimated;

        combat.deck.OnCardDrawn += DrawCardAnimated;
        combat.deck.OnCardDiscarded += DiscardCardAnimated;
        combat.deck.OnCardExhausted += ExhaustCardAnimated;
        combat.deck.OnCardAddedToHand += AddCardAnimated;
        //CreateInitialHand();
    }

    public void InitCharacters()
    {
        characterUIs.Clear();
        allZones.Clear();

        var playerZones = new List<GameObject>();
        foreach (Transform child in playerRoot)
            playerZones.Add(child.gameObject);

        var enemyZones = new List<GameObject>();
        foreach (Transform child in enemyRoot)
            enemyZones.Add(child.gameObject);

        int playerIndex = 0;
        int enemyIndex = 0;

        // PLAYER
        if (combat.player != null)
        {
            GameObject playerZone = playerIndex < playerZones.Count ? playerZones[playerIndex] : Instantiate(playerPrefab, playerRoot);
            playerZone.SetActive(true);
            var pUI = playerZone.GetComponent<CharacterUI>();
            pUI.SetCharacter(combat.player, this);

            var dz = playerZone.GetComponent<DropZone>();
            dz.Init(combat, combat.player, false);
            allZones.Add(dz);
            characterUIs.Add(pUI);
            playerIndex++;
        }
        // ENEMIES
        foreach (var enemy in combat.enemies)
        {
            GameObject zone = enemyIndex < enemyZones.Count ? enemyZones[enemyIndex] : Instantiate(enemyPrefab, enemyRoot);
            zone.SetActive(true);

            var dz2 = zone.GetComponent<DropZone>();
            dz2.Init(combat, enemy, true);
            var eUI = zone.GetComponent<CharacterUI>();
            eUI.SetCharacter(enemy, this);
            characterUIs.Add(eUI);
            allZones.Add(dz2);
            enemyIndex++;
        }

        for (int i = playerIndex; i < playerZones.Count; i++)
            playerZones[i].SetActive(false);

        for (int i = enemyIndex; i < enemyZones.Count; i++)
            enemyZones[i].SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(playerRoot as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(enemyRoot as RectTransform);
    }
    public Transform GetView(Character character)
    {
        foreach (var ui in characterUIs)
        {
            if (ui.character == character)
                return ui.transform;
        }
        return null;
    }

    public DropZone GetDropZone(Character character)
    {
        foreach (var zone in allZones)
        {
            if (zone != null && zone.target == character)
                return zone;
        }

        return null;
    }

    public void ShowDamagePopup(Character character, int amount, bool healing = false, bool blocked = false)
    {
        if (character == null || amount <= 0 || animator == null || animator.animationLayer == null || DamagePopupPrefab == null)
            return;

        GameObject popupObject = Instantiate(DamagePopupPrefab, animator.animationLayer, false);
        popupObject.transform.SetAsLastSibling();

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            Vector3 startPosition = new Vector3(Random.Range(-10f, 10f), 24f, 0f);
            
            DropZone zone = GetDropZone(character);
            if (zone != null)
            {
                RectTransform zoneRect = zone.GetComponent<RectTransform>();
                RectTransform animLayerRect = animator.animationLayer as RectTransform;
                if (zoneRect != null && animLayerRect != null)
                {
                    Vector3 zoneWorldPos = zoneRect.TransformPoint(Vector3.zero);
                    Vector3 localPos = animLayerRect.InverseTransformPoint(zoneWorldPos);
                    startPosition = new Vector3(localPos.x + Random.Range(-10f, 10f), localPos.y + 24f, localPos.z);
                }
            }
            
            popupRect.localPosition = startPosition;
        }

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Play(amount, healing, blocked);
        }
    }

    public IEnumerator AnimateCharacterDeath(Character character)
    {
        DropZone zone = GetDropZone(character);
        if (zone == null)
            yield break;

        yield return zone.PlayDeathAnimation();
    }
    public void RefreshUI(bool refreshHand = true)
    {
        Debug.Log("Refreshing UI");
        selectedCard = null;
        foreach (var ui in characterUIs)
        {
            ui.Refresh();
        }
        energyText.text = combat.player != null ? $"{combat.player.resources.energy}" : "-";
        deckCountText.text = $"{combat.deck.drawPile.Count}";
        discardCountText.text = $"{combat.deck.discardPile.Count}";

        RefreshHandLayout();
    }
    void CreateInitialHand()
    {
        currentHandViews.Clear();

        foreach (var card in combat.deck.hand)
        {
            CreateHandCard(card);
        }

        RefreshHandLayout();
    }
    public void RefreshHandLayout()
    {
        currentHandViews.RemoveAll(v => v == null);

        handLayout.selectedCard = selectedCard;

        handLayout.Arrange(currentHandViews);
        foreach (var view in currentHandViews)
        {
            view.SetCard(view.cardInstance);
            // Force refresh description to ensure context is up-to-date after any state change
            view.RefreshDescription(null, true);
        }
    }

    public void HighlightTargets(TargetingMode mode, Character hovered)
    {
        foreach (var zone in allZones)
        {
            bool shouldHighlight = false;

            switch (mode)
            {
                case TargetingMode.Enemy:
                    shouldHighlight = zone.target == hovered;
                    break;

                case TargetingMode.AllEnemies:
                    shouldHighlight = zone.target != combat.player&& hovered != null;
                    break;

                case TargetingMode.Player:
                    shouldHighlight = (zone.target == combat.player) && (hovered == combat.player);
                    break;

                case TargetingMode.AllCharacters:
                    shouldHighlight = (hovered!=null);
                    break;
                case TargetingMode.None:
                    shouldHighlight = false;
                    break;
                case TargetingMode.RandomEnemy:
                    shouldHighlight = zone.target != combat.player&& hovered != null;
                    break;
            }

            zone.SetHighlight(shouldHighlight);
        }
    }

    public CardView CreateHandCard(CardInstance card)
    {
        GameObject obj = Instantiate(cardButtonPrefab, handPanel);

        CardView view = obj.GetComponentInChildren<CardView>();

        view.SetCard(card);

        currentHandViews.Add(view);

        return view;
    }
    public static void ReparentKeepScreenPosition(
    RectTransform rect,
    Transform newParent
    )
    {
        Vector3 pos = rect.position;
        Quaternion rot = rect.rotation;
        Vector3 scale = rect.lossyScale;

        rect.SetParent(newParent, true);

        rect.position = pos;
        rect.rotation = rot;

        rect.localScale = Vector3.one;
    }
    public void DrawCardAnimated(CardInstance card)
    {
        CardView view = CreateHandCard(card);

        RectTransform rect =
            view.rootRect;

        rect.SetParent(animator.animationLayer, false);
        ReparentKeepScreenPosition(rect, animator.animationLayer);

        Vector3 startPosition = deckAnchor.position;
        rect.position = startPosition;

        view.isAnimating = true;

        int staggerIndex = pendingDrawAnimations++;
        StartCoroutine(AnimateDrawWithStagger(view, startPosition, staggerIndex, 1f, false));
    }

    IEnumerator AnimateDraw(CardView view, Vector3 startPosition, float speedMultiplier, bool arcAwayFromTarget)
    {
        RectTransform rect =
            view.rootRect;

        yield return null;

        rect.SetParent(handPanel, true);
        ReparentKeepScreenPosition(rect, handPanel);

        RefreshHandLayout();

        Vector2 targetLocal =
            handLayout.GetTargetPosition(view);

        Vector3 target =
            handPanel.TransformPoint(targetLocal);

        rect.SetParent(animator.animationLayer, true);
        ReparentKeepScreenPosition(rect, animator.animationLayer);

        rect.position = startPosition;

        yield return animator.MoveCard(
            rect,
            startPosition,
            target,
            speedMultiplier,
            true,
            true,
            startScale: new Vector3(0.4f, 0.4f, 1f),
            endScale: new Vector3(1f, 1f, 1f),
            arcAwayFromTarget: arcAwayFromTarget,
            arcAwayDistance: 4f
        );

        rect.SetParent(handPanel, true);
        ReparentKeepScreenPosition(rect, handPanel);

        rect.position = target;

        view.isAnimating = false;

        RefreshHandLayout();
    }

    public void DiscardCardAnimated(CardInstance card)
    {
        CardView view = GetView(card);

        if (view == null)
            return;

        currentHandViews.Remove(view);

        StartCoroutine(
            AnimateDiscard(view)
        );

        RefreshHandLayout();
    }
    public void ExhaustCardAnimated(CardInstance card)
    {
        CardView view = GetView(card);

        if (view != null)
        {
            currentHandViews.Remove(view);
            StartCoroutine(AnimateExhaust(view));
        }
        else
        {
            StartCoroutine(AnimateExhaust(card));
        }

        RefreshHandLayout();
    }
    IEnumerator AnimateDiscard(CardView view)
    {
        RectTransform rect =
            view.rootRect;

        view.isAnimating = true;

        rect.SetParent(animator.animationLayer, true);
        ReparentKeepScreenPosition(rect, animator.animationLayer);

        yield return animator.MoveCard(
            rect,
            rect.position,
            discardAnchor.position,
            1f,
            true,
            true,
            startScale: Vector3.one,
            endScale: new Vector3(0.4f, 0.4f, 1f)
        );

        Destroy(view.gameObject);
    }
    IEnumerator AnimateExhaust(CardView view)
    {
        RectTransform rect =
            view.rootRect;

        view.isAnimating = true;

        rect.SetParent(animator.animationLayer, true);
        ReparentKeepScreenPosition(rect, animator.animationLayer);

        yield return view.PlayExhaustAnimation();

        Destroy(view.gameObject);
    }

    IEnumerator AnimateExhaust(CardInstance card)
    {
        CardView view = CreateCardView(card, false, animator.animationLayer.TransformPoint(Vector3.zero));
        if (view == null)
            yield break;

        view.isAnimating = true;
        view.rootRect.SetAsLastSibling();

        yield return view.PlayExhaustAnimation();

        Destroy(view.gameObject);
    }



    public void ShowGameOver(Character enemy)
    {
        gameOverController.Show(enemy);
    }
    Vector2 ScreenToHandLocal(Vector3 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handPanel as RectTransform,
            screenPos,
            null,
            out Vector2 local
        );

        return local;
    }
    Vector3 HandLocalToScreen(Vector2 local)
    {
        return (handPanel as RectTransform).TransformPoint(local);
    }

public IEnumerator AnimateCardToCenter(CardView view)
{
    if (view == null || view.rootRect == null)
        yield break;

    view.isAnimating = true;
    int queueIndex = pendingPlayedCardAnimations++;

    RectTransform rect = view.rootRect;

    Vector3 startPos = rect.position;

    rect.SetParent(
        animator.animationLayer,
        true
    );
    rect.SetAsLastSibling();

    rect.position = startPos;

    Canvas.ForceUpdateCanvases();

    Vector3 queueOffset = new Vector3(
        queueIndex * 18f,
        -queueIndex * 9f,
        0f
    );
    Vector3 center =
        animator.animationLayer.TransformPoint(queueOffset);

    float tilt = Random.Range(-5f, 5f);

    yield return animator.MoveCard(
        rect,
        startPos,
        center,
        3f,
        false,
        true,
        endRotation: Quaternion.Euler(0f, 0f, tilt)
    );

    pendingPlayedCardAnimations = Mathf.Max(0, pendingPlayedCardAnimations - 1);
}
    public IEnumerator AnimateCardToDiscard(
        CardView view,
        bool exhaust
    )
    {
        if (exhaust)
        {
            yield return AnimateExhaust(view.cardInstance);
            yield break;
        }

        yield return animator.MoveCard(
            view.rootRect,
            view.rootRect.position,
            discardAnchor.position,
            1f,
            true,
            true,
            startScale: Vector3.one,
            endScale: new Vector3(0.4f, 0.4f, 1f)
        );

        Destroy(view.rootRect.gameObject);
    }

    public IEnumerator AnimateCardToPile(CardInstance card, CardSelectionSource destination)
    {
        if (card == null)
            yield break;

        Vector3 startWorldPosition = animator.animationLayer.TransformPoint(Vector3.zero);
        CardView view = CreateCardView(card, false, startWorldPosition);
        if (view == null)
            yield break;

        RectTransform rect = view.rootRect;
        rect.SetParent(animator.animationLayer, true);
        ReparentKeepScreenPosition(rect, animator.animationLayer);
        rect.position = startWorldPosition;

        Vector3 targetPosition = destination switch
        {
            CardSelectionSource.DrawPile => deckAnchor.position,
            CardSelectionSource.DiscardPile => discardAnchor.position,
            CardSelectionSource.ExhaustPile => discardAnchor.position,
            CardSelectionSource.All => deckAnchor.position,
            CardSelectionSource.AllExceptExhaustPile => deckAnchor.position,
            _ => deckAnchor.position
        };

        yield return StartCoroutine(animator.MoveCard(
            rect,
            startWorldPosition,
            targetPosition,
            speedMultiplier: 0.6f,
            curved: true,
            forceRotation: false,
            startScale: new Vector3(2.5f, 2.5f, 1f),
            endScale: new Vector3(0.4f, 0.4f, 1f),
            arcAwayFromTarget: true,
            arcAwayDistance: 4f
        ));

        Destroy(view.gameObject);
    }
    public void AddCardAnimated(CardInstance card)
    {
        CardView view = CreateHandCard(card);

        RectTransform rect =
            view.rootRect;

        rect.SetParent(animator.animationLayer, false);
        ReparentKeepScreenPosition(rect, animator.animationLayer);

        Vector3 center = animator.animationLayer.TransformPoint(Vector3.zero);
        rect.position = center;

        view.isAnimating = true;

        int staggerIndex = pendingDrawAnimations++;
        StartCoroutine(AnimateDrawWithStagger(view, center, staggerIndex, 0.8f, true));
    }

    IEnumerator AnimateDrawWithStagger(CardView view, Vector3 startPosition, int staggerIndex, float speedMultiplier, bool arcAwayFromTarget)
    {
        if (staggerIndex > 0)
        {
            yield return new WaitForSeconds(0.05f * staggerIndex);
        }
        SFXManager.Instance.PlaySound("Draw");

        yield return AnimateDraw(view, startPosition, speedMultiplier, arcAwayFromTarget);

        pendingDrawAnimations = Mathf.Max(0, pendingDrawAnimations - 1);
    }
    public void TransformCard(CardInstance oldCard, CardInstance newCard)
    {
        if (oldCard == null || newCard == null)
            return;

        oldCard.data = newCard.data;
        oldCard.targetingMode = newCard.targetingMode;
        oldCard.baseModifiers.Clear();
        oldCard.addedModifiers.Clear();
        oldCard.enchantments.Clear();
        oldCard.addedEffects.Clear();

        CardView view = GetView(oldCard);
        if (view != null)
        {
            view.SetCard(newCard);
            view.RefreshDescription(null, true);
        }
    }
    public void RemoveView(CardView view)
    {
        Debug.Log("Removing card view: " + view.cardInstance?.displayName);
        currentHandViews.Remove(view);

        if (selectedCard == view)
            selectedCard = null;

        RefreshHandLayout();
    }
    public void ShowCardsInDiscard()
    {
        List<CardInstance> discardCards = combat.deck.discardPile;
        RunManager.Instance.ui.deckGridPanel.Show(discardCards,"Défausse");
    }
    public void ShowCardsInDeck()
    {
        List<CardInstance> deckCards = combat.deck.drawPile;
        RunManager.Instance.ui.deckGridPanel.Show(deckCards,"Pioche");
    }
    public IEnumerator EnergyTextGlowRed()
    {
        Color original = Color.white;
        energyText.color = Color.red;
        float elapsed = 0f;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            energyText.color = Color.Lerp(Color.red, original, elapsed / duration);
            yield return null;
        }
    }
}