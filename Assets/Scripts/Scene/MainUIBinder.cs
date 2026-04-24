using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lean.Transition;

public class MainUIBinder : MonoBehaviour
{
    public static MainUIBinder Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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
    [SerializeField] private Button privacyPolicyButton;

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

    private PlayerStatusController userController;
    private static NotificationSystem Notif => NotificationSystem.Instance;

    #region Unity Lifecycle
    private void Start()
    {
        RegisterButton(dailyRewardButton,      OnClick_OpenDailyReward);
        RegisterButton(settingsButton,         OnClick_OpenSettings);
        RegisterButton(closeSettingsButton,    OnClick_CloseSettings);
        RegisterButton(disconnectButton,       OnClick_Disconnect);
        RegisterButton(deleteAccountButton,    OnClick_DeleteAccount);
        RegisterButton(privacyPolicyButton,    OnClick_PrivacyPolicy);
        RegisterButton(closeDailyRewardButton, OnClick_CloseDailyReward);
        RegisterButton(claimDailyRewardButton, OnClick_ClaimDailyReward);

        RegisterToggle(muteToggle,       OnToggle_Mute);
        RegisterToggle(fullscreenToggle, OnToggle_Fullscreen);

        userController = FindFirstObjectByType<PlayerStatusController>();
        _ = RefreshDailyRewardIndicatorAsync();
    }

