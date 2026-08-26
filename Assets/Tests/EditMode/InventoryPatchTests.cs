using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

public class InventoryPatchTests
{
    [Test]
    public void LitLesChampsQueLeServeurEnvoie()
    {
        var patch = JToken.Parse(@"{
            ""goldDelta"": 12,
            ""removedCardInstanceIds"": [""cardinst_a"", ""cardinst_b""],
            ""addedCards"": [{""instanceId"":""cardinst_c"",""cardId"":""katana""}],
            ""enchantedCards"": [{""instanceId"":""cardinst_d"",""cardId"":""frappe""}],
            ""addedRelics"": [{""instanceId"":""relicinst_1"",""relicId"":""EPRelic"",""stage"":0}]
        }");

        STSInventoryPatch read = STSInventoryPatch.Read(patch);

        Assert.That(read.GoldDelta, Is.EqualTo(12));
        Assert.That(read.RemovedCardInstanceIds, Is.EquivalentTo(new[] { "cardinst_a", "cardinst_b" }));
        Assert.That(read.AddedCards.Count, Is.EqualTo(1));
        Assert.That(read.EnchantedCards.Count, Is.EqualTo(1));
        Assert.That(read.AddedRelics.Count, Is.EqualTo(1));
    }

    [Test]
    public void UnPatchVideOuNulNeChangeRien()
    {
        // Toutes les réponses ne portent pas toutes les clés : en lire une absente ne
        // doit pas faire échouer l'application du reste.
        foreach (var patch in new[] { null, JToken.Parse("{}"), JToken.Parse("null") })
        {
            STSInventoryPatch read = STSInventoryPatch.Read(patch);
            Assert.That(read.GoldDelta, Is.Zero);
            Assert.That(read.RemovedCardInstanceIds, Is.Empty);
            Assert.That(read.AddedCards, Is.Empty);
            Assert.That(read.EnchantedCards, Is.Empty);
            Assert.That(read.AddedRelics, Is.Empty);
        }
    }

    [Test]
    public void IgnoreLesEntreesSansIdentifiant()
    {
        var patch = JToken.Parse(@"{""removedCardInstanceIds"": ["""", null, ""cardinst_x""]}");

        STSInventoryPatch read = STSInventoryPatch.Read(patch);

        Assert.That(read.RemovedCardInstanceIds.Single(), Is.EqualTo("cardinst_x"));
    }
}
