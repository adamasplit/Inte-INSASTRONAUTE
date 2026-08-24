# Piles adressées par combattant — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remplacer l'accès à l'unique `deck` par un accès adressé
`piles(combatantId)`, en lisant le `combatantId` que les événements du serveur
portent déjà et que le client jette aujourd'hui.

**Architecture:** Un jeu de piles par combattant, indexé dans un registre parallèle à
celui du plan 1. L'abstraction `ICombatantPiles<TCard>` vit dans l'assembly C# pur
`STS.AuthoritativeCombat` avec son implémentation distante ; l'implémentation locale,
qui enveloppe le `DeckManager` existant, reste dans `Assembly-CSharp` parce que
`DeckManager` et `CardInstance` référencent tous deux `UnityEngine`. Le rejeu est
ré-adressé **sur place** : aucun déplacement de fichier, pour garder la surface de
collision minimale.

**Tech Stack:** Unity 6000.3.2f1, C# (netstandard2.1), NUnit via le Test Framework
Unity, Newtonsoft.Json, tests EditMode lancés en batchmode.

**Spec:** `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`
(§4.3 pour le design des piles, §3.4 entrées 5, 6, 8 et 9 pour le critère
d'acceptation)

**Plan précédent :** `2026-08-23-combatant-identity-seam.md` — fournit
`CombatantRegistry<T>`, `CombatantDescriptor` et `CombatantSnapshotReader`, dont ce
plan dépend. Il doit être fusionné avant de commencer.

---

## Ce que ce plan corrige, en une phrase

`ReplayCardMovedEvent` (ligne 1280) et `ReplayPileShuffledEvent` (ligne 1369) lisent
`fromPile`, `toPile` et `pile`, **mais jamais le `combatantId` que le serveur place
dans le même événement**, et écrivent le résultat dans l'unique `deck`. En PvE le
joueur est le seul à avoir des cartes, donc personne ne le voit. En PvP, une carte
piochée par l'adversaire retirerait une carte de la main du joueur local.

---

## Global Constraints

- **Assembly de test sans moteur.** `Assets/Tests/EditMode/STS.EditModeTests.asmdef`
  a `"noEngineReferences": true` et `"overrideReferences": true`. Tout code testé doit
  être du C# pur : **aucun `using UnityEngine`**, aucun `MonoBehaviour`, aucun `Mathf`,
  `Debug`, `Time`.
- **Références précompilées disponibles dans les tests :** `nunit.framework.dll` et
  `Newtonsoft.Json.dll` uniquement.
- **`CardInstance` et `DeckManager` sont des types moteur.** `CardInstance.cs` et
  `DeckManager.cs` font tous deux `using UnityEngine`. Ils ne peuvent pas entrer dans
  `STS.AuthoritativeCombat`. C'est la raison d'être du paramètre générique `TCard` :
  Unity instanciera `CombatantPilesRegistry<CardInstance>`, les tests
  `CombatantPilesRegistry<string>`. Même procédé que `CombatantRegistry<TCombatant>`
  au plan 1.
- **Style de test :** NUnit, classe publique sans namespace, `[Test]`, assertions en
  style contrainte (`Assert.That(x, Is.EqualTo(y))`). Modèles :
  `Assets/Tests/EditMode/CombatantRegistryTests.cs`.
- **Branche :** travailler sur `experimental_refactor`. Ne jamais commiter sur
  `experimental` : `CombatManager.cs` est activement modifié par Etienne PINGLIER.
- **Vocabulaire des piles du serveur — vérifié le 2026-08-23.** Le serveur n'émet que
  quatre valeurs, en majuscules exactes : `"HAND"`, `"DRAW"`, `"DISCARD"`,
  `"EXHAUST"`. Aucune autre orthographe n'existe côté serveur.
- **Champs des événements — vérifiés le 2026-08-23** dans
  `fr.insastronaute.webapi.sts.combat.event` :
  - `CardMoved(combatId, revision, combatantId, cardInstanceId, definitionId,
    fromPile, toPile, destinationIndex)`
  - `PileShuffled(combatId, revision, combatantId, pile, cardInstanceIds)`
  - `CardDrawn(combatId, revision, combatantId, cardInstanceId, definitionId,
    handIndex)`
  - `CardPlayed(combatId, revision, **actorId**, cardInstanceId, definitionId,
    targetIds)`
- **Le nom du propriétaire n'est pas le même partout.** `CardMoved`, `PileShuffled`
  et `CardDrawn` disent `combatantId` ; **`CardPlayed` dit `actorId`**. Lire
  `combatantId` sur un `CardPlayed` donne `null`. Vérifié sur un log de partie réelle
  du 2026-08-23 : `{"actorId":"player","cardInstanceId":…,"eventType":"CardPlayed"}`.
- **`handIndex` est un champ légitime de `CardDrawn`.** Il n'existe pas sur
  `CardMoved`, qui dit `destinationIndex`. Ne pas confondre les deux : seules les
  lectures de `handIndex` **dans `ReplayCardMovedEvent`** sont fautives.
  Les orthographes réellement inventées par le client sont : `sourcePile`,
  `destinationPile`, `toIndex`, `cardID`, `statusName`.
- **Forme des piles dans l'état :** les piles propres du joueur arrivent en entier
  sous `piles: { draw, hand, discard, exhaust }`. Celles d'un adversaire arrivent
  sous `hiddenPiles: { drawCount, handCount, discard, exhaust }`. Le serveur garantit
  qu'**exactement un des deux** est non nul (`StsPvpCombatView`, ligne 98).
- **Commande de test EditMode** — Unity 6000.3.2f1 en batchmode écrit ses résultats
  puis **ne sort pas** ; il faut le tuer une fois le fichier écrit. Utiliser le
  lanceur, qui s'en charge :
  ```bash
  /tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh <tag>
  ```
  À défaut, le recréer : lancer Unity avec `-batchmode -nographics -runTests
  -projectPath <projet> -testPlatform EditMode -testResults /tmp/editmode-<tag>.xml
  -logFile /tmp/unity-<tag>.log`, attendre que le XML soit non vide, puis
  `pkill -KILL -x Unity` et `rm -f Temp/UnityLockfile`.
- **Aucun éditeur Unity ne doit avoir le projet ouvert** pendant l'exécution
  (vérifier l'absence de `Temp/UnityLockfile`).
- **Nombre de tests attendu au départ : 61.** Chaque tâche annonce le nouveau total ;
  un total qui ne monte pas du compte annoncé signifie que les tests n'ont pas été
  découverts, pas qu'ils passent.

---

## Task 0 : Prérequis — base verte

**Files:**
- Modify: aucun fichier de code

- [ ] **Step 1: Vérifier qu'on est sur la bonne branche avec le plan 1 fusionné**

```bash
cd /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE
git branch --show-current                       # attendu : experimental_refactor
git log --oneline -1 -- Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantRegistry.cs
git status --porcelain
```

Attendu : `CombatantRegistry.cs` existe et est commité. L'arbre peut contenir
`Inte-INSASTRONAUTE.slnx` modifié (différence de fins de ligne préexistante) ; rien
d'autre.

- [ ] **Step 2: Vérifier que la suite passe avant tout changement**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh plan2-baseline
```

Attendu : `total=61 passed=61 failed=0 -> Passed`. **Si ce n'est pas le cas,
s'arrêter et le signaler.**

---

## Task 1 : Le vocabulaire des piles

**But :** remplacer la reconnaissance approximative des noms de piles par une
correspondance exacte, et rendre l'inconnu explicite au lieu de le laisser passer.
`ResolvePileName` (ligne 1710) fait aujourd'hui `upper.Contains("HAND")`, puis
retourne `upper` tel quel quand rien ne correspond — une pile inconnue ressort donc
comme une chaîne arbitraire que `GetPileByName` traduira en `null`. C'est l'entrée 8
de l'inventaire.

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/PileKind.cs`
- Test: `Assets/Tests/EditMode/PileKindTests.cs`

**Interfaces:**
- Consumes: rien
- Produces:
  - `enum PileKind { Draw, Hand, Discard, Exhaust }`
  - `static class PileKinds` — `static PileKind? Parse(string wireName)`,
    `static string ToWireName(PileKind kind)`

- [ ] **Step 1: Écrire le test**

Créer `Assets/Tests/EditMode/PileKindTests.cs` :

```csharp
using NUnit.Framework;

public class PileKindTests
{
    [Test]
    public void ParsesTheFourNamesTheServerEmits()
    {
        Assert.That(PileKinds.Parse("HAND"), Is.EqualTo(PileKind.Hand));
        Assert.That(PileKinds.Parse("DRAW"), Is.EqualTo(PileKind.Draw));
        Assert.That(PileKinds.Parse("DISCARD"), Is.EqualTo(PileKind.Discard));
        Assert.That(PileKinds.Parse("EXHAUST"), Is.EqualTo(PileKind.Exhaust));
    }

    /// <summary>
    /// The server emits upper case exactly, but tolerating case costs nothing and
    /// removes a whole class of untraceable mismatch. Tolerating *substrings* is what
    /// we refuse: cf. spec §3.4 entry 8.
    /// </summary>
    [Test]
    public void ToleratesCaseAndSurroundingWhitespaceOnly()
    {
        Assert.That(PileKinds.Parse("hand"), Is.EqualTo(PileKind.Hand));
        Assert.That(PileKinds.Parse("  DRAW  "), Is.EqualTo(PileKind.Draw));
    }

    [Test]
    public void RefusesASpellingTheServerHasNeverEmitted()
    {
        // The old ResolvePileName answered Draw to all three of these, by substring.
        Assert.That(PileKinds.Parse("DRAW_PILE"), Is.Null);
        Assert.That(PileKinds.Parse("DECK"), Is.Null);
        Assert.That(PileKinds.Parse("THE HAND OF FATE"), Is.Null);
    }

    [Test]
    public void RefusesNothingness()
    {
        Assert.That(PileKinds.Parse(null), Is.Null);
        Assert.That(PileKinds.Parse(""), Is.Null);
        Assert.That(PileKinds.Parse("   "), Is.Null);
    }

    [Test]
    public void RoundTripsThroughTheWireName()
    {
        Assert.That(PileKinds.ToWireName(PileKind.Hand), Is.EqualTo("HAND"));
        Assert.That(PileKinds.Parse(PileKinds.ToWireName(PileKind.Exhaust)),
            Is.EqualTo(PileKind.Exhaust));
    }
}
```

- [ ] **Step 2: Écrire l'implémentation**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/PileKind.cs` :

```csharp
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
```

- [ ] **Step 3: Lancer les tests**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task1
```

Attendu : `total=66 passed=66`. Les 5 tests de `PileKindTests` doivent apparaître
nommément :

```bash
python3 -c "
import xml.etree.ElementTree as ET
r=ET.parse('/tmp/editmode-task1.xml').getroot()
n=[tc.get('name') for tc in r.iter('test-case') if 'PileKindTests' in (tc.get('fullname') or '')]
print(len(n), 'decouverts'); [print(' -',x) for x in n]"
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/PileKind.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/PileKind.cs.meta \
        Assets/Tests/EditMode/PileKindTests.cs \
        Assets/Tests/EditMode/PileKindTests.cs.meta
git commit -m "feat(sts): close the pile vocabulary to what the server emits

Recognising a pile by substring meant DECK, DRAW_PILE and DRAW all named the
draw pile, and an unknown name passed through unchanged. The server's vocabulary
is four upper-case words, so anything else is a mismatch worth reporting rather
than guessing at.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 2 : Les piles d'un combattant

**But :** donner une forme à « les piles de ce combattant-là », avec les deux cas que
le serveur distingue : les nôtres en entier, celles d'un autre réduites à des
compteurs.

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/ICombatantPiles.cs`
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/RemotePiles.cs`
- Test: `Assets/Tests/EditMode/RemotePilesTests.cs`

**Interfaces:**
- Consumes: `PileKind` (Task 1)
- Produces:
  - `interface ICombatantPiles<TCard> where TCard : class` —
    `bool IsFullyVisible { get; }`,
    `IList<TCard> Pile(PileKind kind)`,
    `int Count(PileKind kind)`,
    `bool RemoveEverywhere(TCard card)`
  - `sealed class RemotePiles<TCard> : ICombatantPiles<TCard> where TCard : class` —
    constructeur `RemotePiles(int drawCount, int handCount, IEnumerable<TCard> discard,
    IEnumerable<TCard> exhaust)`

`Pile(kind)` renvoie `null` pour une pile qu'on n'a pas le droit de voir, plutôt
qu'une liste vide : une liste vide se confond avec « pile visible et vide », et c'est
précisément la confusion qui ferait écrire dans le vide sans que personne ne s'en
aperçoive.

- [ ] **Step 1: Écrire le test**

Créer `Assets/Tests/EditMode/RemotePilesTests.cs` :

```csharp
using System.Collections.Generic;
using NUnit.Framework;

public class RemotePilesTests
{
    private static RemotePiles<string> OpponentPiles()
    {
        return new RemotePiles<string>(
            drawCount: 7,
            handCount: 3,
            discard: new[] { "burned", "spent" },
            exhaust: new[] { "gone" });
    }

    [Test]
    public void AnnouncesThatItIsNotFullyVisible()
    {
        Assert.That(OpponentPiles().IsFullyVisible, Is.False);
    }

    /// <summary>
    /// The server sends the opponent's draw and hand as counts only, so there is no
    /// list to hand back. Returning null rather than an empty list keeps "you may not
    /// see this" distinguishable from "this is empty". Cf. spec §4.3.
    /// </summary>
    [Test]
    public void HidesTheCardsItWasNeverGiven()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.Pile(PileKind.Draw), Is.Null);
        Assert.That(piles.Pile(PileKind.Hand), Is.Null);
    }

    [Test]
    public void CountsTheHiddenPilesItWasGivenNumbersFor()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.Count(PileKind.Draw), Is.EqualTo(7));
        Assert.That(piles.Count(PileKind.Hand), Is.EqualTo(3));
    }

    [Test]
    public void ShowsThePublicPilesInFull()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.Pile(PileKind.Discard),
            Is.EqualTo(new[] { "burned", "spent" }));
        Assert.That(piles.Pile(PileKind.Exhaust), Is.EqualTo(new[] { "gone" }));
        Assert.That(piles.Count(PileKind.Discard), Is.EqualTo(2));
    }

    [Test]
    public void RemovesFromThePublicPilesOnly()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.RemoveEverywhere("burned"), Is.True);
        Assert.That(piles.Pile(PileKind.Discard), Is.EqualTo(new[] { "spent" }));

        // A card we cannot see cannot be removed, and saying so is the point.
        Assert.That(piles.RemoveEverywhere("never-seen"), Is.False);
    }

    [Test]
    public void TreatsMissingListsAsEmptyRatherThanThrowing()
    {
        var piles = new RemotePiles<string>(0, 0, null, null);

        Assert.That(piles.Pile(PileKind.Discard), Is.Empty);
        Assert.That(piles.Count(PileKind.Exhaust), Is.Zero);
    }
}
```

- [ ] **Step 2: Écrire l'interface**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/ICombatantPiles.cs` :

```csharp
using System.Collections.Generic;

/// <summary>
/// One combatant's four piles.
///
/// <para>Two implementations exist because the server shows two different things: our
/// own piles in full, and another combatant's reduced to counts plus the two public
/// piles. <see cref="Pile"/> returns null for a pile this viewer is not allowed to
/// see, which is deliberately different from an empty list.</para>
/// </summary>
public interface ICombatantPiles<TCard> where TCard : class
{
    /// <summary>True when every pile is readable as a list.</summary>
    bool IsFullyVisible { get; }

    /// <summary>The cards in that pile, or null when they are not ours to see.</summary>
    IList<TCard> Pile(PileKind kind);

    /// <summary>How many cards that pile holds — known even when the cards are not.</summary>
    int Count(PileKind kind);

    /// <summary>
    /// Removes the card from whichever visible pile holds it. Returns false when no
    /// visible pile held it, which includes the case where it sits in a hidden one.
    /// </summary>
    bool RemoveEverywhere(TCard card);
}
```

- [ ] **Step 3: Écrire `RemotePiles`**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/RemotePiles.cs` :

```csharp
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Another combatant's piles, as the server projects them: draw and hand as counts
/// only, discard and exhaust in full because both are public. Mirrors
/// StsPvpCombatView.StsPvpOpponentPiles.
/// </summary>
public sealed class RemotePiles<TCard> : ICombatantPiles<TCard> where TCard : class
{
    private readonly int drawCount;
    private readonly int handCount;
    private readonly List<TCard> discard;
    private readonly List<TCard> exhaust;

    public RemotePiles(
        int drawCount,
        int handCount,
        IEnumerable<TCard> discard,
        IEnumerable<TCard> exhaust)
    {
        this.drawCount = drawCount < 0 ? 0 : drawCount;
        this.handCount = handCount < 0 ? 0 : handCount;
        this.discard = discard == null ? new List<TCard>() : discard.ToList();
        this.exhaust = exhaust == null ? new List<TCard>() : exhaust.ToList();
    }

    public bool IsFullyVisible => false;

    public IList<TCard> Pile(PileKind kind)
    {
        switch (kind)
        {
            case PileKind.Discard: return discard;
            case PileKind.Exhaust: return exhaust;
            default: return null;   // draw and hand are counts only
        }
    }

    public int Count(PileKind kind)
    {
        switch (kind)
        {
            case PileKind.Draw: return drawCount;
            case PileKind.Hand: return handCount;
            case PileKind.Discard: return discard.Count;
            case PileKind.Exhaust: return exhaust.Count;
            default: return 0;
        }
    }

    public bool RemoveEverywhere(TCard card)
    {
        if (card == null)
            return false;

        bool removed = discard.Remove(card);
        return exhaust.Remove(card) || removed;
    }
}
```

- [ ] **Step 4: Lancer les tests**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task2
```

Attendu : `total=72 passed=72`, dont 6 dans `RemotePilesTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/ICombatantPiles.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/ICombatantPiles.cs.meta \
        Assets/Scripts/Scene/STS/Combat/Authoritative/RemotePiles.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/RemotePiles.cs.meta \
        Assets/Tests/EditMode/RemotePilesTests.cs \
        Assets/Tests/EditMode/RemotePilesTests.cs.meta
git commit -m "feat(sts): give a combatant's piles a shape, visible or not

The server shows our own piles in full and another combatant's as counts plus
the two public piles. Pile() answers null for what we may not see, which stays
distinguishable from an empty pile — a distinction the single deck could not
make.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 3 : Le registre des piles

**But :** l'équivalent, pour les piles, de ce que `CombatantRegistry` est pour les
combattants. Séparé plutôt que fusionné parce que les piles arrivent à chaque état
alors que l'identité est fixée une fois, et parce qu'un combattant sans piles est
normal (un ennemi PvE n'en a pas).

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantPilesRegistry.cs`
- Test: `Assets/Tests/EditMode/CombatantPilesRegistryTests.cs`

**Interfaces:**
- Consumes: `ICombatantPiles<TCard>`, `PileKind` (Task 2)
- Produces:
  - `sealed class CombatantPilesRegistry<TCard> where TCard : class` —
    `void Set(string combatantId, ICombatantPiles<TCard> piles)`,
    `ICombatantPiles<TCard> For(string combatantId)`,
    `IList<TCard> Pile(string combatantId, PileKind kind)`,
    `void Clear()`

- [ ] **Step 1: Écrire le test**

Créer `Assets/Tests/EditMode/CombatantPilesRegistryTests.cs` :

```csharp
using System.Collections.Generic;
using NUnit.Framework;

public class CombatantPilesRegistryTests
{
    private static CombatantPilesRegistry<string> TwoCombatants()
    {
        var registry = new CombatantPilesRegistry<string>();
        registry.Set("player", new RemotePiles<string>(
            drawCount: 5, handCount: 2,
            discard: new[] { "mine-discarded" }, exhaust: null));
        registry.Set("enemy-0", new RemotePiles<string>(
            drawCount: 9, handCount: 4,
            discard: new[] { "theirs-discarded" }, exhaust: null));
        return registry;
    }

    [Test]
    public void KeepsEachCombatantsPilesApart()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        Assert.That(registry.Pile("player", PileKind.Discard),
            Is.EqualTo(new[] { "mine-discarded" }));
        Assert.That(registry.Pile("enemy-0", PileKind.Discard),
            Is.EqualTo(new[] { "theirs-discarded" }));
    }

    [Test]
    public void CountsBelongToTheirOwner()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        Assert.That(registry.For("player").Count(PileKind.Draw), Is.EqualTo(5));
        Assert.That(registry.For("enemy-0").Count(PileKind.Draw), Is.EqualTo(9));
    }

    /// <summary>
    /// A combatant we hold no piles for is the normal PvE case for an enemy. Answering
    /// null lets the caller skip the event instead of writing into someone else's
    /// deck, which is what the single deck did. Cf. spec §4.3.
    /// </summary>
    [Test]
    public void ReturnsNullForACombatantItHoldsNoPilesFor()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        Assert.That(registry.For("enemy-1"), Is.Null);
        Assert.That(registry.Pile("enemy-1", PileKind.Hand), Is.Null);
        Assert.That(registry.For(null), Is.Null);
    }

    [Test]
    public void ReplacesPilesWhenAFresherStateArrives()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        registry.Set("player", new RemotePiles<string>(
            drawCount: 1, handCount: 0, discard: null, exhaust: null));

        Assert.That(registry.For("player").Count(PileKind.Draw), Is.EqualTo(1));
        Assert.That(registry.Pile("player", PileKind.Discard), Is.Empty);
    }

    [Test]
    public void ForgetsEverythingOnClear()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        registry.Clear();

        Assert.That(registry.For("player"), Is.Null);
    }
}
```

- [ ] **Step 2: Écrire l'implémentation**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantPilesRegistry.cs` :

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Which piles belong to which combatant.
///
/// <para>Separate from <c>CombatantRegistry</c> on purpose: identity is settled once
/// at setup and never moves, whereas piles are replaced by every state that arrives.
/// Holding no piles for a combatant is normal — a PvE enemy has none — so
/// <see cref="For"/> answers null rather than inventing an empty set.</para>
/// </summary>
public sealed class CombatantPilesRegistry<TCard> where TCard : class
{
    private readonly Dictionary<string, ICombatantPiles<TCard>> pilesByCombatant =
        new Dictionary<string, ICombatantPiles<TCard>>(StringComparer.Ordinal);

    public void Set(string combatantId, ICombatantPiles<TCard> piles)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            throw new ArgumentException("Piles need an owner", nameof(combatantId));
        if (piles == null)
            throw new ArgumentNullException(nameof(piles));

        pilesByCombatant[combatantId] = piles;
    }

    public ICombatantPiles<TCard> For(string combatantId)
    {
        if (string.IsNullOrEmpty(combatantId))
            return null;

        return pilesByCombatant.TryGetValue(combatantId, out ICombatantPiles<TCard> piles)
            ? piles
            : null;
    }

    /// <summary>
    /// Convenience for the common read. Null means either "no such combatant" or
    /// "that pile is not ours to see"; both call for skipping, not guessing.
    /// </summary>
    public IList<TCard> Pile(string combatantId, PileKind kind)
    {
        return For(combatantId)?.Pile(kind);
    }

    public void Clear()
    {
        pilesByCombatant.Clear();
    }
}
```

- [ ] **Step 3: Lancer les tests**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task3
```

Attendu : `total=77 passed=77`, dont 5 dans `CombatantPilesRegistryTests`.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantPilesRegistry.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantPilesRegistry.cs.meta \
        Assets/Tests/EditMode/CombatantPilesRegistryTests.cs \
        Assets/Tests/EditMode/CombatantPilesRegistryTests.cs.meta
git commit -m "feat(sts): index piles by the combatant that owns them

Kept apart from the combatant registry because identity is settled once while
piles are replaced by every state, and because holding no piles for a combatant
is the normal case for a PvE enemy rather than an error.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 4 : Les piles locales, adossées au `DeckManager`

**But :** faire entrer le `DeckManager` existant dans l'abstraction sans rien changer
à ce qu'il fait. Il reste le seul propriétaire de l'UI de main, des animations et des
événements `OnCardDrawn` / `OnCardDiscarded` / `OnCardExhausted` /
`OnCardAddedToHand`.

Ce fichier vit dans `Assembly-CSharp`, **pas** dans `STS.AuthoritativeCombat` :
`DeckManager` et `CardInstance` font tous deux `using UnityEngine`. Il n'est donc pas
couvert par les tests EditMode ; c'est un adaptateur de quinze lignes utiles, et la
tâche 8 le vérifie en jeu.

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/LocalPiles.cs`

**Interfaces:**
- Consumes: `ICombatantPiles<TCard>`, `PileKind` (Task 2), `DeckManager`,
  `CardInstance`
- Produces:
  - `sealed class LocalPiles : ICombatantPiles<CardInstance>` — constructeur
    `LocalPiles(DeckManager deck)`

- [ ] **Step 1: Écrire l'adaptateur**

Créer `Assets/Scripts/Scene/STS/Combat/LocalPiles.cs` :

```csharp
using System.Collections.Generic;

/// <summary>
/// The local player's piles, which are the DeckManager's own lists.
///
/// <para>This is an adapter and nothing more: it hands back the very lists the
/// DeckManager holds, so the hand UI, the animations and the OnCard* events keep
/// working off the same objects they always did. It lives outside
/// STS.AuthoritativeCombat because DeckManager and CardInstance both reference the
/// Unity engine.</para>
/// </summary>
public sealed class LocalPiles : ICombatantPiles<CardInstance>
{
    private readonly DeckManager deck;

    public LocalPiles(DeckManager deck)
    {
        this.deck = deck;
    }

    public bool IsFullyVisible => true;

    public IList<CardInstance> Pile(PileKind kind)
    {
        if (deck == null)
            return null;

        switch (kind)
        {
            case PileKind.Draw: return deck.drawPile;
            case PileKind.Hand: return deck.hand;
            case PileKind.Discard: return deck.discardPile;
            case PileKind.Exhaust: return deck.exhaustPile;
            default: return null;
        }
    }

    public int Count(PileKind kind)
    {
        IList<CardInstance> pile = Pile(kind);
        return pile == null ? 0 : pile.Count;
    }

    public bool RemoveEverywhere(CardInstance card)
    {
        if (deck == null || card == null)
            return false;

        bool removed = deck.hand.Remove(card);
        removed |= deck.drawPile.Remove(card);
        removed |= deck.discardPile.Remove(card);
        removed |= deck.exhaustPile.Remove(card);
        return removed;
    }
}
```

- [ ] **Step 2: Vérifier que le projet compile**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task4
```

Attendu : `total=77 passed=77` — aucun test nouveau, mais la compilation d'
`Assembly-CSharp` est le point de cette étape. Une erreur `ICombatantPiles<> not
found` signifierait que `LocalPiles.cs` a été placé sous
`Assets/Scripts/Scene/STS/Combat/Authoritative/`, donc dans l'assembly pur, où
`CardInstance` est invisible : le déplacer d'un niveau au-dessus.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/LocalPiles.cs \
        Assets/Scripts/Scene/STS/Combat/LocalPiles.cs.meta
git commit -m "feat(sts): expose the DeckManager's lists as one combatant's piles

An adapter, deliberately: it hands back the DeckManager's own lists so the hand
UI, the animations and the OnCard* subscriptions keep operating on the same
objects. It sits outside the pure assembly because DeckManager and CardInstance
both reference the engine.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 5 : Peupler le registre des piles à chaque état

**But :** brancher le registre sur `ApplyAuthoritativeCombatState`, sans encore
changer un seul lecteur. À la fin de cette tâche le registre est correct et
inutilisé ; le comportement du jeu est strictement inchangé.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

**Interfaces:**
- Consumes: `CombatantPilesRegistry<CardInstance>`, `LocalPiles`,
  `RemotePiles<CardInstance>`, `PileKinds` (Tasks 1–4)
- Produces: `CombatantPilesRegistry<CardInstance> combatantPiles` — champ privé lu
  par les tâches 6 et 7

- [ ] **Step 1: Déclarer le registre**

Dans `CombatManager`, juste après la déclaration de `combatantRegistryBuilt` (issue
du plan 1, autour de la ligne 95) :

```csharp
    private readonly CombatantPilesRegistry<CardInstance> combatantPiles =
        new CombatantPilesRegistry<CardInstance>();
```

Pas de drapeau « déjà construit » ici, à la différence du registre d'identité : les
piles changent à chaque état, et c'est précisément ce qu'on veut refléter.

- [ ] **Step 2: Remplir le registre à l'application de l'état**

Dans `ApplyAuthoritativeCombatState`, la boucle qui parcourt les combattants lit déjà
`combatantToken["piles"]` pour le joueur. Ajouter, dans cette même boucle et juste
avant le `if (target.isPlayer && ...)` existant :

```csharp
                RegisterCombatantPiles(combatantId, target, combatantToken);
```

Puis ajouter la méthode, à côté de `BuildCombatantRegistry` :

```csharp
    /// <summary>
    /// Records whose piles are whose for this state. The local combatant keeps the
    /// DeckManager — it owns the hand UI and the animations — while anyone else is
    /// held as the server projects them: counts for draw and hand, cards for the two
    /// public piles.
    /// </summary>
    void RegisterCombatantPiles(string combatantId, Character combatant, JToken combatantToken)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            return;

        if (combatant != null && combatant == player && deck != null)
        {
            combatantPiles.Set(combatantId, new LocalPiles(deck));
            return;
        }

        JToken hidden = combatantToken["hiddenPiles"];
        if (hidden != null && hidden.Type == JTokenType.Object)
        {
            combatantPiles.Set(combatantId, new RemotePiles<CardInstance>(
                hidden.Value<int?>("drawCount") ?? 0,
                hidden.Value<int?>("handCount") ?? 0,
                ReadPileCards(hidden["discard"]),
                ReadPileCards(hidden["exhaust"])));
        }
    }

    /// <summary>
    /// Builds the card objects of a pile we are allowed to see. A card the catalogue
    /// does not know is skipped rather than replaced by a blank, so a gap stays
    /// visible instead of becoming a plausible card.
    /// </summary>
    List<CardInstance> ReadPileCards(JToken pileToken)
    {
        var cards = new List<CardInstance>();
        if (!(pileToken is JArray pile))
            return cards;

        foreach (JToken cardToken in pile)
        {
            string instanceId = cardToken.Value<string>("instanceId")
                ?? cardToken.Value<string>("cardInstanceId");
            string definitionId = cardToken.Value<string>("definitionId");

            CardInstance card = FindCardByInstanceId(instanceId)
                ?? BuildCardFromDefinition(definitionId, instanceId);
            if (card != null)
                cards.Add(card);
        }
        return cards;
    }
```

- [ ] **Step 3: Vider le registre à la fin du combat**

Dans `OnDestroy`, à côté de `combatantRegistry.Clear();` ajouté par le plan 1 :

```csharp
        combatantPiles.Clear();
```

- [ ] **Step 4: Vérifier que le projet compile et que rien n'a bougé**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task5
```

Attendu : `total=77 passed=77`. Le comportement du jeu est inchangé : le registre est
rempli mais personne ne le lit encore.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "feat(sts): record whose piles are whose on every state

Nothing reads the registry yet, so behaviour is unchanged. The local combatant
keeps the DeckManager because it owns the hand UI and the animations; anyone
else is held as the server projects them.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 6 : Ré-adresser le rejeu

**But :** la tâche qui achète la propriété. Les dix-huit accès à `deck.` du bloc de
rejeu et les trois appels à `GetPileByName` deviennent des lectures adressées.

C'est la tâche la plus délicate du plan, et la seule non couverte par les tests
EditMode. Elle est mécanique mais nombreuse : ne pas la fusionner avec une autre.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

**Interfaces:**
- Consumes: `combatantPiles` (Task 5), `PileKinds` (Task 1)
- Produces: rien pour les tâches suivantes

- [ ] **Step 1: Remplacer `GetPileByName` par une lecture adressée**

Remplacer la méthode entière (autour de la ligne 1918) par :

```csharp
    /// <summary>
    /// The named pile of the named combatant, or null when either is unknown to us.
    /// Null is a refusal to guess: the caller must skip, not fall back on the local
    /// deck. Cf. spec §3.4 entries 6 and 9.
    /// </summary>
    List<CardInstance> GetPileByName(string combatantId, string pileName)
    {
        PileKind? kind = PileKinds.Parse(pileName);
        if (kind == null)
            return null;

        return combatantPiles.Pile(combatantId, kind.Value) as List<CardInstance>;
    }
```

`LocalPiles.Pile` renvoie les `List<CardInstance>` du `DeckManager` et
`RemotePiles.Pile` une `List<TCard>` : le `as` réussit dans les deux cas. Il renvoie
null si une implémentation future rend un autre `IList`, ce qui est le comportement
voulu — refuser plutôt que deviner.

- [ ] **Step 2: Supprimer `ResolvePileName`**

Supprimer la méthode entière (autour de la ligne 1710). `PileKinds.Parse` la remplace,
et Task 1 a montré que sa reconnaissance par sous-chaîne acceptait des noms que le
serveur n'émet pas.

- [ ] **Step 3: Ré-adresser `ReplayCardMovedEvent`**

Remplacer le début de la méthode (autour de la ligne 1280) jusqu'au bloc de
suppression par :

```csharp
    IEnumerator ReplayCardMovedEvent(JToken combatEvent)
    {
        string combatantId = combatEvent.Value<string>("combatantId");
        string cardInstanceId = combatEvent.Value<string>("cardInstanceId");
        string definitionId = combatEvent.Value<string>("definitionId");
        string fromPile = combatEvent.Value<string>("fromPile");
        string toPile = combatEvent.Value<string>("toPile");

        ICombatantPiles<CardInstance> piles = combatantPiles.For(combatantId);
        if (piles == null)
            yield break;

        CardInstance card = FindCardByInstanceId(cardInstanceId)
            ?? BuildCardFromDefinition(definitionId, cardInstanceId);
        if (card == null)
            yield break;

        List<CardInstance> fromList = GetPileByName(combatantId, fromPile);
        List<CardInstance> toList = GetPileByName(combatantId, toPile);
        if (fromList != null)
        {
            fromList.Remove(card);
        }
        else
        {
            piles.RemoveEverywhere(card);
        }
```

Le reste de la méthode est inchangé, à ceci près que l'index de destination se lit
désormais sur le seul champ que le serveur émette (voir tâche 7).

- [ ] **Step 4: Ré-adresser `ReplayPileShuffledEvent`**

Remplacer les deux premières lignes utiles de la méthode (autour de la ligne 1374) :

```csharp
        string combatantId = combatEvent.Value<string>("combatantId");
        List<CardInstance> pile = GetPileByName(combatantId, combatEvent.Value<string>("pile"));
        if (pile == null)
            yield break;
```

Noter la disparition du `?? deck?.drawPile` : une pile inconnue ne se déverse plus
dans la pioche du joueur local. C'est l'entrée 9 de l'inventaire.

Plus bas dans la même méthode, le bloc qui retire la carte des quatre piles
(actuellement lignes 1398–1401) devient :

```csharp
                    combatantPiles.For(combatantId)?.RemoveEverywhere(card);
```

- [ ] **Step 5: Ré-adresser les accès restants du bloc de rejeu**

Reprendre chacune des lignes suivantes, qui écrivent aujourd'hui dans `deck` sans
regarder à qui appartient l'événement. Pour chacune, lire d'abord
`string combatantId = combatEvent.Value<string>("combatantId");` en tête de méthode
si elle ne le fait pas déjà, puis :

- ligne 1173 (`ReplayCardPlayedEvent`) — **attention, cet événement nomme son
  propriétaire `actorId`, pas `combatantId`** ; la méthode le lit déjà dans sa
  variable `actorId` (ligne 1139), donc la réutiliser telle quelle et **ne pas**
  ajouter de lecture de `combatantId` ici.
  `AuthoritativeCombatStateReducer.MoveCard(deck.hand, deck.discardPile, card);`
  devient :
  ```csharp
              ICombatantPiles<CardInstance> actorPiles = combatantPiles.For(actorId);
              if (actorPiles != null)
              {
                  AuthoritativeCombatStateReducer.MoveCard(
                      actorPiles.Pile(PileKind.Hand) as List<CardInstance>,
                      actorPiles.Pile(PileKind.Discard) as List<CardInstance>,
                      card);
              }
  ```
- lignes 1267–1273 (`ReplayCardDrawnEvent`) — les trois `Remove` suivis de
  `InsertCardAt(deck.hand, …)` deviennent :
  ```csharp
          ICombatantPiles<CardInstance> drawPiles = combatantPiles.For(combatantId);
          if (drawPiles == null)
              yield break;

          drawPiles.RemoveEverywhere(card);
          List<CardInstance> hand = drawPiles.Pile(PileKind.Hand) as List<CardInstance>;
          if (hand != null && !hand.Contains(card))
              InsertCardAt(hand, card, handIndex);
  ```

- [ ] **Step 6: Vérifier qu'il ne reste aucun accès `deck.` dans le bloc de rejeu**

```bash
awk 'NR>=1015 && NR<=1650 && /deck\./ {print NR": "$0}' \
  Assets/Scripts/Scene/STS/Combat/CombatManager.cs
```

Attendu : **aucune ligne**, sauf celles de `RebuildAuthoritativeHand` (autour de 1633
à 1648), qui reconstruit l'affichage de la main du joueur local et doit continuer de
lire `deck` — c'est de l'UI locale, pas du rejeu adressé. Les laisser, et vérifier
qu'il n'en reste que celles-là.

- [ ] **Step 7: Vérifier que le projet compile**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task6
```

Attendu : `total=77 passed=77`, aucune erreur de compilation.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): replay each event into the piles of its own combatant

CardMoved and PileShuffled have always carried a combatantId; the client read
the pile names and threw the owner away, then wrote the result into the one
deck it had. In PvE nobody noticed, because only the player holds cards. In PvP
an opponent drawing a card would have removed one from the local hand.

An unknown pile no longer falls back on the local draw pile either: the event is
skipped instead.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 7 : Retirer les orthographes que le serveur n'émet pas

**But :** entrée 8 de l'inventaire. Chaque orthographe acceptée « au cas où » est une
divergence qu'on ne verra jamais : si le serveur changeait de nom de champ, le client
continuerait de fonctionner sur l'ancien jusqu'au jour où les deux disparaîtraient.

Tâche séparée de la précédente exprès : un relecteur peut vouloir garder ces replis
défensifs tout en acceptant le ré-adressage. Les faits qui justifient de les retirer
sont dans les Global Constraints, vérifiés dans les `record` du serveur.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

**Interfaces:**
- Consumes: rien
- Produces: rien

- [ ] **Step 1: L'index de destination**

Dans `ReplayCardMovedEvent`, remplacer :

```csharp
            int targetIndex = combatEvent.Value<int?>("toIndex")
                ?? combatEvent.Value<int?>("destinationIndex")
                ?? combatEvent.Value<int?>("handIndex")
```

par la seule lecture du champ que `CardMoved` déclare :

```csharp
            int targetIndex = combatEvent.Value<int?>("destinationIndex")
```

Conserver la valeur par défaut qui suit ces lignes dans le code actuel.

- [ ] **Step 2: Le nom du statut**

Autour de la ligne 1116, remplacer :

```csharp
        if (combatEvent["statusType"] != null || combatEvent["status"] != null || combatEvent["statusName"] != null)
```

par :

```csharp
        if (combatEvent["statusType"] != null)
```

- [ ] **Step 3: Les noms de piles dans la même garde**

Autour de la ligne 1124, remplacer :

```csharp
        if (combatEvent["fromPile"] != null || combatEvent["toPile"] != null || combatEvent["sourcePile"] != null || combatEvent["destinationPile"] != null)
```

par :

```csharp
        if (combatEvent["fromPile"] != null || combatEvent["toPile"] != null)
```

- [ ] **Step 4: L'identifiant de carte du statut**

Autour de la ligne 1435, supprimer la ligne de repli :

```csharp
            ?? combatEvent.Value<string>("cardID")
```

- [ ] **Step 5: Vérifier qu'aucune orthographe inventée ne subsiste**

```bash
grep -nE 'sourcePile|destinationPile|"toIndex"|"cardID"|"statusName"' \
  Assets/Scripts/Scene/STS/Combat/CombatManager.cs
```

Attendu : **aucune ligne**.

Deux pièges dans cette vérification :

- **Ne pas chercher `"handIndex"`.** C'est un champ réel de `CardDrawn`, lu
  légitimement par `ReplayCardDrawnEvent` (ligne 1272). Seule sa lecture dans
  `ReplayCardMovedEvent` était fautive, et le Step 1 l'a retirée. Le supprimer de
  `ReplayCardDrawnEvent` casserait la position d'insertion des cartes piochées.
- `status.cardID` (le champ C# de `StatusInstance`, ligne 1446) n'est pas concerné :
  c'est un nom de propriété locale, pas une orthographe de protocole. Le `grep` cible
  les chaînes entre guillemets pour cette raison.

- [ ] **Step 6: Vérifier que le projet compile**

```bash
/tmp/claude-1000/-home-brehan-Documents-Insastronaute/*/scratchpad/run-editmode.sh task7
```

Attendu : `total=77 passed=77`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): read the field names the server actually sends

sourcePile, destinationPile, toIndex, handIndex, cardID and statusName appear in
no server record: the client invented them as fallbacks. Each spelling accepted
just in case is a divergence that can never be observed — if the contract moved,
the client would keep working on the old name until both vanished.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 8 : Non-régression PvE en jeu réel

Les tests EditMode ne couvrent ni `CombatManager` ni `LocalPiles`. Cette
vérification est manuelle, et c'est elle qui autorise la fusion.

**Files:**
- Modify: aucun, sauf correctif si un problème apparaît

- [ ] **Step 1: Une run PvE complète**

Jouer un combat entier et vérifier, dans l'ordre :

1. la pioche donne les bonnes cartes, dans l'ordre que le serveur a tiré ;
2. une carte jouée part de la main vers la défausse, avec son animation ;
3. le remélange en fin de pioche conserve l'ordre annoncé par le serveur ;
4. une carte exilée atteint la pile d'exil et n'en revient pas ;
5. le compte de cartes affiché sur la pioche et la défausse est juste ;
6. les cartes ajoutées en cours de combat (`Désespoir`, cartes créées par un effet)
   atterrissent dans la bonne pile.

Le point 3 est le plus révélateur : c'est celui qui empruntait le repli
`?? deck?.drawPile`, désormais supprimé.

- [ ] **Step 2: Une rencontre à plusieurs ennemis**

Rejouer la vérification du plan 1 — tuer le premier ennemi d'une rencontre à trois —
pour confirmer que le ré-adressage des piles n'a pas défait le ré-adressage des
identités.

- [ ] **Step 3: Le tutoriel**

Le tutoriel emprunte le chemin local : `UsesAuthoritativeCombat` y est faux, donc ni
le registre de piles ni le rejeu ne s'exécutent. Il doit se jouer de bout en bout
sans changement. C'est la garantie qu'on n'a pas touché au chemin local.

- [ ] **Step 4: Mettre à jour l'inventaire de l'étude**

Dans `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`,
§3.4, marquer les entrées **6** (piles non adressées), **8** (orthographes du
protocole) et **9** (repli silencieux) comme traitées, en nommant la tâche qui les a
retirées. Ne pas les supprimer : l'inventaire est un état d'avancement.

L'entrée **5** (`DeckManager` et ses dix gardes) n'est **pas** traitée par ce plan :
`LocalPiles` l'enveloppe sans le remplacer. Le noter explicitement pour que
l'inventaire ne mente pas.

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md
git commit -m "docs(sts): mark the pile addressing workarounds as removed

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Ce que ce plan ne fait pas

- **Il ne déplace pas le rejeu.** Les 635 lignes de `Replay*Event` restent dans
  `CombatManager`. Le §4.4 de l'étude prévoit de les extraire dans
  `AuthoritativeEventReplayer` ; ce plan fait le ré-adressage, qui est le changement
  qui a du sens, et laisse le déménagement, qui est mécanique, à un plan suivant.
  La raison est la collision : Etienne PINGLIER modifie `CombatManager.cs` en
  parallèle, et déplacer 635 lignes rendrait toute fusion douloureuse, alors que les
  éditions de ce plan tiennent en une vingtaine de lignes dispersées.
- **Il ne retire pas les dix gardes de `DeckManager`** (entrée 5). `LocalPiles`
  l'enveloppe ; le remplacer suppose que plus personne n'appelle ses méthodes
  mutantes, ce qui dépend du sort du tutoriel — la question ouverte du §8.4.
- **Il n'affiche rien pour un combattant distant.** `RemotePiles` existe et est
  correct, mais aucun dos de carte n'est rendu à l'écran : c'est du ressort du
  plan PvP (§4.7).
- **Il ne touche ni à l'issue du combat ni au ciblage** (§4.5 et §4.6), qui font
  l'objet du plan suivant.
