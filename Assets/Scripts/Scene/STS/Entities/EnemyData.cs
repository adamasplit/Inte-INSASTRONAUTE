using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyMoveEntry
{
    public STSCardData card;
    public string moveName;
    public List<EffectEntry> effects = new();
    public List<int> nextMoveIndices = new();
    public int weight = 1;
    public STSCardData CreateRuntimeCard(string enemyName)
    {
        if (card == null && (effects == null || effects.Count == 0))
            return null;

        var runtimeCard = ScriptableObject.CreateInstance<STSCardData>();
        runtimeCard.name = !string.IsNullOrEmpty(moveName) ? moveName : $"{enemyName}_EnemyMove";
        runtimeCard.id = runtimeCard.name;
        runtimeCard.cardName = !string.IsNullOrEmpty(moveName)
            ? moveName
            : (card != null && !string.IsNullOrEmpty(card.cardName) ? card.cardName : runtimeCard.name);
        runtimeCard.collectionCardId = null;
        runtimeCard.collectionCard = null;
        runtimeCard.icon = card != null ? card.icon : null;
        runtimeCard.cost = 0;
        runtimeCard.xCost = false;
        runtimeCard.type = CardType.Compétence;
        runtimeCard.rarity = CardRarity.Common;
        runtimeCard.targetingMode = TargetingMode.Enemy;
        runtimeCard.animationSpeed = card != null ? card.animationSpeed : 1f;
        runtimeCard.startingCount = 0;

        runtimeCard.effects = new List<EffectEntry>();
        var sourceEffects = effects != null && effects.Count > 0 ? effects : card?.effects;
        if (sourceEffects != null)
        {
            foreach (var effect in sourceEffects)
            {
                runtimeCard.effects.Add(EffectEntry.Clone(effect));
                if (effect.type==EffectType.Damage||effect.type==EffectType.Multihit)
                {
                    runtimeCard.type=CardType.Attaque;
                }
            }
        }

        runtimeCard.modifiers = new List<ModifierData>();
        if (card != null && card.modifiers != null)
        {
            foreach (var modifier in card.modifiers)
            {
                runtimeCard.modifiers.Add(modifier);
            }
        }

        runtimeCard.tags = new List<CardTag>();
        if (card != null && card.tags != null)
        {
            foreach (var tag in card.tags)
            {
                runtimeCard.tags.Add(tag);
            }
        }

        return runtimeCard;
    }
}

[CreateAssetMenu(menuName = "Combat/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public string displayName;
    public int maxHP;

    public List<STSCardData> pattern;
    public bool randomStart=false;
    public List<EnemyMoveEntry> movePattern;
    public List<STSCardData> rewardCards;
    public StatusType startingStatus;
    public int startingStatusDuration;
    public int startingStatusValue;
    public string startingStatusInfo;

    public int ActionCount => movePattern != null && movePattern.Count > 0
        ? movePattern.Count
        : pattern != null ? pattern.Count : 0;

    public EnemyMoveEntry GetActionAt(int index)
    {
        if (movePattern != null && movePattern.Count > 0)
        {
            return movePattern[index % movePattern.Count];
        }

        if (pattern != null && pattern.Count > 0)
        {
            return new EnemyMoveEntry { card = pattern[index % pattern.Count] };
        }

        return null;
    }

    public int GetNextActionIndex(int currentIndex)
    {
        if (ActionCount == 0)
            return 0;

        var action = GetActionAt(currentIndex);
        if (action == null)
            return 0;

        if (action.nextMoveIndices == null || action.nextMoveIndices.Count == 0)
            return (currentIndex + 1) % ActionCount;

        if (action.nextMoveIndices.Count > 0)
            return GetNextMoveIndex(action);

        return (currentIndex + 1) % ActionCount;
    }

    public int PickRandomActionIndex()
    {
        if (ActionCount == 0)
            return 0;

        int totalWeight = 0;
        for (int i = 0; i < ActionCount; i++)
        {
            var action = GetActionAt(i);
            if (action != null && (action.card != null || (action.effects != null && action.effects.Count > 0)))
            {
                totalWeight += Mathf.Max(1, action.weight);
            }
        }

        if (totalWeight <= 0)
            return 0;

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < ActionCount; i++)
        {
            var action = GetActionAt(i);
            if (action == null || (action.card == null && (action.effects == null || action.effects.Count == 0)))
                continue;

            roll -= Mathf.Max(1, action.weight);
            if (roll < 0)
                return i;
        }

        return 0;
    }
    public int GetNextMoveIndex(EnemyMoveEntry currentMove)
    {
        if (currentMove.nextMoveIndices == null || currentMove.nextMoveIndices.Count == 0)
            return -1;

        int totalWeight = 0;
        foreach (var moveIndex in currentMove.nextMoveIndices)
        {
            var move = GetActionAt(moveIndex);
            if (move != null)
            {
                totalWeight += Mathf.Max(1, move.weight);
            }
        }

        if (totalWeight <= 0)
            return -1;

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < currentMove.nextMoveIndices.Count; i++)
        {
            int moveIndex = currentMove.nextMoveIndices[i];
            var move = GetActionAt(moveIndex);
            if (move == null)
                continue;

            roll -= Mathf.Max(1, move.weight);
            if (roll < 0)
                return moveIndex;
        }

        return -1;
    }
    #if UNITY_EDITOR
    private void OnValidate()
    {
        enemyName = name;
    }
    #endif
}