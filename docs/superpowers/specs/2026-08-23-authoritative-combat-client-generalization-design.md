# Généraliser le client de combat autoritatif

> Étude rédigée le 2026-08-23. Fait suite au branchement du moteur autoritatif sur
> le PvP côté serveur (`backend/docs/superpowers/plans/2026-08-22-pvp-authoritative-combat.md`)
> et au contrat client qui en découle
> (`backend/docs/superpowers/specs/2026-08-22-pvp-unity-client-contract.md`).
>
> **Statut : proposition, non implémentée.** Elle couvre trois dépôts —
> `UnityPanel/Inte-INSASTRONAUTE` (branche `experimental`), `insastral` et `backend`
> (branche `dev`).

## 1. La décision demandée

Le serveur sait déjà faire jouer un nombre quelconque de combattants répartis en
équipes, humains ou pilotés par l'IA. Le client Unity, lui, ne sait afficher qu'un
joueur contre des ennemis. Brancher le PvP 1v1 ne demande pas, en apparence, de
corriger cet écart : il se contourne, parce qu'un seul combattant y possède des piles
visibles. Tout mode ultérieur — 2v2, co-op contre un boss — le rencontre de plein
fouet.

Mais l'écart ne se contourne pas entièrement : une partie mord **déjà**, en PvE, en
jeu normal. L'identité d'un combattant y est dérivée de sa position dans une liste
locale que le client mute quand un ennemi meurt, alors que le serveur n'en retire
jamais aucun. Dans une rencontre à plusieurs ennemis, dès la première mort, l'état
atterrit sur le mauvais combattant et le ciblage désigne un mort. Le §3.3 établit le
mécanisme.

Cette étude propose donc de **rendre le client aussi général que le moteur l'est
déjà** — ce qui corrige ce défaut par construction, unifie le PvE et le multijoueur
sur un chemin unique et testé, et rend les modes suivants accessibles. Seul le 1v1
PvP est livré à l'issue ; les autres modes deviennent du paramétrage et du
matchmaking, plus de la chirurgie client.

## 2. Ce que le moteur modélise

Rien dans le moteur ne connaît la notion de « joueur » ni celle d'« ennemis ».

`CombatState` porte une liste libre de `combatants`, une `timeline`, un
`activeCombatantId` et un `winnerTeamId`. `CombatantState` porte, pour chaque
combattant, un `combatantId`, un `teamId`, un `controllerType` valant `HUMAN` ou
`AI`, ses points de vie, son armure, son énergie, ses piles, ses statuts, ses
reliques, et — pour ceux que l'IA pilote — un `intentCardId` et un `patternIndex`.
`CombatSetup` prend une liste libre de `CombatantSetup`.

Autrement dit : **1v1, 2v2, co-op contre un boss et même un affrontement à N équipes
sont déjà exprimables côté serveur sans toucher au moteur.** L'asymétrie
joueur/ennemis n'existe que dans Unity.

Deux limites existent néanmoins côté serveur, examinées en §6 : la projection PvP
(`StsPvpCombatView`) laisse tomber `controllerType` et les champs d'intention, et
`StsPvpCombatSetupFactory` est figée à deux participants.

## 3. Ce que le client suppose

Le client autoritatif suppose, partout, qu'il n'existe qu'un seul combattant
possédant des cartes, et que c'est le joueur local. Ce n'est pas une négligence
ponctuelle : c'est le modèle.

Les preuves, dans l'ordre de gravité, sur `Assets/Scripts/Scene/STS/Combat/CombatManager.cs`
après le pull du 2026-08-23 :

- **`public DeckManager deck;` (ligne 71)** — un seul jeu de piles pour tout le combat.
  `GetPileByName` (ligne 1907) ne sait résoudre `HAND`, `DRAW`, `DISCARD` et `EXHAUST`
  que sur celui-là, et ne prend aucun identifiant de combattant en argument.
- **`ReplayCardDrawnEvent` (ligne 1246) ne lit jamais `combatantId`.** Il retire la
  carte de `deck.drawPile` et l'insère dans `deck.hand`, quel que soit le combattant
  que l'événement nomme. En co-op, la pioche d'un allié atterrirait dans ma main.
- **`ApplyAuthoritativePlayerPiles` n'est appelé que sous `if (target.isPlayer)`**
  (ligne 852). Avec deux combattants alliés, le second écraserait les piles du premier.
- **`player => allies.FirstOrDefault()` (ligne 66)** — un seul joueur, par construction.
- **`GetAuthoritativeCombatantId` (ligne 788)** traduit en `"player"` ou
  `"enemy-{index}"`. Les identifiants PvP sont des UUID d'utilisateur et les équipes
  valent `team-0` / `team-1`.
- **`endTurnButton.interactable` compare `activeCombatantId` à `"player"`** (ligne 864).
  En PvP, le bouton de fin de tour ne s'activerait jamais.
- **`TryEndCombatIfNeeded` (ligne 2402)** déduit la victoire de
  `allies.All(a => !a.IsAlive)` et `enemies.All(e => !e.IsAlive)`, c'est-à-dire des
  points de vie locaux, et n'est gardé par aucun test de mode. C'est le client qui
  devine, là où le serveur tranche via `status` et `winnerTeamId`.
- **`DropZone.Init(CombatManager cm, Character t, bool acceptsEnemy)`**
  (`Assets/Scripts/Scene/STS/UI/DropZone.cs:60`) fige le binaire allié/ennemi jusque
  dans la couche de ciblage.

### 3.1 Pourquoi le PvP ne révèle pas le problème

En PvP 1v1, un seul combattant a des piles visibles — les miennes. L'adversaire est
réduit par `StsPvpCombatView` à des compteurs (`drawCount`, `handCount`) plus sa
défausse et son exil, qui sont publics. Le `deck` global colle donc par accident.

On brancherait le PvP sans jamais rencontrer l'écart, et on le découvrirait entier au
premier co-op, dans une couche qu'on aurait entre-temps arrêté de connaître. C'est le
cœur de l'argument : **le coût de ce report n'est pas linéaire.**

### 3.2 Ce que la couche coûte déjà

Anatomie de `CombatManager.cs`, 2730 lignes au 2026-08-23 :

