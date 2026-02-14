using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Lean.Transition;

/// <summary>
/// Main UI binder for settings, daily rewards, and notification panels.
/// Manages transitions and user interactions.
/// </summary>
public class MainUIBinder : MonoBehaviour
{
    #region Bottom Bar
    [Header("=== Bottom Bar ===")]
    [SerializeField] private Button dailyRewardButton;
    #endregion

    #region Settings
    [Header("=== Settings ===")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button deleteAccountButton;
    
    [Header("Settings Transitions")]
    [SerializeField] private LeanPlayer settingsShowTransition;
    [SerializeField] private LeanPlayer settingsHideTransition;

    [Header("Settings - Audio")]
    [SerializeField] private Toggle muteToggle;

    [Header("Settings - Graphics")]
    [SerializeField] private Toggle fullscreenToggle;
    #endregion

    #region Daily Reward
    [Header("=== Daily Reward ===")]
    [SerializeField] private GameObject dailyRewardPanel;
    [SerializeField] private Button closeDailyRewardButton;
    [SerializeField] private Button claimDailyRewardButton;
    [SerializeField] private TMP_Text dailyRewardDayText;
    [SerializeField] private TMP_Text dailyRewardDescriptionText;
    [SerializeField] private Transform dailyRewardItemsContainer;
    
    [Header("Daily Reward Transitions")]
    [SerializeField] private LeanPlayer dailyRewardShowTransition;
    [SerializeField] private LeanPlayer dailyRewardHideTransition;
    [SerializeField] private LeanPlayer dailyRewardClaimTransition;
    #endregion

    #region Notifications & Popups
    [Header("=== Notifications & Popups ===")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Button closeNotificationButton;
    
    [Header("Notification Transitions")]
    [SerializeField] private LeanPlayer notificationShowTransition;
    [SerializeField] private LeanPlayer notificationHideTransition;
    
    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private TMP_Text confirmationTitleText;
    [SerializeField] private TMP_Text confirmationMessageText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Confirmation Popup Transitions")]
    [SerializeField] private LeanPlayer confirmationShowTransition;
    [SerializeField] private LeanPlayer confirmationHideTransition;
    #endregion

    private PlayerStatusController userController;
    private Queue<string> notificationQueue = new Queue<string>();
    private bool isShowingNotification = false;

    [Header("Notification Settings")]
    [SerializeField] private float notificationDisplayDuration = 3f;
    [SerializeField] private bool autoCloseNotifications = true;

    #region Unity Lifecycle
    private void Start()
    {
        InitializeButtons();
        InitializeToggles();
        userController = FindFirstObjectByType<PlayerStatusController>();
    }

    private void OnDestroy()
    {
        UnregisterButtons();
        UnregisterToggles();
    }
    #endregion

    #region Initialization
    private void InitializeButtons()
    {
        // Main Page
        RegisterButton(dailyRewardButton, OnClick_OpenDailyReward);

        // Settings
        RegisterButton(settingsButton, OnClick_OpenSettings);
        RegisterButton(closeSettingsButton, OnClick_CloseSettings);
        RegisterButton(disconnectButton, OnClick_Disconnect);
        RegisterButton(deleteAccountButton, OnClick_DeleteAccount);

        // Daily Reward
        RegisterButton(closeDailyRewardButton, OnClick_CloseDailyReward);
        RegisterButton(claimDailyRewardButton, OnClick_ClaimDailyReward);

        // Notifications
        RegisterButton(closeNotificationButton, OnClick_CloseNotification);
        RegisterButton(confirmYesButton, OnClick_ConfirmYes);
        RegisterButton(confirmNoButton, OnClick_ConfirmNo);
    }

    private void InitializeToggles()
    {
        RegisterToggle(muteToggle, OnToggle_Mute);
        RegisterToggle(fullscreenToggle, OnToggle_Fullscreen);
    }
    #endregion

    #region Helper Methods
    private void RegisterButton(Button button, Action callback)
    {
        if (button != null)
            button.onClick.AddListener(() => callback?.Invoke());
    }

    private void RegisterToggle(Toggle toggle, Action<bool> callback)
    {
        if (toggle != null)
            toggle.onValueChanged.AddListener(value => callback?.Invoke(value));
    }

    private void UnregisterButtons()
    {
        if (settingsButton) settingsButton.onClick.RemoveAllListeners();
        if (closeSettingsButton) closeSettingsButton.onClick.RemoveAllListeners();
        if (disconnectButton) disconnectButton.onClick.RemoveAllListeners();
        if (deleteAccountButton) deleteAccountButton.onClick.RemoveAllListeners();
        if (dailyRewardButton) dailyRewardButton.onClick.RemoveAllListeners();
        if (closeDailyRewardButton) closeDailyRewardButton.onClick.RemoveAllListeners();
        if (claimDailyRewardButton) claimDailyRewardButton.onClick.RemoveAllListeners();
        if (closeNotificationButton) closeNotificationButton.onClick.RemoveAllListeners();
        if (confirmYesButton) confirmYesButton.onClick.RemoveAllListeners();
        if (confirmNoButton) confirmNoButton.onClick.RemoveAllListeners();
    }

    private void UnregisterToggles()
    {
        if (muteToggle) muteToggle.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle) fullscreenToggle.onValueChanged.RemoveAllListeners();
    }

    public void ShowNotification(string message)
    {
        Debug.Log($"[MainUI] {message}");
        
        // Add to queue
        notificationQueue.Enqueue(message);
        
        // Process queue if not already showing a notification
        if (!isShowingNotification)
        {
            ProcessNextNotification();
        }
    }

    private void ProcessNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isShowingNotification = false;
            return;
        }

