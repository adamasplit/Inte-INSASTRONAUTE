using UnityEngine;
using System.Collections.Generic;

public class TimelineUI : MonoBehaviour
{
    public Transform container;
    public GameObject iconPrefab;

    public float spacing = 100f;

    List<TurnIcon> icons = new();
    List<TurnEntry> previousTimeline = null;

    public void Display(List<TurnEntry> timeline, bool preview = false)
    {
        EnsureIconCount(timeline.Count);

        for (int i = 0; i < timeline.Count; i++)
        {
            var entry = timeline[i];
            var icon = icons[i];

            icon.gameObject.SetActive(true);

            icon.Set(entry.character);
            icon.SetPreview(preview);

            //   POSITION CIBLE (nouvelle)
            Vector3 targetPos = new Vector3((i - timeline.Count / 2f) * spacing, 0, 0);

            //   POSITION DE DÉPART (ancienne)
            if (previousTimeline != null)
            {
                int oldIndex = FindMatchingIndex(previousTimeline, entry);

                if (oldIndex != -1)
                {
                    Vector3 oldPos = new Vector3(
                        (oldIndex - previousTimeline.Count / 2f) * spacing,
                        0,
                        0
                    );

                    // IMPORTANT : on place l'icône à son ancienne position AVANT de lui donner une target
                    icon.transform.localPosition = oldPos;
                }
            }

            icon.SetTargetPosition(targetPos);

            icon.SetType(entry.visualType);
        }

        // désactiver les icônes en trop
        for (int i = timeline.Count; i < icons.Count; i++)
        {
            icons[i].gameObject.SetActive(false);
        }

        //   on sauvegarde pour le prochain frame
        if (!preview) previousTimeline = CloneTimeline(timeline);
    }

    int FindMatchingIndex(List<TurnEntry> list, TurnEntry entry)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];

            // match simple (suffisant pour l'instant)
            if (e.character == entry.character && Mathf.Approximately(e.time, entry.time))
                return i;
        }

        return -1;
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