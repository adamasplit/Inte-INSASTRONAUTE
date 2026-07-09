using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]
public class EventData
{
    public string title;
    public string description;
    public string imageName; // For JSON export, store image name only
    public float weight = 1f;
    public List<PanelOptionData> options;

    public EventData(EventDataSO so)
    {
        title = so.title;
        description = so.description;
        imageName = so.image != null ? so.image.name : null;
        weight = so.weight;
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
    public string id;
    public string text;
    public string iconName;
    public string completionMessage;
    public bool closePanel = true;
    public List<string> previewCardIds = new();
    public List<PanelOptionEntryData> entries = new();
    public PanelOptionData(PanelOption option)
    {
        id = option.id;
        text = option.text;
        iconName = option.icon != null ? option.icon.name : null;
        completionMessage = option.completionMessage;
        closePanel = option.closePanel;

        if (option.previewCardIds != null && option.previewCardIds.Count > 0)
        {
            previewCardIds.AddRange(option.previewCardIds);
        }
        else if (option.entries != null)
        {
            foreach (var entry in option.entries)
            {
                if (entry.type == EventOptionType.AddCard && entry.value == 1 && !string.IsNullOrEmpty(entry.id) && !previewCardIds.Contains(entry.id))
                {
                    previewCardIds.Add(entry.id);
                }
            }
        }

        if (option.entries != null)
        {
            foreach (var entry in option.entries)
            {
                entries.Add(new PanelOptionEntryData(entry));
            }
        }
    }

    public PanelOption ToPanelOption(EventManager manager)
    {
        PanelOption opt = new PanelOption();
        opt.id = id;
        opt.text = text;
        opt.completionMessage = completionMessage;
        opt.closePanel = closePanel;
        opt.previewCardIds = new List<string>(previewCardIds);
        // icon lookup by iconName if needed
        opt.entries = new List<PanelOptionEntry>();
        foreach (var entryData in entries)
        {
            var entry = new PanelOptionEntry
            {
                type = System.Enum.Parse<EventOptionType>(entryData.type),
                value = entryData.value,
                id = entryData.id,
                targetIds = entryData.targetIds != null ? new List<string>(entryData.targetIds) : new List<string>(),
                cardRewardProfile = entryData.cardRewardProfile != null ? new CardRewardProfile(entryData.cardRewardProfile) : new CardRewardProfile(),
                cardRewardProfiles = entryData.cardRewardProfiles != null
                    ? entryData.cardRewardProfiles.Select(profile => new CardRewardProfile(profile)).ToList()
                    : new List<CardRewardProfile>(),
                replacementOptions = entryData.replacementOptions != null
                    ? entryData.replacementOptions.Select(replacement => replacement.ToPanelOption(manager)).ToList()
                    : new List<PanelOption>()
            };
            opt.entries.Add(entry);
        }
        opt.action = EventActionFactory.GetAction(opt, manager);
        return opt;
    }
}
[System.Serializable]
public class PanelOptionEntryData
{
    public string type;
    public int value;
    public string id;
    public List<string> targetIds = new();
    public List<PanelOptionData> replacementOptions = new();
    public CardRewardProfile cardRewardProfile = new();
    public List<CardRewardProfile> cardRewardProfiles = new();
    public PanelOptionEntryData(PanelOptionEntry entry)
    {
        type = entry.type.ToString();
        value = entry.value;
        id = entry.id;

        if (entry.targetIds != null && entry.targetIds.Count > 0)
        {
            targetIds.AddRange(entry.targetIds);
        }

        if (entry.replacementOptions != null && entry.replacementOptions.Count > 0)
        {
            foreach (var replacement in entry.replacementOptions)
            {
                replacementOptions.Add(new PanelOptionData(replacement));
            }
        }

        cardRewardProfile = entry.cardRewardProfile != null ? new CardRewardProfile(entry.cardRewardProfile) : new CardRewardProfile();
        if (entry.cardRewardProfiles != null && entry.cardRewardProfiles.Count > 0)
        {
            foreach (var profile in entry.cardRewardProfiles)
            {
                cardRewardProfiles.Add(profile != null ? new CardRewardProfile(profile) : new CardRewardProfile());
            }
        }
    }
}