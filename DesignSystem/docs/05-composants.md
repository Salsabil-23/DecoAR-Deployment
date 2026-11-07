# 05 · Composants

Une seule version de chaque composant, déclinée en thème **Verre** ou **Papier**. Mesures en points du Canvas (ref). Les noms de couleurs sont les rôles de `02-couleurs.md`.

Code : `unity/Scripts/Runtime/DecoUI.cs` construit les composants marqués ⚙.

---

## Navigation

### Bouton retour ⚙
Pilule, hauteur 88, padding 32 / 40, icône `retour` 40 + libellé `label` (« Boutique », « Rayon », « Connexion »).
- Verre : surface verre. Papier : fantôme, texte `text/primary`.
- Toujours en haut à gauche, dans la zone sûre.

### Barre du haut (HUD)
- Hauteur 112, marge 48 + zone sûre.
- Groupe gauche : carte profil, Réglages, Vue. Groupe droit : Suggestions, Panier. Écart 20.
- Centre : réservé aux toasts.

### Onglets défilants (rayons, personnage)
- Pilules hauteur 88, padding horizontal 32, écart 12, style `label`.
- Inactif : tuile 7 %, texte `text/secondary`. Actif : fond `state/selected` (laiton), texte encre, icône éventuelle encre.
- L'onglet actif se recentre automatiquement (défilement 240 ms, `standard`). Dégradé de fondu de 48 sur les bords quand ça déborde.

### Tiroir latéral
- Largeur 760 (paysage) ; en portrait, feuille du bas sur 85 % de la hauteur, poignée 64 × 8 en haut.
- Verre fort, rayon `lg` côté intérieur seulement, du haut (sous la barre) jusqu'à 48 du bas.
- En-tête : titre `titre-2` + bouton fermer rond 88. Padding 32.
- Entrée : glisse depuis le bord, 360 ms `entree` ; sortie 240 ms `sortie`. Voile 40 % derrière.

### Transition de scène
Fondu vers `lin.1000` en 300 ms, puis fondu d'entrée en 450 ms. Pendant un chargement > 1 s, le symbole DecoAR apparaît au centre avec l'anneau qui tourne (voir *Chargement*).

---

## Boutons ⚙

| Variante | Verre | Papier | Usage |
| --- | --- | --- | --- |
| Principal | laiton / encre | terracotta 600 / blanc | Action principale, une par écran |
| Commerce | sauge 600 / blanc | sauge 600 / blanc | Panier, paiement |
| Accent | terracotta 400 / encre | laiton / encre | Rare : micro actif, étape en cours |
| Secondaire | blanc 12 % / crème | lin 150 / encre | Alternatives |
| Fantôme | texte crème | texte terracotta 700 | Liens, actions faibles |

- Hauteur **112** (compact **88**), rayon pilule, padding horizontal 48, icône 52 + écart 12, texte `bouton`.
- Pressé : assombri de 14 % + échelle 0,97 (90 ms). Désactivé : opacité 55 %, pas de vibration.
- Chargement : le libellé laisse la place à l'anneau qui tourne (même largeur, pas de saut).
- Bouton qui « respire » (`DecoBreathe`) : seulement « Payer » au comptoir et « Ajouter au panier » en AR après la pose. Jamais deux en même temps.

### Bouton rond flottant (HUD) ⚙
Diamètre **112**, verre, icône 52 crème. Actif : icône laiton + tuile 16 %. Avec pastille compteur en haut à droite.

### Outil AR (bouton + étiquette)
Bouton rond 112 + étiquette `legende` dessous (écart 8), dans une colonne à droite, écart 24. Étiquette sur une petite pilule de verre (hauteur 44) pour rester lisible sur la caméra.

### Lien texte
`label` semi-gras, couleur `action/link` (papier) ou crème souligné (verre). Chevron `›` facultatif. Zone tactile 104 de haut.

---

## Données et listes

### Carte produit (catalogue)
- **360 × 184**, rayon `md`, verre.
- Haut : pastille de pose (icône `pose-*` 32 sur rond 48 coloré `pose/*`) + nom `corps` semi-gras sur 2 lignes max.
- Bas : prix `prix` laiton 300 aligné à gauche, bouton « + » rond 72 à droite (ajout rapide).
- Sélectionnée (produit inspecté) : liseré laiton 4 + légère montée de 8.

### Ligne de liste
Hauteur 112, tuile 7 %, rayon `sm`, padding 24. Icône 40 · texte `corps` (+ sous-texte `legende`) · action à droite (chevron, interrupteur, bouton icône).

### Ligne de panier
Vignette 96 (rayon `xs`) · nom + coloris · stepper `− 1 +` (boutons ronds 72, chiffre tabulaire) · prix aligné à droite. Glisser vers la gauche révèle « Retirer ».

### Ligne de total
Séparateur blanc 12 %, « Total » `label` à gauche, montant `titre-3` laiton à droite, mention « TVA incluse » `legende`.

### Tableau d'administration (papier)
- En-tête `surtitre` `text/tertiary`, lignes de 120, séparateurs `border/hairline`.
- Colonnes : produit (nom + référence `legende`), catégorie (chip terracotta 100 / texte terracotta 700), date, actions.
- « Supprimer » : 1er toucher → le bouton devient « Confirmer ? » brique pendant 3 s. En portrait, le tableau devient une liste de cartes.

---

## Saisie

