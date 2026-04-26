using UnityEngine;
public class Enemy : Character
{
    public Enemy(string name) : base(name, 0)
    {
        this.isPlayer = false;
        data=Resources.Load<EnemyData>("STS/Enemies/" + name);
        if (data != null)
        {
            this.name = data.enemyName;
            maxHP = data.maxHP;
            currentHP = maxHP;
        }
    }
    public EnemyData data;

    private int patternIndex = 0;

    public void Init(EnemyData d)
    {
        data = d;
        maxHP = d.maxHP;
        currentHP = maxHP;
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