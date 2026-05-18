using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEventData", menuName = "STS/Event Data", order = 1)]
public class EventDataSO : ScriptableObject
{
    public string title;
    public string description;
    public Sprite image;
    public List<PanelOption> options;
}
