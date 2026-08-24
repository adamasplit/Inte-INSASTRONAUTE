using NUnit.Framework;

public class CombatRejectionMessagesTests
{
    /// Les huit codes que le moteur émet. Un code sans phrase montrable est un refus que
    /// le joueur subira sans explication — c'est exactement ce qu'on corrige ici.
    [Test]
    public void EveryCodeTheEngineSendsHasSomethingToShow()
    {
        foreach (string code in CombatRejectionMessages.KnownCodes)
        {
            Assert.That(CombatRejectionMessages.ForCode(code), Is.Not.Null.And.Not.Empty,
                "no message for " + code);
        }
        Assert.That(CombatRejectionMessages.KnownCodes, Has.Count.EqualTo(8));
    }

    [Test]
    public void AnUnknownCodeStillProducesSomethingToShow()
    {
        Assert.That(CombatRejectionMessages.ForCode("BRAND_NEW_CODE"),
            Is.Not.Null.And.Not.Empty);
        Assert.That(CombatRejectionMessages.ForCode(null), Is.Not.Null.And.Not.Empty);
    }

    /// L'énergie manquante a déjà son retour visuel — le compteur qui rougit — et c'est
    /// le seul code qui le mérite : les autres ne parlent pas d'énergie.
    [Test]
    public void OnlyMissingEnergyGlowsTheEnergyCounter()
    {
        Assert.That(CombatRejectionMessages.WarrantsEnergyGlow("INSUFFICIENT_ENERGY"),
            Is.True);
        Assert.That(CombatRejectionMessages.WarrantsEnergyGlow("INVALID_TARGET"), Is.False);
        Assert.That(CombatRejectionMessages.WarrantsEnergyGlow(null), Is.False);
    }

    [Test]
    public void CodesAreMatchedExactly()
    {
        Assert.That(CombatRejectionMessages.ForCode("insufficient_energy"),
            Is.EqualTo(CombatRejectionMessages.ForCode("BRAND_NEW_CODE")));
    }
}
