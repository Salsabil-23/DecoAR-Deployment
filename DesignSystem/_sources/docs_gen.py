"""Génère les docs riches en données (couleurs, typo, espacements, icônes) à partir des tokens."""
from common import *
import tokens as T
from icons import ICONS

D = f"{ROOT}/docs"
R = T.REF_TO_DP

def sw(h):  # pastille couleur visible sur GitHub / VS Code
    return f"![{h}](https://placehold.co/18x18/{h[1:]}/{h[1:]}.png)"

# ===================================================================== 02 COULEURS
def couleurs():
    L = ["# 02 · Couleurs", "",
         "Une seule palette pour toute l'application. Elle remplace les couleurs du logo, du HUD, de M6 et d'Auth, "
         "qui avaient chacune leurs variantes (deux terracotta, deux crèmes, deux sauges).", "",
         "La palette a deux niveaux :", "",
         "- **Primitives** : les teintes brutes, numérotées de 50 (très clair) à 900 (très foncé). On ne les utilise pas directement dans les écrans.",
         "- **Sémantiques** : des rôles (« texte principal », « action principale »…) qui pointent vers une primitive, différente selon le thème. **Les écrans n'utilisent que les rôles.**", "",
         "## 1. Primitives", ""]
    roles = {
        "terracotta": "Couleur de marque. Logo, action principale du thème papier, accents.",
        "sauge": "Le vivant, le « oui » : ajouter au panier, payer, interrupteur actif.",
        "laiton": "La lumière, le précieux : action principale sur verre (AR), prix, sélection.",
        "lin": "Neutres chauds : fonds, textes, verre. Jamais de gris froid ni de noir pur.",
        "brique": "Pastille compteur, erreurs.",
        "ardoise": "Information neutre (rare).",
        "succes": "Confirmations, « Taille réelle ».",
    }
    for fam, steps in T.PALETTE.items():
        L += [f"### {dict(succes='Succès').get(fam, fam.capitalize())}", "", roles[fam], "", "| Pas | Hex | |", "| --- | --- | --- |"]
        for s, h in steps.items():
            star = " ★ référence" if (fam, s) in [("terracotta", "500"), ("sauge", "500"), ("laiton", "500"), ("lin", "100"), ("lin", "950")] else ""
            L.append(f"| {s} | `{h}` | {sw(h)}{star} |")
        L.append("")
    L += ["### Types de pose (pastilles des cartes produit)", "", "| Pose | Hex | |", "| --- | --- | --- |"]
    for k, h in T.POSE.items(): L.append(f"| {k} | `{h}` | {sw(h)} |")
    L += ["", "Ces trois couleurs sont **toujours accompagnées de l'icône** `pose-sol`, `pose-mur` ou `pose-table` : la couleur seule ne suffit pas (daltonisme).", "",
          "### Dégradés", "", "| Nom | De → à | Usage |", "| --- | --- | --- |"]
    use = {"halo-ar": "Moments « magiques » : génération 3D, première pose AR, bouton AR pressé.",
           "aube": "Fond des écrans papier (splash, connexion, personnage).",
           "braise": "Grands aplats de marque (icône, bannière)."}
    for k, (a, b) in T.GRADIENTS.items(): L.append(f"| {k} | `{a}` → `{b}` | {use[k]} |")
    L += ["", "## 2. Les deux thèmes", "",
          "| Thème | Où | Idée |", "| --- | --- | --- |",
          "| **Verre** | Boutique M5, catalogue M3, AR M4, HUD, tiroirs, fiche produit | Verre fumé chaud au-dessus de la 3D ou de la caméra. Texte crème. |",
          "| **Papier** | Connexion, inscription, profil, personnage M1, administration M6 | Lin clair, cartes blanches, texte encre. |", "",
          "Règle simple : **si ça flotte sur une image vivante, c'est Verre ; si c'est un écran à soi, c'est Papier.**", "",
          "### Rôles sémantiques", "", "| Rôle | Verre | Papier |", "| --- | --- | --- |"]
    keys = list(dict.fromkeys(list(T.THEMES["verre"]) + list(T.THEMES["papier"])))
    def cell(t, k):
        if k not in T.THEMES[t]: return "—"
        h, a = T.THEMES[t][k]
        return f"`{h}`" + (f" à {round(a*100)} %" if a < 1 else "")
    for k in keys: L.append(f"| `{k}` | {cell('verre', k)} | {cell('papier', k)} |")
    L += ["", "### Hiérarchie des actions", "",
          "| Action | Verre | Papier | Exemples |", "| --- | --- | --- | --- |",
          "| Principale | Laiton + texte encre | Terracotta 600 + texte blanc | « Voir chez moi en AR », « Se connecter », « + Générer un modèle 3D » |",
          "| Commerce | Sauge 600 + texte blanc | Sauge 600 + texte blanc | « Ajouter au panier », « Payer · 1 890 TND › » |",
          "| Accent | Terracotta 400 | Laiton | Micro actif, étape en cours |",
          "| Secondaire | Blanc 12 % + texte crème | Lin 150 + texte encre | « Reposer sur l'étagère », « Galerie » |",
          "| Fantôme | Texte crème seul | Texte terracotta 700 | « Mot de passe oublié ? », « Vider le panier » |", "",
          "Une seule action principale par écran. Sauge est réservé à l'argent et au panier : l'utilisateur apprend que « vert = j'achète ».", "",
          "## 3. Contrastes vérifiés", "",
          "Calculés selon WCAG 2.2. Pour le verre, on prend le **pire cas** : verre posé sur une image blanche (mur clair filmé en AR).", "",
          "| Thème | Texte | Sur | Ratio | Niveau |", "| --- | --- | --- | --- | --- |"]
    for t, fg, fgh, bgl, bgh, c, lvl in T.contrast_report():
        L.append(f"| {t} | `{fg}` `{fgh}` | {bgl} `{bgh}` | {c:.2f} | {lvl} |")
    L += ["", "**Règles qui en découlent :**", "",
          "- Sur verre, `text/tertiary` et les couleurs de retour (succès, erreur) ne servent qu'au texte ≥ 34 ref en semi-gras, ou à un point / une icône accompagné d'un texte crème.",
          "- **Aucun texte directement sur la caméra ou la 3D.** Toujours une surface de verre dessous (opacité ≥ 80 %), ou le voile `dc-voile-haut` pour les bandeaux.",
          "- Le prix sur verre est en laiton 300 (pas 500) pour garder 4,5:1.", "",
          "## 4. Ancien → nouveau", "",
          "| Avant | Après |", "| --- | --- |",
          "| Terracotta logo `#C5603F` / interface `#C7734A` | `terracotta.500` `#DF5F2A` (orange du symbole) |",
          "| Crème `#FBF6EE` / `#FAF2E3` / `#FAF7F2` | `lin.50` `#FDF9F3` (fond papier), `lin.100` `#FAF2E3` (texte sur verre) |",
          "| Sauge `#A9C2A0` / `#73946E` | `sauge.300` / `sauge.500` (boutons : `sauge.600`) |",
          "| Laiton `#D1A652` | `laiton.500` (inchangé) |",
          "| Verre `#1F1A14` à 72 % | `lin.950` à **80 %** (lisibilité) |",
          "| Doux `#B8A38F` | `lin.300` `#DDCDB5` (texte secondaire sur verre) |",
          "| Rouge brique `#D15C45` | `brique.600` `#B94A35` (pastille : blanc lisible) |",
          "| Fond M6 `#2B241C` | M6 passe en thème Papier (`lin.50`) |",
          "| Succès `#9ECC94` | `succes.300` (inchangé) |", "",
          "## 5. Décor 3D", "",
          "Les matériaux 3D du catalogue reprennent la palette (sRGB, à saisir tels quels dans les matériaux URP) :", "",
          "| Élément | Primitive | Hex |", "| --- | --- | --- |",
          f"| Mur | lin.200 | `{T.P('lin.200')}` |", f"| Étagère laquée | lin.150 | `{T.P('lin.150')}` |",
          f"| Fond d'étagère | sauge.500 | `{T.P('sauge.500')}` |", f"| Panneau | terracotta.500 | `{T.P('terracotta.500')}` |",
          f"| Enseigne | lin.900 + texture `assets/3d/enseigne-etagere.png` | `{T.P('lin.900')}` |",
          f"| Liseré, rubans lumineux | laiton.500 (émissif ×1,5) | `{T.P('laiton.500')}` |"]
    write(f"{D}/02-couleurs.md", "\n".join(L) + "\n")

