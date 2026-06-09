using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class PanelOption
{
    public string text;
    public Sprite icon;
    public List<PanelOptionEntry> entries = new();
    public System.Action action;
}