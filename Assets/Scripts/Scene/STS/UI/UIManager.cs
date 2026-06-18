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
    public HandLayoutController handLayout;
    public List<DropZone> allZones = new();
    public CardView selectedCard;
    public GameOverController gameOverController;
    public RectTransform discardAnchor;
    public RectTransform deckAnchor;
    public CardAnimator animator;
    public TextMeshProUGUI discardCountText;
    public TextMeshProUGUI deckCountText;
    public CardSelectionController selectionController;
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
    public void SelectCard(CardView card)
    {
        if (selectedCard == card)
        {
            Deselect();
            return;
        }
        if (selectedCard != null) selectedCard.Deselect();
        selectedCard = card;
        card.Select(handLayout.cardSide(card));
        RefreshHandLayout();
    }

    public CardView GetView(CardInstance card)
    {
        foreach (var view in currentHandViews)
        {
            if (view.cardInstance == card)
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
        if (selectedCard != null)
        {
            selectedCard.Deselect();
            selectedCard = null;
            RefreshHandLayout();
        }
    }
    public void Init(CombatManager cm)
    {
        combat = cm;
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

        rect.position = deckAnchor.position;

        view.isAnimating = true;

        StartCoroutine(
            AnimateDraw(view)
        );
    }

    IEnumerator AnimateDraw(CardView view)
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

        rect.position = deckAnchor.position;

        yield return animator.MoveCard(
            rect,
            rect.position,
            target
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

        if (view == null)
            return;

        currentHandViews.Remove(view);

        StartCoroutine(
            AnimateExhaust(view)
        );

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
            discardAnchor.position
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

        Vector3 center = Vector3.zero;

        yield return animator.MoveCard(
            rect,
            rect.position,
            center
        );

        yield return new WaitForSeconds(0.15f);

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
    view.isAnimating = true;

    RectTransform rect = view.rootRect;

    Vector3 startPos = rect.position;

    rect.SetParent(
        animator.animationLayer,
        true
    );

    rect.position = startPos;

    Canvas.ForceUpdateCanvases();

    Vector3 center =
        animator.animationLayer.TransformPoint(Vector3.zero);

    yield return animator.MoveCard(
        rect,
        startPos,
        center,
        3f,
        false,
        true
    );
}
    public IEnumerator AnimateCardToDiscard(
        CardView view,
        bool exhaust
    )
    {
        if (exhaust)
        {
            Destroy(view.rootRect.gameObject);
            yield break;
        }

        yield return animator.MoveCard(
            view.rootRect,
            view.rootRect.position,
            discardAnchor.position,
            1f,
            true,
            true
        );

        Destroy(view.rootRect.gameObject);
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

        StartCoroutine(
            AnimateDraw(view)
        );
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
            view.SetCard(oldCard);
            view.RefreshDescription(null, true);
        }
    }
    public void RemoveView(CardView view)
    {
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