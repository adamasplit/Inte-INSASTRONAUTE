using System;
using NUnit.Framework;

public class SurrenderConfirmationTests
{
    [Test]
    public void OnePressNeverSurrenders()
    {
        var confirmation = new SurrenderConfirmation();

        Assert.That(confirmation.Press(), Is.False);
        Assert.That(confirmation.IsArmed, Is.True);
    }

    [Test]
    public void TheSecondPressIsTheOneThatSurrenders()
    {
        var confirmation = new SurrenderConfirmation();

        confirmation.Press();

        Assert.That(confirmation.Press(), Is.True);
        Assert.That(confirmation.IsArmed, Is.False, "the gate closes behind the surrender it let through");
    }

    /// Un joueur qui a armé la confirmation puis a joué son tour ne doit pas abandonner en
    /// touchant le même bouton dix minutes plus tard.
    [Test]
    public void AnArmedConfirmationForgottenLongEnoughDisarmsItself()
    {
        var confirmation = new SurrenderConfirmation();
        confirmation.Press();

        confirmation.Advance(SurrenderConfirmation.DefaultWindowSeconds);

        Assert.That(confirmation.IsArmed, Is.False);
        Assert.That(confirmation.Press(), Is.False, "the next press arms again rather than surrendering");
    }

    [Test]
    public void TimePassingInsideTheWindowKeepsItArmed()
    {
        var confirmation = new SurrenderConfirmation(8d);
        confirmation.Press();

        confirmation.Advance(3d);
        confirmation.Advance(3d);

        Assert.That(confirmation.IsArmed, Is.True);
        Assert.That(confirmation.SecondsLeftToConfirm, Is.EqualTo(2d).Within(0.001));
        Assert.That(confirmation.Press(), Is.True);
    }

    [Test]
    public void TimePassingWithNothingArmedChangesNothing()
    {
        var confirmation = new SurrenderConfirmation();

        confirmation.Advance(600d);

        Assert.That(confirmation.IsArmed, Is.False);
        Assert.That(confirmation.SecondsLeftToConfirm, Is.Zero);
    }

    [Test]
    public void ChangingOnesMindDisarmsIt()
    {
        var confirmation = new SurrenderConfirmation();
        confirmation.Press();

        confirmation.Reset();

        Assert.That(confirmation.IsArmed, Is.False);
        Assert.That(confirmation.Press(), Is.False);
    }

    [Test]
    public void TheButtonSaysWhichPressItIs()
    {
        var confirmation = new SurrenderConfirmation();

        Assert.That(confirmation.Label, Is.EqualTo(SurrenderConfirmation.IdleLabel));
        confirmation.Press();
        Assert.That(confirmation.Label, Is.EqualTo(SurrenderConfirmation.ArmedLabel));
        Assert.That(confirmation.Label, Is.Not.EqualTo(SurrenderConfirmation.IdleLabel));
    }

    /// Le serveur fait passer l'abandon par <c>concede</c>, qui appelle <c>moveTheRating</c>
    /// exactement comme le forfait d'un absent. L'avertissement doit le dire, pas le suggérer.
    [Test]
    public void TheWarningSaysTheRatingIsLostAsItWouldBeOnAnAbsence()
    {
        Assert.That(SurrenderConfirmation.Warning, Does.Contain("classement"));
        Assert.That(SurrenderConfirmation.Warning, Does.Contain("quitté"));
    }

    [Test]
    public void AWindowWithNoLengthIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SurrenderConfirmation(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SurrenderConfirmation(-1d));
    }
}
