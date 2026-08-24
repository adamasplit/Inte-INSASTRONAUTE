using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Text;

public class RunEndUnlockPanel : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Button okButton;
    public CanvasGroup canvasGroup;

    private Action pendingOnClose;

    void Awake()
    {
        // Survive the scene load back to the main menu regardless of where this panel is parented.
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        if (okButton != null)
            okButton.onClick.AddListener(OnOkPressed);
    }

    public void Show(List<STSCardData> unlockedCards, Action onClose)
    {
        pendingOnClose = onClose;

        if (messageText != null)
            messageText.text = BuildMessage(unlockedCards);

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(false);
    }

    void OnOkPressed()
    {
        Action callback = pendingOnClose;
        pendingOnClose = null;
        Hide();
        callback?.Invoke();
    }

    private static string BuildMessage(List<STSCardData> unlockedCards)
    {
        List<string> names = new();
        if (unlockedCards != null)
        {
            foreach (STSCardData card in unlockedCards)
            {
                if (card != null && !string.IsNullOrWhiteSpace(card.cardName))
                    names.Add(card.cardName);
            }
        }

        if (names.Count == 0)
            return "";

        StringBuilder sb = new StringBuilder(names.Count > 1 ? "Vous avez débloqué les cartes " : "Vous avez débloqué la carte ");
        for (int i = 0; i < names.Count; i++)
        {
            sb.Append('"').Append(names[i]).Append('"');
            if (i < names.Count - 2)
                sb.Append(", ");
            else if (i == names.Count - 2)
                sb.Append(" et ");
        }
        sb.Append(" pour le mode multijoueur!");
        return sb.ToString();
    }
}
