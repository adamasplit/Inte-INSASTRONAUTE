using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class EnemyDataJsonExporter
{
    private const string SourceJsonPath = "Assets/StreamingAssets/EnemyData/enemies.json";
    private const string TargetFolder = "Assets/Resources/STS/Enemies";

    [MenuItem("Tools/Export STS Enemy Data to ScriptableObjects")]
    public static void ExportEnemyDataToScriptableObjects()
    {
        if (!File.Exists(SourceJsonPath))
        {
            Debug.LogError($"Enemy JSON not found at '{SourceJsonPath}'.");
            return;
        }

        string json = File.ReadAllText(SourceJsonPath);
        EnemyDataWrapper wrapper = JsonConvert.DeserializeObject<EnemyDataWrapper>(json);
        if (wrapper == null || wrapper.enemies == null)
        {
            Debug.LogError("Failed to deserialize enemy JSON.");
            return;
        }

        Dictionary<string, STSCardData> cardLookup = BuildCardLookup();
        EnsureFolderExists(TargetFolder);

        int exportedCount = 0;
        foreach (EnemyDataDTO dto in wrapper.enemies)
        {
            if (dto == null)
            {
                continue;
            }

            string fileName = string.IsNullOrWhiteSpace(dto.id) ? dto.enemyName : dto.id;
            string assetPath = $"{TargetFolder}/{fileName}.asset";
            EnemyData enemyAsset = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
            bool isNewAsset = enemyAsset == null;

            if (isNewAsset)
            {
                enemyAsset = ScriptableObject.CreateInstance<EnemyData>();
            }

            PopulateEnemyAsset(enemyAsset, dto, cardLookup);

            if (isNewAsset)
            {
                AssetDatabase.CreateAsset(enemyAsset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(enemyAsset);
            }

            exportedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Exported {exportedCount} enemies to '{TargetFolder}'.");
    }

    private static void PopulateEnemyAsset(EnemyData enemy, EnemyDataDTO dto, Dictionary<string, STSCardData> cardLookup)
    {
        enemy.name = !string.IsNullOrWhiteSpace(dto.id) ? dto.id : dto.enemyName;
        enemy.id = !string.IsNullOrWhiteSpace(dto.id) ? dto.id : enemy.name;
        enemy.enemyName = !string.IsNullOrWhiteSpace(dto.enemyName) ? dto.enemyName : enemy.name;
        enemy.displayName = dto.displayName;
        enemy.maxHP = dto.maxHP;
        enemy.randomStart = dto.randomStart;
        enemy.pattern = ResolveCardList(dto.patternCardIds, cardLookup, dto.id, "pattern");
        enemy.movePattern = ResolveMovePattern(dto.movePattern, cardLookup, dto.id);
        enemy.rewardCards = ResolveCardList(dto.rewardCardIds, cardLookup, dto.id, "reward");

        if (!string.IsNullOrWhiteSpace(dto.startingStatus) &&
            Enum.TryParse(dto.startingStatus, out StatusType parsedStatus))
        {
            enemy.startingStatus = parsedStatus;
        }

        enemy.startingStatusDuration = dto.startingStatusDuration;
        enemy.startingStatusValue = dto.startingStatusValue;
        enemy.startingStatusInfo = dto.startingStatusInfo;
    }

    private static List<STSCardData> ResolveCardList(List<string> cardIds, Dictionary<string, STSCardData> cardLookup, string enemyId, string listName)
    {
        List<STSCardData> cards = new();
        if (cardIds == null)
        {
            return cards;
        }

        foreach (string cardId in cardIds)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                continue;
            }

            if (cardLookup.TryGetValue(cardId, out STSCardData card))
            {
                cards.Add(card);
            }
            else
            {
                Debug.LogWarning($"Enemy '{enemyId}' references missing {listName} card '{cardId}'.");
            }
        }

        return cards;
    }

    private static List<EnemyMoveEntry> ResolveMovePattern(List<EnemyMoveEntryDTO> moveDtos, Dictionary<string, STSCardData> cardLookup, string enemyId)
    {
        List<EnemyMoveEntry> moves = new();
        if (moveDtos == null)
        {
            return moves;
        }

        foreach (EnemyMoveEntryDTO moveDto in moveDtos)
        {
            if (moveDto == null)
            {
                continue;
            }

            EnemyMoveEntry move = new()
            {
                moveName = moveDto.moveName,
                weight = moveDto.weight
            };

            if (!string.IsNullOrWhiteSpace(moveDto.cardId) && cardLookup.TryGetValue(moveDto.cardId, out STSCardData card))
            {
                move.card = card;
            }
            else if (!string.IsNullOrWhiteSpace(moveDto.cardId))
            {
                Debug.LogWarning($"Enemy '{enemyId}' references missing move card '{moveDto.cardId}'.");
            }

            if (moveDto.effects != null)
            {
                move.effects = new List<EffectEntry>();
                foreach (EffectEntryDTO effectDto in moveDto.effects)
                {
                    if (effectDto != null)
                    {
                        move.effects.Add(EffectEntry.FromDTO(effectDto));
                    }
                }
            }

            if (moveDto.nextMoveIndices != null)
            {
                move.nextMoveIndices = new List<int>(moveDto.nextMoveIndices);
            }

            moves.Add(move);
        }

        return moves;
    }

    private static Dictionary<string, STSCardData> BuildCardLookup()
    {
        Dictionary<string, STSCardData> lookup = new(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:STSCardData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            STSCardData card = AssetDatabase.LoadAssetAtPath<STSCardData>(path);
            if (card == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(card.id))
            {
                lookup[card.id] = card;
            }

            if (!string.IsNullOrWhiteSpace(card.cardName))
            {
                lookup[card.cardName] = card;
            }
        }

        return lookup;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    [Serializable]
    private class EnemyDataWrapper
    {
        public List<EnemyDataDTO> enemies;
    }
}