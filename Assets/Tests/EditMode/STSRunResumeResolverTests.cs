using NUnit.Framework;

public class STSRunResumeResolverTests
{
    [Test]
    public void ActiveEncounterResumesCombat()
    {
        Assert.That(Resolve(hasActiveEncounter: true), Is.EqualTo(STSRunResumePhase.Combat));
    }

    [Test]
    public void ActiveEventResumesEvent()
    {
        Assert.That(Resolve(hasActiveEvent: true, enteredNodeType: "Event"), Is.EqualTo(STSRunResumePhase.Event));
    }

    [Test]
    public void EnteredRestResumesRest()
    {
        Assert.That(Resolve(enteredNodeType: "Rest"), Is.EqualTo(STSRunResumePhase.Rest));
    }

    [Test]
    public void PendingRewardsResumeReward()
    {
        Assert.That(Resolve(hasPendingRewards: true), Is.EqualTo(STSRunResumePhase.Reward));
    }

    [Test]
    public void CompletedBossResumesRetreat()
    {
        Assert.That(Resolve(currentNodeType: "Boss", currentNodeCompleted: true), Is.EqualTo(STSRunResumePhase.Retreat));
    }

    [Test]
    public void OrdinaryRunResumesMap()
    {
        Assert.That(Resolve(), Is.EqualTo(STSRunResumePhase.Map));
    }

    [Test]
    public void ResumedRestStartsWithAllCharges()
    {
        Assert.That(STSRestState.InitialCharges(isEnteredRest: true, currentCharges: 0, maxCharges: 15), Is.EqualTo(15));
    }

    [Test]
    public void OrdinarySceneKeepsExistingRestCharges()
    {
        Assert.That(STSRestState.InitialCharges(isEnteredRest: false, currentCharges: 2, maxCharges: 15), Is.EqualTo(2));
    }

    [Test]
    public void HealingIsClampedToMaximumHp()
    {
        Assert.That(STSHealing.Apply(currentHp: 40, maxHp: 70, amount: 35), Is.EqualTo(70));
    }

    private static STSRunResumePhase Resolve(
        bool hasActiveEncounter = false,
        bool hasActiveEvent = false,
        string enteredNodeType = null,
        bool hasPendingRewards = false,
        string currentNodeType = null,
        bool currentNodeCompleted = false)
    {
        return STSRunResumeResolver.Resolve(
            hasActiveEncounter,
            hasActiveEvent,
            enteredNodeType,
            hasPendingRewards,
            currentNodeType,
            currentNodeCompleted);
    }
}
