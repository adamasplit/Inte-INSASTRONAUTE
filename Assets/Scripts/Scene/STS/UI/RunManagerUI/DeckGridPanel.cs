using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DeckGridPanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public GridLayoutGroup gridLayout;
    public Transform gridContainer;
    public GameObject cardGridItemPrefab;
    
    [Header("Preview")]
    public Transform previewContainer;
    public GameObject previewCardPrefab;
    public GameObject previewPanel;
    public CanvasGroup previewCanvasGroup;
    
    [Header("Controls")]
    public Button closeButton;
    public CanvasGroup panelCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float entranceDuration = 0.25f;
    [SerializeField] private float entranceOffset = 180f;
    [SerializeField] private bool enableEntranceAnimation = false;

    [Header("Content Padding")]
    [SerializeField] private float contentPadding = 48f;

    [Header("Depth")]
    [SerializeField] private bool normalizeCardDepth = true;
    [SerializeField] private float cardLocalZ = 0f;

    private CardGridItemView selectedItemView;
    /// <summary>
    /// Vrai quand le panneau n'est ouvert que pour agrandir une carte venue d'ailleurs.
    ///
    /// <para>Dans cet etat il n'y a ni deck, ni titre, ni bouton de fermeture : seulement le
    /// fond et la carte au centre. Refermer l'apercu referme alors le panneau entier, alors
    /// qu'en usage normal cela rend simplement la main a la grille.</para>
    /// </summary>
    private bool zoomOnly;
    /// Ce que le mode agrandissement-seul a eteint, et qu'il rallumera en sortant.
    private readonly List<GameObject> hiddenByZoomOnly = new List<GameObject>();
    private GameObject animatingCardObj;
    private bool isAnimating = false;
    private bool refreshQueued = false;
    private GridLayoutGroup.Constraint initialGridConstraint;
    private int initialGridConstraintCount;

    void Awake()
    {
        if (gridLayout != null)
        {
            initialGridConstraint = gridLayout.constraint;
            initialGridConstraintCount = gridLayout.constraintCount;
        }
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(List<CardInstance> deck,string name)
    {
        // Une ouverture normale annule un eventuel mode agrandissement-seul, sans quoi le
        // panneau afficherait le deck sans son titre ni son bouton de fermeture.
        SetZoomOnly(false);

        titleText.text = name;

        gameObject.SetActive(true);
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // Clear existing grid items
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        selectedItemView = null;

        // Create grid items
        foreach (var card in deck)
        {
            var obj = Instantiate(cardGridItemPrefab, gridContainer);
            var itemView = obj.GetComponentInChildren<CardGridItemView>();
            if (itemView != null)
            {
                itemView.Init(card, this);
            }
            EnsureItemVisible(obj);
        }

        // Rebuild layout once the panel is active so the scroll content gets its real size.
        QueueGridRefresh();

        UpdateCloseButtonState();
        // Hide preview initially
        HidePreview();
    }

    private void EnsureItemVisible(GameObject item)
    {
        if (item == null)
            return;

        item.SetActive(true);
        item.transform.localScale = Vector3.one;
        item.transform.SetAsLastSibling();

        RectTransform rect = item.transform as RectTransform;
        if (rect != null)
        {
            Vector3 localPos = rect.localPosition;
            float z = normalizeCardDepth ? cardLocalZ : localPos.z;
            rect.localPosition = new Vector3(localPos.x, localPos.y, z);

            if (gridLayout != null)
            {
                if (rect.rect.width <= 1f)
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, gridLayout.cellSize.x);
                if (rect.rect.height <= 1f)
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gridLayout.cellSize.y);
            }
        }

        NormalizeItemDepth(item.transform);

        CanvasGroup[] canvasGroups = item.GetComponentsInChildren<CanvasGroup>(true);
        foreach (CanvasGroup cg in canvasGroups)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Agrandit une carte qui n'appartient pas a ce panneau, avec exactement l'agrandissement
    /// que le panneau de deck fait deja.
    ///
    /// <para>Elle existe pour l'historique des cartes jouees d'un duel, qui voulait ce geste-la
    /// — la carte qui vient se poser au centre, le fond qui s'assombrit, un clic a cote qui
    /// referme — et n'avait aucune raison d'en reecrire une version approchante. L'animation est
    /// litteralement la meme : <see cref="SelectCard"/> et cette methode appellent la meme coroutine,
    /// donc les deux ne peuvent pas diverger.</para>
    ///
    /// <para>Le panneau s'ouvre alors sans son deck : ni grille, ni titre, ni bouton de
    /// fermeture. Ce qui reste est le fond et la carte, c'est-a-dire l'apercu tout seul.</para>
    /// </summary>
    /// <param name="card">La carte a montrer, deja construite.</param>
    /// <param name="from">
    /// D'ou la carte part, typiquement la vignette cliquee. Null fait partir l'animation du
    /// centre, ce qui reste correct quand l'appelant n'a pas de point de depart a offrir.
    /// </param>
    public void ZoomExternalCard(CardInstance card, RectTransform from)
    {
        if (card == null || isAnimating)
            return;

        SetZoomOnly(true);

        gameObject.SetActive(true);
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // Aucune carte de la grille n'est concernee : la grille n'est meme pas montee.
        selectedItemView = null;
        HidePreview();

        StartCoroutine(AnimateCardToPreview(card, from, null));
    }

    /// <summary>
    /// Entre ou sort du mode agrandissement-seul.
    ///
    /// <para>Tout ce que le panneau contient est masque, <b>sauf l'apercu</b> : c'est lui qui
    /// porte le voile sombre et le clic qui referme, et c'est tout ce qu'un agrandissement
    /// demande. Le titre, la grille et le bouton de fermeture s'en vont donc ensemble.</para>
    ///
    /// <para>Masquer par « tout sauf l'apercu » plutot qu'en nommant le titre, la liste et le
    /// bouton un par un : la hierarchie n'est pas connue d'ici — la grille vit deux niveaux plus
    /// bas, sous un ScrollView qui a son propre fond — et un panneau qui gagnerait un element
    /// demain le laisserait trainer par-dessus la carte agrandie sans que personne y pense.</para>
    ///
    /// <para>Seuls les objets qui etaient allumes sont eteints, et eux seuls sont rallumes : on
    /// ne rend jamais visible quelque chose que la scene avait ferme.</para>
    /// </summary>
    private void SetZoomOnly(bool enabled)
    {
        if (zoomOnly == enabled)
            return;

        zoomOnly = enabled;
        if (enabled)
            HideEverythingButThePreview();
        else
            RestoreWhatZoomOnlyHid();
    }

    private void HideEverythingButThePreview()
    {
        hiddenByZoomOnly.Clear();
        foreach (Transform child in transform)
        {
            GameObject candidate = child.gameObject;
            if (previewPanel != null && candidate == previewPanel)
                continue;
            if (!candidate.activeSelf)
                continue;

            candidate.SetActive(false);
            hiddenByZoomOnly.Add(candidate);
        }
    }

    private void RestoreWhatZoomOnlyHid()
    {
        foreach (GameObject hidden in hiddenByZoomOnly)
        {
            if (hidden != null)
                hidden.SetActive(true);
        }
        hiddenByZoomOnly.Clear();
    }

    /// Referme un agrandissement ouvert depuis l'exterieur, et le panneau avec.
    private void EndZoomOnly()
    {
        if (!zoomOnly)
            return;

        SetZoomOnly(false);
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void SelectCard(CardInstance card, CardGridItemView itemView)
    {
        if (isAnimating) return;
        if (card == null)
            return;

        if (selectedItemView == itemView)
        {
            // Deselect the card if it's already selected
            if (selectedItemView != null)
                selectedItemView.gameObject.SetActive(true);
            HidePreview();
            selectedItemView = null;
            return;
        }
        //Hide previously selected card's preview if any
        if (selectedItemView != null && selectedItemView != itemView)
        {
            selectedItemView.gameObject.SetActive(true);
            HidePreview();
        }
        selectedItemView = itemView;
        StartCoroutine(AnimateCardToPreview(
            card, itemView != null ? itemView.GetComponent<RectTransform>() : null, itemView));
    }

    void ShowPreview(CardInstance card)
    {
        // No longer used in this flow, but kept for compatibility if needed elsewhere
    }

    /// <summary>
    /// Fait venir <paramref name="card"/> se poser au centre de l'apercu.
    ///
    /// <para>Une seule coroutine pour les deux usages — une carte de la grille, ou une carte
    /// venue d'ailleurs par <see cref="ZoomExternalCard"/> — parce que c'est le geste lui-meme qui
    /// devait etre partage. En ecrire une seconde copie pour l'historique aurait donne deux
    /// agrandissements qui se ressemblent aujourd'hui et divergent au premier reglage.</para>
    /// </summary>
    /// <param name="from">
    /// D'ou part la carte. Null la fait partir de sa position d'arrivee, ce qui revient a une
    /// apparition sur place plutot qu'a un vol depuis un point qu'on n'a pas.
    /// </param>
    /// <param name="itemToHide">
    /// La carte de la grille a effacer pendant l'agrandissement, quand il y en a une. Un
    /// agrandissement venu de l'exterieur n'en a pas : rien de cette grille ne bouge.
    /// </param>
    System.Collections.IEnumerator AnimateCardToPreview(
        CardInstance card, RectTransform from, CardGridItemView itemToHide)
    {
        isAnimating = true;

        // Clone the card prefab at the grid item's position.
        //
        // En mode agrandissement-seul la zone de defilement est masquee, et c'est justement
        // elle qui accueille la copie en usage normal : l'y poser la rendrait invisible. La
        // copie va donc sur le panneau lui-meme, qui est actif dans les deux cas.
        Transform cloneParent = zoomOnly ? transform : gridContainer.parent;
        var cardObj = Instantiate(cardGridItemPrefab, cloneParent);
        cardObj.transform.SetAsLastSibling();
        animatingCardObj = cardObj;
        var cardView = cardObj.GetComponentInChildren<CardView>();
        if (cardView != null)
            cardView.SetCard(card);

        var animRect = cardObj.GetComponent<RectTransform>();

        // Target: center of previewPanel (or screen)
        RectTransform previewRect = previewPanel.GetComponent<RectTransform>();
        Vector3 targetWorldPos = previewRect.transform.position;
        // Target scale: make the card about 80% of preview panel height (keep aspect)
        float cardAspect = animRect.rect.width / animRect.rect.height;
        float previewHeight = previewRect.rect.height * 0.8f;
        float previewWidth = previewHeight * cardAspect;
        Vector3 targetScale = 0.7f*new Vector3(previewWidth / animRect.rect.width, previewHeight / animRect.rect.height, 1f);

        // Sans point de depart, la carte se pose la ou elle doit finir : il n'y a rien a
        // survoler, et partir d'un coin arbitraire se verrait.
        animRect.position = from != null ? from.position : targetWorldPos;
        animRect.localScale = from != null ? from.localScale : targetScale;

        // Hide the original grid card during animation
        if (itemToHide != null)
            itemToHide.gameObject.SetActive(false);

        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 initialPos = animRect.position;
        Vector3 initialScale = animRect.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            animRect.position = Vector3.Lerp(initialPos, targetWorldPos, t);
            animRect.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        animRect.position = targetWorldPos;
        animRect.localScale = targetScale;

        // Show the preview panel (background, etc), but do not instantiate a new preview card
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 1f;
            previewCanvasGroup.blocksRaycasts = true;
        }
        previewPanel.SetActive(true);
        if (previewContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)previewContainer);

        isAnimating = false;
    }

    void HidePreview()
    {
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 0f;
            previewCanvasGroup.blocksRaycasts = false;
        }
        previewPanel.SetActive(false);
        if (previewContainer != null)
        {
            foreach (Transform child in previewContainer)
                Destroy(child.gameObject);
        }
        // Also destroy the animating card if present
        if (animatingCardObj != null)
        {
            Destroy(animatingCardObj);
            animatingCardObj = null;
        }
    }

    public void Hide()
    {
        if (SelectionManager.Instance != null && SelectionManager.Instance.selectionMode)
            return;

        SetZoomOnly(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        // Restore grid card if needed
        if (selectedItemView != null)
            selectedItemView.gameObject.SetActive(true);
    }
    /// <summary>
    /// Referme l'apercu, ce que le clic a cote de la carte declenche.
    ///
    /// <para>Quand le panneau n'etait ouvert que pour agrandir une carte venue d'ailleurs, il n'y
    /// a pas de grille a laquelle rendre la main : le meme clic referme donc tout. C'est ce qui
    /// fait qu'un clic a cote ferme l'historique d'un duel comme il ferme l'apercu d'un deck.</para>
    /// </summary>
    public void ClearSelection()
    {
        // Restore grid card if needed
        if (selectedItemView != null)
        {
            selectedItemView.gameObject.SetActive(true);
        }
        HidePreview();
        selectedItemView = null;
        EndZoomOnly();
    }

    private void OnEnable()
    {
        QueueGridRefresh();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
            QueueGridRefresh();
    }

    private void QueueGridRefresh()
    {
        // Ouvert pour agrandir une carte venue d'ailleurs : il n'y a pas de grille montee, et
        // OnEnable passe pourtant par ici puisque le panneau vient d'etre active.
        if (zoomOnly)
            return;

        if (refreshQueued || !isActiveAndEnabled)
            return;

        refreshQueued = true;
        if (enableEntranceAnimation)
            StartCoroutine(RefreshGridContentSizeAfterFrame());
        else
            StartCoroutine(RefreshGridContentSizeAfterFrameImmediate());
    }

    private IEnumerator RefreshGridContentSizeAfterFrame()
    {
        yield return null;
        RefreshGridContentSize();

        RectTransform gridContainerRect = gridContainer as RectTransform;
        if (enableEntranceAnimation && gridContainerRect != null)
            yield return AnimateGridEntrance(gridContainerRect);
    }

    private IEnumerator RefreshGridContentSizeAfterFrameImmediate()
    {
        yield return null;
        RefreshGridContentSize();
    }

    private void RefreshGridContentSize()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform gridContainerRect = gridContainer as RectTransform;
        if (gridContainerRect == null || gridLayout == null)
        {
            refreshQueued = false;
            return;
        }

        RectTransform viewportRect = GetViewportRect();
        if (viewportRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainerRect);

        int itemCount = gridContainer.childCount;
        if (itemCount <= 0)
        {
            refreshQueued = false;
            return;
        }

        NormalizeAllItemsDepth();

        RectTransform sizingRect = viewportRect != null ? viewportRect : gridContainerRect;
        int columns = GetColumnCount(sizingRect, itemCount);
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        float height = gridLayout.padding.top + gridLayout.padding.bottom;
        if (rows > 0)
        {
            height += rows * gridLayout.cellSize.y;
            height += Mathf.Max(0, rows - 1) * gridLayout.spacing.y;
        }

        height += contentPadding * 2f;

        gridContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        Canvas.ForceUpdateCanvases();
        refreshQueued = false;
    }

    private void NormalizeAllItemsDepth()
    {
        if (!normalizeCardDepth || gridContainer == null)
            return;

        foreach (Transform child in gridContainer)
            NormalizeItemDepth(child);
    }

    private void NormalizeItemDepth(Transform root)
    {
        if (!normalizeCardDepth || root == null)
            return;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform r in rects)
        {
            Vector3 p = r.localPosition;
            r.localPosition = new Vector3(p.x, p.y, cardLocalZ);
        }
    }

    private IEnumerator AnimateGridEntrance(RectTransform gridContainerRect)
    {
        Vector2 targetPosition = gridContainerRect.anchoredPosition;
        Vector2 startPosition = targetPosition + Vector2.down * Mathf.Max(entranceOffset, gridContainerRect.rect.height * 0.5f);

        gridContainerRect.anchoredPosition = startPosition;

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float eased = t * t * (3f - 2f * t);
            gridContainerRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            yield return null;
        }

        gridContainerRect.anchoredPosition = targetPosition;
    }

    private RectTransform GetViewportRect()
    {
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.viewport != null)
            return scrollRect.viewport;

        Transform parentTransform = gridContainer != null ? gridContainer.parent : null;
        return parentTransform as RectTransform;
    }

    private int GetColumnCount(RectTransform availableAreaRect, int itemCount)
    {
        if (initialGridConstraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, initialGridConstraintCount);

        if (initialGridConstraint == GridLayoutGroup.Constraint.FixedRowCount)
            return Mathf.Max(1, Mathf.CeilToInt((float)itemCount / Mathf.Max(1, initialGridConstraintCount)));

        float availableWidth = availableAreaRect.rect.width - gridLayout.padding.left - gridLayout.padding.right;
        float cellAndSpacingWidth = gridLayout.cellSize.x + gridLayout.spacing.x;
        int calculated = Mathf.FloorToInt((availableWidth + gridLayout.spacing.x) / Mathf.Max(1f, cellAndSpacingWidth));
        return Mathf.Max(1, calculated);
    }

    void Update()
    {
        UpdateCloseButtonState();
    }

    private void UpdateCloseButtonState()
    {
        if (closeButton == null)
            return;

        bool isSelecting = SelectionManager.Instance != null && SelectionManager.Instance.selectionMode;
        closeButton.interactable = !isSelecting;
    }
}
