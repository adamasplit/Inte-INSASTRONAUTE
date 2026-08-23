# Couture d'identité des combattants — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remplacer l'identification positionnelle des combattants (`enemies[N]`,
`enemies.IndexOf`) par un registre explicite `combatantId → Character`, ce qui corrige
un bug PvE actif dans les rencontres à plusieurs ennemis.

**Architecture:** Un nouvel assembly C# pur `STS.AuthoritativeCombat`
(`noEngineReferences: true`), sur le modèle de `STS.ReactCombatBridge`, contenant un
registre générique et un lecteur de snapshot. `CombatManager` s'y branche et cesse de
dériver l'identité d'une position de liste. Aucun changement de comportement
observable en PvE, hors la correction du bug.

**Tech Stack:** Unity 6000.3.2f1, C# (netstandard2.1), NUnit via le Test Framework
Unity, Newtonsoft.Json, tests EditMode lancés en batchmode.

**Spec:** `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`
(voir §3.3 pour le défaut corrigé, §4.2 pour le registre, §3.4 entrées 1 et 2 pour le
critère d'acceptation)

## Global Constraints

- **Assembly de test sans moteur.** `Assets/Tests/EditMode/STS.EditModeTests.asmdef`
  a `"noEngineReferences": true` et `"overrideReferences": true`. Tout code testé doit
  donc être du C# pur : **aucun `using UnityEngine`**, aucun `MonoBehaviour`, aucun
  `IEnumerator` de coroutine, aucun `Mathf`, `Debug`, `Time`.
- **Références précompilées disponibles dans les tests :** `nunit.framework.dll` et
  `Newtonsoft.Json.dll` uniquement.
- **Style de test :** NUnit, classe publique sans namespace, `[Test]`, assertions en
  style contrainte (`Assert.That(x, Is.EqualTo(y))`). Modèle :
  `Assets/Tests/EditMode/AuthoritativeCardPlayPileTests.cs`.
- **Branche :** une branche dédiée partant de `experimental`, jamais `experimental`
  directement — `CombatManager.cs` est activement modifié par un autre contributeur.
- **Convention d'identifiants serveur :** le joueur est `player`, les ennemis sont
  `enemy-0`, `enemy-1`, … dans l'ordre de `activeEncounter.enemyIds`. Les équipes PvE
  sont fournies par le champ `teamId` de chaque combattant du snapshot. Ces
  identifiants sont attribués une fois au setup serveur et **ne changent jamais**, y
  compris après la mort d'un combattant.
- **Le serveur ne retire jamais un combattant mort** de `combatants` : un mort y reste
  avec `hp: 0`.
- **Commande de test EditMode :**
  ```bash
  /home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
    -batchmode -nographics -runTests \
    -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
    -testPlatform EditMode \
    -testResults /tmp/editmode-results.xml \
    -logFile - 2>&1 | tail -40
  ```
  Aucun éditeur Unity ne doit avoir le projet ouvert pendant l'exécution (vérifier
  l'absence de `Temp/UnityLockfile`). Le résultat lisible est
  `/tmp/editmode-results.xml` ; en cas d'échec de compilation, l'erreur apparaît dans
  la sortie du `-logFile -`.

---

## Task 0 : Prérequis — assainir l'arbre et créer la branche

Cette tâche ne produit pas de code, mais elle conditionne tout le reste : **lancer
Unity en batchmode avec des `.meta` supprimés lui fait régénérer ces fichiers avec de
nouveaux GUID**, ce qui casse les références des 28 cartes concernées. Or toutes les
tâches suivantes lancent Unity.

**Files:**
- Modify: aucun fichier de code

- [ ] **Step 1: Constater l'état exact**

```bash
cd /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE
git status --porcelain | grep '^ D' | wc -l   # attendu : 28
git status --porcelain | grep '^ M'           # index.json, Inte-INSASTRONAUTE.slnx
ls Assets/StreamingAssets/STSCardData/Acharnement.json   # le .json existe encore
```

- [ ] **Step 2: Trancher le sort des 28 `.meta` avec le propriétaire du dépôt**

Deux issues, au choix de l'humain — **ne pas décider seul** :

*Suppression accidentelle* (le cas probable, puisque les `.json` sont toujours là) :
```bash
git restore Assets/StreamingAssets/STSCardData/
```

*Nettoyage volontaire* : alors les `.json` doivent partir aussi, et le commit doit le
dire. Demander confirmation avant.

- [ ] **Step 3: Vérifier que l'arbre est propre**

```bash
git status --porcelain
```
Attendu : plus aucune ligne `D`. Les deux `M` (`index.json`, `.slnx`) et le fichier
non suivi `docs/superpowers/specs/2026-08-23-*.md` peuvent rester ; ils seront
commités séparément.

- [ ] **Step 4: Créer la branche de travail**

```bash
git fetch origin
git switch -c refactor/combatant-identity-seam origin/experimental
```

- [ ] **Step 5: Vérifier que la suite de tests actuelle passe, avant tout changement**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-baseline.xml \
  -logFile - 2>&1 | tail -40
```
Attendu : tous les tests existants passent. **Si ce n'est pas le cas, s'arrêter et le
signaler** — on ne construit pas sur une base rouge.

- [ ] **Step 6: Commiter l'étude de conception**

```bash
git add docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md \
        docs/superpowers/plans/2026-08-23-combatant-identity-seam.md
git commit -m "docs(sts): study the authoritative combat client generalization

Records why the Unity client must model what the engine models — a set of
combatants with teams and controller types — and inventories the ten
workarounds that stand in for it today, including a live PvE defect in
multi-enemy encounters.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 1 : Prouver le défaut d'indexation

**But :** transformer l'analyse du §3.3 en fait vérifié. Ce test reproduit, en C# pur,
la logique exacte que `CombatManager` applique aujourd'hui, et montre qu'elle désigne
le mauvais combattant après une mort.

**Files:**
- Create: `Assets/Tests/EditMode/CombatantIdentityTests.cs`

**Interfaces:**
- Consumes: rien (le test est autonome)
- Produces: rien (aucune API de production ; ce test documente un défaut)

- [ ] **Step 1: Écrire le test de caractérisation**

Créer `Assets/Tests/EditMode/CombatantIdentityTests.cs` :

```csharp
using System.Collections.Generic;
using NUnit.Framework;

public class CombatantIdentityTests
{
    /// <summary>
    /// Réplique fidèle de ce que fait CombatManager aujourd'hui :
    /// ResolveCombatant (ligne 1941) et GetAuthoritativeCombatantId (ligne 796)
    /// dérivent tous deux l'identité d'une position dans la liste `enemies`,
    /// que CleanupSlainCharactersRoutine (ligne 2359) mute quand un ennemi meurt.
    /// </summary>
    private static string PositionalResolve(List<string> enemies, string combatantId)
    {
        if (!combatantId.StartsWith("enemy-"))
            return null;
        int index = int.Parse(combatantId.Substring("enemy-".Length));
        return index >= 0 && index < enemies.Count ? enemies[index] : null;
    }

    private static string PositionalIdOf(List<string> enemies, string enemy)
    {
        int index = enemies.IndexOf(enemy);
        return index >= 0 ? $"enemy-{index}" : null;
    }

    [Test]
    public void PositionalResolutionIsCorrectWhileNobodyHasDied()
    {
        var enemies = new List<string> { "Enemy_1", "Enemy_2", "Enemy_3" };

        Assert.That(PositionalResolve(enemies, "enemy-1"), Is.EqualTo("Enemy_2"));
        Assert.That(PositionalIdOf(enemies, "Enemy_2"), Is.EqualTo("enemy-1"));
    }

    [Test]
    public void PositionalResolutionMisidentifiesCombatantsAfterADeath()
    {
        // Le serveur garde le mort dans `combatants` avec hp 0 et ne renumérote rien :
        // Enemy_2 reste "enemy-1" et Enemy_3 reste "enemy-2".
        var enemies = new List<string> { "Enemy_1", "Enemy_2", "Enemy_3" };
        enemies.Remove("Enemy_1"); // ce que fait CleanupSlainCharactersRoutine

        // L'état destiné à Enemy_2 atterrit sur Enemy_3.
        Assert.That(PositionalResolve(enemies, "enemy-1"), Is.EqualTo("Enemy_3"));

        // Et une carte visant Enemy_2 part étiquetée "enemy-0", un combattant mort
        // que le moteur refuse comme cible (CombatEngine ligne 775, target.hp() > 0).
        Assert.That(PositionalIdOf(enemies, "Enemy_2"), Is.EqualTo("enemy-0"));
    }
}
```

- [ ] **Step 2: Lancer les tests et constater qu'ils passent**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-results.xml \
  -logFile - 2>&1 | tail -40
```

Attendu : **PASS**, les deux tests. Ce n'est pas un test de régression : il *affirme*
le comportement défectueux actuel. Le second test dit noir sur blanc que
l'identification est fausse après une mort. C'est la preuve demandée par le §3.3.

Si `PositionalResolutionMisidentifiesCombatantsAfterADeath` échoue, c'est que ma
lecture du code était fausse — **s'arrêter et le signaler** avant d'aller plus loin.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/EditMode/CombatantIdentityTests.cs \
        Assets/Tests/EditMode/CombatantIdentityTests.cs.meta
git commit -m "test(sts): pin down how positional identity breaks after a death

Replicates what CombatManager does today and shows that, once an enemy dies
and leaves the local list, the state meant for one combatant lands on another
while a card aimed at it is sent naming a corpse.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 2 : Le registre de combattants

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/STS.AuthoritativeCombat.asmdef`
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantDescriptor.cs`
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantRegistry.cs`
- Modify: `Assets/Tests/EditMode/STS.EditModeTests.asmdef`
- Test: `Assets/Tests/EditMode/CombatantRegistryTests.cs`

**Interfaces:**
- Consumes: rien
- Produces:
  - `enum CombatantController { Human, Ai }`
  - `sealed class CombatantDescriptor` — propriétés `string CombatantId`,
    `string TeamId`, `CombatantController Controller`, `bool IsLocal` ;
    constructeur `CombatantDescriptor(string combatantId, string teamId,
    CombatantController controller, bool isLocal)`
  - `sealed class CombatantRegistry<TCombatant> where TCombatant : class` —
    `void Register(CombatantDescriptor descriptor, TCombatant combatant)`,
    `TCombatant Resolve(string combatantId)`,
    `string IdOf(TCombatant combatant)`,
    `CombatantDescriptor DescriptorOf(string combatantId)`,
    `string LocalCombatantId { get; }`,
    `bool IsLocalCombatant(string combatantId)`,
    `IReadOnlyList<TCombatant> Opponents()`,
    `IReadOnlyList<TCombatant> Allies()`,
    `void Clear()`

Le type est générique pour rester sans dépendance moteur : `Character` référence
`UnityEngine.Mathf` et ne peut pas entrer dans cet assembly. Unity l'instanciera en
`CombatantRegistry<Character>`, les tests en `CombatantRegistry<string>`.

- [ ] **Step 1: Écrire le test qui échoue**

Créer `Assets/Tests/EditMode/CombatantRegistryTests.cs` :

```csharp
using System.Collections.Generic;
using NUnit.Framework;

public class CombatantRegistryTests
{
    private static CombatantRegistry<string> ThreeEnemyEncounter()
    {
        var registry = new CombatantRegistry<string>();
        registry.Register(
            new CombatantDescriptor("player", "team-player", CombatantController.Human, true),
            "Player");
        registry.Register(
            new CombatantDescriptor("enemy-0", "team-enemies", CombatantController.Ai, false),
            "Enemy_1");
        registry.Register(
            new CombatantDescriptor("enemy-1", "team-enemies", CombatantController.Ai, false),
            "Enemy_2");
        registry.Register(
            new CombatantDescriptor("enemy-2", "team-enemies", CombatantController.Ai, false),
            "Enemy_3");
        return registry;
    }

    [Test]
    public void ResolvesEachCombatantToItsOwnCharacter()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.Resolve("enemy-1"), Is.EqualTo("Enemy_2"));
        Assert.That(registry.IdOf("Enemy_2"), Is.EqualTo("enemy-1"));
    }

    /// <summary>
    /// Le test qui mesure le chantier : l'identité ne dépend d'aucune position, donc
    /// la mort d'un combattant ne déplace celle de personne. Cf. spec §3.3.
    /// </summary>
    [Test]
    public void IdentitySurvivesTheDeathOfAnotherCombatant()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        // Enemy_1 meurt. Rien n'est désinscrit : le serveur le garde dans son état
        // avec hp 0, et c'est sa présence qui empêche ses voisins de glisser.
        // Le mort se résout donc toujours, c'est la propriété distinctive ici.
        Assert.That(registry.Resolve("enemy-0"), Is.EqualTo("Enemy_1"));

        // Et les vivants gardent l'identité qu'ils avaient avant sa mort.
        Assert.That(registry.Resolve("enemy-1"), Is.EqualTo("Enemy_2"));
        Assert.That(registry.Resolve("enemy-2"), Is.EqualTo("Enemy_3"));
        Assert.That(registry.IdOf("Enemy_2"), Is.EqualTo("enemy-1"));
        Assert.That(registry.IdOf("Enemy_3"), Is.EqualTo("enemy-2"));
    }

    [Test]
    public void KnowsWhichCombatantIsLocal()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.LocalCombatantId, Is.EqualTo("player"));
        Assert.That(registry.IsLocalCombatant("player"), Is.True);
        Assert.That(registry.IsLocalCombatant("enemy-0"), Is.False);
    }

    [Test]
    public void SplitsCombatantsByTeamRatherThanByRole()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.Opponents(),
            Is.EquivalentTo(new[] { "Enemy_1", "Enemy_2", "Enemy_3" }));
        Assert.That(registry.Allies(), Is.EquivalentTo(new[] { "Player" }));
    }

    [Test]
    public void ReadsBackTheDescriptorOfAKnownCombatant()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        CombatantDescriptor descriptor = registry.DescriptorOf("enemy-2");

        Assert.That(descriptor.TeamId, Is.EqualTo("team-enemies"));
        Assert.That(descriptor.Controller, Is.EqualTo(CombatantController.Ai));
        Assert.That(descriptor.IsLocal, Is.False);
    }

    [Test]
    public void ReturnsNullRatherThanGuessingForAnUnknownCombatant()
    {
        CombatantRegistry<string> registry = ThreeEnemyEncounter();

        Assert.That(registry.Resolve("enemy-9"), Is.Null);
        Assert.That(registry.Resolve(null), Is.Null);
        Assert.That(registry.IdOf("Ghost"), Is.Null);
        Assert.That(registry.DescriptorOf("enemy-9"), Is.Null);
    }
}
```

- [ ] **Step 2: Lancer les tests et vérifier l'échec**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-results.xml \
  -logFile - 2>&1 | tail -40
```

Attendu : **échec de compilation**, `The type or namespace name 'CombatantRegistry'
could not be found`.

- [ ] **Step 3: Créer l'assembly C# pur**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/STS.AuthoritativeCombat.asmdef`,
calqué sur `STS.ReactCombatBridge.asmdef` :

```json
{
  "name": "STS.AuthoritativeCombat",
  "rootNamespace": "",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

`autoReferenced: true` fait que `Assembly-CSharp`, où vit `CombatManager`, y accède
sans configuration supplémentaire.

- [ ] **Step 4: Écrire `CombatantDescriptor`**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantDescriptor.cs` :

```csharp
using System;

public enum CombatantController
{
    Human,
    Ai
}

/// <summary>
/// Ce que le serveur dit d'un combattant, indépendamment de sa représentation à
/// l'écran : qui il est, dans quelle équipe, qui le pilote, et s'il est le nôtre.
/// </summary>
public sealed class CombatantDescriptor
{
    public CombatantDescriptor(
        string combatantId,
        string teamId,
        CombatantController controller,
        bool isLocal)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            throw new ArgumentException("A combatant needs an id", nameof(combatantId));
        if (string.IsNullOrWhiteSpace(teamId))
            throw new ArgumentException("A combatant needs a team", nameof(teamId));

        CombatantId = combatantId;
        TeamId = teamId;
        Controller = controller;
        IsLocal = isLocal;
    }

    public string CombatantId { get; }
    public string TeamId { get; }
    public CombatantController Controller { get; }
    public bool IsLocal { get; }
}
```

- [ ] **Step 5: Écrire `CombatantRegistry`**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantRegistry.cs` :

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// La correspondance entre les combattants que le serveur nomme et les objets que le
/// client affiche.
///
/// <para>Elle existe pour une raison précise : l'identité d'un combattant ne doit
/// jamais se déduire de sa position dans une liste. Le serveur attribue les
/// identifiants une fois pour toutes et garde les morts dans son état, tandis que le
/// client retire les morts de ses listes d'affichage. Dériver l'un de l'autre fait
/// atterrir l'état d'un combattant sur son voisin dès la première mort.</para>
///
/// <para>Le type du combattant est un paramètre générique parce que cet assembly ne
/// référence pas le moteur Unity : c'est ce qui rend cette classe testable.</para>
/// </summary>
public sealed class CombatantRegistry<TCombatant> where TCombatant : class
{
    private readonly Dictionary<string, CombatantDescriptor> descriptors =
        new Dictionary<string, CombatantDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, TCombatant> combatantsById =
        new Dictionary<string, TCombatant>(StringComparer.Ordinal);
    private readonly List<KeyValuePair<string, TCombatant>> registrationOrder =
        new List<KeyValuePair<string, TCombatant>>();

    private string localCombatantId;
    private string localTeamId;

    public string LocalCombatantId => localCombatantId;

    public void Register(CombatantDescriptor descriptor, TCombatant combatant)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));
        if (combatant == null)
            throw new ArgumentNullException(nameof(combatant));
        if (descriptors.ContainsKey(descriptor.CombatantId))
            throw new InvalidOperationException(
                "Combatant already registered: " + descriptor.CombatantId);

        descriptors[descriptor.CombatantId] = descriptor;
        combatantsById[descriptor.CombatantId] = combatant;
        registrationOrder.Add(
            new KeyValuePair<string, TCombatant>(descriptor.CombatantId, combatant));

        if (descriptor.IsLocal)
        {
            localCombatantId = descriptor.CombatantId;
            localTeamId = descriptor.TeamId;
        }
    }

    public TCombatant Resolve(string combatantId)
    {
        if (string.IsNullOrEmpty(combatantId))
            return null;

        return combatantsById.TryGetValue(combatantId, out TCombatant combatant)
            ? combatant
            : null;
    }

    public string IdOf(TCombatant combatant)
    {
        if (combatant == null)
            return null;

        foreach (KeyValuePair<string, TCombatant> entry in registrationOrder)
        {
            if (ReferenceEquals(entry.Value, combatant) || entry.Value.Equals(combatant))
                return entry.Key;
        }
        return null;
    }

    public CombatantDescriptor DescriptorOf(string combatantId)
    {
        if (string.IsNullOrEmpty(combatantId))
            return null;

        return descriptors.TryGetValue(combatantId, out CombatantDescriptor descriptor)
            ? descriptor
            : null;
    }

    public bool IsLocalCombatant(string combatantId)
    {
        return localCombatantId != null
            && string.Equals(localCombatantId, combatantId, StringComparison.Ordinal);
    }

    public IReadOnlyList<TCombatant> Allies()
    {
        return ByTeam(sameTeam: true);
    }

    public IReadOnlyList<TCombatant> Opponents()
    {
        return ByTeam(sameTeam: false);
    }

    public void Clear()
    {
        descriptors.Clear();
        combatantsById.Clear();
        registrationOrder.Clear();
        localCombatantId = null;
        localTeamId = null;
    }

    private IReadOnlyList<TCombatant> ByTeam(bool sameTeam)
    {
        var result = new List<TCombatant>();
        if (localTeamId == null)
            return result;

        foreach (KeyValuePair<string, TCombatant> entry in registrationOrder)
        {
            CombatantDescriptor descriptor = descriptors[entry.Key];
            bool isSameTeam =
                string.Equals(descriptor.TeamId, localTeamId, StringComparison.Ordinal);
            if (isSameTeam == sameTeam)
                result.Add(entry.Value);
        }
        return result;
    }
}
```

- [ ] **Step 6: Autoriser l'assembly de test à voir le nouvel assembly**

Modifier `Assets/Tests/EditMode/STS.EditModeTests.asmdef`, tableau `references` :

```json
  "references": [
    "STS.RunResume",
    "STS.ReactCombatBridge",
    "STS.AuthoritativeCombat"
  ],
```

Ne rien changer d'autre dans ce fichier.

- [ ] **Step 7: Lancer les tests et vérifier qu'ils passent**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-results.xml \
  -logFile - 2>&1 | tail -40
```

Attendu : **PASS** pour les six tests de `CombatantRegistryTests`, et toujours PASS
pour les tests préexistants.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/ \
        Assets/Tests/EditMode/CombatantRegistryTests.cs \
        Assets/Tests/EditMode/CombatantRegistryTests.cs.meta \
        Assets/Tests/EditMode/STS.EditModeTests.asmdef
git commit -m "feat(sts): name combatants instead of counting them

A registry maps the ids the server assigns to the objects the client draws, so
identity stops being a position in a list the client mutates when someone dies.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 3 : Lire les descripteurs depuis le snapshot

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantSnapshotReader.cs`
- Test: `Assets/Tests/EditMode/CombatantSnapshotReaderTests.cs`

**Interfaces:**
- Consumes: `CombatantDescriptor`, `CombatantController` (Task 2)
- Produces:
  - `static class CombatantSnapshotReader` —
    `static IReadOnlyList<CombatantDescriptor> ReadCombatants(JToken combatToken,
    string localCombatantId)`

`Newtonsoft.Json.dll` est déjà une référence précompilée de l'assembly de test, mais
**pas** de `STS.AuthoritativeCombat`. Il faut donc l'ajouter à cet assembly.

- [ ] **Step 1: Écrire le test qui échoue**

Créer `Assets/Tests/EditMode/CombatantSnapshotReaderTests.cs` :

```csharp
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class CombatantSnapshotReaderTests
{
    private const string PveSnapshot = @"{
        ""combatants"": [
            { ""combatantId"": ""player"",  ""teamId"": ""team-player"",
              ""controllerType"": ""HUMAN"", ""hp"": 60 },
            { ""combatantId"": ""enemy-0"", ""teamId"": ""team-enemies"",
              ""controllerType"": ""AI"",   ""hp"": 0 },
            { ""combatantId"": ""enemy-1"", ""teamId"": ""team-enemies"",
              ""controllerType"": ""AI"",   ""hp"": 12 }
        ]
    }";

    [Test]
    public void ReadsEveryCombatantInSnapshotOrder()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants, Has.Count.EqualTo(3));
        Assert.That(combatants[0].CombatantId, Is.EqualTo("player"));
        Assert.That(combatants[1].CombatantId, Is.EqualTo("enemy-0"));
        Assert.That(combatants[2].CombatantId, Is.EqualTo("enemy-1"));
    }

    /// <summary>
    /// Un mort reste dans l'état du serveur avec hp 0 et doit rester enregistré :
    /// c'est ce qui empêche l'identité de ses voisins de glisser. Cf. spec §3.3.
    /// </summary>
    [Test]
    public void KeepsDeadCombatantsSoThatIdentitiesDoNotShift()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants[1].CombatantId, Is.EqualTo("enemy-0"));
    }

    [Test]
    public void ReadsTeamAndControllerType()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants[0].TeamId, Is.EqualTo("team-player"));
        Assert.That(combatants[0].Controller, Is.EqualTo(CombatantController.Human));
        Assert.That(combatants[2].Controller, Is.EqualTo(CombatantController.Ai));
    }

    [Test]
    public void MarksExactlyTheLocalCombatant()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants[0].IsLocal, Is.True);
        Assert.That(combatants[1].IsLocal, Is.False);
        Assert.That(combatants[2].IsLocal, Is.False);
    }

    [Test]
    public void TreatsAnUnknownControllerTypeAsAi()
    {
        // Un combattant dont le type est absent n'est pas le joueur local : le
        // supposer humain lui donnerait la main. L'IA est le défaut sûr.
        string snapshot = @"{ ""combatants"": [
            { ""combatantId"": ""enemy-0"", ""teamId"": ""team-enemies"" } ] }";

        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(snapshot), "player");

        Assert.That(combatants[0].Controller, Is.EqualTo(CombatantController.Ai));
    }

    [Test]
    public void SkipsMalformedCombatantsRatherThanInventingThem()
    {
        string snapshot = @"{ ""combatants"": [
            { ""teamId"": ""team-enemies"", ""controllerType"": ""AI"" },
            { ""combatantId"": ""enemy-1"", ""controllerType"": ""AI"" },
            { ""combatantId"": ""enemy-2"", ""teamId"": ""team-enemies"",
              ""controllerType"": ""AI"" } ] }";

        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(snapshot), "player");

        Assert.That(combatants, Has.Count.EqualTo(1));
        Assert.That(combatants[0].CombatantId, Is.EqualTo("enemy-2"));
    }

    [Test]
    public void ReturnsNothingForAnEmptyOrShapelessSnapshot()
    {
        Assert.That(CombatantSnapshotReader.ReadCombatants(null, "player"), Is.Empty);
        Assert.That(
            CombatantSnapshotReader.ReadCombatants(JObject.Parse("{}"), "player"),
            Is.Empty);
    }
}
```

- [ ] **Step 2: Lancer les tests et vérifier l'échec**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-results.xml \
  -logFile - 2>&1 | tail -40
```

Attendu : **échec de compilation**, `CombatantSnapshotReader` introuvable.

- [ ] **Step 3: Donner accès à Newtonsoft à l'assembly**

Modifier `Assets/Scripts/Scene/STS/Combat/Authoritative/STS.AuthoritativeCombat.asmdef` :

```json
{
  "name": "STS.AuthoritativeCombat",
  "rootNamespace": "",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "Newtonsoft.Json.dll"
  ],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

Noter le passage de `overrideReferences` à `true` : sans lui, `precompiledReferences`
est ignoré.

- [ ] **Step 4: Écrire `CombatantSnapshotReader`**

Créer `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantSnapshotReader.cs` :

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// Lit la liste des combattants d'un état de combat autoritatif.
///
/// <para>Les morts sont conservés : le serveur les garde dans son état avec des
/// points de vie à zéro, et c'est leur présence qui empêche l'identité de leurs
/// voisins de glisser. Un combattant mal formé est ignoré plutôt que complété par
/// des valeurs plausibles.</para>
/// </summary>
public static class CombatantSnapshotReader
{
    public static IReadOnlyList<CombatantDescriptor> ReadCombatants(
        JToken combatToken,
        string localCombatantId)
    {
        var result = new List<CombatantDescriptor>();
        if (!(combatToken is JObject combat) || !(combat["combatants"] is JArray combatants))
            return result;

        foreach (JToken combatantToken in combatants)
        {
            if (!(combatantToken is JObject combatant))
                continue;

            string combatantId = combatant.Value<string>("combatantId");
            string teamId = combatant.Value<string>("teamId");
            if (string.IsNullOrWhiteSpace(combatantId) || string.IsNullOrWhiteSpace(teamId))
                continue;

            result.Add(new CombatantDescriptor(
                combatantId,
                teamId,
                ReadController(combatant.Value<string>("controllerType")),
                string.Equals(combatantId, localCombatantId, StringComparison.Ordinal)));
        }
        return result;
    }

    /// <summary>
    /// Un type de contrôleur absent ou inconnu vaut IA : supposer un humain
    /// donnerait la main à un combattant que le joueur ne pilote pas.
    /// </summary>
    private static CombatantController ReadController(string controllerType)
    {
        return string.Equals(controllerType, "HUMAN", StringComparison.OrdinalIgnoreCase)
            ? CombatantController.Human
            : CombatantController.Ai;
    }
}
```

- [ ] **Step 5: Lancer les tests et vérifier qu'ils passent**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-results.xml \
  -logFile - 2>&1 | tail -40
```

Attendu : **PASS** pour les sept tests de `CombatantSnapshotReaderTests`, et tous les
autres toujours verts.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/ \
        Assets/Tests/EditMode/CombatantSnapshotReaderTests.cs \
        Assets/Tests/EditMode/CombatantSnapshotReaderTests.cs.meta
git commit -m "feat(sts): read combatant descriptors from an authoritative snapshot

Keeps the dead in the list, because their presence is what stops their
neighbours' identities from shifting, and skips a malformed combatant rather
than completing it with plausible values.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 4 : Brancher `CombatManager` sur le registre

C'est la seule tâche qui touche le code moteur, donc la seule non couverte par les
tests EditMode. Elle est délibérément mécanique : aucune logique nouvelle, seulement
le remplacement de trois dérivations positionnelles par des lectures du registre.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

**Interfaces:**
- Consumes: `CombatantRegistry<Character>`, `CombatantDescriptor`,
  `CombatantSnapshotReader.ReadCombatants` (Tasks 2 et 3)
- Produces: rien pour les tâches suivantes de ce plan

- [ ] **Step 1: Déclarer le registre**

Dans `CombatManager`, près des autres champs privés (autour de la ligne 87, à côté de
`authoritativeTimelineEntries`), ajouter :

```csharp
    private readonly CombatantRegistry<Character> combatantRegistry =
        new CombatantRegistry<Character>();
    private bool combatantRegistryBuilt;
```

Le drapeau est explicite plutôt que déduit de l'état du registre : un combat où le
combattant local serait absent laisserait `LocalCombatantId` à `null`, et le registre
se reconstruirait à chaque état reçu — `Register` lèverait alors sur un identifiant
déjà connu. C'est l'entrée 3 de l'inventaire du §3.4 en miniature : ne pas déduire un
mode d'un effet de bord.

- [ ] **Step 2: Peupler le registre à l'application de l'état**

Dans `ApplyAuthoritativeCombatState`, juste après la garde qui récupère `combatants`
(actuellement autour de la ligne 808, après `if (combatants == null) return;`),
insérer :

```csharp
        if (!combatantRegistryBuilt)
            BuildCombatantRegistry(combatToken);
```

`EnsureAllies()` et `EnsureEncounterEnemies()` s'exécutent lignes 121–122, dans
`Init()`, avant que `BootstrapAuthoritativeCombatRoutine` ne démarre (lignes 182 et
184). `allies` et `enemies` sont donc déjà peuplés quand le premier état arrive, et
c'est ce qui rend la construction par convention correcte à cet instant précis.

Puis ajouter la méthode, à côté de `ResolveCombatant` :

```csharp
    /// <summary>
    /// Associe une fois pour toutes les identifiants du serveur aux Character de la
    /// scène. L'ordre des ennemis vient de `activeEncounter.enemyIds`, celui-là même
    /// dont le serveur tire ses `enemy-{index}`, donc les deux coïncident à la
    /// construction — et n'ont plus jamais besoin de coïncider ensuite.
    /// </summary>
    void BuildCombatantRegistry(JToken combatToken)
    {
        combatantRegistry.Clear();
        combatantRegistryBuilt = true;

        IReadOnlyList<CombatantDescriptor> descriptors =
            CombatantSnapshotReader.ReadCombatants(combatToken, "player");

        foreach (CombatantDescriptor descriptor in descriptors)
        {
            Character combatant = ResolveCombatantByConvention(descriptor.CombatantId);
            if (combatant != null)
                combatantRegistry.Register(descriptor, combatant);
            else
                Debug.LogWarning(
                    $"[STS-COMBAT] No Character for combatant {descriptor.CombatantId}");
        }
    }

    /// <summary>
    /// La convention PvE, utilisée uniquement au moment de construire le registre,
    /// avant qu'aucune mort n'ait pu déplacer quoi que ce soit.
    /// </summary>
    Character ResolveCombatantByConvention(string combatantId)
    {
        if (string.Equals(combatantId, "player", StringComparison.Ordinal))
            return player;

        if (combatantId.StartsWith("enemy-", StringComparison.Ordinal)
            && int.TryParse(combatantId.Substring("enemy-".Length), out int enemyIndex)
            && enemyIndex >= 0
            && enemyIndex < enemies.Count)
        {
            return enemies[enemyIndex];
        }
        return null;
    }
```

- [ ] **Step 3: Remplacer `ResolveCombatant` par une lecture du registre**

Remplacer le corps de `ResolveCombatant` (actuellement lignes 1941–1958) par :

```csharp
    Character ResolveCombatant(string combatantId)
    {
        return combatantRegistry.Resolve(combatantId);
    }
```

- [ ] **Step 4: Remplacer `GetAuthoritativeCombatantId`**

Remplacer le corps (actuellement lignes 788–798) par :

```csharp
    string GetAuthoritativeCombatantId(Character character)
    {
        return combatantRegistry.IdOf(character);
    }
```

- [ ] **Step 5: Rendre le bouton de fin de tour indépendant de la chaîne "player"**

Ligne 864, remplacer :

```csharp
            turnSystem.endTurnButton.interactable = string.Equals(activeCombatantId, "player", StringComparison.Ordinal)
                && !combatEnded;
```

par :

```csharp
            turnSystem.endTurnButton.interactable =
                combatantRegistry.IsLocalCombatant(activeCombatantId) && !combatEnded;
```

- [ ] **Step 6: Vider le registre à la fin du combat**

Dans `OnDestroy`, à côté de `authoritativeMessageQueue.Clear();`, ajouter :

```csharp
        combatantRegistry.Clear();
        combatantRegistryBuilt = false;
```

- [ ] **Step 7: Vérifier que le projet compile et que les tests passent**

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics -runTests \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -testPlatform EditMode \
  -testResults /tmp/editmode-results.xml \
  -logFile - 2>&1 | tail -40
```

Attendu : compilation sans erreur, tous les tests verts. Une erreur de type
`Character` introuvable depuis l'assembly pur signifierait qu'on a mis du code Unity
au mauvais endroit : `CombatantRegistry<Character>` s'instancie côté
`Assembly-CSharp`, pas dans `STS.AuthoritativeCombat`.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): resolve combatants through the registry

The three places that derived a server identity from a list position now read
it from the registry, so a death no longer shifts anyone else's identity, and
the end turn button stops recognising the local player by the string 'player'.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 5 : Non-régression PvE en jeu réel

Les tests EditMode ne couvrent pas `CombatManager`. Cette vérification est manuelle,
et c'est elle qui autorise la fusion.

**Files:**
- Modify: aucun, sauf correctif si un problème apparaît

- [ ] **Step 1: Lancer une run PvE et atteindre une rencontre à plusieurs ennemis**

Les rencontres concernées sont listées dans
`Assets/StreamingAssets/EnemyPool/EnemyPool.json` : chercher celles dont `enemyIds`
contient plusieurs entrées, par exemple `["Enemy_1","Enemy_2","Enemy_3"]`.

- [ ] **Step 2: Vérifier le comportement corrigé**

Tuer le **premier** ennemi de la liste avant les autres, puis vérifier, dans l'ordre :

1. les dégâts infligés ensuite touchent bien l'ennemi visé, et pas son voisin ;
2. les points de vie affichés correspondent à l'ennemi frappé ;
3. les statuts appliqués atterrissent sur la bonne cible ;
4. aucune carte n'est refusée par le serveur avec un message de cible invalide ;
5. le bouton de fin de tour reste actif quand c'est au joueur d'agir.

Avant ce correctif, les points 1 à 4 échouaient.

- [ ] **Step 3: Vérifier une rencontre à un seul ennemi**

Aucun changement attendu : c'est le cas où l'ancien code était déjà correct, donc
c'est le meilleur détecteur de régression introduite par le registre.

- [ ] **Step 4: Vérifier le tutoriel**

Le tutoriel emprunte le chemin **local**, pas autoritatif (`CanBootstrapAuthoritativeCombat`
exige un run et une rencontre). Il ne devrait donc rien voir de ce changement.
Vérifier qu'il se joue toujours de bout en bout — c'est la garantie qu'on n'a pas
touché au chemin local par inadvertance.

- [ ] **Step 5: Mettre à jour l'inventaire de l'étude**

Dans `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`,
§3.4, marquer les entrées 1 et 2 comme traitées, en indiquant la tâche qui les a
retirées. Ne pas les supprimer de la liste : l'inventaire est un état d'avancement.

- [ ] **Step 6: Commit et ouverture de la pull request**

```bash
git add docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md
git commit -m "docs(sts): mark the positional identity workarounds as removed

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
git push -u origin refactor/combatant-identity-seam
```

Ouvrir la pull request vers `experimental`, en mentionnant le contributeur qui
travaille en parallèle sur `CombatManager.cs` — la fusion doit être rapide pour
limiter la divergence.

---

## Ce que ce plan ne fait pas

Les tâches suivantes de l'étude font l'objet de plans séparés, dans cet ordre :

1. **Piles adressées par combattant** — `CombatantPiles`, `LocalPiles`, `RemotePiles`
   (spec §4.3), et l'extraction du rejeu d'événements (§4.4). C'est le gros morceau.
2. **Issue et ciblage** — `CombatOutcomeSource` (§4.5) et le ciblage par équipe (§4.6).
3. **Câblage PvP** — changements serveur (§6), pont React (§7), bootstrap et écran de
   résultat PvP (§5).

Ce découpage est délibéré : `CombatManager.cs` est modifié en parallèle par un autre
contributeur, et chaque plan qui reste petit réduit la fenêtre de collision.
