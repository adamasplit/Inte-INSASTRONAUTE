using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject configurationPanel;
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private MultiplayerDeckPanel multiplayerDeckPanel;

    [Header("Configuration")]
    [SerializeField] private TMP_Dropdown characterDropdown;
    [SerializeField] private Button saveProfileButton;
    [SerializeField] private Button openDeckButton;
    [SerializeField] private Button closeDeckButton;
    [SerializeField] private Button quickMatchButton;
    [SerializeField] private Button challengeButton;
    [SerializeField] private TMP_InputField challengeTargetInput;
    [SerializeField] private Toggle friendlyMatchToggle;
    [SerializeField] private TextMeshProUGUI playerIdText;

    [Header("Notification")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Button notificationOkButton;

    private readonly List<SelectableCharacter> availableCharacters = new();
    private bool suppressDropdownCallback;
    private bool isQuickMatchQueued;

    private void Start()
    {
        STSSceneLoader.Instance?.SceneReady();
    }

    private async void Awake()
    {
        BuildCharacterDropdown();
        WireButtons();

        if (multiplayerDeckPanel != null)
        {
            multiplayerDeckPanel.SetHost(this);
        }

        await LoadRemotePvpProfileAsync();
        ShowConfigurationPanel();

        // « Revanche » : l'écran de fin de duel nous a renvoyés ici en demandant une
        // nouvelle recherche. Il n'existe pas d'endpoint de revanche côté serveur, donc
        // c'est un matchmaking ordinaire, pas un rematch contre le même joueur.
        if (RunManager.Instance != null && RunManager.Instance.ConsumePvpQuickMatchRequest())
        {
            await QuickMatchAsync();
        }
    }

    private void BuildCharacterDropdown()
    {
        if (characterDropdown == null)
        {
            return;
        }

        availableCharacters.Clear();
        characterDropdown.ClearOptions();

        List<string> options = new();
        foreach (SelectableCharacter character in Enum.GetValues(typeof(SelectableCharacter)))
        {
            if (character == SelectableCharacter.Aucun
                || character == SelectableCharacter.Impossible
                || character == SelectableCharacter.Starting)
            {
                continue;
            }

            availableCharacters.Add(character);
            options.Add(character.ToString());
        }

        characterDropdown.AddOptions(options);
        characterDropdown.onValueChanged.RemoveAllListeners();
        characterDropdown.onValueChanged.AddListener(OnCharacterChanged);
    }

    private void WireButtons()
    {
        if (saveProfileButton != null)
        {
            saveProfileButton.onClick.RemoveAllListeners();
            saveProfileButton.onClick.AddListener(() => _ = SaveProfileAsync());
        }

        if (quickMatchButton != null)
        {
            quickMatchButton.onClick.RemoveAllListeners();
            quickMatchButton.onClick.AddListener(() => _ = QuickMatchAsync());
        }

        if (openDeckButton != null)
        {
            openDeckButton.onClick.RemoveAllListeners();
            openDeckButton.onClick.AddListener(OpenDeckPanel);
        }

        if (closeDeckButton != null)
        {
            closeDeckButton.onClick.RemoveAllListeners();
            closeDeckButton.onClick.AddListener(ShowConfigurationPanel);
        }

        if (challengeButton != null)
        {
            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(() => _ = SendChallengeAsync());
        }

        if (notificationOkButton != null)
        {
            notificationOkButton.onClick.RemoveAllListeners();
            notificationOkButton.onClick.AddListener(HideNotification);
        }
    }

    private async Task LoadRemotePvpProfileAsync()
    {
        try
        {
            JToken profile = await STSApiClient.GetPvpProfileAsync();
            if (profile == null)
            {
                return;
            }

            string selectedCharacter = profile.Value<string>("selectedCharacter");
            if (Enum.TryParse(selectedCharacter, true, out SelectableCharacter character))
            {
                SelectCharacter(character);
            }

            string playerId = profile.Value<string>("playerId")
                ?? profile.Value<string>("userId")
                ?? profile.Value<string>("id");
            if (playerIdText != null && !string.IsNullOrWhiteSpace(playerId))
            {
                playerIdText.text = $"ID: {playerId}";
            }

            if (RunManager.Instance != null && !string.IsNullOrWhiteSpace(playerId))
            {
                RunManager.Instance.pvpLocalUserId = playerId;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load PVP profile: {ex.Message}");
        }
    }

    public void ShowConfigurationPanel()
    {
        if (configurationPanel != null)
        {
            configurationPanel.SetActive(true);
        }

        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }
    }

    public void ShowDeckPanel()
    {
        if (configurationPanel != null)
        {
            configurationPanel.SetActive(false);
        }

        if (deckPanel != null)
        {
            deckPanel.SetActive(true);
        }

        if (multiplayerDeckPanel != null)
        {
            multiplayerDeckPanel.OpenForCharacter(GetSelectedCharacter());
        }
    }

    public void OpenDeckPanel()
    {
        ShowDeckPanel();
    }

    public void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
        }

        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }
    }

    public void OnCharacterChanged(int dropdownIndex)
    {
        if (suppressDropdownCallback)
        {
            return;
        }

        if (dropdownIndex < 0 || dropdownIndex >= availableCharacters.Count)
        {
            return;
        }

        _ = SaveProfileAsync();
    }

    private void SelectCharacter(SelectableCharacter character)
    {
        int index = availableCharacters.IndexOf(character);
        if (index < 0 || characterDropdown == null)
        {
            return;
        }

        suppressDropdownCallback = true;
        characterDropdown.value = index;
        suppressDropdownCallback = false;
    }

    private async Task SaveProfileAsync()
    {
        if (characterDropdown == null || characterDropdown.value < 0 || characterDropdown.value >= availableCharacters.Count)
        {
            ShowNotification("Aucun personnage PVP valide n'est sélectionné.");
            return;
        }

        SelectableCharacter selectedCharacter = availableCharacters[characterDropdown.value];
        try
        {
            JToken response = await STSApiClient.UpdatePvpProfileAsync(new JObject
            {
                ["selectedCharacter"] = selectedCharacter.ToString(),
                ["friendMatch"] = friendlyMatchToggle != null && friendlyMatchToggle.isOn
            });

            if (response == null)
            {
                ShowNotification("La configuration PVP n'a pas pu être sauvegardée.");
                return;
            }

            string playerId = response.Value<string>("playerId")
                ?? response.Value<string>("userId")
                ?? response.Value<string>("id");
            if (playerIdText != null && !string.IsNullOrWhiteSpace(playerId))
            {
                playerIdText.text = $"ID: {playerId}";
            }

            if (RunManager.Instance != null && !string.IsNullOrWhiteSpace(playerId))
            {
                RunManager.Instance.pvpLocalUserId = playerId;
            }

            ShowNotification("Configuration PVP sauvegardée.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to save PVP profile: {ex.Message}");
            ShowNotification("Erreur lors de la sauvegarde PVP.");
        }
    }

    private async Task QuickMatchAsync()
    {
        if (isQuickMatchQueued)
        {
            await CancelQuickMatchAsync();
            return;
        }

        isQuickMatchQueued = true;
        if (quickMatchButton != null)
        {
            quickMatchButton.interactable = false;
        }

        STSSceneLoader.Instance?.BeginLoading("Recherche rapide PVP...", true, () => _ = CancelQuickMatchAsync());
        ShowNotification("Recherche rapide PVP en cours...");

        try
        {
            JToken response = await STSApiClient.QuickMatchPvpAsync(new JObject
            {
                ["friendly"] = friendlyMatchToggle != null && friendlyMatchToggle.isOn,
                ["skipMatchmaking"] = false
            });

            if (response == null)
            {
                ShowNotification("La recherche rapide PVP n'a pas répondu.");
                await CancelQuickMatchAsync(false);
                return;
            }

            string battleId = response.Value<string>("battleId");
            if (!string.IsNullOrWhiteSpace(battleId))
            {
                await EnterPvpBattleAsync(battleId);
                return;
            }

            bool queued = response.Value<bool?>("queued") ?? response.Value<bool?>("isQueued") ?? false;
            if (!queued)
            {
                ShowNotification("Recherche rapide PVP lancée.");
            }
            else
            {
                ShowNotification("Recherche rapide PVP en cours...");
            }

            // Le joueur qui s'inscrit le premier ne reçoit pas de battleId : c'est le
            // second qui en obtient un. Sans cette veille, seul le second entre jamais
            // dans le combat, et le premier attend indéfiniment devant un menu.
            StartCoroutine(WatchForMatchedBattleRoutine());
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to start PVP matchmaking: {ex.Message}");
            ShowNotification("Erreur lors du matchmaking PVP.");
            await CancelQuickMatchAsync(false);
        }
    }

    /// <summary>
    /// L'unique porte d'entrée d'un duel : les participants en cache, la file d'attente
    /// refermée, la session ouverte, puis la scène.
    ///
    /// <para>Pas de BeginLoading ici : GameManager.Start en ouvre un et le referme dans
    /// son <c>finally</c>. En ajouter un second ferait rester le compteur à un, et
    /// l'écran de chargement ne se lèverait jamais.</para>
    /// </summary>
    private async Task EnterPvpBattleAsync(string battleId)
    {
        await CacheBattleParticipantsAsync(battleId);
        await CancelQuickMatchAsync(false);

        if (RunManager.Instance == null)
        {
            ShowNotification("Impossible de rejoindre le combat : gestionnaire de partie absent.");
            return;
        }

        RunManager.Instance.BeginPvpBattle(battleId);
        Debug.Log($"[STS-PVP] Entering battle {battleId}");
        STSSceneLoader.Instance?.LoadScene("STS_Combat");
    }

    /// <summary>
    /// Interroge les notifications PVP jusqu'à ce qu'une bataille apparaisse.
    ///
    /// <para><b>La forme exacte d'une notification n'est pas vérifiable depuis ce
    /// dépôt</b> : le nom de la requête est traduit en URL dans `insastral`, et le DTO
    /// vit dans `webAPI`. Plutôt que d'inventer un nom de champ, on cherche le premier
    /// `battleId` présent n'importe où dans la réponse, et la trame brute est
    /// journalisée pour que la passe suivante puisse le nommer. C'est un provisoire
    /// assumé, pas une tolérance de protocole.</para>
    /// </summary>
    private IEnumerator WatchForMatchedBattleRoutine()
    {
        while (isQuickMatchQueued)
        {
            yield return new WaitForSeconds(2f);
            if (!isQuickMatchQueued)
                yield break;

            Task<JToken> notificationsTask = STSApiClient.ListPvpNotificationsAsync();
            while (!notificationsTask.IsCompleted)
                yield return null;

            if (notificationsTask.Status != TaskStatus.RanToCompletion || notificationsTask.Result == null)
                continue;

            JToken notifications = notificationsTask.Result;
            Debug.Log($"[STS-PVP] notifications payload: {notifications.ToString(Newtonsoft.Json.Formatting.None)}");

            string battleId = FindFirstBattleId(notifications);
            if (string.IsNullOrWhiteSpace(battleId))
                continue;

            _ = EnterPvpBattleAsync(battleId);
            yield break;
        }
    }

    private static string FindFirstBattleId(JToken token)
    {
        if (!(token is JContainer container))
            return null;

        foreach (JToken descendant in container.DescendantsAndSelf())
        {
            if (descendant is JProperty property
                && string.Equals(property.Name, "battleId", StringComparison.Ordinal))
            {
                string value = property.Value?.Value<string>();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private async Task CancelQuickMatchAsync(bool showNotification = true)
    {
        isQuickMatchQueued = false;
        if (quickMatchButton != null)
        {
            quickMatchButton.interactable = true;
        }

        STSSceneLoader.Instance?.EndLoading();
        STSSceneLoader.Instance?.SceneReady();

        if (showNotification)
        {
            ShowNotification("Recherche rapide PVP annulée.");
        }
    }

    private async Task SendChallengeAsync()
    {
        string targetId = challengeTargetInput != null ? challengeTargetInput.text?.Trim() : null;
        if (string.IsNullOrWhiteSpace(targetId))
        {
            ShowNotification("Entrez un ID joueur avant d'envoyer un défi.");
            return;
        }

        try
        {
            JToken response = await STSApiClient.SendPvpChallengeAsync(new JObject
            {
                ["targetUserId"] = targetId,
                ["friendly"] = friendlyMatchToggle != null && friendlyMatchToggle.isOn
            });

            if (response == null)
            {
                ShowNotification("Le défi PVP n'a pas pu être envoyé.");
                return;
            }

            ShowNotification("Défi PVP envoyé.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to send PVP challenge: {ex.Message}");
            ShowNotification("Erreur lors de l'envoi du défi PVP.");
        }
    }

    public SelectableCharacter GetSelectedCharacter()
    {
        if (characterDropdown == null || characterDropdown.value < 0 || characterDropdown.value >= availableCharacters.Count)
        {
            return SelectableCharacter.EP;
        }

        return availableCharacters[characterDropdown.value];
    }

    private async Task CacheBattleParticipantsAsync(string battleId)
    {
        if (string.IsNullOrWhiteSpace(battleId) || RunManager.Instance == null)
        {
            return;
        }

        try
        {
            JToken battleState = await STSApiClient.GetPvpBattleStateAsync(battleId);
            if (battleState == null)
            {
                return;
            }

            List<STSApiClient.StsPvpParticipantSnapshot> participants = STSApiClient.ExtractPvpParticipants(battleState);
            RunManager.Instance.CachePvpBattleParticipants(battleId, participants);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to cache PVP battle participants: {ex.Message}");
        }
    }
    public void ReturnToMainMenu()
    {
        STSSceneLoader.Instance?.LoadScene("STS_Boot");
    }
}