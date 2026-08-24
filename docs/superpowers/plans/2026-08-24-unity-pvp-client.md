# Le client PvP Unity — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** rendre le 1v1 PvP jouable de bout en bout côté Unity — entrer dans le combat
depuis le matchmaking, se brancher sur le bon transport, hydrater les combattants depuis
le snapshot PvP, montrer le compte à rebours et les refus du serveur, et sortir du combat
par un écran de résultat PvP au lieu de la boucle de récompenses PvE.

**Architecture :** les trois plans précédents ont posé toute la couture interne — registre
d'identité, piles adressées, issue lue, ciblage par équipe. Il ne reste, littéralement,
que les bords : **par où on entre** (menu multijoueur → scène de combat), **à quoi on se
branche** (le mode dans la charge utile de connexion), **qui est « moi »** (plus de
littéral `"player"`), et **par où on sort** (fin PvP, pas fin de run). Cinq briques
neuves en C# pur et testées, le reste étant du câblage de fichiers existants.

**Tech Stack :** Unity 6000.3.2f1, C# (netstandard2.1), NUnit (EditMode), Newtonsoft.Json.

**Spec :** `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md`,
§5 (bootstrap et sortie PvP) et §4.7 (interface). **Attention : les §3.4 et §4.7 ont été
mis à jour le 2026-08-24 ; les autres sections datent du 2026-08-23 et certaines sont
périmées.** Ne rien répéter du §6 sans vérifier : le serveur est fait et livré depuis.

**Plans précédents, tous fusionnés sur `experimental_refactor` :**
- `2026-08-23-combatant-identity-seam.md` — `CombatantRegistry<T>`, `CombatantDescriptor`,
  `CombatantSnapshotReader`
- `2026-08-23-combatant-addressed-piles.md` — `PileKinds`, `ICombatantPiles<T>`,
  `RemotePiles<T>`, `CombatantPilesRegistry<T>`, `LocalPiles`
- `2026-08-24-combat-outcome-and-team-targeting.md` — `CombatOutcomeSource`,
  `TeamOutcome.Draw`, ciblage par équipe, `IsHostileTo`

---

## Ce que le serveur offre déjà — à traiter comme acquis

Livré et vivant sur la branche `dev` du backend. **Ne rien replanifier de tout ceci.**

- `GET /api/sts/pvp/battles/{battleId}/snapshot` → `CombatSnapshotDto(version, combatId,
  revision, "PVP", view)`
- souscription socket `/user/queue/sts/pvp/battles/{battleId}` — publication des
  commandes sur `/app/sts/pvp/battles/{battleId}/commands`
- messages : `STATE_UPDATED`, `COMBAT_EVENT` (un par événement, avec un discriminant
  `eventType`), `COMMAND_REJECTED` de charge utile `{code, message}`
- codes de refus, ceux du moteur : `INSUFFICIENT_ENERGY`, `CARD_NOT_IN_HAND`,
  `INVALID_TARGET`, `NOT_ACTOR_TURN`, `OUT_OF_SYNC`, `COMBAT_NOT_FOUND`,
  `INVALID_COMMAND`, `INTERNAL_ERROR`
- la vue porte `turnDeadline` et `serverTime` ; un tour PvP dure **30 secondes**
- un forfait comme une fin normale émettent `CombatEnded` avec `winnerTeamId`
  (`null` = match nul)
- la main et la pioche d'un adversaire sont cachées : ses piles arrivent en compteurs, et
  les événements qui nommeraient une carte cachée sont filtrés côté serveur

Côté React, également fait et testé : le pont résout l'endpoint de snapshot, la
souscription et la destination des commandes **à partir d'un mode `"PVE" | "PVP"`**, et
`parseCombatSnapshot` accepte déjà `"PVP"`. **Rien à faire de ce côté non plus.** Ce qui
manque est du ressort de ce plan et d'une seule ligne : personne n'envoie ce mode.

---

## Ce que ce plan corrige, en une phrase

Le PvP n'a aujourd'hui **aucune entrée** — le matchmaking trouve un adversaire, affiche
« Match PVP trouvé ! » et s'arrête là — et il n'aurait **aucune sortie** : la fin d'un
combat retombe dans la complétion de nœud et l'écran de récompenses d'une run PvE.

## Ce qui a été vérifié dans le code avant d'écrire ce plan

Constats du 2026-08-24 sur `experimental_refactor` à `f3d43d5`. Les numéros de ligne
bougeront ; les faits, non. **Trois d'entre eux contredisent ce qu'on m'avait décrit, et
ils sont signalés comme tels.**

1. **`MultiplayerMenuController` ne charge rien du tout après le matchmaking —
   correction.** `QuickMatchAsync` (`Assets/Scripts/Scene/STS/UI/MultiplayerMenuController.cs:320-327`) :
   ```csharp
   string battleId = response.Value<string>("battleId");
   if (!string.IsNullOrWhiteSpace(battleId))
   {
       await CacheBattleParticipantsAsync(battleId);
       ShowNotification("Match PVP trouvé !");
       await CancelQuickMatchAsync(false);
       return;
   }
   ```
   Il met les participants en cache et **rend la main**. Le seul `LoadScene("STS_Boot")`
   du fichier est `ReturnToMainMenu()` (ligne 431), le bouton « retour » du menu, sans
   rapport avec le matchmaking. **Aucun combat n'est ouvert, ni local ni distant.**

2. **Le combat local avec le pseudo de l'adversaire existe bel et bien, mais ailleurs et
   plus tard.** `CombatManager.Init` appelle `RunManager.Instance?.ApplyPvpParticipantDisplayNames(allies, enemies)`
   (`CombatManager.cs:140`), et cette méthode ne se garde que sur `pvpBattleId`
   (`RunManager.cs:683`), champ écrit au matchmaking (`RunManager.cs:669`) et effacé
   nulle part sauf en fin de run (`RunManager.cs:327`). Donc **la prochaine rencontre PvE
   jouée après un matchmaking affiche le pseudo de l'adversaire PvP sur son premier
   ennemi.** C'est une fuite active, pas un chemin PvP.

3. **`STSApiClient.SendPvpBattleActionAsync` existe et n'est appelée nulle part —
   confirmé.** Définie `Assets/Scripts/Scene/STS/Api/STSApiClient.cs:682`, aucun appelant
   (`grep -rn --include='*.cs' SendPvpBattleActionAsync Assets` ne rend que la
   définition). **Et elle n'est pas seule :** `ListPvpNotificationsAsync` (ligne 658) et
   `AcknowledgePvpNotificationAsync` (ligne 664) sont mortes elles aussi. C'est la
   conséquence la plus lourde : `QuickMatchAsync` envoie `skipMatchmaking: false`, donc le
   **premier** joueur à s'inscrire reçoit `queued` sans `battleId` — et rien ne l'informera
   jamais qu'un adversaire est arrivé. **Sans polling des notifications, seul le second
   joueur peut entrer dans le combat**, ce qui suffit à rendre le mode injouable.

4. **Comment un combat PvE se monte aujourd'hui, dans l'ordre exact.**
   - `GameManager.Start()` (`Assets/Scripts/Scene/STS/Core/GameManager.cs:12-39`) charge
     les bases, puis `SetupGame()`, puis `ui.Init(combat)`, `turnSystem.Begin()`,
     `ui.RefreshUI()`, et enfin `combat.Init()`.
   - `SetupGame()` (ligne 41) a deux branches : sans run ou en tutoriel, un joueur de
     test et trois `Dummy` ; sinon (ligne 113) `combat.allies.Add(RunManager.Instance.player)`,
     `combat.enemies = EnemySelector.GetRandomEncounter(...)`, `combat.deck = new DeckManager()`
     rempli depuis `RunManager.Instance.deck`. **Il n'existe aucune troisième branche.**
     Ouvrir `STS_Combat` sans run donne donc un joueur de secours et une rencontre PvE
     tirée au hasard.
   - `CombatManager.UsesAuthoritativeCombat` (ligne 132) vaut
     `RunManager.Instance.activeCombat != null && Type == JTokenType.Object` — c'est
     l'entrée 3 de l'inventaire, le mode déduit d'un effet de bord.
   - `CombatManager.Init` (ligne 136) : `EnsureAllies()`, `EnsureEncounterEnemies()`,
     les noms PvP, `ui.Init`, `ui.InitCharacters`, puis **si `UsesAuthoritativeCombat`**
     applique `RunManager.Instance.activeCombat` tout de suite et connecte la socket
     (lignes 172-190) ; **sinon si `CanBootstrapAuthoritativeCombat()`** (ligne 209 :
     `activeEncounter != null && runId non vide`) lance `BootstrapAuthoritativeCombatRoutine`
     qui va chercher l'état par REST ; **sinon** `StartLocalCombatFlow()`.
   - `ApplyAuthoritativeCombatState` (ligne 824) écrit `RunManager.Instance.activeCombat`
     (ligne 828), construit le registre d'identité **une fois** (ligne 836), et par
     combattant appelle `RegisterCombatantPiles` (ligne 879) puis
     `ApplyAuthoritativePlayerPiles` (ligne 885).
   - `BuildCombatantRegistry` (ligne 2135) passe **`"player"` en dur** comme identifiant
     du combattant local : `CombatantSnapshotReader.ReadCombatants(combatToken, "player")`
     (ligne 2141), et `ResolveCombatantByConvention` (ligne 2158) ne connaît que
     `player`, `player-N` et `enemy-N`.

5. **Quatre littéraux `"player"` restants bloquent le PvP, et le pire est celui des
   piles.** `grep -rn --include='*.cs' '"player"' Assets/Scripts` :
   - `CombatManager.cs:883` — `if (target.isPlayer && string.Equals(combatantId, "player", ...))`
     garde `ApplyAuthoritativePlayerPiles`. En PvP l'identifiant local est un UUID :
     **la condition ne se vérifie jamais, les piles locales ne sont jamais semées, et le
     joueur reste avec une main vide pour toujours.** C'est le blocage n°1.
   - `CombatManager.cs:1654` — `ReplayEnergySpentEvent` ignore l'événement si le
     `combatantId` n'est pas `"player"`. En PvP, l'énergie ne baisse qu'au prochain
     `STATE_UPDATED`.
   - `CombatManager.cs:1112` — `TurnStarted` n'incrémente `state.turnCount` que pour
     `"player"`.
   - `CombatManager.cs:2141` — l'identifiant local en dur du registre, ci-dessus.