| Bloc | Lignes | Taille | Touché par la généralisation |
|---|---|---|---|
| Champs, `Update` | 1–118 | ~118 | partiellement |
| `Init`, bootstrap, connexion socket, `EnsureAllies`/`EnsureEncounterEnemies` | 119–364 | ~245 | oui |
| `PlayCard`, commandes autoritatives (WebGL + repli HTTP), sélection de cartes, file de messages | 365–787 | ~423 | oui |
| Identité combattant ↔ `Character` | 788–799 | ~12 | **oui** |
| Application d'état + timeline | 800–1003 | ~204 | **oui** |
| Rejeu d'événements | 1004–1616 | ~613 | **oui** |
| Helpers JSON, statuts, `ResolveCombatant`, piles joueur | 1617–2018 | ~402 | **oui** |
| Simulation locale (`PlayCardRoutine`, `FollowUpCard`) | 2019–2352 | ~334 | **non** |
| Nettoyage des morts | 2353–2448 | ~96 | partiellement |
| Ciblage | 2449–2527 | ~79 | **oui** |
| Fin de combat + progression PvE | 2528–2730 | ~203 | oui |

La couche autoritative — application d'état, rejeu, helpers, identité — représente
**environ 1230 lignes**, soit 45 % du fichier.

Un fait daté vaut mieux qu'un argument : le pull du 2026-08-23 (commits `e65621f`
à `482a952`) a ajouté **environ 200 lignes dans cette couche précise** —
`ApplyTurnEndedToTimeline`, `RefreshTimelineDisplay`, `ReplayArmorGainedEvent`,
`PlayStatusChangeFeedback`. Le fichier est passé de 2576 à 2730 lignes en une
journée. La couche à généraliser grossit plus vite que le reste.

### 3.3 Un défaut PvE déjà actif

Ce qui précède se lit comme une assurance contre des modes futurs. Ça n'en est pas
une : **l'identité positionnelle produit déjà un bug en PvE, atteignable en jeu
normal.**

Le mécanisme :

- Le moteur ne retire jamais un combattant mort de `state.combatants()`. Partout il
  filtre sur `hp() > 0` — `CombatEngine`, `EffectResolver`, `CombatOrchestrator` — sans
  jamais supprimer. Les identifiants `enemy-{index}` sont attribués une fois au setup
  (`StsAuthoritativeCombatService.enemySetups`) et ne bougent plus.
- Unity retire le mort de sa liste : `enemies.Remove(enemy)` dans
  `CleanupSlainCharactersRoutine` (ligne 2359), **y compris en mode autoritatif**. Et
  cela arrive dès la première mort, pas à la fin du combat : `TryEndCombatIfNeeded`
  se déclenche sur `hasDeadCharacters`, lance `ResolveCombatEndRoutine`, qui nettoie
  puis ressort si le combat n'est pas terminé.
- `ResolveCombatant("enemy-N")` renvoie `enemies[N]` par position (ligne 1941), et
  `GetAuthoritativeCombatantId` renvoie `enemy-{enemies.IndexOf(character)}`, par
  position également (ligne 796).

Dans une rencontre à trois ennemis, dès que `enemy-0` meurt, la liste locale devient
`[enemy-1, enemy-2]`. À partir de là, l'état destiné à `enemy-1` est appliqué sur
`enemies[1]`, c'est-à-dire sur le combattant que le serveur appelle `enemy-2` : **les
points de vie et les statuts atterrissent sur le mauvais ennemi.** Symétriquement, une
carte visant le deuxième ennemi à l'écran part étiquetée `enemy-0`, un combattant mort
que le moteur refuse comme cible (`CombatEngine`, ligne 775, `target.hp() > 0`).

Les rencontres concernées existent dans les données livrées :
`Assets/StreamingAssets/EnemyPool/EnemyPool.json` contient notamment
`['Enemy_1','Enemy_2','Enemy_3']`, `['Enemy_1','Enemy_2']` et `['Enemy_1','Enemy_1']`.

Réserve de méthode : ceci est établi par lecture du code, pas par exécution. Le
symptôme visible dépend de l'ordre des morts, et le `ui.InitCharacters()` déclenché
par le nettoyage peut en masquer une partie. La première tâche d'implémentation
devrait être un test EditMode qui reproduit la séquence — il doit échouer sur le code
actuel.

La cause racine est exactement ce que `CombatantRegistry` remplace : **une identité
serveur dérivée d'une position dans une liste locale que le client mute.**

### 3.4 L'inventaire du bricolage à retirer

L'objectif déclaré de ce chantier est d'arrêter de bricoler. Un objectif nommé se
vérifie ; une aspiration non. Voici donc la liste exacte, chaque entrée vérifiée dans
le code au 2026-08-23. Elle sert de critère d'acceptation : le chantier est réussi
quand elle est vide.

1. ~~**Identité dérivée d'une position mutable**~~ — `enemy-{enemies.IndexOf(character)}`
   (ligne 796) et `enemies[N]` (ligne 1941). Cause du défaut §3.3.
   **Traitée** par le plan `2026-08-23-combatant-identity-seam` (tâche 4) : les deux
   dérivations lisent le registre. La convention positionnelle ne subsiste que dans
   `ResolveCombatantByConvention`, appelée une fois à la construction du registre,
   avant qu'aucune mort n'ait pu déplacer quoi que ce soit.
   *Le merge `8f17676` avait entre-temps étendu le même schéma au côté allié —
   `allies.IndexOf` → `player-{index}` et `allies[N]` — donc le défaut existait en
   double au moment de le retirer. `CombatantIdentityTests` en garde la trace.*
2. ~~**Identité par chaîne littérale**~~ — `activeCombatantId == "player"` (ligne 864)
   pour décider si le bouton de fin de tour est actif.
   **Traitée**, mais pas par nous : le merge `8f17676` a remplacé la comparaison de
   chaîne par `ResolveCombatant(activeCombatantId)?.isPlayer`. Cette ligne s'est donc
   corrigée d'elle-même quand `ResolveCombatant` est passée par le registre. La
   sémantique retenue est celle du merge — « tout combattant du côté joueur » — et
   non `IsLocalCombatant` ; c'est au plan PvP de trancher entre les deux, puisque
   c'est là que la différence devient observable.
   **Tranché le 2026-08-24** par `2026-08-24-unity-pvp-client` (tâche 10, step 6) :
   c'est `IsLocalCombatant` qui décide. « Tout combattant du côté joueur » ne coûtait
   rien tant que le seul humain de la scène était nous ; en duel, l'adversaire en est
   un aussi. Le repli sur `isPlayer` subsiste pour le seul cas où le registre est vide
   — le tutoriel, qui n'a pas de serveur pour le remplir.
11. ~~**Propriété des piles décidée par une chaîne littérale**~~ — `target.isPlayer &&
    combatantId == "player"` (ligne 858), introduit par le merge `8f17676` pour que
    les alliés supplémentaires n'écrasent pas le deck local.
    **Traitée** par le plan `2026-08-23-combatant-addressed-piles` : le registre de
    piles répond à la question, et `LocalPiles` / `RemotePiles` disent ce qu'un
    combattant donné laisse voir.
