using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class STSRunSaveData
{
    public int version = 1;
    public string runId;
    public string selectedCharacter;
    public bool forceTutorial;
    public int act;
    public int currentFloor;
    public int restCharges;
    public int maxRestCharges;
    public int gold;
    public bool eliteEncounter;
    public bool bossEncounter;
    public bool regenerateMap;
    public int currentNodeId = -1;
    public PlayerSaveData player;
    public List<CardSaveData> deck = new();
    public List<RelicSaveData> relics = new();
    public List<MapNodeSaveData> map = new();
}

[Serializable]
public class PlayerSaveData
{
    public string name;
    public int maxHP;
    public int currentHP;
    public int armor;
    public int energy;
    public int bp;
    public List<StatusSaveData> statusEffects = new();
}

[Serializable]
public class CardSaveData
{
    public string instanceId;
    public string cardId;
    public string targetingMode;
    public string lastDescription;
    public List<ModifierSaveData> addedModifiers = new();
    public List<EffectEntryDTO> addedEffects = new();
    public List<CardEnchantmentSaveData> enchantments = new();
    public List<string> tags = new();
}

[Serializable]
public class ModifierSaveData
{
    public string modifierClass;
    public string statType;
    public string modifierType;
    public int value;
}

[Serializable]
public class CardEnchantmentSaveData
{
    public string enchantmentClass;
    public int level;
}

[Serializable]
public class RelicSaveData
{
    public string relicClass;
    public int stage;
}

[Serializable]
public class MapNodeSaveData
{
    public int id;
    public int floor;
    public string type;
    public int x;
    public float posX;
    public bool visited;
    public bool completed;
    public List<int> nextIds = new();
    public List<int> prevIds = new();
}

[Serializable]
public class StatusSaveData
{
    public string statusType;
    public int value;
    public int duration;
    public int index;
    public string effectInfo;
}

public static class STSRunSaveSystem
{
    private const string SaveFileName = "sts_run_save.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasLoadableSave()
    {
        if (!File.Exists(SavePath))
            return false;

        try
        {
            return JsonConvert.DeserializeObject<STSRunSaveData>(File.ReadAllText(SavePath))?.player != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Invalid save file at '{SavePath}': {ex.Message}");
            return false;
        }
    }

    public static bool SaveRun(RunManager run)
    {
        if (run == null || run.player == null || run.map == null || run.map.Count == 0)
        {
            Debug.LogWarning("SaveRun called without a complete run state.");
            return false;
        }

        try
        {
            STSRunSaveData data = Capture(run);
            File.WriteAllText(SavePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save run to '{SavePath}': {ex}");
            return false;
        }
    }

    public static bool LoadRun(RunManager run)
    {
        if (run == null)
            return false;

        if (!File.Exists(SavePath))
            return false;

        try
        {
            STSRunSaveData data = JsonConvert.DeserializeObject<STSRunSaveData>(File.ReadAllText(SavePath));
            if (data == null || data.player == null)
                return false;

            Apply(run, data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load run from '{SavePath}': {ex}");
            return false;
        }
    }

    public static bool ClearSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to delete save file '{SavePath}': {ex.Message}");
            return false;
        }
    }

    private static STSRunSaveData Capture(RunManager run)
    {
        var data = new STSRunSaveData
        {
            runId = run.runId,
            selectedCharacter = run.selectedCharacter.ToString(),
            forceTutorial = run.forceTutorial,
            act = run.act,
            currentFloor = run.currentFloor,
            restCharges = run.restCharges,
            maxRestCharges = run.maxRestCharges,
            gold = run.gold,
            eliteEncounter = run.eliteEncounter,
            bossEncounter = run.bossEncounter,
            regenerateMap = run.RegenerateMap,
            currentNodeId = run.currentNode != null ? run.currentNode.id : -1,
            player = CapturePlayer(run.player)
        };

        if (run.deck != null)
        {
            foreach (CardInstance card in run.deck)
            {
                if (card != null)
                    data.deck.Add(CaptureCard(card));
            }
        }

        if (run.relics != null)
        {
            foreach (Relic relic in run.relics)
            {
                if (relic != null)
                    data.relics.Add(CaptureRelic(relic));
            }
        }

        if (run.map != null)
        {
            foreach (MapNode node in run.map)
            {
                if (node != null)
                    data.map.Add(CaptureNode(node));
            }
        }

        return data;
    }