6. **`isPlayer`, lui, ne bloque rien en 1v1 — et c'est un fait, pas une opinion.**
   - `GetDisplayTargets`, cas `AnyPlayer` (ligne 2716) teste `hovered.isPlayer` : en 1v1
     nos alliés se réduisent à nous, donc la restriction est exacte. Elle deviendrait
     fausse en co-op, pas avant.
   - `AutoCardTargets` (ligne 2739) teste `!source.isPlayer` : la source d'une carte que
     nous jouons est toujours le joueur local.
   - `PresentCardPlayed` (lignes 1254, 1278, 1285) traite l'acteur non-joueur en créant
     une vue de carte depuis sa position et en l'animant : **un adversaire humain sera
     donc animé exactement comme un ennemi PvE, sans rien changer.**
   - `CombatManager.cs:898` — `endTurnButton.interactable = activeCombatant.isPlayer` :
     fonctionne par accident en 1v1 (l'adversaire est un `Enemy`, `isPlayer == false`),
     mais dit « n'importe quel combattant du côté joueur » là où il faut « moi ». C'est
     l'arbitrage que le §3.4 entrée 2 laissait explicitement à ce plan.
   - `DropZone.Init` (ligne 79) fait `((Enemy)target)` sur tout ce qui n'est pas
     `isPlayer` : **l'adversaire PvP doit donc être un `Enemy`**, sous peine
     d'`InvalidCastException`. Avec `data == null` la branche retombe sur
     `Resources.Load<Sprite>("STS/Characters/" + target.name)` (ligne 91), et
     `Assets/Resources/STS/Characters/` contient bien `EP.png`, `MECA.png`, `GM.png`,
     `ITI.png`, `CFI.png`, `MRIE.png`, `GC.png`, `AI.png`, `PERF.png` — un fichier par
     valeur jouable de `SelectableCharacter`.
   - `Enemy.PeekNextAction()` (`Assets/Scripts/Scene/STS/Entities/Enemy.cs:112`) rend
     `null` quand `data == null` et qu'aucune intention autoritative n'a été posée, et
     `CharacterUI.RefreshIntent` (ligne 194) vide alors l'affichage : **l'absence
     d'intention pour un combattant humain est déjà gratuite.**

7. **La fin d'un combat, aujourd'hui.** `TryEndCombatIfNeeded` (ligne 2628) →
   `ResolveCombatEndRoutine` (ligne 2651) → `EndCombat()` (ligne 2820).
   - Branche victoire : hooks de reliques, `currentNode.completed = true`,
     `RewardGenerator.GenerateReward(result)` construit depuis
     `RunManager.Instance.currentFloor / eliteEncounter / bossEncounter / act`,
     `SubmitCombatResultAsync("victory")`, puis `LoadScene("STS_Reward")` (ligne 2891) ou
     `"STS_Retreat"`.
   - Branche défaite/nul : `SubmitCombatResultAsync("defeat")` puis
     `ui.ShowGameOver(enemies)` (ligne 2910), dont le bouton appelle
     `RunManager.GrantRunEndUnlocks(false)` et `OnRunEnd()`
     (`Assets/Scripts/Scene/STS/UI/GameOverController.cs:45-53`).
   - `SubmitCombatResultAsync` (ligne 2914) sort bien sans rien faire quand `runId` ou
     `activeEncounter` manquent — **mais c'est la seule protection, et elle ne couvre pas
     le cas réel** : un joueur qui met une run PvE en pause et lance un match garde son
     `runId` et son `activeEncounter`, et gagnerait alors un nœud de sa run en gagnant un
     duel. La protection doit devenir explicite.
   - `ApplyAuthoritativePlayerPiles` réécrit **`RunManager.Instance.deck`** (ligne 2196) :
     en PvP, le deck de la run serait remplacé par le deck de duel.

8. **Rien ne sait afficher un compte à rebours, et il n'y a pas d'endroit évident.**
   `grep -rniE "countdown|remainingSeconds|timeLeft|turnDeadline|serverTime|Timer" Assets/Scripts`
   ne rend que `trailTimer` (`CardAnimator`) et `waitTimer` (`STSTutorialManager`).
   Personne ne lit `turnDeadline` ni `serverTime`. **Et l'endroit qui semblerait naturel
   n'existe pas :** `CombatManager.Update` (ligne 23) est entièrement encadré par
   `#if UNITY_EDITOR` — dans un build WebGL, `CombatManager` n'a pas de `Update`. Le seul
   `Update` qui tourne en permanence dans la scène de combat est `TurnSystem.Update`
   (`Assets/Scripts/Scene/STS/Combat/TurnSystem.cs:22`), qui sort immédiatement en mode
   autoritatif — c'est donc là, **avant** cette sortie, que le tic doit vivre.

9. **La connexion, et la ligne qui manque.** `ReactCombatBridge.ConnectAsync`
   (`Assets/Scripts/Scene/STS/Api/ReactCombatBridge.cs:49-55`) :
   ```csharp
   public static Task<bool> ConnectAsync(string combatId)
   {
       ReactCombatBridge bridge = EnsureInstance();
       bridge.core.Connect(combatId);
       string json = JsonConvert.SerializeObject(new { combatId });
       return Task.FromResult(InvokeConnect(json) != 0);
   }
   ```
   Un seul appelant : `CombatManager.ConnectAuthoritativeCombatSocketRoutine` (ligne 257).
   `Insastral_CombatConnect` (`Assets/Plugins/WebGL/InsastralBridge.jslib:23`) se contente
   de passer la chaîne à `window.insastralCombatBridge.connect(json)` : **le `.jslib` n'a
   rien à changer.** `ReactCombatBridgeCore.Connect` (`.../CombatBridge/ReactCombatBridgeCore.cs:67`)
   ne connaît que l'identifiant.
10. **Le transport est déjà l'identifiant que le pont attend partout.**
    `ReactCombatBridgeCore.HandleCombatEvent` refuse tout message dont le `combatId` ne
    vaut pas `CombatId` (ligne 142), et `CreateCommand` écrit `combatId = CombatId` dans
    chaque commande (ligne 101). En PvP, cet identifiant est donc le **battleId**.
    `AuthoritativeCombatIdentity.GetTransportId(runId, activeCombat)`
    (`Assets/Scripts/Scene/STS/Api/AuthoritativeCombatIdentity.cs:5`) **lève** si le
    `runId` est vide : inutilisable en PvP, et épinglée par
    `ReactCombatBridgeTests.AuthoritativeConnectionUsesRunIdAsTransportId` (ligne 19).
    Il faut une seconde méthode, pas une modification.
11. **`COMMAND_REJECTED` n'est traité nulle part côté application.**
    `ProcessAuthoritativeMessageQueue` (`CombatManager.cs:640-664`) ne connaît que
    `COMBAT_SNAPSHOT`, `COMBAT_EVENT` et `STATE_UPDATED` ; un `COMMAND_REJECTED` traverse
    la file et disparaît. Le noyau du pont, lui, le comprend (ligne 152) et règle la
    commande en attente — donc rien ne se bloque, mais **le joueur ne voit rien**.
12. **`SURRENDER` est explicitement refusé côté client.**
    `ReactCombatBridgeCore.CommandTypes` (ligne 38) ne contient que `PLAY_CARD` et
    `END_TURN`, et `ReactCombatBridgeTests.UnsupportedBackendCommandsAreRejectedLocally`
    (ligne 77) épingle `SELECT_CHOICE` et `SURRENDER` comme refusés. Envoyer un forfait
    demanderait donc de changer ce test **et** de connaître le nom exact de la commande
    côté moteur — que ce dépôt ne permet pas de vérifier. Voir la décision D4.
13. **`RunManager` est le seul objet qui traverse les scènes.** `Awake` fait
    `DontDestroyOnLoad` (`RunManager.cs:65`), et son GUID
    (`4a1d80b3bf15be945ae003beb5b7e2f1`) n'apparaît que dans `Assets/Scenes/STS_Boot.unity`
    — ni dans `STS_MultiplayerMenu.unity`, ni dans `STS_Combat.unity`. C'est donc lui, et
    lui seul, qui peut porter « le combat en cours est un duel ».
14. **`CombatantDescriptor` refuse un combattant sans équipe.** Son constructeur lève sur
    un `teamId` vide (`Assets/Scripts/Scene/STS/Combat/Authoritative/CombatantDescriptor.cs:441`
    → `throw new ArgumentException("A combatant needs a team")`), et
    `CombatantSnapshotReader` saute un combattant sans `teamId`. Si la vue PvP n'émettait
    pas de `teamId`, **le registre resterait vide et rien ne fonctionnerait** : c'est la
    première chose que la tâche 15 vérifie sur une vraie trame.

## Global Constraints

- **Ne jamais modifier, committer ou « nettoyer » les fichiers de cartes**
  (`Assets/StreamingAssets/STSCardData/**`) ni quoi que ce soit sous `card/` ou `print/` :
  un humain y travaille en parallèle. Un test qui échoue là n'est pas le vôtre.
- **Aucune commande git qui modifie l'arbre de travail** : ni `checkout --`, ni `restore`,
  ni `stash`, ni `clean`, ni `reset`. `git add` sur des chemins précis uniquement,
  **jamais** `-A` ni `.`.
- **L'assembly `STS.AuthoritativeCombat` a `noEngineReferences: true`.** Aucun
  `using UnityEngine`, aucun `MonoBehaviour`, aucun `Mathf`/`Debug`/`Time`, et aucune
  référence à `Character`, `CardInstance` ou `DeckManager`, qui référencent le moteur.
  Même règle pour `STS.ReactCombatBridge`. Une classe qui touche à l'un de ces types va
  dans `Assembly-CSharp`, donc hors de ces dossiers.
- **`STS.ReactCombatBridge` a `references: []`** : il ne voit pas `STS.AuthoritativeCombat`.
  Ne pas y utiliser `CombatMode` ni `CombatModes` — le mode y circule comme une `string`.
- **Base de la suite EditMode : 85 tests, 0 échec.** Chaque tâche annonce le nouveau
  total. Un total qui ne monte pas du compte annoncé signifie que les tests n'ont pas été
  découverts, pas qu'ils passent.
- **Aucun test EditMode ne construit `CombatManager` ni `UIManager`** : ce sont des
  `MonoBehaviour`. Pour tout ce qui les touche, la vérification est donc, et seulement :
  **ça compile, les 85 (puis 109) passent toujours, et un humain y joue.** Ne jamais
  écrire « vérifié » pour du code de `CombatManager` sur la foi d'une suite verte.
- **Le PvP ne tourne pas dans l'éditeur.** Tout le transport est sous
  `#if UNITY_WEBGL && !UNITY_EDITOR` (`ReactCombatBridge.InvokeConnect` ligne 131,
  `InvokeCommand` ligne 149, et les branchements de `CombatManager.Init`). La
  vérification en jeu réel est un **build WebGL et deux navigateurs**, pas le mode Play.
- **Le chemin local (tutoriel, `Mode == CombatMode.Local`) doit rester strictement
  inchangé**, et le chemin PvE ne doit changer que là où ce plan le dit explicitement.
- **Le vocabulaire d'équipe vient du serveur** : `teamId` est une chaîne opaque, comparée
  en `StringComparison.Ordinal`. **Ne jamais supposer `"player"` / `"enemy"` ni
  `"team-0"` / `"team-1"`.**
- **Ne pas inventer de nom de champ de protocole.** Trois formes ne sont pas vérifiables
  depuis ce dépôt — la charge utile d'une notification de matchmaking, le nom exact de la
  commande de forfait, et la présence de `controllerType` dans la vue PvP. Chacune est
  marquée comme telle et accompagnée d'une étape qui journalise la trame brute plutôt que
  d'une supposition.
- **Ne pas ouvrir `/home/brehan/IdeaProjects/webAPI` ni
  `/home/brehan/Documents/Insastronaute/insastral`** : d'autres agents y travaillent.

## Comment lancer les tests

Unity en batch **écrit ses résultats puis se bloque au lieu de sortir**. Il faut attendre
le fichier de résultats, pas le processus :

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

Pour vérifier qu'une nouvelle classe de tests a bien été **découverte** et pas seulement
compilée :

```bash
python3 -c "
import xml.etree.ElementTree as ET
r=ET.parse('/tmp/editmode.xml').getroot()
n=[tc.get('name') for tc in r.iter('test-case') if 'CombatModeTests' in (tc.get('fullname') or '')]
print(len(n), 'decouverts'); [print(' -',x) for x in n]"
```

Aucun éditeur Unity ne doit avoir le projet ouvert pendant l'exécution (absence de
`Temp/UnityLockfile`).

## Structure des fichiers

| Fichier | Responsabilité |
|---|---|
| `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatMode.cs` | **Créé.** Le mode, dit plutôt que déduit, et son nom sur le fil |
| `Assets/Scripts/Scene/STS/Combat/Authoritative/LocalCombatantResolver.cs` | **Créé.** Quel combattant du snapshot est le nôtre |
| `Assets/Scripts/Scene/STS/Combat/Authoritative/TurnCountdown.cs` | **Créé.** La deadline serveur → des secondes restantes |
| `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatRejectionMessages.cs` | **Créé.** Un code de refus → une phrase montrable |
| `Assets/Tests/EditMode/CombatModeTests.cs` | **Créé.** 4 tests |
| `Assets/Tests/EditMode/LocalCombatantResolverTests.cs` | **Créé.** 6 tests |
| `Assets/Tests/EditMode/TurnCountdownTests.cs` | **Créé.** 6 tests |
| `Assets/Tests/EditMode/CombatRejectionMessagesTests.cs` | **Créé.** 4 tests |
| `Assets/Tests/EditMode/ReactCombatBridgeTests.cs` | **Modifié.** +4 tests sur la charge utile de connexion |
| `Assets/Scripts/Scene/STS/Api/CombatBridge/ReactCombatBridgeCore.cs` | **Modifié.** Construit la charge utile de connexion, mode compris |
| `Assets/Scripts/Scene/STS/Api/ReactCombatBridge.cs` | **Modifié.** `ConnectAsync(combatId, mode)` |
| `Assets/Scripts/Scene/STS/Api/AuthoritativeCombatIdentity.cs` | **Modifié.** `GetPvpTransportId(battleId)` |
| `Assets/Scripts/Scene/STS/Core/RunManager.cs` | **Modifié.** Porte la session PvP en cours ; expose les participants |
| `Assets/Scripts/Scene/STS/UI/MultiplayerMenuController.cs` | **Modifié.** Entre dans le combat ; guette la notification de match |
| `Assets/Scripts/Scene/STS/Core/GameManager.cs` | **Modifié.** Une troisième branche de montage : le duel |
| `Assets/Scripts/Scene/STS/Entities/Enemy.cs` | **Modifié.** Un constructeur pour un adversaire humain |
| `Assets/Scripts/Scene/STS/Combat/CombatManager.cs` | **Modifié.** Mode, bootstrap PvP, littéraux, refus, fin PvP |
| `Assets/Scripts/Scene/STS/Combat/TurnSystem.cs` | **Modifié.** Fait battre le compte à rebours |
| `Assets/Scripts/Scene/STS/UI/UIManager.cs` | **Modifié.** Compte à rebours, avis de refus, résultat PvP |
| `Assets/Scripts/Scene/STS/UI/CharacterUI.cs` | **Modifié.** Les compteurs de piles d'un adversaire |

---

## Task 0 : Prérequis — base verte

**Files:** aucun.

- [x] **Step 1 :** vérifier la branche et l'arbre.

```bash
cd /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE
git branch --show-current        # attendu : experimental_refactor
git status --porcelain           # attendu : au plus " M Inte-INSASTRONAUTE.slnx"
git rev-parse --short HEAD       # noter ce commit
```

- [x] **Step 2 :** lancer la suite EditMode avec la recette ci-dessus.

- [x] **Step 3 :** vérifier `total="85" passed="85" failed="0"`. Sinon, **s'arrêter et le
      signaler** — ne rien implémenter sur un arbre rouge.

---

## Task 1 : Le mode du combat, dit plutôt que déduit

**But :** donner un nom à ce que le client est en train de jouer. C'est l'entrée 3 de
l'inventaire du §3.4 — « le client est en mode autoritatif *parce qu'un état est arrivé* »
— et c'est aussi la valeur que le pont React attend pour choisir ses trois destinations.

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatMode.cs`
- Test: `Assets/Tests/EditMode/CombatModeTests.cs`

**Interfaces:**
- Produces: `enum CombatMode { Local, Pve, Pvp }`, `static class CombatModes` avec
  `ToWireName(CombatMode)` et `Parse(string)`. Les tâches 5, 9, 10, 12 et 14 les consomment.

- [x] **Step 1 : écrire le test qui échoue**

```csharp
using NUnit.Framework;

public class CombatModeTests
{
    /// Ces deux chaînes ne sont pas décoratives : le pont React choisit l'endpoint de
    /// snapshot, la file privée et la destination des commandes à partir d'elles.
    [Test]
    public void TheTwoServerModesHaveTheWireNamesTheBridgeReads()
    {
        Assert.That(CombatModes.ToWireName(CombatMode.Pve), Is.EqualTo("PVE"));
        Assert.That(CombatModes.ToWireName(CombatMode.Pvp), Is.EqualTo("PVP"));
    }

    [Test]
    public void AWireNameRoundTrips()
    {
        Assert.That(CombatModes.Parse(CombatModes.ToWireName(CombatMode.Pvp)),
            Is.EqualTo(CombatMode.Pvp));
        Assert.That(CombatModes.Parse("pve"), Is.EqualTo(CombatMode.Pve));
    }

    /// Un combat local n'a pas de serveur, donc pas de nom sur le fil. Lui en inventer un
    /// ferait connecter le tutoriel à une socket.
    [Test]
    public void ALocalCombatHasNoWireName()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => CombatModes.ToWireName(CombatMode.Local));
    }

    [Test]
    public void AnUnknownOrMissingWireNameIsNoMode()
    {
        Assert.That(CombatModes.Parse(null), Is.Null);
        Assert.That(CombatModes.Parse(""), Is.Null);
        Assert.That(CombatModes.Parse("COOP"), Is.Null);
    }
}
```

- [x] **Step 2 : le lancer et le voir échouer.** Attendu : échec de compilation,
      `CombatMode` introuvable.

- [x] **Step 3 : écrire `CombatMode.cs`**

```csharp
using System;

/// <summary>
/// Ce que le client est en train de jouer, dit explicitement.
///
/// <para>Jusqu'ici le mode se déduisait d'un effet de bord : le client était autoritatif
/// « parce qu'un état était arrivé » (<c>UsesAuthoritativeCombat</c> lisait
/// <c>RunManager.activeCombat</c>, champ que l'application d'état venait d'écrire). Ça
/// tenait tant qu'il n'existait qu'une seule sorte de combat distant. Un duel, lui, doit
/// être autoritatif avant d'avoir reçu quoi que ce soit — sinon sa première frappe
/// partirait dans le moteur local.</para>
/// </summary>
public enum CombatMode
{
    /// Aucun serveur ne tranche : le tutoriel, et lui seul aujourd'hui.
    Local,

    /// Un combat de run, arbitré par le serveur, adressé par le runId.
    Pve,

    /// Un duel, arbitré par le serveur, adressé par le battleId.
    Pvp
}

public static class CombatModes
{
    public const string PveWireName = "PVE";
    public const string PvpWireName = "PVP";

