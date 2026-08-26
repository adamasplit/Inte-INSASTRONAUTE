using System.Collections.Generic;
using NUnit.Framework;

public class PvpFriendSearchTests
{
    private static readonly IReadOnlyList<PvpFriend> Friends = new[]
    {
        new PvpFriend("u-1", "Élodie Martin"),
        new PvpFriend("u-2", "Eloi Bernard"),
        new PvpFriend("u-3", "Camille Durand")
    };

    [Test]
    public void SearchIgnoresCaseAndAccents()
    {
        IReadOnlyList<PvpFriend> matches = PvpFriendSearch.Filter(Friends, "elo", 5);

        Assert.That(matches.Count, Is.EqualTo(2));
        Assert.That(matches[0].UserId, Is.EqualTo("u-1"));
        Assert.That(matches[1].UserId, Is.EqualTo("u-2"));
    }

    [Test]
    public void SearchMatchesAnyPartOfTheDisplayName()
    {
        IReadOnlyList<PvpFriend> matches = PvpFriendSearch.Filter(Friends, "durand", 5);

        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(matches[0].DisplayName, Is.EqualTo("Camille Durand"));
    }

    [Test]
    public void EmptySearchDoesNotDumpTheWholeFriendList()
    {
        Assert.That(PvpFriendSearch.Filter(Friends, "  ", 5).Count, Is.Zero);
    }

    [Test]
    public void ResultCountIsBoundedForTheUnityMenu()
    {
        Assert.That(PvpFriendSearch.Filter(Friends, "e", 1).Count, Is.EqualTo(1));
    }
}
