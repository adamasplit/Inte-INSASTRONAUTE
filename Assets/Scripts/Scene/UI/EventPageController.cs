using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventPageController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MainUIBinder ui;

    [Header("Common UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text metaText; // dates/status
    [SerializeField] private GameObject infoRoot;
    [SerializeField] private GameObject betRoot;

    [Header("Bet UI")]
    [SerializeField] private TMP_Text oddsText;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button betYesButton;
    [SerializeField] private Button betNoButton;

    private EventDto _current;

    private void Awake()
    {
        if (betYesButton)
        {
            betYesButton.onClick.RemoveAllListeners();
            betYesButton.onClick.AddListener(() => _ = OnClickPlaceBetAsync(true));
        }

        if (betNoButton)
        {
            betNoButton.onClick.RemoveAllListeners();
            betNoButton.onClick.AddListener(() => _ = OnClickPlaceBetAsync(false));
        }
    }

    public void Show(EventDto e)
    {
        _current = e;

        if (titleText) titleText.text = e.title ?? "";
        if (bodyText) bodyText.text = e.body ?? "";

        // Meta: petit résumé V1
        if (metaText)
        {
            var status = e.type == "PARI" ? (string.IsNullOrWhiteSpace(e.status) ? "OPEN" : e.status) : "";
            metaText.text = e.type == "PARI"
                ? $"Coef: {e.odds:0.##} | Status: {status}"
                : "";
        }

        var isBet = e.type == "PARI";
        if (infoRoot) infoRoot.SetActive(!isBet);
        if (betRoot) betRoot.SetActive(isBet);

        if (isBet && oddsText) oddsText.text = $"Coef: {e.odds:0.##}";

        if (isBet && amountInput) amountInput.text = "10"; // défaut
    }

    private async Task OnClickPlaceBetAsync(bool sideYes)
    {
        if (_current == null || _current.type != "PARI")
            return;

        if (!int.TryParse(amountInput.text, out var amount) || amount <= 0)
        {
            ui.ShowNotification("Mise invalide.");
            return;
        }

        // Vérif OPEN côté client (le serveur recheck aussi)
        if (!string.Equals(_current.status, "OPEN", StringComparison.OrdinalIgnoreCase))
        {
            ui.ShowNotification("Les paris sont fermés.");
            return;
        }

        var sideName = sideYes ? "YES" : "NO";
        ui.ShowConfirmation(
            title: "Confirmer le pari",
            message: $"Parier {amount} TOKEN sur {sideName}\n{_current.title}\nCoef: {_current.odds:0.##}\n\nConfirmer ?",
            onYes: () => _ = PlaceBetConfirmedAsync(amount, sideYes),
            onNo: null
        );
    }

    private async Task PlaceBetConfirmedAsync(int amount, bool sideYes)
    {
        try
        {
            // Appel Cloud Code
            var res = await BetsClient.PlaceBetAsync(_current, amount, sideYes);

            if (!res.ok)
            {
                ui.ShowNotification(res.message ?? "Pari refusé.");
                return;
            }

            ui.ShowNotification("Pari enregistré.");

            // Refresh player status to update tokens
            var statusController = FindFirstObjectByType<PlayerStatusController>();
            if (statusController != null)
            {
                await statusController.RefreshStatusAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            ui.ShowNotification("Erreur lors du pari.");
        }
    }
}
