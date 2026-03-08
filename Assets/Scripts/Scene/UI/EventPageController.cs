using System;
using System.Collections.Generic;
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
    [SerializeField] private TMP_Text metaText;
    [SerializeField] private GameObject infoRoot;
    [SerializeField] private GameObject betRoot;

    [Header("Bet – Commun")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button betConfirmButton; // bouton unique pour les deux modes

    [Header("Bet – Liste")]
    [SerializeField] private GameObject listRoot;
    [SerializeField] private TMP_Dropdown optionsDropdown;

    [Header("Bet – Réponse libre")]
    [SerializeField] private GameObject freeRoot;
    [SerializeField] private TMP_InputField answerInput;

    private EventDto _current;

    private void Awake()
    {
        if (betConfirmButton)
        {
            betConfirmButton.onClick.RemoveAllListeners();
            betConfirmButton.onClick.AddListener(() => _ = OnClickConfirmAsync());
        }
    }

    public void Show(EventDto e)
    {
        _current = e;

        if (titleText) titleText.text = e.title ?? "";
        if (bodyText)  bodyText.text  = e.body  ?? "";

        if (metaText)
        {
            var status = e.type == "PARI" ? (string.IsNullOrWhiteSpace(e.status) ? "OPEN" : e.status) : "";
            metaText.text = e.type == "PARI" ? $"Status: {status}" : "";
        }

        var isBet = e.type == "PARI";
        if (infoRoot) infoRoot.SetActive(!isBet);
        if (betRoot)  betRoot.SetActive(isBet);

        if (!isBet) return;

        if (amountInput) amountInput.text = "10";

        var isList = e.answerType == "list";
        if (listRoot) listRoot.SetActive(isList);
        if (freeRoot) freeRoot.SetActive(!isList);

        if (isList && optionsDropdown != null)
        {
            optionsDropdown.ClearOptions();
            if (e.options != null)
            {
                var entries = new List<string>();
                foreach (var opt in e.options)
                    entries.Add($"{opt.label}  (×{opt.odds:0.##})");
                optionsDropdown.AddOptions(entries);
            }
            optionsDropdown.value = 0;
            optionsDropdown.RefreshShownValue();
        }

        if (answerInput) answerInput.text = "";
    }

    // ── Dispatch selon le mode ────────────────────────────────────────────────

    private async Task OnClickConfirmAsync()
    {
        if (_current == null) return;

        if (!int.TryParse(amountInput.text, out var amount) || amount <= 0)
        {
            ui.ShowNotification("Mise invalide.");
            return;
        }
        if (!string.Equals(_current.status, "OPEN", StringComparison.OrdinalIgnoreCase))
        {
            ui.ShowNotification("Les paris sont fermés.");
            return;
        }

        if (_current.answerType == "list")
            await ConfirmListBetAsync(amount);
        else
            await ConfirmFreeBetAsync(amount);
    }

    private Task ConfirmListBetAsync(int amount)
    {
        if (_current.options == null || _current.options.Length == 0 || optionsDropdown == null)
        {
            ui.ShowNotification("Aucune option disponible.");
            return Task.CompletedTask;
        }

        var idx = optionsDropdown.value;
        if (idx < 0 || idx >= _current.options.Length)
        {
            ui.ShowNotification("Sélection invalide.");
            return Task.CompletedTask;
        }

        var option = _current.options[idx];
        ui.ShowConfirmation(
            title: "Confirmer le pari",
            message: $"Parier {amount} TOKEN sur \"{option.label}\" (×{option.odds:0.##})\n{_current.title}\n\nConfirmer ?",
            onYes: () => _ = PlaceBetConfirmedAsync(amount, option.label, option.odds),
            onNo: null
        );
        return Task.CompletedTask;
    }

    private Task ConfirmFreeBetAsync(int amount)
    {
        var answer = answerInput != null ? answerInput.text.Trim() : "";
        if (string.IsNullOrEmpty(answer))
        {
            ui.ShowNotification("Réponse invalide.");
            return Task.CompletedTask;
        }

        ui.ShowConfirmation(
            title: "Confirmer le pari",
            message: $"Parier {amount} TOKEN sur \"{answer}\"\n{_current.title}\n\nConfirmer ?",
            onYes: () => _ = PlaceBetConfirmedAsync(amount, answer, 0f),
            onNo: null
        );
        return Task.CompletedTask;
    }

    // ── Appel Cloud Code ──────────────────────────────────────────────────────

    private async Task PlaceBetConfirmedAsync(int amount, string choice, float odds)
    {
        try
        {
            var res = await BetsClient.PlaceBetAsync(_current, amount, choice, odds);

            if (!res.ok)
            {
                ui.ShowNotification(res.message ?? "Pari refusé.");
                return;
            }

            ui.ShowNotification("Pari enregistré.");

            var statusController = FindFirstObjectByType<PlayerStatusController>();
            if (statusController != null)
                await statusController.RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            ui.ShowNotification("Erreur lors du pari.");
        }
    }
}
