using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class TimelineUI : MonoBehaviour
{
    public Transform container;
    public GameObject iconPrefab;
    public TurnSystem turnSystem;
    List<GameObject> currentIcons = new();
    public List<TurnEntry> displayedTimeline = new();
    public void Display(List<TurnEntry> timeline, bool preview = false)
    {
        Clear();
        for (int i = 0; i < timeline.Count; i++)
        {
            var entry = timeline[i];

            var obj = Instantiate(iconPrefab, container);
            var icon = obj.GetComponent<TurnIcon>();

            icon.Set(entry.character);
            icon.SetPreview(preview&&displayedTimeline!=timeline);
            switch (entry.visualType)
            {
                case TurnVisualType.Removed:
                    icon.SetRemoved();
                    break;

                case TurnVisualType.Delayed:
                    icon.SetDelayed();
                    break;

                case TurnVisualType.Advanced:
                    icon.SetAdvanced();
                    break;
            }

            currentIcons.Add(obj);
        }
        displayedTimeline = timeline;
    }

    void Clear()
    {
        foreach (var obj in currentIcons)
            Destroy(obj);

        currentIcons.Clear();
    }
}