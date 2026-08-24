using NUnit.Framework;

public class CombatModeTests
{
    /// Ces deux chaînes ne sont pas décoratives : le pont React choisit l'endpoint de
    /// snapshot, la file privée et la destination des commandes à partir d'elles.
    [Test]
    public void TheTwoServerModesHaveTheWireNamesTheBridgeReads()
    {
        Assert.That(CombatModes.ToWireName(CombatMode.Pve), Is.EqualTo("PVE"));
        Assert.That(CombatModes.ToWireName(CombatMode.Pvp), Is.EqualTo("PVP"));
    }

    [Test]
    public void AWireNameRoundTrips()
    {
        Assert.That(CombatModes.Parse(CombatModes.ToWireName(CombatMode.Pvp)),
            Is.EqualTo(CombatMode.Pvp));
        Assert.That(CombatModes.Parse("pve"), Is.EqualTo(CombatMode.Pve));
    }

    /// Un combat local n'a pas de serveur, donc pas de nom sur le fil. Lui en inventer un
    /// ferait connecter le tutoriel à une socket.
    [Test]
    public void ALocalCombatHasNoWireName()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => CombatModes.ToWireName(CombatMode.Local));
    }

    [Test]
    public void AnUnknownOrMissingWireNameIsNoMode()
    {
        Assert.That(CombatModes.Parse(null), Is.Null);
        Assert.That(CombatModes.Parse(""), Is.Null);
        Assert.That(CombatModes.Parse("COOP"), Is.Null);
    }
}
