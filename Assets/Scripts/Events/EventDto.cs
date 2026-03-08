using System;

[Serializable]
public class OptionDto
{
    public string label;
    public float odds;
}

[Serializable]
public class OutcomeDto
{
    public string answer;
    public float odds;
}

[Serializable]
public class EventDto
{
    public string id;
    public string type;         // "INFO" / "PARI"

    public bool enabled;
    public int priority;

    public string title;
    public string body;
    public string bannerUrl;

    // PARI
    public string answerType;   // "list" | "free"
    public OptionDto[] options; // list : options disponibles avec leurs côtes
    public string deadlineIso;
    public string status;       // "OPEN" / "CLOSED"

    // État CLOSED
    public string outcome;       // list : label gagnant ; free : vide
    public OutcomeDto[] outcomes; // free : bonnes réponses avec leurs côtes

    // Vrai si l'outcome est disponible (utilisé pour filtrer les événements à résoudre)
    public bool HasOutcome =>
        answerType == "free"
            ? outcomes != null && outcomes.Length > 0
            : !string.IsNullOrEmpty(outcome);
}
