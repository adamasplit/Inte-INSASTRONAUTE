using NUnit.Framework;

/// <summary>
/// Les nombres viennent du serveur : validateDeckCards ne refuse une carte que si
/// elle porte un collectionCardId que le joueur ne possède pas. Tout écart ici
/// referme le multijoueur sur ceux qui ont déjà une collection.
/// </summary>
public class PvpDeckEligibilityTests
{
    [Test]
    public void ACardWithoutACollectionIdBelongsToEveryone()
    {
        // Le cas qui bloquait tout : sans collection, la grille était vide et aucun
        // deck ne pouvait être composé, donc aucun combat cherché.
        Assert.That(
            PvpDeckEligibility.IsUsable(null, owned: false, multiplayerExclusive: false, 0, 0),
            Is.True);
        Assert.That(
            PvpDeckEligibility.IsUsable("   ", owned: false, multiplayerExclusive: false, 0, 0),
            Is.True);
    }

    [Test]
    public void ACollectionCardStillHasToBeOwned()
    {
        Assert.That(
            PvpDeckEligibility.IsUsable("carte_rare", owned: false, multiplayerExclusive: false, 0, 0),
            Is.False);
        Assert.That(
            PvpDeckEligibility.IsUsable("carte_rare", owned: true, multiplayerExclusive: false, 0, 0),
            Is.True);
    }

    [Test]
    public void AMultiplayerExclusiveIsUnlockedByLevel()
    {
        Assert.That(
            PvpDeckEligibility.IsUsable(null, owned: false, multiplayerExclusive: true, 3, 2),
            Is.False);
        Assert.That(
            PvpDeckEligibility.IsUsable(null, owned: false, multiplayerExclusive: true, 3, 3),
            Is.True);
    }

    [Test]
    public void TheStarterCardsBelongToEveryPool()
    {
        // Le cas oublié par deux des trois copies du filtre : quarante et une cartes
        // que le panneau déclarait compatibles sans jamais les afficher.
        Assert.That(PvpDeckEligibility.BelongsToPool("Starting", "EP"), Is.True);
        Assert.That(PvpDeckEligibility.BelongsToPool("Aucun", "EP"), Is.True);
        Assert.That(PvpDeckEligibility.BelongsToPool("EP", "EP"), Is.True);
    }

    [Test]
    public void AnotherCharactersCardStaysOutOfThePool()
    {
        Assert.That(PvpDeckEligibility.BelongsToPool("MECA", "EP"), Is.False);
    }

    [Test]
    public void AnUnnamedCharacterIsTreatedAsGeneric()
    {
        Assert.That(PvpDeckEligibility.BelongsToPool(null, "EP"), Is.True);
        Assert.That(PvpDeckEligibility.BelongsToPool("", "EP"), Is.True);
    }

    [Test]
    public void OwningAMultiplayerExclusiveDoesNotSkipItsLevel()
    {
        // Le niveau prime : posséder la carte de collection correspondante ne
        // remplace pas la progression que le serveur exige.
        Assert.That(
            PvpDeckEligibility.IsUsable("carte_rare", owned: true, multiplayerExclusive: true, 5, 1),
            Is.False);
    }
}
