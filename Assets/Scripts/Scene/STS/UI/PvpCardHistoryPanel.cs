using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// L'historique des cartes jouées d'un duel : une colonne de vignettes, en haut à droite.
///
/// <para>Il est <b>partagé</b> : les deux joueurs voient la même liste, dans le même ordre, parce
/// qu'elle est servie par le serveur et non accumulée par chaque client. Une liste accumulée ici
/// repartirait vide à chaque reconnexion — un snapshot ne rejoue aucun événement — et les deux
/// camps finiraient par ne plus voir la même chose. Voir <see cref="PvpPlayedCardHistory"/>.</para>
///
/// <para>Cliquer une vignette agrandit la carte au centre de l'écran ; cliquer à côté referme.
/// Ce geste n'est pas réécrit ici : c'est celui du panneau de deck, emprunté tel quel par
/// <see cref="DeckGridPanel.ZoomExternalCard"/>. Deux agrandissements écrits séparément se
/// ressembleraient aujourd'hui et divergeraient au premier réglage, et celui du deck est déjà
/// celui qu'on veut.</para>
///
/// <para><b>À câbler dans la scène :</b> <see cref="entriesContainer"/> (un
/// <c>VerticalLayoutGroup</c> dans un <c>ScrollRect</c>, ancré en haut à droite) et
/// <see cref="entryPrefab"/> (le prefab de vignette portant un <see cref="PvpCardHistoryItem"/>).
/// L'agrandissement, lui, n'a rien à câbler : le panneau de deck vit dans la scène de boot, qui
/// survit au chargement du combat, et il est retrouvé tout seul par
/// <c>RunManager.Instance.ui.deckGridPanel</c>. Le champ <see cref="zoomPanel"/> n'est là que
/// pour en désigner un autre si l'envie prend.</para>
/// </summary>
public class PvpCardHistoryPanel : MonoBehaviour
{
    [Header("Liste")]
    [SerializeField] private GameObject root;
    [SerializeField] private Transform entriesContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI emptyLabel;

    [Header("Carte agrandie")]
    [Tooltip("Laisser vide : le panneau de deck de la scène de boot est trouvé tout seul.")]
    [SerializeField] private DeckGridPanel zoomPanel;

    [Header("Limites")]
    [Tooltip("Combien de vignettes au plus. Les plus anciennes sortent en premier.")]
    [SerializeField] private int maxEntries = 40;
    [Tooltip("Fait défiler jusqu'à la dernière carte jouée quand la liste s'allonge.")]
    [SerializeField] private bool followLatest = true;

    private readonly List<GameObject> spawnedEntries = new();
    private List<PvpPlayedCard> shownHistory;

    private void Awake()
    {
        // Rien tant que rien n'a été joué.
        SetVisible(false);
    }

    /// <summary>
    /// Remplace la liste affichée par <paramref name="history"/>.
    /// </summary>
    /// <param name="history">Les cartes jouées, dans l'ordre où elles l'ont été.</param>
    /// <param name="resolve">
    /// Ce qui, d'une entrée, rend la carte à montrer, le nom de qui l'a jouée, et de quel côté.
    /// Rendre une carte nulle écarte l'entrée plutôt que d'ouvrir une vignette vide.
    /// </param>
    public void Show(List<PvpPlayedCard> history, Func<PvpPlayedCard, PvpHistoryEntryView> resolve)
    {
        if (history == null || resolve == null)
            return;

        // L'état complet arrive à chaque coup joué. Reconstruire à chaque fois détruirait les
        // vignettes sous le doigt du joueur — et la grande carte qu'il vient d'ouvrir avec.
        if (!PvpPlayedCardHistory.Differs(shownHistory, history))
            return;

        shownHistory = new List<PvpPlayedCard>(history);
        Rebuild(resolve);
    }

    /// Ce qu'il faut savoir d'une entrée pour l'afficher.
    public readonly struct PvpHistoryEntryView
    {
        public PvpHistoryEntryView(CardInstance card, string actorName, bool playedByOurSide)
        {
            Card = card;
            ActorName = actorName;
            PlayedByOurSide = playedByOurSide;
        }

        public CardInstance Card { get; }
        public string ActorName { get; }
        public bool PlayedByOurSide { get; }
    }