    private void OnDestroy()
    {
        if (settingsButton)         settingsButton.onClick.RemoveAllListeners();
        if (closeSettingsButton)    closeSettingsButton.onClick.RemoveAllListeners();
        if (disconnectButton)       disconnectButton.onClick.RemoveAllListeners();
        if (deleteAccountButton)    deleteAccountButton.onClick.RemoveAllListeners();
        if (privacyPolicyButton)    privacyPolicyButton.onClick.RemoveAllListeners();
        if (dailyRewardButton)      dailyRewardButton.onClick.RemoveAllListeners();
        if (closeDailyRewardButton) closeDailyRewardButton.onClick.RemoveAllListeners();
        if (claimDailyRewardButton) claimDailyRewardButton.onClick.RemoveAllListeners();
        if (muteToggle)             muteToggle.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle)       fullscreenToggle.onValueChanged.RemoveAllListeners();
    }
    #endregion

    #region Helpers
    private void RegisterButton(Button button, Action callback)
    {
        if (button != null) button.onClick.AddListener(() => callback?.Invoke());
    }

    private void RegisterToggle(Toggle toggle, Action<bool> callback)
    {
        if (toggle != null) toggle.onValueChanged.AddListener(v => callback?.Invoke(v));
    }
    #endregion

    #region Settings
    private void OnClick_OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        if (settingsShowTransition != null && settingsShowTransition.IsUsed)
            settingsShowTransition.Begin();
    }

    private void OnClick_CloseSettings()
    {
        if (settingsPanel == null) return;
        if (settingsHideTransition != null && settingsHideTransition.IsUsed)
        {
            settingsHideTransition.Begin();
            Invoke(nameof(HideSettingsPanel), 0.3f);
        }
        else settingsPanel.SetActive(false);
    }

    private void HideSettingsPanel() { if (settingsPanel) settingsPanel.SetActive(false); }

    private void OnClick_Disconnect()
    {
        Notif?.ShowConfirmation(
            "Déconnexion",
            "Voulez-vous vraiment vous déconnecter ?",
            () => AuthController.Instance.SignOut()
        );
    }

    private void OnClick_DeleteAccount()
    {
        Notif?.ShowConfirmation(
            "Suppression du compte",
            "Cette action supprimera toutes vos données de jeu. Un formulaire s'ouvrira. Continuer ?",
            async () => {
                try
                {
                    Application.OpenURL(AuthController.ACCOUNT_DELETION_REQUEST_URL);
                    await AuthController.Instance.DeleteAccount();
                    Notif?.ShowNotification("Données supprimées. Formulaire de suppression ouvert.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainUIBinder] Erreur suppression compte : {ex.Message}");
                    Notif?.ShowNotification("Erreur lors de la suppression du compte.");
                }
            }
        );
    }

    private void OnToggle_Mute(bool isMuted)       => AudioListener.volume = isMuted ? 0f : 1f;
    private void OnToggle_Fullscreen(bool value)   => Screen.fullScreen = value;

    private void OnClick_PrivacyPolicy()
    {
        if (!string.IsNullOrEmpty(privacyPolicyURL))
            Application.OpenURL(privacyPolicyURL);
        else
            Notif?.ShowNotification("URL de la politique de confidentialité non configurée.");
    }
    #endregion

    #region Daily Reward
    private void OnClick_OpenDailyReward() => _ = OpenDailyRewardAsync();

    private async Task OpenDailyRewardAsync()
    {
        if (dailyRewardPanel)
        {
            dailyRewardPanel.SetActive(true);
            if (dailyRewardShowTransition != null && dailyRewardShowTransition.IsUsed)
                dailyRewardShowTransition.Begin();
        }
        await PopulateDailyRewardItemsAsync();
        await RefreshDailyRewardIndicatorAsync();
    }

    public async Task RefreshDailyRewardIndicatorAsync()
    {
        try
        {
            var status = await DailyRewardClient.GetStatusAsync();
            if (dailyRewardRedDot != null) dailyRewardRedDot.SetActive(status.CanClaim);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainUIBinder] Statut daily reward indisponible : {ex.Message}");
            if (dailyRewardRedDot != null) dailyRewardRedDot.SetActive(false);
        }
    }

    private async Task PopulateDailyRewardItemsAsync()
    {
        if (dailyRewardItemsContainer == null || collectableItemPrefab == null) return;

        foreach (Transform child in dailyRewardItemsContainer)
            Destroy(child.gameObject);

        var config = await DailyRewardRemoteConfig.GetConfigAsync();
        if (config?.rewards == null || config.rewards.Length == 0) return;

        foreach (var reward in config.rewards)
        {
            var go = Instantiate(collectableItemPrefab, dailyRewardItemsContainer);
            go.GetComponent<CollectableItemDisplay>()?.SetItem(reward);
        }
    }

    private void OnClick_CloseDailyReward()
    {
        if (dailyRewardPanel == null) return;
        if (dailyRewardHideTransition != null && dailyRewardHideTransition.IsUsed)
        {
            dailyRewardHideTransition.Begin();
            Invoke(nameof(HideDailyRewardPanel), 0.3f);
        }
        else dailyRewardPanel.SetActive(false);
    }

    private void HideDailyRewardPanel() { if (dailyRewardPanel) dailyRewardPanel.SetActive(false); }

    private void OnClick_ClaimDailyReward() => _ = ClaimDailyRewardAsync();

    private async Task ClaimDailyRewardAsync()
    {
        if (claimDailyRewardButton != null) claimDailyRewardButton.interactable = false;
        if (dailyRewardClaimTransition != null && dailyRewardClaimTransition.IsUsed)
            dailyRewardClaimTransition.Begin();

        DailyRewardResult result;
        try { result = await DailyRewardClient.ClaimAsync(); }
        catch (Exception ex)
        {
            Debug.LogError($"[MainUIBinder] Erreur réseau daily reward : {ex.Message}");
            Notif?.ShowNotification("Erreur réseau. Impossible de réclamer la récompense.");
            if (claimDailyRewardButton != null) claimDailyRewardButton.interactable = true;
            return;
        }

        if (result.ok)
        {
            Notif?.ShowNotification(BuildSuccessMessage(result));
            await userController.RefreshStatusAsync();
            await PlayerProfileStore.LoadPackCollectionAsync();
            await RefreshDailyRewardIndicatorAsync();
            Invoke(nameof(OnClick_CloseDailyReward), 2f);
        }
        else
        {
            Notif?.ShowNotification(BuildErrorMessage(result));
            await RefreshDailyRewardIndicatorAsync();
        }

        if (claimDailyRewardButton != null) claimDailyRewardButton.interactable = true;
    }

    private string BuildSuccessMessage(DailyRewardResult result)
    {
        if (result.grantedRewards == null || result.grantedRewards.Length == 0)
            return "Récompenses du jour réclamées !";
        var parts = new List<string>();
        foreach (var r in result.grantedRewards) parts.Add(FormatReward(r));
        return $"Récompenses reçues : {string.Join(", ", parts)} !";
    }

    private static string FormatReward(GrantedReward r)
    {
        int amount = Mathf.Max(0, r.amount);
        return r.type switch
        {
            "TOKEN" => $"{amount} TOKEN",
            "PC"    => $"{amount} PC",
            "PACK"  => string.IsNullOrEmpty(r.packId)
                           ? $"{amount} pack{(amount > 1 ? "s" : "")}"
                           : $"{amount} pack{(amount > 1 ? "s" : "")} ({r.packId})",
            _       => $"{amount} {r.type}"
        };
    }

    private string BuildErrorMessage(DailyRewardResult result)
    {
        switch (result.errorCode)
        {
            case "ALREADY_CLAIMED":
                int h = result.cooldownSecondsRemaining / 3600;
                int m = (result.cooldownSecondsRemaining % 3600) / 60;
                return h > 0
                    ? $"Récompense déjà réclamée. Reviens dans {h}h{m:D2}min !"
                    : $"Récompense déjà réclamée. Reviens dans {m} minute{(m > 1 ? "s" : "")} !";
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
}
