using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
public class TurnSystem : MonoBehaviour
{
    public CombatManager combat;
    public UIManager ui;
    public TimelineUI timelineUI;

    public List<TurnEntry> timeline = new();

    public int baseDelay = 10;
    public Button endTurnButton;
    public TurnEntry currentTurnEntry;

    bool startTurnRoutineRunning;
    bool endTurnRoutineRunning;
    Dictionary<int, float> pendingTurnDelay = new();

    void Update()
    {
        if (combat == null || combat.combatEnded || !combat.allowTurn)
            return;

        if (combat.CardPlaysRunning || startTurnRoutineRunning || endTurnRoutineRunning)
            return;

        bool hasDeadCharacters = combat.allies.Any(a => a != null && !a.IsAlive)
            || combat.enemies.Any(e => e != null && !e.IsAlive);

        if (hasDeadCharacters)
        {
            combat.TryEndCombatIfNeeded();
            return;
        }

        if (!combat.GetAllCharacters().Any(c => c != null && c.onTurn))
        {
            StartNextTurn();
        }
    }

    public void Begin()
    {
        combat.ResetCombatStatus();
        InitTimeline();
        StartNextTurn();
    }

    // -------------------------
    // INIT
    // -------------------------
    void InitTimeline()
    {
        timeline.Clear();

        foreach (var c in combat.allies)
        {
            timeline.Add(new TurnEntry
            {
                character = c,
                time = c != null && c.isPlayer ? 0f : Random.Range(0f, 5f),
                uid = TurnEntry.nextUID++
            });
        }
        float time=0f;
        foreach (var e in combat.enemies)
        {
             // pour éviter que les ennemis aient tous la même initiative
            time+=baseDelay/ (combat.enemies.Count+1);
            timeline.Add(new TurnEntry
            {
                character = e,
                time = time,
                uid = TurnEntry.nextUID++
            });
            
        }

        SortTimeline();
        timelineUI.Display(GetDisplayTimeline(timeline));
    }

    void SortTimeline()
    {
        timeline = timeline.OrderBy(t => t.time).ToList();
    }

    public Character CurrentCharacter => timeline.Count > 0 ? timeline[0].character : null;

    void SyncTimelineWithLivingCharacters()
    {
        var living = new HashSet<Character>(combat.GetAllCharacters());
        timeline.RemoveAll(t => t.character == null || !t.character.IsAlive || !living.Contains(t.character));
    }

    // -------------------------
    // TURN FLOW
    // -------------------------
    public void StartNextTurn()
    {
        if (startTurnRoutineRunning)
            return;

        // Lock immediately to avoid scheduling multiple start-turn coroutines in the same frame.
        startTurnRoutineRunning = true;
        StartCoroutine(StartNextTurnRoutine());
    }

    private IEnumerator StartNextTurnRoutine()
    {
        while (!combat.allowTurn)
            yield return null;

        // Wait for all card plays to finish before starting the next turn
        while (combat.CardPlaysRunning)
            yield return null;

        yield return WaitForTimelineAnimation();

        if (combat.TryEndCombatIfNeeded())
        {
            startTurnRoutineRunning = false;
            yield break;
        }

        SyncTimelineWithLivingCharacters();

        if (timeline.Count == 0)
        {
            startTurnRoutineRunning = false;
            yield break;
        }

        SortTimeline();

        while (timeline.Count > 0)
        {
            var nextEntry = timeline[0];
            if (nextEntry.character != null && nextEntry.character.IsAlive)
                break;

            timeline.RemoveAt(0);
        }

        if (timeline.Count == 0)
        {
            startTurnRoutineRunning = false;
            yield break;
        }

        var entry = timeline[0];
        currentTurnEntry = entry;
        var character = entry.character;

        // Animate the timeline into the new state before the turn actually starts.
        timelineUI.Display(GetDisplayTimeline(timeline));
        yield return WaitForTimelineAnimation();

        character.StartTurn();

        if (character.isPlayer)
            StartPlayerTurn(character);
        else
            StartCoroutine(EnemyTurn(character));

        ui.RefreshUI();

        startTurnRoutineRunning = false;
    }

    void StartPlayerTurn(Character player)
    {
        if (player.HasStatus("Étourdissement"))
        {
            PlayerEndTurn();
            return;
        }
        for (int i=0;i<5;i++)
        {
            player.DrawCard();
        }
        foreach (var relic in RunManager.Instance.relics)
        {
            if (relic == null)
            {
                Debug.LogError("Null relic found in RunManager.Instance.relics");
                continue;   
            }
            relic.OnTurnStart(player);
        }
        endTurnButton.interactable = true;
    }

