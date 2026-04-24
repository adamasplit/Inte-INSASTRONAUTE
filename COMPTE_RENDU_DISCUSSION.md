# Compte rendu — Session de conception INSA'STRONAUTE
*Date : 20 avril 2026 — Deadline projet : fin juillet 2026*

---

## Contexte

Application Unity (WebGL + Android) pour la **campagne d'intégration INSA**.
Cible : futurs étudiants intégrés pendant la semaine d'intégration.
Test précédent : 60-70 testeurs en conditions réelles de campagne, tous types d'appareils.

---

## PARTIE 1 — Feedbacks des tests (60-70 personnes)

### 🔴 Problèmes critiques

#### 1. Cache WebGL — Le plus urgent
**Problème :** Quand un hotfix était déployé, les utilisateurs continuaient d'avoir l'ancienne version en cache navigateur. Seule solution : supprimer les données de navigation côté utilisateur — impossible en version finale.
**Impact :** Tout correctif en production était inefficace.
**Solution décidée :** Configurer les headers HTTP côté serveur (`Cache-Control: no-cache` pour `index.html`, hachage des noms de fichiers pour les assets). Fix unique côté hébergement, règle le problème définitivement.
**Priorité :** À traiter en premier après le prochain build.

#### 2. Système de paris — Complètement à refaire
**Problème :** Interface non user-friendly, conçue "côté informaticien". Pas d'images, peu lisible, peu explicite. Mise à jour des événements via Remote Config = toujours en retard.
**Décision :** Refonte complète. Événements gérés via un back-office web (plus Remote Config). Images obligatoires sur chaque événement.

---

### 🟠 Problèmes importants

#### 3. Batterie et performances
**Problème :** Consommation batterie excessive en jeu, probablement liée aux particules et à la scène monolithique.
**Décision :** Division de la scène "Main - Copie" en sous-scènes . Audit des particle systems à faire en parallèle.
**Note :** "Main - Copie" est à renommer en scène de production propre.

#### 4. Débogage — Unity ID vs pseudo
**Problème :** Dans le dashboard UGS, seul l'Unity Player ID (GUID) est visible, pas le pseudonyme. Impossible d'identifier rapidement un joueur en cas de bug signalé.
**Solution décidée :** Ajouter un écran de profil affichant l'Unity Player ID au joueur, et/ou une fonction de recherche par pseudonyme côté back-office.

#### 5. Tutoriel — Trop faible et trop fragile
**Problèmes identifiés :**
- Laisse trop de liberté au joueur, bugs si le flow n'est pas suivi exactement
- Parfois ennuyeux
- Se redéclenche à chaque session s'il n'a pas été fini entièrement
- Bugs lors des changements de scène
- Tutoriel du minijeu totalement inadapté

**Décision :** Refonte complète. Passage à un système de **coach marks contextuels** : chaque section explique ses propres fonctionnalités à la première visite, via des tooltips. Plus de séquence linéaire bloquante.

---

### 🟡 Améliorations identifiées

#### 6. Ouverture de packs
**Retour :** L'expérience d'ouverture peut être plus satisfaisante et user-friendly.
**Décision :** À améliorer (priorité moyenne, après les features critiques).

#### 7. Achat en lot au shop
**Demande :** Achat x10 (ou plus) de packs.
**Décision :** À implémenter. Envisager une garantie de rareté sur le x10 pour le rendre attractif.

#### 8. Notifications
**Retour :** Système peu user-friendly, parfois peu explicite.
**Décision :** À retravailler avec le reste de l'UX.

---

### 🔒 Sécurité
**Point soulevé :** Des données confidentielles seront manipulées. La sécurité doit être parfaite et justifiée.
**Statut :** À détailler lors d'une prochaine session (types de données concernées à définir).
**Principe retenu :** Toute vérification de rôle ou de permission se fait côté serveur, jamais côté client Unity.

---

## PARTIE 2 — Nouvelles features décidées

### Feature 1 — DÉFIS (la plus importante)

**Description :** Pendant l'intégration, les intégrés réalisent des défis variés (physiques, sociaux, en jeu) et soumettent des preuves photo/vidéo. Les admins valident ou refusent via une interface dédiée.

**Fonctionnalités :**
- Défis organisés par **thèmes**
- **Défis progressifs / chaînés** : le défi 2 se débloque uniquement après validation du défi 1
- Soumission de preuve : photo ou vidéo depuis l'app
- **Interface admin de validation style Tinder** (swipe gauche = refus, swipe droite = validation), accessible depuis l'app (menu caché) ou interface web (POUR ADMINS)
- En cas de refus : message explicatif affiché au joueur
- Ajout de points manuels par les admins (accès direct aux compteurs de points, demandé par le pôle défi)

**Demandes spécifiques du pôle défi :**
- Accès direct sur les compteurs de points pour modifier comme voulu
- Classement complet (pas seulement top 10)
- Classement par thème de défi
- Défis évolutifs (chaînes progressives)

---

### Feature 2 — Nouveau système de monnaies (3 monnaies distinctes)

**Problématique résolue :** Éviter que les joueurs ne veuillent pas dépenser pour préserver leur position au classement.

| Monnaie | Stockage | Usage | Peut descendre ? |
|---|---|---|---|
| **Points Défis** | VM (base de données) | Leaderboard uniquement — jamais dépensé | ❌ jamais |
| **TOKEN** | UGS Economy | Shop + Paris | ✅ oui |
| **PC** (Collection Points) | UGS Cloud Save | Affiché, basé sur la collection | ❌ |

