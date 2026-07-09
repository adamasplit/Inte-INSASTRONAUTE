using UnityEngine;
using System.Linq;
using System;
public class SpecificStatusOnSelfModifier : StatModifier
{
    public StatusType statusType;
    public string statusName;
    public int perStatus = 2;
    public SpecificStatusOnSelfModifier(StatType type, int amount,string statusId)
    {
        this.type = type;
        this.statusType = Enum.Parse<StatusType>(statusId);
        this.statusName = StatusEffect.Factory(statusType,0,0).Name;
        perStatus = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.source == null)
            return value;
        StatusEffect status = ctx.source.statusEffects.FirstOrDefault(s => s.statusType == statusType);
        if (status == null)
            return value;
        return value + perStatus*Mathf.Max(1, Mathf.Max(0, status.Value)+Mathf.Max(0, status.Duration));
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perStatus,modifierType)} par {statusName}";
    }
}