    public System.Collections.IEnumerator EnemyTurn(Character enemyChar)
    {
        if (combat.TryEndCombatIfNeeded())
            yield break;
        if (enemyChar.HasStatus("Étourdissement"))
        {
            enemyChar.RemoveStatus(enemyChar.statusEffects.First(s => s.Name == "Étourdissement"));
            EndTurn(enemyChar.turnDelay(baseDelay));
            yield break;
        }

        var enemy = enemyChar as Enemy;
        if (enemy == null)
        {
            EndTurn(enemyChar.turnDelay(baseDelay));
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        var action = enemy.GetNextActionPlan();

        if (action != null)
        {
            var runtimeCard = action.CreateRuntimeCard(enemy.name);
            if (runtimeCard == null)
            {
                yield return new WaitForSeconds(0.2f);
                EndTurn(enemyChar.turnDelay(baseDelay));
                yield break;
            }
            var cardInstance = new CardInstance(runtimeCard);
            var target = combat.allies.FirstOrDefault(a => a != null && a.IsAlive);
            if (target == null)
            {
                combat.TryEndCombatIfNeeded();
                yield break;
            }
            combat.PlayCard(enemy, cardInstance, new List<Character> { target });
        }

        yield return new WaitForSeconds(0.2f);

        EndTurn(enemyChar.turnDelay(baseDelay));
    }

    // -------------------------
    // PLAYER END TURN
    // -------------------------
    public void PlayerEndTurn()
    {
        endTurnButton.interactable = false;
        combat.deck.DiscardHand();
        combat.NotifyTurnEnded();
        EndTurn(CurrentCharacter.turnDelay(baseDelay));
    }

    // -------------------------
    // END TURN
    // -------------------------
    public void EndTurn(int delay)
    {
        if (combat.combatEnded || timeline.Count == 0 || endTurnRoutineRunning)
            return;

        StartCoroutine(EndTurnRoutine(delay));
    }

    IEnumerator EndTurnRoutine(int delay)
    {
        endTurnRoutineRunning = true;

        yield return WaitForTimelineAnimation();
        yield return WaitForCardAnimations();

        if (combat.combatEnded || timeline.Count == 0)
        {
            endTurnRoutineRunning = false;
            yield break;
        }

        var entry = timeline[0];
        var character = entry.character;

        character.EndTurn();

        timeline.RemoveAt(0);

        if (character.IsAlive)
        {
            float extraDelay = 0;

            if (pendingTurnDelay.TryGetValue(entry.uid, out var bonus))
            {
                extraDelay = bonus;
                pendingTurnDelay.Remove(entry.uid);
            }
            timeline.Add(new TurnEntry
            {
                character = character,
                time = entry.time + delay + extraDelay,
                uid = TurnEntry.nextUID++
            });
        }
        if (combat.TryEndCombatIfNeeded())
        {
            endTurnRoutineRunning = false;
            yield break;
        }

        SyncTimelineWithLivingCharacters();

        if (timeline.Count == 0)
        {
            endTurnRoutineRunning = false;
            yield break;
        }

        SortTimeline();
        timelineUI.Display(GetDisplayTimeline(timeline));

        endTurnRoutineRunning = false;
        StartNextTurn();
    }
    public void AddPendingDelay(TurnEntry entry, float amount)
    {
        if (!pendingTurnDelay.ContainsKey(entry.uid))
            pendingTurnDelay[entry.uid] = 0;

        pendingTurnDelay[entry.uid] += amount;
    }

    IEnumerator WaitForTimelineAnimation()
    {
        while (timelineUI != null && timelineUI.IsAnimating)
            yield return null;
    }

    IEnumerator WaitForCardAnimations()
    {
        while (combat.CardPlaysRunning)
            yield return null;
    }

    // =========================================================
    // PREVIEW SYSTEM (IMPORTANT FIX)
    // =========================================================
    public List<TurnEntry> SimulateCard(List<TurnEntry> timeline,CardInstance card, List<Character> targets)
    {
        var sim = CloneTimeline(timeline);

        var source = CurrentCharacter;
        
        
        var ctxSelf = new EffectContext
        {
            source = source,
            target = source,
            combat = combat,
            state = combat.state,
            card = card,
            timeline = sim,
            isPreview = true
        };

        foreach (var effect in card.GetEffects())
        {
            if (effect.targetSelf)
                EffectResolver.Preview(effect, ctxSelf);
            else
            foreach (var target in targets)            
            {
                var ctx = new EffectContext
                {
                    source = source,
                    target = target,
                    combat = combat,
                    state = combat.state,
                    card = card,
                    timeline = sim,
                    isPreview = true
                };
                EffectResolver.Preview(effect, ctx);
                sim= ctx.timeline;
            }
        }
        foreach (var entry in sim)
        {
            if (ctxSelf.timeline.Any(t => t.character == entry.character && t.visualType != TurnVisualType.Normal))
            {
                entry.visualType = ctxSelf.timeline.First(t => t.character == entry.character).visualType;
            }
        }
        sim.Sort((a, b) => a.time.CompareTo(b.time));
        return sim;
    }

    List<TurnEntry> CloneTimeline(List<TurnEntry> timeline = null)
    {
        return timeline
            .Select(t => new TurnEntry
            {
                character = t.character,
                time = t.time,
                visualType = t.visualType,
                uid = t.uid
            })
            .ToList();
    }

    void ReinsertCharacter(List<TurnEntry> sim, Character c)
    {
        // trouver le tour actuel (celui qui vient d'agir)
        var current = sim
            .Where(t => t.character == c)
            .OrderBy(t => t.time)
            .FirstOrDefault();

        if (current == null)
            return;

        // supprimer CE tour (consommé)
        sim.Remove(current);

        // recréer un nouveau tour plus tard
        sim.Add(new TurnEntry
        {
            character = c,
            time = current.time + c.turnDelay(baseDelay),
            visualType = TurnVisualType.Normal, // IMPORTANT
            uid = current.uid // IMPORTANT pour le matching dans le UI
        });
    }

    public List<TurnEntry> GetDisplayTimeline(List<TurnEntry> baseTimeline)
    {
        return GetFuture(baseTimeline, 10);
    }

    public List<TurnEntry> GetFuture(List<TurnEntry> baseTimeline, int steps)
    {
        var sim = CloneTimeline(baseTimeline);

        var result = new List<TurnEntry>();

        for (int i = 0; i < steps; i++)
        {
            if (sim.Count == 0)
                break;

            Sort(sim);

            var next = sim[0];
            float extraDelay = 0;

            if (pendingTurnDelay.TryGetValue(next.uid, out var bonus))
            {
                extraDelay = bonus;
            }
            result.Add(new TurnEntry
            {
                character = next.character,
                time = next.time,
                visualType = next.visualType,
                uid = next.uid
            });

            sim.RemoveAt(0);

            sim.Add(new TurnEntry
            {
                character = next.character,
                time = next.time + next.character.turnDelay(baseDelay)+ extraDelay,
                visualType = TurnVisualType.Normal,
                uid = TurnEntry.nextUID++
            });
        }

        return result;
    }
    public void Sort(List<TurnEntry> list)
    {
        list.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public List<TurnEntry> AdvanceAllTurns(List<TurnEntry> source, Character target, float amount)
    {
        var sim = Clone(source);

        foreach (var entry in sim)
        {
            if (entry.character == target)
            {
                entry.time -= amount;
                entry.visualType = TurnVisualType.Advanced;
            }
        }
        return sim.OrderBy(t => t.time).ToList();
    }

    public List<TurnEntry> DelayAllTurns(List<TurnEntry> source, Character target, float amount)
    {
        var sim = Clone(source);

        foreach (var entry in sim)
        {
            if (entry.character == target)
            {
                entry.time += amount;
                entry.visualType = TurnVisualType.Delayed;
            }
        }

        return sim.OrderBy(t => t.time).ToList();
    }

    public void ApplyAdvanceAllTurns(Character target, float amount)
    {
        foreach (var entry in timeline)
        {
            if (entry.character != target)
                continue;

            // TOUR ACTUEL
            if (entry.uid == currentTurnEntry.uid)
            {
                AddPendingDelay(entry, -amount);
                continue;
            }

            // TOURS FUTURS
            entry.time -= amount;
        }

        SortTimeline();
    }

    public void ApplyDelayAllTurns(Character target, float amount)
    {
        foreach (var entry in timeline)
        {
            if (entry.character != target)
                continue;

            // TOUR ACTUEL
            if (entry.uid == currentTurnEntry.uid)
            {
                AddPendingDelay(entry, amount);
                continue;
            }

            // TOURS FUTURS
            entry.time += amount;
        }

        SortTimeline();
    }

    public List<TurnEntry> Clone(List<TurnEntry> source)
    {
        return source
            .Select(t => new TurnEntry
            {
                character = t.character,
                time = t.time,
                visualType = t.visualType,
                uid = t.uid
            })
            .ToList();
    }
    public float GetTimeUntilNextTurn(Character character)
    {
        var entry = timeline.FirstOrDefault(t => t.character == character);
        if (entry == null)
            return -1f;

        float currentTime = timeline[0].time;
        return Mathf.Max(0f, entry.time - currentTime);
    }
}