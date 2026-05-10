using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
public class CircularMenu : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Items")]
    public List<RectTransform> items= new List<RectTransform>();

    [Header("Layout")]
    public float radius = 500f;
    public float verticalScale = 0.35f;

    [Header("Scale")]
    public float minScale = 0.6f;
    public float maxScale = 1.2f;

    [Header("Drag")]
    public float dragSensitivity = 0.005f;
    public float inertiaDamping = 5f;

    [Header("Snap")]
    public bool snapToClosest = true;
    public float snapSpeed = 5f;

    float rotationOffset;
    float velocity;

    bool dragging;
    float targetRotation;
bool snapping;

    void Start()
    {
        foreach (Transform child in transform)
        {
            if (child is RectTransform rt)
            {
                items.Add(rt);
            }
        }
        ArrangeItems();
    }
    void Update()
    {
        if (!dragging)
        {
            rotationOffset += velocity * Time.deltaTime;

            velocity = Mathf.Lerp(
                velocity,
                0,
                Time.deltaTime * inertiaDamping
            );

            if (snapToClosest && Mathf.Abs(velocity) < 0.001f)
            {
                if (!snapping)
                {
                    ComputeSnapTarget();
                }

                rotationOffset = Mathf.Lerp(
                    rotationOffset,
                    targetRotation,
                    Time.deltaTime * snapSpeed
                );

                if (Mathf.Abs(rotationOffset - targetRotation) < 0.001f)
                {
                    rotationOffset = targetRotation;
                    snapping = false;
                }
            }
        }

        ArrangeItems();
    }

    void ArrangeItems()
    {
        List<(RectTransform rect, float depth)> ordered =
            new List<(RectTransform, float)>();

        int count = items.Count;

        for (int i = 0; i < count; i++)
        {
            float angle =
                ((float)i / count) * Mathf.PI * 2f
                + rotationOffset;

            float x = Mathf.Cos(angle) * radius;
            float y = -Mathf.Sin(angle) * radius * verticalScale;

            Vector2 targetPos = new Vector2(x, y);

            items[i].anchoredPosition =
                Vector2.Lerp(
                    items[i].anchoredPosition,
                    targetPos,
                    Time.deltaTime * 10f
                );

            // profondeur simulée
            float depth = (-y / radius + 1f) * 0.5f;

            // scale
            float scale = Mathf.Lerp(minScale, maxScale, depth);
            items[i].localScale = Vector3.one * scale;

            ordered.Add((items[i], depth));
        }

        // TRI profondeur
        ordered.Sort((a, b) => a.depth.CompareTo(b.depth));

        // appliquer ordre visuel
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].rect.SetSiblingIndex(i);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        snapping = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = -eventData.delta.x;

        rotationOffset += delta * dragSensitivity;

        velocity = delta * dragSensitivity * 5f;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    void ComputeSnapTarget()
    {
        int closestIndex = 0;
        float bestDepth = 999f;

        int count = items.Count;

        for (int i = 0; i < count; i++)
        {
            float angle =
                ((float)i / count) * Mathf.PI * 2f
                + rotationOffset;

            float y = Mathf.Sin(angle);

            if (y < bestDepth)
            {
                bestDepth = y;
                closestIndex = i;
            }
        }

        targetRotation =
            -((float)closestIndex / count)
            * Mathf.PI * 2f
            - Mathf.PI / 2f;

        snapping = true;
    }
}