using UnityEngine;
public class ResourceSet
{
    public int energy;
    // futur
    public int bp;
    public int GetOverdraft()
    {
        return Mathf.Min(0, energy);
    }
}