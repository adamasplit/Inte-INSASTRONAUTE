using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class STSRunAuditData
{
    public int version = 1;
    public string runId;
    public string createdAtUtc;
    public string updatedAtUtc;
    public string endedAtUtc;
    public string applicationVersion;
    public string unityVersion;
    public List<STSRunAuditEvent> events = new();
}

[Serializable]
public class STSRunAuditEvent
{
    public string eventId;
    public string eventType;
    public string timestampUtc;
    public string sceneName;
    public string reason;
    public int currentFloor;
    public int act;
    public int currentNodeId;
    public int sourceNodeId = -1;
    public int targetNodeId = -1;
    public MapNodeAuditSnapshot sourceNode;
    public MapNodeAuditSnapshot targetNode;
    public STSRunAuditSnapshot snapshot;
}

[Serializable]
public class STSRunAuditSnapshot
{
    public string selectedCharacter;
    public bool forceTutorial;
    public bool regenerateMap;
    public bool inCombat;
    public bool eliteEncounter;
    public bool bossEncounter;
    public int currentFloor;
    public int act;
    public int restCharges;
    public int maxRestCharges;
    public int gold;
    public int currentNodeId;
    public int deckCount;
    public int relicCount;
    public PlayerAuditSnapshot player;
}

[Serializable]
public class PlayerAuditSnapshot
{
    public string name;
    public int maxHP;
    public int currentHP;
    public int armor;
    public int energy;
    public int bp;
}

[Serializable]
public class MapNodeAuditSnapshot
{
    public int id;
    public int floor;
    public string type;
    public int x;
    public float posX;
    public bool visited;
    public List<int> nextIds = new();
    public List<int> prevIds = new();
}

public static class STSRunAuditSystem
{
    private const string AuditFolderName = "sts_run_audit";

    private static string AuditDirectory => Path.Combine(Application.persistentDataPath, AuditFolderName);

    private static string GetAuditPath(string runId)
    {
        return Path.Combine(AuditDirectory, $"sts_run_audit_{runId}.json");
    }

    public static string EnsureRunId(RunManager run)
    {
        if (run == null)
            return null;

        if (string.IsNullOrWhiteSpace(run.runId))
        {
            run.runId = Guid.NewGuid().ToString("N");
        }

        return run.runId;
    }

    public static void RecordRunStarted(RunManager run)
    {
        if (run == null)
            return;

        string runId = EnsureRunId(run);
        if (string.IsNullOrWhiteSpace(runId))
            return;

        STSRunAuditData data = LoadOrCreate(runId);
        if (string.IsNullOrWhiteSpace(data.createdAtUtc))
        {
            data.createdAtUtc = UtcNow();
        }

        AppendEvent(data, new STSRunAuditEvent
        {
            eventType = "RunStarted",
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            reason = "start_run",
            currentFloor = run.currentFloor,
            act = run.act,
            currentNodeId = run.currentNode != null ? run.currentNode.id : -1,
            snapshot = CaptureSnapshot(run)
        });

        Save(runId, data);
    }

    public static void RecordNodeEntered(RunManager run, MapNode node, string sceneName, string reason = null)
    {
        RecordNodeEvent(run, "NodeEntered", null, node, sceneName, reason);
    }

    public static void RecordNodeExited(RunManager run, MapNode sourceNode, MapNode targetNode, string sceneName, string reason = null)
    {
        RecordNodeEvent(run, "NodeExited", sourceNode, targetNode, sceneName, reason);
    }

    public static void RecordRunEnded(RunManager run, string reason = null)
    {
        if (run == null || string.IsNullOrWhiteSpace(run.runId))
            return;

        STSRunAuditData data = LoadOrCreate(run.runId);
        data.endedAtUtc = UtcNow();

        AppendEvent(data, new STSRunAuditEvent
        {
            eventType = "RunEnded",
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            reason = reason,
            currentFloor = run.currentFloor,
            act = run.act,
            currentNodeId = run.currentNode != null ? run.currentNode.id : -1,
            snapshot = CaptureSnapshot(run)
        });

        Save(run.runId, data);
    }

