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
    public string type; // For JSON export, store action name only (if needed)
    public int value; // Additional field to store any relevant value for the option
    public PanelOptionData(PanelOption option)
    {
        text = option.text;
        iconName = option.icon != null ? option.icon.name : null;
        type = option.type.ToString(); // Store the enum type as string for JSON export
        value = option.value; // Store the value for JSON export
    }

    public PanelOption ToPanelOption()
    {
        PanelOption opt = new PanelOption();
        opt.text = text;
        // icon lookup by iconName if needed
        opt.type = System.Enum.TryParse<EventOptionType>(type, out var parsedType) ? parsedType : EventOptionType.None;
        opt.value = value; // Assign the value to the PanelOption
        return opt;
    }
}