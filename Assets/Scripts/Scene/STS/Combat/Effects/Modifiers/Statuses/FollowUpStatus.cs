using UnityEngine;
using System.Collections.Generic;
public abstract class FollowUpStatus:StatusEffect
{
    protected int moveIndex=0;
    protected bool randomCard=false;
    public FollowUpStatus(int value,int duration,string effectInfo="")
    {
        Duration = -1;
        Value = 1;
        moveIndex = value;
        maxValue=Mathf.Max(1, duration);
        debuff=false;
        buff=true;
        framed=true;
        STSCardData data=null;
        if (effectInfo!=""&&effectInfo!=null)
        {
            data = STSCardDatabase.Get(effectInfo);
        }
        if (data == null) 
        {
            randomCard=true;
            Name = "Carte aléatoire";
        }
        else
        {
            Name = data.cardName;
            if (data.HasTag(CardTag.Status) || data.HasTag(CardTag.Curse))
            {
                buff=false;
                debuff=true;
            }
        }
    }
    public override string IconPath()
    {
        if (randomCard)
        {
            return "Random";
        }
        STSCardData data = STSCardDatabase.Get(Name);
        if (data != null&&data.icon!=null)
        {
            return data.icon.name;
        }
        return base.IconPath();
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        list.Add(this);
    }
    protected void IncrementFollowUp(Character source,Character target)
    {
        Value++;
        if (Value>maxValue)
        {
            Value=1;
            source.GetCombatManager().FollowUpCard(randomCard, Name, source, target);
        }
    }
}