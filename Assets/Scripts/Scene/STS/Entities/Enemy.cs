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

    public void Init(EnemyData d)
    {
        name=d.name;
        data = d;
        maxHP = d.maxHP;
        if (RunManager.Instance != null)
        {
            for (int i = 0; i < RunManager.Instance.act; i++)
            {
                maxHP = Mathf.RoundToInt(maxHP * 1.5f); // Scale HP by 50% per act
            }
        }
        currentHP = maxHP;
        Debug.Log($"Initialized enemy {name} with {maxHP} HP. Adding starting status: {d.startingStatus} with value {d.startingStatusValue} and duration {d.startingStatusDuration}");
        if (d.startingStatusValue != 0 || d.startingStatusDuration != 0)
        {
            AddStatus(StatusEffect.Factory(d.startingStatus, d.startingStatusValue, d.startingStatusDuration));
        }
    }

    public STSCardData GetNextAction()
    {
        if (data.pattern.Count == 0)
            return null;

        var card = data.pattern[patternIndex];

        patternIndex = (patternIndex + 1) % data.pattern.Count;

        return card;
    }

    public STSCardData PeekNextAction()
    {
        if (data.pattern.Count == 0)
            return null;

        return data.pattern[patternIndex];
    }
}