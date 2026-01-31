using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Lean.Transition;

/// <summary>
/// Binder principal pour tous les boutons et éléments UI de l'interface de jeu.
/// Organisé par catégories : Main Page, Settings, Collection, etc.
/// Utilise LeanGUI pour les transitions et animations.
/// 
/// === COMMENT UTILISER LES TRANSITIONS LEAN ===
/// 1. Créez un GameObject vide comme enfant de votre panel (ex: "ShowTransition")
/// 2. Sur ce GameObject, ajoutez des composants LeanTransition (ex: LeanCanvasGroupAlpha, LeanScale)
///    - Trouvez-les dans : Add Component > Lean > Transition > Method
/// 3. Configurez chaque composant :
///    - Target: le panel à animer (ou laissez vide pour animer le GameObject lui-même)
///    - Duration: durée de l'animation (ex: 0.3)
///    - Ease: courbe d'animation (ex: Smooth)
///    - Pour Show: Alpha 0→1, Scale 0.8→1, etc.
///    - Pour Hide: Alpha 1→0, Scale 1→0.8, etc.
/// 4. Dans l'inspecteur de MainUIBinder, assignez ce GameObject au champ LeanPlayer correspondant
/// 
/// Exemple de transitions recommandées :
/// - Popup Show: Scale (0.8→1) + CanvasGroup Alpha (0→1) sur 0.3s avec Ease Smooth
/// - Popup Hide: Scale (1→0.8) + CanvasGroup Alpha (1→0) sur 0.2s avec Ease Smooth
/// - Panel Slide: Position (offset→0) + CanvasGroup Alpha (0→1) sur 0.3s
/// </summary>
public class MainUIBinder : MonoBehaviour
{
    #region Bottom Bar
    [Header("=== Bottom Bar ===")]
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button collectionButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button dailyRewardButton;
    [SerializeField] private Button achievementsButton;
    #endregion

    #region Settings
    [Header("=== Settings ===")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button deleteAccountButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button supportButton;
    
    [Header("Settings Transitions")]
    [SerializeField] private LeanPlayer settingsShowTransition;
    [SerializeField] private LeanPlayer settingsHideTransition;

    [Header("Settings - Audio")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Settings - Graphics")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Settings - Gameplay")]
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle notificationsToggle;
    [SerializeField] private TMP_Dropdown languageDropdown;
    #endregion

    #region Collection Page
    [Header("=== Collection Page ===")]
    [SerializeField] private GameObject collectionPanel;
    [SerializeField] private Button openCollectionButton;
    [SerializeField] private Button closeCollectionButton;
    [SerializeField] private Button collectionFilterAllButton;
    [SerializeField] private Button collectionFilterOwnedButton;
    [SerializeField] private Button collectionFilterLockedButton;
    [SerializeField] private Transform collectionItemsContainer;
    [SerializeField] private GameObject collectionItemPrefab;
    
    [Header("Collection Transitions")]
    [SerializeField] private LeanPlayer collectionShowTransition;
    [SerializeField] private LeanPlayer collectionHideTransition;

