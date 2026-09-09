using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class EnemyDataJsonExporter
{
    private const string SourceFolder = "Assets/StreamingAssets/EnemyData";
    private const string TargetFolder = "Assets/Resources/STS/Enemies";

    [MenuItem("Tools/Export STS Enemy Data to ScriptableObjects")]
    public static void ExportEnemyDataToScriptableObjects()
    {
        if (!Directory.Exists(SourceFolder))
        {
            Debug.LogError($"Enemy JSON folder not found at '{SourceFolder}'.");
            return;
        }

        List<EnemyDataDTO> enemies = ReadEnemyDtos(out int fromBundle);
        if (enemies.Count == 0)
        {
            Debug.LogError($"No enemy found in '{SourceFolder}'.");
            return;
        }

        Dictionary<string, STSCardData> cardLookup = BuildCardLookup();
        EnsureFolderExists(TargetFolder);

        int exportedCount = 0;
        foreach (EnemyDataDTO dto in enemies)
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
        Debug.Log($"Exported {exportedCount} enemies to '{TargetFolder}' " +
                  $"({exportedCount - fromBundle} depuis les fichiers par ennemi, {fromBundle} depuis enemies.json).");
    }

    /// <summary>
    /// Les fichiers par ennemi font foi ; enemies.json ne sert que de repli pour ceux qu'ils
    /// ne couvrent pas. Le recueil est régénéré séparément et se retrouve régulièrement en
    /// retard sur les fichiers individuels : le lire seul recréait les assets à partir de
    /// valeurs périmées (Deca, Donu, Asteroid_Adept, Cultist en septembre 2026).
    /// </summary>
    private static List<EnemyDataDTO> ReadEnemyDtos(out int fromBundle)
    {
        List<EnemyDataDTO> perEnemy = new();
        List<EnemyDataDTO> bundled = new();

        string[] files = Directory.GetFiles(SourceFolder, "*.json");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (string file in files)
        {
            JToken token;
            try
            {
                token = JToken.Parse(File.ReadAllText(file));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to parse '{file}': {exception.Message}");
                continue;
            }

            if (token is not JObject obj)
            {
                continue;
            }

            try
            {
                if (obj["enemies"] is JArray array)
                {
                    List<EnemyDataDTO> parsed = array.ToObject<List<EnemyDataDTO>>();
                    if (parsed != null)
                    {
                        bundled.AddRange(parsed);
                    }
                }
                else if (obj["id"] != null || obj["enemyName"] != null)
                {
                    EnemyDataDTO dto = obj.ToObject<EnemyDataDTO>();
                    if (dto != null)
                    {
                        perEnemy.Add(dto);
                    }
                }

                // Les autres JSON du dossier (index.json…) ne décrivent pas d'ennemi.
            }
            catch (JsonException exception)
            {
                Debug.LogError($"Failed to deserialize '{file}': {exception.Message}");
            }
        }

        List<EnemyDataDTO> result = new(perEnemy.Count + bundled.Count);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        fromBundle = 0;

        foreach (EnemyDataDTO dto in perEnemy)
        {
            string key = KeyOf(dto);
            if (key != null && seen.Add(key))
            {
                result.Add(dto);
            }
        }

        foreach (EnemyDataDTO dto in bundled)
        {
            string key = KeyOf(dto);
            if (key != null && seen.Add(key))
            {
                result.Add(dto);
                fromBundle++;
            }
        }

        return result;
    }

    private static string KeyOf(EnemyDataDTO dto)
    {
        if (dto == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.id))
        {
            return dto.id;
        }

        return string.IsNullOrWhiteSpace(dto.enemyName) ? null : dto.enemyName;
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
}