        isShowingNotification = true;
        string message = notificationQueue.Dequeue();
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            if (notificationText) notificationText.text = message;
            
            // Play show transition
            if (notificationShowTransition != null && notificationShowTransition.IsUsed)
            {
                notificationShowTransition.Begin();
            }
            
            // Auto-close after duration if enabled
            if (autoCloseNotifications)
            {
                CancelInvoke(nameof(OnClick_CloseNotification));
                Invoke(nameof(OnClick_CloseNotification), notificationDisplayDuration);
            }
        }
    }

    public void ShowConfirmation(string title, string message, Action onYes, Action onNo = null)
    {
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(true);
            if (confirmationTitleText) confirmationTitleText.text = title;
            if (confirmationMessageText) confirmationMessageText.text = message;
            
            // Store callbacks for later use
            currentConfirmationYesCallback = onYes;
            currentConfirmationNoCallback = onNo;
            
            // Play show transition
            if (confirmationShowTransition != null && confirmationShowTransition.IsUsed)
            {
                confirmationShowTransition.Begin();
            }
        }
    }

    private Action currentConfirmationYesCallback;
    private Action currentConfirmationNoCallback;
    #endregion

    #region Settings Callbacks
    private void OnClick_OpenSettings()
    {
        Debug.Log("[MainUI] Opening settings");
        if (settingsPanel)
        {
            settingsPanel.SetActive(true);
            if (settingsShowTransition != null && settingsShowTransition.IsUsed)
            {
                settingsShowTransition.Begin();
            }
        }
    }

    private void OnClick_CloseSettings()
    {
        Debug.Log("[MainUI] Closing settings");
        if (settingsPanel)
        {
            if (settingsHideTransition != null && settingsHideTransition.IsUsed)
            {
                settingsHideTransition.Begin();
                Invoke(nameof(HideSettingsPanel), 0.3f);
            }
            else
            {
                settingsPanel.SetActive(false);
            }
        }
    }

    private void HideSettingsPanel()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void OnClick_Disconnect()
    {
        Debug.Log("[MainUI] Disconnect requested");
        ShowConfirmation(
            "Déconnexion",
            "Voulez-vous vraiment vous déconnecter ?",
            () => {
                Debug.Log("[MainUI] User confirmed disconnect");
                AuthController.Instance.SignOut();
                ShowNotification("Déconnexion réussie");
            },
            () => Debug.Log("[MainUI] Disconnect cancelled")
        );
    }

    private void OnClick_DeleteAccount()
    {
        Debug.Log("[MainUI] Delete account requested");
        ShowConfirmation(
            "Suppression du compte",
            "ATTENTION : Cette action est irréversible. Voulez-vous vraiment supprimer votre compte ?",
            () => {
                Debug.Log("[MainUI] User confirmed account deletion");
                // TODO: Call account deletion logic
                ShowNotification("Compte supprimé");
            },
            () => Debug.Log("[MainUI] Account deletion cancelled")
        );
    }

    private void OnToggle_Mute(bool isMuted)
    {
        Debug.Log($"[MainUI] Mute toggled: {isMuted}");
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    private void OnToggle_Fullscreen(bool isFullscreen)
    {
        Debug.Log($"[MainUI] Fullscreen toggled: {isFullscreen}");
        Screen.fullScreen = isFullscreen;
    }
    #endregion

    #region Daily Reward Callbacks
    private void OnClick_OpenDailyReward()
    {
        Debug.Log("[MainUI] Opening daily reward");
        if (dailyRewardPanel)
        {
            dailyRewardPanel.SetActive(true);
            if (dailyRewardShowTransition != null && dailyRewardShowTransition.IsUsed)
            {
                dailyRewardShowTransition.Begin();
            }
        }
    }

    private void OnClick_CloseDailyReward()
    {
        Debug.Log("[MainUI] Closing daily reward");
        if (dailyRewardPanel)
        {
            if (dailyRewardHideTransition != null && dailyRewardHideTransition.IsUsed)
            {
                dailyRewardHideTransition.Begin();
                Invoke(nameof(HideDailyRewardPanel), 0.3f);
            }
            else
            {
                dailyRewardPanel.SetActive(false);
            }
        }
    }

    private void HideDailyRewardPanel()
    {
        if (dailyRewardPanel) dailyRewardPanel.SetActive(false);
    }

    private void OnClick_ClaimDailyReward()
    {
        Debug.Log("[MainUI] Claiming daily reward");
        
        // Play claim transition
        if (dailyRewardClaimTransition != null && dailyRewardClaimTransition.IsUsed)
        {
            dailyRewardClaimTransition.Begin();
        }
        
        DailyRewardClient.ClaimAsync();
        ShowNotification("Récompense quotidienne récupérée !");

        _ = userController.RefreshStatusAsync();
        
        // Auto-close after a delay
        Invoke(nameof(OnClick_CloseDailyReward), 1.5f);
    }
    #endregion

    #region Notifications Callbacks
    private void OnClick_CloseNotification()
    {
        // Cancel auto-close if manually closing
        CancelInvoke(nameof(OnClick_CloseNotification));
        
        // Play hide transition before closing
        if (notificationHideTransition != null && notificationHideTransition.IsUsed)
        {
            notificationHideTransition.Begin();
            // Delay closing to allow transition to play
            Invoke(nameof(HideNotificationPanel), 0.3f);
        }
        else
        {
            HideNotificationPanel();
        }
    }

    private void HideNotificationPanel()
    {
        if (notificationPanel) notificationPanel.SetActive(false);
        
        // Process next notification in queue
        ProcessNextNotification();
    }

    private void OnClick_ConfirmYes()
    {
        // Play hide transition before closing
        if (confirmationHideTransition != null && confirmationHideTransition.IsUsed)
        {
            confirmationHideTransition.Begin();
            // Delay closing to allow transition to play
            Invoke(nameof(HideConfirmationAndCallYes), 0.3f);
        }
        else
        {
            HideConfirmationAndCallYes();
        }
    }

    private void HideConfirmationAndCallYes()
    {
        if (confirmationPopup) confirmationPopup.SetActive(false);
        currentConfirmationYesCallback?.Invoke();
        currentConfirmationYesCallback = null;
        currentConfirmationNoCallback = null;
    }

    private void OnClick_ConfirmNo()
    {
        // Play hide transition before closing
        if (confirmationHideTransition != null && confirmationHideTransition.IsUsed)
        {
            confirmationHideTransition.Begin();
            // Delay closing to allow transition to play
            Invoke(nameof(HideConfirmationAndCallNo), 0.3f);
        }
        else
        {
            HideConfirmationAndCallNo();
        }
    }

    private void HideConfirmationAndCallNo()
    {
        if (confirmationPopup) confirmationPopup.SetActive(false);
        currentConfirmationNoCallback?.Invoke();
        currentConfirmationYesCallback = null;
        currentConfirmationNoCallback = null;
    }
    #endregion
}
