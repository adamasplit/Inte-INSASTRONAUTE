using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MapView : MonoBehaviour
{
    public ScrollRect scrollRect; // Assign in inspector
    public MapManager mapManager;
    public GameObject nodePrefab;
    public Transform mapPanel;
    public Transform linesContainer;
    public Camera uiCamera;
    public Material lineMaterial;

    public float spacingX = 200f;
    public float spacingY = 250f;
    public float referenceViewportWidth = 1080f;
    public float referenceViewportHeight = 1920f;
    public float minVisualScale = 0.85f;
    public float maxVisualScale = 1.35f;
    public float nodeScaleMultiplier = 1f;
    public float lineWidthMultiplier = 0.14f;
    [Range(0f, 0.5f)] public float horizontalNoiseRatio = 0.1f;
    [Range(0f, 0.5f)] public float verticalNoiseRatio = 0.06f;

    private Dictionary<MapNode, RectTransform> nodeToUI = new();
    private Dictionary<MapNode, NodeView> nodeToView = new();
    private readonly List<LineEntry> lineEntries = new();

    private class LineEntry
    {
        public MapNode source;
        public MapNode target;
        public LineRenderer renderer;
    }

    public void GenerateView(List<MapNode> allNodes)
    {
        // clear UI
        foreach (Transform child in mapPanel)
            Destroy(child.gameObject);

        foreach (Transform child in linesContainer)
            Destroy(child.gameObject);

        nodeToUI.Clear();
        nodeToView.Clear();
        lineEntries.Clear();
        int maxFloor = 0;
        Canvas.ForceUpdateCanvases();
        RectTransform parentRect = mapPanel.parent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            Canvas.ForceUpdateCanvases();
        }

        float panelWidth = GetAvailableMapWidth();
        float viewportHeight = GetAvailableMapHeight();
        float visualScale = GetVisualScale(panelWidth, viewportHeight);
        float horizontalSpan = Mathf.Max(panelWidth - spacingX, 0f);
        // 1. Spawn nodes
        foreach (var node in allNodes)
        {
            GameObject obj = Instantiate(nodePrefab, mapPanel);
            RectTransform rt = obj.GetComponent<RectTransform>();

            // Use posX for natural horizontal placement (0..1 mapped to panel width)
            float xPos = (node.posX - 0.5f) * horizontalSpan; // center at 0, spread out
            Vector2 visualOffset = GetNodeVisualOffset(node, rt, xPos, panelWidth, horizontalSpan, visualScale);
            rt.anchoredPosition = new Vector2(
                xPos + visualOffset.x,
                node.floor * spacingY + visualOffset.y
            );
            if (node.type==NodeType.Start)
            {
                rt.anchoredPosition = new Vector2(0f, 0f);
            }
            if (node.floor > maxFloor) maxFloor = node.floor;

            nodeToUI[node] = rt;

            NodeView nodeView = obj.GetComponent<NodeView>() 
                                ?? obj.GetComponentInChildren<NodeView>();

            nodeView.Init(node, mapManager, GetNodeScale(node, visualScale), RefreshView);
            nodeToView[node] = nodeView;
        }
        mapPanel.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(mapPanel.parent.GetComponent<RectTransform>().sizeDelta.x, (maxFloor + 1) * spacingY + 400f);
        // 2. Draw connections
        DrawConnections(allNodes);

        RefreshView();

        // Scroll to current node if possible
        if (mapManager != null && mapManager.currentNode != null && scrollRect != null && nodeToUI.ContainsKey(mapManager.currentNode))
        {
            RectTransform nodeRT = nodeToUI[mapManager.currentNode];
            // Convert node position to viewport space
            Vector2 nodePos = nodeRT.anchoredPosition;
            float contentHeight = ((RectTransform)mapPanel).rect.height;
            float viewportHeight2 = scrollRect.viewport.rect.height;
            // Calculate normalized scroll position (0 = bottom, 1 = top)
            float targetY = nodePos.y;
            float normalized = Mathf.Clamp01((targetY - viewportHeight2 / 2) / (contentHeight - viewportHeight2));
            // In Unity, verticalNormalizedPosition: 1 = top, 0 = bottom
            scrollRect.verticalNormalizedPosition = 1f - normalized;
        }
    }

    float GetAvailableMapWidth()
    {
        float result = 0f;
        string source = "none";
        
        if (scrollRect != null && scrollRect.viewport != null)
        {
            float viewportWidth = scrollRect.viewport.rect.width;
            if (viewportWidth > 0f)
            {
                result = viewportWidth;
                source = "viewport";
            }
        }

        if (result <= 0f)
        {
            RectTransform parentRect = mapPanel.parent as RectTransform;
            if (parentRect != null && parentRect.rect.width > 0f)
            {
                result = parentRect.rect.width;
                source = "parent";
            }
        }

        if (result <= 0f)
        {
            RectTransform mapRect = mapPanel as RectTransform;
            if (mapRect != null && mapRect.rect.width > 0f)
            {
                result = mapRect.rect.width;
                source = "mapPanel";
            }
        }
        return result;
    }

    float GetAvailableMapHeight()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            float viewportHeight = scrollRect.viewport.rect.height;
            if (viewportHeight > 0f)
                return viewportHeight;
        }

        RectTransform parentRect = mapPanel.parent as RectTransform;
        if (parentRect != null && parentRect.rect.height > 0f)
            return parentRect.rect.height;

        RectTransform mapRect = mapPanel as RectTransform;
        if (mapRect != null && mapRect.rect.height > 0f)
            return mapRect.rect.height;

        return referenceViewportHeight;
    }

    float GetVisualScale(float viewportWidth, float viewportHeight)
    {
        float widthScale = referenceViewportWidth > 0f ? viewportWidth / referenceViewportWidth : 1f;
        float heightScale = referenceViewportHeight > 0f ? viewportHeight / referenceViewportHeight : 1f;
        return Mathf.Clamp(Mathf.Min(widthScale, heightScale), minVisualScale, maxVisualScale);
    }

    float GetNodeScale(MapNode node, float visualScale)
    {
        float typeScale = node.type == NodeType.Boss ? 1.5f : 1f;
        return typeScale * nodeScaleMultiplier * visualScale;
    }

    Vector2 GetNodeVisualOffset(MapNode node, RectTransform nodeRect, float baseX, float panelWidth, float horizontalSpan, float visualScale)
    {
        if (node.type == NodeType.Start)
            return Vector2.zero;

        float horizontalAmplitude = horizontalSpan * horizontalNoiseRatio;
        float verticalAmplitude = spacingY * verticalNoiseRatio;

        float noiseX = Mathf.PerlinNoise(node.id * 0.173f, node.floor * 0.619f);
        float noiseY = (Mathf.PerlinNoise(node.id * 0.431f, node.floor * 0.287f + 37f) - 0.5f) * 2f;

        float inwardDirection = baseX > 0f ? -1f : 1f;
        if (Mathf.Approximately(baseX, 0f))
            inwardDirection = Mathf.PerlinNoise(node.id * 0.719f, node.floor * 0.151f) > 0.5f ? -1f : 1f;

        float desiredOffsetX = noiseX * horizontalAmplitude * visualScale * inwardDirection;
        float halfNodeWidth = nodeRect != null ? nodeRect.rect.width * nodeRect.localScale.x * 0.5f : 0f;
        float horizontalPadding = halfNodeWidth + (spacingX * 0.08f);
        float halfPanelWidth = panelWidth * 0.5f;
        float minX = -halfPanelWidth + horizontalPadding;
        float maxX = halfPanelWidth - horizontalPadding;
        float clampedOffsetX = Mathf.Clamp(baseX + desiredOffsetX, minX, maxX) - baseX;

        return new Vector2(
            clampedOffsetX,
            noiseY * verticalAmplitude * visualScale
        );
    }

    void DrawConnections(List<MapNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!nodeToUI.ContainsKey(node)) continue;

            foreach (var next in node.next)
            {
                if (!nodeToUI.ContainsKey(next)) continue;

                CreateLine(node, next, nodeToUI[node], nodeToUI[next]);
            }
        }
    }
    void CreateLine(MapNode source, MapNode target, RectTransform a, RectTransform b)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(linesContainer, false);

        LineRenderer lr = line.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.useWorldSpace = false;

        Vector3 start = UIToLocal(a);
        Vector3 end = UIToLocal(b);

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        // MATERIAL
        lr.material = lineMaterial;

        // WIDTH (taper)
        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.2f)
        );
        lr.widthMultiplier = lineWidthMultiplier * GetVisualScale(GetAvailableMapWidth(), GetAvailableMapHeight());

        // COLOR + GLOW (HDR)
        Color glow = new Color(0.2f, 0.6f, 1f) * 3f;

        lr.startColor = glow;
        lr.endColor = glow * 0.5f;
        lr.sortingLayerName = "UI";

        lineEntries.Add(new LineEntry
        {
            source = source,
            target = target,
            renderer = lr
        });
    }
    public void RefreshView()
    {
        if (mapManager == null || mapManager.currentNode == null)
            return;

        foreach (var kvp in nodeToUI)
        {
            var node = kvp.Key;

            if (!nodeToView.TryGetValue(node, out var nodeView) || nodeView == null)
                continue;

            bool isReachable = mapManager.currentNode.next.Contains(node);

            nodeView.SetAccessibilityState(isReachable, 1f);
        }

        RefreshLines();
    }

    void RefreshLines()
    {
        if (mapManager == null || mapManager.currentNode == null)
            return;

        foreach (var entry in lineEntries)
        {
            // Always show all lines
            entry.renderer.gameObject.SetActive(true);
        }
    }

    Vector3 UIToLocal(RectTransform rt)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            linesContainer as RectTransform,
            RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position),
            uiCamera,
            out localPos
        );

        return localPos;
    }

    void Update()
    {
        float pulse = Mathf.Lerp(0.5f, 3f, Mathf.PingPong(Time.time, 1f));
        float accessiblePulse = 1f + Mathf.Sin(Time.time * 4f) * 0.045f;

        if (mapManager != null && mapManager.currentNode != null)
        {
            foreach (var kvp in nodeToView)
            {
                var node = kvp.Key;
                var nodeView = kvp.Value;

                if (nodeView == null)
                    continue;

                bool isReachable = mapManager.currentNode.next.Contains(node);
                nodeView.SetAccessibilityState(isReachable, isReachable ? accessiblePulse : 1f);
            }
        }

        foreach (var entry in lineEntries)
        {
            var lr = entry.renderer;
            if (lr == null || !lr.gameObject.activeSelf) continue;

            bool isCrossableNow = mapManager != null && mapManager.currentNode != null && entry.source == mapManager.currentNode;
            bool isAlreadyCrossed = entry.source.visited && entry.target.visited;
            if (isCrossableNow || isAlreadyCrossed)
            {
                Color baseColor = new Color(0.2f, 0.6f, 1f);
                lr.startColor = baseColor * pulse;
                lr.endColor = baseColor * (pulse * 0.5f);
            }
            else
            {
                // Dim white for inaccessible lines
                Color dimWhite = new Color(1f, 1f, 1f, 0.2f);
                lr.startColor = dimWhite;
                lr.endColor = dimWhite;
            }
        }
    }
}