    /// <summary>
    /// Le nom que le pont React lit pour choisir ses destinations. Il n'y en a que deux,
    /// et un combat local n'en a aucun.
    /// </summary>
    public static string ToWireName(CombatMode mode)
    {
        switch (mode)
        {
            case CombatMode.Pve: return PveWireName;
            case CombatMode.Pvp: return PvpWireName;
            default: throw new ArgumentOutOfRangeException(
                nameof(mode), "A local combat has no server mode");
        }
    }

    public static CombatMode? Parse(string wireName)
    {
        if (string.IsNullOrWhiteSpace(wireName))
            return null;

        switch (wireName.Trim().ToUpperInvariant())
        {
            case PveWireName: return CombatMode.Pve;
            case PvpWireName: return CombatMode.Pvp;
            default: return null;
        }
    }
}
```

- [x] **Step 4 : relancer.** Attendu : **89 tests, 0 échec** (85 + 4), et les 4 de
      `CombatModeTests` nommément découverts.

- [x] **Step 5 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/CombatMode.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/CombatMode.cs.meta \
        Assets/Tests/EditMode/CombatModeTests.cs \
        Assets/Tests/EditMode/CombatModeTests.cs.meta
git commit -m "feat(sts): name the combat mode instead of inferring it

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

> Les `.meta` sont générés par Unity au premier import. S'ils n'existent pas encore,
> lancer la suite EditMode, qui les crée. **Ne pas les écrire à la main** — un GUID
> inventé casse les références.

---

## Task 2 : Le combattant local, résolu au lieu d'être supposé

**But :** remplacer le `"player"` en dur de `BuildCombatantRegistry` (ligne 2141) par une
question posée au snapshot. Sans ça, en PvP, le registre ne marque personne comme local :
`LocalCombatantId` reste `null`, `Allies()` et `Opponents()` rendent des listes vides
(`CombatantRegistry.ByTeam` sort immédiatement si `localTeamId == null`), et **tout le
ciblage par équipe du plan 3 s'effondre en silence.**

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/LocalCombatantResolver.cs`
- Test: `Assets/Tests/EditMode/LocalCombatantResolverTests.cs`

**Interfaces:**
- Consumes: rien (Newtonsoft est déjà une référence précompilée de l'assembly).
- Produces: `static string LocalCombatantResolver.Resolve(JToken combatToken, string preferredCombatantId)`.
  La tâche 10 la consomme.

- [x] **Step 1 : écrire le test qui échoue**

```csharp
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
```

- [x] **Step 2 : le lancer et le voir échouer.**

- [x] **Step 3 : écrire `LocalCombatantResolver.cs`**

```csharp
using System;
using Newtonsoft.Json.Linq;

/// <summary>
/// Lequel des combattants d'un état est celui que ce client pilote.
///
/// <para>Le client répondait <c>"player"</c>, en dur. C'est exact en PvE, où le serveur
/// nomme le joueur ainsi par convention, et faux en PvP, où les identifiants sont des
/// UUID d'utilisateur.</para>
///
/// <para>Deux règles, dans cet ordre. La première est l'identifiant qu'on nous donne —
/// la convention PvE, ou l'identifiant d'utilisateur connu du menu multijoueur — et elle
/// ne s'applique que si le snapshot le contient réellement : un identifiant attendu et
/// absent est une divergence, pas une réponse. La seconde est une propriété du protocole
/// plutôt qu'un champ : la projection PvP ne montre les cartes qu'au spectateur, et
/// réduit tout autre combattant à des compteurs. **Celui qui montre ses cartes pendant
/// qu'un autre les cache est donc celui qui regarde.** Cette règle refuse de conclure dès
/// qu'il y a plusieurs mains visibles, parce qu'en co-op la question n'aurait plus de
/// réponse unique.</para>
/// </summary>
public static class LocalCombatantResolver
{
    public static string Resolve(JToken combatToken, string preferredCombatantId)
    {
        if (!(combatToken is JObject combat) || !(combat["combatants"] is JArray combatants))
            return null;

        if (!string.IsNullOrWhiteSpace(preferredCombatantId))
        {
            foreach (JToken combatantToken in combatants)
            {
                if (string.Equals(
                        combatantToken?.Value<string>("combatantId"),
                        preferredCombatantId,
                        StringComparison.Ordinal))
                {
                    return preferredCombatantId;
                }
            }
        }

        string onlyVisible = null;
        int visibleCount = 0;
        bool anyHidden = false;

        foreach (JToken combatantToken in combatants)
        {
            if (!(combatantToken is JObject combatant))
                continue;

            string combatantId = combatant.Value<string>("combatantId");
            if (string.IsNullOrWhiteSpace(combatantId))
                continue;

            if (combatant["hiddenPiles"] is JObject)
                anyHidden = true;

            if (combatant["piles"] is JObject)
            {
                visibleCount++;
                onlyVisible = combatantId;
            }
        }

        return anyHidden && visibleCount == 1 ? onlyVisible : null;
    }
}
```

- [x] **Step 4 : relancer.** Attendu : **95 tests, 0 échec** (89 + 6).

- [x] **Step 5 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/LocalCombatantResolver.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/LocalCombatantResolver.cs.meta \
        Assets/Tests/EditMode/LocalCombatantResolverTests.cs \
        Assets/Tests/EditMode/LocalCombatantResolverTests.cs.meta
git commit -m "feat(sts): ask the snapshot which combatant is ours

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 3 : Le compte à rebours, en C# pur

**But :** un tour PvP dure 30 secondes et le joueur ne le sait pas. La vue porte
`turnDeadline` et `serverTime` ; `serverTime` existe précisément pour que l'horloge du
client ne compte pas à sa place.

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/TurnCountdown.cs`
- Test: `Assets/Tests/EditMode/TurnCountdownTests.cs`

**Interfaces:**
- Produces: `readonly struct TurnCountdown` — `static TurnCountdown None`,
  `static TurnCountdown FromState(string turnDeadline, string serverTime, DateTimeOffset receivedAt)`,
  `bool HasDeadline`, `double SecondsRemainingAt(DateTimeOffset now)`. La tâche 12 la
  consomme.

- [x] **Step 1 : écrire le test qui échoue**

```csharp
using System;
using NUnit.Framework;

public class TurnCountdownTests
{
    private static readonly DateTimeOffset ServerNow =
        DateTimeOffset.Parse("2026-08-24T10:00:00Z");
    private static readonly DateTimeOffset ClientNow =
        DateTimeOffset.Parse("2026-08-24T10:00:00Z");

    private static TurnCountdown ThirtySecondTurn(DateTimeOffset receivedAt)
    {
        return TurnCountdown.FromState(
            ServerNow.AddSeconds(30).ToString("o"),
            ServerNow.ToString("o"),
            receivedAt);
    }

    [Test]
    public void AStateWithoutADeadlineIsNoCountdown()
    {
        TurnCountdown countdown = TurnCountdown.FromState(null, ServerNow.ToString("o"), ClientNow);

        Assert.That(countdown.HasDeadline, Is.False);
        Assert.That(countdown.SecondsRemainingAt(ClientNow), Is.Zero);
    }

    [Test]
    public void AFreshDeadlineReadsItsFullLength()
    {
        TurnCountdown countdown = ThirtySecondTurn(ClientNow);

        Assert.That(countdown.HasDeadline, Is.True);
        Assert.That(countdown.SecondsRemainingAt(ClientNow), Is.EqualTo(30d).Within(0.01));
    }

    [Test]
    public void TimePassingShortensIt()
    {
        TurnCountdown countdown = ThirtySecondTurn(ClientNow);

        Assert.That(countdown.SecondsRemainingAt(ClientNow.AddSeconds(12)),
            Is.EqualTo(18d).Within(0.01));
    }

    /// La raison d'être de serverTime : une horloge client fausse de dix minutes ne doit
    /// pas faire lire zéro seconde à un tour qui vient de commencer.
    [Test]
    public void AWrongClientClockDoesNotChangeTheRemainingTime()
    {
        DateTimeOffset skewedClientNow = ClientNow.AddMinutes(-10);
        TurnCountdown countdown = ThirtySecondTurn(skewedClientNow);

        Assert.That(countdown.SecondsRemainingAt(skewedClientNow),
            Is.EqualTo(30d).Within(0.01));
        Assert.That(countdown.SecondsRemainingAt(skewedClientNow.AddSeconds(25)),
            Is.EqualTo(5d).Within(0.01));
    }

    [Test]
    public void APastDeadlineReadsZeroRatherThanNegative()
    {
        TurnCountdown countdown = ThirtySecondTurn(ClientNow);

        Assert.That(countdown.SecondsRemainingAt(ClientNow.AddSeconds(45)), Is.Zero);
    }

    [Test]
    public void AnUnreadableTimestampIsNoCountdown()
    {
        Assert.That(TurnCountdown.FromState("bientôt", ServerNow.ToString("o"), ClientNow)
            .HasDeadline, Is.False);
        Assert.That(TurnCountdown.FromState(ServerNow.ToString("o"), "maintenant", ClientNow)
            .HasDeadline, Is.False);
    }
}
```

- [x] **Step 2 : le lancer et le voir échouer.**

- [x] **Step 3 : écrire `TurnCountdown.cs`**

```csharp
using System;
using System.Globalization;

/// <summary>
/// Combien de temps il reste au tour en cours, mesuré sur l'horloge du serveur.
///
/// <para>Un tour PvP dure trente secondes et se perd sans prévenir. Le compte à rebours
/// ne peut pas se calculer sur l'heure locale : elle peut être fausse de plusieurs
/// minutes, et le joueur verrait alors un tour déjà expiré ou éternel. La vue porte donc
/// <c>serverTime</c> à côté de <c>turnDeadline</c> ; l'écart entre cette heure-là et
/// celle qu'il était ici quand le message est arrivé est le décalage qu'on applique
/// ensuite à chaque lecture.</para>
///
/// <para>Une structure sans deadline est la réponse normale, pas une erreur : le PvE
/// n'envoie aucun de ces deux champs, et rien ne doit s'afficher.</para>
/// </summary>
public readonly struct TurnCountdown
{
    private readonly DateTimeOffset deadline;
    private readonly TimeSpan clockOffset;

    private TurnCountdown(DateTimeOffset deadline, TimeSpan clockOffset)
    {
        this.deadline = deadline;
        this.clockOffset = clockOffset;
        HasDeadline = true;
    }

    public static TurnCountdown None => default;

    public bool HasDeadline { get; }

    public static TurnCountdown FromState(
        string turnDeadline,
        string serverTime,
        DateTimeOffset receivedAt)
    {
        if (!TryParse(turnDeadline, out DateTimeOffset parsedDeadline))
            return None;
        if (!TryParse(serverTime, out DateTimeOffset parsedServerTime))
            return None;

        return new TurnCountdown(parsedDeadline, parsedServerTime - receivedAt);
    }

    /// <summary>
    /// Les secondes restantes, jamais négatives : un tour expiré vaut zéro, et c'est le
    /// serveur qui dira ce qu'il advient de lui.
    /// </summary>
    public double SecondsRemainingAt(DateTimeOffset now)
    {
        if (!HasDeadline)
            return 0d;

        double remaining = (deadline - (now + clockOffset)).TotalSeconds;
        return remaining < 0d ? 0d : remaining;
    }

    private static bool TryParse(string value, out DateTimeOffset parsed)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out parsed);
    }
}
```

- [x] **Step 4 : relancer.** Attendu : **101 tests, 0 échec** (95 + 6).

- [x] **Step 5 : preuve par mutation.** Remplacer `parsedServerTime - receivedAt` par
      `TimeSpan.Zero` et relancer : `AWrongClientClockDoesNotChangeTheRemainingTime` doit
      **échouer**. Annuler la mutation, relancer, revert vérifié. Si le test reste vert,
      il ne prouve rien.

- [x] **Step 6 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/TurnCountdown.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/TurnCountdown.cs.meta \
        Assets/Tests/EditMode/TurnCountdownTests.cs \
        Assets/Tests/EditMode/TurnCountdownTests.cs.meta
git commit -m "feat(sts): count a turn down on the server's clock, not ours

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 4 : Le refus du serveur, en une phrase

**But :** le serveur refuse une commande avec un code de son moteur ; le client n'en fait
rien (constat 11). Une carte refusée ne bouge simplement pas, et le joueur recommence.

**Files:**
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/CombatRejectionMessages.cs`
- Test: `Assets/Tests/EditMode/CombatRejectionMessagesTests.cs`

**Interfaces:**
- Produces: `static class CombatRejectionMessages` — `ForCode(string)`,
  `WarrantsEnergyGlow(string)`, `IReadOnlyList<string> KnownCodes`. La tâche 11 la consomme.

- [x] **Step 1 : écrire le test qui échoue**

```csharp
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
```

> Le dernier test dit quelque chose de précis : les codes sont comparés **à l'octet**,
> comme les identifiants d'équipe. Une casse différente est un code inconnu, pas le même
> écrit autrement. C'est la même règle que `PileKinds` a close pour les piles — à ceci
> près qu'ici on ne tolère pas la casse, parce qu'un code inconnu a une réponse utile
> (le message générique) là où une pile inconnue n'en avait pas.

- [x] **Step 2 : le lancer et le voir échouer.**

- [x] **Step 3 : écrire `CombatRejectionMessages.cs`**

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Ce qu'on montre au joueur quand le serveur refuse sa commande.
///
/// <para>Le moteur nomme ses refus ; le client les recevait et n'en faisait rien, si bien
/// qu'une carte refusée se contentait de ne pas bouger. Les huit codes sont ceux du
/// moteur, pas ceux du transport : ils décrivent une règle du jeu, et se traduisent donc
/// en une phrase de jeu.</para>
///
/// <para>Un code inconnu obtient le message générique plutôt qu'un vide : le moteur peut
/// en gagner un demain, et un refus muet est le défaut qu'on retire.</para>
/// </summary>
public static class CombatRejectionMessages
{
    public const string Generic = "Le serveur a refusé cette action.";

