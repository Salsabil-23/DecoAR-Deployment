# 06 · Icônes

59 icônes dessinées pour DecoAR : les 26 icônes existantes redessinées, plus celles qui manquaient (compte, retours, poses, réglages).

![Aperçu sur verre](../icons/apercu-icones-sombre.png)

## Construction

- Grille **24 × 24**, zone utile 20 × 20 (2 de marge).
- Trait **2**, extrémités et angles **arrondis**, pas de remplissage (sauf petits points).
- Formes douces, coins arrondis ≈ 2 : même famille que l'arche du logo et que la police Outfit.
- Monochromes : la couleur vient du thème (`icon/default`, ou la couleur de l'action).
- Signature DecoAR : **l'anneau pointillé** de l'AR (logo) revient dans `ar`, `vue-3e` et `geste-double`.

## Fichiers

| Dossier | Contenu |
| --- | --- |
| `icons/svg/` | Une icône par fichier, couleur `currentColor` (web, Figma) |
| `icons/decoar-icons-sprite.svg` | Toutes les icônes en `<symbol>` : `<svg><use href="#dc-panier"/></svg>` |
| `icons/png-unity/64, 128, 256/` | PNG **blancs** pour Unity : on teinte avec `Image.color` |

Unity : prendre le dossier 128 (net jusqu'à 64 ref à l'écran) ; 256 pour les grandes illustrations de fenêtres d'aide.

## Tailles d'affichage

| Token | Ref | Usage |
| --- | --- | --- |
| `icon-sm` | 40 | Dans un champ, une ligne de liste, une étiquette |
| `icon-md` | 52 | Boutons, boutons ronds du HUD |
| `icon-lg` | 64 | Outils AR, guide des gestes |

## Catalogue

### Navigation

| Nom (fichier) | Signification |
| --- | --- |
| `retour` | Retour |
| `chevron` | Chevron |
| `chevron-bas` | Dérouler |
| `fermer` | Fermer |
| `boutique` | Boutique |
| `rayon` | Rayon |

### Boutique

| Nom (fichier) | Signification |
| --- | --- |
| `panier` | Panier |
| `catalogue` | Catalogue |
| `suggestions` | Suggestions |
| `favori` | Favori |
| `paiement` | Paiement |
| `colis` | Livraison |
| `position` | Ma position |
| `profil` | Profil |
| `reglages` | Réglages |
| `recentrer` | Recentrer |

### Vue

| Nom (fichier) | Signification |
| --- | --- |
| `vue-1re` | Vue 1re personne |
| `vue-3e` | Vue 3e personne |
| `gyroscope` | Gyroscope |

### Quantité

| Nom (fichier) | Signification |
| --- | --- |
| `plus` | Plus |
| `moins` | Moins |

### Recherche

| Nom (fichier) | Signification |
| --- | --- |
| `loupe` | Rechercher |
| `micro` | Micro |
| `micro-actif` | Micro (écoute) |

### AR et produit

| Nom (fichier) | Signification |
| --- | --- |
| `ar` | Voir en AR |
| `generer-3d` | Générer en 3D |
| `photo` | Photo |
| `galerie` | Galerie |
| `coloris` | Coloris |
| `taille` | Taille |
| `taille-reelle` | Taille réelle |
| `pose-sol` | Pose au sol |
| `pose-mur` | Pose au mur |
| `pose-table` | Pose sur table |
| `surface` | Surface détectée |

### Gestes

| Nom (fichier) | Signification |
| --- | --- |
| `geste-deplacer` | Déplacer |
| `geste-tourner` | Tourner |
| `geste-pincer` | Pincer |
| `geste-toucher` | Toucher |
| `geste-secouer` | Secouer |
| `geste-double` | Double toucher |

### Retours

| Nom (fichier) | Signification |
| --- | --- |
| `valide` | Validé |
| `info` | Information |
| `alerte` | Attention |
| `erreur` | Erreur |
| `aide` | Aide |
| `chrono` | Chronomètre |
| `lumiere` | Lumière |
| `hors-ligne` | Hors ligne |

### Compte

| Nom (fichier) | Signification |
| --- | --- |
| `visage` | Visage |
| `cadenas` | Mot de passe |
| `email` | E-mail |
| `telephone` | Téléphone |
| `modifier` | Modifier |
| `supprimer` | Supprimer |
| `deconnexion` | Déconnexion |
| `admin` | Administration |

### Réglages

| Nom (fichier) | Signification |
| --- | --- |
| `musique` | Musique |
| `vibration` | Vibrations |

## Correspondance avec l'ancien jeu

| Ancienne icône | Nouvelle |
| --- | --- |
| Retour ‹ / Chevron › / Fermer × | `retour` / `chevron` / `fermer` |
| Panier (sac) | `panier` |
| Catalogue (grille) | `catalogue` |
| Suggestions (étincelles) | `suggestions` |
| Profil | `profil` |
| Réglages (curseurs) | `reglages` |
| Recentrer (viseur) | `recentrer` |
| Position | `position` |
| Œil / Personnage | `vue-1re` / `vue-3e` |
| Plus / Moins | `plus` / `moins` |
| Loupe / Micro | `loupe` / `micro` (+ `micro-actif`) |
| Cube AR | `ar` |
| Photo | `photo` |
| Coloris | `coloris` |
| Taille | `taille` (+ `taille-reelle`) |
| Déplacer / Tourner / Pincer / Toucher | `geste-deplacer` / `geste-tourner` / `geste-pincer` / `geste-toucher` |

## Règles

- Une icône seule (sans texte) doit être **universelle** : retour, fermer, panier, loupe, plus/moins. Toutes les autres ont une étiquette (outils AR) ou une info-bulle d'aide la première fois.
- État actif : l'icône passe en `state/selected` (laiton sur verre, terracotta sur papier) et la tuile derrière passe à 16 %.
- Pas d'icônes en couleurs multiples dans l'interface ; la couleur est réservée aux illustrations (`assets/etats-vides`).
