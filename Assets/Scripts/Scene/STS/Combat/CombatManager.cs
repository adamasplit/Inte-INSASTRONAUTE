using UnityEngine;
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
        }
        foreach (var ally in allies)
        {
            ally.combat = this;
        }
    }

    public void PlayCard(Character source, CardInstance card, List<Character> targets)
    {
        StartCoroutine(PlayCardRoutine(source, card, targets));
    }

    IEnumerator PlayCardRoutine(Character source, CardInstance card, List<Character> targets)
    {
        
        if (source.resources.energy < card.data.cost&&source.isPlayer)
        {
            yield break;
        }
        CardView playedView = null;

        if (source != null && source.isPlayer)
        {
            playedView = ui.GetView(card);

            deck.RemoveFromHand(card);

            if (playedView != null)
            {
                ui.RemoveView(playedView);

                yield return ui.AnimateCardToCenter(playedView);
            }
        }
        foreach (StatusEffect status in source.statusEffects)
        {
            status.BeforeAction(source);
        }
        source.SpendEnergy(card.data.cost);
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
        foreach (var effect in card.data.effects)
        {
            foreach(var target in targets)
            {
            ctxTarget.target = target;
            
                if (effect.targetSelf)
                    EffectResolver.Apply(effect, ctxSelf);
                else
                    EffectResolver.Apply(effect, ctxTarget);
            }
            yield return new WaitForSeconds(0.3f); // Small delay between effects for better readability
        }
        if (source != null && source.isPlayer)
        {
            if (card.data.exhaust)
                deck.Exhaust(card);
            else
                deck.SendToDiscard(card);

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
            var result = new CombatResult
            {
                enemies = currentEnemiesData,
                floor = RunManager.Instance.currentFloor
            };
            var rewards = rewardGenerator.GenerateCardChoices(result);
            RunManager.Instance.pendingReward = new Reward();
            RunManager.Instance.pendingReward.cardChoices = rewards;
            SceneManager.LoadScene("STS_Reward");
        }
        else if (outcome == TeamOutcome.Defeat)
        {
            ui.ShowGameOver(enemies.FirstOrDefault());
        }
    }
}