    private static readonly Dictionary<string, string> MessagesByCode =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["INSUFFICIENT_ENERGY"] = "Pas assez d'énergie.",
            ["CARD_NOT_IN_HAND"] = "Cette carte n'est plus dans votre main.",
            ["INVALID_TARGET"] = "Cible invalide.",
            ["NOT_ACTOR_TURN"] = "Ce n'est pas votre tour.",
            ["OUT_OF_SYNC"] = "Synchronisation en cours…",
            ["COMBAT_NOT_FOUND"] = "Ce combat n'existe plus.",
            ["INVALID_COMMAND"] = "Action impossible.",
            ["INTERNAL_ERROR"] = "Erreur du serveur.",
        };

    private static readonly List<string> Codes = new List<string>(MessagesByCode.Keys);

    public static IReadOnlyList<string> KnownCodes => Codes;

    public static string ForCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Generic;

        return MessagesByCode.TryGetValue(code, out string message) ? message : Generic;
    }

    /// <summary>
    /// Le seul refus qui a déjà son langage visuel : le compteur d'énergie qui rougit,
    /// que le refus local utilise depuis toujours.
    /// </summary>
    public static bool WarrantsEnergyGlow(string code)
    {
        return string.Equals(code, "INSUFFICIENT_ENERGY", StringComparison.Ordinal);
    }
}
```

- [x] **Step 4 : relancer.** Attendu : **105 tests, 0 échec** (101 + 4).

- [x] **Step 5 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/Authoritative/CombatRejectionMessages.cs \
        Assets/Scripts/Scene/STS/Combat/Authoritative/CombatRejectionMessages.cs.meta \
        Assets/Tests/EditMode/CombatRejectionMessagesTests.cs \
        Assets/Tests/EditMode/CombatRejectionMessagesTests.cs.meta
git commit -m "feat(sts): turn the engine's rejection codes into something to show

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 5 : Le mode voyage dans la charge utile de connexion

**But :** **la tâche qui allume tout le reste.** Le pont React sait choisir l'endpoint de
snapshot, la file privée et la destination des commandes à partir d'un mode ; personne ne
lui envoie ce mode. Aujourd'hui `ConnectAsync` poste `{ "combatId": "…" }` et rien d'autre
(constat 9).

**Ce que coûte l'oubli, et pourquoi cette tâche le dit dans son propre commit :** un duel
connecté sans mode prend **silencieusement** les routes PvE. La socket s'ouvre — donc
`connected == true`, donc aucun avertissement —, la souscription se fait sur une file où
personne ne publiera jamais, et chaque commande part vers une destination PvE que le
serveur ignore. **Aucune erreur n'apparaît nulle part.** Le symptôme est un combat qui ne
commence pas, sans une ligne de journal pour dire pourquoi. C'est le mode de défaillance
le plus coûteux de tout ce plan, et il tient à un champ manquant.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Api/CombatBridge/ReactCombatBridgeCore.cs`
- Modify: `Assets/Scripts/Scene/STS/Api/ReactCombatBridge.cs`
- Modify: `Assets/Scripts/Scene/STS/Api/AuthoritativeCombatIdentity.cs`
- Modify: `Assets/Tests/EditMode/ReactCombatBridgeTests.cs`

**Interfaces:**
- Produces: `static string ReactCombatBridgeCore.CreateConnectPayload(string combatId, string mode)`,
  `static Task<bool> ReactCombatBridge.ConnectAsync(string combatId, string mode)`,
  `static string AuthoritativeCombatIdentity.GetPvpTransportId(string battleId)`.
  Les tâches 9 et 10 les consomment.

- [x] **Step 1 : ajouter les tests à `ReactCombatBridgeTests.cs`**

À la fin de la classe existante, sans rien retirer :

```csharp
    [Test]
    public void ConnectPayloadNamesTheCombatAndItsMode()
    {
        string payload = ReactCombatBridgeCore.CreateConnectPayload("battle-77", "PVP");

        StringAssert.Contains("\"combatId\":\"battle-77\"", payload);
        StringAssert.Contains("\"mode\":\"PVP\"", payload);
    }

    /// Sans mode, la couche React retombe sur ses routes PvE sans le dire : la socket
    /// s'ouvre, la file reste vide et les commandes disparaissent. Refuser ici est le
    /// seul endroit où cette panne peut encore faire du bruit.
    [Test]
    public void ConnectPayloadRefusesAModelessConnection()
    {
        Assert.Throws<System.ArgumentException>(
            () => ReactCombatBridgeCore.CreateConnectPayload("battle-77", null));
        Assert.Throws<System.ArgumentException>(
            () => ReactCombatBridgeCore.CreateConnectPayload("battle-77", "   "));
    }

    [Test]
    public void ConnectPayloadRefusesACombatlessConnection()
    {
        Assert.Throws<System.ArgumentException>(
            () => ReactCombatBridgeCore.CreateConnectPayload("", "PVP"));
    }

    /// Le PvE s'adresse par son runId, le PvP par son battleId. Deux méthodes plutôt
    /// qu'une paramétrée : la première lève sur un runId vide, ce qui est exactement ce
    /// qu'un PvP lui présenterait.
    [Test]
    public void APvpConnectionUsesTheBattleIdAsTransportId()
    {
        Assert.That(AuthoritativeCombatIdentity.GetPvpTransportId("battle-77"),
            Is.EqualTo("battle-77"));
        Assert.Throws<System.ArgumentException>(
            () => AuthoritativeCombatIdentity.GetPvpTransportId(" "));
    }
```

- [x] **Step 2 : les lancer et les voir échouer.**

- [x] **Step 3 : écrire `CreateConnectPayload` dans `ReactCombatBridgeCore`**

À placer à côté de `CreateCommand`. **Ne pas y utiliser `CombatModes`** : cet assembly ne
référence pas `STS.AuthoritativeCombat` (`references: []`).

```csharp
    /// <summary>
    /// La charge utile qui ouvre une socket de combat.
    ///
    /// <para>Le <c>mode</c> est ce qui dit à la couche React quelles trois destinations
    /// employer : l'endpoint de snapshot, la file privée et la destination des commandes.
    /// Rien d'autre ne le transporte, parce qu'aucun code React n'ouvre jamais un combat
    /// — c'est Unity qui le fait.</para>
    ///
    /// <para>Il est exigé plutôt que défauté, et c'est délibéré : un duel connecté sans
    /// mode emprunte les routes PvE <b>en silence</b>. La socket s'ouvre, la file reste
    /// vide, les commandes partent dans le vide, et aucune erreur n'apparaît nulle part.
    /// Lever ici est le dernier endroit où cette panne fait encore du bruit.</para>
    /// </summary>
    public static string CreateConnectPayload(string combatId, string mode)
    {
        if (string.IsNullOrWhiteSpace(combatId))
            throw new ArgumentException("A combat identifier is required", nameof(combatId));
        if (string.IsNullOrWhiteSpace(mode))
            throw new ArgumentException("A combat mode is required", nameof(mode));

        return JsonConvert.SerializeObject(new { combatId, mode });
    }
```

- [x] **Step 4 : faire passer le mode dans `ReactCombatBridge.ConnectAsync`**

Remplacer la méthode (lignes 49-55) par :

```csharp
    public static Task<bool> ConnectAsync(string combatId, string mode)
    {
        ReactCombatBridge bridge = EnsureInstance();
        bridge.core.Connect(combatId);
        string json = ReactCombatBridgeCore.CreateConnectPayload(combatId, mode);
        return Task.FromResult(InvokeConnect(json) != 0);
    }
```

**Aucune surcharge sans mode.** Un appelant qui aurait oublié le mode doit cesser de
compiler, pas se connecter au hasard. `Disconnect()` (ligne 57) ne change pas : la
déconnexion n'a pas de route à choisir.

`using Newtonsoft.Json;` reste nécessaire dans `ReactCombatBridgeCore.cs` (il y est déjà,
ligne 4) ; il devient inutile dans `ReactCombatBridge.cs` — **ne pas le retirer sans
vérifier** que `JsonConvert` n'y sert plus ailleurs (`grep -n JsonConvert Assets/Scripts/Scene/STS/Api/ReactCombatBridge.cs`).

- [x] **Step 5 : écrire `GetPvpTransportId`**

Dans `AuthoritativeCombatIdentity.cs`, **à côté** de `GetTransportId`, sans y toucher :

```csharp
    /// <summary>
    /// Un duel s'adresse par son identifiant de bataille : c'est lui qui compose le sujet
    /// de la souscription, la destination des commandes et l'URL du snapshot, et c'est lui
    /// que chaque message doit porter comme <c>combatId</c> pour que le noyau du pont
    /// l'accepte.
    /// </summary>
    public static string GetPvpTransportId(string battleId)
    {
        if (string.IsNullOrWhiteSpace(battleId))
            throw new ArgumentException(
                "A PvP battle transport requires a battle id", nameof(battleId));

        return battleId;
    }
```

- [x] **Step 6 : mettre à jour l'appelant PvE**

Dans `CombatManager`, `ConnectAuthoritativeCombatSocketRoutine` (ligne 255) prend
désormais le mode :

```csharp
    IEnumerator ConnectAuthoritativeCombatSocketRoutine(string transportId, string mode)
    {
        Task<bool> connectTask = ReactCombatBridge.ConnectAsync(transportId, mode);
        while (!connectTask.IsCompleted)
            yield return null;

        bool connected = connectTask.Status == TaskStatus.RanToCompletion && connectTask.Result;
        Debug.Log($"[STS-BRIDGE] socket connect combatId={transportId} mode={mode} success={connected}");
        if (!connected)
            Debug.LogWarning("[STS-BRIDGE] Combat socket failed to connect; commands will silently no-op until reconnected.");
    }
```

Et les deux appels PvE existants (lignes 186 et 244) passent le mode PvE :

```csharp
            StartCoroutine(ConnectAuthoritativeCombatSocketRoutine(
                AuthoritativeCombatIdentity.GetTransportId(
                    RunManager.Instance.runId,
                    RunManager.Instance.activeCombat),
                CombatModes.ToWireName(CombatMode.Pve)));
```

> **Le PvE se met donc à envoyer `mode: "PVE"` là où il n'envoyait rien.** C'est voulu :
> la couche React lit ce champ et son défaut est justement PvE, donc l'expliciter ne
> change pas la route — mais ça retire le défaut silencieux, qui est le sujet de cette
> tâche. **Si la non-régression PvE de la tâche 15 échouait à la connexion**, le repli
> conservateur est de rendre le mode optionnel dans `CreateConnectPayload` **pour le seul
> PvE** ; ne jamais le rendre optionnel pour le PvP.

- [x] **Step 7 : relancer.** Attendu : **109 tests, 0 échec** (105 + 4), et
      `AuthoritativeConnectionUsesRunIdAsTransportId` toujours vert — c'est le contrôle
      qui dit qu'on a ajouté au lieu de remplacer.

- [x] **Step 8 : commit**

```bash
git add Assets/Scripts/Scene/STS/Api/CombatBridge/ReactCombatBridgeCore.cs \
        Assets/Scripts/Scene/STS/Api/ReactCombatBridge.cs \
        Assets/Scripts/Scene/STS/Api/AuthoritativeCombatIdentity.cs \
        Assets/Scripts/Scene/STS/Combat/CombatManager.cs \
        Assets/Tests/EditMode/ReactCombatBridgeTests.cs
git commit -m "feat(sts): tell the bridge which combat it is connecting to

The React layer picks its snapshot endpoint, its private queue and its command
destination from a mode, and no code sent one, because no React code ever opens
a combat: Unity does. A PvP battle connected without it took the PvE routes in
complete silence — socket up, queue empty, commands dropped, no error anywhere.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 6 : La session PvP portée par `RunManager`

**But :** un endroit, un seul, où « le combat en cours est un duel » est écrit —
`RunManager` étant le seul objet qui traverse les scènes (constat 13). Et au passage, la
fermeture de la fuite du constat 2.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Core/RunManager.cs`

**Interfaces:**
- Produces: `RunManager.activePvpBattleId`, `BeginPvpBattle(string)`, `EndPvpBattle()`,
  `LocalPvpParticipant()`, `OpponentPvpParticipant()`. Les tâches 7, 8, 9 et 14 les
  consomment.

- [x] **Step 1 : déclarer le champ de session**

À côté de `pvpBattleId` (ligne 47) :

```csharp
    /// La bataille effectivement en train d'être jouée.
    ///
    /// Distinct de `pvpBattleId`, qui ne fait que retenir la dernière bataille annoncée
    /// par le matchmaking et n'est effacé qu'en fin de run. Tout ce qui doit se comporter
    /// autrement en duel s'appuie sur ce champ-ci, de sorte qu'un matchmaking passé ne
    /// puisse jamais faire croire à une rencontre PvE qu'elle est un duel.
    public string activePvpBattleId;
```

- [x] **Step 2 : ouvrir et fermer la session**

À côté de `CachePvpBattleParticipants` (ligne 667) :

```csharp
    public void BeginPvpBattle(string battleId)
    {
        activePvpBattleId = string.IsNullOrWhiteSpace(battleId) ? null : battleId.Trim();
    }

    /// Referme la session, et rien d'autre : une run PvE mise en pause pendant le duel
    /// doit se retrouver exactement comme elle était.
    public void EndPvpBattle()
    {
        activePvpBattleId = null;
        inCombat = false;
    }
```

- [x] **Step 3 : rendre les participants lisibles de l'extérieur**

`ApplyPvpParticipantDisplayNames` (ligne 681) calcule déjà le participant local et
l'adverse dans son corps. **Extraire ces deux calculs** en méthodes publiques et faire
appeler celles-ci par la méthode existante, afin qu'il n'existe qu'une seule définition
de « qui est l'adversaire » :

```csharp
    /// Le participant que ce client pilote, reconnu par l'identifiant d'utilisateur que
    /// le profil PVP a donné. À défaut, le premier de la première équipe — un repli qui
    /// n'est juste que pour l'hôte, et qui ne sert qu'à ne pas afficher un écran vide.
    public STSApiClient.StsPvpParticipantSnapshot LocalPvpParticipant()
    {
        if (pvpParticipants == null || pvpParticipants.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(pvpLocalUserId))
        {
            STSApiClient.StsPvpParticipantSnapshot mine = pvpParticipants.Find(p =>
                p != null
                && !string.IsNullOrWhiteSpace(p.userId)
                && string.Equals(p.userId, pvpLocalUserId, StringComparison.OrdinalIgnoreCase));
            if (mine != null)
                return mine;
        }

        return pvpParticipants.Find(p => p != null && p.teamIndex == 0 && p.slotIndex == 0)
            ?? pvpParticipants.Find(p => p != null);
    }

    /// Le premier participant d'une autre équipe que la nôtre. En 1v1 il n'y en a qu'un ;
    /// la formulation par équipe est ce qui la laissera vraie en 2v2.
    public STSApiClient.StsPvpParticipantSnapshot OpponentPvpParticipant()
    {
        if (pvpParticipants == null || pvpParticipants.Count == 0)
            return null;

        STSApiClient.StsPvpParticipantSnapshot local = LocalPvpParticipant();
        return pvpParticipants.Find(p =>
                   p != null && p != local
                   && (local == null || p.teamIndex != local.teamIndex))
               ?? pvpParticipants.Find(p => p != null && p != local);
    }
```

Puis, dans `ApplyPvpParticipantDisplayNames`, remplacer les deux blocs de recherche par
`LocalPvpParticipant()` et `OpponentPvpParticipant()`. **Ne rien changer d'autre à son
corps** : les affectations de `playerDisplayName` / `playerUserId` restent identiques.

- [x] **Step 4 : fermer la fuite du constat 2**

Toujours dans `ApplyPvpParticipantDisplayNames`, remplacer la garde :

