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
    Defeat
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
    private bool authoritativeCommandInFlight;
    private float authoritativeCommandInFlightSince;
    private const float AuthoritativeCommandWatchdogSeconds = 8f;

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

    // Plays this client has already shown, keyed by card instance, so the server echo confirms
    // them instead of playing them a second time. See PlayCardAuthoritativeRoutine.
    private readonly Dictionary<string, Coroutine> presentedCardPlays =
        new Dictionary<string, Coroutine>(StringComparer.Ordinal);

    public bool UsesAuthoritativeCombat => RunManager.Instance != null
        && RunManager.Instance.activeCombat != null
        && RunManager.Instance.activeCombat.Type == JTokenType.Object;

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
            Enemy enn=enemy as Enemy;
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
        if (RunManager.Instance!=null)
        {
            RunManager.Instance.inCombat=true;
            STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "combat_init");
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
            StartCoroutine(ConnectAuthoritativeCombatSocketRoutine(AuthoritativeCombatIdentity.GetTransportId(
                RunManager.Instance.runId,
                RunManager.Instance.activeCombat)));
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
            StartCoroutine(ConnectAuthoritativeCombatSocketRoutine(AuthoritativeCombatIdentity.GetTransportId(
                RunManager.Instance.runId,
                RunManager.Instance.activeCombat)));
#endif
            STSSceneLoader.Instance?.SceneReady();
            yield break;
        }

        StartLocalCombatFlow();
    }

    IEnumerator ConnectAuthoritativeCombatSocketRoutine(string transportId)
    {
        Task<bool> connectTask = ReactCombatBridge.ConnectAsync(transportId);
        while (!connectTask.IsCompleted)
            yield return null;

        bool connected = connectTask.Status == TaskStatus.RanToCompletion && connectTask.Result;
        Debug.Log($"[STS-BRIDGE] socket connect combatId={transportId} success={connected}");
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

        // Gate the optimistic presentation on the one thing we can validate locally: the play
        // would otherwise animate fully before the server gets a chance to reject it with
        // INSUFFICIENT_ENERGY, so an unaffordable card looked played even though it never was.
        int cardCost = card != null ? card.Cost() : 0;
        if (player != null && cardCost >= 0 && player.resources.energy < cardCost)
        {
            authoritativeCommandInFlight = false;
            activeCardPlays = Mathf.Max(0, activeCardPlays - 1);
            Debug.Log($"[STS-COMBAT] PlayCard rejected locally: insufficient energy ({player.resources.energy} < {cardCost}) card={card?.displayName ?? "<null>"}");
            ui.StartCoroutine(ui.EnergyTextGlowRed());
            yield break;
        }

        // Show the play now rather than when the server echoes it back. Waiting on the round trip
        // leaves the card sitting in the hand, where HandLayoutController pulls it back into its
        // slot every frame, so the play read as arriving late instead of merely being confirmed
        // late. The echo below waits on this same coroutine, which keeps damage and status events
        // behind the card animation exactly as they were.
        if (card != null && !string.IsNullOrWhiteSpace(card.instanceId) && player != null)
        {
            presentedCardPlays[card.instanceId] = StartCoroutine(PresentCardPlayed(player, card, targets));
        }

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
                if (commandTask.Status != TaskStatus.RanToCompletion || commandTask.Result == ReactCombatCommandOutcome.Unknown)
                {
                    Debug.LogWarning("[STS-COMBAT] Failed to submit backend play-card command via Bridge.");
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
        {
            // No echo is coming for a play that was not accepted, so drop the presentation the
            // echo was meant to claim; the resync below puts the card back where it belongs.
            if (card != null && !string.IsNullOrWhiteSpace(card.instanceId))
                presentedCardPlays.Remove(card.instanceId);
            yield return RefreshAuthoritativeCombatState();
        }
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
        {
            if (card != null && !string.IsNullOrWhiteSpace(card.instanceId))
                presentedCardPlays.Remove(card.instanceId);
            yield return RefreshAuthoritativeCombatState();
        }
#endif
    }

    IEnumerator RefreshAuthoritativeCombatState()
    {
        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId))
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
        if (playedCard == null || playedCard.data == null || playedCard.data.effects == null)
            yield break;

        EffectEntry effect = playedCard.data.effects.FirstOrDefault(entry => entry.type == EffectType.CardSelection);
        if (effect == null || (effect.cardFilterTags != null && effect.cardFilterTags.Count > 0))
            yield break;

        bool supportedAction = effect.cardSelectionEffect == CardSelectionEffect.Exhaust
            || effect.cardSelectionEffect == CardSelectionEffect.Discard
            || effect.cardSelectionEffect == CardSelectionEffect.ReturnToHand
            || effect.cardSelectionEffect == CardSelectionEffect.TopOfDrawPile;
        if (!supportedAction)
            yield break;

        List<CardInstance> candidates = effect.cardSelectionSource switch
        {
            CardSelectionSource.Hand => deck.hand,
            CardSelectionSource.DrawPile => deck.drawPile,
            CardSelectionSource.DiscardPile => deck.discardPile,
            CardSelectionSource.ExhaustPile => deck.exhaustPile,
            _ => new List<CardInstance>()
        };
        candidates = candidates
            .Where(candidate => candidate != null && candidate.instanceId != playedCard.instanceId)
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
            if (type == "COMBAT_SNAPSHOT")
            {
                JToken state = message["payload"]?["state"];
                if (state != null) ApplyAuthoritativeCombatState(state, true);
            }
            else if (type == "COMBAT_EVENT")
            {
                JToken payload = message["payload"];
                if (payload != null)
                    yield return ReplayAuthoritativeEvents(new List<JToken> { payload });
            }
            else if (type == "STATE_UPDATED")
            {
                JToken payload = message["payload"];
                if (payload != null) ApplyAuthoritativeCombatState(payload, true);
            }
        }

        authoritativeMessageQueueRunning = false;
    }

    void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ReactCombatBridge.CombatEventReceived -= HandleReactCombatEvent;
        ReactCombatBridge.CombatStatusChanged -= HandleReactCombatStatusChanged;
