using NUnit.Framework;

public class ServerAuthorityTests
{
    [Test]
    public void UneRunServeurDonneLAutoriteAuServeur()
    {
        Assert.That(STSServerAuthority.Decides("2195d2ae-687b-4d6a-a08a-6cf724fce410"), Is.True);
    }

    [Test]
    public void SansRunLeMoteurLocalGardeLaMain()
    {
        // Bac à sable et tutoriel : aucun serveur à interroger, le moteur local doit
        // continuer de fonctionner.
        Assert.That(STSServerAuthority.Decides(null), Is.False);
        Assert.That(STSServerAuthority.Decides(""), Is.False);
        Assert.That(STSServerAuthority.Decides("   "), Is.False);
    }
}
