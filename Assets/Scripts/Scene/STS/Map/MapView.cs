using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MapView : MonoBehaviour
{
    public MapManager mapManager;
    public GameObject nodePrefab;
    public Transform mapPanel;
    public Transform linesContainer;

    public float spacingX = 200f;
    public float spacingY = 250f;

    private Dictionary<MapNode, RectTransform> nodeToUI = new();

    public void GenerateView(List<MapNode> allNodes)
    {
        // clear UI
        foreach (Transform child in mapPanel)
            Destroy(child.gameObject);

        nodeToUI.Clear();
        int maxFloor = 0;
        // 1. Spawn nodes
        foreach (var node in allNodes)
        {
            GameObject obj = Instantiate(nodePrefab, mapPanel);
            RectTransform rt = obj.GetComponent<RectTransform>();

            // POSITION (basée sur graph, pas grille)
            rt.anchoredPosition = new Vector2(
                (node.x - 2) * spacingX,
                node.floor * spacingY
            );
            if (node.floor > maxFloor) maxFloor = node.floor;

            nodeToUI[node] = rt;

            NodeView nodeView = obj.GetComponent<NodeView>() 
                                ?? obj.GetComponentInChildren<NodeView>();

            nodeView.Init(node, mapManager, RefreshView);
        }
        mapPanel.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(mapPanel.parent.GetComponent<RectTransform>().sizeDelta.x, (maxFloor + 1) * spacingY + 100f);
        // 2. Draw connections
        DrawConnections(allNodes);

        RefreshView();
    }
    void DrawConnections(List<MapNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!nodeToUI.ContainsKey(node)) continue;

            foreach (var next in node.next)
            {
                if (!nodeToUI.ContainsKey(next)) continue;

                CreateLine(nodeToUI[node], nodeToUI[next]);
            }
        }
    }
    void CreateLine(RectTransform a, RectTransform b)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(linesContainer, false);

        Image img = line.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.6f);

        RectTransform rt = line.GetComponent<RectTransform>();

        Vector2 dir = b.anchoredPosition - a.anchoredPosition;
        float dist = dir.magnitude;

        rt.sizeDelta = new Vector2(6f, dist);
        rt.anchoredPosition = a.anchoredPosition + dir * 0.5f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
    public void RefreshView()
    {
        if (mapManager == null || mapManager.currentNode == null)
            return;

        foreach (var kvp in nodeToUI)
        {
            var node = kvp.Key;
            var rt = kvp.Value;

            var nodeView = rt.GetComponent<NodeView>();

            bool isReachable = mapManager.currentNode.next.Contains(node);

            nodeView.button.interactable = isReachable;

            nodeView.icon.color = isReachable
                ? Color.white
                : new Color(1f, 1f, 1f, 0.4f);
        }
    }
}