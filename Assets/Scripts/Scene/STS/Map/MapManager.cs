using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class MapManager : MonoBehaviour
{
    public MapNode currentNode;
    public MapView view;
    public MapGenerator generator = new();

    private System.Collections.Generic.List<MapNode> allNodes;

    void Awake()
    {
        if (RunManager.Instance!=null && RunManager.Instance.map != null&&!RunManager.Instance.RegenerateMap)
        {
            allNodes = RunManager.Instance.map;
            currentNode = RunManager.Instance.currentNode;
            view.GenerateView(allNodes);
            return;
        }
        var map = generator.Generate(RunManager.Instance != null&&RunManager.Instance.RegenerateMap ? RunManager.Instance.currentFloor : 0);
        RunManager.Instance.currentNode = generator.startNode;
        RunManager.Instance.RegenerateMap = false;
        RunManager.Instance.map = map;
        allNodes = map;
        Debug.Log($"Generated map with {allNodes.Count} nodes");
        currentNode = generator.startNode;

        view.GenerateView(allNodes);
    }

    public void MoveToNode(MapNode node)
    {
        if (currentNode == null)
        {
            Debug.LogError("currentNode is null");
            return;
        }

        if (!currentNode.next.Contains(node))
        {
            string allowed = currentNode.next.Count == 0
                ? "none"
                : string.Join(", ", currentNode.next.Select(n => n.id.ToString()));

            Debug.LogWarning(
                $"Blocked: {node.id}, allowed: {allowed}");
            return;
        }

        currentNode = node;
        RunManager.Instance.currentNode = currentNode;

        ResolveNode(node);

        view.RefreshView();
    }

    void ResolveNode(MapNode node)
    {
        node.visited = true;
        switch (node.type)
        {
            case NodeType.Combat:
                SceneManager.LoadScene("STS_Combat");
                break;

            case NodeType.Rest:
                break;

            case NodeType.Event:
                break;
            case NodeType.Elite:
                RunManager.Instance.eliteEncounter = true;
                SceneManager.LoadScene("STS_Combat");
                break;
            case NodeType.Boss:
                RunManager.Instance.bossEncounter = true;
                SceneManager.LoadScene("STS_Combat");
                break;
            default:
                Debug.LogError($"Unknown node type: {node.type}");
                break;
        }
    }
}