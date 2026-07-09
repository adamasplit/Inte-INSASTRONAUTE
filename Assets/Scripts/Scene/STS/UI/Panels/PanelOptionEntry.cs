using System.Collections.Generic;

[System.Serializable]
public class PanelOptionEntry
{
    public EventOptionType type=EventOptionType.None;
    public int value;
    public string id;
    public List<string> targetIds = new();
    public List<PanelOption> replacementOptions = new();
    public CardRewardProfile cardRewardProfile = new();
    public List<CardRewardProfile> cardRewardProfiles = new();
}