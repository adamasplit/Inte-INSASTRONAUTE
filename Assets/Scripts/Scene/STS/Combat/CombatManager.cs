using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
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

    // Tracks the number of active PlayCard coroutines
    private int activeCardPlays = 0;
    public bool CardPlaysRunning => activeCardPlays > 0;
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
    public void Init()
    {
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
        }

        STSSceneLoader.Instance?.SceneReady();
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
        StartCoroutine(PlayCardRoutine(source, card, targets, ignoreEnergy, createView));
    }

    IEnumerator PlayCardRoutine(Character source, CardInstance card, List<Character> targets, bool ignoreEnergy = false, bool createView = false)
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

        if (source==null||source.resources.energy < card.Cost(ctxTarget)&&source.isPlayer&&!ignoreEnergy)
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
                source.SpendEnergy(card.Cost(ctxTarget));
            }
        }
        StartCoroutine(ui.GetView(source).GetComponent<DropZone>().FlashWhite());
        while (CardPlaysRunning) // Wait for other card plays to finish to avoid overlapping effects and ensure proper sequencing
        {
            yield return null;
        }
        activeCardPlays++; // Increment active card plays counter
        currentCard = card; // Set current card for animation purposes

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
                    Debug.Log($"Randomly re-targeted card effects to {newTarget.name} (current HP: {newTarget.currentHP}) due to targeting mode.");
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

        if (source != null && source.isPlayer)
        {
            bool exhausted = false;
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
                        float exhaustChance = BattleCalculator.GetModifiedValue(100, StatType.ExhaustChance, ctxSelf) / 100f;
                        if (UnityEngine.Random.value < exhaustChance)
                        {
                            deck.Exhaust(card);
                            exhausted = true;
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

            if (playedView != null)
            {
                yield return ui.AnimateCardToDiscard(
                    playedView,
                    exhausted
                );
            }
        }

        state.ResetActionFlags();
        yield return new WaitForSeconds(0.2f * card.data.animationSpeed); // Delay after effects for better readability
        activeCardPlays--; // Decrement active card plays counter
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

    public void FollowUpCard(bool randomCard, string cardName, Character source,Character target)
    {
        Debug.Log($"Follow-up card triggered: {(randomCard ? "Random card" : cardName)} from {source.name} targeting {target.name}");
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
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnCombatEnd(player);
            }
            var result = new CombatResult
            {
                enemies = currentEnemiesData,
                floor = RunManager.Instance.currentFloor,
                elite = RunManager.Instance.eliteEncounter,
                boss = RunManager.Instance.bossEncounter,
                act = RunManager.Instance.act
            };
            //Debug.Log("Generating rewards for combat result: floor " + result.floor + ", elite: " + result.elite + ", boss: " + result.boss);
            RunManager.Instance.pendingReward = RewardGenerator.GenerateReward(result);
            STSSceneLoader.Instance.LoadScene("STS_Reward");
        }
        else if (outcome == TeamOutcome.Defeat)
        {
            ui.ShowGameOver(enemies.FirstOrDefault());
        }
    }
}