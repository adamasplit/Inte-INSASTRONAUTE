using System.Collections.Generic;
using UnityEngine;
public class EnchantmentData
{
    public string name;
    public string description;
    public int maxLevel;

    public virtual List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>();
    }
}