# ===================================================================== 03 TYPO
def typo():
    L = ["# 03 · Typographie", "",
         "**Outfit partout**, la police du logo. Géométrique et douce, elle a les mêmes courbes que l'arche du symbole. "
         "Licence SIL OFL : gratuite, utilisable dans une application commerciale (fichier `fonts/OFL.txt`).", "",
         "Graisses fournies : Regular 400, Medium 500, SemiBold 600, Bold 700 (`fonts/`).", "",
         "## Pourquoi les tailles augmentent", "",
         "Le Canvas est en 1920 × 1080 avec un équilibre 0,5. Sur un téléphone 20:9 typique (2400 × 1080 px, densité 2,625), "
         f"**1 point du Canvas ≈ {R} dp**. L'ancien texte courant (22) faisait donc **≈ 9,5 dp**, et l'ancienne cible tactile (64) **≈ 27 dp**. "
         "Apple recommande 17 pt pour le corps et 44 pt pour une cible ; Android 14 sp et 48 dp.", "",
         "La nouvelle échelle remonte tout au niveau des recommandations iOS et Android, tout en restant compacte pour le HUD paysage.", "",
         "## Échelle", "",
         "| Style | Taille (ref) | ≈ dp / pt | Graisse | Interligne | Approche | Usage |",
         "| --- | --- | --- | --- | --- | --- | --- |"]
    for k, (s, w, lh, ls, u) in T.TYPE.items():
        L.append(f"| `{k}` | {s} | {s*R:.1f} | {w} | {lh} | {ls:+g} em | {u} |")
    L += ["", "## Règles", "",
          "- **Chiffres** : prix, quantités, dimensions et chronomètre en chiffres tabulaires (balise TMP `<mspace=0.6em>` sur les chiffres ou police Outfit Medium, dont les chiffres sont presque à chasse fixe).",
          "- **Capitales** uniquement pour `surtitre` (rayon, numéro d'étape), avec l'approche +0,08 em.",
          "- **Longueur de ligne** : 45 caractères maximum dans les tiroirs, 60 dans les écrans papier.",
          "- **Texte riche** : un sous-titre dans la même ligne = taille 80 %, `text/secondary`. Exemple TMP : `Fauteuil Lina<br><size=80%><color=#DDCDB5>Salon · Pose au sol</color></size>`.",
          "- **Jamais** de texte sous 28 ref (≈ 12 dp).", "",
          "## Mise en place dans TextMeshPro", "",
          "1. `Window › TextMeshPro › Font Asset Creator`, police `Outfit_600SemiBold.ttf`.",
          "2. Réglages : *Sampling Point Size* Auto, *Padding* 9, *Packing* Optimum, *Atlas* 1024 × 1024, *Render Mode* SDFAA, "
          "*Character Set* « Custom Characters » avec le contenu de `unity/Fonts/caracteres-fr.txt`.",
          "3. Enregistrer sous `Resources/DecoAR/Fonts/Outfit-SemiBold SDF.asset`. Répéter pour Regular, Medium et Bold.",
          "4. Dans `Project Settings › TextMesh Pro › Settings`, mettre **Outfit-Regular SDF** en police par défaut, avec les trois autres en *Fallback*.",
          "5. Option : dans le font asset Regular, onglet *Font Weights*, brancher Medium / SemiBold / Bold pour que la balise `<b>` et `fontWeight` choisissent la bonne graisse.", "",
          "## Équivalences natives", "",
          "Si une partie de l'application est un jour reprise en natif (SwiftUI, Jetpack Compose, Flutter), utiliser la colonne « ≈ dp / pt » arrondie : "
          "display 31, titre-1 24, titre-2 20, titre-3 17, corps-l 16, corps 15, label 14, légende 12."]
    write(f"{D}/03-typographie.md", "\n".join(L) + "\n")
    chars = (" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"
             "ÀÂÄÆÇÉÈÊËÎÏÔŒÙÛÜŸàâäæçéèêëîïôœùûüÿ«»‹›…–—’‘“”•·×÷°€±²³½¼¾ºª№←→↑↓✓●○ ")
    write(f"{ROOT}/unity/Fonts/caracteres-fr.txt", chars + "\n")

