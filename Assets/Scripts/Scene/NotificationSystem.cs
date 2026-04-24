using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Lean.Transition;

public class NotificationSystem : MonoBehaviour
{
    public static NotificationSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Notification")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Button closeNotificationButton;
    [SerializeField] private LeanPlayer notificationShowTransition;
    [SerializeField] private LeanPlayer notificationHideTransition;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private bool autoClose = true;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private TMP_Text confirmationTitleText;
    [SerializeField] private TMP_Text confirmationMessageText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private LeanPlayer confirmationShowTransition;
    [SerializeField] private LeanPlayer confirmationHideTransition;

    [Header("Loading Screen")]
    [SerializeField] private LoadingScreen loadingScreen;

    private readonly Queue<string> _queue = new();
    private bool _isShowingNotification;
    private Action _onYes;
    private Action _onNo;

    void Start()
    {
        if (closeNotificationButton) closeNotificationButton.onClick.AddListener(CloseNotification);
        if (confirmYesButton)        confirmYesButton.onClick.AddListener(OnYes);
        if (confirmNoButton)         confirmNoButton.onClick.AddListener(OnNo);
    }

    void OnDestroy()
    {
        if (closeNotificationButton) closeNotificationButton.onClick.RemoveAllListeners();
        if (confirmYesButton)        confirmYesButton.onClick.RemoveAllListeners();
        if (confirmNoButton)         confirmNoButton.onClick.RemoveAllListeners();
    }

    // ── Notifications ──────────────────────────────────────────────────────────

    public void ShowNotification(string message)
    {
        _queue.Enqueue(message);
        if (!_isShowingNotification) ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (_queue.Count == 0) { _isShowingNotification = false; return; }

        _isShowingNotification = true;
        var msg = _queue.Dequeue();

        if (notificationPanel == null) { Debug.LogError("[NotificationSystem] notificationPanel non assigné."); return; }

        notificationPanel.SetActive(true);
        if (notificationText) notificationText.text = msg;

        if (notificationShowTransition != null && notificationShowTransition.IsUsed)
            notificationShowTransition.Begin();

        if (autoClose)
        {
            CancelInvoke(nameof(CloseNotification));
            Invoke(nameof(CloseNotification), displayDuration);
        }
    }

    private void CloseNotification()
    {
        CancelInvoke(nameof(CloseNotification));
        if (notificationHideTransition != null && notificationHideTransition.IsUsed)
        {
            notificationHideTransition.Begin();
            Invoke(nameof(HideNotificationPanel), 0.3f);
        }
        else HideNotificationPanel();
    }

    private void HideNotificationPanel()
    {
        if (notificationPanel) notificationPanel.SetActive(false);
        ProcessQueue();
    }

    // ── Confirmation ───────────────────────────────────────────────────────────

    public void ShowConfirmation(string title, string message, Action onYes, Action onNo = null)
    {
        _onYes = onYes;
        _onNo  = onNo;

        if (confirmationPopup == null) { Debug.LogError("[NotificationSystem] confirmationPopup non assigné."); return; }

        confirmationPopup.SetActive(true);
        if (confirmationTitleText)   confirmationTitleText.text   = title;
        if (confirmationMessageText) confirmationMessageText.text = message;

        if (confirmationShowTransition != null && confirmationShowTransition.IsUsed)
            confirmationShowTransition.Begin();
    }

    private void OnYes()
    {
        HideConfirmation();
        var cb = _onYes; _onYes = null; _onNo = null;
        cb?.Invoke();
    }

    private void OnNo()
    {
        HideConfirmation();
        var cb = _onNo; _onYes = null; _onNo = null;
        cb?.Invoke();
    }

    private void HideConfirmation()
    {
        if (confirmationHideTransition != null && confirmationHideTransition.IsUsed)
        {
            confirmationHideTransition.Begin();
            Invoke(nameof(SetConfirmationInactive), 0.3f);
        }
        else SetConfirmationInactive();
    }

    private void SetConfirmationInactive()
    {
        if (confirmationPopup) confirmationPopup.SetActive(false);
    }

    // ── Loading Screen ─────────────────────────────────────────────────────────

    public void ShowLoading(int totalSteps)
    {
        if (loadingScreen == null) { Debug.LogError("[NotificationSystem] loadingScreen non assigné."); return; }
        loadingScreen.gameObject.SetActive(true);
        loadingScreen.Initialize(totalSteps);
    }

    public void IncrementLoadingStep() => loadingScreen?.IncrementStep();

    public void HideLoading()
    {
        if (loadingScreen) loadingScreen.gameObject.SetActive(false);
    }
}
