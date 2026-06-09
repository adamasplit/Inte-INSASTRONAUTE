using System;
using UnityEngine;

[Serializable]
public class TutorialNode
{
    [TextArea] public string text;

    public Action onStart;
    public Action onComplete;

    public Func<bool> condition;

    public Func<TutorialNode> next;
}