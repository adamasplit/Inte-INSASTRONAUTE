using UnityEngine;
[System.Serializable]
public class PanelOption
{
    public string text;
    public Sprite icon;
    public EventOptionType type=EventOptionType.None;
    public int value;
#if UNITY_EDITOR
    public System.Action action;
#endif
}