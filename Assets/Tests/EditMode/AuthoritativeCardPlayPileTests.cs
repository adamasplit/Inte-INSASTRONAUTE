using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class AuthoritativeCardPlayPileTests
{
    [Test]
    public void MovePlayedCardToDiscardRemovesCardFromHand()
    {
        var hand = new List<string> { "katana", "shield" };
        var discard = new List<string>();

        AuthoritativeCombatStateReducer.MoveCard(hand, discard, "katana");

        Assert.That(hand, Does.Not.Contain("katana"));
        Assert.That(discard, Contains.Item("katana"));
    }

    [Test]
    public void ResolveDamageUsesAuthoritativeRemainingValues()
    {
        AuthoritativeDamageState state = AuthoritativeCombatStateReducer.ResolveDamage(
            currentHp: 20,
            currentArmor: 3,
            remainingHp: 15,
            remainingArmor: 0);

        Assert.That(state.Hp, Is.EqualTo(15));
        Assert.That(state.Armor, Is.Zero);
    }

    [Test]
    public void ReadStatusesPreservesBackendTypeValueDurationAndIndex()
    {
        JArray statuses = JArray.Parse(
            "[{\"statusType\":\"Burn\",\"value\":5,\"duration\":2,\"cardId\":\"thermal\",\"index\":3}]");

        IReadOnlyList<AuthoritativeStatusState> result =
            AuthoritativeCombatStateReducer.ReadStatuses(statuses);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StatusType, Is.EqualTo("Burn"));
        Assert.That(result[0].Value, Is.EqualTo(5));
        Assert.That(result[0].Duration, Is.EqualTo(2));
        Assert.That(result[0].CardId, Is.EqualTo("thermal"));
        Assert.That(result[0].Index, Is.EqualTo(3));
    }

    [TestCase(0, "ACTIVE", 1)]
    [TestCase(0, "FINISHED", 1)]
    [TestCase(3, "ACTIVE", 3)]
    [TestCase(0, "", 0)]
    public void ResolveTurnCountNeverSubmitsZeroForAStartedCombat(
        int currentTurnCount,
        string combatStatus,
        int expected)
    {
        Assert.That(
            AuthoritativeCombatStateReducer.ResolveTurnCount(currentTurnCount, combatStatus),
            Is.EqualTo(expected));
    }

    [TestCase(false, true, false, false)]
    [TestCase(true, true, false, false)]
    [TestCase(true, true, true, true)]
    [TestCase(true, false, false, true)]
    public void AuthoritativeNodeEntryMustBeAcceptedBeforeLoadingItsScene(
        bool accepted,
        bool combatScene,
        bool hasEncounter,
        bool expected)
    {
        Assert.That(
            AuthoritativeCombatStateReducer.CanLoadEnteredNode(accepted, combatScene, hasEncounter),
            Is.EqualTo(expected));
    }
}