#endif
        authoritativeMessageQueue.Clear();
        authoritativeMessageQueueRunning = false;
        presentedCardPlays.Clear();
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
            else if (commandTask.Status != TaskStatus.RanToCompletion || commandTask.Result == ReactCombatCommandOutcome.Unknown)
            {
                Debug.LogWarning("[STS-COMBAT] Failed to submit backend end-turn command via Bridge.");
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
        JToken activeCombat = RunManager.Instance != null ? RunManager.Instance.activeCombat : null;
        if (activeCombat == null || activeCombat.Type != JTokenType.Object)
            return 0L;

        return activeCombat.Value<long?>("revision") ?? 0L;
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
        if (character == null)
            return null;

        if (character.isPlayer)
            return "player";

        int enemyIndex = enemies.IndexOf(character);
        return enemyIndex >= 0 ? $"enemy-{enemyIndex}" : null;
    }

    void ApplyAuthoritativeCombatState(JToken combatToken, bool refreshUI)
    {
        if (combatToken == null || combatToken.Type != JTokenType.Object || RunManager.Instance == null)
            return;

        RunManager.Instance.activeCombat = combatToken;

        JArray combatants = combatToken["combatants"] as JArray;
        if (combatants == null)
            return;

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

            if (target.isPlayer)
            {
                ApplyAuthoritativePlayerPiles(combatantToken["piles"]);
            }
        }

        state.turnCount = AuthoritativeCombatStateReducer.ResolveTurnCount(
            state.turnCount,
            combatToken.Value<string>("status"));

        if (turnSystem != null && turnSystem.endTurnButton != null)
        {
            turnSystem.endTurnButton.interactable = string.Equals(activeCombatantId, "player", StringComparison.Ordinal)
                && !combatEnded;
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

            authoritativeTimeline.Add(entry);
            seenCombatantIds.Add(combatantId);
        }

        foreach (string staleId in authoritativeTimelineEntries.Keys.Where(id => !seenCombatantIds.Contains(id)).ToList())
            authoritativeTimelineEntries.Remove(staleId);

        turnSystem.timeline = authoritativeTimeline.OrderBy(entry => entry.time).ToList();

        // The server only ever reports one upcoming entry per combatant, so once a combatant's
        // turn passes they would vanish from the timeline until the next sync. Keep one stable
        // local projection per combatant (its own uid, so TimelineUI can animate it smoothly)
        // instead of the old GetFuture() unbounded exponential growth, but only as a visual
        // estimate — the server's next sync always overwrites it with the truth.
        foreach (TurnEntry currentEntry in authoritativeTimeline)
        {
            if (currentEntry.character == null)
                continue;

            string combatantId = GetAuthoritativeCombatantId(currentEntry.character);
            if (string.IsNullOrWhiteSpace(combatantId))
                continue;

            if (!authoritativeTimelineProjectionEntries.TryGetValue(combatantId, out TurnEntry projectedEntry))
            {
                projectedEntry = new TurnEntry { character = currentEntry.character, uid = TurnEntry.nextUID++ };
                authoritativeTimelineProjectionEntries[combatantId] = projectedEntry;
            }
            projectedEntry.character = currentEntry.character;
            projectedEntry.time = currentEntry.time + currentEntry.character.turnDelay(turnSystem.baseDelay);
            turnSystem.timeline.Add(projectedEntry);
        }

        turnSystem.timelineUI.Display(turnSystem.timeline);
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
            switch (eventType)
            {
                case "CardPlayed":
                    yield return ReplayCardPlayedEvent(combatEvent);
                    break;
                case "CardDrawn":
                    yield return ReplayCardDrawnEvent(combatEvent);
                    break;
                case "CardMoved":
                    yield return ReplayCardMovedEvent(combatEvent);
                    break;
                case "PileShuffled":
                    yield return ReplayPileShuffledEvent(combatEvent);
                    break;
                case "StatusApplied":
                    yield return ReplayStatusAppliedEvent(combatEvent);
                    break;
                case "StatusRemoved":
                    ReplayStatusRemovedEvent(combatEvent);
                    yield return new WaitForSeconds(0.05f);
                    break;
                case "StatusUpdated":
                    ReplayStatusUpdatedEvent(combatEvent);
                    yield return new WaitForSeconds(0.05f);
                    break;
                case "DamageApplied":
                    ReplayDamageAppliedEvent(combatEvent);
                    yield return new WaitForSeconds(0.12f);
                    break;
                case "HealApplied":
                    ReplayHealAppliedEvent(combatEvent);
                    yield return new WaitForSeconds(0.12f);
                    break;
                case "HpLost":
                    ReplayHpLostEvent(combatEvent);
                    yield return new WaitForSeconds(0.12f);
                    break;
                case "ArmorGained":
                    yield return ReplayArmorGainedEvent(combatEvent);
                    break;
                case "ArmorBroken":
                    ReplayArmorBrokenEvent(combatEvent);
                    yield return FlashCombatantWhite(ResolveCombatant(combatEvent.Value<string>("targetId")));
                    break;
                case "EnergySpent":
                    ReplayEnergySpentEvent(combatEvent);
                    break;
                case "EnergyGained":
                    ReplayEnergyGainedEvent(combatEvent);
                    break;
                case "TurnStarted":
                    if (string.Equals(combatEvent.Value<string>("combatantId"), "player", StringComparison.Ordinal))
                        state.turnCount = Mathf.Max(1, state.turnCount + 1);
                    yield return new WaitForSeconds(0.05f);
                    break;
                case "TurnEnded":
                    yield return new WaitForSeconds(0.05f);
                    break;
                case "CombatEnded":
                    yield return new WaitForSeconds(0.1f);
                    break;
            }
        }
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
        if (combatEvent["statusType"] != null || combatEvent["status"] != null || combatEvent["statusName"] != null)
        {
            if ((combatEvent.Value<bool?>("removed") ?? false) || (combatEvent.Value<bool?>("expired") ?? false))
                return "StatusRemoved";
            if (combatEvent["remainingDuration"] != null || combatEvent["newValue"] != null)
                return "StatusUpdated";
            return "StatusApplied";
        }
        if (combatEvent["fromPile"] != null || combatEvent["toPile"] != null || combatEvent["sourcePile"] != null || combatEvent["destinationPile"] != null)
            return "CardMoved";
        if (combatEvent["pile"] != null || combatEvent["drawSize"] != null)
            return "PileShuffled";
        if (combatEvent["previousReadyAtTick"] != null)
            return "TurnEnded";
        if (combatEvent["readyAtTick"] != null)
            return "TurnStarted";
        if (combatEvent["winnerTeamId"] != null)
            return "CombatEnded";
        return string.Empty;
    }

    IEnumerator ReplayCardPlayedEvent(JToken combatEvent)
    {
        string actorId = combatEvent.Value<string>("actorId");
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");

        Character actor = ResolveCombatant(actorId);
        if (actor == null || string.IsNullOrWhiteSpace(cardInstanceId) || string.IsNullOrWhiteSpace(definitionId))
            yield break;

        // This play was already shown when it was submitted; wait for that animation rather than
        // replaying it, so the events that follow stay behind the card as they always did.
        if (actor.isPlayer && presentedCardPlays.TryGetValue(cardInstanceId, out Coroutine presenting))
        {
            presentedCardPlays.Remove(cardInstanceId);
            yield return presenting;
            yield break;
        }

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
        if (actor.isPlayer && deck != null)
        {
            AuthoritativeCombatStateReducer.MoveCard(deck.hand, deck.discardPile, card);
        }

        CardView playedView = actor.isPlayer ? ui.GetView(card) : null;
        if (playedView == null)
        {
            Transform sourceView = ui.GetView(actor);
            playedView = ui.CreateCardView(card, false, sourceView != null ? (Vector3?)sourceView.position : null);
        }

        if (playedView == null)
            yield break;

        ui.GetDropZone(actor)?.PlayActionSprite(DropZone.ActionSpriteVariant(card));

        // The server resolves a whole AI turn chain in one round-trip and streams every event
        // back-to-back; without this pause enemy actions replay with no perceptible gap between
        // them, unlike the old local EnemyTurn coroutine which paused 0.2s before/after each move.
        if (!actor.isPlayer)
            yield return new WaitForSeconds(0.2f);

        yield return ui.AnimateCardToCenter(playedView);
        playedView.Flash();
        PlayCardEffectFeedback(targets, card);

        if (actor.isPlayer)
        {
            // The card leaves the centre while its effects land, which is what the local combat
            // path does and says in as many words: effects begin exactly when the card starts
            // leaving the center. Waiting for that exit instead put the whole of it in front of
            // every hit — 0.4s of travel plus the read pause — and that wait, not the round trip,
            // is what made an attack feel late.
            StartCoroutine(ui.AnimateCardToDiscard(playedView, false));
            yield break;
        }

        yield return new WaitForSeconds(0.08f);
        yield return ui.AnimateCardToDiscard(playedView, false);
        yield return new WaitForSeconds(0.2f);
    }

    // The authoritative replay path only ever animated card movement and popped up numbers;
    // it never played the per-effect SFX/VFX the local (non-authoritative) flow already has.
    // Takes the targets already resolved rather than the event they came from: a play is now
    // shown when it is submitted, before any event exists, and it deserves the same feedback.
    void PlayCardEffectFeedback(List<Character> targets, CardInstance card)
    {
        List<EffectEntry> effects = card.GetEffects();
        if (effects == null || effects.Count == 0)
        {
            Debug.Log($"[STS-VFX] no effects found for card {card?.displayName ?? "<null>"}");
            return;
        }

        targets ??= new List<Character>();

        foreach (EffectEntry effect in effects)
        {
            string effectName = effect.GetEffectName();
            Debug.Log($"[STS-VFX] card={card?.displayName ?? "<null>"} effect={effect.type} sfx={effectName} targets={targets.Count}");
            SFXManager.Instance?.PlaySound(effectName);

            if (targets.Count == 0)
                continue;

            foreach (Character target in targets)
            {
                Transform targetView = ui.GetView(target);
                if (targetView != null)
                    VFXManager.Instance?.PlayEffect(effect, targetView.position);
            }
        }
    }

    IEnumerator ReplayCardDrawnEvent(JToken combatEvent)
    {
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");

        CardInstance card = FindCardByInstanceId(cardInstanceId)
            ?? BuildCardFromDefinition(definitionId, cardInstanceId);
        if (card == null || deck == null || ui == null)
            yield break;

        deck.discardPile.Remove(card);
        deck.exhaustPile.Remove(card);
        deck.drawPile.Remove(card);
        if (!deck.hand.Contains(card))
        {
            int handIndex = combatEvent.Value<int?>("handIndex") ?? -1;
            InsertCardAt(deck.hand, card, handIndex);
        }

        ui.DrawCardAnimated(card);
        yield return new WaitForSeconds(0.12f);
    }

    IEnumerator ReplayCardMovedEvent(JToken combatEvent)
    {
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");
        string fromPile = ResolvePileName(combatEvent["fromPile"]?.ToString() ?? combatEvent["sourcePile"]?.ToString());
        string toPile = ResolvePileName(combatEvent["toPile"]?.ToString() ?? combatEvent["destinationPile"]?.ToString());

        CardInstance card = FindCardByInstanceId(cardInstanceId)
            ?? BuildCardFromDefinition(definitionId, cardInstanceId);
        if (card == null || deck == null)
            yield break;

        List<CardInstance> fromList = GetPileByName(fromPile);
        List<CardInstance> toList = GetPileByName(toPile);
        if (fromList != null)
        {
            fromList.Remove(card);
        }
        else
        {
            deck.hand.Remove(card);
            deck.drawPile.Remove(card);
            deck.discardPile.Remove(card);
            deck.exhaustPile.Remove(card);
        }

        if (toList != null && !toList.Contains(card))
        {
            int targetIndex = combatEvent.Value<int?>("toIndex")
                ?? combatEvent.Value<int?>("destinationIndex")
                ?? combatEvent.Value<int?>("handIndex")
                ?? -1;
            InsertCardAt(toList, card, targetIndex);
        }

        if (ui == null)
        {
            yield return null;
            yield break;
        }

        if (string.Equals(toPile, "EXHAUST", StringComparison.Ordinal))
        {
            if (ui.GetView(card) != null)
            {
                ui.ExhaustCardAnimated(card);
            }
            else
            {
                yield return ui.AnimateCardToPile(card, CardSelectionSource.ExhaustPile);
            }
            yield return new WaitForSeconds(0.12f);
            yield break;
        }

        if (string.Equals(toPile, "DISCARD", StringComparison.Ordinal))
        {
            if (ui.GetView(card) != null)
            {
                ui.DiscardCardAnimated(card);
            }
            else
            {
                yield return ui.AnimateCardToPile(card, CardSelectionSource.DiscardPile);
            }
            yield return new WaitForSeconds(0.10f);
            yield break;
        }

        if (string.Equals(toPile, "HAND", StringComparison.Ordinal))
        {
            if (string.Equals(fromPile, "DRAW", StringComparison.Ordinal))
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

        if (string.Equals(toPile, "DRAW", StringComparison.Ordinal))
        {
            yield return ui.AnimateCardToPile(card, CardSelectionSource.DrawPile);
        }
    }

    IEnumerator ReplayPileShuffledEvent(JToken combatEvent)
    {
        // The server shuffled and told us the order it got. Shuffling again locally would produce
        // a different one, and every draw after it would disagree with the server about which
        // card came up.
        string pileName = ResolvePileName(combatEvent.Value<string>("pile"));
        List<CardInstance> pile = GetPileByName(pileName) ?? deck?.drawPile;
        JToken orderToken = combatEvent["cardInstanceIds"];
        if (pile != null && orderToken != null && orderToken.Type == JTokenType.Array)
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
                    deck.hand.Remove(card);
                    deck.drawPile.Remove(card);
                    deck.discardPile.Remove(card);
                    deck.exhaustPile.Remove(card);
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
            ?? combatEvent.Value<string>("cardID")
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
        if (!string.Equals(combatantId, "player", StringComparison.Ordinal) || player == null)
            return;

        player.resources.energy = combatEvent.Value<int?>("remainingEnergy") ?? player.resources.energy;
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

    string ResolvePileName(string rawPile)
    {
        if (string.IsNullOrWhiteSpace(rawPile))
            return null;

        string upper = rawPile.Trim().ToUpperInvariant();
        if (upper.Contains("HAND"))
            return "HAND";
        if (upper.Contains("DRAW") || upper.Contains("DECK"))
            return "DRAW";
        if (upper.Contains("DISCARD"))
            return "DISCARD";
        if (upper.Contains("EXHAUST"))
            return "EXHAUST";
        return upper;
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
            status.Value = stateValue.Value;
            status.Duration = stateValue.Duration;
            status.cardID = stateValue.CardId;
            status.index = stateValue.Index;
            retained.Add(status);
        }

        target.statusEffects.RemoveAll(status => status == null || !retained.Contains(status));

        // Status effects in authoritative mode are just synced snapshots now — the old client-side
        // tick hooks (OnTurnEnd/OnDamageTaken/etc.) never run, so the only place feedback can come
        // from is detecting changes in this snapshot: appearing/disappearing/changing value.
        PlayStatusChangeFeedback(target, beforeStatuses);
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
            ?? combatEvent.Value<string>("status")
            ?? combatEvent.Value<string>("statusName");
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
        string cardId = combatEvent.Value<string>("cardId") ?? combatEvent.Value<string>("cardID");

        if (TryResolveStatusType(combatEvent, out StatusType statusType))
        {
            return target.statusEffects
                .Where(s => s != null
                    && s.statusType == statusType
                    && (!index.HasValue || s.index == index.Value)
                    && (string.IsNullOrWhiteSpace(cardId) || string.Equals(s.cardID, cardId, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        string rawName = combatEvent.Value<string>("statusName") ?? combatEvent.Value<string>("status");
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

    List<CardInstance> GetPileByName(string pileName)
    {
        if (deck == null || string.IsNullOrWhiteSpace(pileName))
            return null;

        switch (pileName)
        {
            case "HAND":
                return deck.hand;
            case "DRAW":
                return deck.drawPile;
            case "DISCARD":
                return deck.discardPile;
            case "EXHAUST":
                return deck.exhaustPile;
            default:
                return null;
        }
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
        if (string.IsNullOrWhiteSpace(combatantId))
            return null;

        if (string.Equals(combatantId, "player", StringComparison.Ordinal))
            return player;

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
        if (RunManager.Instance != null)
        {
            RunManager.Instance.deck = deck.drawPile
                .Concat(deck.hand)
                .Concat(deck.discardPile)
                .Concat(deck.exhaustPile)
                .Select(card => card != null ? card.Clone() : null)
                .Where(card => card != null)
                .ToList();
        }
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
            exitAnimation = StartCoroutine(ui.AnimateCardToDiscard(playedView, exhausted));
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

        if (!alliesSlain && !enemiesSlain && !hasDeadCharacters)
            return false;

        resolvingCombatCleanup = true;
        StartCoroutine(ResolveCombatEndRoutine());
        return true;
    }

    private IEnumerator ResolveCombatEndRoutine()
    {
        yield return CleanupSlainCharactersRoutine();

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

        yield return EndCombat();
        resolvingCombatCleanup = false;
    }

    public List<Character> GetDisplayTargets(TargetingMode mode, Character hovered)
    {
        switch (mode)
        {
            case TargetingMode.Enemy:
                return hovered != null && hovered.IsAlive ? new List<Character> { hovered } : new();

            case TargetingMode.Player:
                return player != null && player.IsAlive ? new List<Character> { player } : new();

            case TargetingMode.AllEnemies:
                return enemies.Where(e => e != null && e.IsAlive).ToList();

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
            return new List<Character>{player};
        }
        switch (mode)
        {
            case TargetingMode.Enemy:
                if (target!=null&&target!=source)
                    return new List<Character>{target};
                else
                    return RandomEnemy();
            case TargetingMode.AllEnemies:
                return enemies.Where(e => e != null && e.IsAlive).ToList();
            default:
                return RandomEnemy();
        }
    }
    public List<Character> GetAllCharacters()
    {
        var list = enemies.Where(e => e != null && e.IsAlive).Cast<Character>().ToList();
        if (player != null && player.IsAlive)
            list.Add(player);
        return list;
    }
    public List<Character> GetAdversaries(Character character)
    {
        if (character.isPlayer)
        {
            return enemies.Where(e => e != null && e.IsAlive).ToList();
        }
        else
        {
            return player != null && player.IsAlive ? new List<Character> { player } : new List<Character>();
        }
    }
    public List<Character> RandomEnemy()
    {
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();
                return aliveEnemies.Any()
                    ? new List<Character> { aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)] }
                    : new List<Character>();
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
        else if (outcome == TeamOutcome.Defeat)
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

    private async Task<bool> SubmitCombatResultAsync(string result)
    {
        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId) || RunManager.Instance.activeEncounter == null)
            return true;

        if (RunManager.Instance.unrestrictedMode)
            return true;

        STSApiNodeCompleteRequest request;
        if (UsesAuthoritativeCombat)
        {
            // Authoritative combats are resolved server-side from the run's stored combat state;
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