3. **Mode déduit d'un effet de bord** — `UsesAuthoritativeCombat` vaut
   `RunManager.Instance.activeCombat != null` (ligne 115), champ écrit par
   `ApplyAuthoritativeCombatState` (ligne 805). Le client est en mode autoritatif
   *parce qu'un état est arrivé*, et non parce qu'on le lui a dit.
   **Close à moitié, et pas davantage**, par `2026-08-24-unity-pvp-client` (tâches 1 et
   9) : `CombatMode` nomme le mode, `CombatManager.Mode` le rend, et
   `UsesAuthoritativeCombat` n'est plus qu'un `Mode != Local`. Le duel, lui, est
   explicite : il se reconnaît à la session que le menu multijoueur ouvre
   (`RunManager.activePvpBattleId`), donc **avant** d'avoir reçu le moindre état — ce
   qui est précisément la propriété qui manquait, puisque sans elle la première carte
   d'un duel serait partie dans le moteur local. **Mais `Mode` distingue toujours le PvE
   du local par la présence d'un état** : cette moitié-là reste déduite, et l'entrée
   reste donc ouverte.
4. ~~**Deux sources de vérité sur l'issue**~~ — `TryEndCombatIfNeeded` déduisait la
   victoire des PV locaux, alors que `SubmitCombatResultAsync` envoie déjà
   `result = null` en mode autoritatif parce que le serveur tranche depuis son état.
   **Traitée** par le plan `2026-08-24-combat-outcome-and-team-targeting` (tâches 2 et
   3) : `CombatOutcomeSource` lit le `winnerTeamId` du `CombatEnded`, et la dérivation
   locale ne subsiste que pour le tutoriel, qui n'a pas de serveur pour trancher.
   *Deux choses n'étaient pas dans le plan et ont été trouvées en l'exécutant. Le match
   nul n'avait pas de valeur dans `TeamOutcome`, donc il s'affichait en victoire — les
   deux équipes anéanties donnant « tous les ennemis sont morts ». Et le plan ne
   nommait que la sortie anticipée interne : `TryEndCombatIfNeeded` reposait la même
   question un cran plus haut et refusait de lancer la routine tant qu'aucun mort
   n'était visible, si bien que l'issue annoncée était enregistrée puis jamais
   appliquée.*
5. **Une classe qui se désactive de l'intérieur** — `DeckManager` appelle
   `ShouldBypassLocalDeckMutations()` dans **onze** de ses méthodes. Ce sont deux
   implémentations déguisées en onze gardes.
6. ~~**Piles non adressées**~~ — `GetPileByName(pileName)` ne prenait aucun combattant.
   **Traitée** par le plan `2026-08-23-combatant-addressed-piles` : la méthode prend un
   `combatantId` et passe par `CombatantPilesRegistry`.
