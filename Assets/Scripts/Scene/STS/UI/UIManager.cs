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
    // L'ecran de fin d'un duel. Laisse vide tant qu'un humain ne l'a pas pose dans
    // STS_Combat : ShowPvpResult retombe alors sur l'avis de combat.
    public PvpResultController pvpResultController;
    public RectTransform discardAnchor;
    public RectTransform deckAnchor;
    [Header("Enemy card animation")]
    public RectTransform enemyDiscardAnchor;
    public RectTransform enemyDeckAnchor;
    public CardAnimator animator;
    public TextMeshProUGUI discardCountText;
    public TextMeshProUGUI deckCountText;
    public CardSelectionController selectionController;

    [Header("Combat distant")]
    // Laissés vides tant qu'un humain ne les a pas branchés dans la scène : tout ce qui
    // les lit est null-safe, donc le PvE ne voit rien changer.
    public TextMeshProUGUI combatNoticeText;
    public TextMeshProUGUI turnCountdownText;

    [Tooltip("Voile affiché tant que le duel attend le serveur. Doit couvrir le plateau et "
        + "intercepter les clics : ce qu'il cache n'est pas encore jouable.")]
    public GameObject waitingForServerOverlay;

    [Header("Abandon d'un duel")]
    // Branchez le bouton sur OnSurrenderPressed() et, s'il existe, le bouton d'annulation
    // sur OnSurrenderCancelled(). Tout est null-safe : tant que rien n'est pose dans la
    // scene, le duel se joue exactement comme avant, sans bouton.
    public GameObject surrenderButton;
    public TextMeshProUGUI surrenderButtonLabel;
    public GameObject surrenderPrompt;
    public TextMeshProUGUI surrenderPromptText;
    Coroutine combatNoticeRoutine;
    private int pendingDrawAnimations = 0;
    /// <summary>Une dérive vue pendant une animation, à reprendre dès que la main est posée.</summary>
    private bool handSyncDeferred;
    private int pendingPlayedCardAnimations = 0;
    private readonly HashSet<CardView> playedCardViews = new();
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
        if (card == null)
            return null;

        foreach (var view in currentHandViews)
        {
            if (IsViewOfCard(view, card))
                return view;
        }
        // Also check if the card is in the animation layer (e.g., during draw or discard animations)
        if (animator == null || animator.animationLayer == null)
            return null;

        foreach (Transform child in animator.animationLayer)
        {
            CardView view = child.GetComponentInChildren<CardView>();
            if (IsViewOfCard(view, card))
                return view;
        }

        return null;
    }

    static bool IsViewOfCard(CardView view, CardInstance card)
    {
        return view != null
            && view.cardInstance != null
            && (view.cardInstance == card
                || (!string.IsNullOrEmpty(card.instanceId)
                    && view.cardInstance.instanceId == card.instanceId));
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

        if (combat.Mode == CombatMode.Pvp)
            WireRunHeaderForPvp();

        InitSurrender();
        //CreateInitialHand();
    }

    void OnDestroy()
    {
        // Rend l'entete de run a la carte, meme si le duel se termine sans passer par
        // ShowPvpResult (scene rechargee, combat interrompu, ...).
        if (RunManager.Instance != null)
            RunManager.Instance.ui?.EndPvpCombatOverride();
    }

    /// <summary>
    /// Un duel n'a pas ses propres champs d'avis/decompte/abandon dans la scene : il
    /// emprunte ceux de l'entete de run (etage/acte/sauvegarde) plutot que d'en exiger
    /// de nouveaux. Ne remplace jamais un champ qu'un humain a deja branche a la main.
    /// </summary>
    void WireRunHeaderForPvp()
    {
        RunManagerUI header = RunManager.Instance != null ? RunManager.Instance.ui : null;
        if (header == null)
            return;

        header.gameObject.SetActive(true);
        header.BeginPvpCombatOverride(combat);

        if (combatNoticeText == null)
            combatNoticeText = header.floorText;
        if (turnCountdownText == null)
            turnCountdownText = header.actText;
        if (surrenderButton == null && header.saveAndReturnToMenuButton != null)
            surrenderButton = header.saveAndReturnToMenuButton.gameObject;
        if (surrenderButtonLabel == null)
            surrenderButtonLabel = header.saveAndReturnToMenuButtonLabel;
    }

    /// <summary>
    /// Le bouton d'abandon n'existe que dans un duel.
    ///
    /// <para>Un combat de run n'a personne a qui abandonner : l'endpoint refuserait, et le
    /// bouton ne promettrait rien. On le cache donc partout ailleurs plutot que de le laisser
    /// visible et inerte.</para>
    /// </summary>
    void InitSurrender()
    {
        bool duel = combat != null && combat.Mode == CombatMode.Pvp;

        if (surrenderButton != null)
        {
            surrenderButton.SetActive(duel);
            var button = surrenderButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.interactable = duel;
        }

        if (surrenderButtonLabel != null)
            surrenderButtonLabel.text = SurrenderConfirmation.IdleLabel;

        HideSurrenderPrompt();
    }

    /// Le bouton d'abandon a ete presse. C'est le combat qui decide ce que ca veut dire :
    /// la premiere pression arme la confirmation, la seconde abandonne.
    public void OnSurrenderPressed()
    {
        combat?.RequestSurrender();
    }

    /// Le joueur se ravise.
    public void OnSurrenderCancelled()
    {
        combat?.CancelSurrender();
    }

    /// <summary>
    /// Affiche ce que l'abandon coute, avant qu'il ne soit fait.
    ///
    /// <para>Le texte le dit sans detour : le serveur regle l'abandon par <c>concede</c>, qui
    /// deplace le classement exactement comme le forfait d'un joueur absent. Sans cette phrase,
    /// le bouton laisserait croire a une sortie gratuite.</para>
    ///
    /// <para>Sans champ branche dans la scene, l'avertissement passe par l'avis de combat : un
    /// abandon ne doit jamais partir sans que le joueur ait lu ce qu'il fait.</para>
    /// </summary>
    public void ShowSurrenderPrompt(SurrenderConfirmation confirmation)
    {
        if (confirmation == null)
            return;

        if (surrenderButtonLabel != null)
            surrenderButtonLabel.text = confirmation.Label;

        if (surrenderPromptText != null)
            surrenderPromptText.text = SurrenderConfirmation.Warning;

        if (surrenderPrompt != null)
        {
            surrenderPrompt.SetActive(true);
            return;
        }

        ShowCombatNotice(SurrenderConfirmation.Warning);
    }

    /// Referme l'avertissement et rend au bouton son texte de repos.
    public void HideSurrenderPrompt()
    {
        if (surrenderPrompt != null)
            surrenderPrompt.SetActive(false);
        if (surrenderButtonLabel != null)
            surrenderButtonLabel.text = SurrenderConfirmation.IdleLabel;
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

        // PLAYERS — one dropzone per ally, same treatment as the enemy row below.
        foreach (var ally in combat.allies)
        {
            if (ally == null)
                continue;

            GameObject playerZone = playerIndex < playerZones.Count ? playerZones[playerIndex] : Instantiate(playerPrefab, playerRoot);
            playerZone.SetActive(true);
            var pUI = playerZone.GetComponent<CharacterUI>();
            pUI.SetCharacter(ally, this);

            var dz = playerZone.GetComponent<DropZone>();
            dz.Init(combat, ally, combat.IsHostileTo(combat.GetActingPlayer(), ally));
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
            dz2.Init(combat, enemy, combat.IsHostileTo(combat.GetActingPlayer(), enemy));
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

        if (!healing && !blocked)
            GetDropZone(character)?.PlayActionSprite(4);

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
    public void RefreshUI(bool refreshHand = true, bool skipHandLayout = false)
    {
        selectedCard = null;
        foreach (var ui in characterUIs)
        {
            ui.Refresh();
        }
        Character actingPlayer = combat != null ? combat.GetActingPlayer() : null;
        energyText.text = actingPlayer != null ? $"{actingPlayer.resources.energy}" : "-";
        deckCountText.text = $"{combat.deck.drawPile.Count}";
        discardCountText.text = $"{combat.deck.discardPile.Count}";

        if (!skipHandLayout)
            RefreshHandLayout();
    }

    // True while any hand card is mid-animation (draw, discard, play, etc.) — a layout run
    // during one of those would fight the animation and snap cards instead of flowing.
    public bool HandHasAnimatingCard => currentHandViews.Any(view => view != null && view.isAnimating);
    void CreateInitialHand()
    {
        currentHandViews.Clear();

        foreach (var card in combat.deck.hand)
        {
            CreateHandCard(card);
        }

        RefreshHandLayout();
    }

    public void SyncHandFromDeckState()
    {
        HideCombatCardPreview();
        selectedCard = null;

        foreach (var view in currentHandViews)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        currentHandViews.Clear();

        if (combat == null || combat.deck == null || combat.deck.hand == null)
            return;

        foreach (var card in combat.deck.hand)
        {
            if (card != null)
            {
                CreateHandCard(card);
            }
        }

        RefreshHandLayout();
    }

    /// <summary>
    /// Rebuilds the hand views only when they no longer match the deck's hand.
    /// </summary>
    /// <remarks>
    /// The authoritative state replaces <c>deck.hand</c> with freshly built CardInstance objects
    /// on every update, so a view created earlier points at an object the hand no longer holds.
    /// Left alone, the player sees a card the server has already discarded — and clicking it is
    /// refused with CARD_NOT_IN_HAND — while cards the server dealt never appear.
    /// Rebuilding unconditionally would destroy views mid-animation, so this only acts on drift.
    /// </remarks>
    public void SyncHandFromDeckStateIfDrifted()
    {
        if (combat == null || combat.deck == null || combat.deck.hand == null)
        {
            handSyncDeferred = false;
            return;
        }

        if (!HandHasDrifted())
        {
            handSyncDeferred = false;
            return;
        }

        // Une reconstruction détruit toutes les vues, celles encore en vol comprises : la main
        // se téléportait à son état final au milieu d'une pioche, pendant que les sons de la
        // pioche continuaient de jouer. La dérive est réelle et sera corrigée — mais une fois
        // les cartes posées, pas pendant leur course.
        if (HandHasAnimatingCard)
        {
            handSyncDeferred = true;
            return;
        }

        handSyncDeferred = false;
        SyncHandFromDeckState();
    }

    /// <summary>
    /// La main affichée ne correspond plus à celle que l'état décrit.
    /// </summary>
    /// <remarks>
    /// Compte les exemplaires plutôt que de comparer deux ensembles : deux vues pour une même
    /// carte se confondaient en une seule entrée, la comparaison ne voyait rien, et le doublon
    /// restait à l'écran jusqu'à ce qu'autre chose reconstruise la main.
    ///
    /// <para>L'ordre, lui, ne compte pas : le rang d'une carte est l'affaire de la disposition,
    /// et reconstruire pour un simple réagencement coûterait les vues en cours d'animation.</para>
    /// </remarks>
    bool HandHasDrifted()
    {
        Dictionary<string, int> tally = new Dictionary<string, int>(System.StringComparer.Ordinal);
        int shownCount = 0;
        foreach (CardView view in currentHandViews)
        {
            if (view == null || view.cardInstance == null
                || string.IsNullOrEmpty(view.cardInstance.instanceId))
                continue;

            string instanceId = view.cardInstance.instanceId;
            tally.TryGetValue(instanceId, out int seen);
            tally[instanceId] = seen + 1;
            shownCount++;
        }

        int heldCount = 0;
        foreach (CardInstance card in combat.deck.hand)
        {
            if (card == null || string.IsNullOrEmpty(card.instanceId))
                continue;

            if (!tally.TryGetValue(card.instanceId, out int seen) || seen == 0)
                return true;

            tally[card.instanceId] = seen - 1;
            heldCount++;
        }

        return shownCount != heldCount;
    }

    /// <summary>
    /// Reprend la reconstruction qu'une animation en cours avait fait remettre à plus tard.
    /// </summary>
    void Update()
    {
        if (!handSyncDeferred || HandHasAnimatingCard)
            return;

        SyncHandFromDeckStateIfDrifted();
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
        Character actingPlayer = combat != null ? combat.GetActingPlayer() : null;

        foreach (var zone in allZones)
        {
            bool shouldHighlight = false;

            switch (mode)
            {
                case TargetingMode.Enemy:
                    shouldHighlight = zone.target == hovered;
                    break;

                case TargetingMode.AllEnemies:
                    shouldHighlight = zone.target != null && !zone.target.isPlayer && hovered != null;
                    break;

                case TargetingMode.Player:
                    shouldHighlight = (zone.target == actingPlayer) && (hovered == actingPlayer);
                    break;

                // Une seule cible, comme Enemy : AnyPlayer choisit *un* allié, celui qu'on
                // vise. GetDisplayTargets ne rend d'ailleurs que celui-là. Cette ligne allumait
                // tous les alliés vivants — elle ne se servait de `hovered` que pour savoir
                // qu'on visait quelque chose, jamais pour savoir quoi — si bien que la carte
                // frappait bien la bonne cible mais que l'affichage en désignait quatre.
                //
                // À ne pas confondre avec AllEnemies, AllCharacters et RandomEnemy juste
                // en dessous : eux touchent, ou peuvent toucher, tout un camp, et allumer
                // tout ce camp est exactement ce qu'ils doivent faire.
                case TargetingMode.AnyPlayer:
                    shouldHighlight = zone.target != null
                        && zone.target == hovered
                        && zone.target.isPlayer
                        && zone.target.IsAlive;
                    break;

                case TargetingMode.AllCharacters:
                    shouldHighlight = (hovered!=null);
                    break;
                case TargetingMode.None:
                    shouldHighlight = false;
                    break;
                case TargetingMode.RandomEnemy:
                    shouldHighlight = zone.target != null && !zone.target.isPlayer && hovered != null;
                    break;
            }

            zone.SetHighlight(shouldHighlight);
        }
    }

    /// <summary>
    /// La vue qu'une carte entrant en main doit animer.
    /// </summary>
    /// <remarks>
    /// En crée une, sauf si cette carte en a déjà une. La reconstruction de la main et la
    /// relecture des événements arrivent toutes deux à mettre une carte en main, et la seconde
    /// ne savait pas que la première était passée : elle fabriquait une deuxième vue pour la même
    /// carte, et le joueur voyait la carte en double. Les deux disparaissaient d'un coup à la
    /// première reconstruction venue, ce qui donnait l'impression que jouer l'une défaussait
    /// l'autre.
    ///
    /// <para>Une vue déjà en vol est laissée à son animation : rien à reprendre, et la relancer
    /// la ferait repartir de la pioche au milieu de sa course. Le null qu'on rend alors dit à
    /// l'appelant qu'il n'y a rien à animer.</para>
    /// </remarks>
    CardView ViewToAnimateInto(CardInstance card)
    {
        // Uniquement parmi les vues de la main. GetView regarde aussi la couche d'animation, où
        // séjourne une carte en train d'en *sortir* : une carte défaussée puis reprise doit
        // recevoir une vue neuve, pas celle que l'animation de défausse va détruire.
        CardView existing = null;
        foreach (CardView view in currentHandViews)
        {
            if (IsViewOfCard(view, card))
            {
                existing = view;
                break;
            }
        }

        if (existing == null)
            return CreateHandCard(card);

        return existing.isAnimating ? null : existing;
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
        CardView view = ViewToAnimateInto(card);
        if (view == null)
            return;

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
        // La vue peut disparaître à chaque reprise de la coroutine : une synchronisation d'état
        // autoritative reconstruit la main en entier et détruit les vues en place, celles encore
        // en vol comprises. Reparenter un Transform détruit lève une NullReferenceException
        // depuis Transform.SetParent, la coroutine meurt, et pendingDrawAnimations n'est jamais
        // décrémenté — la pioche suivante attend alors un décalage qui ne cesse de grandir.
        if (view == null || view.rootRect == null)
            yield break;

        RectTransform rect =
            view.rootRect;

        yield return null;

        if (view == null || rect == null)
            yield break;

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

        if (view == null || rect == null)
            yield break;

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

        // A played card owns its exit animation already. Card movement events can arrive while
        // that animation is running; starting a second discard coroutine would race it and leave
        // an orphaned view behind.
        if (playedCardViews.Contains(view))
            return;

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

        playedCardViews.Remove(view);
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

        playedCardViews.Remove(view);
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



    public void ShowGameOver(List<Character> enemy)
    {
        gameOverController.Show(enemy);
    }

    /// <summary>
    /// Une phrase brève, au milieu de l'écran, qui s'efface seule. Le serveur refuse une
    /// commande et le client n'en montrait rien : la carte ne bougeait pas, sans un mot.
    /// </summary>
    public void ShowCombatNotice(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (combatNoticeText == null)
        {
            Debug.LogWarning($"[STS-COMBAT] {message} (no notice field wired in the scene)");
            return;
        }

        combatNoticeText.text = message;
        combatNoticeText.gameObject.SetActive(true);

        if (combatNoticeRoutine != null)
            StopCoroutine(combatNoticeRoutine);
        combatNoticeRoutine = StartCoroutine(HideCombatNoticeRoutine());
    }

    IEnumerator HideCombatNoticeRoutine()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        if (combatNoticeText != null)
            combatNoticeText.gameObject.SetActive(false);
        combatNoticeRoutine = null;
    }

    /// <summary>
    /// L'issue d'un duel. Rend true quand le panneau dédié l'a prise en charge — c'est
    /// alors lui qui referme la session et qui rend la main au menu, sur un clic du
    /// joueur plutôt que sur un délai.
    ///
    /// <para>Elle ne passe jamais par GameOverController : celui-ci écrit « Vous avez été
    /// vaincu par … » — faux sur une victoire — et son bouton met fin à la run
    /// (GrantRunEndUnlocks, OnRunEnd), ce qu'un duel n'a aucun droit de faire.</para>
    /// </summary>
    public bool ShowPvpResult(TeamOutcome outcome, string opponentName)
    {
        if (pvpResultController != null)
        {
            pvpResultController.Show(outcome, opponentName);
            return true;
        }

        string against = string.IsNullOrWhiteSpace(opponentName) ? "" : $" contre {opponentName}";
        string message;
        switch (outcome)
        {
            case TeamOutcome.Victory: message = $"Victoire{against} !"; break;
            case TeamOutcome.Defeat:  message = $"Défaite{against}."; break;
            case TeamOutcome.Draw:    message = $"Match nul{against}."; break;
            default:                  message = "Duel terminé."; break;
        }

        Debug.Log($"[STS-PVP] {message}");
        ShowCombatNotice(message);
        return false;
    }

    /// <summary>
    /// Les secondes qu'il reste au tour, ou rien. Le champ n'est pas branché tant qu'un
    /// humain ne l'a pas posé dans la scène, auquel cas cette méthode ne fait rien : le
    /// PvE, qui n'a pas de limite de temps, ne doit de toute façon rien voir.
    /// </summary>
    public void DisplayTurnCountdown(double? secondsRemaining)
    {
        if (turnCountdownText == null)
            return;

        if (secondsRemaining == null)
        {
            // « En attente » ne vaut que pour une attente. Un duel terminé n'a plus de tour et
            // arrivait pourtant ici : il affichait donc qu'il attendait le serveur, par-dessus
            // son propre écran de résultat.
            if (combat != null && combat.IsWaitingForServer)
            {
                if (!turnCountdownText.gameObject.activeSelf)
                    turnCountdownText.gameObject.SetActive(true);
                turnCountdownText.color = Color.white;
                turnCountdownText.text = "En attente...";
                return;
            }

            if (turnCountdownText.gameObject.activeSelf)
                turnCountdownText.gameObject.SetActive(false);
            return;
        }

        int whole = Mathf.Max(0, Mathf.CeilToInt((float)secondsRemaining.Value));
        if (!turnCountdownText.gameObject.activeSelf)
            turnCountdownText.gameObject.SetActive(true);

        turnCountdownText.text = $"{whole}s";
        turnCountdownText.color = whole <= 5 ? Color.red : Color.white;
    }
    /// <summary>
    /// Montre ou cache le voile d'attente du serveur.
    ///
    /// <para>Tant qu'aucun état autoritatif n'est arrivé, le plateau montre des points de vie
    /// de remplissage et des piles vides. Le voile est là pour couvrir ça, et il doit donc
    /// aussi intercepter les clics : ce qu'il cache n'est pas jouable, et une carte lâchée
    /// dessus partirait vers un combat qui n'a pas commencé.</para>
    ///
    /// <para>Rien n'est branché par défaut. Une scène sans voile se joue exactement comme
    /// avant — le texte « En attente... » reste alors la seule indication.</para>
    /// </summary>
    public void DisplayWaitingForServer(bool waiting)
    {
        if (waitingForServerOverlay == null)
            return;

        if (waitingForServerOverlay.activeSelf != waiting)
            waitingForServerOverlay.SetActive(waiting);
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

    playedCardViews.Add(view);
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
        bool exhaust,
        Character actor = null
    )
    {
        if (view == null)
            yield break;

        if (exhaust)
        {
            // Burn the card that was played, not a copy of it: spawning one left the original
            // alive in the animation layer, where nothing ever destroyed it.
            yield return AnimateExhaust(view);
            yield break;
        }

        yield return animator.MoveCard(
            view.rootRect,
            view.rootRect.position,
            DiscardAnchorFor(actor).position,
            1f,
            true,
            true,
            startScale: Vector3.one,
            endScale: new Vector3(0.4f, 0.4f, 1f)
        );

        playedCardViews.Remove(view);
        Destroy(view.rootRect.gameObject);
    }

    public IEnumerator AnimateCardToPile(
        CardInstance card,
        CardSelectionSource destination,
        Character actor = null
    )
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
            CardSelectionSource.DrawPile => DeckAnchorFor(actor).position,
            CardSelectionSource.DiscardPile => DiscardAnchorFor(actor).position,
            CardSelectionSource.ExhaustPile => DiscardAnchorFor(actor).position,
            CardSelectionSource.All => DeckAnchorFor(actor).position,
            CardSelectionSource.AllExceptExhaustPile => DeckAnchorFor(actor).position,
            _ => DeckAnchorFor(actor).position
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

    RectTransform DeckAnchorFor(Character actor)
    {
        return actor != null && !actor.isPlayer && enemyDeckAnchor != null
            ? enemyDeckAnchor
            : deckAnchor;
    }

    RectTransform DiscardAnchorFor(Character actor)
    {
        return actor != null && !actor.isPlayer && enemyDiscardAnchor != null
            ? enemyDiscardAnchor
            : discardAnchor;
    }
    public void AddCardAnimated(CardInstance card)
    {
        CardView view = ViewToAnimateInto(card);
        if (view == null)
            return;

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
        try
        {
            if (staggerIndex > 0)
            {
                yield return new WaitForSeconds(0.05f * staggerIndex);
            }
            if (view == null)
                yield break;

            SFXManager.Instance?.PlaySound("Draw");

            yield return AnimateDraw(view, startPosition, speedMultiplier, arcAwayFromTarget);
        }
        finally
        {
            // Rendu quoi qu'il arrive. Ce compteur est le décalage entre deux cartes piochées :
            // s'il n'est pas repris quand l'animation s'interrompt, chaque pioche suivante attend
            // un peu plus longtemps, jusqu'à ce que les cartes n'apparaissent plus du tout.
            pendingDrawAnimations = Mathf.Max(0, pendingDrawAnimations - 1);
        }
    }
    public void TransformCard(CardInstance oldCard, CardInstance newCard)
    {
        if (oldCard == null || newCard == null)
            return;

        oldCard.data = newCard.data;
        oldCard.displayName = newCard.displayName;
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