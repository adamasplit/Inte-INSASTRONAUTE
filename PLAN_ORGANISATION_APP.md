# Plan d'organisation — INSA'STRONAUTE
*Version 1.0 — Avril 2026*

---

## Vue d'ensemble

L'application s'articule autour de **5 onglets principaux** dans la barre de navigation basse.
La règle de conception : chaque fonctionnalité doit être accessible en **3 taps maximum** depuis n'importe où.

```
┌─────────────────────────────────────────────┐
│                  CONTENU                    │
│                                             │
│                                             │
├──────┬──────┬──────┬──────────┬─────────────┤
│  🏠  │  🎯  │  🃏  │    🏆    │     👤      │
│Accueil│Défis │  Jeu │Classement│   Profil   │
└──────┴──────┴──────┴──────────┴─────────────┘
```

---

## 1. ACCUEIL

**Rôle :** Tableau de bord central. Premier écran vu après la connexion. Donne envie d'agir.

### Éléments affichés
- Bandeau joueur : pseudo, Points Défis, TOKEN
- **Défi mis en avant** : le défi du moment (avec image, points, CTA "Voir le défi")
- **Événement actif** : si un pari est ouvert, carte avec deadline et options (CTA "Parier")
- **Récompense journalière** : bouton de claim si disponible, compteur sinon
- **Ma position au classement** : rang actuel avec mini-podium (les 3 au-dessus/dessous)
- Notifications récentes (défi validé, pari résolu, carte débloquée)

### Navigation depuis l'Accueil
- Tap défi → Page Défi (dans section Défis)
- Tap événement → Page Événement (modal overlay)
- Tap classement → onglet Classement
- Tap notification → écran concerné

---

## 2. DÉFIS

**Rôle :** Hub principal de la campagne d'intégration. C'est ici que les intégrés passent le plus de temps.

### 2.1 Vue liste des défis

```
┌─────────────────────────────────────────┐
│  [Tous] [Thème A] [Thème B] [Thème C]  │  ← filtres par thème
├─────────────────────────────────────────┤
│  [Disponibles ▼] [En cours] [Complétés]│  ← filtre statut
├─────────────────────────────────────────┤
│ ┌─────────────────────────────────────┐ │
│ │ 🌿 Défi Nature          +150 pts   │ │
│ │ Fais un câlin à un arbre           │ │
│ │ [DISPONIBLE]                       │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ 🎯 Défi Sport           +200 pts   │ │
│ │ Gagne une partie de ping-pong      │ │
│ │ [EN ATTENTE DE VALIDATION]         │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ 🔒 Défi Légende         +500 pts   │ │
│ │ Nécessite : Défi Sport complété    │ │
│ │ [VERROUILLÉ]                       │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

**Statuts possibles d'une carte défi :**
- `DISPONIBLE` → peut être soumis
- `EN ATTENTE` → soumission envoyée, en cours de validation admin
- `VALIDÉ` → complété, points accordés
- `REFUSÉ` → rejeté avec message de l'admin (peut resoumettre)
- `VERROUILLÉ` → défi précédent non complété

### 2.2 Défis progressifs (chaînes)

Affichage visuel de la progression dans une chaîne :

```
  [Défi 1 ✅] → [Défi 2 ⏳] → [Défi 3 🔒] → [RÉCOMPENSE FINALE 🏆]
