using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class MapGenerator
{
    public int width = 5;
    public int height = 11;
    public int pathCount = 4;

    public MapNode startNode;
    private List<MapNode> allNodes = new();
    HashSet<Vector2Int> occupied = new();

    public List<MapNode> Generate(int startingFloor = 0)
    {
        allNodes = new List<MapNode>();
        // Start node in the center
        startNode = new MapNode
        {
            id = 0,
            floor = startingFloor,
            x = width / 2,
            posX = 0.5f,
            type = NodeType.Start,
            next = new List<MapNode>(),
            prev = new List<MapNode>(),
            visited = true
        };
        List<MapNode> previousFloorNodes = new List<MapNode> { startNode };
        allNodes.Add(startNode);
        for (int y = 1; y < height; y++)
        {
            int nodeCount = GetNodeCountForFloor(y);

            List<MapNode> floorNodes = new();
            // Distribute nodes horizontally with some jitter
            for (int i = 0; i < nodeCount; i++)
            {
                float baseX = (nodeCount == 1) ? 0.5f : (float)i / (nodeCount - 1);
                float jitter = Random.Range(-0.08f, 0.08f); // small random offset
                float posX = Mathf.Clamp01(baseX + jitter);
                var node = new MapNode
                {
                    id = allNodes.Count,
                    floor = y,
                    x = Mathf.RoundToInt(posX * (width - 1)), // legacy
                    posX = posX,
                    type = GetRandomNodeType(y),
                    next = new(),
                    prev = new()
                };
                allNodes.Add(node);
                floorNodes.Add(node);
            }

            ConnectToPreviousFloorNatural(previousFloorNodes, floorNodes);
            previousFloorNodes = floorNodes;
        }

        return allNodes;
    }
    public NodeType GetRandomNodeType(int floor)
    {
        if (floor == height - 1) return NodeType.Boss;

        int roll = Random.Range(0, 100);

        if (roll < 50) return NodeType.Combat;
        if (roll < 60) 
        {
            if (floor<3) return NodeType.Combat; // avoid early elites
            return NodeType.Elite;
        }
        if (roll < 75) return NodeType.Rest;
        return NodeType.Event;
    }

    int GetNodeCountForFloor(int floor)
    {
        if (floor == height - 1) return 1; // boss
        if (floor == 1) return Random.Range(2, 4); // early
        if (floor < height / 2) return Random.Range(2, 5);
        return Random.Range(1, 4);
    }

    // Connect nodes so that links only go to nearby nodes horizontally (like Slay the Spire)
    void ConnectToPreviousFloorNatural(List<MapNode> prev, List<MapNode> curr)
    {
        Dictionary<MapNode, int> prevCount = new();
        Dictionary<MapNode, int> currCount = new();
        foreach (var p in prev) prevCount[p] = 0;
        foreach (var c in curr) currCount[c] = 0;

        float maxDist = 0.35f; // max allowed horizontal distance for a connection

        // 1. Each prev node must have at least one next
        foreach (var p in prev)
        {
            // Find closest curr node within maxDist
            var candidates = curr.Where(c => Mathf.Abs(c.posX - p.posX) <= maxDist).ToList();
            MapNode c;
            if (candidates.Count > 0)
                c = candidates[Random.Range(0, candidates.Count)];
            else
                c = curr[Random.Range(0, curr.Count)];
            Connect(p, c, prevCount, currCount);
        }

        // 2. Each curr node must have at least one prev
        foreach (var c in curr)
        {
            if (c.prev.Count == 0)
            {
                var candidates = prev.Where(p => Mathf.Abs(c.posX - p.posX) <= maxDist).ToList();
                MapNode p;
                if (candidates.Count > 0)
                    p = candidates[Random.Range(0, candidates.Count)];
                else
                    p = prev[Random.Range(0, prev.Count)];
                Connect(p, c, prevCount, currCount);
            }
        }

        // 3. Extra links for variety
        int extraLinks = Random.Range(0, prev.Count / 2);
        for (int i = 0; i < extraLinks; i++)
        {
            var p = prev[Random.Range(0, prev.Count)];
            var candidates = curr.Where(c => Mathf.Abs(c.posX - p.posX) <= maxDist).ToList();
            if (candidates.Count == 0) continue;
            var c = candidates[Random.Range(0, candidates.Count)];
            Connect(p, c, prevCount, currCount);
        }
    }
    void Connect(MapNode a, MapNode b,
        Dictionary<MapNode, int> aCount,
        Dictionary<MapNode, int> bCount)
    {
        if (!a.next.Contains(b))
        {
            a.next.Add(b);
            b.prev.Add(a);

            aCount[a]++;
            bCount[b]++;
        }
    }
    MapNode GetRandomWithLimit(
    List<MapNode> list,
    Dictionary<MapNode, int> count,
    int max)
    {
        for (int i = 0; i < 10; i++)
        {
            var node = list[Random.Range(0, list.Count)];

            if (count[node] < max)
                return node;
        }

        return list[0];
    }
}