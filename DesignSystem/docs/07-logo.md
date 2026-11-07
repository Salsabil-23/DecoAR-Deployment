# 07 · Logo

![Logo DecoAR](../logo/horizontal/decoar-logo-signature.png)

## Le symbole : « La Maison AR »

Le symbole est l'image choisie par l'équipe, **utilisée telle quelle, sans retouche ni redessin**. On l'a seulement découpée proprement pour chaque usage.

Ce qu'elle raconte :

- **La maison** : chez toi, l'intérieur qu'on aménage.
- **Le fauteuil** sur son **socle lumineux** : le meuble choisi dans la boutique, prêt à être posé.
- **Les coins de visée** blancs : la caméra AR qui cadre l'objet.
- **L'anneau en orbite** : l'objet en 3D, qu'on fait tourner et qu'on place.
- **La plaque pêche** et la lumière de fenêtre au fond : la chaleur de l'univers DecoAR.

## Fichiers

| Fichier | Usage |
| --- | --- |
| `symbole/decoar-icone-originale.png` | L'image d'origine (1254 × 1254), à conserver comme source |
| `symbole/decoar-symbole-NNN.png` | La plaque seule, coins transparents, **avec** ombre douce (1024 → 64) : splash, présentations, grands formats |
| `symbole/decoar-symbole-sans-ombre-NNN.png` | Même chose **sans** ombre : interface, en-têtes, petits formats |
| `horizontal/decoar-logo-horizontal(-sur-sombre)` | Symbole + « DecoAR » : connexion en paysage, bannières |
| `horizontal/decoar-logo-signature(-sur-sombre)` | Avec « ESSAIE · POSE · ADOPTE » : splash, store, présentation |
| `vertical/decoar-logo-vertical(-sur-sombre)` | Écrans portrait |
| `mot-symbole/` | « DecoAR » seul, vectoriel, quand le symbole est déjà à côté |

Les logos horizontaux et verticaux existent en **SVG** (image intégrée + texte vectorisé) et en **PNG** (512 px de haut).

## Le nom

« Deco » en encre `#29211A`, « **AR** » en terracotta `#DF5F2A` (crème et terracotta 300 sur fond sombre). Outfit SemiBold, lettres vectorisées. Signature facultative : **ESSAIE · POSE · ADOPTE**.

## Couleurs

La palette a été alignée sur le symbole : le terracotta de marque est passé de `#C86842` à **`#DF5F2A`**, l'orange du fauteuil et du toit. Voir `02-couleurs.md`.

## Règles

- **Ne pas** recadrer, recolorer, déformer, ajouter un contour ou faire tourner le symbole. Toujours la plaque entière.
- **Zone de protection** : au moins 25 % de la largeur du symbole tout autour.
- **Tailles minimales** : 40 px pour le symbole dans l'interface. En dessous (favicon, notifications), le détail se perd : utiliser le mot-symbole ou l'icône d'app système.
- **Fonds** : lin clair, encre ou photo sombre. Le symbole a sa propre plaque : pas besoin de pastille derrière.
- **Dans l'application** : l'ancien logo rond « D » de M6 et l'ancien logo maison + cube sont remplacés par `decoar-symbole-sans-ombre-256.png`.

## Limites d'une image (et solutions)

Le symbole est une image, pas un dessin vectoriel. Ça a deux conséquences :

- **Pas de version une couleur.** L'icône teintée d'iOS 18 est une version en niveaux de gris de l'image (fournie). L'icône monochrome d'Android 13 n'est pas fournie : Android affiche alors l'icône normale.
- **Agrandissement limité** à ~1250 px (taille de l'image d'origine). Pour une affiche ou un grand panneau, régénérer l'image en plus haute définition depuis l'outil qui l'a créée.
