using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    public MapNode currentNode;
    public MapView view;
    public MapGenerator generator = new();
    public Image blackOverlay;

    private System.Collections.Generic.List<MapNode> allNodes;

    void Awake()
    {
        StartCoroutine(FadeFromBlack(0.5f));
        if (RunManager.Instance!=null && RunManager.Instance.map != null&&!RunManager.Instance.RegenerateMap)
        {
            allNodes = RunManager.Instance.map;
            currentNode = RunManager.Instance.currentNode;
            view.GenerateView(allNodes);
            return;
        }
        var map = generator.Generate(RunManager.Instance != null&&RunManager.Instance.RegenerateMap ? RunManager.Instance.currentFloor : 0);
        if (RunManager.Instance!=null)
        {
            RunManager.Instance.currentNode = generator.startNode;
            RunManager.Instance.RegenerateMap = false;
            RunManager.Instance.map = map;
            RunManager.Instance.player.Heal(RunManager.Instance.player.maxHP);
        }
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

        StartCoroutine(ResolveNode(node));

        view.RefreshView();
    }

    IEnumerator ResolveNode(MapNode node)
    {
        yield return StartCoroutine(FadeToBlack(0.2f));
        node.visited = true;
        switch (node.type)
        {
            case NodeType.Combat:
                SceneManager.LoadScene("STS_Combat");
                break;

            case NodeType.Rest:
                RunManager.Instance.restCharges = RunManager.Instance.maxRestCharges;
                SceneManager.LoadScene("STS_Rest");
                break;

            case NodeType.Event:
                //SceneManager.LoadScene("STS_Event");
                StopAllCoroutines();
                yield return StartCoroutine(FadeFromBlack(0.5f));
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

    IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            blackOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        blackOverlay.color = Color.black;
    }
    IEnumerator FadeFromBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            blackOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        blackOverlay.color = new Color(0f, 0f, 0f, 0f);
    }
}