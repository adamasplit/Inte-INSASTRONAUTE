using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class MapGenerator
{
    public int width = 5;
    public int height = 10;
    public int pathCount = 4;

    public MapNode startNode;
    private List<MapNode> allNodes = new();
    HashSet<Vector2Int> occupied = new();

    public List<MapNode> Generate(int startingFloor = 0)
    {
        allNodes = new List<MapNode>();
        startNode = new MapNode
        {
            id = 0,
            floor = startingFloor,
            x = width / 2,
            type = NodeType.Start,
            next = new List<MapNode>(),
            prev = new List<MapNode>()
        };
        List<MapNode> previousFloorNodes = new List<MapNode> { startNode };
        allNodes.Add(startNode);
        this.startNode = startNode;
        for (int y = 1; y < height; y++)
        {
            int nodeCount = GetNodeCountForFloor(y);

            List<int> slots = new List<int>();
            for (int i = 0; i < width; i++)
                slots.Add(i);

            List<MapNode> floorNodes = new();

            for (int i = 0; i < nodeCount; i++)
            {
                int slotIndex = Random.Range(0, slots.Count);
                int x = slots[slotIndex];
                slots.RemoveAt(slotIndex);

                var node = new MapNode
                {
                    id = allNodes.Count,
                    floor = y,
                    x = x,
                    type = GetRandomNodeType(y),
                    next = new(),
                    prev = new()
                };

                allNodes.Add(node);
                floorNodes.Add(node);
            }

            ConnectToPreviousFloor(previousFloorNodes, floorNodes);
            previousFloorNodes = floorNodes;
        }

        return allNodes;
    }
    public NodeType GetRandomNodeType(int floor)
    {
        if (floor == height - 1) return NodeType.Boss;

        int roll = Random.Range(0, 100);

        if (roll < 50) return NodeType.Combat;
        if (roll < 70) return NodeType.Elite;
        if (roll < 85) return NodeType.Rest;
        return NodeType.Event;
    }

    int GetNodeCountForFloor(int floor)
    {
        if (floor == height - 1) return 1; // boss
        if (floor == 1) return Random.Range(2, 4); // early
        if (floor < height / 2) return Random.Range(2, 5);
        return Random.Range(1, 4);
    }

    void ConnectToPreviousFloor(List<MapNode> prev, List<MapNode> curr)
    {
        // (OPTIONNEL mais recommandé si tu fais des contraintes avancées)
        Dictionary<MapNode, int> prevCount = new();
        Dictionary<MapNode, int> currCount = new();

        foreach (var p in prev) prevCount[p] = 0;
        foreach (var c in curr) currCount[c] = 0;

        // =========================
        // 1. GARANTIE MINIMALE (CRUCIAL)
        // chaque node prev doit avoir AU MOINS un next
        // =========================
        foreach (var p in prev)
        {
            var c = curr[Random.Range(0, curr.Count)];

            Connect(p, c, prevCount, currCount);
        }

        // =========================
        // 2. GARANTIE MINIMALE inverse (sécurité)
        // chaque node curr doit avoir AU MOINS un prev
        // =========================
        foreach (var c in curr)
        {
            if (c.prev.Count == 0)
            {
                var p = prev[Random.Range(0, prev.Count)];
                Connect(p, c, prevCount, currCount);
            }
        }

        // =========================
        // 3. LIENS EXTRA (variété)
        // =========================
        int extraLinks = Random.Range(0, prev.Count / 2);

        for (int i = 0; i < extraLinks; i++)
        {
            var p = prev[Random.Range(0, prev.Count)];
            var c = curr[Random.Range(0, curr.Count)];

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