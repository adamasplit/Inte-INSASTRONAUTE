using UnityEngine;
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
    // Tracks the number of active PlayCard coroutines
    private int activeCardPlays = 0;
    public bool CardPlaysRunning => activeCardPlays > 0;

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
    public RewardGenerator rewardGenerator= new RewardGenerator();
    public List<EnemyData> currentEnemiesData = new();
    public CardAnimator animator;
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
    }


    public void PlayCard(Character source, CardInstance card, List<Character> targets, bool ignoreEnergy = false, bool createView = false)
    {
        StartCoroutine(TrackPlayCardRoutine(source, card, targets, ignoreEnergy, createView));
    }

    private IEnumerator TrackPlayCardRoutine(Character source, CardInstance card, List<Character> targets, bool ignoreEnergy = false, bool createView = false)
    {
        activeCardPlays++;
        yield return StartCoroutine(PlayCardRoutine(source, card, targets, ignoreEnergy, createView));
        activeCardPlays--;
    }

    IEnumerator PlayCardRoutine(Character source, CardInstance card, List<Character> targets, bool ignoreEnergy = false, bool createView = false)
    {
        
        if (source==null||source.resources.energy < card.data.cost&&source.isPlayer&&!ignoreEnergy)
        {
            yield break;
        }
        CardView playedView = null;

        if (source != null && source.isPlayer)
        {
            if (createView)
            {
                playedView=ui.CreateCardView(card);
            }
            else
            {
                playedView = ui.GetView(card);
            }
            deck.RemoveFromHand(card);

            if (playedView != null)
            {
                ui.RemoveView(playedView);

                yield return ui.AnimateCardToCenter(playedView);
            }
            StartCoroutine(ui.GetView(source).GetComponent<DropZone>().FlashWhite());
        }
        foreach (StatusEffect status in source.statusEffects)
        {
            status.BeforeAction(source);
        }
        if (!ignoreEnergy)
        {
            source.SpendEnergy(card.data.cost);
        }
        EffectContext ctxTarget = new EffectContext
        {
            source = source,
            target = null,
            combat = this,
            state = state,
            card = card,
            timeline = turnSystem.timeline
        };

        EffectContext ctxSelf = new EffectContext
        {
            source = source,
            target = source,
            combat = this,
            state = state,
            card = card,
            timeline = turnSystem.timeline
        };
        List<EffectEntry> usedEffectsList= new List<EffectEntry>();
        yield return new WaitForSeconds(0.1f*card.data.animationSpeed); // Delay before effects for better readability
        foreach (var effect in card.data.effects)
        {
            if (effect.type == EffectType.Multihit)
            {
                for(int i=0;i<effect.duration;i++)
                    {
                        usedEffectsList.Add(new EffectEntry
                        {
                            type = EffectType.Damage,
                            value = effect.value,
                            targetSelf=effect.targetSelf
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
            foreach(var target in targets)
            {
            ctxTarget.target = target;
            
                if (effect.targetSelf)
                {
                    VFXManager.Instance.PlayEffect(effect, ui.GetView(source).transform.position);
                    EffectResolver.Apply(effect, ctxSelf);
                }
                else
                {
                    if (ui.GetView(target) != null)
                    {
                        VFXManager.Instance.PlayEffect(effect, ui.GetView(target).transform.position);
                    }
                    EffectResolver.Apply(effect, ctxTarget);
                }
            }
            ui.RefreshUI();
            yield return new WaitForSeconds((effect.type == EffectType.Damage ? 0.1f : 0.3f)*card.data.animationSpeed); // Small delay between effects for better readability
        }
        if (source != null && source.isPlayer)
        {
            if (card.data.type!=CardType.Pouvoir)
            {
                if (card.data.exhaust)
                    deck.Exhaust(card);
                else
                    deck.SendToDiscard(card);
            }

            if (playedView != null)
            {
                yield return ui.AnimateCardToDiscard(
                    playedView,
                    card.data.exhaust
                );
            }
        }
        state.cardsPlayedThisTurn++;
        foreach (StatusEffect status in source.statusEffects)
        {
            status.AfterAction(source);
        }
        foreach (Character character in GetAllCharacters())
        {
            character.AfterAction();
        }
        yield return new WaitForSeconds(0.2f*card.data.animationSpeed); // Delay after effects for better readability

        // Check for end of combat
        bool combatOver = TryEndCombatIfNeeded();
        ui.HighlightTargets(TargetingMode.None, null);
        ui.RefreshUI(false);
        if (!combatOver)
            turnSystem.timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
    }
    

    public void ResetCombatStatus()
    {
        combatEnded = false;
        outcome = TeamOutcome.None;
    }

    public void CleanupSlainCharacters()
    {
        int alliesBefore = allies.Count;
        int enemiesBefore = enemies.Count;

        allies.RemoveAll(a => a == null || !a.IsAlive);
        enemies.RemoveAll(e => e == null || !e.IsAlive);

        if ((alliesBefore != allies.Count || enemiesBefore != enemies.Count) && ui != null)
            ui.InitCharacters();
    }

    public bool TryEndCombatIfNeeded()
    {
        if (combatEnded)
            return true;

        CleanupSlainCharacters();

        bool alliesSlain = allies.Count == 0;
        bool enemiesSlain = enemies.Count == 0;

        if (!alliesSlain && !enemiesSlain)
            return false;

        combatEnded = true;
        outcome = enemiesSlain ? TeamOutcome.Victory : TeamOutcome.Defeat;

        EndCombat();
        return true;
    }

    public List<Character> GetTargets(TargetingMode mode, Character hovered)
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
            {
                var list = enemies.Where(e => e != null && e.IsAlive).Cast<Character>().ToList();
                if (player != null && player.IsAlive)
                    list.Add(player);
                return list;
            }

            default:
                return new();
        }
    }
    public List<Character> GetAllCharacters()
    {
        var list = enemies.Where(e => e != null && e.IsAlive).Cast<Character>().ToList();
        if (player != null && player.IsAlive)
            list.Add(player);
        return list;
    }

    void EndCombat()
    {
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
                boss = RunManager.Instance.bossEncounter
            };
            Debug.Log("Generating rewards for combat result: floor " + result.floor + ", elite: " + result.elite + ", boss: " + result.boss);
            var rewards = rewardGenerator.GenerateCardChoices(result);
            RunManager.Instance.pendingReward = new Reward();
            RunManager.Instance.pendingReward.cardChoices = rewards;
            if (result.elite)
            {
                Relic relic=RelicDrop.GetRandomRelic(result);
                RunManager.Instance.pendingReward.relic = relic;
            }
            SceneManager.LoadScene("STS_Reward");
        }
        else if (outcome == TeamOutcome.Defeat)
        {
            ui.ShowGameOver(enemies.FirstOrDefault());
        }
    }
}