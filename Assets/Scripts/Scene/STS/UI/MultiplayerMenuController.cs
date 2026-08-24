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
    private bool isEnteringPvpBattle;
    private Coroutine matchWatchRoutine;

    /// La cadence de la veille sur les notifications d'appariement.
    ///
    /// Trois secondes : c'est le retard maximum qu'elle ajoute entre l'arrivée de
    /// l'adversaire et l'ouverture du combat, et il reste sous le seuil où une attente
    /// commence à passer pour une panne. En face, ça fait vingt requêtes par minute et
    /// par joueur en file — un ordre de grandeur sous les trente secondes d'un tour, et
    /// une dépense négligeable sur un téléphone, d'autant qu'on n'interroge que pendant
    /// la recherche.
    private const float MatchPollIntervalSeconds = 3f;

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
            StartWatchingForMatchedBattle();
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
        // Les deux joueurs passent par ici : celui dont la demande a refermé
        // l'appariement et qui a reçu son battleId directement, et celui qui l'a appris
        // par une notification. Une seule fois, quoi qu'il arrive : la réponse du
        // matchmaking et la notification peuvent nommer la même bataille à quelques
        // millisecondes d'écart.
        if (isEnteringPvpBattle || string.IsNullOrWhiteSpace(battleId))
        {
            return;
        }

        isEnteringPvpBattle = true;

        try
        {
            await CacheBattleParticipantsAsync(battleId);
            await AcknowledgeMatchNotificationsAsync(battleId);
            await CancelQuickMatchAsync(false);

            if (RunManager.Instance == null)
            {
                isEnteringPvpBattle = false;
                ShowNotification("Impossible de rejoindre le combat : gestionnaire de partie absent.");
                return;
            }

            RunManager.Instance.BeginPvpBattle(battleId);
            Debug.Log($"[STS-PVP] Entering battle {battleId}");
            STSSceneLoader.Instance?.LoadScene("STS_Combat");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-PVP] Failed to enter battle {battleId}: {ex.Message}");
            ShowNotification("Erreur lors de l'ouverture du duel PVP.");
            isEnteringPvpBattle = false;
        }

        if (RunManager.Instance == null)
        {
            isEnteringPvpBattle = false;
            ShowNotification("Impossible de rejoindre le combat : gestionnaire de partie absent.");
            return;
        }

        RunManager.Instance.BeginPvpBattle(battleId);
        Debug.Log($"[STS-PVP] Entering battle {battleId}");
        STSSceneLoader.Instance?.LoadScene("STS_Combat");
    }

    /// <summary>
    /// Acquitte l'annonce d'appariement de cette bataille, avant d'ouvrir la scène.
    ///
    /// <para>Une notification non lue est relue à chaque interrogation : sans cet
    /// acquittement, la prochaine recherche d'adversaire ramènerait le joueur dans ce
    /// combat-là, terminé depuis longtemps. Le joueur dont la demande a refermé
    /// l'appariement reçoit son battleId directement et ne regarde jamais la liste — il a
    /// pourtant une notification à acquitter comme l'autre, puisque le serveur en crée une
    /// pour chacun des deux. C'est pour lui qu'on cherche par bataille plutôt que de se
    /// contenter de l'identifiant qu'on vient de lire.</para>
    ///
    /// <para>Un échec n'empêche pas d'entrer : mieux vaut un duel joué avec une
    /// notification de trop qu'un duel manqué.</para>
    /// </summary>
    private async Task AcknowledgeMatchNotificationsAsync(string battleId)
    {
        try
        {
            JToken notifications = await STSApiClient.ListPvpNotificationsAsync();
            foreach (string notificationId in
                PvpMatchNotifications.QuickMatchIdsForBattle(notifications, battleId))
            {
                await STSApiClient.AcknowledgePvpNotificationAsync(notificationId);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-PVP] Failed to acknowledge the match notification: {ex.Message}");
        }
    }

    private void StartWatchingForMatchedBattle()
    {
        StopWatchingForMatchedBattle();
        matchWatchRoutine = StartCoroutine(WatchForMatchedBattleRoutine());
    }

    /// La veille ne survit pas à la file d'attente : annuler la recherche l'arrête tout
    /// de suite, sans attendre la fin de l'intervalle en cours.
    private void StopWatchingForMatchedBattle()
    {
        if (matchWatchRoutine == null)
        {
            return;
        }

        Coroutine routine = matchWatchRoutine;
        matchWatchRoutine = null;
        StopCoroutine(routine);
    }

    /// <summary>
    /// Interroge les notifications PVP tant qu'on est en file, jusqu'à ce qu'un
    /// appariement soit annoncé.
    ///
    /// <para>Le joueur qui s'inscrit le premier reçoit <c>queued</c> sans battleId : c'est
    /// le second, celui dont la demande referme l'appariement, qui en obtient un. Le
    /// serveur crée alors une notification <c>QUICK_MATCH_FOUND</c> pour les deux, et
    /// c'est le seul moyen qu'a le premier d'apprendre que quelqu'un est arrivé. Sans
    /// cette veille, un seul des deux joueurs entre dans le combat.</para>
    ///
    /// <para>Le choix de la notification et la lecture du battleId sont dans
    /// <see cref="PvpMatchNotifications"/>, testés séparément : ce qui reste ici est de la
    /// glue Unity.</para>
    /// </summary>
    private IEnumerator WatchForMatchedBattleRoutine()
    {
        while (isQuickMatchQueued)
        {
            yield return new WaitForSeconds(MatchPollIntervalSeconds);
            if (!isQuickMatchQueued)
            {
                yield break;
            }

            Task<JToken> notificationsTask = STSApiClient.ListPvpNotificationsAsync();
            while (!notificationsTask.IsCompleted)
            {
                yield return null;
            }

            if (!isQuickMatchQueued)
            {
                yield break;
            }

            // Une interrogation ratée n'annule pas la recherche : l'adversaire est
            // peut-être déjà là, et la suivante le verra.
            if (notificationsTask.Status != TaskStatus.RanToCompletion || notificationsTask.Result == null)
            {
                Debug.LogWarning("[STS-PVP] Notification poll failed, still queued: "
                    + (notificationsTask.Exception?.GetBaseException().Message ?? "empty response"));
                continue;
            }

            PvpMatchNotification match = PvpMatchNotifications.FindQuickMatch(notificationsTask.Result);
            if (!match.Found)
            {
                continue;
            }

            Debug.Log($"[STS-PVP] Quick match notification {match.NotificationId} names battle {match.BattleId}");

            // Se retirer du champ avant d'entrer : l'entrée annule la recherche, et
            // l'annulation arrête la veille — c'est-à-dire cette coroutine-ci.
            matchWatchRoutine = null;
            _ = EnterPvpBattleAsync(match.BattleId);
            yield break;
        }
    }

    private async Task CancelQuickMatchAsync(bool showNotification = true)
    {
        isQuickMatchQueued = false;
        StopWatchingForMatchedBattle();
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