    private static PlayerSaveData CapturePlayer(Player player)
    {
        if (player == null)
            return null;

        var data = new PlayerSaveData
        {
            name = player.name,
            maxHP = player.maxHP,
            currentHP = player.currentHP,
            armor = player.armor,
            energy = player.resources != null ? player.resources.energy : 0,
            bp = player.resources != null ? player.resources.bp : 0
        };

        foreach (StatusEffect status in player.statusEffects)
        {
            if (status == null)
                continue;

            string statusType = status.GetType().Name;
            if (statusType.EndsWith("Status", StringComparison.OrdinalIgnoreCase))
            {
                statusType = statusType.Substring(0, statusType.Length - "Status".Length);
            }

            data.statusEffects.Add(new StatusSaveData
            {
                statusType = statusType,
                value = status.Value,
                duration = status.Duration,
                effectInfo = status.Name
            });
        }

        return data;
    }

    private static CardSaveData CaptureCard(CardInstance card)
    {
        var data = new CardSaveData
        {
            instanceId = card.instanceId,
            cardId = card.data != null ? card.data.cardName : null,
            targetingMode = card.targetingMode.ToString(),
            lastDescription = card.lastDescription
        };

        foreach (StatModifier modifier in card.addedModifiers)
        {
            if (modifier == null)
                continue;

            data.addedModifiers.Add(new ModifierSaveData
            {
                modifierClass = modifier.GetType().Name,
                statType = modifier.type.ToString(),
                modifierType = modifier.modifierType.ToString(),
                value = ReadModifierValue(modifier)
            });
        }

        foreach (EffectEntry effect in card.addedEffects)
        {
            if (effect != null)
                data.addedEffects.Add(effect.ToDTO());
        }

        foreach (CardEnchantment enchantment in card.enchantments)
        {
            if (enchantment?.data == null)
                continue;

            data.enchantments.Add(new CardEnchantmentSaveData
            {
                enchantmentClass = enchantment.data.GetType().Name,
                level = enchantment.level
            });
        }

        if (card.tags != null)
        {
            foreach (CardTag tag in card.tags)
            {
                data.tags.Add(tag.ToString());
            }
        }

        return data;
    }

    private static RelicSaveData CaptureRelic(Relic relic)
    {
        return new RelicSaveData
        {
            relicClass = relic.GetType().Name,
            stage = relic is BaseRelic baseRelic ? baseRelic.stage : 0
        };
    }

    private static MapNodeSaveData CaptureNode(MapNode node)
    {
        var data = new MapNodeSaveData
        {
            id = node.id,
            floor = node.floor,
            type = node.type.ToString(),
            x = node.x,
            posX = node.posX,
            visited = node.visited,
            completed = node.completed
        };

        foreach (MapNode next in node.next)
        {
            if (next != null)
                data.nextIds.Add(next.id);
        }

        foreach (MapNode prev in node.prev)
        {
            if (prev != null)
                data.prevIds.Add(prev.id);
        }

        return data;
    }

    private static void Apply(RunManager run, STSRunSaveData data)
    {
        run.forceTutorial = data.forceTutorial;
        run.act = data.act;
        run.currentFloor = data.currentFloor;
        run.restCharges = data.restCharges;
        run.maxRestCharges = data.maxRestCharges;
        run.gold = data.gold;
        run.eliteEncounter = data.eliteEncounter;
        run.bossEncounter = data.bossEncounter;
        run.RegenerateMap = data.regenerateMap;

        if (Enum.TryParse(data.selectedCharacter, out SelectableCharacter selectedCharacter))
        {
            run.selectedCharacter = selectedCharacter;
        }
        else
        {
            run.selectedCharacter = SelectableCharacter.Aucun;
        }

        run.runId = string.IsNullOrWhiteSpace(data.runId) ? Guid.NewGuid().ToString("N") : data.runId;

        run.player = RestorePlayer(data.player);
        run.deck = RestoreDeck(data.deck);
        run.relics = RestoreRelics(data.relics);
        run.map = RestoreMap(data.map, data.currentNodeId, out MapNode currentNode);
        run.currentNode = currentNode;
        run.pendingReward = null;

        if (run.ui != null)
        {
            run.ui.gameObject.SetActive(true);
        }
    }