**Règle clé :** Compléter un défi donne **+Points Défis** (score permanent) **ET +TOKEN** (monnaie dépensable). Le classement reflète l'investissement dans la campagne, pas la richesse actuelle.

---

### Feature 3 — Collection physique de cartes

- Séparation claire **collection digitale / collection physique** dans l'app
- Enregistrement des cartes physiques via **QR code unique par exemplaire**
- À la scan : l'app vérifie si le QR n'a pas déjà été utilisé → ajoute la carte → marque comme réclamé (usage unique)
- Stockage des QR codes et de leur état (réclamé ou non) sur la VM

---

### Feature 4 — Refonte totale du minijeu (Slay the Spire)

**Nouveau concept :** Roguelike de type Slay the Spire en remplacement du tower defense.

**Boucle de jeu :**
- Le joueur construit un deck de 12 cartes depuis sa collection digitale
- Il part en "run" : progression de salle en salle
- À chaque victoire de salle, choix parmi 3 cartes à ajouter au run (temporaire)
- Des boss puissants donnent des cartes rares ajoutées à la **collection permanente**
- Fin de run : score converti en TOKEN

**Lien collection ↔ minijeu :**
- Plus la collection est riche, plus le deck de départ est varié et puissant
- Certaines cartes rares sont obtenues **uniquement en battant des boss** → motivation à jouer
- Les cartes gardent leur système d'éléments (Planète > Fusée > Étoile, Prismatique bat tout)

---

### Feature 5 — Refonte du système de paris & événements

**Problèmes actuels :** Gestion manuelle via Remote Config, pas d'images, UX pauvre.

**Nouvelle approche :**
- Événements créés et gérés via le **back-office web** sur la VM (plus Remote Config)
- Chaque événement a une image obligatoire
- Affichage des cotes, deadline visible, gain potentiel calculé en temps réel
- Résolution automatique côté serveur à la fermeture de l'événement
- Historique des paris accessible dans le Profil du joueur

---

### Feature 6 — Classement complet

**Demande pôle défi :** Voir l'intégralité du classement, pas seulement les 10 premiers.

**Solution :**
- Liste complète avec scroll (ou pagination)
- Position du joueur **épinglée en bas de l'écran** même s'il est hors top 10
- Classement global + classement par thème de défi (optionnel)

---

## PARTIE 3 — Décisions d'architecture

### Infrastructure

| Composant | Technologie | Rôle |
|---|---|---|
| **VM École** | nginx + PostgreSQL | Backend principal, stockage fichiers, interface admin |
| **UGS Authentication** | Unity Services | Identité joueur (inchangé) |
| **UGS Economy** | Unity Services | TOKEN (inchangé) |
| **UGS Cloud Save** | Unity Services | Collection cartes/packs (inchangé) |
| **UGS Cloud Code** | Unity Services | Distribution de récompenses (appelé par la VM) |

**Ce qui migre vers la VM :** Défis, classement, événements/paris, shop, QR cartes physiques, interface admin.
**Ce qui reste sur UGS :** Auth, TOKEN, collection cartes/packs.

### Authentification inter-systèmes

Le Unity app récupère un **JWT via UGS Auth** → l'envoie dans chaque requête à la VM → la VM valide ce JWT auprès de UGS. Aucun système d'auth custom à créer, l'identité UGS est la source de vérité.

### Interface d'administration

**Choix :** Interface web hébergée sur la VM (accessible depuis téléphone).

**Avantages retenus :**
- Aucun build Unity nécessaire pour les mises à jour admin
- La validation des défis (swipe Tinder) est plus simple à faire en HTML/CSS
- Accessible depuis n'importe quel appareil des admins
- Rôle admin vérifié côté serveur — jamais dans le client Unity

**Accès depuis l'app :** Bouton discret dans le Profil, visible uniquement si le compte a le rôle `ADMIN` (vérifié serveur).

---

## PARTIE 4 — Points en suspens (à traiter en prochaine session)

| Point | Statut |
|---|---|
| Langage backend VM (Node.js / Python / autre) | ⏳ À décider |
| Détail des données confidentielles à protéger | ⏳ À définir |
| Dimensionnement stockage VM (photos/vidéos) | ⏳ À estimer |
| Planning de développement détaillé | ⏳ À construire |
| Minijeu : démarrage en parallèle ou après les autres features | ⏳ À décider |
| Extensions de cartes pendant l'intégration : workflow d'ajout | ⏳ À définir |
| Classement par thème de défi : dans le scope ou non | ⏳ À confirmer |
| Nombre et profils des admins | ⏳ À clarifier |

---

## PARTIE 5 — Priorités recommandées

**Phase 1 — Socle technique (avant de construire quoi que ce soit de nouveau)**
1. Fix du cache WebGL (headers serveur)
2. Division de la scène "Main - Copie"
3. Mise en place de la VM (nginx, PostgreSQL, API de base)
4. Système d'authentification VM via JWT UGS

**Phase 2 — Feature principale**
5. Système de défis (back-office + app)
6. Nouveau classement complet
7. Système de monnaies (Points Défis + TOKEN distincts)

**Phase 3 — Refonte**
8. Refonte paris & événements
9. Nouveau tutoriel contextuel
10. Collection physique (QR code)

**Phase 4 — Améliorations**
11. Refonte minijeu (Slay the Spire)
12. Achat x10 au shop
13. Amélioration ouverture de packs

---

*Document de travail — 20 avril 2026*
*Prochaine session : définir le planning détaillé et les points en suspens*
