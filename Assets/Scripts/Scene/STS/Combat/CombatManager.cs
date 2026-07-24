using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;
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
    public void Init()
    {
        EnsureAllies();
        EnsureEncounterEnemies();
        ui.Init(this);          // inject
        ui.InitCharacters();    // spawn UI
        ui.RefreshUI();
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
        queuedCardPlays++;
        StartCoroutine(PlayCardRoutine(source, card, targets, ignoreEnergy, createView));
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

        bool exhausted = false;
        Coroutine exitAnimation = null;

        if (source != null && source.isPlayer)
        {
            if (card.data.HasTag(CardTag.Exhaust))
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
                foreach (var relic in RunManager.Instance.relics) // Last chacnce for relics to react to death and revive the character or do something 
                {
                    relic.OnDeath(ally);
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

            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnCombatEnd(player);
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

        var request = new STSApiNodeCompleteRequest
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