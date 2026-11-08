# DecoAR · Design System v1.0

> Ton intérieur, avant de l'acheter.

Tout ce qu'il faut pour unifier l'apparence de DecoAR (Unity 6, iPhone et Android) : nouveau logo, icônes d'application, palette, typographie, icônes d'interface, composants, assets et code Unity.

**Commencer ici :** ouvre `design-system.html` dans un navigateur. C'est la planche visuelle de tout le système, sans connexion internet.

## Contenu

```
DecoAR-DesignSystem/
├── design-system.html        Planche visuelle complète (un seul fichier)
├── docs/                     Les règles, en Markdown
│   ├── 01-principes.md            Idée, principes, décisions d'unification
│   ├── 02-couleurs.md             Palette, thèmes Verre / Papier, contrastes, ancien → nouveau
│   ├── 03-typographie.md          Outfit, échelle, mise en place TextMeshPro
│   ├── 04-espacements-formes.md   Espaces, rayons, tailles, cibles tactiles, surfaces
│   ├── 05-composants.md           Spécifications de chaque composant
│   ├── 06-icones.md               Jeu d'icônes et règles
│   ├── 07-logo.md                 Symbole, fichiers, règles d'usage
│   ├── 08-icones-application.md   iOS, Android, Play Store, splash
│   ├── 09-redaction.md            Tutoiement, formats, bibliothèque de messages
│   ├── 10-mouvement-vibrations.md Durées, courbes, animations, vibrations
│   ├── 11-etats-et-ecrans.md      États, thème par écran, plans d'écran
│   └── 12-integration-unity.md    Pas à pas dans le projet Unity
├── logo/                     Symbole (image d'origine + découpes), horizontal, vertical, mot-symbole
├── app-icon/
│   ├── ios/AppIcon.appiconset/    Normale, teintée + Contents.json (Xcode)
│   ├── ios/tailles-unity/         Toutes les tailles pour Player Settings
│   ├── android/unity-adaptive/    Couches 432 px pour Unity
│   ├── android/res/               mipmaps de toutes les densités + XML adaptatif
│   └── android/play-store-512.png
├── icons/                    Icônes d'interface : svg/, sprite SVG, png-unity/64|128|256 (blanches, teintables)
├── assets/
│   ├── ar/                        Réticules (recherche, valide, refusé), motif de surface, logo de surface
│   ├── etats-vides/               8 illustrations + textes.json
│   ├── splash/                    Portrait et paysage
│   ├── store/                     Bannière Play Store 1024 × 500
│   ├── 3d/                        Texture de l'enseigne de l'étagère
│   ├── avatar/                    Avatar « Invité »
│   └── unity-sprites/             Sprites 9-slice (rayons 12 → 56, contours, cercles, ombres, voile)
├── tokens/                   decoar-tokens.json · decoar-tokens.css · decoar-palette.gpl (GIMP, Inkscape, Krita)
├── unity/
│   ├── Scripts/Runtime/           DecoTokens.cs (couleurs, typo, tailles, mouvement) · DecoUI.cs (composants)
│   ├── Scripts/Editor/            DecoSpriteImporter.cs (import automatique des sprites)
│   └── Fonts/caracteres-fr.txt    Jeu de caractères pour le Font Asset Creator
├── fonts/                    Outfit 400, 500, 600, 700 + licence OFL
└── _sources/                 Scripts Python qui génèrent tokens, logo, icônes et assets
```

## L'essentiel en 30 secondes

| | |
| --- | --- |
| **Logo** | « La Maison AR » : ton image, utilisée telle quelle (maison, fauteuil, coins de visée, anneau). |
| **Couleurs** | Terracotta `#DF5F2A` (marque) · Laiton `#D1A652` (essaie-le, prix) · Sauge `#5D7B59` (achète) · Lin `#FAF2E3` → `#1F1A14` (neutres chauds). |
| **Thèmes** | **Verre** sur la 3D et la caméra (M5, M3, M4) · **Papier** pour les formulaires (Auth, Profil, M1, M6). |
| **Police** | Outfit partout. Corps à 34 ref (≈ 14,5 dp), au lieu de 22 (≈ 9,5 dp). |
| **Cibles** | 104 ref minimum (≈ 44 dp), boutons du HUD à 112. |
| **Registre** | Tutoiement partout. |

## Points d'attention

- **Le passage des tailles** (texte et cibles) est le changement le plus visible. Il corrige un vrai problème de lisibilité, mais demande de revoir la mise en page du HUD et des tiroirs (tiroirs : 600 → 760 de large).
- **Les scripts C#** n'ont pas été compilés ici : à vérifier dans Unity 6 (ils utilisent `TextMeshPro` et `UnityEngine.UI`).
- **Les phrases de permissions** (`09-redaction.md`) parlent de la conservation des données : à faire correspondre à ce que fait réellement l'application.
