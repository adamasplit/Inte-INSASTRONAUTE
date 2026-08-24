using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Replicates, in pure C#, the identity rules CombatManager applies today, and pins
/// down where they go wrong. These tests assert the defective behaviour on purpose:
/// they are the evidence the design study asks for (spec §3.3), not a regression net.
/// </summary>
public class CombatantIdentityTests
{
    // --- Faithful replicas of CombatManager, as of the multiplayer merge ---------
    // ResolveCombatant and GetAuthoritativeCombatantId both derive identity from a
    // position in `enemies` / `allies`, two lists CleanupSlainCharactersRoutine
    // mutates when a combatant dies.

    private static string PositionalResolve(
        List<string> allies, List<string> enemies, string combatantId)
    {
        if (combatantId == null)
            return null;

        // `player` is a property: allies.FirstOrDefault().
        if (combatantId == "player")
            return allies.FirstOrDefault();

        if (combatantId.StartsWith("player-")
            && int.TryParse(combatantId.Substring("player-".Length), out int allyIndex)
            && allyIndex >= 0
            && allyIndex < allies.Count)
        {
            return allies[allyIndex];
        }

        if (combatantId.StartsWith("enemy-")
            && int.TryParse(combatantId.Substring("enemy-".Length), out int enemyIndex)
            && enemyIndex >= 0
            && enemyIndex < enemies.Count)
        {
            return enemies[enemyIndex];
        }
        return null;
    }

    private static string PositionalIdOf(
        List<string> allies, List<string> enemies, string character, bool isPlayer)
    {
        if (isPlayer)
        {
            int allyIndex = allies.IndexOf(character);
            return allyIndex > 0 ? $"player-{allyIndex}" : "player";
        }

        int enemyIndex = enemies.IndexOf(character);
        return enemyIndex >= 0 ? $"enemy-{enemyIndex}" : null;
    }

    private static List<string> ThreeEnemies() =>
        new List<string> { "Enemy_1", "Enemy_2", "Enemy_3" };

    private static List<string> TwoAllies() =>
        new List<string> { "Ally_1", "Ally_2" };

    // --- Enemy side --------------------------------------------------------------

    [Test]
    public void PositionalResolutionIsCorrectWhileNobodyHasDied()
    {
        List<string> enemies = ThreeEnemies();

        Assert.That(PositionalResolve(TwoAllies(), enemies, "enemy-1"), Is.EqualTo("Enemy_2"));
        Assert.That(PositionalIdOf(TwoAllies(), enemies, "Enemy_2", false), Is.EqualTo("enemy-1"));
    }

    [Test]
    public void PositionalResolutionMisidentifiesCombatantsAfterADeath()
    {
        // The server keeps the dead in `combatants` with hp 0 and renumbers nobody:
        // Enemy_2 stays "enemy-1" and Enemy_3 stays "enemy-2" for the whole fight.
        List<string> enemies = ThreeEnemies();
        enemies.Remove("Enemy_1"); // what CleanupSlainCharactersRoutine does

        // The state meant for Enemy_2 lands on Enemy_3.
        Assert.That(PositionalResolve(TwoAllies(), enemies, "enemy-1"), Is.EqualTo("Enemy_3"));

        // And a card aimed at Enemy_2 goes out labelled "enemy-0", a corpse the engine
        // refuses as a target (CombatEngine line 775, target.hp() > 0).
        Assert.That(PositionalIdOf(TwoAllies(), enemies, "Enemy_2", false), Is.EqualTo("enemy-0"));
    }

    // --- Ally side, introduced by the multiplayer merge (8f17676) ----------------

    [Test]
    public void PositionalAllyResolutionIsCorrectWhileNobodyHasDied()
    {
        List<string> allies = TwoAllies();

        Assert.That(PositionalResolve(allies, ThreeEnemies(), "player"), Is.EqualTo("Ally_1"));
        Assert.That(PositionalResolve(allies, ThreeEnemies(), "player-1"), Is.EqualTo("Ally_2"));
        Assert.That(PositionalIdOf(allies, ThreeEnemies(), "Ally_2", true), Is.EqualTo("player-1"));
    }

    /// <summary>
    /// The same defect, now on the player side: `player` is allies.FirstOrDefault(),
    /// so burying the first ally silently promotes the second one into its identity.
    /// </summary>
    [Test]
    public void PositionalResolutionMisidentifiesAlliesAfterADeath()
    {
        List<string> allies = TwoAllies();
        allies.Remove("Ally_1"); // CleanupSlainCharactersRoutine again

        // "player" now names a different character than it did a turn ago.
        Assert.That(PositionalResolve(allies, ThreeEnemies(), "player"), Is.EqualTo("Ally_2"));

        // And Ally_2, which the server still calls "player-1", now reports itself as
        // "player" — so its own actions go out under a dead combatant's name.
        Assert.That(PositionalIdOf(allies, ThreeEnemies(), "Ally_2", true), Is.EqualTo("player"));
    }

    /// <summary>
    /// Two distinct ids designate the same combatant, which means the mapping is not
    /// a function in either direction: "player-0" never round-trips.
    /// </summary>
    [Test]
    public void TheFirstAllyAnswersToTwoDifferentIdentifiers()
    {
        List<string> allies = TwoAllies();

        Assert.That(PositionalResolve(allies, ThreeEnemies(), "player"), Is.EqualTo("Ally_1"));
        Assert.That(PositionalResolve(allies, ThreeEnemies(), "player-0"), Is.EqualTo("Ally_1"));
        Assert.That(PositionalIdOf(allies, ThreeEnemies(), "Ally_1", true), Is.EqualTo("player"));
    }
}
