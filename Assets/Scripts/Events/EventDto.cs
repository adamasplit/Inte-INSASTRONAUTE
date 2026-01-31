using System;

[Serializable]
public class EventDto
{
    public string id;
    public string type;        // "INFO" / "PARI"
    
    public bool enabled;
    public int priority;

    public string title;
    public string body;
    public string bannerUrl;

    // PARI
    public float odds;
    public string deadlineIso;
    public string status;      // "OPEN" / "CLOSED"
    public string outcome;     // "", "true", "false" (string for JsonUtility compatibility)

    // Helper properties for outcome handling
    public bool HasOutcome => !string.IsNullOrEmpty(outcome);
    public bool OutcomeValue => outcome == "true";
}
