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
    [SerializeField] private TMP_Dropdown modeDropdown;
    [SerializeField] private Toggle fillWithAiToggle;
    [SerializeField] private TextMeshProUGUI modeHintText;

    /// <summary>
    /// Combien de joueurs cherchent en ce moment dans le mode choisi.
    ///
    /// <para>Facultatif : sans lui, le menu se comporte comme avant et n'interroge rien.</para>
    /// </summary>
    [SerializeField] private TextMeshProUGUI queueCountText;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [Tooltip("Classement du joueur. Facultatif : sans lui, le menu s'affiche comme avant.")]
    [SerializeField] private TextMeshProUGUI eloText;
    [Tooltip("Bilan classé du joueur, « V / D ». Facultatif.")]
    [SerializeField] private TextMeshProUGUI rankedRecordText;

    [Header("Notification")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Button notificationOkButton;

    /// <summary>
    /// Les formats proposes, dans l'ordre du menu deroulant.
    ///
    /// <para>Le nom qui part au serveur est celui de l'enum <c>StsPvpMode</c> : c'est un contrat,
    /// pas un libelle, et un mode que le serveur ne reconnait pas est refuse plutot que ramene
    /// au duel. Le libelle, lui, ne sert qu'a l'affichage.</para>
    /// </summary>
    /// Le classement d'un joueur qui n'en a pas encore, tel que le serveur le pose.
    private const int DefaultElo = 1000;

    private static readonly (string WireName, string Label, int Players)[] PvpModes =
    {
        ("ONE_V_ONE", "1v1", 2),
        ("TWO_V_TWO", "2v2", 4),
        ("RAID", "Raid (2 vs boss)", 2)
    };

    private readonly List<SelectableCharacter> availableCharacters = new();
    private readonly List<PvpFriend> acceptedFriends = new();
    private readonly List<GameObject> friendResultRows = new();
    private bool suppressDropdownCallback;
    private bool isQuickMatchQueued;
    private bool quickMatchFriendly;
    private string quickMatchMode = "ONE_V_ONE";
    private bool isEnteringPvpBattle;
    private int quickMatchGeneration;
    private Coroutine matchWatchRoutine;
    private PvpFriend? selectedChallengeFriend;
    private RectTransform friendResultsRoot;
    private TextMeshProUGUI quickMatchButtonText;

    /// La cadence de la veille sur les notifications d'appariement.
    ///
    /// Trois secondes : c'est le retard maximum qu'elle ajoute entre l'arrivée de
    /// l'adversaire et l'ouverture du combat, et il reste sous le seuil où une attente
    /// commence à passer pour une panne. En face, ça fait vingt requêtes par minute et
    /// par joueur en file — un ordre de grandeur sous les trente secondes d'un tour, et
    /// une dépense négligeable sur un téléphone, d'autant qu'on n'interroge que pendant
    /// la recherche.
    private const float MatchPollIntervalSeconds = 3f;

    /// <summary>
    /// À quelle cadence on recompte les joueurs en file.
    ///
    /// <para>Plus lâche que la veille d'appariement : ce n'est qu'un affichage, et une entrée
    /// de file vit trente secondes sans battement de toute façon. Cinq secondes suffisent à ce
    /// que le nombre bouge sous les yeux du joueur sans qu'un menu resté ouvert coûte quoi que
    /// ce soit.</para>
    /// </summary>
    private const float QueueCountPollIntervalSeconds = 5f;

    private int queueCountGeneration;
    private bool warnedAboutUnreadableQueue;

    private void OnDestroy()
    {
        if (isQuickMatchQueued && !isEnteringPvpBattle)
        {
            isQuickMatchQueued = false;
            _ = STSApiClient.CancelQuickMatchPvpAsync();
        }
    }

    private void Start()
    {
        STSSceneLoader.Instance?.SceneReady();

        if (queueCountText != null)
        {
            queueCountText.text = "";
            StartCoroutine(WatchQueueCountRoutine());
        }
    }

    /// <summary>
    /// Recompte la file tant que le menu est ouvert. Unity arrête la boucle en détruisant
    /// l'objet, donc rien à défaire à la main.
    /// </summary>
    private IEnumerator WatchQueueCountRoutine()
    {
        while (true)
        {
            RefreshQueueCount();
            yield return new WaitForSeconds(QueueCountPollIntervalSeconds);
        }
    }

    /// <summary>
    /// Demande combien de joueurs cherchent dans le mode et le type de file affichés.
    ///
    /// <para>Numéroté comme les autres rafraîchissements du menu : changer de mode relance la
    /// question, et la réponse de la précédente ne doit pas venir écraser celle de la nouvelle
    /// avec le compte d'un mode que le joueur ne regarde plus.</para>
    ///
    /// <para>Une question sans réponse laisse la ligne vide plutôt que d'annoncer zéro : dire
    /// « personne » quand on ne sait pas découragerait une recherche qui aurait abouti.</para>
    /// </summary>
    private async void RefreshQueueCount()
    {
        if (queueCountText == null)
        {
            return;
        }

        int generation = ++queueCountGeneration;
        string mode = SelectedPvpMode().WireName;
        bool friendly = friendlyMatchToggle != null && friendlyMatchToggle.isOn;

        STSApiPvpQueueStatusResponse status = await STSApiClient.GetPvpQueueStatusAsync(mode, friendly);

        if (generation != queueCountGeneration || queueCountText == null)
        {
            return;
        }

        if (status == null)
        {
            queueCountText.text = "";
            // La ligne reste vide pour le joueur, volontairement (voir plus haut). Mais une ligne
            // vide ne dit pas pourquoi, et « la file reste vide » cachait trois pannes qui se
            // ressemblent toutes : l'éditeur, qui n'envoie aucune requête au pont ; un pont
            // déployé qui ne connaît pas encore sts.pvp.matchmaking.queue ; un serveur qui n'a
            // pas encore la route. On le dit une fois dans la console, pour le développeur.
            if (!warnedAboutUnreadableQueue)
            {
                warnedAboutUnreadableQueue = true;
                Debug.LogWarning("[STS-PVP] Le compte de la file est illisible (réponse nulle). "
#if UNITY_EDITOR
                    + "Dans l'éditeur, le pont React n'est jamais appelé : testez en WebGL. "
#endif
                    + "Vérifiez que le pont déployé route sts.pvp.matchmaking.queue et que "
                    + "l'API expose GET /api/sts/pvp/matchmaking/queue.");
            }
            return;
        }

        if (status.waiting <= 0)
        {
            queueCountText.text = "Personne ne cherche ce mode en ce moment.";
            return;
        }

        int required = Mathf.Max(1, status.required);
        queueCountText.text = status.waiting > 1
            ? $"{status.waiting} joueurs en recherche ({status.waiting}/{required} pour lancer)"
            : $"1 joueur en recherche (1/{required} pour lancer)";
    }

    private async void Awake()
    {
        BuildCharacterDropdown();
        BuildModeDropdown();
        WireButtons();

        if (multiplayerDeckPanel != null)
        {
            multiplayerDeckPanel.SetHost(this);
        }

        await LoadRemotePvpProfileAsync();
        await LoadFriendsAsync();
        ShowConfigurationPanel();

        // « Revanche » : l'écran de fin de duel nous a renvoyés ici en demandant une
        // nouvelle recherche. Il n'existe pas d'endpoint de revanche côté serveur, donc
        // c'est un matchmaking ordinaire, pas un rematch contre le même joueur.
        if (RunManager.Instance != null && RunManager.Instance.ConsumePvpQuickMatchRequest())
        {
            await QuickMatchAsync();
            return;
        }

        // Après le chargement, pas avant : le classement et le personnage sont alors affichés,
        // et le tutoriel parle de ce que le joueur a vraiment sous les yeux.
        PlayTutorialOnce(MenuTutorialSeenKey, BuildMenuTutorialSteps());
    }

    private const string MenuTutorialSeenKey = "STS_MultiplayerTutorialSeen";
    private const string DeckTutorialSeenKey = "STS_MultiplayerDeckTutorialSeen";

    private MultiplayerTutorial tutorial;

    /// <summary>
    /// Rejoue le tutoriel de l'écran affiché : celui de l'éditeur si le deck est ouvert, celui
    /// du menu sinon. À brancher sur un bouton d'aide.
    /// </summary>
    public void ReplayTutorial()
    {
        if (deckPanel != null && deckPanel.activeInHierarchy && multiplayerDeckPanel != null)
            PlayTutorial(multiplayerDeckPanel.BuildTutorialSteps());
        else
            PlayTutorial(BuildMenuTutorialSteps());
    }

    private void PlayTutorialOnce(string seenKey, IReadOnlyList<MultiplayerTutorial.Step> steps)
    {
        if (PlayerPrefs.GetInt(seenKey, 0) == 1)
            return;

        PlayTutorial(steps, () =>
        {
            PlayerPrefs.SetInt(seenKey, 1);
            PlayerPrefs.Save();
        });
    }

    private void PlayTutorial(IReadOnlyList<MultiplayerTutorial.Step> steps, Action onFinished = null)
    {
        // Appelé au bout d'un chargement asynchrone : la scène a pu être quittée entre-temps.
        if (this == null || isQuickMatchQueued || isEnteringPvpBattle)
            return;

        if (tutorial == null)
        {
            Transform anchor = deckPanel != null && deckPanel.activeInHierarchy
                ? deckPanel.transform
                : configurationPanel != null ? configurationPanel.transform : transform;
            Canvas canvas = anchor.GetComponentInParent<Canvas>();
            TMP_FontAsset font = notificationText != null ? notificationText.font
                : challengeTargetInput != null && challengeTargetInput.textComponent != null
                    ? challengeTargetInput.textComponent.font
                    : null;
            tutorial = MultiplayerTutorial.Create(canvas != null ? canvas.rootCanvas : null, font);
        }

        if (tutorial != null)
            tutorial.Play(steps, onFinished);
    }

    private static RectTransform AsRect(Component component)
    {
        return component != null ? component.transform as RectTransform : null;
    }

    private IReadOnlyList<MultiplayerTutorial.Step> BuildMenuTutorialSteps()
    {
        return new List<MultiplayerTutorial.Step>
        {
            new("Bienvenue dans le mode multijoueur ! Voici un rapide tour du menu avant votre premier affrontement."),
            new("Choisissez ici votre personnage. Il décide des cartes que vous pourrez mettre dans votre deck, "
                + "et il est enregistré dès que vous le changez.", AsRect(characterDropdown)),
            new("Ce bouton ouvre l'éditeur de deck de votre personnage : c'est là que vous choisissez les cartes "
                + "que vous emmènerez au combat.", AsRect(openDeckButton)),
            new("Choisissez le format de la partie : 1v1, 2v2, ou Raid, où deux joueurs affrontent ensemble un boss.",
                AsRect(modeDropdown)),
            new("Avec cette option, l'IA occupe les places libres : la partie se lance sans attendre que tous les joueurs soient là.",
                AsRect(fillWithAiToggle)),
            new("En match amical, votre classement n'est pas en jeu. Amicales et classées ont chacune leur file : "
                + "vous ne croiserez que des joueurs qui ont fait le même choix.", AsRect(friendlyMatchToggle)),
            new("Ici s'affiche le nombre de joueurs qui cherchent le même format que vous en ce moment.",
                AsRect(queueCountText)),
            new("« Partie rapide » lance la recherche d'adversaires. Vous pouvez l'annuler à tout moment depuis l'écran d'attente.",
                AsRect(quickMatchButton)),
            new("Pour affronter un ami, tapez son nom ici et choisissez-le dans la liste, puis appuyez sur « Défier ».",
                AsRect(challengeTargetInput), AsRect(challengeButton)),
            new("Votre classement et votre bilan de victoires et défaites en parties classées. Ils évoluent à chaque partie classée.",
                AsRect(eloText), AsRect(rankedRecordText)),
            new("Ce bouton enregistre votre personnage et vos préférences de partie.", AsRect(saveProfileButton)),
            new("C'est tout ! Bonne chance dans l'arène.")
        };
    }

    /// <summary>
    /// Remplit le menu des formats et le laisse sur le duel.
    ///
    /// <para>Rien n'est deduit d'un champ de scene : les options sont reconstruites depuis
    /// <see cref="PvpModes"/> a chaque ouverture, pour qu'un libelle laisse a la main dans
    /// l'editeur ne puisse pas envoyer un mode que le serveur refusera.</para>
    /// </summary>
    /// <summary>
    /// Affiche le classement du joueur, et son bilan classé.
    ///
    /// <para>C'est le classement qui décide des appariements et de ce qu'une victoire rapporte,
    /// donc c'est la seule mesure que le joueur a de sa progression en multijoueur. Il n'était
    /// visible nulle part : le profil le servait déjà, personne ne le lisait.</para>
    ///
    /// <para>Les deux champs sont facultatifs, comme le reste du menu : une scène qui ne les
    /// branche pas s'affiche exactement comme avant.</para>
    /// </summary>
    private void DisplayRanking(JToken profile)
    {
        if (profile == null)
            return;

        if (eloText != null)
        {
            // Le classement de départ vaut 1000 côté serveur ; un profil qui n'en porte pas est
            // un profil que le serveur n'a pas encore écrit, pas un joueur à zéro.
            int elo = profile.Value<int?>("elo") ?? DefaultElo;
            eloText.text = $"Classement : {elo}";
        }

        if (rankedRecordText != null)
        {
            int wins = profile.Value<int?>("winsRanked") ?? 0;
            int losses = profile.Value<int?>("lossesRanked") ?? 0;
            rankedRecordText.text = $"{wins} V / {losses} D";
        }
    }

    private void BuildModeDropdown()
    {
        if (modeDropdown == null)
            return;

        modeDropdown.onValueChanged.RemoveAllListeners();
        modeDropdown.ClearOptions();

        var labels = new List<string>();
        foreach (var mode in PvpModes)
            labels.Add(mode.Label);
        modeDropdown.AddOptions(labels);

        modeDropdown.value = 0;
        modeDropdown.RefreshShownValue();
        modeDropdown.onValueChanged.AddListener(_ =>
        {
            RefreshModeHint();
            // Le compte affiché est celui d'une file précise : changer de mode le périme.
            RefreshQueueCount();
        });
        RefreshModeHint();
    }

    private (string WireName, string Label, int Players) SelectedPvpMode()
    {
        int index = modeDropdown != null ? modeDropdown.value : 0;
        return index >= 0 && index < PvpModes.Length ? PvpModes[index] : PvpModes[0];
    }

    /// Dit combien de joueurs le format demande, et ce que le remplissage par l'IA change.
    private void RefreshModeHint()
    {
        if (modeHintText == null)
            return;

        var mode = SelectedPvpMode();
        bool fill = fillWithAiToggle != null && fillWithAiToggle.isOn;
        modeHintText.text = fill
            ? $"{mode.Label} — {mode.Players} joueur(s) ; les places libres seront tenues par l'IA."
            : $"{mode.Label} — en attente de {mode.Players} joueur(s).";
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
            quickMatchButtonText = quickMatchButton.GetComponentInChildren<TextMeshProUGUI>();
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

        if (fillWithAiToggle != null)
        {
            fillWithAiToggle.onValueChanged.RemoveAllListeners();
            fillWithAiToggle.onValueChanged.AddListener(_ => RefreshModeHint());
        }

        if (friendlyMatchToggle != null)
        {
            // Les deux files ne se croisent pas : on ne s'apparie qu'entre amicales ou
            // qu'entre classées, donc basculer change le nombre qu'il faut annoncer.
            friendlyMatchToggle.onValueChanged.RemoveAllListeners();
            friendlyMatchToggle.onValueChanged.AddListener(_ => RefreshQueueCount());
        }

        if (challengeTargetInput != null)
        {
            challengeTargetInput.onValueChanged.RemoveAllListeners();
            challengeTargetInput.onValueChanged.AddListener(OnFriendSearchChanged);
            if (challengeTargetInput.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.text = "Rechercher un ami par nom...";
            }
            BuildFriendResultsRoot();
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

            DisplayRanking(profile);
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
            PlayTutorialOnce(DeckTutorialSeenKey, multiplayerDeckPanel.BuildTutorialSteps());
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
        quickMatchFriendly = friendlyMatchToggle != null && friendlyMatchToggle.isOn;
        quickMatchMode = SelectedPvpMode().WireName;
        bool fillWithAi = fillWithAiToggle != null && fillWithAiToggle.isOn;
        int generation = ++quickMatchGeneration;
        UpdateQuickMatchButton();

        ShowQueueLoadingScreen();
        ShowNotification("Recherche rapide PVP en cours...");

        try
        {
            JToken response = await STSApiClient.QuickMatchPvpAsync(new JObject
            {
                ["friendly"] = quickMatchFriendly,
                ["skipMatchmaking"] = false,
                ["mode"] = quickMatchMode,
                // Sans cela, un 2v2 attend quatre joueurs connectes en meme temps. Le
                // demander explicitement evite de se retrouver entoure de robots sans
                // l'avoir voulu.
                ["fillWithAi"] = fillWithAi
            });

            if (!isQuickMatchQueued || generation != quickMatchGeneration)
            {
                await CancelQuickMatchAsync(false, false);
                return;
            }

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
            await CancelQuickMatchAsync(false, false);
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
            await CancelQuickMatchAsync(false, false);

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

            // Réinscrire la recherche sert de heartbeat au serveur et peut également
            // conclure directement le match. Les entrées qui ne battent plus sont
            // expirées côté backend, donc un onglet fermé ne devient pas un adversaire fantôme.
            Task<JToken> heartbeatTask = STSApiClient.HeartbeatQuickMatchPvpAsync();
            while (!heartbeatTask.IsCompleted)
            {
                yield return null;
            }

            if (!isQuickMatchQueued)
            {
                yield break;
            }

            if (heartbeatTask.Status != TaskStatus.RanToCompletion || heartbeatTask.Result == null)
            {
                Debug.LogWarning("[STS-PVP] Matchmaking heartbeat failed; the next poll will retry.");
            }
            else
            {
                string heartbeatBattleId = heartbeatTask.Result.Value<string>("battleId");
                if (!string.IsNullOrWhiteSpace(heartbeatBattleId))
                {
                    matchWatchRoutine = null;
                    _ = EnterPvpBattleAsync(heartbeatBattleId);
                    yield break;
                }

                if (heartbeatTask.Result.Value<bool?>("queued") != true)
                {
                    isQuickMatchQueued = false;
                    quickMatchGeneration++;
                    matchWatchRoutine = null;
                    UpdateQuickMatchButton();
                    STSSceneLoader.Instance?.EndLoading();
                    STSSceneLoader.Instance?.SceneReady();
                    ShowNotification("La recherche a expiré. Relancez-la pour chercher un joueur.");
                    yield break;
                }
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

    /// <summary>
    /// Ouvre l'écran d'attente de la recherche, avec de quoi en sortir.
    ///
    /// <para>Deux endroits en ont besoin : le lancement de la recherche, et la reprise quand
    /// une annulation a échoué. Le second l'oubliait, et laissait le joueur toujours en file
    /// devant un menu qui ne le disait plus et ne proposait plus d'en sortir.</para>
    ///
    /// <para>Le rappel ne fait rien si la file est déjà quittée : le bouton reste cliquable
    /// pendant l'aller-retour avec le serveur.</para>
    /// </summary>
    private void ShowQueueLoadingScreen()
    {
        STSSceneLoader.Instance?.BeginLoading(
            "Recherche rapide PVP...",
            true,
            () =>
            {
                if (isQuickMatchQueued)
                    _ = CancelQuickMatchAsync();
            });
    }

    private async Task<bool> CancelQuickMatchAsync(bool showNotification = true, bool resumeOnFailure = true)
    {
        isQuickMatchQueued = false;
        quickMatchGeneration++;
        StopWatchingForMatchedBattle();
        UpdateQuickMatchButton();

        bool cancelled = false;
        string matchedBattleId = null;
        try
        {
            JToken state = await STSApiClient.CancelQuickMatchPvpAsync();
            cancelled = state?.Value<bool?>("cancelled") == true;
            matchedBattleId = state?.Value<string>("battleId");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-PVP] Failed to leave matchmaking queue: {ex.Message}");
        }

        STSSceneLoader.Instance?.EndLoading();
        STSSceneLoader.Instance?.SceneReady();

        if (!string.IsNullOrWhiteSpace(matchedBattleId))
        {
            await EnterPvpBattleAsync(matchedBattleId);
            return false;
        }

        if (!cancelled && resumeOnFailure && !isEnteringPvpBattle)
        {
            isQuickMatchQueued = true;
            UpdateQuickMatchButton();
            StartWatchingForMatchedBattle();
            // On vient de refermer l'écran d'attente au-dessus, et la recherche continue :
            // sans ça, le joueur reste en file sans plus rien pour en sortir.
            ShowQueueLoadingScreen();
            ShowNotification("Impossible d'annuler la recherche. Nouvel essai en cours...");
            return false;
        }

        if (showNotification && cancelled)
        {
            ShowNotification("Recherche rapide PVP annulée.");
        }

        return cancelled;
    }

    private async Task SendChallengeAsync()
    {
        if (!selectedChallengeFriend.HasValue)
        {
            ShowNotification("Sélectionnez un ami dans les résultats avant d'envoyer un défi.");
            return;
        }

        PvpFriend target = selectedChallengeFriend.Value;

        try
        {
            JToken response = await STSApiClient.SendPvpChallengeAsync(new JObject
            {
                ["targetUserId"] = target.UserId,
                ["friendly"] = friendlyMatchToggle != null && friendlyMatchToggle.isOn
            });

            if (response == null)
            {
                ShowNotification("Le défi PVP n'a pas pu être envoyé.");
                return;
            }

            ShowNotification($"Défi PVP envoyé à {target.DisplayName}.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to send PVP challenge: {ex.Message}");
            ShowNotification("Erreur lors de l'envoi du défi PVP.");
        }
    }

    private async Task LoadFriendsAsync()
    {
        acceptedFriends.Clear();
        try
        {
            JToken response = await STSApiClient.ListFriendsAsync();
            if (response is not JArray friends)
                return;

            foreach (JToken entry in friends)
            {
                JToken user = entry?["user"];
                PvpFriend friend = new(
                    user?.Value<string>("id"),
                    user?.Value<string>("displayName"));
                if (friend.IsValid)
                    acceptedFriends.Add(friend);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-PVP] Failed to load accepted friends: {ex.Message}");
        }
    }

    private void OnFriendSearchChanged(string query)
    {
        selectedChallengeFriend = null;
        RenderFriendResults(PvpFriendSearch.Filter(acceptedFriends, query, 5));
    }

    private void SelectChallengeFriend(PvpFriend friend)
    {
        selectedChallengeFriend = friend;
        challengeTargetInput?.SetTextWithoutNotify(friend.DisplayName);
        if (friendResultsRoot != null)
            friendResultsRoot.gameObject.SetActive(false);
    }

    private void BuildFriendResultsRoot()
    {
        if (challengeTargetInput == null || friendResultsRoot != null)
            return;

        GameObject root = new("FriendSearchResults", typeof(RectTransform), typeof(VerticalLayoutGroup));
        friendResultsRoot = root.GetComponent<RectTransform>();
        friendResultsRoot.SetParent(challengeTargetInput.transform.parent, false);
        friendResultsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        friendResultsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        friendResultsRoot.anchoredPosition = new Vector2(0f, -115f);
        friendResultsRoot.sizeDelta = new Vector2(900f, 280f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        root.SetActive(false);
    }

    private void RenderFriendResults(IReadOnlyList<PvpFriend> matches)
    {
        if (friendResultsRoot == null)
            return;

        foreach (GameObject row in friendResultRows)
            Destroy(row);
        friendResultRows.Clear();

        foreach (PvpFriend friend in matches)
        {
            GameObject row = new($"Friend_{friend.UserId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(friendResultsRoot, false);
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);
            row.GetComponent<LayoutElement>().preferredHeight = 48f;

            GameObject labelObject = new("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(row.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 0f);
            labelRect.offsetMax = new Vector2(-18f, 0f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = friend.DisplayName;
            label.font = challengeTargetInput.textComponent.font;
            label.fontSize = 28f;
            label.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            PvpFriend captured = friend;
            row.GetComponent<Button>().onClick.AddListener(() => SelectChallengeFriend(captured));
            friendResultRows.Add(row);
        }

        friendResultsRoot.gameObject.SetActive(matches.Count > 0);
    }

    private void UpdateQuickMatchButton()
    {
        if (quickMatchButton != null)
            quickMatchButton.interactable = true;
        if (quickMatchButtonText != null)
            quickMatchButtonText.text = isQuickMatchQueued ? "Annuler la recherche" : "Partie rapide";
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