```csharp
        if (string.IsNullOrWhiteSpace(pvpBattleId) || pvpParticipants == null || pvpParticipants.Count == 0)
            return;
```

par :

```csharp
        // Sur la bataille en cours, et sur elle seule. La garde précédente lisait
        // `pvpBattleId`, qui retient la dernière bataille annoncée par le matchmaking et
        // survit à tout : la rencontre PvE jouée après un matchmaking affichait donc le
        // pseudo de l'adversaire PvP sur son premier ennemi.
        if (string.IsNullOrWhiteSpace(activePvpBattleId) || pvpParticipants == null || pvpParticipants.Count == 0)
            return;
```

- [x] **Step 5 : effacer la session en fin de run**

Dans la méthode qui remet la run à zéro, à côté de `ClearPvpBattleParticipants();`
(ligne 327), ajouter :

```csharp
        activePvpBattleId = null;
```

- [x] **Step 6 : compiler.** Suite EditMode : **109 tests, 0 échec**. Aucun test ne
      couvre `RunManager` (c'est un `MonoBehaviour`) : cette étape ne vérifie que la
      compilation.

- [x] **Step 7 : commit**

```bash
git add Assets/Scripts/Scene/STS/Core/RunManager.cs
git commit -m "feat(sts): carry the battle being played, not the last one matched

Also stops a finished matchmaking from renaming the next PvE encounter's first
enemy after the opponent it found.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 7 : L'entrée PvP depuis le menu multijoueur

**But :** le matchmaking trouve un adversaire et s'arrête (constat 1). Et le joueur qui
s'inscrit **le premier** n'apprend jamais que l'adversaire est arrivé (constat 3) : sans
cette moitié-là, un seul des deux joueurs entre dans le combat, ce qui suffit à rendre le
mode injouable.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/UI/MultiplayerMenuController.cs`
- Create: `Assets/Scripts/Scene/STS/Combat/Authoritative/PvpMatchNotifications.cs` (Step 4)
- Test: `Assets/Tests/EditMode/PvpMatchNotificationsTests.cs` (Step 4)

- [x] **Step 1 : entrer dans le combat quand une bataille est trouvée**

Remplacer, dans `QuickMatchAsync` (lignes 320-327) :

```csharp
            string battleId = response.Value<string>("battleId");
            if (!string.IsNullOrWhiteSpace(battleId))
            {
                await CacheBattleParticipantsAsync(battleId);
                ShowNotification("Match PVP trouvé !");
                await CancelQuickMatchAsync(false);
                return;
            }
```

par :

```csharp
            string battleId = response.Value<string>("battleId");
            if (!string.IsNullOrWhiteSpace(battleId))
            {
                await EnterPvpBattleAsync(battleId);
                return;
            }
```

- [x] **Step 2 : écrire l'entrée**

```csharp
    /// L'unique porte d'entrée d'un duel : les participants en cache, la file d'attente
    /// refermée, la session ouverte, puis la scène.
    ///
    /// Pas de BeginLoading ici : GameManager.Start en ouvre un et le referme dans son
    /// `finally`. En ajouter un second ferait rester le compteur à un, et l'écran de
    /// chargement ne se lèverait jamais.
    private async Task EnterPvpBattleAsync(string battleId)
    {
        await CacheBattleParticipantsAsync(battleId);
        await CancelQuickMatchAsync(false);

        if (RunManager.Instance == null)
        {
            ShowNotification("Impossible de rejoindre le combat : gestionnaire de partie absent.");
            return;
        }

        RunManager.Instance.BeginPvpBattle(battleId);
        Debug.Log($"[STS-PVP] Entering battle {battleId}");
        STSSceneLoader.Instance?.LoadScene("STS_Combat");
    }
```

- [x] **Step 3 : guetter la notification quand on reste en file**

Dans la branche `queued` de `QuickMatchAsync`, démarrer la veille :

```csharp
            bool queued = response.Value<bool?>("queued") ?? response.Value<bool?>("isQueued") ?? false;
            if (!queued)
            {
                ShowNotification("Recherche rapide PVP lancée.");
            }
            else
            {
                ShowNotification("Recherche rapide PVP en cours...");
            }

            // Le joueur qui s'inscrit le premier ne reçoit pas de battleId : c'est le
            // second qui en obtient un. Sans cette veille, seul le second entre jamais
            // dans le combat, et le premier attend indéfiniment devant un menu.
            StartCoroutine(WatchForMatchedBattleRoutine());
```

et :

```csharp
    /// <summary>
    /// Interroge les notifications PVP jusqu'à ce qu'une bataille apparaisse.
    ///
    /// <para><b>La forme exacte d'une notification n'est pas vérifiable depuis ce
    /// dépôt</b> : le nom de la requête est traduit en URL dans `insastral`, et le DTO
    /// vit dans `webAPI` — deux dépôts qu'on n'ouvre pas ici. Plutôt que d'inventer un
    /// nom de champ, on cherche le premier `battleId` présent n'importe où dans la
    /// réponse, et le Step 4 journalise la trame brute pour que la passe suivante puisse
    /// le nommer. C'est un provisoire assumé, pas une tolérance de protocole.</para>
    /// </summary>
    private IEnumerator WatchForMatchedBattleRoutine()
    {
        while (isQuickMatchQueued)
        {
            yield return new WaitForSeconds(2f);
            if (!isQuickMatchQueued)
                yield break;

            Task<JToken> notificationsTask = STSApiClient.ListPvpNotificationsAsync();
            while (!notificationsTask.IsCompleted)
                yield return null;

            if (notificationsTask.Status != TaskStatus.RanToCompletion || notificationsTask.Result == null)
                continue;

            JToken notifications = notificationsTask.Result;
            Debug.Log($"[STS-PVP] notifications payload: {notifications.ToString(Newtonsoft.Json.Formatting.None)}");

            string battleId = FindFirstBattleId(notifications);
            if (string.IsNullOrWhiteSpace(battleId))
                continue;

            _ = EnterPvpBattleAsync(battleId);
            yield break;
        }
    }

    private static string FindFirstBattleId(JToken token)
    {
        if (token == null)
            return null;

        foreach (JToken descendant in token.DescendantsAndSelf())
        {
            if (descendant is JProperty property
                && string.Equals(property.Name, "battleId", StringComparison.Ordinal))
            {
                string value = property.Value?.Value<string>();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        return null;
    }
```

`DescendantsAndSelf()` est défini sur `JContainer`, pas sur `JToken`. Écrire donc, pour
rester sûr :

```csharp
        if (!(token is JContainer container))
            return null;

        foreach (JToken descendant in container.DescendantsAndSelf())
```

Ajouter en tête de fichier les `using` manquants : `System.Collections` (pour
`IEnumerator`) — `System`, `System.Threading.Tasks`, `Newtonsoft.Json.Linq` et
`UnityEngine` y sont déjà (lignes 1-7).

- [x] **Step 4 : la forme réelle des notifications — relevée sur le serveur, 2026-08-24**

Elle n'a pas eu besoin d'être devinée : `GET /api/sts/pvp/notifications` rend une liste de

```
{ id: UUID, type: string, title: string, body: string,
  actorUserId: UUID, read: boolean, createdAt: Instant, payload: { ... } }
```

et `POST /api/sts/pvp/notifications/{notificationId}/ack` en marque une comme lue. Le
`type` vaut `CHALLENGE_RECEIVED`, `CHALLENGE_ACCEPTED`, `CHALLENGE_DECLINED`,
`QUICK_MATCH_FOUND`, `BATTLE_UPDATED` ou `INFO`. À l'appariement, le serveur crée un
`QUICK_MATCH_FOUND` **pour les deux joueurs**, de charge utile
`{ "battleId": "<uuid>", "friendly": <bool> }`.

`FindFirstBattleId` est donc remplacée par `PvpMatchNotifications`, en C# pur et testée
(8 tests). Trois choses que le provisoire n'avait pas :

- **le type est filtré.** `CHALLENGE_RECEIVED`, `CHALLENGE_DECLINED` et `BATTLE_UPDATED`
  nomment une bataille eux aussi : lire le premier `battleId` venu faisait entrer dans un
  combat terminé au lieu de laisser chercher.
- **la notification est acquittée avant d'ouvrir la scène**, et **par bataille** plutôt que
  par l'identifiant qu'on vient de lire : le joueur qui a reçu son `battleId` directement
  ne regarde jamais la liste, et sa notification non lue le ramènerait dans ce combat-là à
  sa recherche suivante.
- **l'intervalle est de 3 secondes** (`MatchPollIntervalSeconds`) : c'est le retard maximum
  ajouté entre l'arrivée de l'adversaire et l'ouverture du combat, pour 20 requêtes par
  minute et par joueur en file, et seulement pendant la recherche.

Une interrogation ratée est journalisée et n'annule pas la recherche ; annuler la
recherche arrête la veille tout de suite, par la poignée de coroutine.

- [x] **Step 5 : compiler.** Suite EditMode : **109 tests, 0 échec** ; **142 après le Step 4**.

- [x] **Step 6 : commit**

```bash
git add Assets/Scripts/Scene/STS/UI/MultiplayerMenuController.cs
git commit -m "feat(sts): walk into the battle the matchmaker found

Both halves of it: the player who gets a battleId back, and the one who queued
first and would otherwise never learn an opponent had arrived.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 8 : Le montage de la scène en duel

**But :** `GameManager.SetupGame` n'a que deux branches, et celle qui s'applique sans run
tire une rencontre PvE au hasard (constat 4). Il en faut une troisième.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Entities/Enemy.cs`
- Modify: `Assets/Scripts/Scene/STS/Core/GameManager.cs`

- [x] **Step 1 : un constructeur d'adversaire humain**

Dans `Enemy.cs`, après les deux constructeurs existants :

```csharp
    /// <summary>
    /// Un adversaire piloté par un humain.
    ///
    /// <para>Sans <c>EnemyData</c>, et c'est le point : il n'a ni motif ni intention à
    /// afficher, puisque c'est l'autre joueur qui décide. <c>PeekNextAction</c> rend
    /// <c>null</c> dans ce cas et <c>CharacterUI</c> vide alors la zone d'intention, donc
    /// rien à faire de plus.</para>
    ///
    /// <para>Il reste un <c>Enemy</c> plutôt qu'un <c>Character</c> parce que
    /// <c>DropZone.Init</c> caste en <c>Enemy</c> tout ce qui n'est pas <c>isPlayer</c> ;
    /// avec <c>data == null</c>, ce cast réussit et le portrait se charge depuis
    /// <c>STS/Characters/{name}</c>, où le nom du personnage jouable se trouve.</para>
    ///
    /// <para>Ses points de vie arrivent avec le premier état autoritatif ; ceux passés ici
    /// ne servent qu'à ce qu'il ne soit pas mort-né entre le montage et ce premier état.</para>
    /// </summary>
    public Enemy(string characterName, int placeholderMaxHP, string userId, string displayName)
        : base(characterName, placeholderMaxHP)
    {
        this.isPlayer = false;
        this.isAlly = false;
        this.isLocalPlayer = false;
        this.playerUserId = userId;
        this.playerDisplayName = displayName;
    }
```

- [x] **Step 2 : la troisième branche de `SetupGame`**

Tout en haut de `SetupGame()` (ligne 41), **avant** le `if` existant :

```csharp
        if (RunManager.Instance != null
            && !string.IsNullOrWhiteSpace(RunManager.Instance.activePvpBattleId))
        {
            SetupPvpBattle();
            return;
        }
```

- [x] **Step 3 : écrire `SetupPvpBattle`**

```csharp
    /// <summary>
    /// Monte la scène pour un duel : deux combattants, un deck vide, aucune run.
    ///
    /// <para>Les points de vie, l'énergie, les statuts et les piles arrivent avec le
    /// premier état autoritatif ; ici on ne pose que les objets que l'interface a besoin
    /// d'avoir sous la main pour se construire. C'est la même division qu'en PvE, où
    /// EnsureEncounterEnemies pose les ennemis avant que l'état ne les remplisse.</para>
    ///
    /// <para><c>RunManager.player</c> n'est pas touché : une run PvE mise en pause pour
    /// jouer un duel doit se retrouver intacte.</para>
    /// </summary>
    void SetupPvpBattle()
    {
        RunManager run = RunManager.Instance;
        STSApiClient.StsPvpParticipantSnapshot localParticipant = run.LocalPvpParticipant();
        STSApiClient.StsPvpParticipantSnapshot opponentParticipant = run.OpponentPvpParticipant();

        const int PlaceholderHp = 1;

        var localPlayer = new Player(CharacterNameOf(localParticipant), PlaceholderHp)
        {
            playerDisplayName = localParticipant != null ? localParticipant.displayName : null,
            playerUserId = localParticipant != null ? localParticipant.userId : null
        };

        combat.allies.Clear();
        combat.allies.Add(localPlayer);

        combat.enemies = new List<Character>
        {
            new Enemy(
                CharacterNameOf(opponentParticipant),
                PlaceholderHp,
                opponentParticipant != null ? opponentParticipant.userId : null,
                opponentParticipant != null ? opponentParticipant.displayName : null)
        };

        // Vide : le premier état apporte les quatre piles telles que le serveur les tient.
        combat.deck = new DeckManager();

        Debug.Log($"[STS-PVP] Scene set up for battle {run.activePvpBattleId}: "
            + $"{localPlayer.name} vs {combat.enemies[0].name}");
    }

    /// Le personnage choisi par un participant, qui est aussi le nom du portrait sous
    /// Resources/STS/Characters. À défaut, EP — le premier de la liste jouable — plutôt
    /// qu'un nom vide, qui laisserait un emplacement sans image.
    static string CharacterNameOf(STSApiClient.StsPvpParticipantSnapshot participant)
    {
        string selected = participant != null ? participant.selectedCharacter : null;
        return string.IsNullOrWhiteSpace(selected)
            ? SelectableCharacter.EP.ToString()
            : selected.Trim();
    }
```

- [x] **Step 4 : ne pas empiler un `EnemyData` nul**

Dans `CombatManager.Init`, la boucle de la ligne 146 fait
`Enemy enn = enemy as Enemy; currentEnemiesData.Add(enn.data);`. En duel, `enn.data` vaut
`null`. Remplacer par :

```csharp
        foreach (var enemy in enemies)
        {
            Enemy enn = enemy as Enemy;
            if (enn == null)
                continue;

            // Un adversaire humain n'a pas d'EnemyData : rien à empiler pour lui, et
            // currentEnemiesData ne sert de toute façon qu'à composer une récompense PvE.
            if (enn.data != null)
                currentEnemiesData.Add(enn.data);
            enn.combat = this;
        }
```

- [x] **Step 5 : compiler.** Suite EditMode : **109 tests, 0 échec**.

- [x] **Step 6 : commit**

```bash
git add Assets/Scripts/Scene/STS/Entities/Enemy.cs \
        Assets/Scripts/Scene/STS/Core/GameManager.cs \
        Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "feat(sts): set the combat scene up for a duel

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 9 : Le bootstrap PvP dans `CombatManager`

**But :** un duel doit être autoritatif **avant** d'avoir reçu quoi que ce soit — sinon sa
première carte partirait dans le moteur local —, il n'a pas d'état de run à appliquer, et
il ne doit rien écrire dans la run du joueur.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

**Interfaces:**
- Consumes: `CombatMode`, `CombatModes` (tâche 1), `GetPvpTransportId`, `ConnectAsync`
  (tâche 5), `RunManager.activePvpBattleId` (tâche 6).
- Produces: `CombatManager.Mode`. Les tâches 10 à 14 la consomment.

- [x] **Step 1 : le mode, et `UsesAuthoritativeCombat` défini par lui**

Remplacer la propriété de la ligne 132 par :

```csharp
    /// <summary>
    /// Ce que ce combat est. Un duel se reconnaît à la session ouverte par le menu
    /// multijoueur ; un combat de run, à l'état que la run porte.
    ///
    /// <para>L'ordre compte : le duel se déclare <b>avant</b> d'avoir reçu son premier
    /// état, là où le PvE ne se déclarait qu'après. C'est ce qui empêche la première
    /// carte d'un duel de partir dans le moteur local pendant que la socket s'ouvre.</para>
    /// </summary>
    public CombatMode Mode
    {
        get
        {
            if (RunManager.Instance == null)
                return CombatMode.Local;

            if (!string.IsNullOrWhiteSpace(RunManager.Instance.activePvpBattleId))
                return CombatMode.Pvp;

            return RunManager.Instance.activeCombat != null
                && RunManager.Instance.activeCombat.Type == JTokenType.Object
                    ? CombatMode.Pve
                    : CombatMode.Local;
        }
    }

    public bool UsesAuthoritativeCombat => Mode != CombatMode.Local;
```

> La branche PvE est mot pour mot l'ancienne condition : **le PvE et le tutoriel ne
> changent pas de comportement d'un iota.** Seul le duel gagne une réponse.

- [x] **Step 2 : garder l'état autoritatif chez soi**

Déclarer, à côté de `combatantRegistryBuilt` (ligne 103) :

```csharp
    // L'état autoritatif courant. En PvE c'est aussi RunManager.activeCombat, parce que la
    // run le possède ; en PvP la run n'a rien à voir avec ce combat et ne doit surtout pas
    // s'en trouver modifiée — un joueur qui met une run en pause pour jouer un duel doit la
    // retrouver telle quelle.
    private JToken authoritativeCombatState;
```

Dans `ApplyAuthoritativeCombatState`, remplacer la ligne 828 :

```csharp
        RunManager.Instance.activeCombat = combatToken;
```

par :

```csharp
        authoritativeCombatState = combatToken;
        if (Mode != CombatMode.Pvp)
            RunManager.Instance.activeCombat = combatToken;
```

Et faire lire `GetAuthoritativeRevision` (ligne 792) sur le champ plutôt que sur la run :

```csharp
    long GetAuthoritativeRevision()
    {
        JToken state = authoritativeCombatState;
        if (state == null || state.Type != JTokenType.Object)
            return 0L;

        return state.Value<long?>("revision") ?? 0L;
    }
```

- [x] **Step 3 : ne pas écraser le deck de la run**

Dans `ApplyAuthoritativePlayerPiles`, remplacer la garde de la ligne 2194 :

```csharp
        if (RunManager.Instance != null)
```

par :

```csharp
        // Le deck de la run est le deck de la run. Les piles d'un duel viennent du deck
        // PVP, stocké ailleurs côté serveur, et les recopier ici remplacerait le deck
        // d'une run en pause par celui du duel.
        if (Mode != CombatMode.Pvp && RunManager.Instance != null)
```

- [x] **Step 4 : ne pas auditer un nœud qu'on n'a pas visité**

Dans `Init`, remplacer le bloc de la ligne 168 :

```csharp
        if (RunManager.Instance!=null)
        {
            RunManager.Instance.inCombat=true;
            STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "combat_init");
        }
