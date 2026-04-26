using System.Collections.Generic;
public class MapNode
{
    public int id;
    public int floor;
    public NodeType type;
    public int x; // for display only
    public List<MapNode> next = new();
    public List<MapNode> prev = new(); // important pour debug + affichage
}