# ===================================================================== 04 ESPACES
def espaces():
    L = ["# 04 · Espacements, formes et surfaces", "", f"Toutes les valeurs sont en points du Canvas (ref). ≈ dp = ref × {R}.", "",
         "## Espacements (base 8, avec un pas de 12)", "", "| Token | Ref | ≈ dp | Usage typique |", "| --- | --- | --- | --- |"]
    use = {"0": "", "1": "Icône ↔ texte serré", "2": "Icône ↔ texte dans un bouton, puces", "3": "Entre lignes d'un formulaire compact",
           "4": "Padding interne des cartes, entre boutons du HUD", "5": "Padding des tiroirs", "6": "Marge d'écran, padding horizontal des boutons",
           "7": "Entre sections", "8": "Grands vides (états vides)", "9": "Écran d'accueil, splash"}
    for k, v in T.SPACE.items(): L.append(f"| `space-{k}` | {v} | {v*R:.0f} | {use[k]} |")
    L += ["", "## Rayons", "", "| Token | Ref | Usage |", "| --- | --- | --- |"]
    ru = {"xs": "Petites vignettes, pastilles carrées", "sm": "Lignes de liste, tuiles, champs sur verre", "md": "Cartes produit, champs papier, cadres",
          "lg": "Tiroirs, fiche produit, fenêtres, visionneuse 3D", "xl": "Grandes cartes papier, feuilles du bas", "pill": "Boutons, pastilles, onglets, toasts (rayon = moitié de la hauteur)"}
    for k, v in T.RADIUS.items(): L.append(f"| `radius-{k}` | {v if v < 999 else '∞'} | {ru[k]} |")
    L += ["", "Les rayons reprennent la courbe de l'arche du logo : **tout ce qui contient un objet (carte, cadre 3D, fiche) a des coins généreux ; tout ce qu'on touche est une pilule.**", "",
          "## Tailles clés", "", "| Token | Ref | ≈ dp |", "| --- | --- | --- |"]
    for k, v in T.SIZE.items():
        if isinstance(v, list): L.append(f"| `{k}` | {v[0]} × {v[1]} | {v[0]*R:.0f} × {v[1]*R:.0f} |")
        else: L.append(f"| `{k}` | {v} | {v*R:.0f} |")
    L += ["", "**Cible tactile minimum : 104 ref (≈ 44 dp).** Un élément visuellement plus petit (pastille de 40, bouton icône de 88) garde une zone tactile invisible de 104 : "
          "en uGUI, un `Image` transparent (alpha 0, *Raycast Target* activé) parent de l'élément visible.", "",
          "## Grille et zones sûres", "",
          "- Marge d'écran : 48 ref **plus** la zone sûre de l'appareil (`Screen.safeArea`). Le HUD est posé dans un `RectTransform` ajusté à la zone sûre.",
          "- Paysage : HUD en deux groupes (gauche / droite) en haut, actions en bas au centre, tiroirs de 760 de large.",
          "- Portrait (Auth, M1, M6, AR) : une colonne, contenu centré, largeur max 960 ; les actions principales en bas, au-dessus de la barre de geste.",
          "- iPhone : laisser 34 pt (≈ 80 ref) libres en bas pour l'indicateur d'accueil ; ne rien placer d'important sous l'îlot dynamique.", "",
          "## Surfaces et élévation", "",
          "| Niveau | Thème | Recette |", "| --- | --- | --- |",
          "| Verre | Verre | Fond `lin.950` 80 %, liseré blanc 12 % de 2,5, ombre noire 38 % décalée de 6, flou 16 (sprite `dc-ombre-r28`) |",
          "| Verre fort | Verre | Fond `lin.950` 94 % : tiroirs, fiche produit, fenêtres |",
          "| Tuile | Verre | Blanc 7 % (16 % si active), rayon sm : lignes de liste, champs |",
          "| Flottant | Verre | Ombre noire 45 %, décalage 12, flou 32 : boutons qui « respirent », toasts |",
          "| Carte | Papier | Blanc, liseré `lin.200`, ombre `lin.900` 10 % décalée de 4, flou 16 |",
          "| Creux | Papier | `lin.150`, sans ombre : champs, zones de dépôt |", "",
          "**Flou d'arrière-plan.** uGUI ne floute pas nativement. Le design system ne l'exige pas (le verre à 80 % suffit). "
          "Si l'équipe veut un vrai verre dépoli en URP, ajouter un *Renderer Feature* qui copie l'image de la caméra floutée à faible résolution, et l'utiliser comme texture du fond de verre ; garder alors l'opacité à 60 %.",
          "", "## Voile assombri", "", "Derrière un tiroir ou une fenêtre : noir 40 % (`surface/scrim`), fondu 240 ms. Toucher le voile ferme le tiroir."]
    write(f"{D}/04-espacements-formes.md", "\n".join(L) + "\n")