```

Le joueur voit clairement ce qui l'attend et ce qui est débloqué.

### 2.3 Page détail d'un défi

- Image du défi (grande, en haut)
- Titre + description complète
- Thème + Points accordés
- Prérequis si défi chaîné
- Historique de ses tentatives (validé/refusé + commentaire admin)
- **Bouton "Soumettre une preuve"**

### 2.4 Soumission de preuve

1. Tap "Soumettre une preuve"
2. Choix : Prendre une photo / Filmer une vidéo / Choisir depuis galerie
3. Prévisualisation + confirmation
4. Message de confirmation : "Soumission envoyée, en attente de validation"
5. Statut visible dans la liste et sur l'Accueil

---

## 3. JEU

**Rôle :** Hub du jeu de cartes. Regroupe le minijeu, la collection et le shop — tout ce qui touche aux cartes.

### Sous-navigation (onglets internes)

```
[ Collection ] [ Aventure ] [ Shop ]
```

### 3.1 Collection

- **Onglets :** Digitale / Physique
- **Vue :** Grille de cartes, filtre par élément, rareté, possédé/non possédé
- Tap sur une carte → fiche complète (image, stats, effet en jeu, rareté, si possédée en double+)
- Bouton **"Scanner une carte physique"** (QR code) accessible en haut
- Bouton **"Construire mon deck"** → Deck Builder

**Deck Builder :**
- 12 cartes max depuis la collection
- Max 2 exemplaires d'une même carte
- Deck sauvegardé en cloud (pas en local — voir note technique)

### 3.2 Aventure (Minijeu Slay the Spire)

**Boucle de jeu :**
```
Sélection du deck → Départ en run → Salles → Boss → Récompenses
```

- **Écran de départ :** deck actuel affiché, stats rapides, bouton "Partir"
- **Pendant le run :** progression salle par salle, choix de cartes à la draft entre les salles
- **Combat :** cartes avec effets uniques, éléments (Planète > Fusée > Étoile + Prismatique)
- **Victoire de salle :** choix parmi 3 cartes à ajouter au run (pas à la collection)
- **Boss :** ennemi puissant, récompense en cas de victoire = carte rare ajoutée **à la collection permanente**
- **Fin de run :** score → converti en TOKEN

**Lien collection → jeu :**
- Plus ta collection est riche, plus ton deck de départ est fort
- Les cartes gagnées en boss s'ajoutent à la collection et peuvent être utilisées dans les runs suivants
- Certaines cartes très rares ne s'obtiennent qu'en battant des boss

### 3.3 Shop

- **Sections :** Packs / Cartes individuelles
- Achat **x1 / x10** de packs (le x10 avec réduction ou garantie de rareté)
- Solde TOKEN toujours visible
- Animation d'ouverture de pack (à améliorer pour le côté satisfaisant)

---

## 4. CLASSEMENT

**Rôle :** Afficher le classement complet, pas seulement le top 10. Point clé demandé par le pôle défi.

### Structure

```
┌─────────────────────────────────────────┐
│  [Global] [Par thème]                   │
├─────────────────────────────────────────┤
│  🥇  1.  PseudoJoueur1     2450 pts    │
│  🥈  2.  PseudoJoueur2     2200 pts    │
│  🥉  3.  PseudoJoueur3     1980 pts    │
│      4.  PseudoJoueur4     1750 pts    │
│      5.  PseudoJoueur5     1600 pts    │
│       ... (liste complète, scroll)      │
├─────────────────────────────────────────┤
│  ══════════════════════════════════     │  ← séparateur
│  📍 42.  TOI              850 pts      │  ← ta position toujours visible
└─────────────────────────────────────────┘
```

- Liste complète avec pagination ou scroll infini
- Position du joueur **épinglée en bas**, visible même si hors top 10
- Classement par thème de défis (optionnel, selon demande pôle défi)

---

## 5. PROFIL

**Rôle :** Compte joueur, stats, paramètres.

### Contenu

- **En-tête :** Avatar (si on en ajoute), pseudo, membre depuis
- **Stats :**
  - Points Défis (score total)
  - TOKEN (solde actuel)
  - PC (Collection Points)
  - Rang actuel
  - Nombre de défis complétés
  - Nombre de cartes possédées
- **Historique :**
  - Défis complétés (avec date)
  - Paris placés et résultats
- **Paramètres :** Son, notifications, langue
- **Compte :** Déconnexion, suppression de compte (RGPD)

---

## 6. PARIS & ÉVÉNEMENTS

**Rôle :** Fonctionnalité transversale, accessible depuis l'Accueil et le Profil. Pas d'onglet dédié car les événements sont ponctuels.

### Accès
- Depuis l'Accueil : carte événement → modal overlay
- Depuis le Profil : historique des paris

### Page Événement

```
┌─────────────────────────────────────────┐
│  [IMAGE DE L'ÉVÉNEMENT]                 │
│                                         │
│  Titre de l'événement                   │
│  Description claire et lisible          │
│                                         │
│  ⏱ Ferme dans : 2h 34min               │
│                                         │
│  ┌─────────────┐  ┌─────────────────┐  │
│  │  Option A   │  │    Option B     │  │
│  │   Cote x2.5 │  │    Cote x1.8   │  │
│  └─────────────┘  └─────────────────┘  │
│                                         │
│  Mise : [___] TOKEN    Solde : 450 🪙   │
│                                         │
│         [ CONFIRMER LE PARI ]           │
└─────────────────────────────────────────┘
```

- Image obligatoire (gérée par l'admin via le back-office)
- Cotes affichées clairement
- Gain potentiel calculé en temps réel selon la mise
- Confirmation explicite avant validation

---

## 7. ADMIN (menu caché, accès rôle-based)

**Accès :** Visible uniquement si le compte a le rôle `ADMIN`, vérifié côté serveur.
**Point d'entrée :** Bouton discret dans l'écran Profil.

### Sections

#### 7.1 Validation des défis (priorité principale)
Interface de swipe style Tinder :
```
← REFUSER          ACCEPTER →
      ┌────────────────────┐
      │   [Photo/vidéo]    │
      │                    │
      │  Pseudo : Joueur   │
      │  Défi : Câlin arbre│
      │  Soumis il y a 5min│
      └────────────────────┘
      Swipe gauche / droite
      ou boutons si préféré
```
- En cas de refus : champ texte pour motif (affiché au joueur)
- File d'attente visible (X soumissions en attente)

#### 7.2 Gestion des défis
- Créer un défi (titre, description, image, thème, points, prérequis éventuel)
- Modifier / archiver un défi existant
- Organiser les chaînes progressives (drag & drop)

#### 7.3 Gestion des événements & paris
- Créer un événement (titre, image, description, options, cotes, deadline)
- Fermer un événement (déclenche la résolution automatique des paris)
- Modifier un événement ouvert

#### 7.4 Gestion des points
- Rechercher un joueur (par pseudo)
- Voir ses Points Défis, TOKEN, défis complétés
- Ajouter / retirer des Points Défis manuellement (avec motif obligatoire)

---

## 8. FLUX D'ONBOARDING (premier lancement)

```
Écran de connexion
       ↓
Création de compte (ou invité)
       ↓
Tutoriel contextuel (coach marks, pas de flow forcé)
  - L'accueil explique le dashboard
  - La section Défis explique les défis quand on y arrive
  - Le Jeu explique le deck builder quand on l'ouvre
       ↓
Accueil (avec défi du moment mis en avant)
```

**Principe du nouveau tutoriel :** chaque section explique ses propres fonctionnalités à la première visite, via des tooltips contextuels. Pas de séquence linéaire bloquante.

---

## 9. NOTES TECHNIQUES POUR LES DÉVELOPPEURS

| Point | Décision |
|---|---|
| Deck sauvegardé | Cloud (VM) — pas PlayerPrefs |
| Auth | UGS Authentication (inchangé) |
| Points Défis | Base de données VM |
| TOKEN | UGS Economy (inchangé) |
| Photos/vidéos défis | Stockage VM |
| Admin rôle | Vérifié serveur — jamais côté client |
| Cache WebGL | Headers HTTP côté serveur (fix prioritaire) |
| Scènes Unity | À diviser (travail en cours par collègue) |
| QR physique | UUID unique par exemplaire, scan = activation unique |

---

*Document de travail — à mettre à jour au fil des décisions*
