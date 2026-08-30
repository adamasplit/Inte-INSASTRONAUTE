using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
public enum TeamOutcome
{
    None,
    Victory,
    Defeat,
    Draw
}

public class CombatManager : MonoBehaviour
{
    // Editor-only cheat: Press Space to win battle by setting all enemy HP to zero
#if UNITY_EDITOR
    // Requires Input System package
    void Update()
    {
        #if ENABLE_INPUT_SYSTEM
        if (!combatEnded && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Cheat: Ending combat with victory (Input System).");
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.currentHP = 0;
                }
            }
            TryEndCombatIfNeeded();
        }
        if (!combatEnded && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Debug.Log("Cheat: Adding energy to player (Input System).");
            if (player != null)            
            {
                player.resources.energy += 3;
                ui.RefreshUI();
            }
        }
        if (!combatEnded && Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            Debug.Log("Cheat: Drawing a card for player (Input System).");
            if (player != null)
            {
                deck.Draw();
            }
            TryEndCombatIfNeeded();
        }
        #endif
    }
#endif

    // Tracks running and queued card-play coroutines so turn flow can wait reliably.
    private int activeCardPlays = 0;
    private int queuedCardPlays = 0;
    private int activeEffectResolutions = 0;
    public bool CardPlaysRunning => activeCardPlays > 0 || queuedCardPlays > 0;
    private bool resolvingCombatCleanup = false;

    public Player player => allies.FirstOrDefault();
    public List<Player> allies = new();
    public List<Character> enemies = new();
    public List<Character> characters => GetAllCharacters();

    public DeckManager deck;
    public UIManager ui;
    public TurnSystem turnSystem;

    public CombatState state = new CombatState();
    public bool combatEnded { get; private set; }
    public TeamOutcome outcome { get; private set; } = TeamOutcome.None;

    /// L'issue que le serveur a annoncée dans son CombatEnded, ou None s'il n'a rien dit.
    /// Distincte de `outcome`, qui reste ce que le combat local dérive : le chemin local
    /// (tutoriel) n'a pas de serveur pour trancher et continue de déduire.
    TeamOutcome announcedOutcome = TeamOutcome.None;
    public List<EnemyData> currentEnemiesData = new();
    public CardAnimator animator;
    public CardInstance currentCard; // For animation purposes
    public STSTutorialManager tutorial;
    private bool tutorialMode;
    public bool forceTutorial = false;
    public bool allowTurn = false; 
    private bool turnSystemInitialized;
    private bool authoritativeHandSynced;
    private readonly Dictionary<string, TurnEntry> authoritativeTimelineEntries = new();
    private readonly Dictionary<string, TurnEntry> authoritativeTimelineProjectionEntries = new();
    private readonly Dictionary<string, long> authoritativeTimelinePendingSelfDelays = new();
    private bool authoritativeCommandInFlight;
    private float authoritativeCommandInFlightSince;
    private const float AuthoritativeCommandWatchdogSeconds = 8f;
    private readonly CombatantRegistry<Character> combatantRegistry =
        new CombatantRegistry<Character>();
    // Explicit rather than inferred from the registry being empty: a combat whose local
    // combatant never showed up would leave LocalCombatantId null, and an emptiness test
    // would rebuild on every state — Register would then throw on a known id.
    private bool combatantRegistryBuilt;

    // L'état autoritatif courant. En PvE c'est aussi RunManager.activeCombat, parce que la
    // run le possède ; en PvP la run n'a rien à voir avec ce combat et ne doit surtout pas
    // s'en trouver modifiée — un joueur qui met une run en pause pour jouer un duel doit la
    // retrouver telle quelle.
    private JToken authoritativeCombatState;

    // La deadline du tour, relue à chaque état. None en PvE, où le tour n'expire pas.
    private TurnCountdown turnCountdown = TurnCountdown.None;

    // No "already built" flag here, unlike the identity registry above: piles are
    // replaced by every state that arrives, and reflecting that is the whole point.
    private readonly CombatantPilesRegistry<CardInstance> combatantPiles =
        new CombatantPilesRegistry<CardInstance>();

    // Both known transports (STOMP + REST) time out around 5s; anything still "in flight"
    // past that is a stuck flag, not a real pending request, and must not softlock input forever.
    bool AuthoritativeCommandBusy
    {
        get
        {
            if (!authoritativeCommandInFlight)
                return false;

            if (Time.unscaledTime - authoritativeCommandInFlightSince > AuthoritativeCommandWatchdogSeconds)
            {
                Debug.LogWarning("[STS-COMBAT] Authoritative command watchdog fired: clearing a stuck in-flight flag.");
                authoritativeCommandInFlight = false;
                return false;
            }

            return true;
        }
    }
    private readonly Queue<JObject> authoritativeMessageQueue = new();
    private bool authoritativeMessageQueueRunning;

    /// <summary>
    /// Ce que ce combat est. Un duel se reconnaît à la session ouverte par le menu
    /// multijoueur ; un combat de run, à l'état que la run porte.
    ///
    /// <para>L'ordre compte : le duel se déclare <b>avant</b> d'avoir reçu son premier
    /// état, là où le PvE ne se déclarait qu'après. C'est ce qui empêche la première
    /// carte d'un duel de partir dans le moteur local pendant que la socket s'ouvre.</para>
    /// </summary>
    public CombatMode Mode
    {
        get
        {
            if (RunManager.Instance == null)
                return CombatMode.Local;

            if (!string.IsNullOrWhiteSpace(RunManager.Instance.activePvpBattleId))
                return CombatMode.Pvp;

            return RunManager.Instance.activeCombat != null
                && RunManager.Instance.activeCombat.Type == JTokenType.Object
                    ? CombatMode.Pve
                    : CombatMode.Local;
        }
    }

    public bool UsesAuthoritativeCombat => Mode != CombatMode.Local;

    public void Init()
    {
        EnsureAllies();
        EnsureEncounterEnemies();
        RunManager.Instance?.ApplyPvpParticipantDisplayNames(allies, enemies);
        ResetCombatStatus();
        ui.Init(this);          // inject
        ui.InitCharacters();    // spawn UI
        currentEnemiesData = new();
        deck.combatManager = this; // inject
        foreach (var enemy in enemies)
        {
            Enemy enn = enemy as Enemy;
            if (enn == null)
                continue;

            // Un adversaire humain n'a pas d'EnemyData : rien à empiler pour lui, et
            // currentEnemiesData ne sert de toute façon qu'à composer une récompense PvE.
            if (enn.data != null)
                currentEnemiesData.Add(enn.data);
            enn.combat = this;
        }
        foreach (var ally in allies)
        {
            ally.combat = this;
        }
        if (tutorial != null)
        {
            if (RunManager.Instance==null || forceTutorial||RunManager.Instance.forceTutorial)
            {
                allowTurn=false;
                tutorialMode = true;
            }
            else
            {
                allowTurn = true;
                tutorialMode = false;
            }
            tutorial.Init();
        }
        if (RunManager.Instance != null)
        {
            RunManager.Instance.inCombat = true;
            // Un duel n'est pas un nœud de carte : l'auditer en tant que tel écrirait dans
            // l'historique d'une run l'entrée dans un combat qui ne lui appartient pas.
            if (Mode != CombatMode.Pvp)
            {
                STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "combat_init");
            }
        }

        if (Mode == CombatMode.Pvp)
        {
            allowTurn = true;
            StartCoroutine(BootstrapPvpBattleRoutine());
            return;
        }

        if (UsesAuthoritativeCombat)
        {
            allowTurn = true;
            // Apply the state we already have immediately; the socket only carries future
            // updates and never proactively pushes a snapshot on connect, so waiting for it
            // here left onTurn/endTurnButton/hand/timeline uninitialized forever in WebGL.
            ApplyAuthoritativeCombatState(RunManager.Instance.activeCombat, true);
#if UNITY_WEBGL && !UNITY_EDITOR
            ReactCombatBridge.CombatEventReceived += HandleReactCombatEvent;
            ReactCombatBridge.CombatStatusChanged += HandleReactCombatStatusChanged;
            StartCoroutine(ConnectAuthoritativeCombatSocketRoutine(
                AuthoritativeCombatIdentity.GetTransportId(
                    RunManager.Instance.runId,
                    RunManager.Instance.activeCombat),
                CombatModes.ToWireName(CombatMode.Pve)));
#endif
            STSSceneLoader.Instance?.SceneReady();
            return;
        }

        if (CanBootstrapAuthoritativeCombat())
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ReactCombatBridge.CombatEventReceived += HandleReactCombatEvent;
            ReactCombatBridge.CombatStatusChanged += HandleReactCombatStatusChanged;
            StartCoroutine(BootstrapAuthoritativeCombatRoutine());
#else
            StartCoroutine(BootstrapAuthoritativeCombatRoutine());