# ===================================================================== 06 ICONES
def icones():
    groups = {}
    for n, (label, g, _) in ICONS.items(): groups.setdefault(g, []).append((n, label))
    L = ["# 06 · Icônes", "", f"{len(ICONS)} icônes dessinées pour DecoAR : les 26 icônes existantes redessinées, plus celles qui manquaient (compte, retours, poses, réglages).", "",
         "![Aperçu sur verre](../icons/apercu-icones-sombre.png)", "",
         "## Construction", "",
         "- Grille **24 × 24**, zone utile 20 × 20 (2 de marge).",
         "- Trait **2**, extrémités et angles **arrondis**, pas de remplissage (sauf petits points).",
         "- Formes douces, coins arrondis ≈ 2 : même famille que l'arche du logo et que la police Outfit.",
         "- Monochromes : la couleur vient du thème (`icon/default`, ou la couleur de l'action).",
         "- Signature DecoAR : **l'anneau pointillé** de l'AR (logo) revient dans `ar`, `vue-3e` et `geste-double`.", "",
         "## Fichiers", "",
         "| Dossier | Contenu |", "| --- | --- |",
         "| `icons/svg/` | Une icône par fichier, couleur `currentColor` (web, Figma) |",
         "| `icons/decoar-icons-sprite.svg` | Toutes les icônes en `<symbol>` : `<svg><use href=\"#dc-panier\"/></svg>` |",
         "| `icons/png-unity/64, 128, 256/` | PNG **blancs** pour Unity : on teinte avec `Image.color` |", "",
         "Unity : prendre le dossier 128 (net jusqu'à 64 ref à l'écran) ; 256 pour les grandes illustrations de fenêtres d'aide.", "",
         "## Tailles d'affichage", "", "| Token | Ref | Usage |", "| --- | --- | --- |",
         "| `icon-sm` | 40 | Dans un champ, une ligne de liste, une étiquette |", "| `icon-md` | 52 | Boutons, boutons ronds du HUD |", "| `icon-lg` | 64 | Outils AR, guide des gestes |", "",
         "## Catalogue", ""]
    for g, items in groups.items():
        L += [f"### {g}", "", "| Nom (fichier) | Signification |", "| --- | --- |"]
        for n, label in items: L.append(f"| `{n}` | {label} |")
        L.append("")
    L += ["## Correspondance avec l'ancien jeu", "",
          "| Ancienne icône | Nouvelle |", "| --- | --- |",
          "| Retour ‹ / Chevron › / Fermer × | `retour` / `chevron` / `fermer` |", "| Panier (sac) | `panier` |", "| Catalogue (grille) | `catalogue` |",
          "| Suggestions (étincelles) | `suggestions` |", "| Profil | `profil` |", "| Réglages (curseurs) | `reglages` |", "| Recentrer (viseur) | `recentrer` |",
          "| Position | `position` |", "| Œil / Personnage | `vue-1re` / `vue-3e` |", "| Plus / Moins | `plus` / `moins` |", "| Loupe / Micro | `loupe` / `micro` (+ `micro-actif`) |",
          "| Cube AR | `ar` |", "| Photo | `photo` |", "| Coloris | `coloris` |", "| Taille | `taille` (+ `taille-reelle`) |",
          "| Déplacer / Tourner / Pincer / Toucher | `geste-deplacer` / `geste-tourner` / `geste-pincer` / `geste-toucher` |", "",
          "## Règles", "",
          "- Une icône seule (sans texte) doit être **universelle** : retour, fermer, panier, loupe, plus/moins. Toutes les autres ont une étiquette (outils AR) ou une info-bulle d'aide la première fois.",
          "- État actif : l'icône passe en `state/selected` (laiton sur verre, terracotta sur papier) et la tuile derrière passe à 16 %.",
          "- Pas d'icônes en couleurs multiples dans l'interface ; la couleur est réservée aux illustrations (`assets/etats-vides`)."]
    write(f"{D}/06-icones.md", "\n".join(L) + "\n")

if __name__ == "__main__":
    couleurs(); typo(); espaces(); icones(); print("ok")