### Champ texte ⚙
| | Verre | Papier |
| --- | --- | --- |
| Fond | tuile 7 % | `surface/sunken` |
| Bordure repos | aucune | aucune |
| Focus | liseré laiton 3 | liseré terracotta 600 3 + halo `focus-ring` |
| Erreur | liseré brique 300 + message | liseré brique 700 + message |

Hauteur **104**, rayon `sm` (verre) ou `md` (papier), padding 32, icône 40 à gauche facultative. Libellé **au-dessus** (`label`, `text/secondary`), jamais seulement un placeholder. Message d'aide / erreur en dessous (`legende`, icône `alerte` 32).

### Mot de passe + jauge
Bouton œil à droite. Jauge : 4 segments de 8 de haut, écart 6 : brique → laiton → sauge 400 → sauge 600, avec le mot (« Trop faible », « Moyen », « Bon », « Excellent »).

### Code à 6 chiffres
6 cases 96 × 112, rayon `md`, chiffres `titre-1`. Case active : liseré terracotta. Collage du code accepté. Erreur : petite secousse horizontale (3 aller-retours, 240 ms) + vibration moyenne.

### Barre de recherche
Pilule 104 : `loupe` · champ · `micro` · bouton « OK » si texte. Micro actif : fond terracotta 400, icône `micro-actif`, anneau qui pulse autour.

### Interrupteur
64 × 112 (zone 104). Piste : blanc 16 % / `lin.300` (off) ; sauge 600 (on). Pouce crème 52, glisse en 150 ms `standard`. Vibration légère.

### Pastilles de couleur (coloris, personnage)
Rond **88** (zone 104), écart 16. Sélection : anneau laiton 4 à 6 de distance + coche si la couleur est claire. La première « Original » a un liseré pointillé. Nom du coloris affiché sous la rangée.

### Chips de catégorie (choix unique)
Pilules 88 ; inactive `surface/sunken` ; active terracotta 600 / blanc avec icône `valide`.

---

## Retours d'information

### Toast ⚙
Pilule de verre 96 de haut, 560 à 900 de large, en haut au centre sous la zone sûre. Point de 20 coloré (laiton info, sauge 300 succès, brique 300 erreur) + texte `corps`. Entrée 240 ms par le haut, reste 2,6 s, sortie 150 ms. Un seul à la fois ; le nouveau remplace l'ancien.

### Pastille compteur ⚙
Rond 40, `brique.600`, chiffre blanc Bold 24, « 9+ ». Rebond (`DecoPop`) à chaque ajout.

### Chargement
- **Court (< 3 s)** : anneau pointillé (celui du logo) qui tourne, 52, laiton.
- **Long (génération 3D)** : grand symbole avec l'anneau qui tourne, chronomètre `titre-3` tabulaire, message serveur `corps` sous le chrono, halo AR qui « respire » derrière.
- Jamais de barre de progression inventée.

### Consigne AR
Pilule de verre en bas au centre, point pulsé (laiton, 1,2 s) + texte `corps-l`. Une seule consigne à la fois.

### Fenêtre d'aide (guide des gestes)
Verre fort, rayon `lg`, 1100 de large (portrait : 92 %). Grille 2 × 2 : icône de geste 64 dans un rond tuile 120 + titre `titre-3` + phrase `corps`. Bouton principal « C'est compris ».

### Fenêtre de paiement
Papier (même sur la boutique) : on entre dans un moment « sérieux ». Logo Stripe fourni par Stripe, mention « Paiement sécurisé par Stripe » `legende` avec icône `cadenas`.

### État vide
Illustration (`assets/etats-vides`, 480 × 360) · titre `titre-3` · texte `corps` `text/secondary` · une action. Centré, largeur max 720.

---

## Caméra et 3D

### Cadre de scan du visage
Ovale de 560 × 700 encadré par **les quatre coins de visée** du symbole (blancs, trait 6) : liseré crème 4 sur fond voilé 55 % autour. Progression : l'arche se remplit d'un trait laiton qui fait le tour. Réussite : trait sauge + coche + vibration moyenne.

### Visionneuse 3D (M6, inspection)
Rayon `lg`, fond dégradé `lin.850` → `lin.1000` (verre) ou `lin.150` → `lin.200` (papier), socle 3D. Boutons ronds 88 dans le coin haut droit (Rotation, Recentrer), aide `legende` en bas : « Glisse pour tourner · pince pour zoomer ».

### Étapes numérotées
Rond 56 avec chiffre Bold : à venir (contour `lin.300`), en cours (terracotta 600 plein + halo), faite (sauge 600 + coche). Titre `titre-3`, ligne verticale pointillée entre les étapes.

### Réticule AR
Sprites `assets/ar/reticule-recherche|valide|refuse.png`, posés à plat sur la surface. Recherche : anneau pointillé qui tourne lentement (8 s / tour). Valide : anneau plein laiton + vibration légère. Refusé : brique + croix + consigne (« Ce produit se pose sur un mur »).

### Étiquette de prix 3D et enseigne
Étiquette : pilule crème `lin.50`, prix encre Bold, petit point de la couleur de pose. Enseigne : texture `assets/3d/enseigne-etagere.png` (le nom du rayon est écrit à part en TMP 3D, laiton 400).

---

## Profil

### Carte profil compacte (HUD)
Pilule de verre 112 : avatar rond 88 (liseré laiton 3) + prénom `label`. « Invité » : avatar `assets/avatar/avatar-invite.png`.

### En-tête de profil (papier)
Avatar 200 avec bouton « modifier » rond 64 en bas à droite, nom `titre-1`, e-mail `corps` `text/secondary`.