#endif
            return;
        }

        StartLocalCombatFlow();
    }

    bool CanBootstrapAuthoritativeCombat()
    {
        return RunManager.Instance != null
            && RunManager.Instance.activeEncounter != null
            && !string.IsNullOrWhiteSpace(RunManager.Instance.runId);
    }

    IEnumerator BootstrapAuthoritativeCombatRoutine()
    {
        Task<STSApiCombatStateResponse> stateTask = STSApiClient.GetCombatStateAsync(RunManager.Instance.runId);
        while (!stateTask.IsCompleted)
            yield return null;

        bool appliedAuthoritativeState = false;

        try
        {
            if (stateTask.Status == TaskStatus.RanToCompletion
                && stateTask.Result != null
                && stateTask.Result.accepted
                && stateTask.Result.combat != null)
            {
                allowTurn = true;
                ApplyAuthoritativeCombatState(stateTask.Result.combat, true);
                appliedAuthoritativeState = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-COMBAT] Failed to bootstrap authoritative combat state: {ex.Message}");
        }

        if (appliedAuthoritativeState)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(ConnectAuthoritativeCombatSocketRoutine(
                AuthoritativeCombatIdentity.GetTransportId(
                    RunManager.Instance.runId,
                    RunManager.Instance.activeCombat),
                CombatModes.ToWireName(CombatMode.Pve)));
#endif
            STSSceneLoader.Instance?.SceneReady();
            yield break;
        }

        StartLocalCombatFlow();
    }

    /// <summary>
    /// Ouvre un duel.
    ///
    /// <para>Contrairement au PvE, il n'y a rien à appliquer d'avance : le premier état
    /// qu'un duel voit est le COMBAT_SNAPSHOT que la couche React va chercher en ouvrant
    /// la socket. On se connecte, et on attend.</para>
    ///
    /// <para>Et contrairement au PvE, <b>il n'y a pas de repli sur StartLocalCombatFlow</b>.
    /// Un combat dont l'autre moitié est un autre joueur n'a pas de vérité locale : jouer
    /// une simulation en attendant afficherait un combat imaginaire.</para>
    /// </summary>
    IEnumerator BootstrapPvpBattleRoutine()
    {
        // Un yield inconditionnel : le seul autre est dans un bloc #if, et sans celui-ci la
        // méthode cesserait d'être un itérateur dans un build non-WebGL.
        yield return null;

        string battleId = RunManager.Instance != null
            ? RunManager.Instance.activePvpBattleId
            : null;

        if (string.IsNullOrWhiteSpace(battleId))
        {
            Debug.LogError("[STS-PVP] The combat scene was opened as a duel without a battle id; nothing can connect.");
        }
        else
        {
            // Avant la socket, pas après : tant qu'aucun battement n'est arrivé, le serveur
            // lit ce joueur comme absent, et l'ouverture du transport n'est pas instantanée.
            StartPvpHeartbeat(battleId);

#if UNITY_WEBGL && !UNITY_EDITOR
            ReactCombatBridge.CombatEventReceived += HandleReactCombatEvent;
            ReactCombatBridge.CombatStatusChanged += HandleReactCombatStatusChanged;
            yield return ConnectAuthoritativeCombatSocketRoutine(
                AuthoritativeCombatIdentity.GetPvpTransportId(battleId),
                CombatModes.ToWireName(CombatMode.Pvp));
#else
            Debug.LogWarning("[STS-PVP] A duel needs the React combat bridge, which exists only in a WebGL player: no state will arrive in this build.");
#endif
        }

        // Toujours, même après l'erreur : sans ça l'écran de chargement ne se lève jamais
        // et le joueur reste devant un voile, ce qui est pire qu'un combat vide.
        STSSceneLoader.Instance?.SceneReady();
    }

    // Le battement qui prouve la présence, et la boucle qui le fait battre. Les deux ne
    // vivent que le temps d'un duel : voir StartPvpHeartbeat / StopPvpHeartbeat.
    private PvpHeartbeat pvpHeartbeat;
    private Coroutine pvpHeartbeatRoutine;

    // Le verrou du bouton d'abandon. Il est ici et non dans l'UI parce que c'est le combat
    // qui sait s'il est encore en cours, et parce qu'un test peut alors le lire seul.
    private readonly SurrenderConfirmation surrenderConfirmation = new SurrenderConfirmation();
    private bool surrendering;

    /// <summary>
    /// Ouvre le battement de présence du duel.
    ///
    /// <para>Sans lui, <c>StsPvpBattleTimeoutScheduler</c> déclare forfait au bout de
    /// 120 secondes un joueur qui n'a rien joué — assis devant son écran. Le mécanisme
    /// existait entièrement côté serveur ; il n'y manquait que cet appelant.</para>
    /// </summary>
    void StartPvpHeartbeat(string battleId)
    {
        if (pvpHeartbeatRoutine != null)
            return;

        pvpHeartbeat = new PvpHeartbeat(
            () => STSApiClient.SendPvpBattleHeartbeatAsync(battleId),
            warning => Debug.LogWarning(warning));
        pvpHeartbeat.Begin();
        pvpHeartbeatRoutine = StartCoroutine(PvpHeartbeatRoutine());
    }

    /// <summary>
    /// Referme le battement. Un duel terminé dont le client continuerait de battre
    /// entretiendrait au serveur une présence qui n'existe plus.
    /// </summary>
    void StopPvpHeartbeat()
    {
        pvpHeartbeat?.Stop();

        if (pvpHeartbeatRoutine != null)
        {
            StopCoroutine(pvpHeartbeatRoutine);
            pvpHeartbeatRoutine = null;
        }
    }

    /// <summary>
    /// La boucle du duel : elle fait passer le temps au battement et à la confirmation
    /// d'abandon, et rien d'autre.
    ///
    /// <para><c>unscaledDeltaTime</c> parce qu'une pause ou un ralenti d'animation ne rend pas
    /// le joueur absent, et que le serveur, lui, compte en secondes réelles.</para>
    ///
    /// <para>La tâche rendue par <c>AdvanceAsync</c> n'est délibérément pas attendue : un
    /// battement lent ne doit pas retarder le suivant, et il ne peut pas échouer vers ici —
    /// <see cref="PvpHeartbeat"/> avale ses propres erreurs.</para>
    /// </summary>
    IEnumerator PvpHeartbeatRoutine()
    {
        while (pvpHeartbeat != null && pvpHeartbeat.IsBeating)
        {
            float elapsed = Time.unscaledDeltaTime;

            bool wasArmed = surrenderConfirmation.IsArmed;
            surrenderConfirmation.Advance(elapsed);
            // La fenêtre s'est refermée toute seule : le bouton doit le montrer, sinon il
            // resterait à « Confirmer l'abandon » alors qu'il ne confirme plus rien.
            if (wasArmed && !surrenderConfirmation.IsArmed)
                ui?.HideSurrenderPrompt();

            _ = pvpHeartbeat.AdvanceAsync(elapsed);
            yield return null;
        }

        pvpHeartbeatRoutine = null;
    }

    /// <summary>
    /// Le bouton d'abandon, branché depuis la scène.
    ///
    /// <para>La première pression ne fait qu'armer la confirmation et afficher ce que
    /// l'abandon coûte : le serveur le règle par <c>concede</c>, qui déplace le classement
    /// exactement comme le forfait d'un joueur absent. Un clic accidentel qui perdrait un
    /// match classé serait un défaut plus grave que l'absence de bouton.</para>
    ///
    /// <para>C'est du HTTP, pas une commande de la socket : le pont de combat ne connaît que
    /// <c>PLAY_CARD</c> et <c>END_TURN</c>, et n'a pas à inventer de <c>SURRENDER</c>.</para>
    /// </summary>
    public void RequestSurrender()
    {
        if (Mode != CombatMode.Pvp)
            return;

        if (combatEnded || surrendering || leavingPvpBattle)
            return;

        if (!surrenderConfirmation.Press())
        {
            ui?.ShowSurrenderPrompt(surrenderConfirmation);
            return;
        }

        ui?.HideSurrenderPrompt();
        surrendering = true;
        StartCoroutine(SurrenderRoutine());
    }

    /// Le joueur se ravise : la confirmation se désarme et le bouton reprend son texte.
    public void CancelSurrender()
    {
        surrenderConfirmation.Reset();
        ui?.HideSurrenderPrompt();
    }

    /// <summary>
    /// L'abandon lui-même.
    ///
    /// <para>On coupe le battement d'abord : une fois l'abandon parti, continuer à prouver sa
    /// présence n'a plus de sens. Puis on attend le <c>CombatEnded</c> que le serveur diffuse
    /// aux deux joueurs — c'est lui qui referme l'écran, comme pour n'importe quelle autre fin
    /// de duel, ce qui évite deux chemins de sortie qui pourraient diverger.</para>
    ///
    /// <para>Un appel qui échoue laisse le combat exactement où il était et le dit : le joueur
    /// peut réessayer. Le rejouer une seconde fois est sans danger, le serveur rend un combat
    /// déjà terminé inchangé.</para>
    /// </summary>
    IEnumerator SurrenderRoutine()
    {
        string battleId = RunManager.Instance != null
            ? RunManager.Instance.activePvpBattleId
            : null;

        StopPvpHeartbeat();
        ui?.ShowCombatNotice("Abandon en cours...");

        Task<JToken> surrenderTask = STSApiClient.SurrenderPvpBattleAsync(battleId);
        while (!surrenderTask.IsCompleted)
            yield return null;

        bool accepted = surrenderTask.Status == TaskStatus.RanToCompletion && surrenderTask.Result != null;
        if (!accepted)
        {
            Debug.LogWarning($"[STS-PVP] Surrender of battle {battleId} was not accepted; the duel goes on.");
            ui?.ShowCombatNotice("L'abandon n'a pas abouti. Le duel continue.");
            surrendering = false;
            if (!combatEnded && !leavingPvpBattle && !string.IsNullOrWhiteSpace(battleId))
                StartPvpHeartbeat(battleId);
            yield break;
        }

        Debug.Log($"[STS-PVP] Battle {battleId} surrendered; waiting for the server's CombatEnded.");
    }

    private bool leavingPvpBattle;

    /// <summary>
    /// Sort d'un duel qu'on ne sait pas jouer, en le disant (décision D3).
    ///
    /// <para>Le défaut du plan — journaliser et continuer — laissait le joueur dans un
    /// combat inerte : registre vide, donc pas d'équipes, pas de ciblage, pas de bouton de
    /// fin de tour, et rien à l'écran pour dire pourquoi. <b>Le coût de ce choix est
    /// connu :</b> partir avant la fin fait expirer nos tours et le serveur clôt le combat
    /// comme un forfait.</para>
    /// </summary>
    void LeavePvpBattle(string reason)
    {
        if (leavingPvpBattle)
            return;

        leavingPvpBattle = true;
        Debug.LogError($"[STS-PVP] Leaving the battle: {reason}");
        ui?.ShowCombatNotice(reason);
        StartCoroutine(LeavePvpBattleRoutine());
    }

    IEnumerator LeavePvpBattleRoutine()
    {
        StopPvpHeartbeat();
        yield return new WaitForSecondsRealtime(2.5f);

        ReactCombatBridge.Disconnect();
        RunManager.Instance?.EndPvpBattle();
        STSSceneLoader.Instance?.LoadScene("STS_MultiplayerMenu");
    }

    IEnumerator ConnectAuthoritativeCombatSocketRoutine(string transportId, string mode)
    {
        Task<bool> connectTask = ReactCombatBridge.ConnectAsync(transportId, mode);
        while (!connectTask.IsCompleted)
            yield return null;

        bool connected = connectTask.Status == TaskStatus.RanToCompletion && connectTask.Result;
        Debug.Log($"[STS-BRIDGE] socket connect combatId={transportId} mode={mode} success={connected}");
        if (!connected)
            Debug.LogWarning("[STS-BRIDGE] Combat socket failed to connect; commands will silently no-op until reconnected.");
    }

    void HandleReactCombatStatusChanged(string status)
    {
        Debug.Log($"[STS-BRIDGE] status changed: {status}");
        if (string.Equals(status, "DISCONNECTED", StringComparison.Ordinal))
            Debug.LogWarning("[STS-BRIDGE] Combat socket disconnected; end turn/play card commands will silently no-op until reconnected.");
    }

    void StartLocalCombatFlow()
    {
        ui.RefreshUI();

        // Build the timeline only after allies/enemies are fully hydrated from API state.
        if (!turnSystemInitialized && turnSystem != null)
        {
            turnSystem.Begin();
            turnSystemInitialized = true;
        }

        STSSceneLoader.Instance?.SceneReady();
    }

    private void EnsureAllies()
    {
        allies ??= new List<Player>();
        allies.RemoveAll(a => a == null);

        if (allies.Count > 0)
            return;

        if (RunManager.Instance != null && RunManager.Instance.player != null)
        {
            allies.Add(RunManager.Instance.player);
            return;
        }

        Debug.LogWarning("Combat started without a player ally. Creating a fallback player to keep turn flow valid.");
        Player fallbackPlayer = new Player("Player", 100);
        allies.Add(fallbackPlayer);
        if (RunManager.Instance != null)
        {
            RunManager.Instance.player = fallbackPlayer;
        }
    }

    private void EnsureEncounterEnemies()
    {
        // En duel, l'adversaire a déjà été monté par GameManager.SetupPvpBattle. Une run
        // PvE mise en pause garde son activeEncounter : sans cette sortie, la branche
        // ci-dessous remplacerait l'adversaire humain par les ennemis de cette rencontre.
        if (Mode == CombatMode.Pvp)
            return;

        if (RunManager.Instance != null && RunManager.Instance.activeEncounter != null && RunManager.Instance.activeEncounter.enemyIds != null && RunManager.Instance.activeEncounter.enemyIds.Count > 0)
        {
            enemies = new List<Character>();
            foreach (string enemyId in RunManager.Instance.activeEncounter.enemyIds)
            {
                if (string.IsNullOrWhiteSpace(enemyId))
                    continue;

                Enemy enemy = new Enemy(enemyId);
                if (enemy != null && enemy.data != null && enemy.IsAlive)
                {
                    enemies.Add(enemy);
                }
                else
                {
                    Debug.LogWarning($"Encounter enemy '{enemyId}' could not be initialized from local data.");
                }
            }

            if (enemies.Count > 0)
            {
                Debug.Log($"[STS-COMBAT] Initialized authoritative encounter enemies=[{string.Join(",", enemies.Select(enemy => enemy.name))}]");
                return;
            }
        }

        if (enemies != null && enemies.Count > 0)
            return;

        Debug.LogWarning("Combat started with no enemies. Spawning a fallback Ironclad enemy so combat can continue.");
        enemies = new List<Character> { CreateFallbackIroncladEnemy() };
    }

    private Enemy CreateFallbackIroncladEnemy()
    {
        EnemyData ironcladData = EnemyDataDatabase.Get("Ironclad")
            ?? Resources.Load<EnemyData>("STS/Enemies/Ironclad");

        if (ironcladData != null)
        {
            return new Enemy(ironcladData);
        }

        Debug.LogWarning("Ironclad enemy data was not found. Creating a minimal runtime Ironclad fallback.");

        EnemyData runtimeData = ScriptableObject.CreateInstance<EnemyData>();
        runtimeData.name = "Ironclad";
        runtimeData.id = "Ironclad";
        runtimeData.enemyName = "Ironclad";
        runtimeData.displayName = "Ironclad";
        runtimeData.maxHP = 30;
        runtimeData.randomStart = false;
        runtimeData.pattern = new List<STSCardData>();
        runtimeData.movePattern = new List<EnemyMoveEntry>();
        runtimeData.rewardCards = new List<STSCardData>();
        runtimeData.startingStatusInfo = string.Empty;

        return new Enemy(runtimeData);
    }

    public void FieldTurnEnd()
    {
        foreach (var character in GetAllCharacters())
        {
            character.FieldTurnEnd();
        }
    }


    public void PlayCard(Character source, CardInstance card, List<Character> targets, bool ignoreEnergy = false, bool createView = false)
    {
        Debug.Log($"[STS-COMBAT] PlayCard entered card={card?.displayName ?? "<null>"} instanceId={card?.instanceId ?? "<null>"} source={(source != null ? source.name : "<null>")} targets={targets?.Count ?? 0} authoritative={UsesAuthoritativeCombat} inFlight={authoritativeCommandInFlight}");

        if (UsesAuthoritativeCombat && source != null && source.isPlayer)
        {
            if (combatEnded)
            {
                Debug.LogWarning($"[STS-COMBAT] PlayCard blocked: combat already ended card={card?.displayName ?? "<null>"}");
                return;
            }

            if (AuthoritativeCommandBusy)
            {
                Debug.LogWarning($"[STS-COMBAT] PlayCard blocked: authoritative command already in flight card={card?.displayName ?? "<null>"}");
                return;
            }

            queuedCardPlays++;
            StartCoroutine(PlayCardAuthoritativeRoutine(card, targets));
            return;
        }

        queuedCardPlays++;
        StartCoroutine(PlayCardRoutine(source, card, targets, ignoreEnergy, createView));
    }

    IEnumerator PlayCardAuthoritativeRoutine(CardInstance card, List<Character> targets)
    {
        authoritativeCommandInFlight = true;
        authoritativeCommandInFlightSince = Time.unscaledTime;
        activeCardPlays++;
        queuedCardPlays = Mathf.Max(0, queuedCardPlays - 1);

        List<string> selectedCardInstanceIds = new();
        yield return CollectAuthoritativeCardSelection(card, selectedCardInstanceIds);

        // Gate on the one thing we can validate locally before ever contacting the server: an
        // unaffordable card would otherwise still get submitted only to be rejected, wasting a
        // round trip for feedback we already know the answer to. With several allies the energy
        // pool belongs to the ally whose turn it is, not necessarily the first one.
        // Le coût se calcule dans un contexte, comme partout ailleurs : sans lui, une carte
        // dont une relique ou un statut baisse le prix serait comparée à l'énergie sur son
        // coût de base, et refusée ici alors que le serveur l'accepterait.
        Player actingPlayer = GetActingPlayer();
        EffectContext costContext = new EffectContext
        {
            source = actingPlayer,
            target = null,
            combat = this,
            state = state,
            card = card,
            timeline = turnSystem != null ? turnSystem.timeline : null,
            targets = targets
        };
        int cardCost = card != null ? card.Cost(costContext) : 0;
        if (actingPlayer != null && cardCost >= 0 && actingPlayer.resources.energy < cardCost)
        {
            authoritativeCommandInFlight = false;
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
            Debug.Log($"[STS-COMBAT] PlayCard rejected locally: insufficient energy ({actingPlayer.resources.energy} < {cardCost}) card={card?.displayName ?? "<null>"}");
            ui.StartCoroutine(ui.EnergyTextGlowRed());
            yield break;
        }

        // The card is only ever animated to the middle once the server actually accepts the
        // play: for WebGL that happens when the resulting CardPlayed event arrives over the
        // socket and ReplayCardPlayedEvent presents it; for REST it happens below once
        // response.accepted is true. A rejected command must never show the card being played.

#if UNITY_WEBGL && !UNITY_EDITOR
        var payload = new
        {
            cardInstanceId = card != null ? card.instanceId : null,
            targetIds = MapTargetsToAuthoritativeIds(targets),
            selectedCardInstanceIds
        };
        string currentRev = ReactCombatBridge.CurrentRevision ?? GetAuthoritativeRevision().ToString();
        Debug.Log($"[STS-COMBAT] Sending PLAY_CARD card={card?.displayName ?? "<null>"} instanceId={card?.instanceId ?? "<null>"} targetIds=[{string.Join(",", payload.targetIds)}] revision={currentRev}");
        Task<ReactCombatCommandOutcome> commandTask = ReactCombatBridge.SendCommandAsync("PLAY_CARD", payload, currentRev);

        // Don't trust Task.Delay alone to bound this wait; poll a frame-based deadline too so a
        // dropped/never-acked socket command can't hang the coroutine past the watchdog.
        float deadline = Time.unscaledTime + AuthoritativeCommandWatchdogSeconds;
        while (!commandTask.IsCompleted && Time.unscaledTime < deadline)
            yield return null;

        bool needsResyncWebGL = false;
        try
        {
            if (!commandTask.IsCompleted)
            {
                Debug.LogWarning("[STS-COMBAT] PLAY_CARD via Bridge never completed before deadline; socket may be disconnected.");
                needsResyncWebGL = true;
            }
            else
            {
                Debug.Log($"[STS-COMBAT] PLAY_CARD completed taskStatus={commandTask.Status} outcome={(commandTask.Status == TaskStatus.RanToCompletion ? commandTask.Result.ToString() : "<none>")}");
                if (commandTask.Status == TaskStatus.RanToCompletion && commandTask.Result == ReactCombatCommandOutcome.Rejected)
                {
                    // The state never changed for a rejected play, so there is nothing to resync;
                    // no CardPlayed event will ever arrive for this attempt either.
                    Debug.LogWarning($"[STS-COMBAT] Backend rejected play-card command via Bridge card={card?.displayName ?? "<null>"}");
                }
                // Anything that is not a confirmation leaves us unsure of the server's
                // state, so resync. Testing for "not confirmed" rather than listing the
                // outcomes we know keeps a new one from being silently ignored here.
                else if (commandTask.Status != TaskStatus.RanToCompletion
                    || commandTask.Result != ReactCombatCommandOutcome.Confirmed)
                {
                    Debug.LogWarning($"[STS-COMBAT] Play-card command did not confirm via Bridge outcome={(commandTask.Status == TaskStatus.RanToCompletion ? commandTask.Result.ToString() : "<none>")}");
                    needsResyncWebGL = true;
                }
            }
        }
        finally
        {
            authoritativeCommandInFlight = false;
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
        }

        // A lost/unacknowledged command leaves the client's view of whose turn it is stale;
        // re-fetch the authoritative state so the UI does not freeze forever.
        if (needsResyncWebGL)
            yield return RefreshAuthoritativeCombatState();
#else
        Task<STSApiCombatCommandResponse> commandTask = STSApiClient.SubmitCombatCommandAsync(
            RunManager.Instance != null ? RunManager.Instance.runId : null,
            new STSApiCombatCommandRequest
            {
                commandType = "PLAY_CARD",
                expectedRevision = GetAuthoritativeRevision(),
                cardInstanceId = card != null ? card.instanceId : null,
                targetIds = MapTargetsToAuthoritativeIds(targets),
                selectedCardInstanceIds = selectedCardInstanceIds
            });

        while (!commandTask.IsCompleted)
            yield return null;

        bool needsResync = false;
        try
        {
            if (commandTask.Status != TaskStatus.RanToCompletion || commandTask.Result == null)
            {
                Debug.LogWarning("[STS-COMBAT] Failed to submit backend play-card command.");
                needsResync = true;
            }
            else
            {
                STSApiCombatCommandResponse response = commandTask.Result;
                if (!response.accepted)
                {
                    if (string.Equals(response.rejectionCode, "INSUFFICIENT_ENERGY", StringComparison.OrdinalIgnoreCase))
                    {
                        ui.StartCoroutine(ui.EnergyTextGlowRed());
                    }
                    else
                    {
                        Debug.LogWarning($"[STS-COMBAT] Backend play-card rejected: {response.rejectionCode} {response.rejectionMessage}");
                    }
                    // A rejection can mean the client's cached revision drifted from the server
                    // (e.g. AI turns advanced in a previous request); resync so play/end-turn keep working.
                    needsResync = true;
                }
                else
                {
                    yield return ReplayAuthoritativeEvents(response.events);
                    ApplyAuthoritativeCombatState(response.combat, true);
                }
            }
        }
        finally
        {
            authoritativeCommandInFlight = false;
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
        }

        if (needsResync)
            yield return RefreshAuthoritativeCombatState();
#endif
    }

    IEnumerator RefreshAuthoritativeCombatState()
    {
        // La route de resynchronisation est celle d'une run. Un duel se resynchronise par
        // la couche React, qui refait son snapshot sur l'endpoint PvP dès qu'elle voit un
        // trou de révision ; passer par ici lui appliquerait l'état d'une run en pause.
        if (Mode != CombatMode.Pve
            || RunManager.Instance == null
            || string.IsNullOrWhiteSpace(RunManager.Instance.runId))
            yield break;

        Task<STSApiCombatStateResponse> stateTask = STSApiClient.GetCombatStateAsync(RunManager.Instance.runId);
        while (!stateTask.IsCompleted)
            yield return null;

        if (stateTask.Status == TaskStatus.RanToCompletion
            && stateTask.Result != null
            && stateTask.Result.accepted
            && stateTask.Result.combat != null)
        {
            ApplyAuthoritativeCombatState(stateTask.Result.combat, true);
        }
    }

    IEnumerator CollectAuthoritativeCardSelection(CardInstance playedCard, List<string> selectedIds)
    {
        if (deck == null || playedCard == null || playedCard.data == null || playedCard.data.effects == null)
            yield break;

        EffectEntry effect = playedCard.GetEffects().FirstOrDefault(entry => entry.type == EffectType.CardSelection);
        if (effect == null)
            yield break;

        // Every CardSelectionEffect the server actually resolves needs its selection collected,
        // not just the original four (Exhaust/Discard/ReturnToHand/TopOfDrawPile) — None is the
        // only one it never implements (CardSelectionSupport.IMPLEMENTED_ACTIONS). Restricting
        // this to that first quartet is what made every other CardSelection card (Enchant,
        // Transform, ReduceCost, ...) submit an empty selection and get rejected as
        // INVALID_CARD_SELECTION even though the server could resolve it.
        if (effect.cardSelectionEffect == CardSelectionEffect.None)
            yield break;

        System.Predicate<CardInstance> predicate = BuildCardSelectionFilter(effect.cardFilterTags);

        List<CardInstance> candidates = effect.cardSelectionSource switch
        {
            CardSelectionSource.Hand => deck.hand,
            CardSelectionSource.DrawPile => deck.drawPile,
            CardSelectionSource.DiscardPile => deck.discardPile,
            CardSelectionSource.ExhaustPile => deck.exhaustPile,
            CardSelectionSource.All => deck.hand.Concat(deck.discardPile).Concat(deck.drawPile).Concat(deck.exhaustPile).ToList(),
            CardSelectionSource.AllExceptExhaustPile => deck.hand.Concat(deck.discardPile).Concat(deck.drawPile).ToList(),
            _ => new List<CardInstance>()
        };
        candidates = candidates
            .Where(candidate => candidate != null && candidate.instanceId != playedCard.instanceId && predicate(candidate))
            .ToList();
        int amount = effect.value < 0 ? candidates.Count : Mathf.Min(effect.value, candidates.Count);
        if (amount == 0)
            yield break;

        List<CardInstance> selectedCards = new();
        if (effect.cardSelectionSource == CardSelectionSource.Hand)
        {
            HashSet<string> candidateIds = candidates.Select(candidate => candidate.instanceId).ToHashSet();
            var request = new CardSelectionRequest
            {
                amount = amount,
                message = $"Choisissez {amount} carte" + (amount > 1 ? "s" : ""),
                filter = candidate => candidate != null && candidateIds.Contains(candidate.instanceId)
            };
            yield return ui.RequestCardSelection(request, cards => selectedCards = cards);
        }
        else if (candidates.Count <= amount)
        {
            selectedCards = candidates;
        }
        else
        {
            var panel = RunManager.Instance.ui.deckGridPanel;
            panel.Show(candidates, "Choisissez des cartes");
            SelectionManager.Instance.StartSelection(amount, cards => selectedCards = cards);
            while (SelectionManager.Instance.selectionMode)
                yield return null;
            panel.Hide();
        }

        selectedIds.AddRange(selectedCards.Select(selected => selected.instanceId));
    }

    /// <summary>
    /// The same OR-combined tag predicate EffectResolver.Apply's own CardSelection case builds
    /// for local combat, needed here too now that the authoritative path collects a selection
    /// for a filtered CardSelection instead of skipping it outright.
    /// </summary>
    static System.Predicate<CardInstance> BuildCardSelectionFilter(List<CardFilterTag> tags)
    {
        if (tags == null || tags.Count == 0)
            return _ => true;

        return candidate =>
        {
            if (candidate == null || candidate.data == null)
                return false;

            foreach (var tag in tags)
            {
                switch (tag)
                {
                    case CardFilterTag.Attack:
                        if (candidate.data.type == CardType.Attaque) return true;
                        break;
                    case CardFilterTag.Skill:
                        if (candidate.data.type == CardType.Compétence) return true;
                        break;
                    case CardFilterTag.Power:
                        if (candidate.data.type == CardType.Pouvoir) return true;
                        break;
                    case CardFilterTag.Retain:
                        if (candidate.data.HasTag(CardTag.Retain)) return true;
                        break;
                    case CardFilterTag.Cost0:
                        if (candidate.data.cost == 0) return true;
                        break;
                    case CardFilterTag.Cost1:
                        if (candidate.data.cost == 1) return true;
                        break;
                    case CardFilterTag.Cost2:
                        if (candidate.data.cost == 2) return true;
                        break;
                    case CardFilterTag.Cost3Plus:
                        if (candidate.data.cost >= 3) return true;
                        break;
                    case CardFilterTag.Unupgraded:
                        if (!candidate.HasEnchantments()) return true;
                        break;
                    case CardFilterTag.Upgraded:
                        if (candidate.HasEnchantments()) return true;
                        break;
                    case CardFilterTag.Atom:
                        if (candidate.HasTag(CardTag.Atom)) return true;
                        break;
                    case CardFilterTag.Molecule:
                        if (candidate.HasTag(CardTag.Molecule)) return true;
                        break;
                    default:
                        break;
                }
            }
            return false;
        };
    }

    void HandleReactCombatEvent(string json)
    {
        JObject message;
        try { message = JObject.Parse(json); } catch { return; }

        authoritativeMessageQueue.Enqueue(message);
        if (!authoritativeMessageQueueRunning)
            StartCoroutine(ProcessAuthoritativeMessageQueue());
    }

    IEnumerator ProcessAuthoritativeMessageQueue()
    {
        authoritativeMessageQueueRunning = true;
        while (authoritativeMessageQueue.Count > 0)
        {
            JObject message = authoritativeMessageQueue.Dequeue();
            string type = message.Value<string>("type");
            IEnumerator subRoutine = null;
            try
            {
                if (type == "COMBAT_SNAPSHOT")
                {
                    JToken state = message["payload"]?["state"];
                    if (state != null) ApplyAuthoritativeCombatState(state, true);
                }
                else if (type == "COMBAT_EVENT")
                {
                    JToken payload = message["payload"];
                    if (payload != null)
                        subRoutine = ReplayAuthoritativeEvents(new List<JToken> { payload });
                }
                else if (type == "STATE_UPDATED")
                {
                    JToken payload = message["payload"];
                    if (payload != null) ApplyAuthoritativeCombatState(payload, true);
                }
                else if (type == "COMMAND_REJECTED")
                {
                    HandleCommandRejected(message["payload"]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[STS-COMBAT] Exception processing authoritative message type={type}: {ex}");
            }

            if (subRoutine != null)
            {
                yield return subRoutine;
            }
        }

        authoritativeMessageQueueRunning = false;
    }

    /// <summary>
    /// Le serveur a refusé une commande, dans le vocabulaire de son moteur.
    ///
    /// <para>Le refus était déjà réglé un cran plus bas — le noyau du pont libère la
    /// commande en attente, donc rien ne se bloquait — mais il n'atteignait jamais
    /// l'écran. Une carte refusée se contentait de ne pas bouger, et le joueur
    /// recommençait.</para>
    ///
    /// <para>On ne resynchronise pas ici, volontairement : le chemin PvE resynchronise
    /// déjà sur l'issue de la commande, et un duel se resynchronise par la couche
    /// React.</para>
    /// </summary>
    void HandleCommandRejected(JToken payload)
    {
        string code = payload?.Value<string>("code");
        string serverMessage = payload?.Value<string>("message");
        Debug.LogWarning($"[STS-COMBAT] Command rejected code={code ?? "<none>"} message={serverMessage ?? "<none>"}");

        if (ui == null)
            return;

        if (CombatRejectionMessages.WarrantsEnergyGlow(code))
            ui.StartCoroutine(ui.EnergyTextGlowRed());

        ui.ShowCombatNotice(CombatRejectionMessages.ForCode(code));
    }

    void OnDestroy()
    {
        StopPvpHeartbeat();
#if UNITY_WEBGL && !UNITY_EDITOR
        ReactCombatBridge.CombatEventReceived -= HandleReactCombatEvent;
        ReactCombatBridge.CombatStatusChanged -= HandleReactCombatStatusChanged;
#endif
        authoritativeMessageQueue.Clear();
        authoritativeMessageQueueRunning = false;
        combatantRegistry.Clear();
        combatantRegistryBuilt = false;
        combatantPiles.Clear();
        authoritativeTimelinePendingSelfDelays.Clear();
    }

    public void RequestAuthoritativeEndTurn()
    {
        if (!UsesAuthoritativeCombat)
            return;

        if (combatEnded)
        {
            Debug.LogWarning("[STS-COMBAT] EndTurn blocked: combat already ended.");
            return;
        }

        if (AuthoritativeCommandBusy)
        {
            Debug.LogWarning("[STS-COMBAT] EndTurn blocked: authoritative command already in flight.");
            return;
        }

        StartCoroutine(AuthoritativeEndTurnRoutine());
    }

    IEnumerator AuthoritativeEndTurnRoutine()
    {
        authoritativeCommandInFlight = true;
        authoritativeCommandInFlightSince = Time.unscaledTime;
        activeCardPlays++;

#if UNITY_WEBGL && !UNITY_EDITOR
        var payload = new { targetIds = new List<string>() };
        string currentRev = ReactCombatBridge.CurrentRevision ?? GetAuthoritativeRevision().ToString();
        Task<ReactCombatCommandOutcome> commandTask = ReactCombatBridge.SendCommandAsync("END_TURN", payload, currentRev);

        // Don't trust Task.Delay alone to bound this wait; poll a frame-based deadline too so a
        // dropped/never-acked socket command can't hang the coroutine past the watchdog.
        float deadline = Time.unscaledTime + AuthoritativeCommandWatchdogSeconds;
        while (!commandTask.IsCompleted && Time.unscaledTime < deadline)
            yield return null;

        bool needsResyncWebGL = false;
        try
        {
            if (!commandTask.IsCompleted)
            {
                Debug.LogWarning("[STS-COMBAT] END_TURN via Bridge never completed before deadline; socket may be disconnected.");
                needsResyncWebGL = true;
            }
            // A rejection was evaluated against a known state, so there is nothing to
            // resync. Everything else — a failure, an outcome this build does not know —
            // leaves us unsure, and guessing is what softlocked a combat before.
            else if (commandTask.Status != TaskStatus.RanToCompletion
                || (commandTask.Result != ReactCombatCommandOutcome.Confirmed
                    && commandTask.Result != ReactCombatCommandOutcome.Rejected))
            {
                Debug.LogWarning($"[STS-COMBAT] End-turn command did not confirm via Bridge outcome={(commandTask.Status == TaskStatus.RanToCompletion ? commandTask.Result.ToString() : "<none>")}");
                needsResyncWebGL = true;
            }
        }
        finally
        {
            authoritativeCommandInFlight = false;
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
        }

        if (needsResyncWebGL)
            yield return RefreshAuthoritativeCombatState();
#else
        Task<STSApiCombatCommandResponse> commandTask = STSApiClient.SubmitCombatCommandAsync(
            RunManager.Instance != null ? RunManager.Instance.runId : null,
            new STSApiCombatCommandRequest
            {
                commandType = "END_TURN",
                expectedRevision = GetAuthoritativeRevision(),
                targetIds = new List<string>()
            });

        while (!commandTask.IsCompleted)
            yield return null;

        bool needsResync = false;
        try
        {
            if (commandTask.Status != TaskStatus.RanToCompletion || commandTask.Result == null)
            {
                Debug.LogWarning("[STS-COMBAT] Failed to submit backend end-turn command.");
                needsResync = true;
            }
            else
            {
                STSApiCombatCommandResponse response = commandTask.Result;
                if (!response.accepted)
                {
                    Debug.LogWarning($"[STS-COMBAT] Backend end-turn rejected: {response.rejectionCode} {response.rejectionMessage}");
                    needsResync = true;
                }
                else
                {
                    yield return ReplayAuthoritativeEvents(response.events);
                    ApplyAuthoritativeCombatState(response.combat, true);
                }
            }
        }
        finally
        {
            authoritativeCommandInFlight = false;
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
        }

        if (needsResync)
            yield return RefreshAuthoritativeCombatState();
#endif
    }

    long GetAuthoritativeRevision()
    {
        // Le champ, pas la run : en duel la run ne porte pas cet état, et le lire là-bas
        // rendrait la révision d'une run en pause.
        JToken state = authoritativeCombatState;
        if (state == null || state.Type != JTokenType.Object)
            return 0L;

        return state.Value<long?>("revision") ?? 0L;
    }

    List<string> MapTargetsToAuthoritativeIds(List<Character> targets)
    {
        var ids = new List<string>();
        if (targets == null)
            return ids;

        foreach (Character target in targets)
        {
            string combatantId = GetAuthoritativeCombatantId(target);
            if (!string.IsNullOrWhiteSpace(combatantId))
            {
                ids.Add(combatantId);
            }
        }

        return ids;
    }

    string GetAuthoritativeCombatantId(Character character)
    {
        return combatantRegistry.IdOf(character);
    }

    void ApplyAuthoritativeCombatState(JToken combatToken, bool refreshUI)
    {
        if (combatToken == null || combatToken.Type != JTokenType.Object || RunManager.Instance == null)
            return;

        authoritativeCombatState = combatToken;
        if (Mode != CombatMode.Pvp)
            RunManager.Instance.activeCombat = combatToken;

        // Absents en PvE, où le tour n'expire pas : FromState rend alors None et rien ne
        // s'affiche.
        turnCountdown = TurnCountdown.FromState(
            combatToken.Value<string>("turnDeadline"),
            combatToken.Value<string>("serverTime"),
            DateTimeOffset.UtcNow);

        JArray combatants = combatToken["combatants"] as JArray;
        if (combatants == null)
            return;

        if (!combatantRegistryBuilt)
            BuildCombatantRegistry(combatToken);

        foreach (Character character in GetAllCharacters())
        {
            if (character != null)
                character.onTurn = false;
        }

        string activeCombatantId = combatToken.Value<string>("activeCombatantId");

        foreach (JToken combatantToken in combatants)
        {
            if (combatantToken == null || combatantToken.Type != JTokenType.Object)
                continue;

            string combatantId = combatantToken.Value<string>("combatantId");
            Character target = ResolveCombatant(combatantId);
            if (target == null)
                continue;

            target.maxHP = combatantToken.Value<int?>("maxHp") ?? target.maxHP;
            target.currentHP = combatantToken.Value<int?>("hp") ?? target.currentHP;
            target.armor = combatantToken.Value<int?>("armor") ?? target.armor;
            target.resources.energy = combatantToken.Value<int?>("energy") ?? target.resources.energy;
            ApplyAuthoritativeStatuses(target, combatantToken["statuses"]);
            target.onTurn = !string.IsNullOrWhiteSpace(activeCombatantId)
                && string.Equals(combatantId, activeCombatantId, StringComparison.Ordinal);

            if (target is Enemy enemy)
            {
                int? patternIndex = combatantToken.Value<int?>("patternIndex");
                if (patternIndex.HasValue)
                {
                    enemy.SetPatternIndex(patternIndex.Value);
                }
                string intentCardId = combatantToken.Value<string>("intentCardId");
                if (!string.IsNullOrWhiteSpace(intentCardId))
                {
                    CardInstance intentCard = BuildCardFromDefinition(intentCardId, "intent-" + intentCardId);
                    enemy.SetAuthoritativeIntentCard(intentCard != null ? intentCard.data : null);
                }
            }

            RegisterCombatantPiles(combatantId, target, combatantToken);

            // Only the primary player combatant owns the shared deck/hand UI; extra allies have
            // their own piles server-side and must not overwrite the local deck state.
            // Le registre sait qui est local ; la chaîne "player" ne le savait qu'en PvE, et
            // en duel cette condition ne se vérifiait jamais — la main restait vide.
            if (combatantRegistry.IsLocalCombatant(combatantId))
            {
                ApplyAuthoritativePlayerPiles(combatantToken["piles"]);
            }
        }

        state.turnCount = AuthoritativeCombatStateReducer.ResolveTurnCount(
            state.turnCount,
            combatToken.Value<string>("status"));

        if (turnSystem != null && turnSystem.endTurnButton != null)
        {
            // On ne finit que son propre tour. « N'importe quel combattant du côté joueur »
            // marchait tant que le seul combattant humain était nous ; en duel, l'adversaire
            // est un humain lui aussi, et en co-op ce serait le tour d'un allié.
            // Le repli sur isPlayer couvre le tutoriel, où le registre est vide.
            Character activeCombatant = ResolveCombatant(activeCombatantId);
            bool ours = combatantRegistry.LocalCombatantId != null
                ? combatantRegistry.IsLocalCombatant(activeCombatantId)
                : activeCombatant != null && activeCombatant.isPlayer;
            turnSystem.endTurnButton.interactable = ours && !combatEnded;
        }

        ApplyAuthoritativeTimeline(combatToken["timeline"] as JArray);

        if (refreshUI && ui != null)
        {
            // Don't call SyncHandFromDeckState here — the COMBAT_EVENT replays already handle hand state,
            // and calling it destroys card views that are still being animated.
            // Skip the hand re-layout while any card is still mid-animation, not just while the
            // optimistic presentation was recently started: the state's echo arrives after the
            // presentation finishes, and a layout run then would snap the remaining cards into
            // place again even though the leaving card's own animation already settled the hand.
            ui.RefreshUI(false, skipHandLayout: ui.HandHasAnimatingCard);
        }

        // The first application seeds the hand from raw pile data, since no CardDrawn events exist
        // yet to replay. Every later one reconciles it: the state hands us freshly built
        // CardInstance objects, so a view made earlier points at an object the hand no longer
        // holds. Seeding once and trusting the event replays alone left the player holding a card
        // the server had already discarded — refused as CARD_NOT_IN_HAND — and never seeing the
        // cards it had dealt. The reconciliation only rebuilds on drift, so views being animated
        // are left alone.
        if (ui != null)
        {
            authoritativeHandSynced = true;
            ui.SyncHandFromDeckStateIfDrifted();
        }

        TryEndCombatIfNeeded();
    }

    /// <summary>
    /// Les secondes restantes au tour en cours, ou null quand ce combat n'a pas de limite
    /// de temps. Nul aussi une fois le combat terminé : un compte à rebours qui continue
    /// de tourner sur un combat fini est un mensonge.
    /// </summary>
    public double? SecondsLeftInTurn()
    {
        if (combatEnded || !turnCountdown.HasDeadline)
            return null;

        return turnCountdown.SecondsRemainingAt(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// « Main 3 · Pioche 12 » pour un combattant dont on n'a pas le droit de voir les
    /// cartes ; null pour tout autre — un ennemi PvE n'a aucune pile enregistrée, et le
    /// joueur local montre sa vraie main.
    /// </summary>
    public string RemotePilesSummary(Character character)
    {
        ICombatantPiles<CardInstance> piles =
            combatantPiles.For(combatantRegistry.IdOf(character));
        if (piles == null || piles.IsFullyVisible)
            return null;

        return $"Main {piles.Count(PileKind.Hand)}  ·  Pioche {piles.Count(PileKind.Draw)}";
    }

    void ApplyAuthoritativeTimeline(JArray timelineArray)
    {
        if (turnSystem == null || turnSystem.timelineUI == null || timelineArray == null)
            return;

        var seenCombatantIds = new HashSet<string>();
        List<TurnEntry> authoritativeTimeline = new List<TurnEntry>();
        foreach (JToken entryToken in timelineArray)
        {
            if (entryToken == null || entryToken.Type != JTokenType.Object)
                continue;

            string combatantId = entryToken.Value<string>("combatantId");
            Character entryCharacter = ResolveCombatant(combatantId);
            if (entryCharacter == null || string.IsNullOrWhiteSpace(combatantId))
                continue;

            // Reuse the same TurnEntry (and uid) per combatant across syncs, otherwise every
            // sync looks like a brand-new entry to TimelineUI and icons re-appear from the edge
            // instead of animating from their previous position.
            if (!authoritativeTimelineEntries.TryGetValue(combatantId, out TurnEntry entry))
            {
                entry = new TurnEntry { character = entryCharacter, uid = TurnEntry.nextUID++ };
                authoritativeTimelineEntries[combatantId] = entry;
            }
            entry.character = entryCharacter;
            entry.time = entryToken.Value<long?>("readyAtTick") ?? 0L;

            long pendingSelfDelay = entryToken.Value<long?>("pendingSelfDelayTicks") ?? 0L;
            if (pendingSelfDelay == 0L)
                authoritativeTimelinePendingSelfDelays.Remove(combatantId);
            else
                authoritativeTimelinePendingSelfDelays[combatantId] = pendingSelfDelay;

            authoritativeTimeline.Add(entry);
            seenCombatantIds.Add(combatantId);
        }

        foreach (string staleId in authoritativeTimelineEntries.Keys.Where(id => !seenCombatantIds.Contains(id)).ToList())
        {
            authoritativeTimelineEntries.Remove(staleId);
            authoritativeTimelinePendingSelfDelays.Remove(staleId);
        }

        turnSystem.timeline = authoritativeTimeline.OrderBy(entry => entry.time).ToList();
        RefreshTimelineDisplay();
    }

    // Individual TurnEnded events fire once per internal AI step (an End Turn command can
    // resolve a whole chain of enemy turns in one round trip), but only the final STATE_UPDATED
    // carries the full timeline JSON. Updating only there made the whole chain jump in one big
    // leap instead of sliding turn-by-turn the way the old local timeline always did.
    void ApplyTurnEndedToTimeline(JToken combatEvent)
    {
        if (turnSystem == null || turnSystem.timelineUI == null)
            return;

        string combatantId = combatEvent.Value<string>("combatantId");
        long? nextReadyAtTick = combatEvent.Value<long?>("nextReadyAtTick");
        if (string.IsNullOrWhiteSpace(combatantId) || !nextReadyAtTick.HasValue)
            return;

        if (!authoritativeTimelineEntries.TryGetValue(combatantId, out TurnEntry entry))
            return;

        // A fresh uid here, not a mutation of the consumed entry, is what makes TimelineUI treat
        // this as "that icon reached the marker and disappeared, a new one appears for their next
        // turn" instead of the same icon sliding continuously between its old and new time — the
        // old local combat timeline achieved this the same way (remove the entry, append a new one).
        authoritativeTimelineEntries[combatantId] = new TurnEntry
        {
            character = entry.character,
            time = nextReadyAtTick.Value,
            uid = TurnEntry.nextUID++
        };
        authoritativeTimelinePendingSelfDelays.Remove(combatantId);

        turnSystem.timeline = authoritativeTimelineEntries.Values.OrderBy(e => e.time).ToList();
        RefreshTimelineDisplay();
    }

    // Builds the projected lookahead from the current authoritativeTimelineEntries and pushes it
    // to the UI. Called after a full sync and after each TurnEnded event.
    void RefreshTimelineDisplay()
    {
        // The server only ever reports one upcoming entry per combatant, so a single-step guess
        // could never show a fast combatant taking two turns before a slower one's next turn —
        // exactly the case players noticed the timeline getting wrong. Project several steps per
        // combatant instead, keyed by the server's own combatantId (not re-derived from the
        // character reference, which drifts once any earlier enemy dies and shifts indices).
        // This stays a separate display-only list: other systems (CardDrag's play preview,
        // CurrentCharacter, SyncTimelineWithLivingCharacters) assume exactly one entry per living
        // combatant, and mutating the real list doubled every character there.
        const int projectionStepsPerCombatant = 4;
        var projectionKeys = new HashSet<string>();
        List<TurnEntry> displayTimeline = new List<TurnEntry>(turnSystem.timeline);
        foreach (var kvp in authoritativeTimelineEntries)
        {
            string combatantId = kvp.Key;
            TurnEntry realEntry = kvp.Value;
            if (realEntry.character == null)
                continue;

            long projectedTime = (long)realEntry.time;
            long pendingSelfDelay = authoritativeTimelinePendingSelfDelays.TryGetValue(
                combatantId,
                out long pendingDelay)
                ? pendingDelay
                : 0L;
            for (int step = 0; step < projectionStepsPerCombatant; step++)
            {
                projectedTime += (long)Mathf.Max(1f, realEntry.character.turnDelay(turnSystem.baseDelay));
                if (step == 0)
                {
                    projectedTime = Math.Max(0L, projectedTime + pendingSelfDelay);
                }
                // A projection belongs to this specific real turn. When that turn is consumed,
                // its new uid produces fresh future icons instead of moving the old ones back.
                string projectionKey = $"{combatantId}:{realEntry.uid}#{step}";
                projectionKeys.Add(projectionKey);

                if (!authoritativeTimelineProjectionEntries.TryGetValue(projectionKey, out TurnEntry projectedEntry))
                {
                    projectedEntry = new TurnEntry { character = realEntry.character, uid = TurnEntry.nextUID++ };
                    authoritativeTimelineProjectionEntries[projectionKey] = projectedEntry;
                }
                projectedEntry.character = realEntry.character;
                projectedEntry.time = projectedTime;
                displayTimeline.Add(projectedEntry);
            }
        }

        foreach (string staleKey in authoritativeTimelineProjectionEntries.Keys.Where(key => !projectionKeys.Contains(key)).ToList())
            authoritativeTimelineProjectionEntries.Remove(staleKey);

        turnSystem.timelineUI.Display(displayTimeline.OrderBy(entry => entry.time).ToList());
    }

    IEnumerator ReplayAuthoritativeEvents(List<JToken> events)
    {
        if (events == null || events.Count == 0)
            yield break;

        foreach (JToken combatEvent in events)
        {
            if (combatEvent == null || combatEvent.Type != JTokenType.Object)
                continue;

            string eventType = ResolveCombatEventType(combatEvent);
            IEnumerator handler = null;
            try
            {
                switch (eventType)
                {
                    case "CardPlayed":
                        handler = ReplayCardPlayedEvent(combatEvent);
                        break;
                    case "CardDrawn":
                        handler = ReplayCardDrawnEvent(combatEvent);
                        break;
                    case "CardMoved":
                        handler = ReplayCardMovedEvent(combatEvent);
                        break;
                    case "CardMerged":
                        ReplayCardMergedEvent(combatEvent);
                        break;
                    case "PileShuffled":
                        handler = ReplayPileShuffledEvent(combatEvent);
                        break;
                    case "StatusApplied":
                        handler = ReplayStatusAppliedEvent(combatEvent);
                        break;
                    case "StatusRemoved":
                        ReplayStatusRemovedEvent(combatEvent);
                        handler = DelaySeconds(0.05f);
                        break;
                    case "StatusUpdated":
                        ReplayStatusUpdatedEvent(combatEvent);
                        handler = DelaySeconds(0.05f);
                        break;
                    case "DamageApplied":
                        ReplayDamageAppliedEvent(combatEvent);
                        handler = DelaySeconds(0.12f);
                        break;
                    case "HealApplied":
                        ReplayHealAppliedEvent(combatEvent);
                        handler = DelaySeconds(0.12f);
                        break;
                    case "HpLost":
                        ReplayHpLostEvent(combatEvent);
                        handler = DelaySeconds(0.12f);
                        break;
                    case "ArmorGained":
                        handler = ReplayArmorGainedEvent(combatEvent);
                        break;
                    case "ArmorBroken":
                        ReplayArmorBrokenEvent(combatEvent);
                        handler = FlashCombatantWhite(ResolveCombatant(combatEvent.Value<string>("targetId")));
                        break;
                    case "EnergySpent":
                        ReplayEnergySpentEvent(combatEvent);
                        break;
                    case "EnergyGained":
                        ReplayEnergyGainedEvent(combatEvent);
                        break;
                    case "TurnStarted":
                        if (combatantRegistry.IsLocalCombatant(combatEvent.Value<string>("combatantId")))
                            state.turnCount = Mathf.Max(1, state.turnCount + 1);
                        handler = DelaySeconds(0.05f);
                        break;
                    case "TurnEnded":
                        ApplyTurnEndedToTimeline(combatEvent);
                        handler = DelaySeconds(0.05f);
                        break;
                    case "CombatEnded":
                        RecordAnnouncedOutcome(combatEvent);
                        handler = DelaySeconds(0.1f);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[STS-COMBAT] Exception setting up event replay for eventType={eventType}: {ex}");
            }

            if (handler != null)
            {
                while (true)
                {
                    bool hasNext = false;
                    try
                    {
                        hasNext = handler.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[STS-COMBAT] Exception replaying eventType={eventType}: {ex}");
                        break;
                    }
                    if (!hasNext) break;
                    yield return handler.Current;
                }
            }
        }
    }

    private static IEnumerator DelaySeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    string ResolveCombatEventType(JToken combatEvent)
    {
        string explicitType = combatEvent.Value<string>("eventType");
        if (!string.IsNullOrWhiteSpace(explicitType))
            return explicitType;

        if (combatEvent["definitionId"] != null && combatEvent["cardInstanceId"] != null)
            return "CardPlayed";
        if (combatEvent["requestedDamage"] != null)
            return "DamageApplied";
        if (combatEvent["requestedHeal"] != null)
            return "HealApplied";
        if (combatEvent["requestedLoss"] != null)
            return "HpLost";
        if (combatEvent["requestedArmor"] != null)
            return "ArmorGained";
        if (combatEvent["armorLost"] != null)
            return "ArmorBroken";
        if (combatEvent["remainingEnergy"] != null && combatEvent["amount"] != null)
            return "EnergySpent";
        if (combatEvent["resultingEnergy"] != null && combatEvent["amount"] != null)
            return "EnergyGained";
        if (combatEvent["handIndex"] != null)
            return "CardDrawn";
        if (combatEvent["statusType"] != null)
        {
            if ((combatEvent.Value<bool?>("removed") ?? false) || (combatEvent.Value<bool?>("expired") ?? false))
                return "StatusRemoved";
            if (combatEvent["remainingDuration"] != null || combatEvent["newValue"] != null)
                return "StatusUpdated";
            return "StatusApplied";
        }
        if (combatEvent["fromPile"] != null || combatEvent["toPile"] != null)
            return "CardMoved";
        if (combatEvent["pile"] != null || combatEvent["drawSize"] != null)
            return "PileShuffled";
        if (combatEvent["resultingCardInstanceId"] != null)
            return "CardMerged";
        if (combatEvent["previousReadyAtTick"] != null)
            return "TurnEnded";
        if (combatEvent["readyAtTick"] != null)
            return "TurnStarted";
        if (combatEvent["winnerTeamId"] != null)
            return "CombatEnded";
        return string.Empty;
    }

    /// Le serveur vient de clore le combat et dit qui l'emporte. On le note ici plutôt que
    /// d'agir tout de suite : les événements qui suivent dans le même lot doivent finir de
    /// se jouer, et c'est ResolveCombatEndRoutine qui conclut, une fois les animations
    /// terminées.
    void RecordAnnouncedOutcome(JToken combatEvent)
    {
        string localTeamId = LocalTeamId();
        if (string.IsNullOrEmpty(localTeamId))
            return;

        // Absent du JSON quand le combat est nul : le serveur n'écrit pas de vainqueur.
        string winnerTeamId = combatEvent.Value<string>("winnerTeamId");

        switch (CombatOutcomeSource.FromWinner(winnerTeamId, localTeamId))
        {
            case CombatOutcome.Victory: announcedOutcome = TeamOutcome.Victory; break;
            case CombatOutcome.Defeat:  announcedOutcome = TeamOutcome.Defeat;  break;
            case CombatOutcome.Draw:    announcedOutcome = TeamOutcome.Draw;    break;
            default:                    announcedOutcome = TeamOutcome.None;    break;
        }
    }

    /// L'équipe du combattant local, telle que le registre l'a enregistrée.
    string LocalTeamId()
    {
        string localId = combatantRegistry.LocalCombatantId;
        if (string.IsNullOrEmpty(localId))
            return null;

        CombatantDescriptor descriptor = combatantRegistry.DescriptorOf(localId);
        return descriptor?.TeamId;
    }

    IEnumerator ReplayCardPlayedEvent(JToken combatEvent)
    {
        string actorId = combatEvent.Value<string>("actorId");
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");

        Character actor = ResolveCombatant(actorId);
        if (actor == null || string.IsNullOrWhiteSpace(cardInstanceId) || string.IsNullOrWhiteSpace(definitionId))
            yield break;

        CardInstance card = FindCardByInstanceId(cardInstanceId) ?? BuildCardFromDefinition(definitionId, cardInstanceId);
        if (card == null)
            yield break;

        yield return PresentCardPlayed(actor, card, ResolveCombatants(combatEvent["targetIds"]));
    }

    List<Character> ResolveCombatants(JToken combatantIdsToken)
    {
        var resolved = new List<Character>();
        if (combatantIdsToken is not JArray combatantIds)
            return resolved;

        foreach (JToken combatantId in combatantIds)
        {
            Character combatant = ResolveCombatant(combatantId?.Value<string>());
            if (combatant != null)
                resolved.Add(combatant);
        }
        return resolved;
    }

    IEnumerator PresentCardPlayed(Character actor, CardInstance card, List<Character> targets)
    {
        // PresentCardPlayed has a single caller, ReplayCardPlayedEvent, which knows the
        // actor by the id CardPlayed calls actorId — not combatantId. Rather than thread
        // it through the signature, ask the identity registry for the inverse: that is
        // what it is for.
        ICombatantPiles<CardInstance> actorPiles =
            combatantPiles.For(GetAuthoritativeCombatantId(actor));
        bool burns = BurnsOnPlay(card);
        if (actorPiles != null)
        {
            AuthoritativeCombatStateReducer.MoveCard(
                actorPiles.Pile(PileKind.Hand) as List<CardInstance>,
                actorPiles.Pile(burns ? PileKind.Exhaust : PileKind.Discard) as List<CardInstance>,
                card);
        }

        CardView playedView = actor.isPlayer ? ui.GetView(card) : null;
        if (playedView == null)
        {
            Transform sourceView = ui.GetView(actor);
            playedView = ui.CreateCardView(card, false, sourceView != null ? (Vector3?)sourceView.position : null);
        }
        else
        {
            // Discard/exhaust already do this the moment a card leaves the hand; a played card
            // never did, so it kept being counted (and re-arranged) by every RefreshHandLayout()
            // call that fired from any other event replayed during the same play (energy, damage,
            // status, ...) while it was still mid-flight to the center of the table.
            ui.RemoveView(playedView);
        }

        if (playedView == null)
            yield break;

        DropZone actorZone = ui.GetDropZone(actor);
        actorZone?.PlayActionSprite(DropZone.ActionSpriteVariant(card));

        // The server resolves a whole AI turn chain in one round-trip and streams every event
        // back-to-back; without this pause enemy actions replay with no perceptible gap between
        // them, unlike the old local EnemyTurn coroutine which paused 0.2s before/after each move.
        if (!actor.isPlayer)
            yield return new WaitForSeconds(0.2f);

        yield return ui.AnimateCardToCenter(playedView);
        playedView.Flash();
            PlayCardEffectFeedback(actor, targets, card);

        if (actor.isPlayer)
        {
            // The card leaves the centre while its effects land, which is what the local combat
            // path does and says in as many words: effects begin exactly when the card starts
            // leaving the center. Waiting for that exit instead put the whole of it in front of
            // every hit — 0.4s of travel plus the read pause — and that wait, not the round trip,
            // is what made an attack feel late.
            StartCoroutine(ui.AnimateCardToDiscard(playedView, burns, actor));
            yield break;
        }

        yield return new WaitForSeconds(0.08f);
        yield return ui.AnimateCardToDiscard(playedView, burns, actor);
        yield return new WaitForSeconds(0.2f);
    }

    // The server burns a card tagged Exhaust instead of discarding it (CombatEngine.afterPlaying)
    // and emits no CardMoved saying so, so the animation is the only place that fact can show.
    static bool BurnsOnPlay(CardInstance card)
    {
        return card != null
            && card.data != null
            && card.data.type != CardType.Pouvoir
            && card.data.HasTag(CardTag.Exhaust);
    }

    // The authoritative replay path only ever animated card movement and popped up numbers;
    // it never played the per-effect SFX/VFX the local (non-authoritative) flow already has.
    // Takes the targets already resolved rather than the event they came from: a play is now
    // shown when it is submitted, before any event exists, and it deserves the same feedback.
    void PlayCardEffectFeedback(Character source, List<Character> targets, CardInstance card)
    {
        List<EffectEntry> effects = card.GetEffects();
        if (effects == null || effects.Count == 0)
        {
            Debug.Log($"[STS-VFX] no effects found for card {card?.displayName ?? "<null>"}");
            return;
        }

        targets ??= new List<Character>();

        // Conditions are read per target, the way the local path reads them: whether an effect
        // fires can differ from one target to the next.
        EffectContext ctx = new EffectContext
        {
            source = source,
            target = null,
            combat = this,
            state = state,
            card = card,
            timeline = turnSystem != null ? turnSystem.timeline : null,
            targets = targets
        };

        foreach (EffectEntry effect in effects)
        {
            string effectName = effect.GetEffectName();
            Debug.Log($"[STS-VFX] card={card?.displayName ?? "<null>"} effect={effect.type} sfx={effectName} targets={targets.Count}");

            List<Character> effectTargets;
            if (effect.targetSelf)
            {
                effectTargets = source != null ? new List<Character> { source } : new List<Character>();
            }
            else if (effect.targetOthers)
            {
                effectTargets = LivingOpponentsOf(source)
                    .Where(other => !targets.Contains(other))
                    .ToList();
            }
            else
            {
                effectTargets = targets;
            }

            if (effectTargets.Count == 0)
            {
                ctx.target = null;
                if (EffectFires(effect, ctx))
                    SFXManager.Instance?.PlaySound(effectName);
                continue;
            }

            bool sounded = false;
            foreach (Character target in effectTargets)
            {
                ctx.target = target;
                if (!EffectFires(effect, ctx))
                    continue;

                if (!sounded)
                {
                    SFXManager.Instance?.PlaySound(effectName);
                    sounded = true;
                }

                Transform targetView = ui.GetView(target);
                if (targetView != null)
                    VFXManager.Instance?.PlayEffect(effect, targetView.position);
            }
        }
    }

    static bool EffectFires(EffectEntry effect, EffectContext ctx)
    {
        return !effect.conditional
            || EffectResolver.VerifyCondition(effect.conditionType, effect.conditionValue, ctx);
    }

    IEnumerator ReplayCardDrawnEvent(JToken combatEvent)
    {
        string combatantId = combatEvent.Value<string>("combatantId");
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");

        CardInstance card = FindCardByInstanceId(cardInstanceId)
            ?? BuildCardFromDefinition(definitionId, cardInstanceId);
        if (card == null || ui == null)
            yield break;

        ICombatantPiles<CardInstance> drawPiles = combatantPiles.For(combatantId);
        if (drawPiles == null)
            yield break;

        // Only the piles the card is leaving, never the hand. A card already in hand keeps
        // its position — and with it its slot on screen. Pulling it out to re-insert it at
        // the server's index would reshuffle the player's hand under their cursor, and
        // restart its animation, for a card that never moved.
        drawPiles.Pile(PileKind.Draw)?.Remove(card);
        drawPiles.Pile(PileKind.Discard)?.Remove(card);
        drawPiles.Pile(PileKind.Exhaust)?.Remove(card);

        List<CardInstance> hand = drawPiles.Pile(PileKind.Hand) as List<CardInstance>;
        if (hand != null && !hand.Contains(card))
        {
            int handIndex = combatEvent.Value<int?>("handIndex") ?? -1;
            InsertCardAt(hand, card, handIndex);
        }

        ui.DrawCardAnimated(card);
        yield return new WaitForSeconds(0.12f);
    }

    void ReplayCardMergedEvent(JToken combatEvent)
    {
        string combatantId = combatEvent.Value<string>("combatantId");
        string resultingInstanceId = combatEvent.Value<string>("resultingCardInstanceId");
        string resultingDefinitionId = combatEvent.Value<string>("resultingDefinitionId");
        if (string.IsNullOrWhiteSpace(combatantId)
            || string.IsNullOrWhiteSpace(resultingInstanceId)
            || string.IsNullOrWhiteSpace(resultingDefinitionId))
            return;

        ICombatantPiles<CardInstance> piles = combatantPiles.For(combatantId);
        if (piles == null)
            return;

        foreach (JToken selectedId in combatEvent["mergedCardInstanceIds"] as JArray ?? new JArray())
        {
            string instanceId = selectedId?.ToString();
            foreach (PileKind pileKind in new[] { PileKind.Hand, PileKind.Draw, PileKind.Discard, PileKind.Exhaust })
                GetPileByName(combatantId, pileKind.ToString())?.RemoveAll(card => card != null && card.instanceId == instanceId);
        }

        CardInstance merged = BuildCardFromDefinition(resultingDefinitionId, resultingInstanceId);
        List<CardInstance> hand = GetPileByName(combatantId, PileKind.Hand.ToString());
        if (merged == null || hand == null)
            return;

        hand.Add(merged);
        if (piles.IsFullyVisible)
            ui?.SyncHandFromDeckStateIfDrifted();
    }

    IEnumerator ReplayCardMovedEvent(JToken combatEvent)
    {
        string combatantId = combatEvent.Value<string>("combatantId");
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");
        string fromPile = combatEvent.Value<string>("fromPile");
        string toPile = combatEvent.Value<string>("toPile");

        // Parsed once so the animation branches below match on the closed vocabulary
        // instead of on the raw string, which ResolvePileName used to upper-case for
        // them. A name outside the vocabulary now reaches no branch at all.
        PileKind? fromKind = PileKinds.Parse(fromPile);
        PileKind? toKind = PileKinds.Parse(toPile);

        ICombatantPiles<CardInstance> piles = combatantPiles.For(combatantId);
        if (piles == null)
            yield break;

        CardInstance card = FindCardByInstanceId(cardInstanceId)
            ?? BuildCardFromDefinition(definitionId, cardInstanceId);
        if (card == null)
            yield break;

        List<CardInstance> fromList = GetPileByName(combatantId, fromPile);
        List<CardInstance> toList = GetPileByName(combatantId, toPile);
        if (fromList != null)
        {
            fromList.Remove(card);
        }
        else
        {
            piles.RemoveEverywhere(card);
        }

        if (toList != null && !toList.Contains(card))
        {
            int targetIndex = combatEvent.Value<int?>("destinationIndex")
                ?? -1;
            InsertCardAt(toList, card, targetIndex);
        }

        if (ui == null)
        {
            yield return null;
            yield break;
        }

        Character movementActor = ResolveCombatant(combatantId);

        if (toKind == PileKind.Exhaust)
        {
            if (ui.GetView(card) != null)
            {
                ui.ExhaustCardAnimated(card);
            }
            else
            {
                yield return ui.AnimateCardToPile(card, CardSelectionSource.ExhaustPile, movementActor);
            }
            yield return new WaitForSeconds(0.12f);
            yield break;
        }

        if (toKind == PileKind.Discard)
        {
            if (ui.GetView(card) != null)
            {
                ui.DiscardCardAnimated(card);
            }
            else
            {
                yield return ui.AnimateCardToPile(card, CardSelectionSource.DiscardPile, movementActor);
            }
            yield return new WaitForSeconds(0.10f);
            yield break;
        }

        if (toKind == PileKind.Hand)
        {
            if (fromKind == PileKind.Draw)
            {
                ui.DrawCardAnimated(card);
            }
            else
            {
                ui.AddCardAnimated(card);
            }
            yield return new WaitForSeconds(0.12f);
            yield break;
        }

        if (toKind == PileKind.Draw)
        {
            yield return ui.AnimateCardToPile(card, CardSelectionSource.DrawPile);
        }
    }

    IEnumerator ReplayPileShuffledEvent(JToken combatEvent)
    {
        // The server shuffled and told us the order it got. Shuffling again locally would produce
        // a different one, and every draw after it would disagree with the server about which
        // card came up.
        string combatantId = combatEvent.Value<string>("combatantId");
        List<CardInstance> pile = GetPileByName(combatantId, combatEvent.Value<string>("pile"));
        if (pile == null)
            yield break;

        JToken orderToken = combatEvent["cardInstanceIds"];
        if (orderToken != null && orderToken.Type == JTokenType.Array)
        {
            List<CardInstance> reordered = new List<CardInstance>();
            foreach (JToken idToken in orderToken)
            {
                string instanceId = idToken?.ToString();
                if (string.IsNullOrWhiteSpace(instanceId))
                    continue;

                CardInstance card = pile.FirstOrDefault(candidate =>
                        candidate != null
                        && string.Equals(candidate.instanceId, instanceId, StringComparison.Ordinal))
                    ?? FindCardByInstanceId(instanceId);
                if (card == null || reordered.Contains(card))
                    continue;

                // A reshuffle names the cards it took from another pile — the discard, which the
                // client still holds them in. They have to leave it, or the same card ends up in
                // two piles at once.
                if (!pile.Contains(card))
                {
                    combatantPiles.For(combatantId)?.RemoveEverywhere(card);
                }

                reordered.Add(card);
            }

            // Anything the server did not name stays where it was, under what it did name.
            foreach (CardInstance card in pile)
            {
                if (card != null && !reordered.Contains(card))
                {
                    reordered.Add(card);
                }
            }

            pile.Clear();
            pile.AddRange(reordered);
        }
        yield return new WaitForSeconds(0.05f);
    }

    IEnumerator ReplayStatusAppliedEvent(JToken combatEvent)
    {
        Character target = ResolveStatusTarget(combatEvent);
        if (target == null || !TryResolveStatusType(combatEvent, out StatusType statusType))
            yield break;

        int value = combatEvent.Value<int?>("value")
            ?? combatEvent.Value<int?>("potency")
            ?? 1;
        int duration = combatEvent.Value<int?>("duration")
            ?? combatEvent.Value<int?>("remainingDuration")
            ?? -1;
        string cardId = combatEvent.Value<string>("cardId")
            ?? string.Empty;
        int index = combatEvent.Value<int?>("index") ?? 0;

        StatusEffect status = StatusEffect.Factory(statusType, value, duration, cardId, index);
        if (status == null)
            yield break;

        status.Value = value;
        status.Duration = duration;
        status.statusType = statusType;
        status.cardID = cardId;
        status.index = index;

        status.InsertInto(target.statusEffects);
        status.OnApply(target);

        ui?.RefreshUI(false);
        yield return FlashCombatantWhite(target);
    }

    void ReplayStatusRemovedEvent(JToken combatEvent)
    {
        Character target = ResolveStatusTarget(combatEvent);
        if (target == null)
            return;

        List<StatusEffect> toRemove = ResolveMatchingStatuses(target, combatEvent);
        foreach (StatusEffect status in toRemove)
        {
            if (status == null)
                continue;

            status.OnExpire(target);
            target.statusEffects.Remove(status);
        }

        ui?.RefreshUI(false);
    }

    void ReplayStatusUpdatedEvent(JToken combatEvent)
    {
        Character target = ResolveStatusTarget(combatEvent);
        if (target == null)
            return;

        List<StatusEffect> matches = ResolveMatchingStatuses(target, combatEvent);
        if (matches.Count == 0)
            return;

        int? newValue = combatEvent.Value<int?>("newValue")
            ?? combatEvent.Value<int?>("value")
            ?? combatEvent.Value<int?>("potency");
        int? newDuration = combatEvent.Value<int?>("remainingDuration")
            ?? combatEvent.Value<int?>("duration");
        bool shouldRemove = (combatEvent.Value<bool?>("removed") ?? false)
            || (combatEvent.Value<bool?>("expired") ?? false)
            || (newDuration.HasValue && newDuration.Value == 0);

        foreach (StatusEffect status in matches.ToList())
        {
            if (status == null)
                continue;

            if (shouldRemove)
            {
                status.OnExpire(target);
                target.statusEffects.Remove(status);
                continue;
            }

            if (newValue.HasValue)
            {
                status.Value = newValue.Value;
            }

            if (newDuration.HasValue)
            {
                status.Duration = newDuration.Value;
            }
        }

        ui?.RefreshUI(false);
    }

    void ReplayDamageAppliedEvent(JToken combatEvent)
    {
        Character target = ResolveCombatant(combatEvent.Value<string>("targetId"));
        if (target == null || ui == null)
            return;

        AuthoritativeDamageState state = AuthoritativeCombatStateReducer.ResolveDamage(
            target.currentHP,
            target.armor,
            combatEvent.Value<int?>("remainingHp"),
            combatEvent.Value<int?>("remainingArmor"));
        target.currentHP = state.Hp;
        target.armor = state.Armor;

        int hpLost = combatEvent.Value<int?>("hpLost") ?? 0;
        int requestedDamage = combatEvent.Value<int?>("requestedDamage") ?? hpLost;
        bool blocked = hpLost <= 0 && requestedDamage > 0;
        int popupAmount = blocked ? requestedDamage : hpLost;
        if (popupAmount > 0)
        {
            ui.ShowDamagePopup(target, popupAmount, false, blocked);
        }
        ui.RefreshUI(false);
    }

    void ReplayEnergySpentEvent(JToken combatEvent)
    {
        string combatantId = combatEvent.Value<string>("combatantId");
        Character target = ResolveCombatant(combatantId);
        if (target == null)
            return;

        target.resources.energy = combatEvent.Value<int?>("remainingEnergy") ?? target.resources.energy;
        ui?.RefreshUI(false);
    }

    void ReplayEnergyGainedEvent(JToken combatEvent)
    {
        string combatantId = combatEvent.Value<string>("combatantId");
        Character target = ResolveCombatant(combatantId);
        if (target == null)
            return;

        target.resources.energy = combatEvent.Value<int?>("resultingEnergy") ?? target.resources.energy;
        ui?.RefreshUI(false);
    }

    void ReplayHealAppliedEvent(JToken combatEvent)
    {
        Character target = ResolveCombatant(combatEvent.Value<string>("targetId"));
        if (target == null || ui == null)
            return;

        target.currentHP = combatEvent.Value<int?>("remainingHp") ?? target.currentHP;
        int actualHeal = combatEvent.Value<int?>("actualHeal") ?? 0;
        if (actualHeal > 0)
            ui.ShowDamagePopup(target, actualHeal, healing: true);
        ui.RefreshUI(false);
    }

    void ReplayHpLostEvent(JToken combatEvent)
    {
        Character target = ResolveCombatant(combatEvent.Value<string>("targetId"));
        if (target == null || ui == null)
            return;

        target.currentHP = combatEvent.Value<int?>("remainingHp") ?? target.currentHP;
        int actualLoss = combatEvent.Value<int?>("actualLoss") ?? 0;
        if (actualLoss > 0)
            ui.ShowDamagePopup(target, actualLoss, healing: false, blocked: false);
        ui.RefreshUI(false);
    }

    void ReplayArmorBrokenEvent(JToken combatEvent)
    {
        Character target = ResolveCombatant(combatEvent.Value<string>("targetId"));
        if (target == null)
            return;

        int armorLost = combatEvent.Value<int?>("armorLost") ?? 0;
        target.armor = Mathf.Max(0, target.armor - armorLost);
        ui?.RefreshUI(false);
    }

    IEnumerator FlashCombatantWhite(Character target)
    {
        if (target == null || ui == null)
            yield break;

        DropZone zone = ui.GetDropZone(target);
        if (zone == null)
            yield break;

        yield return zone.FlashWhite();
    }

    // Unlike every other stat event, ArmorGained previously only played the flash and never
    // actually applied the value to the character, so gained armor was invisible on the UI.
    IEnumerator ReplayArmorGainedEvent(JToken combatEvent)
    {
        Character target = ResolveCombatant(combatEvent.Value<string>("targetId"));
        if (target == null || ui == null)
            yield break;

        target.armor = combatEvent.Value<int?>("resultingArmor") ?? target.armor;
        yield return FlashCombatantWhite(target);
        ui.RefreshUI(false);
    }

    CardInstance FindCardByInstanceId(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || deck == null)
            return null;

        foreach (CardInstance card in deck.hand)
        {
            if (card != null && string.Equals(card.instanceId, instanceId, StringComparison.Ordinal))
                return card;
        }
        foreach (CardInstance card in deck.drawPile)
        {
            if (card != null && string.Equals(card.instanceId, instanceId, StringComparison.Ordinal))
                return card;
        }
        foreach (CardInstance card in deck.discardPile)
        {
            if (card != null && string.Equals(card.instanceId, instanceId, StringComparison.Ordinal))
                return card;
        }
        foreach (CardInstance card in deck.exhaustPile)
        {
            if (card != null && string.Equals(card.instanceId, instanceId, StringComparison.Ordinal))
                return card;
        }
        return null;
    }

    CardInstance BuildCardFromDefinition(string definitionId, string instanceId)
    {
        // Enemy move cards are runtime-generated server-side with IDs like "enemy-move:{enemyId}:{index}";
        // they live outside the card database, so resolve them from local enemy data instead of
        // logging a bogus "Card not found!" error first.
        if (definitionId != null && definitionId.StartsWith("enemy-move:", StringComparison.Ordinal))
        {
            string[] parts = definitionId.Split(':');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int moveIndex))
            {
                string enemyId = parts[1];
                EnemyData enemyData = EnemyDataDatabase.Get(enemyId);
                if (enemyData == null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy is Enemy e && e.data != null && e.data.id == enemyId)
                        {
                            enemyData = e.data;
                            break;
                        }
                    }
                }
                if (enemyData != null && moveIndex >= 0 && moveIndex < enemyData.ActionCount)
                {
                    EnemyMoveEntry move = enemyData.GetActionAt(moveIndex);
                    if (move != null)
                    {
                        STSCardData runtimeCard = move.CreateRuntimeCard(enemyId);
                        if (runtimeCard != null)
                        {
                            return new CardInstance(runtimeCard)
                            {
                                instanceId = instanceId
                            };
                        }
                    }
                }
            }
        }

        STSCardData data = STSCardDatabase.Get(definitionId);
        if (data != null)
        {
            return new CardInstance(data)
            {
                instanceId = instanceId
            };
        }

        Debug.LogWarning($"[STS-COMBAT] Could not build card from definition '{definitionId}'.");
        return null;
    }

    Character ResolveStatusTarget(JToken combatEvent)
    {
        string targetId = combatEvent.Value<string>("targetId")
            ?? combatEvent.Value<string>("combatantId")
            ?? combatEvent.Value<string>("ownerId");
        return ResolveCombatant(targetId);
    }

    void ApplyAuthoritativeStatuses(Character target, JToken statusesToken)
    {
        if (target == null || statusesToken == null)
            return;

        var beforeStatuses = target.statusEffects
            .Where(status => status != null)
            .Select(status => (type: status.statusType, cardId: status.cardID ?? string.Empty, index: status.index, value: status.Value, duration: status.Duration))
            .ToList();

        IReadOnlyList<AuthoritativeStatusState> authoritativeStatuses =
            AuthoritativeCombatStateReducer.ReadStatuses(statusesToken);
        var retained = new HashSet<StatusEffect>();

        foreach (AuthoritativeStatusState stateValue in authoritativeStatuses)
        {
            var statusToken = new JObject
            {
                ["statusType"] = stateValue.StatusType,
                ["cardId"] = stateValue.CardId,
                ["index"] = stateValue.Index
            };
            if (!TryResolveStatusType(statusToken, out StatusType statusType))
                continue;

            StatusEffect status = target.statusEffects.FirstOrDefault(candidate =>
                candidate != null
                && candidate.statusType == statusType
                && candidate.index == stateValue.Index
                && string.Equals(candidate.cardID ?? string.Empty, stateValue.CardId, StringComparison.OrdinalIgnoreCase));

            if (status == null)
            {
                status = StatusEffect.Factory(
                    statusType,
                    stateValue.Value,
                    stateValue.Duration,
                    stateValue.CardId,
                    stateValue.Index);
                if (status == null)
                    continue;
                target.statusEffects.Add(status);
            }

            status.statusType = statusType;
            status.Duration = stateValue.Duration;
            status.cardID = stateValue.CardId;
            status.index = stateValue.Index;
            // These count cards (or turns) towards a threshold rather than holding a plain
            // value, so what the player sees is how many are still needed, not the threshold
            // itself — the server sends that progress apart from the threshold.
            status.Value = IsFollowUpStatusType(statusType)
                ? Mathf.Max(0, Mathf.Max(1, stateValue.Value) - stateValue.Progress)
                : stateValue.Value;
            retained.Add(status);
        }

        target.statusEffects.RemoveAll(status => status == null || !retained.Contains(status));

        // Status effects in authoritative mode are just synced snapshots now — the old client-side
        // tick hooks (OnTurnEnd/OnDamageTaken/etc.) never run, so the only place feedback can come
        // from is detecting changes in this snapshot: appearing/disappearing/changing value.
        PlayStatusChangeFeedback(target, beforeStatuses);
    }

    static bool IsFollowUpStatusType(StatusType statusType)
    {
        return statusType == StatusType.CardFollowUp
            || statusType == StatusType.AnyCardFollowUp
            || statusType == StatusType.FieldTurnFollowUp;
    }

    // Authoritative statuses arrive as full snapshots, so "trigger" feedback has to be inferred
    // from the diff: a status that just appeared/changed is what used to fire the per-status
    // tick hooks (Burn/Thorns/Trap/Continuous/Sadism/MechaArm) that carry the SFX/VFX calls.
    // Skip the very first sync of a given status entirely, otherwise entering combat would
    // play every starting-status's trigger effect at once even though none of them have fired yet.
    void PlayStatusChangeFeedback(Character target, List<(StatusType type, string cardId, int index, int value, int duration)> beforeStatuses)
    {
        if (target == null || ui == null)
            return;

        // First-ever sync for this combatant has no "before" to diff against; everything would
        // count as new. Don't play any feedback on the very first application.
        if (beforeStatuses.Count == 0)
            return;

        foreach (StatusEffect status in target.statusEffects)
        {
            if (status == null)
                continue;

            var previous = beforeStatuses.FirstOrDefault(s =>
                s.type == status.statusType
                && string.Equals(s.cardId, status.cardID ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && s.index == status.index);

            bool isNew = !beforeStatuses.Any(s =>
                s.type == status.statusType
                && string.Equals(s.cardId, status.cardID ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && s.index == status.index);

            bool valueChanged = !isNew && previous.value != status.Value;

            if (!isNew && !valueChanged)
                continue;

            string feedbackName = status.statusType switch
            {
                StatusType.Burn => "DamageFire",
                StatusType.Thorns => "Thorns",
                StatusType.Trap => "DamageExplosionSmall",
                StatusType.Continuous => "Continuous",
                StatusType.Sadism => "Sadism",
                StatusType.MechaArm => "DamageMagic",
                _ => null
            };

            if (string.IsNullOrEmpty(feedbackName))
                continue;

            Transform targetView = ui.GetView(target);
            if (targetView == null)
                continue;

            SFXManager.Instance?.PlaySound(feedbackName);
            VFXManager.Instance?.PlayEffect(feedbackName, targetView.position);
        }
    }

    bool TryResolveStatusType(JToken combatEvent, out StatusType statusType)
    {
        statusType = default;

        string raw = combatEvent.Value<string>("statusType")
            ?? combatEvent.Value<string>("status");
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (Enum.TryParse(raw, true, out statusType))
            return true;

        string normalized = NormalizeStatusTypeToken(raw);
        foreach (StatusType candidate in Enum.GetValues(typeof(StatusType)))
        {
            if (string.Equals(NormalizeStatusTypeToken(candidate.ToString()), normalized, StringComparison.Ordinal))
            {
                statusType = candidate;
                return true;
            }
        }

        return false;
    }

    List<StatusEffect> ResolveMatchingStatuses(Character target, JToken combatEvent)
    {
        if (target == null)
            return new List<StatusEffect>();

        int? index = combatEvent.Value<int?>("index");
        string cardId = combatEvent.Value<string>("cardId");

        if (TryResolveStatusType(combatEvent, out StatusType statusType))
        {
            return target.statusEffects
                .Where(s => s != null
                    && s.statusType == statusType
                    && (!index.HasValue || s.index == index.Value)
                    && (string.IsNullOrWhiteSpace(cardId) || string.Equals(s.cardID, cardId, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        string rawName = combatEvent.Value<string>("status");
        if (string.IsNullOrWhiteSpace(rawName))
            return new List<StatusEffect>();

        string normalizedName = NormalizeStatusTypeToken(rawName);
        return target.statusEffects
            .Where(s => s != null
                && (string.Equals(NormalizeStatusTypeToken(s.Name), normalizedName, StringComparison.Ordinal)
                    || string.Equals(NormalizeStatusTypeToken(s.GetType().Name.Replace("Status", string.Empty)), normalizedName, StringComparison.Ordinal)))
            .ToList();
    }

    string NormalizeStatusTypeToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var chars = raw.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToUpperInvariant();
    }

    /// <summary>
    /// The named pile of the named combatant, or null when either is unknown to us.
    /// Null is a refusal to guess: the caller must skip, not fall back on the local
    /// deck. Cf. spec §3.4 entries 6 and 9.
    /// </summary>
    List<CardInstance> GetPileByName(string combatantId, string pileName)
    {
        PileKind? kind = PileKinds.Parse(pileName);
        if (kind == null)
            return null;

        return combatantPiles.Pile(combatantId, kind.Value) as List<CardInstance>;
    }

    void InsertCardAt(List<CardInstance> pile, CardInstance card, int index)
    {
        if (pile == null || card == null)
            return;

        if (index < 0 || index > pile.Count)
        {
            pile.Add(card);
            return;
        }

        pile.Insert(index, card);
    }

    Character ResolveCombatant(string combatantId)
    {
        return combatantRegistry.Resolve(combatantId);
    }

    /// Les vivants de l'équipe adverse à `character`, vus par le registre.
    ///
    /// Sur le chemin local le registre est vide : on retombe sur les listes positionnelles,
    /// qui restent la vérité du tutoriel.
    List<Character> LivingOpponentsOf(Character character)
    {
        string id = combatantRegistry.IdOf(character);
        if (id == null)
            return character != null && character.isPlayer
                ? enemies.Where(e => e != null && e.IsAlive).ToList()
                : allies.Where(a => a != null && a.IsAlive).Cast<Character>().ToList();

        string team = combatantRegistry.DescriptorOf(id)?.TeamId;
        if (string.IsNullOrEmpty(team))
            return new List<Character>();

        return AllRegistered()
            .Where(other => other != null && other.IsAlive)
            .Where(other => !string.Equals(TeamOf(other), team, StringComparison.Ordinal))
            .ToList();
    }

    string TeamOf(Character character)
    {
        string id = combatantRegistry.IdOf(character);
        return id == null ? null : combatantRegistry.DescriptorOf(id)?.TeamId;
    }

    /// Tout le monde, des deux côtés, dans l'ordre où le registre les tient.
    List<Character> AllRegistered()
    {
        var everyone = new List<Character>();
        everyone.AddRange(combatantRegistry.Allies());
        everyone.AddRange(combatantRegistry.Opponents());
        return everyone;
    }

    /// <summary>
    /// Records whose piles are whose for this state. The local combatant keeps the
    /// DeckManager — it owns the hand UI and the animations — while anyone else is
    /// held as the server projects them: counts for draw and hand, cards for the two
    /// public piles.
    /// </summary>
    void RegisterCombatantPiles(string combatantId, Character combatant, JToken combatantToken)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            return;

        if (combatant != null && combatant == player && deck != null)
        {
            combatantPiles.Set(combatantId, new LocalPiles(deck));
            return;
        }

        JToken hidden = combatantToken["hiddenPiles"];
        if (hidden != null && hidden.Type == JTokenType.Object)
        {
            combatantPiles.Set(combatantId, new RemotePiles<CardInstance>(
                hidden.Value<int?>("drawCount") ?? 0,
                hidden.Value<int?>("handCount") ?? 0,
                ReadPileCards(hidden["discard"]),
                ReadPileCards(hidden["exhaust"])));
        }
    }

    /// <summary>
    /// Builds the card objects of a pile we are allowed to see. A card the catalogue
    /// does not know is skipped rather than replaced by a blank, so a gap stays
    /// visible instead of becoming a plausible card.
    /// </summary>
    List<CardInstance> ReadPileCards(JToken pileToken)
    {
        var cards = new List<CardInstance>();
        if (!(pileToken is JArray pile))
            return cards;

        foreach (JToken cardToken in pile)
        {
            string instanceId = cardToken.Value<string>("instanceId")
                ?? cardToken.Value<string>("cardInstanceId");
            string definitionId = cardToken.Value<string>("definitionId");

            CardInstance card = FindCardByInstanceId(instanceId)
                ?? BuildCardFromDefinition(definitionId, instanceId);
            if (card != null)
                cards.Add(card);
        }
        return cards;
    }

    /// <summary>
    /// Ties the server's ids to the scene's Character objects, once and for all. The
    /// enemy order comes from activeEncounter.enemyIds, the very list the server draws
    /// its enemy-{index} from, so the two agree at construction time — and never need
    /// to agree again afterwards.
    /// </summary>
    void BuildCombatantRegistry(JToken combatToken)
    {
        combatantRegistry.Clear();
        combatantRegistryBuilt = true;

        // "player" reste la convention PvE ; en duel c'est un UUID d'utilisateur, et le
        // résolveur retombe sur la propriété du protocole — celui qui montre ses cartes
        // est celui qui regarde — quand l'identifiant proposé n'est pas dans l'état.
        string localCombatantId = LocalCombatantResolver.Resolve(
            combatToken,
            Mode == CombatMode.Pvp
                ? (RunManager.Instance != null ? RunManager.Instance.pvpLocalUserId : null)
                : "player");

        if (string.IsNullOrEmpty(localCombatantId))
        {
            Debug.LogError("[STS-COMBAT] No local combatant could be identified in this state; "
                + "teams, targeting and the end-turn button will all be inert.");

            // Décision D3 : en duel on ne laisse pas le joueur devant un combat inerte —
            // ni équipes, ni ciblage, ni bouton de fin de tour, et rien pour le lui dire.
            // On l'en sort. Le coût est connu : le serveur comptera l'abandon comme un
            // forfait au bout des trente secondes du tour.
            if (Mode == CombatMode.Pvp)
            {
                LeavePvpBattle("Ce duel n'a pas pu être rejoint : combattant introuvable.");
            }
        }

        IReadOnlyList<CombatantDescriptor> descriptors =
            CombatantSnapshotReader.ReadCombatants(combatToken, localCombatantId);

        foreach (CombatantDescriptor descriptor in descriptors)
        {
            Character combatant = ResolveCombatantByConvention(descriptor.CombatantId);
            if (combatant != null)
                combatantRegistry.Register(descriptor, combatant);
            else
                Debug.LogWarning(
                    $"[STS-COMBAT] No Character for combatant {descriptor.CombatantId}");
        }
    }

    /// <summary>
    /// The positional convention, used only while building the registry — that is,
    /// before any death has had a chance to shift anything.
    /// </summary>
    Character ResolveCombatantByConvention(string combatantId)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            return null;

        // En duel, l'identifiant est celui de l'utilisateur : on le retrouve sur le
        // Character que le montage de scène a étiqueté avec, plutôt que sur une position.
        // Si le serveur nommait ses combattants autrement, c'est ici — et nulle part
        // ailleurs — que la correspondance se referait.
        foreach (Player ally in allies)
        {
            if (ally != null && string.Equals(ally.playerUserId, combatantId, StringComparison.Ordinal))
                return ally;
        }
        foreach (Character enemy in enemies)
        {
            if (enemy != null && string.Equals(enemy.playerUserId, combatantId, StringComparison.Ordinal))
                return enemy;
        }

        if (string.Equals(combatantId, "player", StringComparison.Ordinal))
            return player;

        if (combatantId.StartsWith("player-", StringComparison.Ordinal)
            && int.TryParse(combatantId.Substring("player-".Length), out int allyIndex)
            && allyIndex >= 0
            && allyIndex < allies.Count)
        {
            return allies[allyIndex];
        }

        if (combatantId.StartsWith("enemy-", StringComparison.Ordinal)
            && int.TryParse(combatantId.Substring("enemy-".Length), out int enemyIndex)
            && enemyIndex >= 0
            && enemyIndex < enemies.Count)
        {
            return enemies[enemyIndex];
        }

        return null;
    }

    void ApplyAuthoritativePlayerPiles(JToken pilesToken)
    {
        if (pilesToken == null || pilesToken.Type != JTokenType.Object || deck == null)
            return;

        deck.drawPile = ParseAuthoritativeCardList(pilesToken["draw"]);
        deck.hand = ParseAuthoritativeCardList(pilesToken["hand"]);
        deck.discardPile = ParseAuthoritativeCardList(pilesToken["discard"]);
        deck.exhaustPile = ParseAuthoritativeCardList(pilesToken["exhaust"]);
    }

    List<CardInstance> ParseAuthoritativeCardList(JToken cardsToken)
    {
        var cards = new List<CardInstance>();
        if (cardsToken == null || cardsToken.Type != JTokenType.Array)
            return cards;

        foreach (JToken cardToken in cardsToken)
        {
            if (cardToken == null || cardToken.Type != JTokenType.Object)
                continue;

            string definitionId = cardToken.Value<string>("definitionId");
            string instanceId = cardToken.Value<string>("instanceId");
            if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(instanceId))
                continue;

            // Reuse existing card instances by instanceId so card views (which use reference equality) survive state syncs.
            CardInstance existing = FindCardByInstanceId(instanceId);
            if (existing != null && existing.data != null && string.Equals(existing.data.id, definitionId, StringComparison.Ordinal))
            {
                cards.Add(existing);
                continue;
            }

            STSCardData data = STSCardDatabase.Get(definitionId);
            if (data == null)
                continue;

            var card = new CardInstance(data)
            {
                instanceId = instanceId
            };
            cards.Add(card);
        }

        return cards;
    }

    IEnumerator PlayCardRoutine(Character source, CardInstance card, List<Character> targets, bool ignoreEnergy = false, bool createView = false)
    {
        activeCardPlays++; // Mark this request as running immediately.
        queuedCardPlays = Mathf.Max(0, queuedCardPlays - 1); // Request has started.

        try
        {
        
        EffectContext ctxSelf=new EffectContext
            {
                source = source,
                target = source,
                combat = this,
                state = state,
                card = card,
                timeline = turnSystem.timeline,
                targets=targets
            };
        EffectContext ctxTarget = new EffectContext
            {
                source = source,
                target = null,
                combat = this,
                state = state,
                card = card,
                timeline = turnSystem.timeline,
                targets = targets
            };

        int resolvedCost = card.Cost(ctxTarget);

        if (source==null||source.resources.energy < resolvedCost&&source.isPlayer&&!ignoreEnergy)
        {
            ui.StartCoroutine(ui.EnergyTextGlowRed());
            yield break;
        }
        CardView playedView = null;

        int replayCount=BattleCalculator.GetModifiedValue(1, StatType.ReplayCount, ctxSelf);
        if (card.data.xCost)
        {
            replayCount=replayCount*source.resources.energy;
        }

        if (source != null && source.isPlayer)
        {
            if (createView)
            {
                Transform sourceView = ui.GetView(source);
                playedView = ui.CreateCardView(
                    card,
                    false,
                    sourceView != null ? (Vector3?)sourceView.position : null
                );
            }
            else
            {
                playedView = ui.GetView(card);
            }
            deck.RemoveFromHand(card);

            if (playedView != null)
            {
                if (createView)
                {
                    // Already seeded in CreateCardView.
                }
                else
                {
                    ui.RemoveView(playedView);
                }

                yield return ui.AnimateCardToCenter(playedView);
            }
            if (!ignoreEnergy)
            {
                source.SpendEnergy(resolvedCost);
            }
        }
        StartCoroutine(ui.GetView(source).GetComponent<DropZone>().FlashWhite());
        ui.GetDropZone(source)?.PlayActionSprite(DropZone.ActionSpriteVariant(card));

        bool exhausted = false;
        Coroutine exitAnimation = null;

        if (source != null && source.isPlayer)
        {
            // Powers never reach a pile, so they must not run the exhaust roll/animation path.
            if (card.data.HasTag(CardTag.Exhaust) && card.data.type != CardType.Pouvoir)
            {
                float exhaustChance = BattleCalculator.GetModifiedValue(100, StatType.ExhaustChance, ctxSelf) / 100f;
                exhausted = UnityEngine.Random.value < exhaustChance;
            }
        }

        while (activeEffectResolutions > 0)
        {
            yield return null;
        }

        activeEffectResolutions++;

        try
        {
        currentCard = card; // Set current card for animation purposes
        if (playedView != null && playedView.rootRect != null)
        {
            playedView.rootRect.SetAsLastSibling();
        }
        if (source != null && source.isPlayer && playedView != null)
        {
            // Effects should begin exactly when this card starts leaving the center.
            exitAnimation = StartCoroutine(ui.AnimateCardToDiscard(playedView, exhausted, source));
        }

        // Actually apply effects
        for (int j=0;j<replayCount;j++)
        {
            if (playedView != null)
            {
                playedView.Flash();
            }
            if (source != null && source.isPlayer && card.targetingMode == TargetingMode.RandomEnemy)
            {
                var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();
                if (aliveEnemies.Any())
                {
                    Character newTarget = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
                    ctxTarget.targets = new List<Character> { newTarget };
                    ctxTarget.target = newTarget;
                    targets = new List<Character> { newTarget };
                }
            }

            foreach (StatusEffect status in source.statusEffects)
            {
                status.BeforeAction(source);
            }
            List<EffectEntry> usedEffectsList= new List<EffectEntry>();
            yield return new WaitForSeconds(0.1f*card.data.animationSpeed); // Delay before effects for better readability
            foreach (var effect in card.GetEffects())
            {
                if (effect.type == EffectType.Multihit)
                {
                    for(int i=0;i<effect.duration;i++)
                        {
                            usedEffectsList.Add(new EffectEntry
                            {
                                type = EffectType.Damage,
                                value = effect.value,
                                targetSelf=effect.targetSelf,
                                animationType=effect.animationType,
                            });
                        }
                }
                else
                {
                    usedEffectsList.Add(effect);
                }
            }
            foreach (var effect in usedEffectsList)
            {
                if (effect.conditional)
                {
                    if (!EffectResolver.VerifyCondition(effect.conditionType, effect.conditionValue, ctxTarget))
                    {
                        continue; // Skip this effect if condition is not met
                    }
                }
                SFXManager.Instance.PlaySound(effect.GetEffectName());
                if (effect.targetSelf)
                    {
                        VFXManager.Instance.PlayEffect(effect, ui.GetView(source).transform.position);
                        yield return EffectResolver.Apply(effect, ctxSelf);
                    }
                else if (effect.targetOthers)
                    {
                        // For this effect only, target all enemies that aren't among the original targets
                        var otherTargets = enemies.Where(e => e != null && e.IsAlive && !targets.Contains(e)).ToList();
                        foreach (var target in otherTargets)
                        {
                            ctxTarget.target = target;
                            if (ui.GetView(target) != null)
                            {
                                VFXManager.Instance.PlayEffect(effect, ui.GetView(target).transform.position);
                            }
                            yield return EffectResolver.Apply(effect, ctxTarget);
                        }
                    }
                else
                {
                    foreach(var target in targets)
                    {
                        ctxTarget.target = target;
                        if (ui.GetView(target) != null)
                        {
                            VFXManager.Instance.PlayEffect(effect, ui.GetView(target).transform.position);
                        }
                        yield return EffectResolver.Apply(effect, ctxTarget);
                    }
                }
                ui.RefreshUI();
                yield return new WaitForSeconds((effect.type == EffectType.Damage ? 0.1f : 0.3f)*card.data.animationSpeed/replayCount); // Small delay between effects for better readability
            }
            state.cardsPlayedThisTurn.Add(card);
            state.cardsPlayedThisCombat.Add(card);
            foreach (StatusEffect status in source.statusEffects)
            {
                status.AfterAction(source);
            }
            foreach (var target in targets)
            {
                foreach (StatusEffect status in source.statusEffects)
                {
                    status.OnCardPlayed(source,target,card);
                }
                foreach (StatusEffect status in target.statusEffects)
                {
                    status.OnTargetedByCard(source,target,card);
                }
            }
            if (source.isPlayer)
            {
                foreach (var relic in RunManager.Instance.relics)
                {
                    relic.OnCardPlayed(source, targets, card);
                }
            }
            foreach (Character character in GetAllCharacters())
            {
                character.AfterAction(source, card);
            }
        }
        }
        finally
        {
            activeEffectResolutions = Mathf.Max(0, activeEffectResolutions - 1);
        }

        if (source != null && source.isPlayer)
        {
            if (card.HasEnchantment("Infinity")||card.data.HasTag(CardTag.Infinite))
            {
                deck.AddToHand(card);
                
                if (!card.data.HasTag(CardTag.Infinite)) {
                    StatModifier mod=new FlatModifier(StatType.Cost, 1);
                    mod.temporary=true;
                    card.AddModifier(mod);
                }
            }
            else
            {
                if (card.data.type != CardType.Pouvoir)
                {
                    if (card.data.HasTag(CardTag.Exhaust))
                    {
                        if (exhausted)
                        {
                            deck.Exhaust(card);
                        }
                        else
                        {
                            deck.SendToDiscard(card);
                        }
                    }
                    else
                    {
                        deck.SendToDiscard(card);
                    }
                }
            }

            if (exitAnimation != null)
            {
                yield return exitAnimation;
            }
        }

        state.ResetActionFlags();
        yield return new WaitForSeconds(0.2f * card.data.animationSpeed); // Delay after effects for better readability
        // Check for end of combat
        bool combatOver = TryEndCombatIfNeeded();
        ui.HighlightTargets(TargetingMode.None, null);
        ui.RefreshUI(false);
        if (!combatOver)
            turnSystem.timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
        if (tutorialMode)
        {
            if (source != null && source.isPlayer)
            {
                tutorial.NotifyCardPlayed(card);
            }
            else
            {
                tutorial.NotifyEnemyCardPlayed(source as Enemy, card);
            }
        }
        }
        finally
        {
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
        }
    }

    public void FollowUpCard(bool randomCard, string cardName, Character source,Character target)
    {
        STSCardData data;
        if (randomCard)
        {
            data= STSCardDatabase.GetRandomCard();
        }
        else
        {
            data = STSCardDatabase.Get(cardName);
        }
        if (data == null)
        {
            Debug.LogWarning($"Carte de suivi introuvable : {cardName}");
            return;
        }
        CardInstance followUpCard = new CardInstance(data);
        if (!followUpCard.HasTag(CardTag.FollowUp))
        {
            followUpCard.AddTag(CardTag.FollowUp);
        }
        if (!followUpCard.HasTag(CardTag.Exhaust))
        {
            followUpCard.AddTag(CardTag.Exhaust);
        }
        PlayCard(source,followUpCard,AutoCardTargets(followUpCard.targetingMode,source,target),true,true);
    }


    public void ResetCombatStatus()
    {
        combatEnded = false;
        outcome = TeamOutcome.None;
        announcedOutcome = TeamOutcome.None;
    }

    private IEnumerator CleanupSlainCharactersRoutine()
    {
        bool rebuiltUI = false;

        foreach (var ally in allies.ToList())
        {
            if (ally != null && !ally.IsAlive)
            {
                if (!UsesAuthoritativeCombat && RunManager.Instance != null)
                {
                    foreach (var relic in RunManager.Instance.relics) // Last chance for relics to react to death and revive the character or do something
                    {
                        relic.OnDeath(ally);
                    }
                }
                if (!ally.IsAlive)                
                {
                    if (ui != null)
                    {
                        yield return ui.AnimateCharacterDeath(ally);
                    }
                    allies.Remove(ally);
                    rebuiltUI = true;
                }
            }
        }
        foreach (var enemy in enemies.ToList())
        {
            if (enemy != null && !enemy.IsAlive)
            {
                if (ui != null)
                {
                    yield return ui.AnimateCharacterDeath(enemy);
                }
                enemies.Remove(enemy);
                rebuiltUI = true;
            }
        }

        if (rebuiltUI && ui != null)
            ui.InitCharacters();
    }

    public bool TryEndCombatIfNeeded()
    {
        if (combatEnded || resolvingCombatCleanup)
            return true;

        bool alliesSlain = allies.All(a => a == null || !a.IsAlive);
        bool enemiesSlain = enemies.All(e => e == null || !e.IsAlive);
        bool hasDeadCharacters = allies.Any(a => a != null && !a.IsAlive) || enemies.Any(e => e != null && !e.IsAlive);

        // An outcome the server announced ends the combat whatever the client can see. Without
        // this, the gate below still asks the question the whole point was to stop asking: a
        // combat closed with nobody dead on our side of the wire -- a draw, a forfeit, or simply a
        // client whose view of the hit points differs -- was recorded and then never acted upon,
        // and the player went on playing a combat the server had finished.
        if (announcedOutcome == TeamOutcome.None
                && !alliesSlain && !enemiesSlain && !hasDeadCharacters)
            return false;

        resolvingCombatCleanup = true;
        StartCoroutine(ResolveCombatEndRoutine());
        return true;
    }

    private IEnumerator ResolveCombatEndRoutine()
    {
        yield return CleanupSlainCharactersRoutine();

        // Le serveur a tranché : on le lit. Il connaît des fins que les PV ne racontent pas
        // — un nul, un forfait — et il connaît les PV mieux que nous.
        if (announcedOutcome != TeamOutcome.None)
        {
            combatEnded = true;
            outcome = announcedOutcome;
        }
        else
        {
            // Chemin local : pas de serveur pour trancher, on déduit comme avant.
            bool alliesSlain = allies.All(a => a == null || !a.IsAlive);
            bool enemiesSlain = enemies.All(e => e == null || !e.IsAlive);

            if (!alliesSlain && !enemiesSlain)
            {
                if (ui != null)
                {
                    ui.RefreshUI(false);
                }

                if (turnSystem != null)
                {
                    turnSystem.timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
                }

                resolvingCombatCleanup = false;
                yield break;
            }

            combatEnded = true;
            outcome = enemiesSlain ? TeamOutcome.Victory : TeamOutcome.Defeat;
        }

        yield return EndCombat();
        resolvingCombatCleanup = false;
    }

    /// The player-side character whose turn is currently active. Cards in Player mode aim at
    /// them alone; AnyPlayer mode may aim at any living ally, including them.
    public Player GetActingPlayer()
    {
        foreach (var ally in allies)
        {
            if (ally != null && ally.IsAlive && ally.onTurn)
                return ally;
        }
        return player;
    }

    public List<Character> GetDisplayTargets(TargetingMode mode, Character hovered)
    {
        switch (mode)
        {
            case TargetingMode.Enemy:
                return hovered != null && hovered.IsAlive ? new List<Character> { hovered } : new();

            case TargetingMode.Player:
                Player acting = GetActingPlayer();
                return acting != null && acting.IsAlive ? new List<Character> { acting } : new();

            case TargetingMode.AnyPlayer:
                return hovered != null && hovered.isPlayer && hovered.IsAlive
                    ? new List<Character> { hovered }
                    : new();

            case TargetingMode.AllEnemies:
                return LivingOpponentsOf(GetActingPlayer());

            case TargetingMode.AllCharacters:
                return GetAllCharacters();

            case TargetingMode.RandomEnemy:
                return RandomEnemy();

            default:
                return new();
        }
    }
    public List<Character> AutoCardTargets(TargetingMode mode,Character source,Character target)
    {
        if (mode==TargetingMode.AllCharacters)
        {
            return GetAllCharacters();
        }
        if (!source.isPlayer)
        {
            Character firstOpponent = LivingOpponentsOf(source).FirstOrDefault();
            return firstOpponent != null
                ? new List<Character> { firstOpponent }
                : new List<Character>();
        }
        switch (mode)
        {
            case TargetingMode.Enemy:
                if (target!=null&&target!=source)
                    return new List<Character>{target};
                else
                    return RandomEnemy();
            case TargetingMode.AllEnemies:
                return LivingOpponentsOf(source);
            case TargetingMode.Player:
            {
                Player acting = GetActingPlayer();
                return acting != null && acting.IsAlive ? new List<Character> { acting } : new();
            }
            case TargetingMode.AnyPlayer:
                if (target != null && target.isPlayer && target.IsAlive)
                    return new List<Character> { target };
                return source != null ? new List<Character> { source } : new List<Character>();
            default:
                return RandomEnemy();
        }
    }
    public List<Character> GetAllCharacters()
    {
        if (combatantRegistry.LocalCombatantId == null)
        {
            // Chemin local, inchangé.
            var local = enemies.Where(e => e != null && e.IsAlive).Cast<Character>().ToList();
            foreach (var ally in allies)
            {
                if (ally != null && ally.IsAlive)
                    local.Add(ally);
            }
            return local;
        }

        return AllRegistered().Where(c => c != null && c.IsAlive).ToList();
    }
    public List<Character> GetAdversaries(Character character)
    {
        return LivingOpponentsOf(character);
    }

    /// Deux combattants sont hostiles quand ils ne partagent pas d'équipe.
    ///
    /// Sur le chemin local, où le registre est vide, la question se ramène à `isPlayer`,
    /// qui était la seule réponse possible avant que les équipes existent.
    public bool IsHostileTo(Character viewer, Character other)
    {
        if (viewer == null || other == null)
            return false;

        string viewerTeam = TeamOf(viewer);
        string otherTeam = TeamOf(other);
        if (string.IsNullOrEmpty(viewerTeam) || string.IsNullOrEmpty(otherTeam))
            return viewer.isPlayer != other.isPlayer;

        return !string.Equals(viewerTeam, otherTeam, StringComparison.Ordinal);
    }
    public List<Character> RandomEnemy()
    {
        var candidates = LivingOpponentsOf(GetActingPlayer());
        return candidates.Count == 0
            ? new List<Character>()
            : new List<Character> { candidates[UnityEngine.Random.Range(0, candidates.Count)] };
    }
    public void NotifyTurnEnded()
    {
        if (tutorialMode)
        {
            tutorial.NotifyTurnEnded();
        }
    }

    private System.Collections.IEnumerator EndCombat()
    {
        while (CardPlaysRunning||VFXManager.Instance.activeEffects) // Wait for any ongoing card plays to finish before showing end combat results
        {
            yield return null;
        }

        // A debug combat belongs to no map node, so there is nothing to report and no reward to
        // hand out: it just goes back to the menu that launched it.
        if (RunManager.Instance != null && RunManager.Instance.debugCombat)
        {
            Debug.Log($"[STS-DEBUG] Debug combat ended with outcome={outcome}.");
            RunManager.Instance.inCombat = false;
            RunManager.Instance.debugCombat = false;
            string returnScene = RunManager.Instance.debugCombatReturnScene;
            RunManager.Instance.debugCombatReturnScene = null;
            _ = STSApiClient.ClearDebugCombatAsync(RunManager.Instance.runId);
            RunManager.Instance.activeCombat = null;
            RunManager.Instance.activeEncounter = null;
            if (!string.IsNullOrWhiteSpace(returnScene))
            {
                STSSceneLoader.Instance?.LoadScene(returnScene);
            }
            yield break;
        }

        // Un duel n'est pas un nœud de carte. Sans cette sortie, une victoire PvP
        // déclencherait les hooks de reliques, marquerait le nœud courant terminé,
        // composerait une récompense depuis l'étage et l'acte de la run, appellerait
        // CompleteNode et chargerait STS_Reward. SubmitCombatResultAsync sort bien sans
        // rien faire quand il n'y a pas de run — mais un joueur qui a mis une run en pause
        // pour jouer un duel a toujours son runId et son activeEncounter, et gagnerait donc
        // un nœud de sa run en gagnant son duel.
        if (Mode == CombatMode.Pvp)
        {
            yield return EndPvpBattleRoutine();
            yield break;
        }

        if (outcome == TeamOutcome.Victory)
        {
            STSSceneLoader.Instance?.BeginLoading();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.08f);

            if (!UsesAuthoritativeCombat && RunManager.Instance != null)
            {
                foreach (var relic in RunManager.Instance.relics)
                {
                    relic.OnCombatEnd(player);
                }
            }

            if (RunManager.Instance != null && RunManager.Instance.currentNode != null)
            {
                RunManager.Instance.currentNode.completed = true;
                RunManager.Instance.currentNode.visited = true;
            }

            bool finishedLastActBoss = RunManager.Instance != null
                && RunManager.Instance.bossEncounter
                && EnemyPoolDatabase.IsLastAct(RunManager.Instance.act);

            var result = new CombatResult
            {
                enemies = currentEnemiesData,
                floor = RunManager.Instance.currentFloor,
                elite = RunManager.Instance.eliteEncounter,
                boss = RunManager.Instance.bossEncounter,
                act = RunManager.Instance.act
            };
            Task<bool> completeTask = SubmitCombatResultAsync("victory");
            while (!completeTask.IsCompleted)
            {
                yield return null;
            }

            STSSceneLoader.Instance?.SetBackgroundProgress(0.42f);

            bool completionAccepted = completeTask.Status == TaskStatus.RanToCompletion && completeTask.Result;
            if (!completionAccepted)
            {
                if (RunManager.Instance != null && RunManager.Instance.unrestrictedMode)
                {
                    Debug.LogWarning("[STS-RUN] Combat completion was not accepted, but unrestricted mode is active. Continuing locally.");
                }
                else
                {
                    Debug.LogWarning("[STS-RUN] Combat completion was not accepted. Staying in combat scene to avoid run desync.");
                    STSSceneLoader.Instance?.EndLoading();
                    yield break;
                }
            }

            if (finishedLastActBoss)
            {
                RunManager.Instance.completedFinalAct = true;
                RunManager.Instance.pendingReward = null;
                STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Retreat", "final_act_complete");
                STSSceneLoader.Instance.LoadScene("STS_Retreat");
                STSSceneLoader.Instance?.EndLoading();
                yield break;
            }
            RunManager.Instance.pendingReward = RewardGenerator.GenerateReward(result);
            STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Reward", "combat_complete");
            STSSceneLoader.Instance.LoadScene("STS_Reward");
            STSSceneLoader.Instance?.EndLoading();
        }
        // Ruling : un nul termine la run comme une défaite. Le serveur n'accorde aucune
        // récompense sur un nul, donc l'écran de victoire serait vide ; et sans cette
        // branche l'écran de fin ne s'afficherait pas du tout.
        else if (outcome == TeamOutcome.Defeat || outcome == TeamOutcome.Draw)
        {
            Task<bool> completeTask = SubmitCombatResultAsync("defeat");
            while (!completeTask.IsCompleted)
            {
                yield return null;
            }

            bool completionAccepted = completeTask.Status == TaskStatus.RanToCompletion && completeTask.Result;
            if (!completionAccepted)
            {
                Debug.LogWarning("[STS-RUN] Defeat completion was not accepted by server. Showing local game-over anyway.");
            }
            ui.ShowGameOver(enemies);
        }
    }

    /// <summary>
    /// Referme un duel : on coupe le transport, on montre l'issue, et on rend la main.
    /// Aucune complétion de nœud, aucune récompense, aucun déverrouillage de fin de run.
    ///
    /// <para>Quand le panneau de résultat est branché dans la scène, c'est lui qui referme
    /// la session et charge le menu, sur un clic. Sans lui, on retombe sur l'avis de
    /// combat et un retour au bout de quatre secondes.</para>
    /// </summary>
    IEnumerator EndPvpBattleRoutine()
    {
        // Le duel est fini : plus un battement. En laisser partir un de plus entretiendrait
        // au serveur la presence d'un joueur qui n'est plus dans aucun combat.
        StopPvpHeartbeat();
        surrenderConfirmation.Reset();
        ui?.HideSurrenderPrompt();

        string opponentName = OpponentDisplayName();
        Debug.Log($"[STS-PVP] Battle over: outcome={outcome} opponent={opponentName ?? "<unknown>"}");

        ReactCombatBridge.Disconnect();

        bool panelHasTheFloor = ui != null && ui.ShowPvpResult(outcome, opponentName);
        if (panelHasTheFloor)
            yield break;

        yield return new WaitForSecondsRealtime(4f);

        RunManager.Instance?.EndPvpBattle();
        STSSceneLoader.Instance?.LoadScene("STS_MultiplayerMenu");
    }

    /// Le nom sous lequel l'adversaire s'est présenté, à défaut le nom de son personnage.
    string OpponentDisplayName()
    {
        foreach (Character opponent in combatantRegistry.Opponents())
        {
            if (opponent == null)
                continue;

            return !string.IsNullOrWhiteSpace(opponent.playerDisplayName)
                ? opponent.playerDisplayName
                : opponent.name;
        }

        return null;
    }

    private async Task<bool> SubmitCombatResultAsync(string result)
    {
        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId) || RunManager.Instance.activeEncounter == null)
            return true;

        if (RunManager.Instance.unrestrictedMode)
            return true;

        STSApiNodeCompleteRequest request;
        if (UsesAuthoritativeCombat)
        {
            // Authoritative combats are already resolved server-side from the run's stored combat state;
            // encounterInstanceId/result must stay null so the backend takes that snapshot path instead of
            // validating a client-reported turnCount that the authoritative flow never increments locally.
            request = new STSApiNodeCompleteRequest
            {
                encounterInstanceId = null,
                result = null,
                turnCount = 0,
                playerHpAfter = player != null ? player.currentHP : 0,
                damageTaken = 0,
                enemiesDefeated = new List<string>(),
                deckHash = STSApiClient.ComputeDeckHash(RunManager.Instance.deck)
            };
        }
        else
        {
            request = new STSApiNodeCompleteRequest
            {
                encounterInstanceId = RunManager.Instance.activeEncounter.encounterInstanceId,
                result = result,
                turnCount = state.turnCount,
                playerHpAfter = player != null ? player.currentHP : 0,
                damageTaken = RunManager.Instance.activeEncounter != null ? Mathf.Max(0, RunManager.Instance.activeEncounter.playerHpBefore - (player != null ? player.currentHP : 0)) : 0,
                enemiesDefeated = string.Equals(result, "victory", StringComparison.OrdinalIgnoreCase)
                    ? new List<string>(RunManager.Instance.activeEncounter.enemyIds ?? new List<string>())
                    : enemies.Where(e => e != null && !e.IsAlive).Select(e => e is Enemy enemy ? (enemy.data != null && !string.IsNullOrWhiteSpace(enemy.data.id) ? enemy.data.id : enemy.name) : e.name).ToList(),
                deckHash = STSApiClient.ComputeDeckHash(RunManager.Instance.deck)
            };
        }

        try
        {
            Debug.Log($"[STS-RUN] CompleteNode request (combat) runId={RunManager.Instance.runId} nodeId={(RunManager.Instance.currentNode != null ? RunManager.Instance.currentNode.id : -1)} result={request.result} encounterId={request.encounterInstanceId}");
            STSApiNodeCompleteResponse response = await STSApiClient.CompleteNodeAsync(RunManager.Instance.runId, RunManager.Instance.currentNode != null ? RunManager.Instance.currentNode.id : -1, request);
            if (response != null && response.accepted)
            {
                Debug.Log($"[STS-RUN] CompleteNode response (combat) accepted=true runId={response.runId} currentNodeId={response.currentNodeId} result={request.result}");
                RunManager.Instance.ApplyNodeCompleteResponse(response);
                return true;
            }

            if (await TryRecoverCompletedNodeStateAsync(result, "rejected_or_null_response"))
            {
                return true;
            }

            Debug.LogWarning($"[STS-RUN] CompleteNode response (combat) was null or rejected for result={request.result}.");
            RunManager.Instance.EnableUnrestrictedMode($"combat completion rejected for result={request.result}");
        }
        catch (Exception ex)
        {
            if (await TryRecoverCompletedNodeStateAsync(result, $"exception:{ex.Message}"))
            {
                return true;
            }

            Debug.LogWarning($"[STS-RUN] CompleteNode request (combat) failed for result={request.result}: {ex.Message}");
            RunManager.Instance.EnableUnrestrictedMode($"combat completion failed for result={request.result}: {ex.Message}");
        }

        return false;
    }

    private async Task<bool> TryRecoverCompletedNodeStateAsync(string result, string cause)
    {
        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId))
            return false;

        try
        {
            STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
            if (currentRun == null || !currentRun.hasRun || currentRun.run == null)
                return false;

            STSApiRunState recoveredState = STSApiClient.ConvertToRunState(currentRun.run);
            if (recoveredState == null)
                return false;

            int localNodeId = RunManager.Instance.currentNode != null ? RunManager.Instance.currentNode.id : -1;
            bool nodeMarkedCompleted = recoveredState.map != null && recoveredState.map.Exists(n => n != null && n.id == localNodeId && n.completed);
            bool runProgressed = localNodeId >= 0 && recoveredState.currentNodeId != localNodeId;
            bool encounterCleared = recoveredState.activeEncounter == null;

            bool canTreatAsAccepted = nodeMarkedCompleted
                || runProgressed
                || (string.Equals(result, "victory", StringComparison.OrdinalIgnoreCase) && encounterCleared);

            if (!canTreatAsAccepted)
                return false;

            RunManager.Instance.ApplyRemoteRunState(recoveredState, currentRun.run.pendingRewards);
            Debug.LogWarning($"[STS-RUN] CompleteNode recovered from authoritative current-run state after {cause}. localNodeId={localNodeId} serverCurrentNodeId={recoveredState.currentNodeId} serverCompleted={nodeMarkedCompleted}");
            return true;
        }
        catch (Exception recoveryEx)
        {
            Debug.LogWarning($"[STS-RUN] Current-run recovery after complete-node failure also failed: {recoveryEx.Message}");
            return false;
        }
    }
}
