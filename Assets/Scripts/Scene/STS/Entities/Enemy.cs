using UnityEngine;
public class Enemy : Character
{
    public Enemy(string name) : base(name, 0)
    {
        this.isPlayer = false;
        data=Resources.Load<EnemyData>("STS/Enemies/" + name);
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
    private STSCardData forcedNextAction;

    public void Init(EnemyData d)
    {
        name=d.name;
        data = d;
        patternIndex = 0;
        maxHP = d.maxHP;
        if (RunManager.Instance != null)
        {
            for (int i = 0; i < RunManager.Instance.act; i++)
            {
                maxHP = Mathf.RoundToInt(maxHP * 1.5f); // Scale HP by 50% per act
            }
        }
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
        if (forcedNextAction != null)
        {
            var overrideAction = new EnemyMoveEntry
            {
                card = forcedNextAction
            };

            forcedNextAction = null;
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
        if (forcedNextAction != null)
            return forcedNextAction;

        if (data == null || data.ActionCount == 0)
            return null;

        return data.GetActionAt(patternIndex)?.CreateRuntimeCard(name);
    }

    public void ForceNextAction(string cardName)
    {
        forcedNextAction = STSCardDatabase.Get(cardName);

        if (forcedNextAction == null)
        {
            Debug.LogWarning($"Could not force enemy action '{cardName}' for {name}.");
        }
    }
}