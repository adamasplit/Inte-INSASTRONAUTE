using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class STSApiRunCreateRequest
{
    public string character;
    public string clientVersion;
}

[Serializable]
public class STSApiRunCreateResponse
{
    public string runId;
    public string status;
    public string dataVersion;
    public string selectedCharacter;
    public int act;
    public int currentFloor;
    public int currentNodeId;
    public int? enteredNodeId;
    public STSApiPlayerState player;
    public STSApiRunInventoryState runInventory;
    public STSApiMapState map;
    public List<JToken> pendingRewards = new();
    public JToken activeEvent;
}

[Serializable]
public class STSApiPlayerState
{
    public int maxHp;
    public int currentHp;
}

[Serializable]
public class STSApiRunInventoryState
{
    public int gold;
    public List<STSApiCardState> deck = new();
    public List<STSApiRelicState> relics = new();
}

[Serializable]
public class STSApiCardState
{
    public string instanceId;
    public string cardId;
    public string targetingMode;
    public List<STSApiEnchantmentState> enchantments = new();
}

[Serializable]
public class STSApiEnchantmentState
{
    public string enchantmentClass;
    public int level;
}

[Serializable]
public class STSApiRelicState
{
    public string instanceId;
    public string relicId;
    public int stage;
}

[Serializable]
public class STSApiMapState
{
    public List<STSApiMapNodeState> nodes = new();
}

[Serializable]
public class STSApiMapNodeState
{
    public int id;
    public int floor;
    public string type;
    public float posX;
    public bool visited;
    public bool completed;
    public List<int> nextIds = new();
}

[Serializable]
public class STSApiNodeEnterRequest
{
    public string runId;
    public int nodeId;
}

[Serializable]
public class STSApiNodeEnterResponse
{
    public bool accepted;
    public string runId;
    public int nodeId;
    public string nodeType;
    public STSApiActiveEncounterState activeEncounter;
    public JToken activeEvent;
    public string eventId;
}

[Serializable]
public class STSApiActiveEncounterState
{
    public string encounterInstanceId;
    public string encounterId;
    public List<string> enemyIds = new();
    public int playerHpBefore;
    public string startedAt;
}

[Serializable]
public class STSApiNodeCompleteRequest
{
    public string encounterInstanceId;
    public string result;
    public int turnCount;
    public int playerHpAfter;
    public int damageTaken;
    public List<string> enemiesDefeated = new();
    public string deckHash;
}

[Serializable]
public class STSApiNodeCompleteResponse
{
    public bool accepted;
    public string runId;
    public int currentNodeId;
    public STSApiPlayerState player;
    public JToken runInventoryPatch;
    public JToken accountInventoryPatch;
    public List<JToken> pendingRewards = new();
    public STSApiMapPatchState mapPatch;
}

[Serializable]
public class STSApiMapPatchState
{
    public List<int> visitedNodeIds = new();
    public List<int> completedNodeIds = new();
    public int currentNodeId;
    public int? enteredNodeId;
}

[Serializable]
public class STSApiRunRetireResponse
{
    public bool accepted;
    public string runId;
    public string status;
    public long score;
    public long tokensGranted;
    public long tokenBalance;
    public long visitedNodeScore;
    public long combatVictoryScore;
    public long eliteVictoryScore;
    public long eventVisitedScore;
    public long actReachedScore;
    public long relicOwnedScore;
    public long deckCardScore;
    public long goldOwnedScore;
    public long remainingHpPercentScore;
    public long scorePerToken;
    public string rounding;
    public long minimumReward;
}

[Serializable]
public class STSApiRetreatPreviewResponse
{
    public bool accepted;
    public string runId;
    public string status;
    public long score;
    public long tokensPreview;
    public long projectedTokenBalance;
    public long visitedNodeScore;
    public long combatVictoryScore;
    public long eliteVictoryScore;
    public long eventVisitedScore;
    public long actReachedScore;
    public long relicOwnedScore;
    public long deckCardScore;
    public long goldOwnedScore;
    public long remainingHpPercentScore;
    public long scorePerToken;
    public string rounding;
    public long minimumReward;
}

public static class STSApiClient
{
    public static async Task<STSApiRunCreateResponse> CreateRunAsync(string character, string clientVersion)
    {
        string json = await ReactApiBridge.RequestAsync(
            "sts.runs.create",
            new STSApiRunCreateRequest
            {
                character = character,
                clientVersion = clientVersion
            }
        );

        return ParseResponse<STSApiRunCreateResponse>(json);
    }

    public static async Task<bool> ResetRunAsync(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return false;

        string json = await ReactApiBridge.RequestAsync("sts.runs.reset", new { runId });
        if (string.IsNullOrWhiteSpace(json))
            return false;

        JToken token = ParseEnvelope(json);
        if (token == null)
            return true;

        return token.Type != JTokenType.Boolean || token.Value<bool>();
    }

