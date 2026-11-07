# 03 · Typographie

**Outfit partout**, la police du logo. Géométrique et douce, elle a les mêmes courbes que l'arche du symbole. Licence SIL OFL : gratuite, utilisable dans une application commerciale (fichier `fonts/OFL.txt`).

Graisses fournies : Regular 400, Medium 500, SemiBold 600, Bold 700 (`fonts/`).

## Pourquoi les tailles augmentent

Le Canvas est en 1920 × 1080 avec un équilibre 0,5. Sur un téléphone 20:9 typique (2400 × 1080 px, densité 2,625), **1 point du Canvas ≈ 0.426 dp**. L'ancien texte courant (22) faisait donc **≈ 9,5 dp**, et l'ancienne cible tactile (64) **≈ 27 dp**. Apple recommande 17 pt pour le corps et 44 pt pour une cible ; Android 14 sp et 48 dp.

La nouvelle échelle remonte tout au niveau des recommandations iOS et Android, tout en restant compacte pour le HUD paysage.

## Échelle

| Style | Taille (ref) | ≈ dp / pt | Graisse | Interligne | Approche | Usage |
| --- | --- | --- | --- | --- | --- | --- |
| `display` | 72 | 30.7 | 700 | 1.05 | -0.02 em | Splash, grands titres d'onboarding |
| `titre-1` | 56 | 23.9 | 600 | 1.1 | -0.015 em | Titre d'écran (Auth, Profil, Admin) |
| `titre-2` | 46 | 19.6 | 600 | 1.15 | -0.01 em | Titre de tiroir, de fiche, de fenêtre |
| `titre-3` | 40 | 17.0 | 600 | 1.2 | -0.005 em | Section, nom de produit dans la fiche |
| `corps-l` | 38 | 16.2 | 400 | 1.4 | +0 em | Texte d'introduction, consignes AR |
| `corps` | 34 | 14.5 | 400 | 1.4 | +0 em | Texte courant, champs, lignes de liste |
| `label` | 32 | 13.6 | 500 | 1.25 | +0.005 em | Onglets, boutons secondaires, chips |
| `bouton` | 36 | 15.3 | 600 | 1.0 | +0.005 em | Texte des boutons principaux |
| `legende` | 28 | 11.9 | 500 | 1.3 | +0.01 em | Légendes, étiquettes d'outils, mentions |
| `surtitre` | 24 | 10.2 | 600 | 1.2 | +0.08 em | SURTITRE en capitales (rayon, étape) |
| `prix-xl` | 64 | 27.3 | 700 | 1.0 | -0.02 em | Prix dans la fiche produit |
| `prix` | 36 | 15.3 | 600 | 1.0 | +0 em | Prix dans les cartes et lignes |

## Règles

- **Chiffres** : prix, quantités, dimensions et chronomètre en chiffres tabulaires (balise TMP `<mspace=0.6em>` sur les chiffres ou police Outfit Medium, dont les chiffres sont presque à chasse fixe).
- **Capitales** uniquement pour `surtitre` (rayon, numéro d'étape), avec l'approche +0,08 em.
- **Longueur de ligne** : 45 caractères maximum dans les tiroirs, 60 dans les écrans papier.
- **Texte riche** : un sous-titre dans la même ligne = taille 80 %, `text/secondary`. Exemple TMP : `Fauteuil Lina<br><size=80%><color=#DDCDB5>Salon · Pose au sol</color></size>`.
- **Jamais** de texte sous 28 ref (≈ 12 dp).

## Mise en place dans TextMeshPro

1. `Window › TextMeshPro › Font Asset Creator`, police `Outfit_600SemiBold.ttf`.
2. Réglages : *Sampling Point Size* Auto, *Padding* 9, *Packing* Optimum, *Atlas* 1024 × 1024, *Render Mode* SDFAA, *Character Set* « Custom Characters » avec le contenu de `unity/Fonts/caracteres-fr.txt`.
3. Enregistrer sous `Resources/DecoAR/Fonts/Outfit-SemiBold SDF.asset`. Répéter pour Regular, Medium et Bold.
4. Dans `Project Settings › TextMesh Pro › Settings`, mettre **Outfit-Regular SDF** en police par défaut, avec les trois autres en *Fallback*.
5. Option : dans le font asset Regular, onglet *Font Weights*, brancher Medium / SemiBold / Bold pour que la balise `<b>` et `fontWeight` choisissent la bonne graisse.

## Équivalences natives

Si une partie de l'application est un jour reprise en natif (SwiftUI, Jetpack Compose, Flutter), utiliser la colonne « ≈ dp / pt » arrondie : display 31, titre-1 24, titre-2 20, titre-3 17, corps-l 16, corps 15, label 14, légende 12.
