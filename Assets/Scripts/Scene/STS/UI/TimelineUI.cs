using UnityEngine;
using System.Collections.Generic;

public class TimelineUI : MonoBehaviour
{
    public Transform container;
    public GameObject iconPrefab;

    public float spacing = 100f;

    List<TurnIcon> icons = new();
    List<TurnEntry> previousDisplayTimeline = null;

    public bool IsAnimating { get; private set; }

    public void Display(List<TurnEntry> timeline, bool preview = false)
    {
        bool anyIconMoving = false;
        EnsureIconCount(timeline.Count);
        int currentIndex=0;
        for (int i = 0; i < timeline.Count; i++)
        {
            var entry = timeline[i];
            var icon = icons[i];

            icon.gameObject.SetActive(true);

            icon.Set(entry.character);
            icon.SetPreview(preview);

            //   POSITION CIBLE (nouvelle)
            float x = GetTimelineX(i - currentIndex);
            Vector3 targetPos = new Vector3(x, 0, 0);

            //   POSITION DE DÉPART (ancienne)
            if (previousDisplayTimeline != null)
            {
                int oldIndex = FindMatchingIndex(previousDisplayTimeline, entry);

                if (oldIndex != -1)
                {
                    Vector3 oldPos = new Vector3(
                        GetTimelineX(oldIndex - currentIndex),
                        0,
                        0
                    );

                    // IMPORTANT : on place l'icône à son ancienne position AVANT de lui donner une target
                    icon.transform.localPosition = oldPos;
                }
            }

            icon.SetTargetPosition(targetPos);

            if (!preview && icon.IsMoving())
                anyIconMoving = true;

            icon.SetType(entry.visualType);
            float depth = Mathf.Clamp01(i / (float)timeline.Count);
            icon.SetDepth(depth);
            icon.transform.SetSiblingIndex(timeline.Count-i);
        }

        // désactiver les icônes en trop
        for (int i = timeline.Count; i < icons.Count; i++)
        {
            icons[i].gameObject.SetActive(false);
        }

        if (!preview)
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
        float direction = Mathf.Sign(relativeIndex);
        float distance = Mathf.Abs(relativeIndex);

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
}