    public static async Task<STSApiRunRetireResponse> RetireRunAsync(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return null;

        string json = await ReactApiBridge.RequestAsync("sts.runs.retire", new { runId });
        return ParseResponse<STSApiRunRetireResponse>(json);
    }

    public static async Task<STSApiRetreatPreviewResponse> RetreatPreviewAsync(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return null;

        string json = await ReactApiBridge.RequestWithAliasesAsync(
            new[]
            {
                $"sts.runs.{runId}.retreat-preview",
                "sts.runs.retreat-preview"
            },
            new { runId }
        );

        return ParseResponse<STSApiRetreatPreviewResponse>(json);
    }

    public static async Task<STSApiNodeEnterResponse> EnterNodeAsync(string runId, int nodeId)
    {
        string json = await ReactApiBridge.RequestAsync(
            $"sts.runs.{runId}.nodes.{nodeId}.enter",
            new STSApiNodeEnterRequest
            {
                runId = runId,
                nodeId = nodeId
            }
        );

        return ParseResponse<STSApiNodeEnterResponse>(json);
    }

    public static async Task<STSApiNodeCompleteResponse> CompleteNodeAsync(string runId, int nodeId, STSApiNodeCompleteRequest request)
    {
        string json = await ReactApiBridge.RequestAsync(
            $"sts.runs.{runId}.nodes.{nodeId}.complete",
            request
        );

        return ParseResponse<STSApiNodeCompleteResponse>(json);
    }

    public static STSApiRunState ConvertToRunState(STSApiRunCreateResponse response)
    {
        if (response == null)
            return null;

        var state = new STSApiRunState
        {
            runId = response.runId,
            status = response.status,
            dataVersion = response.dataVersion,
            selectedCharacter = response.selectedCharacter,
            act = response.act,
            currentFloor = response.currentFloor,
            currentNodeId = response.currentNodeId,
            enteredNodeId = response.enteredNodeId,
            playerMaxHp = response.player != null ? response.player.maxHp : 0,
            playerCurrentHp = response.player != null ? response.player.currentHp : 0,
            gold = response.runInventory != null ? response.runInventory.gold : 0,
            deck = ConvertDeck(response.runInventory != null ? response.runInventory.deck : null),
            relics = ConvertRelics(response.runInventory != null ? response.runInventory.relics : null),
            map = ConvertMap(response.map != null ? response.map.nodes : null)
        };

        return state;
    }

    public static List<MapNode> ConvertMap(List<STSApiMapNodeState> nodes)
    {
        var mapNodes = new List<MapNode>();
        if (nodes == null || nodes.Count == 0)
            return mapNodes;

        var byId = new Dictionary<int, MapNode>();
        foreach (STSApiMapNodeState nodeState in nodes)
        {
            if (nodeState == null)
                continue;

            var node = new MapNode
            {
                id = nodeState.id,
                floor = nodeState.floor,
                type = Enum.TryParse(nodeState.type, out NodeType parsedType) ? parsedType : NodeType.Combat,
                posX = nodeState.posX,
                x = Mathf.RoundToInt(nodeState.posX * 4f),
                visited = nodeState.visited,
                completed = nodeState.completed
            };

            byId[node.id] = node;
            mapNodes.Add(node);
        }

        foreach (STSApiMapNodeState nodeState in nodes)
        {
            if (nodeState == null || !byId.TryGetValue(nodeState.id, out MapNode node))
                continue;

            if (nodeState.nextIds == null)
                continue;

            foreach (int nextId in nodeState.nextIds)
            {
                if (!byId.TryGetValue(nextId, out MapNode nextNode))
                    continue;

                if (!node.next.Contains(nextNode))
                    node.next.Add(nextNode);
                if (!nextNode.prev.Contains(node))
                    nextNode.prev.Add(node);
            }
        }

        return mapNodes.OrderBy(n => n.id).ToList();
    }

    public static List<CardInstance> ConvertDeck(List<STSApiCardState> cards)
    {
        var deck = new List<CardInstance>();
        if (cards == null)
            return deck;

        foreach (STSApiCardState cardState in cards)
        {
            CardInstance card = ConvertCard(cardState);
            if (card != null)
                deck.Add(card);
        }

        return deck;
    }

