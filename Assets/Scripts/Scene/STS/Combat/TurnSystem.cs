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
                time = c != null && c.isPlayer ? -1f : Random.Range(0f, 5f),
                uid = TurnEntry.nextUID++
            });
        }

        foreach (var e in combat.enemies)
        {
            timeline.Add(new TurnEntry
            {
                character = e,
                time = Random.Range(0f, 5f),
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
        StartCoroutine(StartNextTurnRoutine());
    }

    private IEnumerator StartNextTurnRoutine()
    {
        startTurnRoutineRunning = true;

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
        for (int i=0;i<5;i++)
        {
            player.DrawCard();
        }
        combat.state.cardsPlayedThisTurn = 0;
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

        var enemy = enemyChar as Enemy;

        yield return new WaitForSeconds(0.3f);

        var cardData = enemy.GetNextAction();

        if (cardData != null)
        {
            var cardInstance = new CardInstance(cardData);

            var target = combat.allies.FirstOrDefault(a => a != null && a.IsAlive);

            if (target == null)
            {
                combat.TryEndCombatIfNeeded();
                yield break;
            }

            combat.PlayCard(enemy, cardInstance, new List<Character> { target });
        }

        yield return new WaitForSeconds(0.3f);

        EndTurn(enemyChar.turnDelay(baseDelay));
    }

    // -------------------------
    // PLAYER END TURN
    // -------------------------
    public void PlayerEndTurn()
    {
        endTurnButton.interactable = false;
        combat.deck.DiscardHand();
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
            timeline.Add(new TurnEntry
            {
                character = character,
                time = entry.time + delay,
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

    IEnumerator WaitForTimelineAnimation()
    {
        while (timelineUI != null && timelineUI.IsAnimating)
            yield return null;
    }

    // =========================================================
    // PREVIEW SYSTEM (IMPORTANT FIX)
    // =========================================================
    public List<TurnEntry> SimulateCard(List<TurnEntry> timeline,CardInstance card, Character target)
    {
        var sim = CloneTimeline(timeline);

        var source = CurrentCharacter;

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

        foreach (var effect in card.data.effects)
        {
            if (effect.targetSelf)
                EffectResolver.Apply(effect, ctxSelf);
            else
                EffectResolver.Apply(effect, ctx);
        }
        sim=new List<TurnEntry>(ctx.timeline);
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
        return GetFuture(10);
    }

    public List<TurnEntry> GetFuture(int steps)
    {
        var sim = CloneTimeline(timeline);

        var result = new List<TurnEntry>();

        for (int i = 0; i < steps; i++)
        {
            if (sim.Count == 0)
                break;

            Sort(sim);

            var next = sim[0];

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
                time = next.time + next.character.turnDelay(baseDelay),
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
            if (entry.character == target&& entry.time>0)
            {
                entry.time = Mathf.Max(currentTurnEntry.time, entry.time - amount); // ne pas avancer avant le tour actuel
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
            if (entry.character == target&& entry.uid!=currentTurnEntry.uid&& entry.time>0)
            {
                entry.time = Mathf.Max(currentTurnEntry.time, entry.time - amount);
            }
        }

        SortTimeline();
    }

    public void ApplyDelayAllTurns(Character target, float amount)
    {
        foreach (var entry in timeline)
        {
            if (entry.character == target)
            {
                entry.time = Mathf.Max(currentTurnEntry.time, entry.time + amount);
            }
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
}