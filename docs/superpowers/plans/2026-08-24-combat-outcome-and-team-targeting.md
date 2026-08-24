# L'issue du combat et le ciblage par équipe — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** que le client lise l'issue du combat que le serveur annonce au lieu de la
redeviner, et qu'il désigne ses cibles par équipe au lieu de les chercher dans une liste
`enemies`.

**Architecture :** les plans 1 et 2 ont posé la couture — `CombatantRegistry` connaît
l'équipe de chaque combattant et la nôtre. Ce plan la consomme. Une seule brique neuve,
`CombatOutcomeSource`, en C# pur ; tout le reste est du recâblage de méthodes existantes de
`CombatManager` sur `registry.Allies()` / `registry.Opponents()`.

**Tech Stack :** Unity 6000.3.2f1, C#, NUnit (EditMode), Newtonsoft.Json.

**Spec :** `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`,
§4.5 (issue du combat) et §4.6 (ciblage par équipe).

---

## Ce que ce plan corrige, en une phrase

Le serveur envoie `CombatEnded` avec un `winnerTeamId` ; le client le jette et recalcule
l'issue sur les PV qu'il voit — ce qui affiche une **victoire sur un match nul**, et refuse
de terminer un combat que le serveur a déjà clos.

## Ce qui a été vérifié dans le code avant d'écrire ce plan

