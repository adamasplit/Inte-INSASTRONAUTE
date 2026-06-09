using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class TimelineUI : MonoBehaviour
{
    public Transform panel;
    public Transform container;
    public GameObject iconPrefab;

    public float spacing = 100f;

    List<TurnIcon> icons = new();
    List<TurnEntry> previousDisplayTimeline = null;

    public bool IsAnimating { get; private set; }

    public void Display(List<TurnEntry> timeline, bool preview = false, List<Character> previewCharacters = null)
    {
        bool anyIconMoving = false;
        EnsureIconCount(timeline.Count);
        float referenceTime = timeline.Count > 0 ? timeline[0].time : 0f;

        // Step 1: Calculate X positions for all icons
        List<float> xPositions = new();
        for (int i = 0; i < timeline.Count; i++)
        {
            var entry = timeline[i];
            float x = GetTimelineX(Mathf.Max(0f, entry.time - referenceTime));
            xPositions.Add(x);
        }

        // Step 2: Group indices by X position (with tolerance)
        float overlapTolerance = 1.0f; // pixels
        Dictionary<float, List<int>> xGroups = new();
        for (int i = 0; i < xPositions.Count; i++)
        {
            float x = xPositions[i];
            bool found = false;
            foreach (var key in xGroups.Keys)
            {
                if (Mathf.Abs(key - x) < overlapTolerance)
                {
                    xGroups[key].Add(i);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                xGroups[x] = new List<int> { i };
            }
        }

        // Step 3: Assign Y offsets for stacking
        float stackSpacing = -50f; // vertical distance between stacked icons
        int totalIcons = timeline.Count;
        for (int i = 0; i < totalIcons; i++)
        {
            var entry = timeline[i];
            var icon = icons[i];

            icon.gameObject.SetActive(true);
            icon.Set(entry.character);
            icon.SetPreview(preview);
            icon.SetHighlight(preview && previewCharacters != null && previewCharacters.Contains(entry.character));
            // Find the group for this icon
            float x = xPositions[i];
            float groupKey = 0f;
            List<int> group = null;
            foreach (var kvp in xGroups)
            {
                if (Mathf.Abs(kvp.Key - x) < overlapTolerance)
                {
                    groupKey = kvp.Key;
                    group = kvp.Value;
                    break;
                }
            }

            int stackIndex = group != null ? group.IndexOf(i) : 0;
            int stackCount = group != null ? group.Count : 1;

            // Stack order: first turn at the top (lowest Y)
            float y = 0f;
            if (stackCount > 1)
            {
                float totalHeight = (stackCount - 1) * stackSpacing;
                y = -totalHeight * 0.5f + stackIndex * stackSpacing;
            }

            Vector3 targetPos = new Vector3(x, y, 0);

            // POSITION DE DÉPART (ancienne)
            if (previousDisplayTimeline != null)
            {
                int oldIndex = FindMatchingIndex(previousDisplayTimeline, entry);
                if (oldIndex != -1)
                {
                    float previousReferenceTime = previousDisplayTimeline.Count > 0 ? previousDisplayTimeline[0].time : 0f;
                    float oldX = GetTimelineX(Mathf.Max(0f, previousDisplayTimeline[oldIndex].time - previousReferenceTime));
                    // Find old group and stack index
                    float oldGroupKey = 0f;
                    List<int> oldGroup = null;
                    foreach (var kvp in xGroups)
                    {
                        if (Mathf.Abs(kvp.Key - oldX) < overlapTolerance)
                        {
                            oldGroupKey = kvp.Key;
                            oldGroup = kvp.Value;
                            break;
                        }
                    }
                    int oldStackIndex = oldGroup != null ? oldGroup.IndexOf(oldIndex) : 0;
                    int oldStackCount = oldGroup != null ? oldGroup.Count : 1;
                    float oldY = 0f;
                    if (oldStackCount > 1)
                    {
                        float oldTotalHeight = (oldStackCount - 1) * stackSpacing;
                        oldY = -oldTotalHeight * 0.5f + oldStackIndex * stackSpacing;
                    }
                    Vector3 oldPos = new Vector3(oldX, oldY, 0);
                    icon.transform.localPosition = oldPos;
                }
            }

            icon.SetTargetPosition(targetPos);
            if (!preview && icon.IsMoving())
                anyIconMoving = true;
            icon.SetType(entry.visualType);
            float depth = Mathf.Clamp01(i / (float)timeline.Count);
            icon.SetDepth(depth);
            icon.transform.SetSiblingIndex(timeline.Count - i);
        }

        // désactiver les icônes en trop
        for (int i = timeline.Count; i < icons.Count; i++)
        {
            icons[i].gameObject.SetActive(false);
        }

        if (!preview)
            foreach (var icon in icons)
            {
                if (icon.gameObject.activeSelf && icon.IsMoving())
                {
                    anyIconMoving = true;
                    break;
                }
            }
        IsAnimating = anyIconMoving;
        //   on sauvegarde pour le prochain frame
        if (!preview) previousDisplayTimeline = CloneTimeline(timeline);
    }

    void LateUpdate()
    {
        if (!IsAnimating)
            return;

        for (int i = 0; i < icons.Count; i++)
        {
            var icon = icons[i];
            if (icon != null && icon.gameObject.activeSelf && icon.IsMoving())
                return;
        }

        IsAnimating = false;
    }

    int FindMatchingIndex(List<TurnEntry> list, TurnEntry entry)
    {
        // Try to match by UID first
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.uid == entry.uid)
                return i;
        }
        // If no UID match, try to match by character AND time
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.character == entry.character && Mathf.Approximately(e.time, entry.time))
                return i;
        }
        // No match found
        return -1;
    }

    float GetTimelineX(int relativeIndex)
    {
        return GetTimelineX((float)relativeIndex);
    }

    float GetTimelineX(float relativeTime)
    {
        float direction = Mathf.Sign(relativeTime);
        float distance = Mathf.Abs(relativeTime);

        // compression progressive
        float compressed = Mathf.Pow(distance,0.9f) * spacing-container.GetComponent<RectTransform>().rect.width*0.5f;

        return compressed * direction;
    }

    List<TurnEntry> CloneTimeline(List<TurnEntry> source)
    {
        var clone = new List<TurnEntry>();

        foreach (var t in source)
            clone.Add(t.Clone());

        return clone;
    }

    void EnsureIconCount(int count)
    {
        while (icons.Count < count)
        {
            var obj = Instantiate(iconPrefab, container);
            icons.Add(obj.GetComponent<TurnIcon>());
        }
    }
    bool shown = true;
    public void ToggleVisibility()
    {
        Debug.Log("Toggle timeline visibility");
        shown = !shown;
        if (shown)
        {
            StartCoroutine(ExpandHeight());
        }
        else
        {
            StartCoroutine(CollapseHeight());
        }
    }
    IEnumerator CollapseHeight()
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scaleY = Mathf.Lerp(1f, 0f, t);
            panel.localScale = new Vector3(1f, scaleY, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panel.localScale = new Vector3(1f, 0f, 1f);
    }
    IEnumerator ExpandHeight()
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scaleY = Mathf.Lerp(0f, 1f, t);
            panel.localScale = new Vector3(1f, scaleY, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panel.localScale = new Vector3(1f, 1f, 1f);
    }
}