    [Header("Collection - Item Details")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Button equipItemButton;
    [SerializeField] private Button unequipItemButton;
    #endregion

    #region Shop Page
    [Header("=== Shop Page ===")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button openShopButton;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private Button shopTabCurrencyButton;
    [SerializeField] private Button shopTabItemsButton;
    [SerializeField] private Button shopTabSpecialButton;
    [SerializeField] private Transform shopItemsContainer;
    [SerializeField] private GameObject shopItemPrefab;
    
    [Header("Shop Transitions")]
    [SerializeField] private LeanPlayer shopShowTransition;
    [SerializeField] private LeanPlayer shopHideTransition;
    
    [Header("Shop - Purchase")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchasePriceText;
    [SerializeField] private TMP_Text shopStatusText;
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

    #region Profile Page
    [Header("=== Profile Page ===")]
    [SerializeField] private Button openProfileButton;
    [SerializeField] private Button closeProfileButton;
    [SerializeField] private Button editProfileButton;
    [SerializeField] private Button changeAvatarButton;
    [SerializeField] private TMP_InputField usernameInputField;
    
    [Header("Profile - Stats")]
    [SerializeField] private TMP_Text profileLevelText;
    [SerializeField] private TMP_Text profileXPText;
    [SerializeField] private TMP_Text gamesPlayedText;
    [SerializeField] private TMP_Text winsText;
    [SerializeField] private TMP_Text lossesText;
    [SerializeField] private Image avatarImage;
    #endregion

    #region Leaderboard
    [Header("=== Leaderboard ===")]
    [SerializeField] private GameObject leaderboardEntryPrefab;
    #endregion

    #region Achievements
    [Header("=== Achievements === (OPTIONAL)")]
    [SerializeField] private Button openAchievementsButton;
    [SerializeField] private Button closeAchievementsButton;
    [SerializeField] private Transform achievementsContainer;
    [SerializeField] private GameObject achievementItemPrefab;
    [SerializeField] private TMP_Text achievementProgressText;
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

    #region Unity Lifecycle
    private void Start()
    {
        InitializeButtons();
        InitializeToggles();
        InitializeSliders();
        userController = FindFirstObjectByType<PlayerStatusController>();
    }

    private void OnDestroy()
    {
        UnregisterButtons();
        UnregisterToggles();
        UnregisterSliders();
    }
    #endregion

    #region Initialization
    private void InitializeButtons()
    {
        // Main Page
        RegisterButton(collectionButton, OnClick_OpenCollection);
        RegisterButton(shopButton, OnClick_OpenShop);
        RegisterButton(dailyRewardButton, OnClick_OpenDailyReward);
        RegisterButton(achievementsButton, OnClick_OpenAchievements);
        RegisterButton(leaderboardButton, OnClick_OpenLeaderboard);

        // Settings
        RegisterButton(settingsButton, OnClick_OpenSettings);
        RegisterButton(closeSettingsButton, OnClick_CloseSettings);
        RegisterButton(disconnectButton, OnClick_Disconnect);
        RegisterButton(deleteAccountButton, OnClick_DeleteAccount);
        RegisterButton(creditsButton, OnClick_Credits);
        RegisterButton(supportButton, OnClick_Support);

        // Collection
        RegisterButton(openCollectionButton, OnClick_OpenCollection);
        RegisterButton(closeCollectionButton, OnClick_CloseCollection);
        RegisterButton(collectionFilterAllButton, () => OnClick_CollectionFilter("All"));
        RegisterButton(collectionFilterOwnedButton, () => OnClick_CollectionFilter("Owned"));
        RegisterButton(collectionFilterLockedButton, () => OnClick_CollectionFilter("Locked"));
        RegisterButton(equipItemButton, OnClick_EquipItem);
        RegisterButton(unequipItemButton, OnClick_UnequipItem);

        // Shop
        RegisterButton(openShopButton, OnClick_OpenShop);
        RegisterButton(closeShopButton, OnClick_CloseShop);
        RegisterButton(shopTabCurrencyButton, () => OnClick_ShopTab("Currency"));
        RegisterButton(shopTabItemsButton, () => OnClick_ShopTab("Items"));
        RegisterButton(shopTabSpecialButton, () => OnClick_ShopTab("Special"));
        RegisterButton(purchaseButton, OnClick_Purchase);

        // Daily Reward
        RegisterButton(closeDailyRewardButton, OnClick_CloseDailyReward);
        RegisterButton(claimDailyRewardButton, OnClick_ClaimDailyReward);

        // Profile
        RegisterButton(openProfileButton, OnClick_OpenProfile);
        RegisterButton(closeProfileButton, OnClick_CloseProfile);
        RegisterButton(editProfileButton, OnClick_EditProfile);
        RegisterButton(changeAvatarButton, OnClick_ChangeAvatar);

        // Achievements
        RegisterButton(openAchievementsButton, OnClick_OpenAchievements);
        RegisterButton(closeAchievementsButton, OnClick_CloseAchievements);

        // Notifications
        RegisterButton(closeNotificationButton, OnClick_CloseNotification);
        RegisterButton(confirmYesButton, OnClick_ConfirmYes);
        RegisterButton(confirmNoButton, OnClick_ConfirmNo);
    }

    private void InitializeToggles()
    {
        RegisterToggle(muteToggle, OnToggle_Mute);
        RegisterToggle(fullscreenToggle, OnToggle_Fullscreen);
        RegisterToggle(vibrationToggle, OnToggle_Vibration);
        RegisterToggle(notificationsToggle, OnToggle_Notifications);
    }

    private void InitializeSliders()
    {
        RegisterSlider(musicVolumeSlider, OnSlider_MusicVolume);
        RegisterSlider(sfxVolumeSlider, OnSlider_SFXVolume);
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

    private void RegisterSlider(Slider slider, Action<float> callback)
    {
        if (slider != null)
            slider.onValueChanged.AddListener(value => callback?.Invoke(value));
    }

    private void UnregisterButtons()
    {
        // Cleanup all button listeners
        if (settingsButton) settingsButton.onClick.RemoveAllListeners();
        if (disconnectButton) disconnectButton.onClick.RemoveAllListeners();
        // Add more as needed...
    }

    private void UnregisterToggles()
    {
        if (muteToggle) muteToggle.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle) fullscreenToggle.onValueChanged.RemoveAllListeners();
        // Add more as needed...
    }

    private void UnregisterSliders()
    {
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }

    public void ShowNotification(string message)
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            if (notificationText) notificationText.text = message;
            
            // Play show transition
            if (notificationShowTransition != null && notificationShowTransition.IsUsed)
            {
                notificationShowTransition.Begin();
            }
        }
        Debug.Log($"[MainUI] {message}");
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

    #region Main Page Callbacks
    private void OnClick_Play()
    {
        Debug.Log("[MainUI] Play button clicked");
        // TODO: Implement game start logic
        ShowNotification("Lancement de la partie...");
    }

    private void OnClick_OpenCollection()
    {
        Debug.Log("[MainUI] Opening collection");
        if (collectionPanel)
        {
            collectionPanel.SetActive(true);
            if (collectionShowTransition != null && collectionShowTransition.IsUsed)
            {
                collectionShowTransition.Begin();
            }
        }
    }

    private void OnClick_OpenShop()
    {
        Debug.Log("[MainUI] Opening shop");
        if (shopPanel)
        {
            shopPanel.SetActive(true);
            if (shopShowTransition != null && shopShowTransition.IsUsed)
            {
                shopShowTransition.Begin();
            }
        }
    }

    private void OnClick_OpenProfile()
    {
        Debug.Log("[MainUI] Opening profile");
        // TODO: Open profile panel
    }

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
        // TODO: Load daily reward data
    }

    private void OnClick_OpenAchievements()
    {
        Debug.Log("[MainUI] Opening achievements");
        // TODO: Open achievements panel
    }

    private void OnClick_OpenLeaderboard()
    {
        Debug.Log("[MainUI] Opening leaderboard");
        // TODO: Open leaderboard panel
    }
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

    private void OnClick_Credits()
    {
        Debug.Log("[MainUI] Opening credits");
        // TODO: Show credits panel
    }

    private void OnClick_Support()
    {
        Debug.Log("[MainUI] Opening support");
        // TODO: Open support link or panel
    }

    private void OnToggle_Mute(bool isMuted)
    {
        Debug.Log($"[MainUI] Mute toggled: {isMuted}");
        // TODO: Implement audio muting
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    private void OnToggle_Fullscreen(bool isFullscreen)
    {
        Debug.Log($"[MainUI] Fullscreen toggled: {isFullscreen}");
        Screen.fullScreen = isFullscreen;
    }

    private void OnToggle_Vibration(bool isEnabled)
    {
        Debug.Log($"[MainUI] Vibration toggled: {isEnabled}");
        // TODO: Save vibration preference
    }

    private void OnToggle_Notifications(bool isEnabled)
    {
        Debug.Log($"[MainUI] Notifications toggled: {isEnabled}");
        // TODO: Save notification preference
    }

    private void OnSlider_MusicVolume(float value)
    {
        Debug.Log($"[MainUI] Music volume: {value}");
        // TODO: Set music volume
    }

    private void OnSlider_SFXVolume(float value)
    {
        Debug.Log($"[MainUI] SFX volume: {value}");
        // TODO: Set SFX volume
    }
    #endregion

    #region Collection Callbacks
    private void OnClick_CloseCollection()
    {
        Debug.Log("[MainUI] Closing collection");
        if (collectionPanel)
        {
            if (collectionHideTransition != null && collectionHideTransition.IsUsed)
            {
                collectionHideTransition.Begin();
                Invoke(nameof(HideCollectionPanel), 0.3f);
            }
            else
            {
                collectionPanel.SetActive(false);
            }
        }
    }

    private void HideCollectionPanel()
    {
        if (collectionPanel) collectionPanel.SetActive(false);
    }

    private void OnClick_CollectionFilter(string filterType)
    {
        Debug.Log($"[MainUI] Collection filter: {filterType}");
        // TODO: Filter collection items
    }

    private void OnClick_EquipItem()
    {
        Debug.Log("[MainUI] Equipping item");
        // TODO: Equip selected item
        ShowNotification("Objet équipé");
    }

    private void OnClick_UnequipItem()
    {
        Debug.Log("[MainUI] Unequipping item");
        // TODO: Unequip selected item
        ShowNotification("Objet déséquipé");
    }
    #endregion

    #region Shop Callbacks
    private void OnClick_CloseShop()
    {
        Debug.Log("[MainUI] Closing shop");
        if (shopPanel)
        {
            if (shopHideTransition != null && shopHideTransition.IsUsed)
            {
                shopHideTransition.Begin();
                Invoke(nameof(HideShopPanel), 0.3f);
            }
            else
            {
                shopPanel.SetActive(false);
            }
        }
    }

    private void HideShopPanel()
    {
        if (shopPanel) shopPanel.SetActive(false);
    }

    private void OnClick_ShopTab(string tabName)
    {
        Debug.Log($"[MainUI] Shop tab: {tabName}");
        // TODO: Switch shop tab
    }

    private void OnClick_Purchase()
    {
        Debug.Log("[MainUI] Purchase requested");
        // TODO: Implement purchase logic
        ShowNotification("Achat effectué !");
    }
    #endregion

    #region Daily Reward Callbacks
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

    #region Profile Callbacks
    private void OnClick_CloseProfile()
    {
        Debug.Log("[MainUI] Closing profile");
        // TODO: Close profile panel
    }

    private void OnClick_EditProfile()
    {
        Debug.Log("[MainUI] Editing profile");
        // TODO: Enable profile editing
    }

    private void OnClick_ChangeAvatar()
    {
        Debug.Log("[MainUI] Changing avatar");
        // TODO: Open avatar selection
    }
    #endregion

    #region Leaderboard Callbacks
    private void OnClick_CloseLeaderboard()
    {
        Debug.Log("[MainUI] Closing leaderboard");
        // TODO: Close leaderboard panel
    }

    private void OnClick_LeaderboardPeriod(string period)
    {
        Debug.Log($"[MainUI] Leaderboard period: {period}");
        // TODO: Load leaderboard for specified period
    }
    #endregion

    #region Achievements Callbacks
    private void OnClick_CloseAchievements()
    {
        Debug.Log("[MainUI] Closing achievements");
        // TODO: Close achievements panel
    }
    #endregion

    #region Notifications Callbacks
    private void OnClick_CloseNotification()
    {
        // Play hide transition before closing
        if (notificationHideTransition != null && notificationHideTransition.IsUsed)
        {
            notificationHideTransition.Begin();
            // Delay closing to allow transition to play
            Invoke(nameof(HideNotificationPanel), 0.3f);
        }
        else
        {
            if (notificationPanel) notificationPanel.SetActive(false);
        }
    }

    private void HideNotificationPanel()
    {
        if (notificationPanel) notificationPanel.SetActive(false);
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
