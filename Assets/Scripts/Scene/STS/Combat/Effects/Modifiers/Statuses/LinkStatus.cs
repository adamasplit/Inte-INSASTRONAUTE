using UnityEngine;
using System.Collections.Generic;
public class LinkStatus:StatusEffect
{
    private string effectInfo="";
    public LinkStatus(int value,int duration,string effectInfo="",int index=0)
    {
        Duration = -1;
        Value=0;
        Name="Lien";
        this.effectInfo=effectInfo;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Tant que {effectInfo} sera en vie, {(isPlayer?"vous ne pourrez pas ":"l'ennemi ne pourra pas ")}perdre de PV.";
    }
    public override string IconPath()
    {
        return effectInfo;
    }
    public override int ValidateHPLoss(int damage, Character target)
    {
        foreach (var character in target.GetCombatManager().GetAllCharacters())
        {
            Enemy enemy = character as Enemy;
            if (enemy != null && enemy.name == effectInfo && enemy.IsAlive)
            {
                return 0;
            }
        }
        return damage;
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        list.Add(this);
    }
}