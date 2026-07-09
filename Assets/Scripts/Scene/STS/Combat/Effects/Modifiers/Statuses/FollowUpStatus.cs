using UnityEngine;
using System.Collections.Generic;
public abstract class FollowUpStatus:StatusEffect
{
    protected int moveIndex=0;
    protected bool randomCard=false;
    protected STSCardData cardData=null;
    public STSCardData GetCardData()
    {
        if (randomCard)
        {
            return null;
        }
        return cardData;
    }
    public FollowUpStatus(int value,int duration,string effectInfo="",int index=0)
    {
        Duration = -1;
        if (duration>0)
        {
            Duration=duration;
        }
        Value = 1;
        moveIndex = index;
        maxValue=Mathf.Max(1, value);
        debuff=false;
        buff=true;
        framed=true;
        cardData=null;
        if (effectInfo!=""&&effectInfo!=null)
        {
            cardData = STSCardDatabase.Get(effectInfo);
        }
        if (cardData == null) 
        {
            randomCard=true;
            Name = "Carte aléatoire";
        }
        else
        {
            Name = cardData.cardName;
            if (cardData.HasTag(CardTag.Status) || cardData.HasTag(CardTag.Curse))
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
        if (cardData != null&&cardData.icon!=null)
        {
            return cardData.icon.name;
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
            if (Duration>0)
            {
                Duration--;
            }
        }
    }
}