# Sources

Scripts Python qui génèrent les fichiers du design system. Pour changer une couleur, modifier `tokens.py` ; pour une icône, `icons.py` ; puis relancer.

Prérequis : Python 3.10+, `pip install fonttools pillow numpy`, et `rsvg-convert` (librsvg).
Dans `common.py`, adapter `ROOT` ; dans `brand_raster.py`, adapter `SRC` (chemin de `orig.png`, l'image du symbole).

Ordre : `tokens.py <ROOT>/tokens` → `brand_raster.py` (logo + icônes d'app) → `icons.py` → `assets.py` → `docs_gen.py` → `planche.py`.
