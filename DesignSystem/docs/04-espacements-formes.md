# 04 · Espacements, formes et surfaces

Toutes les valeurs sont en points du Canvas (ref). ≈ dp = ref × 0.426.

## Espacements (base 8, avec un pas de 12)

| Token | Ref | ≈ dp | Usage typique |
| --- | --- | --- | --- |
| `space-0` | 0 | 0 |  |
| `space-1` | 8 | 3 | Icône ↔ texte serré |
| `space-2` | 12 | 5 | Icône ↔ texte dans un bouton, puces |
| `space-3` | 16 | 7 | Entre lignes d'un formulaire compact |
| `space-4` | 24 | 10 | Padding interne des cartes, entre boutons du HUD |
| `space-5` | 32 | 14 | Padding des tiroirs |
| `space-6` | 48 | 20 | Marge d'écran, padding horizontal des boutons |
| `space-7` | 64 | 27 | Entre sections |
| `space-8` | 96 | 41 | Grands vides (états vides) |
| `space-9` | 128 | 55 | Écran d'accueil, splash |

## Rayons

| Token | Ref | Usage |
| --- | --- | --- |
| `radius-xs` | 12 | Petites vignettes, pastilles carrées |
| `radius-sm` | 20 | Lignes de liste, tuiles, champs sur verre |
| `radius-md` | 28 | Cartes produit, champs papier, cadres |
| `radius-lg` | 40 | Tiroirs, fiche produit, fenêtres, visionneuse 3D |
| `radius-xl` | 56 | Grandes cartes papier, feuilles du bas |
| `radius-pill` | ∞ | Boutons, pastilles, onglets, toasts (rayon = moitié de la hauteur) |

Les rayons reprennent la courbe de l'arche du logo : **tout ce qui contient un objet (carte, cadre 3D, fiche) a des coins généreux ; tout ce qu'on touche est une pilule.**

## Tailles clés

| Token | Ref | ≈ dp |
| --- | --- | --- |
| `touch-min` | 104 | 44 |
| `hud-button` | 112 | 48 |
| `icon-button` | 96 | 41 |
| `button-height` | 112 | 48 |
| `button-height-compact` | 88 | 37 |
| `field-height` | 104 | 44 |
| `swatch` | 88 | 37 |
| `badge` | 40 | 17 |
| `icon-sm` | 40 | 17 |
| `icon-md` | 52 | 22 |
| `icon-lg` | 64 | 27 |
| `screen-margin` | 48 | 20 |
| `gap-hud` | 20 | 9 |
| `drawer-width` | 760 | 324 |
| `product-card` | 360 × 184 | 153 × 78 |
| `stroke-hairline` | 2.5 | 1 |
| `stroke-icon` | 2 | 1 |

**Cible tactile minimum : 104 ref (≈ 44 dp).** Un élément visuellement plus petit (pastille de 40, bouton icône de 88) garde une zone tactile invisible de 104 : en uGUI, un `Image` transparent (alpha 0, *Raycast Target* activé) parent de l'élément visible.

## Grille et zones sûres

- Marge d'écran : 48 ref **plus** la zone sûre de l'appareil (`Screen.safeArea`). Le HUD est posé dans un `RectTransform` ajusté à la zone sûre.
- Paysage : HUD en deux groupes (gauche / droite) en haut, actions en bas au centre, tiroirs de 760 de large.
- Portrait (Auth, M1, M6, AR) : une colonne, contenu centré, largeur max 960 ; les actions principales en bas, au-dessus de la barre de geste.
- iPhone : laisser 34 pt (≈ 80 ref) libres en bas pour l'indicateur d'accueil ; ne rien placer d'important sous l'îlot dynamique.

## Surfaces et élévation

| Niveau | Thème | Recette |
| --- | --- | --- |
| Verre | Verre | Fond `lin.950` 80 %, liseré blanc 12 % de 2,5, ombre noire 38 % décalée de 6, flou 16 (sprite `dc-ombre-r28`) |
| Verre fort | Verre | Fond `lin.950` 94 % : tiroirs, fiche produit, fenêtres |
| Tuile | Verre | Blanc 7 % (16 % si active), rayon sm : lignes de liste, champs |
| Flottant | Verre | Ombre noire 45 %, décalage 12, flou 32 : boutons qui « respirent », toasts |
| Carte | Papier | Blanc, liseré `lin.200`, ombre `lin.900` 10 % décalée de 4, flou 16 |
| Creux | Papier | `lin.150`, sans ombre : champs, zones de dépôt |

**Flou d'arrière-plan.** uGUI ne floute pas nativement. Le design system ne l'exige pas (le verre à 80 % suffit). Si l'équipe veut un vrai verre dépoli en URP, ajouter un *Renderer Feature* qui copie l'image de la caméra floutée à faible résolution, et l'utiliser comme texture du fond de verre ; garder alors l'opacité à 60 %.

## Voile assombri

Derrière un tiroir ou une fenêtre : noir 40 % (`surface/scrim`), fondu 240 ms. Toucher le voile ferme le tiroir.
