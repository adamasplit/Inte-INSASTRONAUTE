using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class EventData
{
    public string title;
    public string description;
    public string imageName; // For JSON export, store image name only
    public List<PanelOptionData> options;

    public EventData(EventDataSO so)
    {
        title = so.title;
        description = so.description;
        imageName = so.image != null ? so.image.name : null;
        options = new List<PanelOptionData>();
        if (so.options != null)
        {
            foreach (var opt in so.options)
            {
                options.Add(new PanelOptionData(opt));
            }
        }
    }
}

[System.Serializable]
public class PanelOptionData
{
    public string text;
    public string iconName;
    public string completionMessage;
    public List<PanelOptionEntryData> entries = new();
    public PanelOptionData(PanelOption option)
    {
        text = option.text;
        iconName = option.icon != null ? option.icon.name : null;
        completionMessage = option.completionMessage;
        if (option.entries != null)
        {
            foreach (var entry in option.entries)
            {
                entries.Add(new PanelOptionEntryData(entry));
            }
        }
    }

    public PanelOption ToPanelOption()
    {
        PanelOption opt = new PanelOption();
        opt.text = text;
        opt.completionMessage = completionMessage;
        // icon lookup by iconName if needed
        opt.entries = new List<PanelOptionEntry>();
        foreach (var entryData in entries)
        {
            var entry = new PanelOptionEntry
            {
                type = System.Enum.Parse<EventOptionType>(entryData.type),
                value = entryData.value
            };
            opt.entries.Add(entry);
        }
        return opt;
    }
}
[System.Serializable]
public class PanelOptionEntryData
{
    public string type;
    public int value;
    public string id;
    public PanelOptionEntryData(PanelOptionEntry entry)
    {
        type = entry.type.ToString();
        value = entry.value;
        id = entry.id;
    }
}