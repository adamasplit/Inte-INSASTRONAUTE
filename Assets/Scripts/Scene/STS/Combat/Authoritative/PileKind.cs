using System;

public enum PileKind
{
    Draw,
    Hand,
    Discard,
    Exhaust
}

/// <summary>
/// The four pile names the server emits, and nothing else.
///
/// <para>The client used to recognise a pile by substring — anything containing
/// "DRAW" or "DECK" was the draw pile — and to pass unknown names through unchanged.
/// The server's vocabulary is closed and upper case, so a name outside it is a
/// protocol mismatch, and saying so is more useful than guessing. Cf. spec §3.4
/// entry 8.</para>
/// </summary>
public static class PileKinds
{
    public static PileKind? Parse(string wireName)
    {
        if (string.IsNullOrWhiteSpace(wireName))
            return null;

        switch (wireName.Trim().ToUpperInvariant())
        {
            case "DRAW": return PileKind.Draw;
            case "HAND": return PileKind.Hand;
            case "DISCARD": return PileKind.Discard;
            case "EXHAUST": return PileKind.Exhaust;
            default: return null;
        }
    }

    public static string ToWireName(PileKind kind)
    {
        switch (kind)
        {
            case PileKind.Draw: return "DRAW";
            case PileKind.Hand: return "HAND";
            case PileKind.Discard: return "DISCARD";
            case PileKind.Exhaust: return "EXHAUST";
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
