using System.Collections.Generic;
using UnityEngine;
public class Enemy : Character
{
    public Enemy(string name) : base(name, 0)
    {
        this.isPlayer = false;
        this.isAlly = false;
        this.isLocalPlayer = false;
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

    public Enemy(EnemyData data) : base(data != null ? (!string.IsNullOrEmpty(data.enemyName) ? data.enemyName : data.name) : "Ironclad", 0)
    {
        this.isPlayer = false;
        this.isAlly = false;
        this.isLocalPlayer = false;
        if (data != null)
        {
            Init(data);
        }
        else
        {
            Debug.LogError("Enemy created with null EnemyData.");
        }
    }
    public EnemyData data;

    /// <summary>
    /// Un adversaire piloté par un humain.
    ///
    /// <para>Sans <c>EnemyData</c>, et c'est le point : il n'a ni motif ni intention à
    /// afficher, puisque c'est l'autre joueur qui décide. <c>PeekNextAction</c> rend
    /// <c>null</c> dans ce cas et <c>CharacterUI</c> vide alors la zone d'intention, donc
    /// rien à faire de plus.</para>
    ///
    /// <para>Il reste un <c>Enemy</c> plutôt qu'un <c>Character</c> parce que
    /// <c>DropZone.Init</c> caste en <c>Enemy</c> tout ce qui n'est pas <c>isPlayer</c> ;
    /// avec <c>data == null</c>, ce cast réussit et le portrait se charge depuis
    /// <c>STS/Characters/{name}</c>, où le nom du personnage jouable se trouve.</para>
    ///
    /// <para>Ses points de vie arrivent avec le premier état autoritatif ; ceux passés ici
    /// ne servent qu'à ce qu'il ne soit pas mort-né entre le montage et ce premier état.</para>
    /// </summary>
    public Enemy(string characterName, int placeholderMaxHP, string userId, string displayName)
        : base(characterName, placeholderMaxHP)
    {
        this.isPlayer = false;
        this.isAlly = false;
        this.isLocalPlayer = false;
        this.playerUserId = userId;
        this.playerDisplayName = displayName;
    }


    private int patternIndex = 0;
    private readonly Queue<STSCardData> forcedNextActions = new();
    public STSCardData authoritativeIntentCard; // Set by the authoritative combat state sync; overrides local PeekNextAction for intent display

    public void SetPatternIndex(int index)
    {
        patternIndex = Mathf.Max(0, index);
    }

    public void SetAuthoritativeIntentCard(STSCardData card)
    {
        authoritativeIntentCard = card;
    }

    public void Init(EnemyData d)
    {
        name=d.displayName;
        if (name == null || name == "")
        {
            name = d.enemyName;
        }
        data = d;
        patternIndex = d.randomStart ? d.PickRandomActionIndex() : 0;
        maxHP = d.maxHP;
        float multiplier = EnemyPoolDatabase.BaseHpScaling;
        if (RunManager.Instance != null)
        {
            if (EnemyPoolDatabase.ActHpScaling != null && EnemyPoolDatabase.ActHpScaling.Count > 0)
            {
                multiplier += EnemyPoolDatabase.ActHpScaling[Mathf.Min(RunManager.Instance.act, EnemyPoolDatabase.ActHpScaling.Count - 1)];
            }

            if (PlayersDatabase.TryGet(RunManager.Instance.selectedCharacter, out PlayerInfoDTO selectedCharacterData))
            {
                multiplier += selectedCharacterData.hpAdditionalMultiplier;
            }
        }
        maxHP = Mathf.RoundToInt(maxHP * multiplier);
        maxHP+=Random.Range(1,5); // Add a random value between 1 and 5 to maxHP
        currentHP = maxHP;
        if (d.startingStatusValue != 0 || d.startingStatusDuration != 0)
        {
            AddStatus(StatusEffect.Factory(d.startingStatus, d.startingStatusValue, d.startingStatusDuration,d.startingStatusInfo,d.startingStatusIndex));
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
        // In authoritative mode, the server tracks the pattern; use the synced intent card.
        if (authoritativeIntentCard != null)
            return authoritativeIntentCard;

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
    public void ForceNextAction(CardInstance card)
    {
        ForceNextAction(card.data, 1);
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