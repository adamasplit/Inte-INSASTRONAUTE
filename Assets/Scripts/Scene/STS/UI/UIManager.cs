using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
    public void SelectCard(CardView card)
    {
        if (selectedCard == card)
        {
            Deselect();
            return;
        }

        selectedCard = card;
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

    public void Deselect()
    {
        selectedCard = null;
        RefreshHandLayout();
    }
    public void Init(CombatManager cm)
    {
        combat = cm;
        InitCharacters();
        combat.deck.OnCardDrawn -= DrawCardAnimated;
        combat.deck.OnCardDiscarded -= DiscardCardAnimated;
        combat.deck.OnCardExhausted -= ExhaustCardAnimated;

        combat.deck.OnCardDrawn += DrawCardAnimated;
        combat.deck.OnCardDiscarded += DiscardCardAnimated;
        combat.deck.OnCardExhausted += ExhaustCardAnimated;
        //CreateInitialHand();
    }

    public void InitCharacters()
    {
        characterUIs.Clear();
        allZones.Clear();
        foreach(Transform child in playerRoot)
            Destroy(child.gameObject);
        foreach(Transform child in enemyRoot)
            Destroy(child.gameObject);
        // PLAYER
        if (combat.player != null)
        {
            var playerZone = Instantiate(playerPrefab, playerRoot);
            var pUI = playerZone.GetComponent<CharacterUI>();
            pUI.SetCharacter(combat.player);

            var dz = playerZone.GetComponent<DropZone>();
            dz.Init(combat, combat.player, false);
            allZones.Add(dz);
            characterUIs.Add(pUI);
        }
        // ENEMIES
        foreach (var enemy in combat.enemies)
        {
            var zone = Instantiate(enemyPrefab, enemyRoot);

            var dz2 = zone.GetComponent<DropZone>();
            dz2.Init(combat, enemy, true);
            var eUI = zone.GetComponent<CharacterUI>();
            eUI.SetCharacter(enemy);
            characterUIs.Add(eUI);
            allZones.Add(dz2);
        }
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
    public void RefreshUI(bool refreshHand = true)
    {
        selectedCard = null;
        foreach (var ui in characterUIs)
        {
            ui.Refresh();
        }
        energyText.text = combat.player != null ? $"{combat.player.resources.energy}" : "-";
    

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

    public void DrawCardAnimated(CardInstance card)
    {
        CardView view = CreateHandCard(card);

        RectTransform rect =
            view.rootRect;

        rect.SetParent(animator.animationLayer, false);

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

        RefreshHandLayout();

        Vector2 targetLocal =
            handLayout.GetTargetPosition(view);

        Vector3 target =
            handPanel.TransformPoint(targetLocal);

        rect.SetParent(animator.animationLayer, true);

        rect.position = deckAnchor.position;

        yield return animator.MoveCard(
            rect,
            rect.position,
            target
        );

        rect.SetParent(handPanel, true);

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

        rect.SetParent(
            animator.animationLayer,
            true
        );

        Vector2 center = Vector2.zero;

        yield return animator.MoveCard(
            rect,
            rect.position,
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
    public void RemoveView(CardView view)
    {
        currentHandViews.Remove(view);

        if (selectedCard == view)
            selectedCard = null;

        RefreshHandLayout();
    }
}