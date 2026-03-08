using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [SerializeField] private GameObject dailyRewardRedDot;
    #endregion

    #region Settings
    [Header("=== Settings ===")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button deleteAccountButton;
    [SerializeField] private Button privacyPolicyButton; // Nouveau bouton pour la politique de confidentialité
    
    [Header("Settings Transitions")]
    [SerializeField] private LeanPlayer settingsShowTransition;
    [SerializeField] private LeanPlayer settingsHideTransition;
    
    [Header("Privacy & Data")]
    [Tooltip("URL de votre politique de confidentialité")]
    [SerializeField] private string privacyPolicyURL = "https://docs.google.com/document/d/e/2PACX-1vRbrHDxjUO4o8WWpW8BSaMfJCUwnPLqkSXxPFk6RJzPlXx95g-HvNMNehf5jJO_Y5H4asb3CJfxFsUl/pub";

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
    
    [Header("Daily Reward — CollectableItem Prefab")]
    [Tooltip("Prefab avec CollectableItemDisplay, instancié pour chaque récompense.")]
    [SerializeField] private GameObject collectableItemPrefab;

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
        _ = RefreshDailyRewardIndicatorAsync();
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
        RegisterButton(privacyPolicyButton, OnClick_PrivacyPolicy);

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
        if (privacyPolicyButton) privacyPolicyButton.onClick.RemoveAllListeners();
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
            "Cette action supprimera toutes vos données de jeu. Pour supprimer définitivement votre compte Unity, un formulaire s'ouvrira. Continuer ?",
            async () => {
                Debug.Log("[MainUI] User confirmed account deletion");
                
                try
                {
                    // 1. Ouvrir le lien de demande de suppression
                    Application.OpenURL(AuthController.ACCOUNT_DELETION_REQUEST_URL);
                    
                    // 2. Supprimer toutes les données locales et cloud
                    await AuthController.Instance.DeleteAccount();
                    
                    ShowNotification("Données supprimées. Formulaire de suppression ouvert.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[MainUI] Erreur lors de la suppression: {ex.Message}");
                    ShowNotification("Erreur lors de la suppression du compte");
                }
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

    private void OnClick_PrivacyPolicy()
    {
        Debug.Log($"[MainUI] Opening privacy policy: {privacyPolicyURL}");
        if (!string.IsNullOrEmpty(privacyPolicyURL))
        {
            Application.OpenURL(privacyPolicyURL);
        }
        else
        {
            Debug.LogWarning("[MainUI] Privacy policy URL not configured");
            ShowNotification("URL de la politique de confidentialité non configurée");
        }
    }
    #endregion

    #region Daily Reward Callbacks

    private void OnClick_OpenDailyReward()
    {
        _ = OpenDailyRewardAsync();
    }

    private async Task OpenDailyRewardAsync()
    {
        Debug.Log("[MainUI] Opening daily reward");
        if (dailyRewardPanel)
        {
            dailyRewardPanel.SetActive(true);
            if (dailyRewardShowTransition != null && dailyRewardShowTransition.IsUsed)
                dailyRewardShowTransition.Begin();
        }
        await PopulateDailyRewardItemsAsync();
        await RefreshDailyRewardIndicatorAsync();
    }

    private async Task RefreshDailyRewardIndicatorAsync()
    {
        try
        {
            var status = await DailyRewardClient.GetStatusAsync();
            if (dailyRewardRedDot != null)
                dailyRewardRedDot.SetActive(status.CanClaim);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainUI] Impossible de récupérer le statut Daily Reward: {ex.Message}");
            if (dailyRewardRedDot != null)
                dailyRewardRedDot.SetActive(false);
        }
    }

    private async Task PopulateDailyRewardItemsAsync()
    {
        if (dailyRewardItemsContainer == null || collectableItemPrefab == null)
        {
            Debug.LogWarning("[MainUI] dailyRewardItemsContainer ou collectableItemPrefab non assigné.");
            return;
        }

        // Vider les items précédents
        foreach (Transform child in dailyRewardItemsContainer)
            Destroy(child.gameObject);

        var config = await DailyRewardRemoteConfig.GetConfigAsync();
        if (config == null || config.rewards == null || config.rewards.Length == 0)
        {
            Debug.LogWarning("[MainUI] Aucune config de récompenses journalières trouvée.");
            return;
        }

        foreach (var reward in config.rewards)
        {
            var go = Instantiate(collectableItemPrefab, dailyRewardItemsContainer);
            go.GetComponent<CollectableItemDisplay>()?.SetItem(reward);
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
        _ = ClaimDailyRewardAsync();
    }

    private async Task ClaimDailyRewardAsync()
    {
        Debug.Log("[MainUI] Claiming daily reward");

        if (claimDailyRewardButton != null)
            claimDailyRewardButton.interactable = false;

        if (dailyRewardClaimTransition != null && dailyRewardClaimTransition.IsUsed)
            dailyRewardClaimTransition.Begin();

        DailyRewardResult result;
        try
        {
            result = await DailyRewardClient.ClaimAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MainUI] Erreur réseau lors du claim : {ex.Message}");
            ShowNotification("Erreur réseau. Impossible de réclamer la récompense.");
            if (claimDailyRewardButton != null) claimDailyRewardButton.interactable = true;
            return;
        }

        if (result.ok)
        {
            ShowNotification(BuildSuccessMessage(result));

            // Recharger les données (tokens + packs accordés côté serveur)
            await userController.RefreshStatusAsync();
            await PlayerProfileStore.LoadPackCollectionAsync();
            PlayerProfileStore.OnPackCollectionChanged?.Invoke();
            await RefreshDailyRewardIndicatorAsync();

            Invoke(nameof(OnClick_CloseDailyReward), 2f);
        }
        else
        {
            ShowNotification(BuildErrorMessage(result));
            await RefreshDailyRewardIndicatorAsync();
        }

        // Toujours réactiver le bouton
        if (claimDailyRewardButton != null) claimDailyRewardButton.interactable = true;
    }

    private string BuildSuccessMessage(DailyRewardResult result)
    {
        if (result.grantedRewards == null || result.grantedRewards.Length == 0)
            return "Récompenses du jour réclamées !";

        var parts = new List<string>();
        foreach (var r in result.grantedRewards)
        {
            parts.Add(FormatGrantedReward(r));
        }
        return $"Récompenses reçues : {string.Join(", ", parts)} !"; 
    }

    private static string FormatGrantedReward(GrantedReward reward)
    {
        var amount = Mathf.Max(0, reward.amount);

        switch (reward.type)
        {
            case "TOKEN":
                return $"{amount} TOKEN";
            case "PC":
                return $"{amount} PC";
            case "PACK":
                return string.IsNullOrEmpty(reward.packId)
                    ? $"{amount} pack{(amount > 1 ? "s" : "")}"
                    : $"{amount} pack{(amount > 1 ? "s" : "")} ({reward.packId})";
            default:
                return $"{amount} {reward.type}";
        }
    }

    private string BuildErrorMessage(DailyRewardResult result)
    {
        switch (result.errorCode)
        {
            case "ALREADY_CLAIMED":
                int h = result.cooldownSecondsRemaining / 3600;
                int m = (result.cooldownSecondsRemaining % 3600) / 60;
                if (h > 0)
                    return $"Récompense déjà réclamée. Reviens dans {h}h{m:D2}min !";
                return $"Récompense déjà réclamée. Reviens dans {m} minute{(m > 1 ? "s" : "")} !";

            case "CONFIG_NOT_FOUND":
            case "CONFIG_ERROR":
                return "Configuration des récompenses introuvable. Réessaie plus tard.";

            case "GRANT_FAILED":
                return result.message;

            default:
                return string.IsNullOrEmpty(result.message)
                    ? "Impossible de réclamer la récompense. Réessaie plus tard."
                    : result.message;
        }
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