    private static void RecordNodeEvent(RunManager run, string eventType, MapNode sourceNode, MapNode targetNode, string sceneName, string reason)
    {
        if (run == null || string.IsNullOrWhiteSpace(run.runId))
            return;

        STSRunAuditData data = LoadOrCreate(run.runId);
        AppendEvent(data, new STSRunAuditEvent
        {
            eventType = eventType,
            sceneName = sceneName,
            reason = reason,
            currentFloor = run.currentFloor,
            act = run.act,
            currentNodeId = run.currentNode != null ? run.currentNode.id : -1,
            sourceNodeId = sourceNode != null ? sourceNode.id : -1,
            targetNodeId = targetNode != null ? targetNode.id : -1,
            sourceNode = CaptureNode(sourceNode),
            targetNode = CaptureNode(targetNode),
            snapshot = CaptureSnapshot(run)
        });

        Save(run.runId, data);
    }

    private static STSRunAuditData LoadOrCreate(string runId)
    {
        Directory.CreateDirectory(AuditDirectory);

        string path = GetAuditPath(runId);
        if (!File.Exists(path))
        {
            return new STSRunAuditData
            {
                runId = runId,
                createdAtUtc = UtcNow(),
                updatedAtUtc = UtcNow(),
                applicationVersion = Application.version,
                unityVersion = Application.unityVersion
            };
        }

        try
        {
            STSRunAuditData data = JsonConvert.DeserializeObject<STSRunAuditData>(File.ReadAllText(path));
            if (data == null)
            {
                data = new STSRunAuditData();
            }

            data.runId = string.IsNullOrWhiteSpace(data.runId) ? runId : data.runId;
            data.applicationVersion = Application.version;
            data.unityVersion = Application.unityVersion;
            data.events ??= new List<STSRunAuditEvent>();
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to read audit file '{path}': {ex.Message}");
            return new STSRunAuditData
            {
                runId = runId,
                createdAtUtc = UtcNow(),
                updatedAtUtc = UtcNow(),
                applicationVersion = Application.version,
                unityVersion = Application.unityVersion
            };
        }
    }

    private static void AppendEvent(STSRunAuditData data, STSRunAuditEvent auditEvent)
    {
        if (data == null || auditEvent == null)
            return;

        auditEvent.eventId = Guid.NewGuid().ToString("N");
        auditEvent.timestampUtc = UtcNow();
        data.updatedAtUtc = auditEvent.timestampUtc;
        data.events ??= new List<STSRunAuditEvent>();
        data.events.Add(auditEvent);
    }

    private static void Save(string runId, STSRunAuditData data)
    {
        try
        {
            Directory.CreateDirectory(AuditDirectory);
            File.WriteAllText(GetAuditPath(runId), JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to write audit file for run '{runId}': {ex.Message}");
        }
    }

    private static STSRunAuditSnapshot CaptureSnapshot(RunManager run)
    {
        if (run == null)
            return null;

        return new STSRunAuditSnapshot
        {
            selectedCharacter = run.selectedCharacter.ToString(),
            forceTutorial = run.forceTutorial,
            regenerateMap = run.RegenerateMap,
            inCombat = run.inCombat,
            eliteEncounter = run.eliteEncounter,
            bossEncounter = run.bossEncounter,
            currentFloor = run.currentFloor,
            act = run.act,
            restCharges = run.restCharges,
            maxRestCharges = run.maxRestCharges,
            gold = run.gold,
            currentNodeId = run.currentNode != null ? run.currentNode.id : -1,
            deckCount = run.deck != null ? run.deck.Count : 0,
            relicCount = run.relics != null ? run.relics.Count : 0,
            player = CapturePlayer(run.player)
        };
    }

    private static PlayerAuditSnapshot CapturePlayer(Player player)
    {
        if (player == null)
            return null;

        return new PlayerAuditSnapshot
        {
            name = player.name,
            maxHP = player.maxHP,
            currentHP = player.currentHP,
            armor = player.armor,
            energy = player.resources != null ? player.resources.energy : 0,
            bp = player.resources != null ? player.resources.bp : 0
        };
    }

    private static MapNodeAuditSnapshot CaptureNode(MapNode node)
    {
        if (node == null)
            return null;

        var snapshot = new MapNodeAuditSnapshot
        {
            id = node.id,
            floor = node.floor,
            type = node.type.ToString(),
            x = node.x,
            posX = node.posX,
            visited = node.visited
        };

        if (node.next != null)
        {
            foreach (MapNode next in node.next)
            {
                if (next != null)
                    snapshot.nextIds.Add(next.id);
            }
        }

        if (node.prev != null)
        {
            foreach (MapNode prev in node.prev)
            {
                if (prev != null)
                    snapshot.prevIds.Add(prev.id);
            }
        }

        return snapshot;
    }

    private static string UtcNow()
    {
        return DateTime.UtcNow.ToString("o");
    }
}