Ces constats datent du 2026-08-24 sur `experimental_refactor` après rebase sur
`origin/experimental` (17 commits d'avance). Les numéros de ligne bougeront ; les faits, non.

1. **`CombatEnded` est reçu et jeté.** `CombatManager.cs`, dans le `switch` du rejeu :
   ```csharp
   case "CombatEnded":
       yield return new WaitForSeconds(0.1f);
       break;
   ```
   Le `winnerTeamId` n'est jamais lu.

2. **L'issue est redevinée sur les PV.** `ResolveCombatEndRoutine` :
   ```csharp
   bool alliesSlain  = allies.All(a => a == null || !a.IsAlive);
   bool enemiesSlain = enemies.All(e => e == null || !e.IsAlive);
   if (!alliesSlain && !enemiesSlain) { /* ... */ yield break; }   // refuse de terminer
   combatEnded = true;
   outcome = enemiesSlain ? TeamOutcome.Victory : TeamOutcome.Defeat;
   ```
   Deux conséquences : un **match nul** (les deux équipes anéanties) donne `enemiesSlain ==
   true`, donc **Victoire** ; et si le miroir de PV du client ne voit personne de mort, la
   routine **rend la main sans terminer** un combat que le serveur a clos.

3. **`TeamOutcome` n'a pas de valeur pour le nul** : `{ None, Victory, Defeat }`.

4. **Le registre sait déjà tout ce qu'il faut** — `CombatantRegistry<Character>` expose
   `Allies()`, `Opponents()`, `LocalCombatantId`, `IsLocalCombatant(id)`,
   `DescriptorOf(id).TeamId`. Les plans 1 et 2 l'ont construit ; ce plan ne fait que le lire.

5. **Cinq méthodes de ciblage parcourent `enemies` / `allies`** dans `CombatManager` :
   `GetDisplayTargets`, `AutoCardTargets`, `GetAllCharacters`, `GetAdversaries`,
   `RandomEnemy`.

6. **Une sixième, récente, fait pareil** : `PlayCardEffectFeedback` (ajoutée par Etienne
   PINGLIER le 2026-08-23) résout `targetOthers` en parcourant `enemies`.

7. **`DropZone.Init(cm, target, bool acceptsEnemy)`** porte un booléen là où il faudrait une
   équipe. Ses deux seuls appelants sont dans `UIManager`, qui passe `false` pour les alliés
   et `true` pour les ennemis.

8. **La spec §4.7 est périmée sur un point** : elle annonce que
   `if (combat.player != null)` fige un allié unique dans `UIManager`. Ce n'est plus vrai —
   le code boucle `foreach (var ally in combat.allies)`. **Ne pas chercher cette ligne : elle
   n'existe plus.** Rien à faire de ce côté.

## Global Constraints

- **Ne jamais modifier, committer ou « nettoyer » les fichiers de cartes**
  (`Assets/StreamingAssets/STSCardData/**`) ni quoi que ce soit sous `card/` ou `print/` :
  un humain y travaille en parallèle. Un test qui échoue là n'est pas le vôtre.
- **Aucune commande git qui modifie l'arbre de travail** : ni `checkout --`, ni `restore`,
  ni `stash`, ni `clean`, ni `reset`. `git add` sur des chemins précis uniquement, **jamais**
  `-A` ni `.`.
- L'assembly `STS.AuthoritativeCombat` a `noEngineReferences: true`. **Aucun `using
  UnityEngine`** dans les fichiers qu'on y ajoute, et aucune référence à `Character`,
  `CardInstance` ou `DeckManager`, qui référencent le moteur.
- Le chemin local (tutoriel, `UsesAuthoritativeCombat == false`) doit rester **strictement
  inchangé**. Toute nouvelle logique est conditionnée à la présence d'une donnée serveur.
- Le vocabulaire d'équipe vient du serveur : `teamId` est une chaîne opaque, comparée en
  `StringComparison.Ordinal`. **Ne jamais supposer `"player"` / `"enemy"`.**

## Comment lancer les tests

Unity en batch **écrit ses résultats puis se bloque au lieu de sortir**. Il faut attendre le
fichier de résultats, pas le processus :

```bash
UNITY=~/Unity/Hub/Editor/6000.3.2f1/Editor/Unity
PROJECT=/home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE
rm -f "$PROJECT/Temp/UnityLockfile" /tmp/editmode.xml
"$UNITY" -batchmode -nographics -runTests -projectPath "$PROJECT" \
         -testPlatform EditMode -testResults /tmp/editmode.xml -logFile /tmp/unity.log &
until [ -s /tmp/editmode.xml ]; do sleep 3; done
sleep 2; pkill -x Unity
grep -oP 'total="\d+" passed="\d+" failed="\d+"' /tmp/editmode.xml | head -1
```

Base de départ : **80 tests, 0 échec.** Si ce nombre n'est pas atteint avant la tâche 1,
s'arrêter : l'arbre n'est pas sain et rien de ce qui suit ne sera interprétable.

## Structure des fichiers

| Fichier | Responsabilité |
|---|---|
| `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcome.cs` | **Créé.** L'issue, en C# pur : `Undecided / Victory / Defeat / Draw` |
| `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcomeSource.cs` | **Créé.** `winnerTeamId` + notre `teamId` → une issue |
| `Assets/Tests/EditMode/CombatOutcomeSourceTests.cs` | **Créé.** Sa table de vérité |
| `Assets/Scripts/Scene/STS/Combat/CombatManager.cs` | **Modifié.** Retient l'issue annoncée ; y obéit ; cible par équipe |
| `Assets/Scripts/Scene/STS/UI/DropZone.cs` | **Modifié.** Reçoit une équipe, pas un booléen |
| `Assets/Scripts/Scene/STS/UI/UIManager.cs` | **Modifié.** Passe l'équipe à `DropZone.Init` |

---

## Task 0 : Prérequis — base verte

**Files:** aucun.

- [ ] **Step 1 :** lancer la suite EditMode avec la recette ci-dessus.
- [ ] **Step 2 :** vérifier `total="80" passed="80" failed="0"`. Sinon, **s'arrêter et le
      signaler** — ne rien implémenter sur un arbre rouge.
- [ ] **Step 3 :** noter le commit de départ : `git rev-parse --short HEAD`.

---

## Task 1 : L'issue, en C# pur

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcome.cs`
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcomeSource.cs`
- Test: `Assets/Tests/EditMode/CombatOutcomeSourceTests.cs`

**Interfaces:**
- Produces: `enum CombatOutcome { Undecided, Victory, Defeat, Draw }` et
  `static CombatOutcome CombatOutcomeSource.FromWinner(string winnerTeamId, string localTeamId)`.
  Les tâches 2 et 3 les consomment.

- [ ] **Step 1 : écrire le test qui échoue**

```csharp
using NUnit.Framework;

public class CombatOutcomeSourceTests
{
    [Test]
    public void TheWinningTeamBeingOursIsAVictory()
    {
        Assert.AreEqual(CombatOutcome.Victory,
            CombatOutcomeSource.FromWinner("players", "players"));
    }

    [Test]
    public void AnotherTeamWinningIsADefeat()
    {
        Assert.AreEqual(CombatOutcome.Defeat,
            CombatOutcomeSource.FromWinner("enemies", "players"));
    }

    /// Un combat terminé sans vainqueur est un nul, et c'est le seul cas où le serveur
    /// n'en nomme aucun : CombatEnded n'est émis que sur un combat fini.
    [Test]
    public void NoWinningTeamIsADraw()
    {
        Assert.AreEqual(CombatOutcome.Draw, CombatOutcomeSource.FromWinner(null, "players"));
        Assert.AreEqual(CombatOutcome.Draw, CombatOutcomeSource.FromWinner("", "players"));
        Assert.AreEqual(CombatOutcome.Draw, CombatOutcomeSource.FromWinner("   ", "players"));
    }

    /// Sans savoir de quelle équipe on est, on ne conclut rien — surtout pas une défaite.
    [Test]
    public void WithoutOurOwnTeamNothingIsDecided()
    {
        Assert.AreEqual(CombatOutcome.Undecided, CombatOutcomeSource.FromWinner("players", null));
        Assert.AreEqual(CombatOutcome.Undecided, CombatOutcomeSource.FromWinner("players", ""));
    }

    /// Les identifiants d'équipe sont opaques et comparés à l'octet : une différence de
    /// casse est une autre équipe, pas la nôtre écrite autrement.
    [Test]
    public void TeamIdsAreComparedExactly()
    {
        Assert.AreEqual(CombatOutcome.Defeat,
            CombatOutcomeSource.FromWinner("Players", "players"));
    }
}
```

- [ ] **Step 2 : le lancer et le voir échouer**

Recette EditMode. Attendu : échec de compilation, `CombatOutcome` et `CombatOutcomeSource`
n'existant pas.

- [ ] **Step 3 : écrire `CombatOutcome.cs`**

```csharp
/// <summary>
/// L'issue d'un combat, telle que le serveur la tranche.
///
/// <para><c>Draw</c> n'a pas d'équivalent dans la dérivation locale sur les PV, et c'est
/// précisément le cas qu'elle traduisait faux : les deux équipes anéanties donnaient
/// « tous les ennemis sont morts », donc une victoire.</para>
/// </summary>
public enum CombatOutcome
{
    Undecided,
    Victory,
    Defeat,
    Draw
}
```

- [ ] **Step 4 : écrire `CombatOutcomeSource.cs`**

```csharp
using System;

/// <summary>
/// Qui a gagné, lu plutôt que déduit.
///
/// <para>Le client dérivait l'issue des points de vie qu'il voyait. Ça marche tant que le
/// combat se termine parce que quelqu'un est mort et que le client possède ces PV — deux
/// suppositions que le PvP casse (un forfait clôt un combat sans qu'aucun combattant ne
/// meure) et que le PvE cassait déjà : deux équipes anéanties donnaient une victoire là où
/// le serveur enregistrait un nul.</para>
///
/// <para>À n'appeler que pour un <c>CombatEnded</c> : c'est là seulement qu'un
/// <c>winnerTeamId</c> absent veut dire « match nul » plutôt que « pas encore fini ».</para>
/// </summary>
public static class CombatOutcomeSource
{
    public static CombatOutcome FromWinner(string winnerTeamId, string localTeamId)
    {
        if (string.IsNullOrWhiteSpace(localTeamId))
            return CombatOutcome.Undecided;

        if (string.IsNullOrWhiteSpace(winnerTeamId))
            return CombatOutcome.Draw;

        return string.Equals(winnerTeamId, localTeamId, StringComparison.Ordinal)
            ? CombatOutcome.Victory
            : CombatOutcome.Defeat;
    }
}
```

- [ ] **Step 5 : relancer les tests**

Attendu : **85 tests, 0 échec** (80 + 5).

- [ ] **Step 6 : preuve par mutation**

Remplacer temporairement `StringComparison.Ordinal` par
`StringComparison.OrdinalIgnoreCase` et relancer : `TeamIdsAreComparedExactly` doit
**échouer**. Annuler la mutation, relancer, revert vérifié. Si le test reste vert, il ne
prouve rien et il faut le corriger avant de continuer.

- [ ] **Step 7 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcome.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcome.cs.meta \
        Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcomeSource.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/CombatOutcomeSource.cs.meta \
        Assets/Tests/EditMode/CombatOutcomeSourceTests.cs \
        Assets/Tests/EditMode/CombatOutcomeSourceTests.cs.meta
git commit -m "feat(sts): read the combat's outcome instead of deriving it

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

> Les `.meta` sont générés par Unity au premier import. S'ils n'existent pas encore, ouvrir
> le projet une fois ou lancer la suite EditMode, qui les crée. **Ne pas les écrire à la
> main** — un GUID inventé casse les références.

---

## Task 2 : Retenir l'issue que le serveur annonce

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

**Interfaces:**
- Consumes: `CombatOutcomeSource.FromWinner` (tâche 1).
- Produces: le champ `announcedOutcome`, que la tâche 3 lit.

- [ ] **Step 1 : ajouter `Draw` à `TeamOutcome`**

En haut de `CombatManager.cs` :

```csharp
public enum TeamOutcome
{
    None,
    Victory,
    Defeat,
    Draw
}
```

- [ ] **Step 2 : déclarer le champ**

À côté de `public TeamOutcome outcome { get; private set; } = TeamOutcome.None;` :

```csharp
    /// L'issue que le serveur a annoncée dans son CombatEnded, ou None s'il n'a rien dit.
    /// Distincte de `outcome`, qui reste ce que le combat local dérive : le chemin local
    /// (tutoriel) n'a pas de serveur pour trancher et continue de déduire.
    TeamOutcome announcedOutcome = TeamOutcome.None;
```

- [ ] **Step 3 : le remplir au rejeu de `CombatEnded`**

Remplacer, dans le `switch` du rejeu :

```csharp
                case "CombatEnded":
                    yield return new WaitForSeconds(0.1f);
                    break;
```

par :

```csharp
                case "CombatEnded":
                    RecordAnnouncedOutcome(combatEvent);
                    yield return new WaitForSeconds(0.1f);
                    break;
```

- [ ] **Step 4 : écrire `RecordAnnouncedOutcome`**

À placer près des autres `Replay*` :

```csharp
    /// Le serveur vient de clore le combat et dit qui l'emporte. On le note ici plutôt que
    /// d'agir tout de suite : les événements qui suivent dans le même lot doivent finir de
    /// se jouer, et c'est ResolveCombatEndRoutine qui conclut, une fois les animations
    /// terminées.
    void RecordAnnouncedOutcome(JToken combatEvent)
    {
        string localTeamId = LocalTeamId();
        if (string.IsNullOrEmpty(localTeamId))
            return;

        // Absent du JSON quand le combat est nul : le serveur n'écrit pas de vainqueur.
        string winnerTeamId = combatEvent.Value<string>("winnerTeamId");

        switch (CombatOutcomeSource.FromWinner(winnerTeamId, localTeamId))
        {
            case CombatOutcome.Victory: announcedOutcome = TeamOutcome.Victory; break;
            case CombatOutcome.Defeat:  announcedOutcome = TeamOutcome.Defeat;  break;
            case CombatOutcome.Draw:    announcedOutcome = TeamOutcome.Draw;    break;
            default:                    announcedOutcome = TeamOutcome.None;    break;
        }
    }

    /// L'équipe du combattant local, telle que le registre l'a enregistrée.
    string LocalTeamId()
    {
        string localId = combatantRegistry.LocalCombatantId;
        if (string.IsNullOrEmpty(localId))
            return null;

        CombatantDescriptor descriptor = combatantRegistry.DescriptorOf(localId);
        return descriptor?.TeamId;
    }
```

- [ ] **Step 5 : remettre à zéro au début de chaque combat**

Trouver la ligne `outcome = TeamOutcome.None;` (celle qui prépare un nouveau combat) et
ajouter juste après :

```csharp
        announcedOutcome = TeamOutcome.None;
```

**Pourquoi c'est indispensable :** sans ça, l'issue annoncée du combat précédent survivrait
et le combat suivant se conclurait sur elle. C'est exactement la famille de bugs corrigée
côté serveur ce mois-ci (les PV qui repartaient de l'état précédent).

- [ ] **Step 6 : compiler**

Lancer la suite EditMode. Attendu : **85 tests, 0 échec**, aucune erreur de compilation.
Aucun test ne couvre encore ce code : `CombatManager` est un `MonoBehaviour` que la suite
EditMode ne construit pas. La vérification est la tâche 8.

- [ ] **Step 7 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "feat(sts): record the outcome the server announces

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 3 : Obéir à l'issue annoncée

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs` (`ResolveCombatEndRoutine`)

**Interfaces:**
- Consumes: `announcedOutcome` (tâche 2).

- [ ] **Step 1 : remplacer la dérivation**

Remplacer :

```csharp
        bool alliesSlain = allies.All(a => a == null || !a.IsAlive);
        bool enemiesSlain = enemies.All(e => e == null || !e.IsAlive);

        if (!alliesSlain && !enemiesSlain)
        {
            if (ui != null)
            {
                ui.RefreshUI(false);
            }

            if (turnSystem != null)
            {
                turnSystem.timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
            }

            resolvingCombatCleanup = false;
            yield break;
        }

        combatEnded = true;
        outcome = enemiesSlain ? TeamOutcome.Victory : TeamOutcome.Defeat;
```

par :

```csharp
        // Le serveur a tranché : on le lit. Il connaît des fins que les PV ne racontent pas
        // — un nul, un forfait — et il connaît les PV mieux que nous.
        if (announcedOutcome != TeamOutcome.None)
        {
            combatEnded = true;
            outcome = announcedOutcome;
        }
        else
        {
            // Chemin local : pas de serveur pour trancher, on déduit comme avant.
            bool alliesSlain = allies.All(a => a == null || !a.IsAlive);
            bool enemiesSlain = enemies.All(e => e == null || !e.IsAlive);

            if (!alliesSlain && !enemiesSlain)
            {
                if (ui != null)
                {
                    ui.RefreshUI(false);
                }

                if (turnSystem != null)
                {
                    turnSystem.timelineUI.Display(turnSystem.GetDisplayTimeline(turnSystem.timeline));
                }

                resolvingCombatCleanup = false;
                yield break;
            }

            combatEnded = true;
            outcome = enemiesSlain ? TeamOutcome.Victory : TeamOutcome.Defeat;
        }
```

**Attention à ce que ce changement fait vraiment :** sur le chemin autoritatif, la sortie
anticipée `yield break` **disparaît**. Un combat que le serveur a clos se termine désormais
même si le client croit tout le monde vivant. C'est le but — c'est le second défaut du §4.5.

- [ ] **Step 2 : traiter le nul dans `EndCombat`**

`EndCombat` teste `outcome == TeamOutcome.Victory` puis `outcome == TeamOutcome.Defeat`. Un
`Draw` ne satisfait ni l'un ni l'autre : **l'écran de fin ne s'afficherait pas du tout**, et
le joueur resterait sur un combat figé — une régression pire que le bug corrigé.

Traiter le nul comme une défaite pour la sortie de run, en le disant :

```csharp
        if (outcome == TeamOutcome.Victory)
        {
            // ... inchangé
        }
        else if (outcome == TeamOutcome.Defeat || outcome == TeamOutcome.Draw)
        {
            // ... la branche défaite existante
        }
```

> **Ruling à confirmer avec l'humain avant de committer si l'occasion se présente, sinon
> appliquer tel quel et le consigner :** un nul termine la run comme une défaite. C'est le
> choix conservateur — le serveur n'accorde aucune récompense sur un nul, donc le traiter en
> victoire donnerait un écran de récompenses vide. Coût si faux : un écran de défaite là où
> l'humain voulait un écran distinct ; c'est du texte, pas de la logique.

- [ ] **Step 3 : compiler**

Suite EditMode. Attendu : **85 tests, 0 échec**.

- [ ] **Step 4 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "fix(sts): end the combat the server ended, with the outcome it gave

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 4 : Les adversaires et les alliés, par équipe

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
  (`GetAdversaries`, `GetAllCharacters`, `RandomEnemy`)

- [ ] **Step 1 : écrire les deux accès d'équipe**

À placer près de `ResolveCombatant` :

```csharp
    /// Les vivants de l'équipe adverse à `character`, vus par le registre.
    ///
    /// Sur le chemin local le registre est vide : on retombe sur les listes positionnelles,
    /// qui restent la vérité du tutoriel.
    List<Character> LivingOpponentsOf(Character character)
    {
        string id = combatantRegistry.IdOf(character);
        if (id == null)
            return character != null && character.isPlayer
                ? enemies.Where(e => e != null && e.IsAlive).ToList()
                : allies.Where(a => a != null && a.IsAlive).Cast<Character>().ToList();

        string team = combatantRegistry.DescriptorOf(id)?.TeamId;
        if (string.IsNullOrEmpty(team))
            return new List<Character>();

        return AllRegistered()
            .Where(other => other != null && other.IsAlive)
            .Where(other => !string.Equals(TeamOf(other), team, StringComparison.Ordinal))
            .ToList();
    }

    string TeamOf(Character character)
    {
        string id = combatantRegistry.IdOf(character);
        return id == null ? null : combatantRegistry.DescriptorOf(id)?.TeamId;
    }

    /// Tout le monde, des deux côtés, dans l'ordre où le registre les tient.
    List<Character> AllRegistered()
    {
        var everyone = new List<Character>();
        everyone.AddRange(combatantRegistry.Allies());
        everyone.AddRange(combatantRegistry.Opponents());
        return everyone;
    }
```

> `System` doit être dans les `using` pour `StringComparison` — il y est déjà
> (`using System;` en tête de fichier). Vérifier plutôt que supposer.

- [ ] **Step 2 : recâbler les trois méthodes**

```csharp
    public List<Character> GetAllCharacters()
    {
        if (combatantRegistry.LocalCombatantId == null)
        {
            // Chemin local, inchangé.
            var local = enemies.Where(e => e != null && e.IsAlive).Cast<Character>().ToList();
            foreach (var ally in allies)
            {
                if (ally != null && ally.IsAlive)
                    local.Add(ally);
            }
            return local;
        }

        return AllRegistered().Where(c => c != null && c.IsAlive).ToList();
    }

    public List<Character> GetAdversaries(Character character)
    {
        return LivingOpponentsOf(character);
    }

    public List<Character> RandomEnemy()
    {
        var candidates = LivingOpponentsOf(GetActingPlayer());
        return candidates.Count == 0
            ? new List<Character>()
            : new List<Character> { candidates[UnityEngine.Random.Range(0, candidates.Count)] };
    }
```

**Piège à ne pas rater :** `RandomEnemy` prenait ses candidats dans `enemies` sans jamais
demander qui tire. Passer par `GetActingPlayer()` change son sens quand ce n'est pas le
joueur local qui agit — c'est voulu, et c'est ce qui rend le 2v2 possible.

- [ ] **Step 3 : compiler.** Suite EditMode, **85 tests, 0 échec**.

- [ ] **Step 4 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): find adversaries by team rather than by list

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 5 : Le ciblage des cartes, par équipe

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
  (`GetDisplayTargets`, `AutoCardTargets`)