    public static CardInstance ConvertCard(STSApiCardState cardState)
    {
        if (cardState == null || string.IsNullOrWhiteSpace(cardState.cardId))
            return null;

        STSCardData cardData = STSCardDatabase.Get(cardState.cardId);
        if (cardData == null)
            return null;

        var card = new CardInstance(cardData)
        {
            instanceId = string.IsNullOrWhiteSpace(cardState.instanceId) ? Guid.NewGuid().ToString("N") : cardState.instanceId
        };

        if (Enum.TryParse(cardState.targetingMode, out TargetingMode targetingMode))
        {
            card.targetingMode = targetingMode;
        }

        if (cardState.enchantments != null)
        {
            foreach (STSApiEnchantmentState enchantmentState in cardState.enchantments)
            {
                if (enchantmentState == null || string.IsNullOrWhiteSpace(enchantmentState.enchantmentClass))
                    continue;

                CardEnchantment enchantment = CreateEnchantment(enchantmentState.enchantmentClass, enchantmentState.level);
                if (enchantment != null)
                    card.enchantments.Add(enchantment);
            }
        }

        return card;
    }

    public static List<Relic> ConvertRelics(List<STSApiRelicState> relics)
    {
        var result = new List<Relic>();
        if (relics == null)
            return result;

        foreach (STSApiRelicState relicState in relics)
        {
            Relic relic = CreateRelic(relicState?.relicId);
            if (relic == null)
                continue;

            if (relic is BaseRelic baseRelic && relicState != null)
            {
                baseRelic.stage = relicState.stage;
            }

            result.Add(relic);
        }

        return result;
    }

    public static string ComputeDeckHash(IEnumerable<CardInstance> deck)
    {
        if (deck == null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (CardInstance card in deck)
        {
            if (card == null)
                continue;

            builder.Append(card.instanceId ?? string.Empty);
            builder.Append('|');
            builder.Append(card.data != null ? card.data.cardName : string.Empty);
            builder.Append('|');
            builder.Append(card.targetingMode);
            builder.Append('|');
            builder.Append(card.enchantments != null ? card.enchantments.Count : 0);
            builder.Append(';');
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static T ParseResponse<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            JToken token = ParseEnvelope(json);
            if (token == null)
                return null;

            if (token.Type == JTokenType.Object)
                return token.ToObject<T>();

            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse API payload for {typeof(T).Name}: {ex.Message}");
            return null;
        }
    }

    private static JToken ParseEnvelope(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            JToken root = JToken.Parse(json);
            if (root.Type != JTokenType.Object)
                return root;

            JObject obj = (JObject)root;

            // React bridge envelope shape: { id, ok, data?, error? }
            if (obj.TryGetValue("ok", StringComparison.OrdinalIgnoreCase, out JToken okToken)
                && okToken != null
                && okToken.Type == JTokenType.Boolean)
            {
                bool ok = okToken.Value<bool>();
                if (!ok)
                {
                    string errorText = obj.TryGetValue("error", StringComparison.OrdinalIgnoreCase, out JToken errorToken)
                        ? errorToken?.ToString(Formatting.None)
                        : "unknown bridge error";
                    Debug.LogWarning($"Bridge request returned ok=false: {errorText}");
                    return null;
                }

                if (obj.TryGetValue("data", StringComparison.OrdinalIgnoreCase, out JToken bridgeData)
                    && bridgeData != null
                    && bridgeData.Type != JTokenType.Null)
                {
                    return bridgeData;
                }
            }

            foreach (string key in new[] { "data", "result", "payload" })
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nested) && nested != null && nested.Type != JTokenType.Null)
                {
                    return nested;
                }
            }

            return root;
        }
        catch
        {
            return null;
        }
    }

    private static CardEnchantment CreateEnchantment(string enchantmentClass, int level)
    {
        Type enchantmentType = FindTypeByName(enchantmentClass);
        if (enchantmentType == null || !typeof(EnchantmentData).IsAssignableFrom(enchantmentType))
            return null;

        try
        {
            var data = Activator.CreateInstance(enchantmentType) as EnchantmentData;
            if (data == null)
                return null;

            return new CardEnchantment
            {
                data = data,
                level = level
            };
        }
        catch
        {
            return null;
        }
    }

    private static Relic CreateRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        Type relicType = FindTypeByName(relicId);
        if (relicType == null || !typeof(Relic).IsAssignableFrom(relicType))
            return null;

        try
        {
            return Activator.CreateInstance(relicType) as Relic;
        }
        catch
        {
            return null;
        }
    }

    private static Type FindTypeByName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.Ordinal));
            if (type != null)
                return type;
        }

        return null;
    }
}

[Serializable]
public class STSApiRunState
{
    public string runId;
    public string status;
    public string dataVersion;
    public string selectedCharacter;
    public int act;
    public int currentFloor;
    public int currentNodeId;
    public int? enteredNodeId;
    public int playerMaxHp;
    public int playerCurrentHp;
    public int gold;
    public List<CardInstance> deck = new();
    public List<Relic> relics = new();
    public List<MapNode> map = new();
    public STSApiActiveEncounterState activeEncounter;
    public JToken activeEvent;
}