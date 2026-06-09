using UnityEngine;

public class STSTutorialHighlight : MonoBehaviour
{
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;

    Rect target;
    Rect current;

    bool active;

    float speed = 5f;

    Canvas cachedCanvas;
    RectTransform cachedCanvasRect;
    Camera cachedCanvasCamera;

    void Awake()
    {
        CacheCanvas();
    }

    void OnEnable()
    {
        CacheCanvas();
    }

    void CacheCanvas()
    {
        cachedCanvas = top != null ? top.GetComponentInParent<Canvas>() : null;
        cachedCanvasRect = cachedCanvas != null ? cachedCanvas.transform as RectTransform : null;
        cachedCanvasCamera = GetCanvasCamera(cachedCanvas);
    }

    void Update()
    {
        if (!active) return;

        current = Lerp(current, target, Time.deltaTime * speed);
        Apply(current);
    }
    public void Highlight(HighlightTarget t, float padding = 0f)
    {
        if (!t.valid) return;

        Highlight(t.screenRect, padding);
    }

    public void Highlight(Rect r, float padding = 0f)
    {
        if (padding > 0f)
            r = Expand(r, padding);

        target = r;

        if (!active)
            current = r;

        active = true;

        Set(true);
        Apply(current);
    }

    Rect Lerp(Rect a, Rect b, float t)
    {
        return new Rect(
            Mathf.Lerp(a.x, b.x, t),
            Mathf.Lerp(a.y, b.y, t),
            Mathf.Lerp(a.width, b.width, t),
            Mathf.Lerp(a.height, b.height, t)
        );
    }

    Rect Expand(Rect r, float padding)
    {
        return Rect.MinMaxRect(
            r.xMin - padding,
            r.yMin - padding,
            r.xMax + padding,
            r.yMax + padding
        );
    }

    

    void Apply(Rect r)
    {
        if (cachedCanvas == null || cachedCanvasRect == null)
            CacheCanvas();

        if (cachedCanvasRect == null)
            return;

        Rect canvasRect = cachedCanvasRect.rect;
        float canvasLeft = canvasRect.xMin;
        float canvasRight = canvasRect.xMax;
        float canvasBottom = canvasRect.yMin;
        float canvasTop = canvasRect.yMax;

        Vector2 bottomLeft = ScreenToCanvasPoint(new Vector2(r.xMin, r.yMin));
        Vector2 topRight = ScreenToCanvasPoint(new Vector2(r.xMax, r.yMax));

        float xMin = Mathf.Clamp(bottomLeft.x, canvasLeft, canvasRight);
        float yMin = Mathf.Clamp(bottomLeft.y, canvasBottom, canvasTop);
        float xMax = Mathf.Clamp(topRight.x, canvasLeft, canvasRight);
        float yMax = Mathf.Clamp(topRight.y, canvasBottom, canvasTop);

        SetPanel(top, canvasLeft, yMax, canvasRight, canvasTop);
        SetPanel(bottom, canvasLeft, canvasBottom, canvasRight, yMin);
        SetPanel(left, canvasLeft, yMin, xMin, yMax);
        SetPanel(right, xMax, yMin, canvasRight, yMax);
    }

    void SetPanel(RectTransform panel, float xMin, float yMin, float xMax, float yMax)
    {
        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        panel.sizeDelta = new Vector2(width, height);
    }

    Vector2 ScreenToCanvasPoint(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cachedCanvasRect,
            screen,
            cachedCanvasCamera,
            out var local
        );

        return local;
    }

    static Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    void Set(bool v)
    {
        top.gameObject.SetActive(v);
        bottom.gameObject.SetActive(v);
        left.gameObject.SetActive(v);
        right.gameObject.SetActive(v);
    }

    public void Hide()
    {
        active = false;
        Set(false);
    }
}