- [ ] **Step 1 : `GetDisplayTargets`**

Seuls deux cas parcourent une liste ; les autres travaillent déjà sur la carte survolée.

```csharp
            case TargetingMode.AllEnemies:
                return LivingOpponentsOf(GetActingPlayer());
```

`RandomEnemy` et `AllCharacters` héritent de la tâche 4. Les cas `Enemy`, `Player` et
`AnyPlayer` restent inchangés — ils lisent `hovered` et `GetActingPlayer()`.

> `AnyPlayer` teste `hovered.isPlayer`. **Le laisser tel quel dans cette tâche.**
> `isPlayer` reste correct en PvE et le remplacer touche le survol, l'UI et le tutoriel
> d'un coup : c'est le sujet du plan PvP, pas de celui-ci.

- [ ] **Step 2 : `AutoCardTargets`**

```csharp
        if (!source.isPlayer)
        {
            Character firstOpponent = LivingOpponentsOf(source).FirstOrDefault();
            return firstOpponent != null
                ? new List<Character> { firstOpponent }
                : new List<Character>();
        }
```

et, dans le `switch` :

```csharp
            case TargetingMode.AllEnemies:
                return LivingOpponentsOf(source);
```

**Ce que ça corrige au passage :** la branche `!source.isPlayer` visait
`allies.FirstOrDefault(...)` — donc l'équipe du joueur, quelle que soit l'équipe de la
source. Un ennemi soignant un autre ennemi visait le joueur.

