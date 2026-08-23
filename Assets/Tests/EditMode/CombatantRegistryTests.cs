using System.Collections.Generic;
using NUnit.Framework;

public class CombatantRegistryTests
{
    private static CombatantRegistry<string> ThreeEnemyEncounter()
    {
        var registry = new CombatantRegistry<string>();
        registry.Register(
            new CombatantDescriptor("player", "team-player", CombatantController.Human, true),
            "Player");
        registry.Register(
            new CombatantDescriptor("enemy-0", "team-enemies", CombatantController.Ai, false),
            "Enemy_1");
        registry.Register(
            new CombatantDescriptor("enemy-1", "team-enemies", CombatantController.Ai, false),
            "Enemy_2");
        registry.Register(
            new CombatantDescriptor("enemy-2", "team-enemies", CombatantController.Ai, false),
            "Enemy_3");
        return registry;
    }

    [Test]
    public void ResolvesEachCombatantToItsOwnCharacter()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.Resolve("enemy-1"), Is.EqualTo("Enemy_2"));
        Assert.That(registry.IdOf("Enemy_2"), Is.EqualTo("enemy-1"));
    }

    /// <summary>
    /// The test that measures the job: identity depends on no position, so nobody's
    /// identity moves when a combatant dies. Cf. spec §3.3.
    /// </summary>
    [Test]
    public void IdentitySurvivesTheDeathOfAnotherCombatant()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        // Enemy_1 dies. Nothing is unregistered: the server keeps it in its state with
        // hp 0, and it is that presence which stops its neighbours from sliding.
        // So the dead still resolves — that is the distinctive property here.
        Assert.That(registry.Resolve("enemy-0"), Is.EqualTo("Enemy_1"));

        // And the living keep the identity they had before it died.
        Assert.That(registry.Resolve("enemy-1"), Is.EqualTo("Enemy_2"));
        Assert.That(registry.Resolve("enemy-2"), Is.EqualTo("Enemy_3"));
        Assert.That(registry.IdOf("Enemy_2"), Is.EqualTo("enemy-1"));
        Assert.That(registry.IdOf("Enemy_3"), Is.EqualTo("enemy-2"));
    }

    [Test]
    public void KnowsWhichCombatantIsLocal()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.LocalCombatantId, Is.EqualTo("player"));
        Assert.That(registry.IsLocalCombatant("player"), Is.True);
        Assert.That(registry.IsLocalCombatant("enemy-0"), Is.False);
    }

    [Test]
    public void SplitsCombatantsByTeamRatherThanByRole()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.Opponents(),
            Is.EquivalentTo(new[] { "Enemy_1", "Enemy_2", "Enemy_3" }));
        Assert.That(registry.Allies(), Is.EquivalentTo(new[] { "Player" }));
    }

    [Test]
    public void ReadsBackTheDescriptorOfAKnownCombatant()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        CombatantDescriptor descriptor = registry.DescriptorOf("enemy-2");

        Assert.That(descriptor.TeamId, Is.EqualTo("team-enemies"));
        Assert.That(descriptor.Controller, Is.EqualTo(CombatantController.Ai));
        Assert.That(descriptor.IsLocal, Is.False);
    }

    [Test]
    public void ReturnsNullRatherThanGuessingForAnUnknownCombatant()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.Resolve("enemy-9"), Is.Null);
        Assert.That(registry.Resolve(null), Is.Null);
        Assert.That(registry.IdOf("Ghost"), Is.Null);
        Assert.That(registry.DescriptorOf("enemy-9"), Is.Null);
    }
}