    private void Rebuild(Func<PvpPlayedCard, PvpHistoryEntryView> resolve)
    {
        ClearEntries();

        if (entriesContainer == null || entryPrefab == null)
        {
            // Sans conteneur ni prefab il n'y a rien à monter. On le dit une bonne fois : un
            // historique silencieusement absent se cherche longtemps.
            Debug.LogWarning("[STS-PVP] Card history panel has no container or entry prefab wired; "
                + "the played-card history will not appear.");
            return;
        }

        // Les plus récentes, quand il y en a trop : c'est ce qu'on relit.
        int firstIndex = maxEntries > 0 && shownHistory.Count > maxEntries
            ? shownHistory.Count - maxEntries
            : 0;

        for (int index = firstIndex; index < shownHistory.Count; index++)
        {
            PvpPlayedCard played = shownHistory[index];
            PvpHistoryEntryView view = resolve(played);
            if (view.Card == null)
                continue;

            GameObject entry = Instantiate(entryPrefab, entriesContainer);
            entry.SetActive(true);
            spawnedEntries.Add(entry);

            PvpCardHistoryItem item = entry.GetComponent<PvpCardHistoryItem>()
                ?? entry.GetComponentInChildren<PvpCardHistoryItem>(true);
            if (item == null)
            {
                Debug.LogWarning("[STS-PVP] The card history entry prefab carries no "
                    + "PvpCardHistoryItem; its entries will not open when clicked.");
                continue;
            }

            CardInstance card = view.Card;
            // La vignette elle-même est le point de départ de l'animation : la carte agrandie
            // part de là où le joueur vient de cliquer, comme elle part de sa case dans le deck.
            RectTransform from = entry.transform as RectTransform;
            item.Bind(card, view.ActorName, view.PlayedByOurSide, () => OpenZoom(card, from));
        }

        bool anything = spawnedEntries.Count > 0;
        SetVisible(anything);
        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(!anything);

        if (anything && followLatest && scrollRect != null)
        {
            // Après la reconstruction, sinon le contenu n'a pas encore sa taille.
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Agrandit <paramref name="card"/>, avec l'agrandissement du panneau de deck.
    ///
    /// <para>Rien n'est animé ni instancié ici : tout est délégué. Ce panneau ne sait que d'où
    /// la carte part, et le panneau de deck sait déjà faire le reste — la carte qui vient se
    /// poser au centre, le fond sombre, et le clic à côté qui referme.</para>
    /// </summary>
    /// <param name="from">La vignette cliquée, d'où part l'animation.</param>
    public void OpenZoom(CardInstance card, RectTransform from)
    {
        if (card == null)
            return;

        DeckGridPanel panel = ResolveZoomPanel();
        if (panel == null)
        {
            // Sans lui il n'y a pas d'agrandissement du tout, et une vignette qui ne réagit pas
            // au clic se cherche longtemps.
            Debug.LogWarning("[STS-PVP] No deck panel available to zoom a history card; "
                + "clicking a played card will do nothing.");
            return;
        }

        panel.ZoomExternalCard(card, from);
    }

    /// <summary>
    /// Le panneau qui agrandit les cartes.
    ///
    /// <para>Celui du menu de run par défaut : il vit dans la scène de boot, qui survit au
    /// chargement du combat — c'est déjà par ce chemin que l'en-tête de run est empruntée
    /// pendant un duel. Le chercher à chaque fois plutôt que de le retenir laisse le duel
    /// fonctionner même si la scène de boot est rechargée entre deux combats.</para>
    /// </summary>
    private DeckGridPanel ResolveZoomPanel()
    {
        if (zoomPanel != null)
            return zoomPanel;

        RunManagerUI header = RunManager.Instance != null ? RunManager.Instance.ui : null;
        return header != null ? header.deckGridPanel : null;
    }

    private void ClearEntries()
    {
        foreach (GameObject entry in spawnedEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        spawnedEntries.Clear();
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);
    }
}
