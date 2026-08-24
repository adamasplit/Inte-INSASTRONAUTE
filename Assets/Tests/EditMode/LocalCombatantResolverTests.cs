using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class LocalCombatantResolverTests
{
    /// Une vue PvP : le spectateur voit ses propres piles en entier, l'autre lui arrive
    /// en compteurs. Le serveur garantit qu'exactement un des deux champs est non nul.
    private const string PvpView = @"{
        ""combatants"": [
            { ""combatantId"": ""u-alice"", ""teamId"": ""team-0"",
              ""piles"": { ""draw"": [], ""hand"": [], ""discard"": [], ""exhaust"": [] } },
            { ""combatantId"": ""u-bob"", ""teamId"": ""team-1"",
              ""hiddenPiles"": { ""drawCount"": 7, ""handCount"": 3,
                                 ""discard"": [], ""exhaust"": [] } }
        ]
    }";

    [Test]
    public void ThePreferredIdWinsWhenTheSnapshotHoldsIt()
    {
        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(PvpView), "u-bob"),
            Is.EqualTo("u-bob"));
    }

    /// Le PvE passe "player" et doit continuer d'obtenir "player", quelles que soient les
    /// piles : c'est ce qui garantit que cette classe ne change rien au mode qui tourne.
    [Test]
    public void APveSnapshotStillResolvesTheConventionalPlayer()
    {
        string pve = @"{ ""combatants"": [
            { ""combatantId"": ""player"",  ""teamId"": ""team-player"",
              ""piles"": { ""hand"": [] } },
            { ""combatantId"": ""enemy-0"", ""teamId"": ""team-enemies"" } ] }";

        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(pve), "player"),
            Is.EqualTo("player"));
    }

    /// La règle de repli, et la seule qui marche en PvP : le combattant qui montre ses
    /// cartes est celui qui regarde. Elle ne dépend d'aucun champ ajouté au protocole.
    [Test]
    public void TheCombatantShowingItsCardsIsTheViewer()
    {
        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(PvpView), null),
            Is.EqualTo("u-alice"));
        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(PvpView), "u-carol"),
            Is.EqualTo("u-alice"));
    }

    /// Sans personne de caché, la règle de repli ne s'applique pas : un état PvE brut
    /// donne des piles à tout le monde et ne désigne ainsi personne.
    [Test]
    public void WithNobodyHiddenNothingIsInferred()
    {
        string everyoneVisible = @"{ ""combatants"": [
            { ""combatantId"": ""a"", ""teamId"": ""t0"", ""piles"": { ""hand"": [] } },
            { ""combatantId"": ""b"", ""teamId"": ""t1"", ""piles"": { ""hand"": [] } } ] }";

        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(everyoneVisible), null),
            Is.Null);
    }

    /// Deux jeux de piles visibles face à un caché, c'est du co-op : la question « lequel
    /// est moi » n'a plus de réponse unique, et deviner en donnerait une fausse.
    [Test]
    public void TwoVisiblePileSetsResolveNothing()
    {
        string coop = @"{ ""combatants"": [
            { ""combatantId"": ""a"", ""teamId"": ""t0"", ""piles"": { ""hand"": [] } },
            { ""combatantId"": ""b"", ""teamId"": ""t0"", ""piles"": { ""hand"": [] } },
            { ""combatantId"": ""c"", ""teamId"": ""t1"",
              ""hiddenPiles"": { ""drawCount"": 1, ""handCount"": 1 } } ] }";

        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(coop), null), Is.Null);
    }

    [Test]
    public void NothingnessResolvesToNull()
    {
        Assert.That(LocalCombatantResolver.Resolve(null, "player"), Is.Null);
        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse("{}"), "player"), Is.Null);
        Assert.That(LocalCombatantResolver.Resolve(JObject.Parse(PvpView), ""),
            Is.EqualTo("u-alice"));
    }
}
