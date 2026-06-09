using System;

[Serializable]
public class STSTutorialStep
{
    public string text;

    public Action onStart;
    public Action onComplete;

    public Func<bool> completionCondition;
    public Func<int> nextStepOverride; 
}