- [ ] **Step 3 : compiler.** Suite EditMode, **85 tests, 0 échec**.

- [ ] **Step 4 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): aim cards at the other team, not at the enemies list

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 6 : Le retour visuel des effets, par équipe

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs` (`PlayCardEffectFeedback`)

**Contexte :** méthode ajoutée le 2026-08-23 par Etienne PINGLIER. Elle résout `targetOthers`
en parcourant `enemies` :

```csharp
            else if (effect.targetOthers)
            {
                effectTargets = enemies
                    .Where(enemy => enemy != null && enemy.IsAlive && !targets.Contains(enemy))
                    .ToList();
            }
```

- [ ] **Step 1 : remplacer par l'équipe adverse de la source**

```csharp
            else if (effect.targetOthers)
            {
                effectTargets = LivingOpponentsOf(source)
                    .Where(other => !targets.Contains(other))
                    .ToList();
            }
```

- [ ] **Step 2 : compiler.** Suite EditMode, **85 tests, 0 échec**.

- [ ] **Step 3 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): show targetOthers feedback on the caster's opponents

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 7 : La zone de dépôt reçoit une équipe

**Files:**
- Modify: `Assets/Scripts/Scene/STS/UI/DropZone.cs`
- Modify: `Assets/Scripts/Scene/STS/UI/UIManager.cs`

- [ ] **Step 1 : renommer le paramètre pour ce qu'il désigne**

Dans `DropZone.Init`, remplacer `bool acceptsEnemy` par `bool hostile`, et le champ
`acceptsEnemyCards` par `isHostileZone`. Le comportement ne change pas : c'est une
**correction de vocabulaire**, préalable au reste.

```csharp
    public void Init(CombatManager cm, Character t, bool hostile)
    {
        // ...
        isHostileZone = hostile;
        // ...
        highlight.color = hostile ? new Color(1, 0, 0, 0f) : new Color(0, 1, 0, 0f);
```

Répercuter sur les trois autres lectures du champ (lignes ~404, ~405, ~551 avant
renommage) : les trouver par `grep -n "acceptsEnemyCards" Assets/Scripts/Scene/STS/UI/DropZone.cs`
et **toutes** les traiter.

- [ ] **Step 2 : `UIManager` calcule l'hostilité au lieu de la coder en dur**

Remplacer `dz.Init(combat, ally, false);` et `dz2.Init(combat, enemy, true);` par un appel
qui demande au `CombatManager` :

```csharp
            dz.Init(combat, ally, combat.IsHostileTo(combat.GetActingPlayer(), ally));
```
```csharp
            dz2.Init(combat, enemy, combat.IsHostileTo(combat.GetActingPlayer(), enemy));
```

- [ ] **Step 3 : écrire `IsHostileTo` dans `CombatManager`**

```csharp
    /// Deux combattants sont hostiles quand ils ne partagent pas d'équipe.
    ///
    /// Sur le chemin local, où le registre est vide, la question se ramène à `isPlayer`,
    /// qui était la seule réponse possible avant que les équipes existent.
    public bool IsHostileTo(Character viewer, Character other)
    {
        if (viewer == null || other == null)
            return false;

        string viewerTeam = TeamOf(viewer);
        string otherTeam = TeamOf(other);
        if (string.IsNullOrEmpty(viewerTeam) || string.IsNullOrEmpty(otherTeam))
            return viewer.isPlayer != other.isPlayer;

        return !string.Equals(viewerTeam, otherTeam, StringComparison.Ordinal);
    }
```

- [ ] **Step 4 : compiler.** Suite EditMode, **85 tests, 0 échec**.

- [ ] **Step 5 : commit**

```bash
git add Assets/Scripts/Scene/STS/UI/DropZone.cs Assets/Scripts/Scene/STS/UI/UIManager.cs \
        Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "refactor(sts): give a drop zone the team it faces, not a boolean

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 8 : Vérification en jeu réel, et l'inventaire

Aucun test EditMode ne construit `CombatManager` ni `UIManager`. **Cette vérification est
manuelle, et c'est elle qui autorise la fusion.**

**Files:** aucun, sauf correctif si un problème apparaît.

- [ ] **Step 1 : une victoire PvE normale**

Gagner un combat. L'écran de victoire s'affiche, les récompenses arrivent. C'est le contrôle
négatif : le chemin le plus fréquent ne doit rien voir de ce plan.

- [ ] **Step 2 : une défaite**

Perdre un combat. Écran de défaite, run terminée.

- [ ] **Step 3 : le ciblage à plusieurs ennemis**

Une rencontre à trois ennemis : survol, `AllEnemies`, ciblage aléatoire, et **tuer le
premier** pour vérifier que les cibles suivent l'identité et non la position.

- [ ] **Step 4 : le tutoriel**

De bout en bout. `UsesAuthoritativeCombat` y est faux, donc ni registre ni issue annoncée :
**il doit être strictement inchangé.** C'est la garantie que les replis locaux tiennent.

- [ ] **Step 5 : deux combats d'affilée**

Enchaîner deux combats dans la même run et vérifier que le second se conclut sur sa propre
issue. C'est le test du remise à zéro de `announcedOutcome` (tâche 2, step 5).

- [ ] **Step 6 : mettre à jour l'inventaire de l'étude**

Dans `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`,
§3.4, marquer les entrées correspondant à l'issue du combat et au ciblage comme traitées, en
nommant la tâche. **Ne pas les supprimer** : l'inventaire est un état d'avancement.

Corriger aussi le **§4.7**, qui annonce que `if (combat.player != null)` fige un allié
unique : cette ligne n'existe plus, `UIManager` boucle déjà sur `combat.allies`.

- [ ] **Step 7 : commit**

```bash
git add docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md
git commit -m "docs(sts): mark the outcome and targeting workarounds as removed

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Ce que ce plan ne fait pas

- **Il ne déplace pas le rejeu.** Les `Replay*Event` restent dans `CombatManager`. Le §4.4
  prévoit `AuthoritativeEventReplayer` ; c'est mécanique et ça collisionne avec le travail
  parallèle sur ce fichier.
- **Il ne touche pas à `isPlayer`.** `AnyPlayer`, le survol, `GetActingPlayer` et le
  tutoriel en dépendent encore. Le remplacer par l'équipe est le sujet du plan PvP.
- **Il n'affiche rien d'un combattant distant** — pas de dos de carte, pas de compteurs
  `drawCount` / `handCount` (§4.7). Plan PvP.
- **Il ne traite pas le forfait ni le timeout** côté client. `CombatOutcomeSource` sait déjà
  les traduire — un combat clos sans vainqueur est un nul — mais rien n'envoie ces commandes
  aujourd'hui.
- **Il ne touche pas `StsPvpBattleSettlement`** côté serveur, qui construit encore son
  `CombatState` à la main et n'émet pas de `CombatEnded`. Tant que ce fichier reste tel
  quel, **le PvP n'annoncera aucune issue** et le client retombera sur la dérivation locale.
  C'est la dépendance principale du plan PvP.
