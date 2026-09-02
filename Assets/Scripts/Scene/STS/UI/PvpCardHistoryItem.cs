using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Une vignette de l'historique des cartes jouées.
///
/// <para>Le prefab ne montre qu'une portion de la carte : la <see cref="CardView"/> est posée
/// dans un masque plus petit qu'elle, ce qui laisse voir l'illustration et le nom sans occuper la
/// colonne. Le détail se lit en cliquant, pas en plissant les yeux — d'où le clic qui ouvre la
/// carte en grand au centre de l'écran.</para>
/// </summary>
public class PvpCardHistoryItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CardView cardView;
    [Tooltip("Affiche qui a joué la carte. Facultatif.")]
    [SerializeField] private TextMeshProUGUI actorLabel;
    [Tooltip("Teinte appliquée selon le camp de celui qui a joué. Facultatif.")]
    [SerializeField] private Image sideAccent;
    [SerializeField] private Color allyColor = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color opponentColor = new Color(1f, 0.4f, 0.35f);

    private Action onClicked;

    /// <param name="card">La carte à montrer, déjà construite : la vignette ne résout rien.</param>
    /// <param name="actorName">Le nom de qui l'a jouée, ou null pour ne rien afficher.</param>
    /// <param name="playedByOurSide">De quel côté teinter la vignette.</param>
    /// <param name="clicked">Ce que fait le clic — ouvrir la carte en grand.</param>
    public void Bind(CardInstance card, string actorName, bool playedByOurSide, Action clicked)
    {
        onClicked = clicked;

        if (cardView == null)
            cardView = GetComponentInChildren<CardView>(true);

        if (cardView != null && card != null)
        {
            cardView.gameObject.SetActive(true);
            cardView.SetCard(card);
        }

        if (actorLabel != null)
        {
            actorLabel.text = actorName ?? string.Empty;
            actorLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(actorName));
        }

        if (sideAccent != null)
            sideAccent.color = playedByOurSide ? allyColor : opponentColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button == PointerEventData.InputButton.Left)
            onClicked?.Invoke();
    }
}
