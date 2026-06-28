using System.Collections.Generic;
using UnityEngine;
public class Enemy : Character
{
    public Enemy(string name) : base(name, 0)
    {
        this.isPlayer = false;
        data = EnemyDataDatabase.Get(name);
        if (data == null)
        {
            data = Resources.Load<EnemyData>("STS/Enemies/" + name);
        }
        if (data != null)
        {
            Init(data);
        }
        else
        {
            Debug.LogError($"Enemy data for {name} not found in Resources/STS/Enemies/");
        }
    }
    public EnemyData data;

    private int patternIndex = 0;
    private readonly Queue<STSCardData> forcedNextActions = new();

    public void Init(EnemyData d)
    {
        name=d.displayName;
        if (name == null || name == "")
        {
            name = d.enemyName;
        }
        Debug.Log($"Initializing enemy {name} with data: {d.name} and displayName: {d.displayName}");
        data = d;
        patternIndex = 0;
        maxHP = d.maxHP;
        float multiplier = 1.3f;
        if (RunManager.Instance != null)
        {
            multiplier+= RunManager.Instance.act*0.5f;
        }
        maxHP = Mathf.RoundToInt(maxHP * multiplier);
        maxHP+=Random.Range(1,5); // Add a random value between 1 and 5 to maxHP
        currentHP = maxHP;
        Debug.Log($"Initialized enemy {name} with {maxHP} HP. Adding starting status: {d.startingStatus} with value {d.startingStatusValue} and duration {d.startingStatusDuration}");
        if (d.startingStatusValue != 0 || d.startingStatusDuration != 0)
        {
            AddStatus(StatusEffect.Factory(d.startingStatus, d.startingStatusValue, d.startingStatusDuration,d.startingStatusInfo));
        }
    }

    public EnemyMoveEntry GetNextActionPlan()
    {
        if (forcedNextActions.Count > 0)
        {
            var overrideAction = new EnemyMoveEntry
            {
                card = forcedNextActions.Dequeue()
            };

            return overrideAction;
        }

        if (data == null || data.ActionCount == 0)
            return null;

        var action = data.GetActionAt(patternIndex);
        patternIndex = data.GetNextActionIndex(patternIndex);
        return action;
    }

    public STSCardData GetNextAction()
    {
        return GetNextActionPlan()?.CreateRuntimeCard(name);
    }

    public STSCardData PeekNextAction()
    {
        if (forcedNextActions.Count > 0)
            return forcedNextActions.Peek();

        if (data == null || data.ActionCount == 0)
            return null;

        return data.GetActionAt(patternIndex)?.CreateRuntimeCard(name);
    }

    public void ForceNextAction(string cardName)
    {
        ForceNextAction(cardName, 1);
    }

    public void ForceNextAction(string cardName, int turns)
    {
        var cardData = STSCardDatabase.Get(cardName);

        if (cardData == null)
        {
            Debug.LogWarning($"Could not force enemy action '{cardName}' for {name}.");
            return;
        }

        ForceNextAction(cardData, turns);
    }

    public void ForceNextAction(STSCardData cardData, int turns = 1)
    {
        if (cardData == null)
        {
            Debug.LogWarning($"Could not force a null enemy action for {name}.");
            return;
        }

        int count = Mathf.Max(1, turns);
        for (int i = 0; i < count; i++)
        {
            forcedNextActions.Enqueue(cardData);
        }
    }
}