    private static Player RestorePlayer(PlayerSaveData data)
    {
        if (data == null)
            return null;

        var player = new Player(data.name, data.maxHP)
        {
            currentHP = data.currentHP,
            armor = data.armor
        };

        player.resources.energy = data.energy;
        player.resources.bp = data.bp;
        player.statusEffects.Clear();

        foreach (StatusSaveData statusData in data.statusEffects)
        {
            StatusEffect status = RestoreStatus(statusData);
            if (status == null)
                continue;

            player.AddStatus(status);
            status.Value = statusData.value;
            status.Duration = statusData.duration;
            status.Update(player);
        }

        return player;
    }

    private static StatusEffect RestoreStatus(StatusSaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.statusType))
            return null;

        if (!Enum.TryParse(data.statusType, out StatusType statusType))
            return null;

        return StatusEffect.Factory(statusType, data.value, data.duration, data.effectInfo,data.index);
    }

    private static List<CardInstance> RestoreDeck(List<CardSaveData> savedDeck)
    {
        var deck = new List<CardInstance>();
        if (savedDeck == null)
            return deck;

        foreach (CardSaveData cardData in savedDeck)
        {
            CardInstance card = RestoreCard(cardData);
            if (card != null)
                deck.Add(card);
        }

        return deck;
    }

    private static CardInstance RestoreCard(CardSaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.cardId))
            return null;

        STSCardData cardData = STSCardDatabase.Get(data.cardId);
        if (cardData == null)
            return null;

        var card = new CardInstance(cardData);
        card.instanceId = string.IsNullOrWhiteSpace(data.instanceId) ? Guid.NewGuid().ToString("N") : data.instanceId;

        if (Enum.TryParse(data.targetingMode, out TargetingMode targetingMode))
        {
            card.targetingMode = targetingMode;
        }

        card.addedModifiers.Clear();
        foreach (ModifierSaveData modifierData in data.addedModifiers)
        {
            StatModifier modifier = RestoreModifier(modifierData);
            if (modifier != null)
                card.addedModifiers.Add(modifier);
        }

        card.addedEffects.Clear();
        foreach (EffectEntryDTO effectDto in data.addedEffects)
        {
            if (effectDto != null)
                card.addedEffects.Add(EffectEntry.FromDTO(effectDto));
        }

        card.enchantments.Clear();
        foreach (CardEnchantmentSaveData enchantmentData in data.enchantments)
        {
            CardEnchantment enchantment = RestoreEnchantment(enchantmentData);
            if (enchantment != null)
                card.enchantments.Add(enchantment);
        }

        card.tags.Clear();
        foreach (string tag in data.tags)
        {
            if (Enum.TryParse(tag, out CardTag parsedTag))
            {
                card.tags.Add(parsedTag);
            }
        }

        card.lastDescription = data.lastDescription ?? string.Empty;
        return card;
    }

    private static StatModifier RestoreModifier(ModifierSaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.modifierClass))
            return null;

        if (!Enum.TryParse(data.statType, out StatType statType))
            return null;

        Type modifierType = FindTypeByName(data.modifierClass);
        if (modifierType == null || !typeof(StatModifier).IsAssignableFrom(modifierType))
            return null;

        StatModifier modifier = null;
        try
        {
            modifier = Activator.CreateInstance(modifierType, statType, data.value) as StatModifier;
        }
        catch
        {
            modifier = null;
        }

        if (modifier == null)
        {
            modifier = Activator.CreateInstance(modifierType) as StatModifier;
            if (modifier == null)
                return null;

            modifier.type = statType;
            SetModifierValue(modifier, data.value);
        }

        if (Enum.TryParse(data.modifierType, out ModifierType parsedModifierType))
        {
            modifier.modifierType = parsedModifierType;
        }

        return modifier;
    }

    private static CardEnchantment RestoreEnchantment(CardEnchantmentSaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.enchantmentClass))
            return null;

        Type enchantmentType = FindTypeByName(data.enchantmentClass);
        if (enchantmentType == null || !typeof(EnchantmentData).IsAssignableFrom(enchantmentType))
            return null;

        EnchantmentData enchantmentData = Activator.CreateInstance(enchantmentType) as EnchantmentData;
        if (enchantmentData == null)
            return null;

        return new CardEnchantment
        {
            data = enchantmentData,
            level = data.level
        };
    }

    private static List<Relic> RestoreRelics(List<RelicSaveData> relicsData)
    {
        var relics = new List<Relic>();
        if (relicsData == null)
            return relics;

        foreach (RelicSaveData relicData in relicsData)
        {
            Relic relic = RestoreRelic(relicData);
            if (relic != null)
                relics.Add(relic);
        }

        return relics;
    }

    private static Relic RestoreRelic(RelicSaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.relicClass))
            return null;

        Type relicType = FindTypeByName(data.relicClass);
        if (relicType == null || !typeof(Relic).IsAssignableFrom(relicType))
            return null;

        Relic relic = Activator.CreateInstance(relicType) as Relic;
        if (relic is BaseRelic baseRelic)
        {
            baseRelic.Upgrade(Mathf.Max(0, data.stage));
        }

        return relic;
    }

    private static List<MapNode> RestoreMap(List<MapNodeSaveData> nodesData, int currentNodeId, out MapNode currentNode)
    {
        var nodes = new List<MapNode>();
        currentNode = null;

        if (nodesData == null || nodesData.Count == 0)
            return nodes;

        var nodeById = new Dictionary<int, MapNode>();
        foreach (MapNodeSaveData nodeData in nodesData)
        {
            if (nodeData == null)
                continue;

            if (!Enum.TryParse(nodeData.type, out NodeType nodeType))
                continue;

            var node = new MapNode
            {
                id = nodeData.id,
                floor = nodeData.floor,
                type = nodeType,
                x = nodeData.x,
                posX = nodeData.posX,
                visited = nodeData.visited,
                completed = nodeData.completed,
                next = new List<MapNode>(),
                prev = new List<MapNode>()
            };

            nodeById[node.id] = node;
            nodes.Add(node);
        }

        foreach (MapNodeSaveData nodeData in nodesData)
        {
            if (nodeData == null || !nodeById.TryGetValue(nodeData.id, out MapNode node))
                continue;

            foreach (int nextId in nodeData.nextIds)
            {
                if (nodeById.TryGetValue(nextId, out MapNode nextNode) && !node.next.Contains(nextNode))
                {
                    node.next.Add(nextNode);
                }
            }

            foreach (int prevId in nodeData.prevIds)
            {
                if (nodeById.TryGetValue(prevId, out MapNode prevNode) && !node.prev.Contains(prevNode))
                {
                    node.prev.Add(prevNode);
                }
            }
        }

        if (currentNodeId >= 0 && nodeById.TryGetValue(currentNodeId, out MapNode savedCurrentNode))
        {
            currentNode = savedCurrentNode;
        }

        return nodes;
    }

    private static int ReadModifierValue(StatModifier modifier)
    {
        var field = modifier.GetType()
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .FirstOrDefault(f => f.FieldType == typeof(int) && f.Name != nameof(StatModifier.type) && f.Name != nameof(StatModifier.modifierType));

        if (field == null)
            return 0;

        object value = field.GetValue(modifier);
        return value is int intValue ? intValue : 0;
    }

    private static void SetModifierValue(StatModifier modifier, int value)
    {
        var field = modifier.GetType()
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .FirstOrDefault(f => f.FieldType == typeof(int) && f.Name != nameof(StatModifier.type) && f.Name != nameof(StatModifier.modifierType));

        if (field != null)
        {
            field.SetValue(modifier, value);
        }
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type match = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (match != null)
                return match;
        }

        return null;
    }
}