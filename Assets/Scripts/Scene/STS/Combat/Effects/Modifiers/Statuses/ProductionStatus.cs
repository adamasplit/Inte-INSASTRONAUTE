using UnityEngine;

/// <summary>
/// Production : le porteur pioche <c>Value</c> cartes de plus en début de tour.
///
/// <para>Se lit comme <see cref="EnergyStatus"/> : une valeur signée et sans fin, plutôt qu'une
/// durée. Une valeur négative retire ce qu'une valeur positive donne — elle défausse d'autant —
/// et une valeur nulle veut dire que le statut n'a plus rien à faire.</para>
/// </summary>
public class ProductionStatus : StatusEffect
{
    public ProductionStatus(int value)
    {
        Name = "Production";
        Value = value;
        Duration = -1;
        framed = true;
        Update(null);
    }

    public override void Update(Character target)
    {
        if (Value < 0)
        {
            buff = false;
            debuff = true;
        }
        else if (Value > 0)
        {
            buff = true;
            debuff = false;
        }
        else
        {
            mustExpire = true;
        }
    }

    public override void OnTurnStart(Character target)
    {
        base.OnTurnStart(target);
        for (int i = 0; i < Mathf.Abs(Value); i++)
        {
            if (Value > 0)
                target.DrawCard();
            else
                target.combat?.deck?.Discard();
        }
    }

    /// <summary>Rien : Production ne s'écoule pas, elle dure jusqu'à ce qu'on la retire.</summary>
    public override void OnTurnEnd(Character target)
    {
    }

    public override string Desc(bool isPlayer)
    {
        int amount = Mathf.Abs(Value);
        string cards = amount > 1 ? $"{amount} cartes" : "1 carte";
        if (Value < 0)
        {
            return isPlayer
                ? $"Défaussez {cards} au début du tour."
                : $"Le personnage défausse {cards} au début du tour.";
        }
        return isPlayer
            ? $"Piochez {cards} supplémentaire{(amount > 1 ? "s" : "")} en début de tour."
            : $"Le personnage pioche {cards} supplémentaire{(amount > 1 ? "s" : "")} en début de tour.";
    }
}
