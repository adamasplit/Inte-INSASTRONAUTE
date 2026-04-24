using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public Player player => allies[0];

    public List<Player> allies = new();
    public List<Character> enemies = new();
    public List<Character> characters => GetAllCharacters();

    public DeckManager deck;
    public UIManager ui;
    public TurnSystem turnSystem;

    public CombatState state = new CombatState();

    void Init()
    {
        ui.Init(this);          // inject
        ui.InitCharacters();    // spawn UI
        ui.RefreshUI();
    }

    public void PlayCard(CardInstance card, List<Character> targets)
    {
        if (player.resources.energy < card.data.cost)
            return;

        player.SpendEnergy(card.data.cost);

        EffectContext ctxTarget = new EffectContext
        {
            source = player,
            target = null,
            combat = this,
            state = state,
            card = card,
            timeline = turnSystem.timeline
        };

        EffectContext ctxSelf = new EffectContext
        {
            source = player,
            target = player,
            combat = this,
            state = state,
            card = card,
            timeline = turnSystem.timeline
        };
        foreach (var target in targets)
        {
            ctxTarget.target = target;
            foreach (var effect in card.data.effects)
            {
                if (effect.targetSelf)
                    EffectResolver.Apply(effect, ctxSelf);
                else
                    EffectResolver.Apply(effect, ctxTarget);
            }
        }

        deck.hand.Remove(card);

        if (card.data.exhaust)
            deck.exhaustPile.Add(card);
        else
            deck.discardPile.Add(card);
        ui.HighlightTargets(TargetingMode.None, null);
        ui.RefreshUI();
        turnSystem.timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
    }

    public int GetModifiedValue(int baseValue, StatType type, EffectContext ctx)
    {
        int value = baseValue;

        foreach (var mod in GetAllModifiers(type))
            value = mod.Modify(value, ctx);

        foreach (var effect in ctx.source.statusEffects)
        {
            if (effect.AppliesTo(type, ctx))
                value = effect.Modify(value, ctx);
        }

        if (ctx.target != null)
        {
            foreach (var effect in ctx.target.statusEffects)
            {
                if (effect.AppliesTo(type, ctx))
                    value = effect.Modify(value, ctx);
            }
        }

        return value;
    }
    public string GetModifiedDescription(int baseValue, StatType type, EffectContext ctx)
    {
        int modifiedValue = GetModifiedValue(baseValue, type, ctx);
        if (modifiedValue <= 0)
            return $"<color=gray>{modifiedValue}</color>";
        else if (modifiedValue < baseValue)
            return $"<color=red>{modifiedValue}</color>";
        else if (modifiedValue > baseValue)
            return $"<color=green>{modifiedValue}</color>";
        else
            return modifiedValue.ToString();
    }

    private List<StatModifier> GetAllModifiers(StatType type)
    {
        return state.GetModifiers(type);
    }

    public List<Character> GetTargets(TargetingMode mode, Character hovered)
    {
        switch (mode)
        {
            case TargetingMode.Enemy:
                return hovered != null ? new List<Character> { hovered } : new();

            case TargetingMode.Player:
                return new List<Character> { player };

            case TargetingMode.AllEnemies:
                return new List<Character>(enemies);

            case TargetingMode.AllCharacters:
            {
                var list = new List<Character>(enemies);
                list.Add(player);
                return list;
            }

            default:
                return new();
        }
    }
    public List<Character> GetAllCharacters()
    {
        var list = new List<Character>(enemies);
        list.Add(player);
        return list;
    }
}