```

par :

```csharp
        if (RunManager.Instance != null)
        {
            RunManager.Instance.inCombat = true;
            // Un duel n'est pas un nœud de carte : l'auditer en tant que tel écrirait dans
            // l'historique d'une run l'entrée dans un combat qui ne lui appartient pas.
            if (Mode != CombatMode.Pvp)
            {
                STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "combat_init");
            }
        }
```

- [x] **Step 5 : la branche PvP de `Init`**

**Avant** le `if (UsesAuthoritativeCombat)` de la ligne 172 :

```csharp
        if (Mode == CombatMode.Pvp)
        {
            allowTurn = true;
            StartCoroutine(BootstrapPvpBattleRoutine());
            return;
        }
```

- [x] **Step 6 : écrire `BootstrapPvpBattleRoutine`**

À placer à côté de `BootstrapAuthoritativeCombatRoutine` :

```csharp
    /// <summary>
    /// Ouvre un duel.
    ///
    /// <para>Contrairement au PvE, il n'y a rien à appliquer d'avance : le premier état
    /// qu'un duel voit est le COMBAT_SNAPSHOT que la couche React va chercher en ouvrant
    /// la socket. On se connecte, et on attend.</para>
    ///
    /// <para>Et contrairement au PvE, <b>il n'y a pas de repli sur StartLocalCombatFlow</b>.
    /// Un combat dont l'autre moitié est un autre joueur n'a pas de vérité locale : jouer
    /// une simulation en attendant afficherait un combat imaginaire.</para>
    /// </summary>
    IEnumerator BootstrapPvpBattleRoutine()
    {
        // Un yield inconditionnel : le seul autre est dans un bloc #if, et sans celui-ci la
        // méthode cesserait d'être un itérateur dans un build non-WebGL.
        yield return null;

        string battleId = RunManager.Instance != null
            ? RunManager.Instance.activePvpBattleId
            : null;

        if (string.IsNullOrWhiteSpace(battleId))
        {
            Debug.LogError("[STS-PVP] The combat scene was opened as a duel without a battle id; nothing can connect.");
        }
        else
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ReactCombatBridge.CombatEventReceived += HandleReactCombatEvent;
            ReactCombatBridge.CombatStatusChanged += HandleReactCombatStatusChanged;
            yield return ConnectAuthoritativeCombatSocketRoutine(
                AuthoritativeCombatIdentity.GetPvpTransportId(battleId),
                CombatModes.ToWireName(CombatMode.Pvp));
#else
            Debug.LogWarning("[STS-PVP] A duel needs the React combat bridge, which exists only in a WebGL player: no state will arrive in this build.");
#endif
        }

        // Toujours, même après l'erreur : sans ça l'écran de chargement ne se lève jamais
        // et le joueur reste devant un voile, ce qui est pire qu'un combat vide.
        STSSceneLoader.Instance?.SceneReady();
    }
