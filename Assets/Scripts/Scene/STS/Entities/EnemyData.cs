using UnityEngine;
using System.Collections.Generic;
using System;
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

    public EnemyMoveEntryDTO ToDTO()
    {
        var dto = new EnemyMoveEntryDTO
        {
            cardId = card != null ? (!string.IsNullOrEmpty(card.id) ? card.id : card.cardName) : null,
            moveName = moveName,
            weight = weight
        };

        if (effects != null)
        {
            foreach (var effect in effects)
            {
                dto.effects.Add(effect.ToDTO());
            }
        }

        if (nextMoveIndices != null)
        {
            dto.nextMoveIndices.AddRange(nextMoveIndices);
        }

        return dto;
    }

    public static EnemyMoveEntry FromDTO(EnemyMoveEntryDTO dto)
    {
        var entry = new EnemyMoveEntry
        {
            moveName = dto.moveName,
            weight = dto.weight
        };

        if (!string.IsNullOrEmpty(dto.cardId))
        {
            entry.card = STSCardDatabase.Get(dto.cardId);
        }

        if (dto.effects != null)
        {
            entry.effects = new List<EffectEntry>();
            foreach (var effectDto in dto.effects)
            {
                entry.effects.Add(EffectEntry.FromDTO(effectDto));
            }
        }

        if (dto.nextMoveIndices != null)
        {
            entry.nextMoveIndices = new List<int>(dto.nextMoveIndices);
        }

        return entry;
    }
}

[CreateAssetMenu(menuName = "Combat/Enemy")]
public class EnemyData : ScriptableObject
{
    public string id;
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
    public int startingStatusIndex;
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

        // Return a random index besides the current one
        return UnityEngine.Random.Range(0, ActionCount - 1) + (currentIndex + 1) % ActionCount;
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

        int roll = UnityEngine.Random.Range(0, totalWeight);
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

        int roll = UnityEngine.Random.Range(0, totalWeight);
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

    public EnemyDataDTO ToDTO()
    {
        var dto = new EnemyDataDTO
        {
            id = !string.IsNullOrEmpty(id) ? id : name,
            enemyName = enemyName,
            displayName = displayName,
            maxHP = maxHP,
            randomStart = randomStart,
            startingStatus = startingStatus.ToString(),
            startingStatusDuration = startingStatusDuration,
            startingStatusValue = startingStatusValue,
            startingStatusInfo = startingStatusInfo
        };

        if (pattern != null)
        {
            foreach (var card in pattern)
            {
                dto.patternCardIds.Add(card != null ? (!string.IsNullOrEmpty(card.id) ? card.id : card.cardName) : null);
            }
        }

        if (movePattern != null)
        {
            foreach (var move in movePattern)
            {
                if (move != null)
                {
                    dto.movePattern.Add(move.ToDTO());
                }
            }
        }

        if (rewardCards != null)
        {
            foreach (var card in rewardCards)
            {
                dto.rewardCardIds.Add(card != null ? (!string.IsNullOrEmpty(card.id) ? card.id : card.cardName) : null);
            }
        }

        return dto;
    }

    public static EnemyData FromDTO(EnemyDataDTO dto)
    {
        var enemy = ScriptableObject.CreateInstance<EnemyData>();
        enemy.name = !string.IsNullOrEmpty(dto.id) ? dto.id : dto.enemyName;
        enemy.id = !string.IsNullOrEmpty(dto.id) ? dto.id : enemy.name;
        enemy.enemyName = !string.IsNullOrEmpty(dto.enemyName) ? dto.enemyName : enemy.name;
        enemy.displayName = dto.displayName;
        enemy.maxHP = dto.maxHP;
        enemy.randomStart = dto.randomStart;

        enemy.pattern = new List<STSCardData>();
        if (dto.patternCardIds != null)
        {
            foreach (var cardId in dto.patternCardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                var card = STSCardDatabase.Get(cardId);
                if (card != null)
                {
                    enemy.pattern.Add(card);
                }
            }
        }

        enemy.movePattern = new List<EnemyMoveEntry>();
        if (dto.movePattern != null)
        {
            foreach (var moveDto in dto.movePattern)
            {
                if (moveDto != null)
                {
                    enemy.movePattern.Add(EnemyMoveEntry.FromDTO(moveDto));
                }
            }
        }

        enemy.rewardCards = new List<STSCardData>();
        if (dto.rewardCardIds != null)
        {
            foreach (var cardId in dto.rewardCardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                var card = STSCardDatabase.Get(cardId);
                if (card != null)
                {
                    enemy.rewardCards.Add(card);
                }
            }
        }

        if (!string.IsNullOrEmpty(dto.startingStatus) && Enum.TryParse(dto.startingStatus, out StatusType parsedStatus))
        {
            enemy.startingStatus = parsedStatus;
        }

        enemy.startingStatusDuration = dto.startingStatusDuration;
        enemy.startingStatusValue = dto.startingStatusValue;
        enemy.startingStatusInfo = dto.startingStatusInfo;

        return enemy;
    }
    #if UNITY_EDITOR
    private void OnValidate()
    {
        id = name;
        enemyName = name;
    }
    #endif
}