"""Genere les PNG de l'icone DecoAR (meme dessin que decoar-icone.svg) pour Unity.

Lancement depuis le dossier du depot :
    M1_FaceServer\\.venv\\Scripts\\python.exe docs\\logo\\generer_icones.py
Les images vont dans Assets/DecoAR/Core/Icons ; le menu Unity "DecoAR > Appliquer l'icone de l'application"
les branche ensuite dans les Player Settings.
"""
from pathlib import Path

import cv2
import numpy as np

SIZE = 1024
SS = 4  # sur-echantillonnage pour des bords lisses
OUT = Path(__file__).resolve().parents[2] / "Assets" / "DecoAR" / "Core" / "Icons"

# Couleurs de l'app (BGR + alpha)
TERRACOTTA = (63, 96, 197, 255)
CREAM = (238, 246, 251, 255)
SAGE = (160, 194, 169, 255)

BRACKETS = [
    [(412, 317), (512, 230), (612, 317)],
    [(331, 388), (260, 450), (260, 540)],
    [(693, 388), (764, 450), (764, 540)],
    [(260, 700), (260, 800), (360, 800)],
    [(764, 700), (764, 800), (664, 800)],
]


def p(x, y):
    return int(round(x * SS)), int(round(y * SS))


def draw_symbol(img):
    """Maison en coins de visee AR, plan detecte et lampe posee."""
    width = 56 * SS
    for line in BRACKETS:
        pts = [p(*pt) for pt in line]
        for a, b in zip(pts, pts[1:]):
            cv2.line(img, a, b, CREAM, width, cv2.LINE_AA)
        for pt in pts:  # bouts et angles arrondis
            cv2.circle(img, pt, width // 2, CREAM, -1, cv2.LINE_AA)
    cv2.ellipse(img, p(512, 690), (132 * SS, 34 * SS), 0, 0, 360, SAGE, 18 * SS, cv2.LINE_AA)
    shade = np.array([p(474, 452), p(550, 452), p(598, 566), p(426, 566)], np.int32)
    cv2.fillPoly(img, [shade], CREAM, cv2.LINE_AA)
    cv2.rectangle(img, p(500, 566), p(524, 666), CREAM, -1, cv2.LINE_AA)
    cv2.ellipse(img, p(512, 676), (46 * SS, 14 * SS), 0, 0, 360, CREAM, -1, cv2.LINE_AA)


def canvas(fill=None):
    img = np.zeros((SIZE * SS, SIZE * SS, 4), np.uint8)
    if fill is not None:
        img[:] = fill
    return img


def mask_rounded(img, radius):
    mask = np.zeros(img.shape[:2], np.uint8)
    r, n = radius * SS, SIZE * SS
    cv2.rectangle(mask, (r, 0), (n - r, n), 255, -1)
    cv2.rectangle(mask, (0, r), (n, n - r), 255, -1)
    for cx, cy in ((r, r), (n - r, r), (r, n - r), (n - r, n - r)):
        cv2.circle(mask, (cx, cy), r, 255, -1, cv2.LINE_AA)
    img[:, :, 3] = (img[:, :, 3].astype(np.uint16) * mask // 255).astype(np.uint8)
    return img


def mask_circle(img):
    mask = np.zeros(img.shape[:2], np.uint8)
    cv2.circle(mask, (SIZE * SS // 2, SIZE * SS // 2), SIZE * SS // 2, 255, -1, cv2.LINE_AA)
    img[:, :, 3] = (img[:, :, 3].astype(np.uint16) * mask // 255).astype(np.uint8)
    return img


def save(img, name):
    small = cv2.resize(img, (SIZE, SIZE), interpolation=cv2.INTER_AREA)
    OUT.mkdir(parents=True, exist_ok=True)
    cv2.imwrite(str(OUT / name), small)
    print("ecrit", OUT / name)


def main():
    # Icone pleine (iOS refuse la transparence ; le systeme arrondit lui-meme)
    full = canvas(TERRACOTTA)
    draw_symbol(full)
    save(full, "DecoAR_Icon.png")
    # Android (Unity 6 n'a plus que l'icone adaptative) icone adaptative : fond uni + dessin sur fond transparent (dans la zone sure de 66 %)
    save(canvas(TERRACOTTA), "DecoAR_Icon_Background.png")
    # fond transparent mais couleur creme : les bords lisses ne virent pas au noir
    fg = canvas(CREAM[:3] + (0,))
    draw_symbol(fg)
    save(fg, "DecoAR_Icon_Foreground.png")


if __name__ == "__main__":
    main()