```

- [x] **Step 7 : ne pas resynchroniser un duel par la route PvE**

`RefreshAuthoritativeCombatState` (ligne 552) part chercher l'état par
`GetCombatStateAsync(runId)`, qui est la route de run. En duel, elle sortirait de toute
façon sur la garde `runId` vide — mais si une run est en pause, elle irait chercher
**l'état de cette run** et l'appliquerait au duel. Remplacer la garde :

```csharp
    IEnumerator RefreshAuthoritativeCombatState()
    {
        // La route de resynchronisation est celle d'une run. Un duel se resynchronise par
        // la couche React, qui refait son snapshot sur l'endpoint PvP dès qu'elle voit un
        // trou de révision ; passer par ici lui appliquerait l'état d'une run en pause.
        if (Mode != CombatMode.Pve
            || RunManager.Instance == null
            || string.IsNullOrWhiteSpace(RunManager.Instance.runId))
            yield break;
```

- [x] **Step 8 : compiler.** Suite EditMode : **109 tests, 0 échec**. Aucun test ne
      construit `CombatManager` : cette étape ne dit que « ça compile ».

- [x] **Step 9 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "feat(sts): bootstrap a duel from its socket, and leave the run alone

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 10 : Les derniers littéraux `"player"`

**But :** les quatre points du constat 5. Le premier est le blocage n°1 du mode : sans
lui, la main du joueur reste vide pour toujours.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`

- [x] **Step 1 : l'identifiant local du registre**

Dans `BuildCombatantRegistry` (ligne 2135), remplacer :

```csharp
        IReadOnlyList<CombatantDescriptor> descriptors =
            CombatantSnapshotReader.ReadCombatants(combatToken, "player");
```

par :

```csharp
        // "player" reste la convention PvE ; en duel c'est un UUID d'utilisateur, et le
        // résolveur retombe sur la propriété du protocole — celui qui montre ses cartes
        // est celui qui regarde — quand l'identifiant proposé n'est pas dans l'état.
        string localCombatantId = LocalCombatantResolver.Resolve(
            combatToken,
            Mode == CombatMode.Pvp
                ? (RunManager.Instance != null ? RunManager.Instance.pvpLocalUserId : null)
                : "player");

        if (string.IsNullOrEmpty(localCombatantId))
        {
            Debug.LogError("[STS-COMBAT] No local combatant could be identified in this state; "
                + "teams, targeting and the end-turn button will all be inert.");
        }

        IReadOnlyList<CombatantDescriptor> descriptors =
            CombatantSnapshotReader.ReadCombatants(combatToken, localCombatantId);
```

> `pvpLocalUserId` est renseigné par le menu multijoueur depuis le profil PVP
> (`MultiplayerMenuController.cs:149` et `:276`). S'il est absent — profil non chargé —
> la seconde règle du résolveur prend le relais, et c'est précisément pourquoi elle existe.

- [x] **Step 2 : `ResolveCombatantByConvention` doit connaître les UUID**

En duel, les identifiants ne suivent aucune convention positionnelle. Ajouter, **en tête**
de la méthode (ligne 2158), une résolution par identifiant d'utilisateur :

```csharp
        // En duel, l'identifiant est celui de l'utilisateur : on le retrouve sur le
        // Character que le montage de scène a étiqueté avec, plutôt que sur une position.
        foreach (Player ally in allies)
        {
            if (ally != null && string.Equals(ally.playerUserId, combatantId, StringComparison.Ordinal))
                return ally;
        }
        foreach (Character enemy in enemies)
        {
            if (enemy != null && string.Equals(enemy.playerUserId, combatantId, StringComparison.Ordinal))
                return enemy;
        }
```

> **Si les identifiants de combattant du serveur ne sont pas les identifiants
> d'utilisateur**, cette boucle ne trouvera rien et le registre restera vide — avec le
> `Debug.LogError` du Step 1 pour le dire. La tâche 15, step 2, lit la trame et tranche :
> si les identifiants sont autres, c'est **ici** que la correspondance se fait, en une
> boucle, et nulle part ailleurs. C'est la raison d'être de cette méthode : un seul
> endroit où l'on établit la correspondance, une fois, à la construction.

- [x] **Step 3 : les piles du joueur local**

Remplacer la ligne 883 :

```csharp
            if (target.isPlayer && string.Equals(combatantId, "player", StringComparison.Ordinal))
```

par :

```csharp
            // Le registre sait qui est local ; la chaîne "player" ne le savait qu'en PvE, et
            // en duel cette condition ne se vérifiait jamais — la main restait vide.
            if (combatantRegistry.IsLocalCombatant(combatantId))
```

- [x] **Step 4 : l'énergie dépensée**

Remplacer les deux lignes de `ReplayEnergySpentEvent` (ligne 1653) :

```csharp
        string combatantId = combatEvent.Value<string>("combatantId");
        if (!string.Equals(combatantId, "player", StringComparison.Ordinal) || player == null)
            return;

        player.resources.energy = combatEvent.Value<int?>("remainingEnergy") ?? player.resources.energy;
```

par une lecture adressée, symétrique de `ReplayEnergyGainedEvent` juste en dessous :

```csharp
        string combatantId = combatEvent.Value<string>("combatantId");
        Character target = ResolveCombatant(combatantId);
        if (target == null)
            return;

        target.resources.energy = combatEvent.Value<int?>("remainingEnergy") ?? target.resources.energy;
```

**Ce que ça change en PvE :** l'énergie dépensée par un ennemi est désormais appliquée à
cet ennemi, là où elle était ignorée. C'est un affichage, pas une règle, et le serveur
l'envoie déjà. Si l'affichage d'énergie d'un ennemi n'existe pas, rien ne s'affiche.

- [x] **Step 5 : le compteur de tours**

Remplacer la ligne 1112 :

```csharp
                    if (string.Equals(combatEvent.Value<string>("combatantId"), "player", StringComparison.Ordinal))
```

par :

```csharp
                    if (combatantRegistry.IsLocalCombatant(combatEvent.Value<string>("combatantId")))
```

- [x] **Step 6 : le bouton de fin de tour**

Remplacer le bloc de la ligne 895 :

```csharp
            Character activeCombatant = ResolveCombatant(activeCombatantId);
            turnSystem.endTurnButton.interactable = activeCombatant != null
                && activeCombatant.isPlayer
                && !combatEnded;
```

par :

```csharp
            // On ne finit que son propre tour. « N'importe quel combattant du côté joueur »
            // marchait tant que le seul combattant humain était nous ; en duel, l'adversaire
            // est un humain lui aussi, et en co-op ce serait le tour d'un allié.
            // Le repli sur isPlayer couvre le tutoriel, où le registre est vide.
            Character activeCombatant = ResolveCombatant(activeCombatantId);
            bool ours = combatantRegistry.LocalCombatantId != null
                ? combatantRegistry.IsLocalCombatant(activeCombatantId)
                : activeCombatant != null && activeCombatant.isPlayer;
            turnSystem.endTurnButton.interactable = ours && !combatEnded;
```

- [x] **Step 7 : vérifier qu'il n'en reste aucun**

```bash
grep -n '"player"' Assets/Scripts/Scene/STS/Combat/CombatManager.cs
```

Attendu : **une seule ligne**, celle de `ResolveCombatantByConvention` (ligne ~2163), qui
est la convention PvE elle-même et doit rester.

- [x] **Step 8 : compiler.** Suite EditMode : **109 tests, 0 échec**.

- [x] **Step 9 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs
git commit -m "fix(sts): ask the registry who we are instead of spelling it 'player'

The pile gate was the one that mattered: in a duel the local id is a user's
UUID, the condition never held, and the player's hand was never seeded at all.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 11 : Les refus du serveur, montrés

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
- Modify: `Assets/Scripts/Scene/STS/UI/UIManager.cs`

- [x] **Step 1 : un endroit où écrire un avis, dans `UIManager`**

À côté des autres champs sérialisés (vers la ligne 36) :

```csharp
    [Header("Combat distant")]
    // Laissés vides tant qu'un humain ne les a pas branchés dans la scène : tout ce qui
    // les lit est null-safe, donc le PvE ne voit rien changer.
    public TextMeshProUGUI combatNoticeText;
    public TextMeshProUGUI turnCountdownText;
```

et la méthode :

```csharp
    Coroutine combatNoticeRoutine;

    /// <summary>
    /// Une phrase brève, au milieu de l'écran, qui s'efface seule. Le serveur refuse une
    /// commande et le client n'en montrait rien : la carte ne bougeait pas, sans un mot.
    /// </summary>
    public void ShowCombatNotice(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (combatNoticeText == null)
        {
            Debug.LogWarning($"[STS-COMBAT] {message} (no notice field wired in the scene)");
            return;
        }

        combatNoticeText.text = message;
        combatNoticeText.gameObject.SetActive(true);

        if (combatNoticeRoutine != null)
            StopCoroutine(combatNoticeRoutine);
        combatNoticeRoutine = StartCoroutine(HideCombatNoticeRoutine());
    }

    IEnumerator HideCombatNoticeRoutine()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        if (combatNoticeText != null)
            combatNoticeText.gameObject.SetActive(false);
        combatNoticeRoutine = null;
    }
```

- [x] **Step 2 : traiter le message dans la file**

Dans `ProcessAuthoritativeMessageQueue` (ligne 640), après la branche `STATE_UPDATED` :

```csharp
            else if (type == "COMMAND_REJECTED")
            {
                HandleCommandRejected(message["payload"]);
            }
```

et la méthode :

```csharp
    /// <summary>
    /// Le serveur a refusé une commande, dans le vocabulaire de son moteur.
    ///
    /// <para>Le refus était déjà réglé un cran plus bas — le noyau du pont libère la
    /// commande en attente, donc rien ne se bloquait — mais il n'atteignait jamais
    /// l'écran. Une carte refusée se contentait de ne pas bouger, et le joueur
    /// recommençait.</para>
    /// </summary>
    void HandleCommandRejected(JToken payload)
    {
        string code = payload?.Value<string>("code");
        string serverMessage = payload?.Value<string>("message");
        Debug.LogWarning($"[STS-COMBAT] Command rejected code={code ?? "<none>"} message={serverMessage ?? "<none>"}");

        if (ui == null)
            return;

        if (CombatRejectionMessages.WarrantsEnergyGlow(code))
            ui.StartCoroutine(ui.EnergyTextGlowRed());

        ui.ShowCombatNotice(CombatRejectionMessages.ForCode(code));
    }
```

> **On ne resynchronise pas ici**, volontairement. Le chemin PvE resynchronise déjà sur
> l'issue de la commande (`needsResync`), et un duel se resynchronise par la couche React.
> Ajouter une troisième resynchronisation ferait trois codes pour un même événement.

- [x] **Step 3 : compiler.** Suite EditMode : **109 tests, 0 échec**.

- [x] **Step 4 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs \
        Assets/Scripts/Scene/STS/UI/UIManager.cs
git commit -m "feat(sts): show the player why the server refused

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 12 : Le compte à rebours à l'écran

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
- Modify: `Assets/Scripts/Scene/STS/Combat/TurnSystem.cs`
- Modify: `Assets/Scripts/Scene/STS/UI/UIManager.cs`

- [x] **Step 1 : retenir la deadline à chaque état**

Dans `CombatManager`, à côté de `authoritativeCombatState` :

```csharp
    private TurnCountdown turnCountdown = TurnCountdown.None;
```

Dans `ApplyAuthoritativeCombatState`, juste après `authoritativeCombatState = combatToken;` :

```csharp
        // Absents en PvE, où le tour n'expire pas : FromState rend alors None et rien ne
        // s'affiche.
        turnCountdown = TurnCountdown.FromState(
            combatToken.Value<string>("turnDeadline"),
            combatToken.Value<string>("serverTime"),
            DateTimeOffset.UtcNow);
```

et l'accès que l'interface lira :

```csharp
    /// Les secondes restantes au tour en cours, ou null quand ce combat n'a pas de
    /// limite de temps. Nul aussi une fois le combat terminé : un compte à rebours qui
    /// continue de tourner sur un combat fini est un mensonge.
    public double? SecondsLeftInTurn()
    {
        if (combatEnded || !turnCountdown.HasDeadline)
            return null;

        return turnCountdown.SecondsRemainingAt(DateTimeOffset.UtcNow);
    }
```

`using System;` est déjà en tête de `CombatManager.cs` (ligne 3).

- [x] **Step 2 : le faire battre**

`TurnSystem.Update` est le seul `Update` qui tourne en permanence dans la scène de combat
(constat 8). Remplacer son début (ligne 22) :

```csharp
    void Update()
    {
        if (combat == null || combat.combatEnded || !combat.allowTurn)
            return;
```

par :

```csharp
    void Update()
    {
        if (combat == null)
            return;

        // Ici, et pas dans CombatManager : son propre Update est entièrement encadré par
        // #if UNITY_EDITOR et n'existe pas dans un build. Et avant la sortie ci-dessous,
        // qui rend la main dès que le combat est autoritatif — c'est-à-dire dans tous les
        // combats qui ont une limite de temps.
        ui?.DisplayTurnCountdown(combat.SecondsLeftInTurn());

        if (combat.combatEnded || !combat.allowTurn)
            return;
```

- [x] **Step 3 : l'afficher**

Dans `UIManager`, à côté de `ShowCombatNotice` :

```csharp
    /// <summary>
    /// Les secondes qu'il reste au tour, ou rien. Le champ n'est pas branché tant qu'un
    /// humain ne l'a pas posé dans la scène, auquel cas cette méthode ne fait rien : le
    /// PvE, qui n'a pas de limite de temps, ne doit de toute façon rien voir.
    /// </summary>
    public void DisplayTurnCountdown(double? secondsRemaining)
    {
        if (turnCountdownText == null)
            return;

        if (secondsRemaining == null)
        {
            if (turnCountdownText.gameObject.activeSelf)
                turnCountdownText.gameObject.SetActive(false);
            return;
        }

        int whole = Mathf.Max(0, Mathf.CeilToInt((float)secondsRemaining.Value));
        if (!turnCountdownText.gameObject.activeSelf)
            turnCountdownText.gameObject.SetActive(true);

        turnCountdownText.text = $"{whole}s";
        turnCountdownText.color = whole <= 5 ? Color.red : Color.white;
    }
```

- [x] **Step 4 : compiler.** Suite EditMode : **109 tests, 0 échec**. Le champ étant
      vide, **rien ne s'affiche encore** : c'est la tâche 15, step 6, qui le branche.

- [x] **Step 5 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs \
        Assets/Scripts/Scene/STS/Combat/TurnSystem.cs \
        Assets/Scripts/Scene/STS/UI/UIManager.cs
git commit -m "feat(sts): count the turn down where the player can see it

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 13 : L'adversaire à l'écran — ses piles en compteurs

**But :** le §4.7 le nomme comme le reste à faire côté affichage. `RemotePiles` porte
`drawCount` et `handCount` depuis le plan 2 et personne ne les lit.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
- Modify: `Assets/Scripts/Scene/STS/UI/CharacterUI.cs`

- [x] **Step 1 : la question, dans `CombatManager`**

```csharp
    /// <summary>
    /// « Main 3 · Pioche 12 » pour un combattant dont on n'a pas le droit de voir les
    /// cartes ; null pour tout autre — un ennemi PvE n'a aucune pile, et le joueur local
    /// montre sa vraie main.
    /// </summary>
    public string RemotePilesSummary(Character character)
    {
        ICombatantPiles<CardInstance> piles =
            combatantPiles.For(combatantRegistry.IdOf(character));
        if (piles == null || piles.IsFullyVisible)
            return null;

        return $"Main {piles.Count(PileKind.Hand)}  ·  Pioche {piles.Count(PileKind.Draw)}";
    }
```

- [x] **Step 2 : l'afficher là où l'intention s'affichait**

Dans `CharacterUI.Refresh`, remplacer le bloc de la ligne 110 :

```csharp
        if (!character.isPlayer)
        {
            // Refresh the enemy's intent
            RefreshIntent(character as Enemy);
        }
```

par :

```csharp
        if (!character.isPlayer)
        {
            // Un combattant humain n'a pas d'intention à montrer — c'est l'autre joueur qui
            // décide — mais il a des piles dont on connaît la taille sans en connaître le
            // contenu. On met les compteurs là où l'intention se serait affichée.
            string remotePiles = uiManager != null && uiManager.combat != null
                ? uiManager.combat.RemotePilesSummary(character)
                : null;

            if (remotePiles != null)
            {
                intentText.text = remotePiles;
                return;
            }

            RefreshIntent(character as Enemy);
        }
```

> **Ce choix n'ajoute aucun champ à aucun prefab**, donc il compile et s'affiche sans
> qu'un humain ouvre l'éditeur. Voir la décision **D2** pour l'alternative.
> **Attention au `return`** : dans le code actuel ce bloc est la dernière chose que
> `Refresh` fait ; vérifier qu'il l'est toujours avant de sortir ainsi, et sinon extraire
> la suite plutôt que de la sauter.

- [x] **Step 3 : compiler.** Suite EditMode : **109 tests, 0 échec**.

- [x] **Step 4 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs \
        Assets/Scripts/Scene/STS/UI/CharacterUI.cs
git commit -m "feat(sts): show how many cards the opponent holds, not which

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 14 : La fin d'un duel

**But :** empêcher la fin d'un duel de tomber dans la boucle de récompenses PvE
(constat 7), et donner une sortie au joueur.

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
- Modify: `Assets/Scripts/Scene/STS/UI/UIManager.cs`

- [x] **Step 1 : brancher avant tout le reste dans `EndCombat`**

Juste après la boucle d'attente des animations (ligne 2822), **avant**
`if (outcome == TeamOutcome.Victory)` :

```csharp
        // Un duel n'est pas un nœud de carte. Sans cette sortie, une victoire PvP
        // déclencherait les hooks de reliques, marquerait le nœud courant terminé,
        // composerait une récompense depuis l'étage et l'acte de la run, appellerait
        // CompleteNode et chargerait STS_Reward. SubmitCombatResultAsync sort bien sans
        // rien faire quand il n'y a pas de run — mais un joueur qui a mis une run en pause
        // pour jouer un duel a toujours son runId et son activeEncounter, et gagnerait donc
        // un nœud de sa run en gagnant son duel.
        if (Mode == CombatMode.Pvp)
        {
            yield return EndPvpBattleRoutine();
            yield break;
        }
```

- [x] **Step 2 : écrire la sortie**

```csharp
    /// <summary>
    /// Referme un duel : on coupe le transport, on montre l'issue, on efface la session,
    /// et on revient au menu multijoueur. Aucune complétion de nœud, aucune récompense,
    /// aucun déverrouillage de fin de run.
    /// </summary>
    IEnumerator EndPvpBattleRoutine()
    {
        string opponentName = OpponentDisplayName();
        Debug.Log($"[STS-PVP] Battle over: outcome={outcome} opponent={opponentName ?? "<unknown>"}");

        ReactCombatBridge.Disconnect();

        if (ui != null)
            ui.ShowPvpResult(outcome, opponentName);

        yield return new WaitForSecondsRealtime(4f);

        RunManager.Instance?.EndPvpBattle();
        STSSceneLoader.Instance?.LoadScene("STS_MultiplayerMenu");
    }

    /// Le nom sous lequel l'adversaire s'est présenté, à défaut le nom de son personnage.
    string OpponentDisplayName()
    {
        foreach (Character opponent in combatantRegistry.Opponents())
        {
            if (opponent == null)
                continue;

            return !string.IsNullOrWhiteSpace(opponent.playerDisplayName)
                ? opponent.playerDisplayName
                : opponent.name;
        }
        return null;
    }
```

> `ReactCombatBridge.Disconnect()` sort de lui-même si aucune socket n'est ouverte
> (`ReactCombatBridge.cs:59`) et son appel natif est encadré par `#if UNITY_WEBGL` : sûr
> hors WebGL.

- [x] **Step 3 : montrer l'issue**

Dans `UIManager` :

```csharp
    /// <summary>
    /// L'issue d'un duel.
    ///
    /// <para>Elle ne passe pas par GameOverController : celui-ci écrit « Vous avez été
    /// vaincu par … » — faux sur une victoire — et son bouton met fin à la run
    /// (GrantRunEndUnlocks, OnRunEnd), ce qu'un duel n'a aucun droit de faire.</para>
    ///
    /// <para>Tant qu'aucun panneau n'est branché, on l'écrit dans l'avis de combat : le
    /// joueur voit son résultat quatre secondes avant de revenir au menu. Voir la
    /// décision D1 du plan.</para>
    /// </summary>
    public void ShowPvpResult(TeamOutcome outcome, string opponentName)
    {
        string against = string.IsNullOrWhiteSpace(opponentName) ? "" : $" contre {opponentName}";
        string message;
        switch (outcome)
        {
            case TeamOutcome.Victory: message = $"Victoire{against} !"; break;
            case TeamOutcome.Defeat:  message = $"Défaite{against}."; break;
            case TeamOutcome.Draw:    message = $"Match nul{against}."; break;
            default:                  message = "Combat terminé."; break;
        }

        Debug.Log($"[STS-PVP] {message}");
        ShowCombatNotice(message);
    }
```

`ShowCombatNotice` efface son texte au bout de 2,5 s et `EndPvpBattleRoutine` attend 4 s :
**l'avis disparaît avant le changement de scène**, ce qui est acceptable pour un
provisoire et cesse de l'être dès qu'un panneau existe (décision D1).

- [x] **Step 4 : compiler.** Suite EditMode : **109 tests, 0 échec**.

- [x] **Step 5 : commit**

```bash
git add Assets/Scripts/Scene/STS/Combat/CombatManager.cs \
        Assets/Scripts/Scene/STS/UI/UIManager.cs
git commit -m "feat(sts): end a duel as a duel, not as a run node

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 15 : Vérification en jeu réel, et l'inventaire

**Rien de ce plan n'est couvert par un test au-delà des briques pures.** `CombatManager`,
`UIManager`, `GameManager`, `RunManager` et `MultiplayerMenuController` sont des
`MonoBehaviour` que la suite EditMode ne construit pas. **Cette vérification est manuelle,
et c'est elle qui autorise la fusion.**

**Elle demande un build WebGL et deux navigateurs** (deux comptes, ou une fenêtre privée) :
tout le transport est sous `#if UNITY_WEBGL && !UNITY_EDITOR`. Le mode Play ne peut rien
prouver ici.

**Files:** aucun, sauf correctif si un problème apparaît.

- [ ] **Step 1 : la non-régression PvE d'abord**

Avant tout duel. Une run PvE : un combat gagné avec ses récompenses, un combat perdu, une
rencontre à plusieurs ennemis, et le tutoriel de bout en bout. **C'est le contrôle négatif
du plan entier** : hormis l'énergie dépensée d'un ennemi (tâche 10, step 4), rien de ce
qui précède ne doit être visible en PvE. Une régression ici se cherche en priorité dans le
`mode: "PVE"` ajouté à la connexion (tâche 5, step 6).

- [ ] **Step 2 : lire une vraie trame avant de conclure quoi que ce soit**

Lancer un duel, et **dans la console du navigateur** relever, sur le premier
`COMBAT_SNAPSHOT` :

1. chaque combattant porte-t-il un `teamId` ? **Si non, le registre reste vide et rien ne
   marche** (constat 14) — c'est le premier point de rupture à écarter ;
2. quelle forme ont les `combatantId` ? Sont-ils les `userId` que
   `RunManager.pvpLocalUserId` et `StsPvpParticipantSnapshot.userId` portent ? Si non,
   c'est le Step 2 de la tâche 10 qu'il faut corriger, et lui seul ;
3. les piles arrivent-elles bien en `piles` pour nous et `hiddenPiles` pour l'autre ?
4. `turnDeadline` et `serverTime` sont-ils présents, et dans quel format ?
5. `controllerType` est-il présent ? Le §6.5 de l'étude disait que non ; s'il manque,
   `CombatantSnapshotReader` classe les deux joueurs en `Ai`, ce qui **ne gêne rien
   aujourd'hui** (aucun code ne lit `Controller`) et devra être corrigé avant le co-op.

**Consigner les réponses sous cette étape.** Elles valent plus que le reste du plan : ce
sont les seuls faits de protocole qu'on n'a pas pu vérifier en l'écrivant.

- [ ] **Step 3 : un duel complet, côté second joueur**

Celui qui reçoit un `battleId` en réponse immédiate. Vérifier, dans l'ordre :

1. la scène de combat s'ouvre au lieu de rester sur la notification ;
2. la main se remplit — **c'est le test de la tâche 10, step 3** : une main vide veut dire
   que le combattant local n'est toujours pas reconnu ;
3. le bouton de fin de tour s'active à notre tour et **seulement** à notre tour ;
4. une carte jouée part, s'anime, et l'énergie descend immédiatement (tâche 10, step 4) ;
5. les cartes jouées par l'adversaire s'animent depuis son côté ;
6. les compteurs « Main n · Pioche n » de l'adversaire bougent quand il pioche.

- [ ] **Step 4 : le même duel, côté premier joueur**

Celui qui est resté en file. **Il doit entrer dans le combat sans rien cliquer** : c'est
le seul test de la veille de notifications (tâche 7, step 3). S'il n'entre pas, relire la
ligne `[STS-PVP] notifications payload:` et reprendre la tâche 7, step 4.

- [ ] **Step 5 : les refus**

Provoquer chacun de ceux qu'on peut provoquer : jouer une carte trop chère
(`INSUFFICIENT_ENERGY` — le compteur d'énergie doit rougir), jouer pendant le tour de
l'autre (`NOT_ACTOR_TURN`). Vérifier que l'avis s'affiche **si** le champ a été branché au
step 6, et sinon qu'il apparaît en avertissement dans la console.

- [ ] **Step 6 : brancher les deux champs d'interface — étape humaine, dans l'éditeur**

Ouvrir `Assets/Scenes/STS_Combat.unity` et poser deux `TextMeshProUGUI` sur le canvas de
combat, puis les assigner sur le composant `UIManager` :

- `turnCountdownText` — près du bouton de fin de tour ; c'est là que le joueur regarde
  quand il se demande combien de temps il lui reste ;
- `combatNoticeText` — au centre, au-dessus de la main.

**Aucune autre étape de ce plan ne touche à une scène ni à un prefab.** Celle-ci est
isolée exprès : elle demande l'éditeur, elle ne peut pas être vérifiée par la suite, et
tout le reste fonctionne sans elle (les deux champs sont null-safe).

- [ ] **Step 7 : la fin, dans ses trois formes**

1. **Une victoire** — l'écran de résultat, puis le retour au menu multijoueur. **Vérifier
   surtout ce qui ne doit pas arriver : pas d'écran de récompenses, pas de `STS_Reward`,
   aucun appel `CompleteNode` dans la console.**
2. **Une défaite** — même chose, sans `ShowGameOver` ni fin de run.
3. **Un abandon** — fermer l'onglet d'un des deux joueurs et laisser les tours expirer.
   Le serveur clôt le combat par un `CombatEnded` ; l'autre joueur doit voir son issue.
   C'est la seule forme de forfait disponible aujourd'hui (décision D4).

- [ ] **Step 8 : le test qui protège la run**

Le plus important, et le moins évident. **Démarrer une run PvE, entrer dans un combat, le
quitter en cours (retour au menu), jouer un duel, puis reprendre la run.** Vérifier que la
run retrouve : son deck (tâche 9, step 3), son `activeCombat` (step 2), son nœud non
complété (tâche 14, step 1), et aucun ennemi portant le pseudo de l'adversaire PvP
(tâche 6, step 4). Ce scénario est celui que toutes les gardes `Mode != CombatMode.Pvp` de
ce plan existent pour couvrir, et le seul qui les exerce.

- [x] **Step 9 : mettre à jour l'inventaire de l'étude**

Dans `docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md` :

- **§3.4 entrée 2** — trancher la question que le plan 3 laissait ouverte : le bouton de
  fin de tour reconnaît désormais *le combattant local*, pas « le côté joueur ».
- **§3.4 entrée 3** — le mode n'est plus déduit d'un effet de bord : `CombatMode` le dit.
  **Ne pas la marquer close sans nuance :** `Mode` lit encore `RunManager.activeCombat`
  pour distinguer PvE de local. Le PvP est explicite, le PvE ne l'est pas encore.
- **§5** — le corriger : il affirme que `MultiplayerMenuController` charge `STS_Boot` et
  retombe dans un combat local. C'est faux (constat 1). Récrire ce qu'il faisait
  réellement, et ce qu'il fait maintenant.
- **§6.2, §6.3, §6.4** — marquer comme livrés côté serveur.
- Ajouter, sous §4.7, ce qui a été observé au Step 2 sur la forme réelle de la vue PvP.

**Ne rien supprimer** : l'inventaire est un état d'avancement.

- [ ] **Step 10 : commit**

```bash
git add docs/superpowers/specs/2026-08-23-authoritative-combat-client-generalization-design.md \
        docs/superpowers/plans/2026-08-24-unity-pvp-client.md
git commit -m "docs(sts): record what the PvP client turned out to be

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Les décisions qui reviennent à l'humain

Quatre points ne se tranchent pas depuis le code. Chacun a un défaut implémenté dans ce
plan — celui qui coûte le moins si on se trompe — et une alternative chiffrée.

> **Tranchées le 2026-08-24.** D1 → **option A**, un `PvpResultController` dédié.
> D2 → **le défaut**, les compteurs dans `intentText` ; un adversaire PvP n'a jamais
> d'intention, par construction, donc la coïncidence redoutée ne peut pas se produire.
> D3 → **l'alternative**, on sort le joueur du duel en le lui disant. D4 → **hors
> périmètre, et pas bloquée sur le client** : `StsPvpBattleSettlement.conceded` n'est
> appelée que par `StsPvpBattleTimeoutScheduler`, donc le forfait n'existe que comme
> conséquence d'un dépassement de temps — il n'y a ni endpoint ni commande à appeler.
> `ReactCombatBridgeCore.CommandTypes` et
> `ReactCombatBridgeTests.UnsupportedBackendCommandsAreRejectedLocally` restent
> **exactement** en l'état.

### D1 — Que propose l'écran de fin d'un duel ?

**Implémenté :** une phrase (« Victoire contre Bob ! ») puis un retour automatique au menu
multijoueur au bout de quatre secondes. Coût si c'est faux : le joueur n'a aucune prise —
pas de revanche, pas de récapitulatif, et l'avis s'efface avant le changement de scène
(l'avis dure 2,5 s, l'attente 4 s).

**Options :**
- **A. Un panneau dédié dans `STS_Combat`** — un `PvpResultController` sur le modèle de
  `GameOverController`, avec « Revanche » et « Retour au menu ». Coût : une édition de
  scène et ~40 lignes. C'est ce qu'un joueur attend d'un mode compétitif.
- **B. Retour immédiat au menu, avec la notification du menu** — `ShowNotification` y
  existe déjà (`MultiplayerMenuController:202`). Coût : presque nul. Le combat disparaît
  brutalement.
- **C. Réutiliser `GameOverController`** — **déconseillé, et ce n'est pas un avis** : son
  texte est écrit pour une défaite (« Vous avez été vaincu par … ») et son bouton appelle
  `GrantRunEndUnlocks(false)` puis `OnRunEnd()`, ce qui **terminerait la run PvE d'un
  joueur qui vient de perdre un duel**.

*Ce qui doit être décidé en même temps : un match nul (`winnerTeamId == null`) affiche-t-il
un écran distinct, ou se lit-il comme une défaite ? Le plan 3 avait tranché « comme une
défaite » pour le PvE, où le nul ferme la run ; en PvP le nul n'a aucune conséquence, donc
l'argument ne se transporte pas.*

**Implémenté : le nul a sa propre ligne, il ne se lit pas comme une défaite.** Pas un
écran séparé — le même panneau, avec son propre titre (« Match nul ») et sa propre
phrase. C'est la chose la plus simple qui reste vraie : le panneau branchait déjà sur
l'issue, donc un cas de plus ne coûte rien, et annoncer une défaite là où le serveur n'a
désigné aucun vainqueur serait faux. **Pour revenir dessus, il n'y a que deux endroits,
tous deux un `switch` sur `TeamOutcome`** : `PvpResultController.Show`
(`Assets/Scripts/Scene/STS/UI/PvpResultController.cs`) pour le panneau, et
`UIManager.ShowPvpResult` pour le repli en avis de combat quand le panneau n'est pas
branché.

**« Revanche » relance un matchmaking, pas un rematch contre le même joueur** : il
n'existe pas d'endpoint de revanche. Le bouton referme la session, pose
`RunManager.requestPvpQuickMatch`, et charge `STS_MultiplayerMenu`, dont l'`Awake`
consomme le drapeau et relance `QuickMatchAsync`.

### D2 — Où s'affichent les compteurs de piles de l'adversaire ?

**Implémenté :** dans `intentText`, la zone d'intention de `CharacterUI`, qu'un combattant
humain laisse vide de toute façon. Coût si c'est faux : la présentation est celle d'une
intention d'ennemi, ce qu'elle n'est pas, et elle disparaîtrait le jour où un adversaire
humain aurait une intention à montrer.

**Alternative :** deux `TextMeshProUGUI` dédiés dans `Assets/Prefabs/STS/EnemyZone.prefab`,
avec des icônes de main et de pioche. Coût : une édition de prefab, plus deux champs dans
`CharacterUI`. C'est plus lisible et ça ne dépend d'aucune coïncidence.

### D3 — Que faire quand le combattant local reste introuvable ?

**Implémenté :** un `Debug.LogError` et un combat qui continue avec un registre vide — donc
inerte : ni équipes, ni ciblage, ni bouton de fin de tour.

**Alternative :** afficher un message au joueur et le renvoyer au menu multijoueur. Plus
honnête, mais ça transforme une divergence de protocole en abandon de match, ce qui côté
serveur compte comme un forfait. À trancher une fois le Step 2 de la tâche 15 fait : si les
identifiants sont bien les `userId`, ce cas devient impossible et la question s'éteint.

**Retenu : l'alternative.** Le défaut laissait le joueur dans un combat inerte — ni
équipes, ni ciblage, ni bouton de fin de tour — sans rien à l'écran pour dire pourquoi.
`CombatManager.LeavePvpBattle` montre la phrase, coupe le transport, referme la session
et charge `STS_MultiplayerMenu` au bout de 2,5 s. **Le coût est assumé et connu : le
serveur comptera l'abandon comme un forfait** au bout des trente secondes du tour. Le
chemin PvE ne change pas — il continue de journaliser et de poursuivre, un joueur PvE
n'ayant rien à faire au menu multijoueur.

### D4 — Le forfait volontaire

**Non implémenté.** Aujourd'hui, quitter un duel se fait en fermant l'onglet et en laissant
les tours expirer (30 s chacun). Le serveur finit par clore le combat et l'adversaire voit
son issue — donc **rien n'est cassé**, mais l'expérience est mauvaise pour les deux.

Ce qu'un bouton « Abandonner » demanderait :

1. le **nom exact de la commande** côté moteur — non vérifiable depuis ce dépôt, et
   `ReactCombatBridgeCore.CommandTypes` (ligne 38) ne connaît que `PLAY_CARD` et
   `END_TURN` ;
2. **modifier un test qui épingle le refus** :
   `ReactCombatBridgeTests.UnsupportedBackendCommandsAreRejectedLocally` (ligne 77) liste
   `SURRENDER` parmi les commandes refusées localement. Ce test n'est pas un obstacle
   accidentel : il dit que le client n'invente pas de commandes. Le changer demande de
   savoir, pas de supposer.

Et une remarque du §14 de l'étude, toujours vraie : le forfait repose sur un heartbeat
estampillé uniquement par les commandes. **Un onglet mis en veille par le navigateur cesse
d'émettre et perd le match**, même si le joueur est là. Ça se corrigerait en estampillant
aussi la présence sur la socket — hors périmètre ici, mais à savoir avant d'ouvrir le mode
à des joueurs réels.

---

## Ce que ce plan ne fait pas

- **Il ne déplace pas le rejeu.** Les `Replay*Event` restent dans `CombatManager` ; le §4.4
  prévoit `AuthoritativeEventReplayer`, qui est mécanique et sans rapport avec le PvP.
- **Il ne rend pas de dos de carte.** Le §4.4 voulait qu'un `definitionId` nul produise une
  carte face cachée ; le serveur filtre désormais ces événements en amont, donc rien
  n'arrive à afficher. Les compteurs de la tâche 13 sont ce qui remplace cette idée.
- **Il ne touche pas au 2v2 ni au co-op.** Trois endroits les bloquent encore et sont
  nommés dans le corps du plan : `TargetingMode.AnyPlayer` teste `isPlayer` (constat 6),
  `LocalCombatantResolver` refuse de conclure avec deux mains visibles (tâche 2), et
  `RemotePilesSummary` suppose qu'un allié montre tout. Aucun ne coûte cher à rouvrir.
- **Il ne retire pas le moteur local** (~8600 lignes, §8) : le tutoriel en dépend, et le
  §8.4 en fait une question ouverte à part entière.
- **Il ne rend pas le PvE explicite.** `Mode` distingue le duel par une session déclarée,
  mais distingue toujours le PvE du local par la présence d'un état — l'entrée 3 de
  l'inventaire n'est donc close qu'à moitié.
- **Il ne traite pas la reconnexion** au-delà de ce que la couche React fait déjà, ni le
  spectateur, ni le rejeu de match, ni le classement.