7. ~~**Binaire allié/ennemi jusque dans l'interface**~~ — `DropZone.Init(CombatManager,
   Character, bool acceptsEnemy)`.
   **Traitée** par le plan `2026-08-24-combat-outcome-and-team-targeting` (tâche 7) :
   la zone reçoit l'hostilité calculée par équipe, et le champ sérialisé garde son
   ancien nom via `[FormerlySerializedAs]` — les prefabs y sont liés.
8. ~~**Tolérance à plusieurs orthographes du protocole**~~ — `fromPile ?? sourcePile`,
   `toPile ?? destinationPile`, `statusType ?? status ?? statusName`, `cardId ??
   cardID`. Le client devinait le contrat du serveur au lieu de le connaître.
   **Traitée** par le plan `2026-08-23-combatant-addressed-piles` (tâche 7) : le
   vocabulaire des piles est clos (`PileKinds.Parse`), et les champs sont lus sous le
   seul nom que le serveur émet. *Les `status.cardID` qui subsistent dans
   `CombatManager` sont un champ C# de `StatusEffect`, pas une orthographe de
   protocole.*
9. **Repli silencieux sur une valeur plausible** — **à moitié traitée.**
   `GetPileByName(pileName) ?? deck?.drawPile` (pile inconnue, on écrit dans la
   pioche) a été retirée par le plan `2026-08-23-combatant-addressed-piles` : une pile
   hors vocabulaire ne joint plus aucune branche.
   **Reste :** `ReactCombatBridge.CurrentRevision ?? GetAuthoritativeRevision()`
   (`CombatManager.cs`, lignes 446 et 709), qui retombe sur `0` en l'absence d'état —
   or `0` est une révision canonique valide, donc l'absence de valeur devient une
   valeur acceptable au lieu d'un refus d'émettre. **Cette entrée reste ouverte.**
10. **Combattants inventés** — `CreateFallbackIroncladEnemy()` (ligne 326) et
    `new Player("Player", 100)` (ligne 286), avec les journaux « spawning a fallback
    Ironclad enemy so combat can continue » et « creating a fallback player to keep
    turn flow valid ». En mode autoritatif, un état incompréhensible appelle une
    resynchronisation, pas l'invention d'un combattant que le serveur ne connaît pas.

Ces entrées ont un motif commun, et c'est lui qu'il faut retenir plutôt que la
liste : **le client se débrouille pour continuer là où il devrait constater qu'il ne
sait pas.** Chacune est un endroit où une information manquante ou ambiguë produit une
valeur plausible au lieu d'une erreur. C'est ce qui rend les symptômes diffus — un
mauvais ennemi touché, une carte refusée, une animation qui saute — et donc coûteux à
diagnostiquer. L'architecture modulaire n'est pas le but : elle est le moyen de rendre
ces endroits nommables, donc testables.

### 3.5 Le précédent qui valide l'approche

Trois morceaux ont déjà été sortis de `CombatManager` par le passé :
`ReactCombatBridgeCore`, `AuthoritativeCombatStateReducer` et
`AuthoritativeCombatIdentity`, regroupés dans l'assembly `STS.ReactCombatBridge`.

Ce sont exactement les seuls morceaux du chemin autoritatif qui ont des tests :
`ReactCombatBridgeTests`, `ReactCombatBridgeJslibTests`,
`AuthoritativeCardPlayPileTests` dans `Assets/Tests/EditMode`. Le reste de la couche
n'est vérifiable qu'en lançant un match. L'extraction proposée ici prolonge ce
mouvement plutôt qu'elle ne l'invente.

## 4. Le design cible

### 4.1 Principe

Sur le chemin autoritatif, Unity cesse de modéliser « un joueur contre des ennemis »
et modélise ce que le moteur modélise : **un ensemble de combattants, chacun avec son
équipe et son type de contrôleur, dont l'un est local.**

Ce principe vaut aussi pour le PvE, et c'est ce qui rend le design tenable : le
snapshot PvE est le `CombatState` brut stocké sur la run
(`StsRunService.authoritativeCombatSnapshot`), donc il **transporte déjà** `teamId` et
`controllerType`. Unity les ignore, c'est tout. Il n'y aura donc pas un chemin PvE et
un chemin multijoueur, mais un seul, alimenté par la même table.

### 4.2 `CombatantRegistry`

Une table `combatantId → { Character, teamId, controllerType, isLocal, piles }`,
construite depuis le snapshot à l'ouverture du combat et mise à jour à chaque état.

Elle remplace, sur le chemin autoritatif, `allies`, `enemies`, `player`, `isPlayer`,
`GetAdversaries`, `RandomEnemy` et les identifiants codés en dur de
`GetAuthoritativeCombatantId` et `ResolveCombatant`. Le PvE l'alimente avec ses
conventions actuelles (`player`, `enemy-{index}`) et ne change pas de comportement
observable ; le PvP l'alimente avec des UUID.

`endTurnButton.interactable` compare désormais `activeCombatantId` à l'identifiant du
combattant local, lu dans le registre, au lieu de la chaîne `"player"`.

### 4.3 `CombatantPiles`

L'accès aux piles devient adressé : `piles(combatantId)` au lieu de `deck`.

Deux implémentations. `LocalPiles` enveloppe le `DeckManager` existant — animations,
UI de main et abonnements `OnCardDrawn` / `OnCardDiscarded` / `OnCardExhausted` /
`OnCardAddedToHand` intacts. `RemotePiles` porte les compteurs `drawCount` et
`handCount` plus la défausse et l'exil, qui sont publics, et se rend en dos de cartes.

Cette étape est moins coûteuse qu'elle n'en a l'air, pour une raison structurelle :
`DeckManager` (`Assets/Scripts/Scene/STS/UI/DeckManager.cs`, 220 lignes) est une
classe C# ordinaire, pas un `MonoBehaviour`, et **toutes ses méthodes mutantes sont
déjà neutralisées en mode autoritatif** par `ShouldBypassLocalDeckMutations()`. En
mode autoritatif, c'est déjà un magasin passif que le rejeu écrit directement. En
instancier un par combattant est presque gratuit ; seul le combattant local reçoit le
branchement UI.

C'est la correction du défaut central : `GetPileByName` ne renverra plus « les piles »
mais « les piles de ce combattant ».

### 4.4 `AuthoritativeEventReplayer`

Les ~613 lignes de rejeu et les ~400 lignes de helpers sortent de `CombatManager`
dans une classe dédiée, sans dépendance à `MonoBehaviour` — c'est de la traduction
JSON → scène.

Le changement de fond n'est pas le déplacement, c'est le **ré-adressage** : chaque
`Replay*Event` résout sa cible par le registre et ses piles par `CombatantPiles`. Un
déplacement sans ré-adressage paierait le déménagement sans acheter la propriété qui
compte.

C'est aussi le point d'accueil du caviardage PvP : lorsque `definitionId` est nul,
le rejeu rend un dos de carte au lieu d'abandonner (aujourd'hui, `card == null`
provoque un `yield break` silencieux).

### 4.5 `CombatOutcomeSource`

« Qui a gagné » devient une entrée explicite plutôt qu'une déduction. Sur le chemin
autoritatif, c'est `winnerTeamId` comparé au `teamId` du combattant local. Le PvE
hors-ligne conserve sa dérivation actuelle sur les points de vie.

Sans cela, en PvP, le client continuerait de deviner l'issue à partir de PV qu'il ne
possède pas — et le cas du forfait, où le serveur clôt le combat sans qu'aucun
combattant ne soit forcément mort, n'a aucune traduction locale correcte.

### 4.6 Ciblage par équipe

`GetDisplayTargets`, `AutoCardTargets` et `GetAdversaries` filtrent sur
`teamId != le mien` au lieu de parcourir la liste `enemies` ; `DropZone.Init` reçoit
l'appartenance d'équipe plutôt qu'un booléen `acceptsEnemy`.

C'est ce qui rend le 2v2 possible : soigner un allié et frapper un adverse deviennent
la même opération, paramétrée par l'équipe.

### 4.7 Interface

`UIManager` place déjà N combattants par côté : un `playerRoot` avec une liste
`playerZones`, un `enemyRoot` avec `enemyZones`, un compteur d'index, un
`Instantiate(prefab, root)` quand la liste est trop courte, la désactivation des zones
inutilisées et un `LayoutRebuilder` sur les deux racines
(`Assets/Scripts/Scene/STS/UI/UIManager.cs`, lignes 255–300). Les rencontres PvE
affichent déjà plusieurs ennemis.

La seule chose qui figeait un allié unique était `if (combat.player != null)`, à
remplacer par une boucle. **Ce n'est plus vrai au 2026-08-24** : `UIManager` boucle
déjà sur `combat.allies`, exactement comme sur `combat.enemies`. Corrigé hors de ce
chantier, entre la rédaction de cette étude et son exécution. **Le 2v2 n'est donc pas
une reconstruction de scène**, et il l'est encore moins qu'écrit ici. Par ailleurs
`Character`, `Player` et `Enemy` sont des objets C# ordinaires (`new Enemy(enemyId)`),
non couplés à un prefab : le modèle se généralise sans toucher aux assets.

~~Reste à ajouter, côté affichage : les compteurs `drawCount` / `handCount` d'un
combattant distant, et l'absence d'intention pour un combattant `HUMAN`.~~
**Fait** par `2026-08-24-unity-pvp-client` (tâche 13) : `CombatManager.RemotePilesSummary`
rend « Main n · Pioche n » pour tout combattant dont les piles ne sont pas entièrement
visibles, et `CharacterUI.Refresh` l'écrit dans `intentText` — la zone d'intention, que
`Enemy.PeekNextAction` laisse déjà vide quand il n'y a pas d'`EnemyData`, ce qui est le
cas d'un adversaire humain. Le choix de cette zone est la **décision D2** du plan :
aucun champ de prefab n'est ajouté. Les compteurs ne s'écrivent que là où l'intention
est restée vide, pour qu'un ennemi PvE qui arriverait un jour avec des piles cachées
garde la sienne.

Restent ouverts pour l'affichage : les deux `TextMeshProUGUI` de `UIManager`
(`combatNoticeText`, `turnCountdownText`) sont **null tant qu'un humain ne les a pas
posés dans `Assets/Scenes/STS_Combat.unity`**, de même que `pvpResultController`. Tout
ce qui les lit est null-safe : sans eux, l'avis de refus part en avertissement dans la
console, le compte à rebours ne s'affiche pas, et la fin de duel retombe sur l'avis de
combat suivi d'un retour automatique au menu.

## 5. Bootstrap et sortie du combat PvP

> **Récrit le 2026-08-24.** Ce qui suivait était faux sur le fait : le paragraphe
> affirmait que `MultiplayerMenuController` chargeait `STS_Boot` après le matchmaking
> et retombait dans un combat local. Il ne chargeait **rien du tout**. Le seul
> `LoadScene("STS_Boot")` du fichier est celui du bouton « retour » du menu, sans
> rapport avec le matchmaking.

**Ce que le menu faisait réellement, jusqu'au 2026-08-24.** `QuickMatchAsync` lisait le
`battleId` de la réponse, mettait les participants en cache, affichait « Match PVP
trouvé ! » et **rendait la main**. Aucun combat n'était ouvert, ni local ni distant, et
aucune action PvP n'était jamais envoyée : `STSApiClient.SendPvpBattleActionAsync`
existait sans appelant — comme `ListPvpNotificationsAsync` et
`AcknowledgePvpNotificationAsync`. Le combat local avec le pseudo de l'adversaire
existait bien, mais ailleurs : `RunManager.ApplyPvpParticipantDisplayNames`, appelée
par `CombatManager.Init`, se gardait sur `pvpBattleId` — écrit au matchmaking, effacé
nulle part avant la fin de la run — et renommait donc le premier ennemi de la
**prochaine rencontre PvE**. C'était une fuite PvE, pas un chemin PvP.

**Ce qu'il fait depuis** (`2026-08-24-unity-pvp-client`, tâches 6 à 9 et 14) :

- `RunManager.activePvpBattleId` porte « le combat en cours est un duel », et lui seul :
  `BeginPvpBattle` l'ouvre, `EndPvpBattle` le referme, et
  `ApplyPvpParticipantDisplayNames` se garde désormais dessus — ce qui ferme la fuite
  ci-dessus.
- `MultiplayerMenuController.EnterPvpBattleAsync` est la porte d'entrée unique :
  participants en cache, file refermée, session ouverte, `STS_Combat` chargée. Le joueur
  qui s'inscrit **le premier** ne reçoit pas de `battleId` ; une veille
  (`WatchForMatchedBattleRoutine`) interroge `ListPvpNotificationsAsync` toutes les deux
  secondes et entre par la même porte. **La forme exacte d'une notification n'est pas
  vérifiable depuis ce dépôt** : la veille cherche le premier `battleId` présent
  n'importe où dans la réponse et journalise la trame brute
  (`[STS-PVP] notifications payload:`) pour qu'une passe suivante puisse nommer le champ.
- `GameManager.SetupGame` a une troisième branche, `SetupPvpBattle` : un `Player` et un
  `Enemy` sans `EnemyData`, un `DeckManager` vide, aucune run touchée. Tout le reste
  arrive avec le premier état autoritatif.
- `CombatManager.Mode == Pvp` est vrai **avant** le premier état.
  `BootstrapPvpBattleRoutine` se connecte sur `GetPvpTransportId(battleId)` en mode
  `"PVP"` et attend le `COMBAT_SNAPSHOT` ; **il n'y a pas de repli sur
  `StartLocalCombatFlow`**, un duel n'ayant pas de vérité locale.
- La run est protégée explicitement, et non plus par accident : l'état autoritatif est
  gardé dans un champ de `CombatManager` au lieu d'écrire `RunManager.activeCombat`, le
  deck de la run n'est plus réécrit par les piles du duel, le nœud courant n'est pas
  audité, et la resynchronisation par la route de run est refusée. **La protection de
  `SubmitCombatResultAsync` ne suffisait pas** : un joueur qui met une run en pause pour
  jouer un duel garde son `runId` et son `activeEncounter`, et gagnait donc un nœud de sa
  run en gagnant un duel.
- La sortie est `EndPvpBattleRoutine`, branchée **avant** tout le reste dans
  `EndCombat` : transport coupé, issue montrée, retour au menu multijoueur. Aucune
  complétion de nœud, aucune récompense, aucun `GrantRunEndUnlocks`. L'écran est un
  `PvpResultController` dédié — **décision D1, option A** —, modelé sur
  `GameOverController` mais jamais lui : celui-là écrit « Vous avez été vaincu par … »,
  faux sur une victoire, et son bouton termine la run.

## 6. Changements côté serveur

### 6.1 Endpoint snapshot PvP — requis

`GET /api/sts/pvp/battles/{battleId}/snapshot` renvoyant
`CombatSnapshotDto(1, battleId, revision, "PVP", vue)`.

Le pont React appelle aujourd'hui `GET /api/sts/combats/{combatId}`
(`insastral/src/lib/unityCombatBridge.ts:214`), qui est réservé au PvE et renvoie
`"PVE"` en dur (`CombatSnapshotController.java:37`). Le PvP n'expose que
`GET /api/sts/pvp/battles/{id}`, qui renvoie un `StsPvpBattleDto` — forme que
`parseCombatSnapshot` rejette, provoquant une boucle de resynchronisation.

### 6.2 La deadline dans la vue — ~~requis~~ **livré côté serveur (2026-08-24)**

> Livré et vivant sur la branche `dev` du backend. La vue porte `turnDeadline` et
> `serverTime` ; côté client, `TurnCountdown` les lit et `UIManager.DisplayTurnCountdown`
> les affiche — dès qu'un humain aura branché `turnCountdownText` dans `STS_Combat`.

`StsPvpCombatView` gagne `turnDeadline` et `serverTime`, renseignés depuis le battle.

Le tour PvP dure **30 secondes** (`StsPvpCombatService.TURN_TIMEOUT_SECONDS`), et la
deadline n'existe aujourd'hui que dans le DTO HTTP, pas dans la vue poussée sur la
socket. Sans elle, aucun compte à rebours n'est affichable et le joueur perd son tour
sans comprendre pourquoi. `serverTime` corrige le décalage d'horloge, comme le fait
déjà `StsPvpBattleDto`.

### 6.3 `COMMAND_REJECTED` — ~~requis~~ **livré côté serveur (2026-08-24)**

> Livré et vivant sur la branche `dev` du backend, avec les huit codes du moteur.
> Côté client, `CombatManager.ProcessAuthoritativeMessageQueue` traite désormais
> `COMMAND_REJECTED` et `CombatRejectionMessages` en fait une phrase montrable ; le
> message traversait la file et disparaissait.

`StsPvpCombatSocketController` laisse aujourd'hui remonter `ForbiddenException` et
`ConflictException`. Il n'existe aucun `@MessageExceptionHandler` dans le projet :
l'exception devient une trame STOMP `ERROR`, qui **ferme la connexion**. Le client la
lit comme une déconnexion et se reconnecte, là où le protocole prévoit un refus
ordinaire.

Le contrôleur doit rattraper ces deux exceptions et publier un `COMMAND_REJECTED` sur
la queue privée de l'auteur, avec son `causationActionId` — exactement la structure
déjà en place pour le PvE (`CombatCommandController.java:92`). Unity sait déjà lire
ce message et se resynchroniser.

### 6.4 `COMBAT_EVENT` projetés — ~~requis~~ **livré côté serveur (2026-08-24)**

> Livré et vivant sur la branche `dev` du backend : un `COMBAT_EVENT` par événement,
> avec un discriminant `eventType`, et les événements qui nommeraient une carte cachée
> filtrés en amont. **Conséquence pour le client :** l'idée du §4.4 d'un dos de carte
> pour un `definitionId` nul n'a plus d'objet, puisque rien n'arrive à afficher. Les
> compteurs du §4.7 sont ce qui la remplace.

`StsPvpCombatBroadcaster` n'émet que `STATE_UPDATED`. Sans les événements, les points
de vie et l'armure sautent d'un état à l'autre sans animation.

La plomberie est gratuite : `StsPvpCommandOutcome` transporte déjà
`List<CombatEvent> events` (`StsPvpCombatService.java:90`). Le coût est la projection,
car **les événements du moteur ne sont pas projetés et trois d'entre eux portent
exactement l'information que `StsPvpCombatView` s'interdit de laisser sortir.**

Il faut un `StsPvpCombatEventView.forViewer(event, viewerCombatantId)` symétrique de
la projection d'état, appliquant cette règle :

| Événement | Pour un combattant autre que le spectateur |
|---|---|
| `CardDrawn` | `cardInstanceId` et `definitionId` mis à nul ; `handIndex` conservé |
| `CardMoved` | idem dès que `fromPile` ou `toPile` vaut `DRAW` ou `HAND` |
| `CardSelectionResolved` | `selectedCardInstanceIds` vidé, seul le nombre conservé |
| `PileShuffled` | `cardInstanceIds` réduit à un compte |
| `CardAltered`, `CardEnchanted`, `CardTransformed` | `cardInstanceId` et les identifiants de définition mis à nul lorsque leur champ `pile` vaut `DRAW` ou `HAND` |
| `CardMerged` | `mergedCardInstanceIds`, `resultingCardInstanceId`, `resultingDefinitionId` et `resultingDisplayName` mis à nul |
| `CardPlayed`, `DamageApplied`, `ArmorGained`, `ArmorBroken`, `StatusApplied`, `StatusRemoved`, `StatusBlocked`, `EnergyGained`, `EnergySpent`, `HealApplied`, `HpLost`, `TurnStarted`, `TurnEnded`, `CombatEnded` | inchangés — information face visible |

`PileShuffled` est le cas critique : il transporte `List<String> cardInstanceIds`,
c'est-à-dire **l'ordre complet de la pioche**. C'est la fuite même que la projection
d'état avait été écrite pour empêcher.

Les quatre événements de mutation de carte méritent une attention particulière, parce
qu'ils se lisent comme des événements publics sans l'être. `CardAltered`,
`CardEnchanted` et `CardTransformed` portent un champ `pile` qui dit précisément où la
carte se trouve, et `CardTransformed` transporte en plus `fromDefinitionId` et
`toDefinitionId` : appliqué à une carte en main adverse, il la nomme. `CardMerged` est
le cas le plus délicat, car il porte `resultingDefinitionId` et
`resultingDisplayName` **sans champ `pile`** permettant de savoir si la fusion a eu
lieu à couvert ; faute de pouvoir le tester, il est caviardé sans condition pour tout
combattant autre que le spectateur.

La règle générale à retenir, plutôt que la table : **un événement est public s'il ne
nomme aucune carte, ou si la carte qu'il nomme est dans une pile face visible.** La
table en découle et devra être revue à chaque nouvel événement du moteur — c'est
exactement le genre de règle qu'un test doit garder, pas une relecture.

### 6.5 `controllerType` et intentions dans la vue — requis pour le co-op

`StsPvpCombatView.StsPvpCombatantView` ne porte ni `controllerType`, ni
`intentCardId`, ni `patternIndex`. Un client généralisé ne peut donc pas savoir quel
combattant affiche une intention.

Sans effet en 1v1, où les deux combattants sont `HUMAN`. Bloquant dès le premier
co-op contre un boss. Ces champs sont publics pour un combattant `AI` — le PvE les
envoie déjà.

### 6.6 Setup à N participants — requis pour 2v2 et co-op

`StsPvpCombatSetupFactory.create(combatId, dataVersion, seed, first, second)` est figée
à deux participants et deux équipes. Elle devient une liste de participants portant
chacun son index d'équipe. Changement contenu, de l'ordre de vingt lignes ;
`teamId(int teamIndex)` est déjà en place.

## 7. Changements côté pont React

Un paramètre de mode, pas un second pont.

`Insastral_CombatConnect({ combatId, mode })`, dont `unityCombatBridge` déduit les
trois chaînes qui changent : abonnement `/user/queue/sts/pvp/battles/{id}`,
publication `/app/sts/pvp/battles/{id}/commands`, snapshot
`/api/sts/pvp/battles/{id}/snapshot`. `createCombatSubscription` reçoit ces
destinations en paramètre au lieu de les construire
(`insastral/src/lib/combatSocket.ts:77` et `:118`).

`combatSync.ts`, la gestion de révision, la resynchronisation et la déduplication ne
bougent pas — c'est tout l'intérêt. `combatProtocol.ts` accepte déjà
`mode: 'PVP' | 'PVE'`.

## 8. Ce que le moteur distant rend supprimable

Le serveur applique désormais toutes les règles. Une grande partie du code de règles
côté Unity est donc devenue une seconde implémentation des mêmes lois — celle qui ne
fait autorité sur rien. Cette section en fait l'inventaire, avec une réserve
importante en §8.4.

Ce chantier **ne supprime rien** : il rend la suppression possible et chiffrable.
Mélanger généralisation et suppression rendrait toute régression indiagnosticable.

### 8.1 Ce qui duplique une règle que le serveur applique déjà

| Surface | Lignes | Statut |
|---|---|---|
| `Combat/Effects/` — résolution, modificateurs, enchantements, statuts | 6096 | duplique le moteur d'effets serveur |
| `Relics/` — hooks d'exécution des reliques | 1941 | duplique le `relicEngine` serveur |
| `CombatManager.PlayCardRoutine` + `FollowUpCard` (2019–2352) | ~334 | duplique la résolution d'une commande |
| `DeckManager` — onze méthodes mutantes déjà neutralisées par `ShouldBypassLocalDeckMutations()` | ~150 | duplique la gestion des piles |
| `TryEndCombatIfNeeded` / `ResolveCombatEndRoutine` — dérivation locale de l'issue | ~90 | duplique `status` + `winnerTeamId` |

Ordre de grandeur : **environ 8600 lignes de règles côté client, contre ~1230 lignes
de client autoritatif.** Le rapport dit à lui seul où est passé l'effort historique.

### 8.2 Ce qui doit rester, et pourquoi

Tout n'est pas supprimable, et la ligne de partage est nette : **le serveur décide,
mais le client doit encore prévoir pour afficher.**

Écrire « inflige 8 dégâts » sur une carte en main, ou afficher l'intention d'un
ennemi, suppose d'appliquer les modificateurs localement *avant* que la commande
existe. C'est un calcul, pas une résolution.

Cette séparation est déjà matérialisée dans le code, ce qui rend la coupe praticable :

- `EffectResolver.Apply` (résolution) n'est appelé que depuis `PlayCardRoutine`
  (`CombatManager` lignes 2192, 2205, 2217) et `VerifyCondition` (ligne 2183) —
  **supprimable**.
- `EffectResolver.Preview` n'est appelé que depuis `TurnSystem` (lignes 379, 393) —
  **à garder**.
- `EffectDescription.cs` (484 lignes) et `BattleCalculator.cs` (109 lignes) ne
  servent qu'à l'affichage : `CharacterUI` et les descriptions de cartes —
  **à garder**.
- Les données de cartes, statuts, reliques et ennemis — noms, icônes, descriptions,
  visuels — **à garder** intégralement ; seule leur exécution est en cause.

### 8.3 Ce qui disparaît sans remplacement

Les entrées 9 et 10 de l'inventaire du §3.4 ne sont pas des règles dupliquées mais des
échappatoires, et elles n'ont pas de successeur : `CreateFallbackIroncladEnemy()`,
le joueur de secours `new Player("Player", 100)`, le repli `?? deck?.drawPile`, et le
repli de révision sur `0`. En mode autoritatif, un état incompréhensible appelle une
resynchronisation ou une erreur, jamais une valeur inventée.

### 8.4 La réserve : le tutoriel retient le moteur local

Il serait faux de conclure que ces 8600 lignes sont mortes. Elles ont un utilisateur,
un seul, et il n'est pas évident : **le tutoriel**.

`CanBootstrapAuthoritativeCombat()` (ligne 192) exige `RunManager.Instance`, une
`activeEncounter` et un `runId`. Un tutoriel lancé hors run — le cas où
`tutorialMode` passe à `true`, ligne 141 — ne remplit aucune de ces conditions et
retombe donc sur `StartLocalCombatFlow()`. Le moteur local est ce qui fait jouer le
tutoriel.

À noter que `tutorialMode` lui-même ne pilote aucun moteur : il ne sert qu'à notifier
le tutoriel (`tutorial.NotifyCardPlayed`, `tutorial.NotifyTurnEnded`). Le couplage est
indirect, via l'absence de run — ce qui le rend d'autant plus facile à casser par
inadvertance.

Supprimer le moteur local suppose donc de trancher d'abord le sort du tutoriel. Trois
voies, à instruire séparément de ce chantier :

1. **Donner un combat serveur au tutoriel** — une run de tutoriel, jetable. La plus
   cohérente : le tutoriel enseignerait le jeu réel plutôt qu'une imitation.
2. **Scripter le tutoriel** sans résolution réelle : des états figés, joués comme une
   démonstration.
3. **Conserver un moteur local réduit** au sous-ensemble que le tutoriel utilise. La
   plus tentante et la pire : elle garde deux implémentations des mêmes règles, avec
   la seconde qui dérive silencieusement.

Tant que cette question n'est pas tranchée, l'inventaire ci-dessus reste un chiffrage,
pas un plan.

### 8.5 Ce que ce chantier ne touche pas

`PlayCardRoutine` et la simulation locale ne sont pas modifiées ici. La cohabitation
des deux moteurs subsiste donc, mais elle cesse d'être ambiguë : le chemin autoritatif
passe intégralement par le registre, et `UsesAuthoritativeCombat` — devenu explicite
plutôt que déduit d'un effet de bord (§3.4, entrée 3) — n'arbitre plus que l'entrée
dans l'un ou l'autre. C'est précisément ce qui rendra la suppression ultérieure
mécanique au lieu d'être risquée.

## 9. Périmètre

**Livré à l'issue, côté PvE :** la correction du défaut d'indexation du §3.3, une
seule source de vérité sur l'issue du combat, et une couche de rejeu enfin couverte
par des tests. Le PvE est le mode qui tourne aujourd'hui ; il n'est pas le passager de
ce chantier, il en est le premier bénéficiaire.

**Livré et jouable à l'issue, côté PvP :** le 1v1 de bout en bout — matchmaking,
combat autoritatif, refus propres, animations, compte à rebours, heartbeat, fin
décidée par le serveur, écran de résultat.

**Conçu pour, non livré :** le 2v2 et le co-op contre un boss. Les seams sont jugés
sur ces trois modes, mais seul le 1v1 est câblé, testé et livré.

**Hors périmètre :** le matchmaking à plus de deux joueurs, le règlement et le
classement d'une équipe (`StsPvpBattleSettlement` suppose deux joueurs), les
spectateurs, le rejeu de match, le chat, la reconnexion au-delà de la
resynchronisation déjà en place, et la question de game design « en co-op, un allié
voit-il ma main ». Cette dernière est absorbée par le design : c'est simplement
`LocalPiles` accordé à un second combattant, donc elle peut être tranchée plus tard
sans rouvrir le rejeu.

## 10. Ce que coûte chaque mode ensuite

**1v1 PvP** — le présent chantier.

**Co-op contre un boss** — côté serveur, `controllerType` et les champs d'intention
dans la vue (§6.5), le setup à N participants (§6.6), la politique de projection
entre alliés, et un matchmaking d'équipe. Côté client, l'affichage de plusieurs
alliés et leurs compteurs de piles. Le rejeu et le ciblage ne sont pas rouverts.

**2v2 PvP** — les mêmes changements serveur, plus le règlement et le classement à
quatre joueurs, qui est la partie réellement coûteuse et non traitée ici. Côté
client, rien de structurel au-delà du co-op.

## 11. Risques

**Le PvE passe sur le nouveau chemin — et c'est le livrable, pas le risque.** Garder
deux clients autoritatifs côte à côte serait le pire résultat possible : ils
divergeraient, et chaque correction de la couche de rejeu — celle-là même qui a reçu
200 lignes le 2026-08-23 — devrait être faite et vérifiée deux fois. Le PvE gagne un
chemin testé, une seule source de vérité sur l'issue du combat (§4.5), et la
disparition du défaut d'indexation décrit en §3.3.

Le risque réel n'est pas la bascule, c'est **l'absence de tests sur ce qu'on
déplace**. Parade — le registre est alimenté en PvE avec les conventions actuelles, de
sorte que le comportement observable reste identique ; l'extraction se fait à
comportement constant avant tout ré-adressage ; et le ré-adressage est vérifié par des
tests EditMode écrits sur la classe extraite, à commencer par celui qui reproduit le
défaut du §3.3.

**La couche bouge sous nos pieds.** Elle a reçu 200 lignes le jour même de cette
étude, depuis une autre branche de travail. Parade — séquencer l'extraction tôt et
d'un bloc plutôt que de la disperser, et se synchroniser avec `origin/experimental`
avant de commencer.

**Des correctifs encodés dans des commentaires.** La correction de cette couche tient
en partie à des bugs déjà payés, documentés en commentaire plutôt qu'en test : la
réconciliation de main qui détruisait des vues de cartes en cours d'animation
(`CombatManager.cs`, lignes 882–891), les chaînes de `COMBAT_EVENT` partageant
une même révision finale (`ReactCombatBridgeCore`). Parade — les transcrire en tests
EditMode au moment de l'extraction, ce qui est précisément l'intérêt d'extraire.

**Estimation.** Le ré-adressage du rejeu est le gros morceau, comparable au reste du
client PvP réuni. `CombatManager` devrait retomber autour de 1600 lignes.

## 12. Stratégie de test

**Backend** — projection des événements : un `CardDrawn` adverse ne nomme aucune
carte, un `PileShuffled` adverse ne porte aucun ordre, un `CardTransformed` adverse en
pile `HAND` ne nomme ni sa définition d'origine ni sa définition d'arrivée alors que le
même événement en pile `DISCARD` reste intact, un `CardMerged` adverse ne nomme rien, un
`CardPlayed` adverse est intact ; `COMMAND_REJECTED` sur révision périmée et sur tour
d'autrui ; forme du snapshot PvP ; setup à N participants.

Un test doit aussi garder la règle elle-même : **tout nouvel événement du moteur est
refusé par défaut tant qu'il n'a pas été classé public ou caviardé.** Sans ce
garde-fou, le prochain événement ajouté au moteur voyagera intact par simple oubli —
c'est ainsi que les quatre événements de mutation de carte avaient échappé à la
première rédaction de cette étude.

**React** — `insastral/tests/combat-socket.test.mjs` pour le routage par mode, et
`UnityPanel/Inte-INSASTRONAUTE/tests/react-bridge-contract.test.mjs` pour le contrat
de pont.

**Unity, EditMode** — d'abord le test du §3.3 : dans une rencontre à trois ennemis
où `enemy-0` meurt en premier, l'état destiné à `enemy-1` atteint bien `enemy-1`, et
une carte visant le deuxième ennemi affiché part étiquetée `enemy-2`. Ce test échoue
sur le code actuel ; il est la mesure du chantier. Puis `CombatantRegistry`
(résolution d'identité, appartenance d'équipe, combattant local) ; `CombatantPiles` (adressage par combattant, un
événement destiné à un allié ne touche pas mes piles) ; `AuthoritativeEventReplayer`
(`definitionId` nul rend un dos de carte, chaque `Replay*Event` vise le bon
combattant) ; `CombatOutcomeSource` (victoire, défaite, forfait sans mort).

Le test qui compte le plus est celui qui n'existe pas aujourd'hui : **un événement
adressé à un combattant qui n'est pas moi ne doit pas modifier mes piles.** Il échoue
sur le code actuel.

## 13. Séquencement proposé

0. Test EditMode reproduisant le défaut du §3.3 : rencontre à trois ennemis,
   `enemy-0` meurt, l'état de `enemy-1` doit atteindre `enemy-1`. **Il doit échouer
   sur le code actuel** — c'est ce qui transforme l'analyse en fait, et c'est le
   test de non-régression du chantier entier.
1. Extraction à comportement constant du rejeu et des helpers, avec les tests
   EditMode qui transcrivent les correctifs existants. Le PvE doit rester identique.
2. `CombatantRegistry` et `CombatantPiles`, alimentés par les conventions PvE.
   Toujours à comportement PvE constant. L'identité cesse d'être positionnelle : le
   test de l'étape 0 passe.
3. Ré-adressage du rejeu par combattant.
4. `CombatOutcomeSource` et ciblage par équipe.
5. Serveur : snapshot PvP, deadline dans la vue, `COMMAND_REJECTED`,
   `COMBAT_EVENT` projetés.
6. Pont React : paramètre de mode.
7. Unity : identité de transport PvP, heartbeat, compte à rebours, bootstrap et écran
   de résultat PvP.
8. Partie jouable de bout en bout, puis campagne de non-régression PvE.

Les étapes 1 à 4 sont livrables indépendamment et ne changent rien d'observable :
elles peuvent être fusionnées avant que le reste ne soit prêt.

**Critère d'acceptation du chantier :** l'inventaire du §3.4 est vide, et le PvE — qui
reste le mode de référence, et qui est déjà un 1 contre N — se joue à l'identique, aux
dix bricolages près.

## 14. Questions ouvertes

- En co-op, un allié voit-il ma main ? Absorbé par le design, à trancher avant le
  co-op.
- Le règlement et le classement à quatre joueurs pour le 2v2 : non traité, et c'est
  le vrai coût de ce mode.
- Le forfait repose sur un heartbeat estampillé uniquement par les commandes. Un
  onglet mis en veille par le navigateur cesse d'émettre et perd le match. Se
  corrigerait proprement en estampillant aussi la présence sur la socket ; hors
  périmètre ici, mais à noter.
- **Le sort du tutoriel (§8.4)**, qui conditionne la suppression des ~8600 lignes de
  règles locales. C'est la question ouverte la plus lourde de ce document : elle ne
  bloque pas ce chantier, mais elle décide de ce qu'il permet ensuite.
