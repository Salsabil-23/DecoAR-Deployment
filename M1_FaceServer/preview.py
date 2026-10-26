"""Apercu de sortie/tete.obj sans Unity : face, trois quarts et profil -> sortie/apercu.jpg"""
from pathlib import Path

import cv2
import numpy as np

from texture import _raster, _sample

out = Path(__file__).parent / "sortie"
v, vt, fv, ft = [], [], [], []
for line in open(out / "tete.obj", encoding="utf-8"):
    if line.startswith("v "):
        v.append([float(x) for x in line.split()[1:4]])
    elif line.startswith("vt "):
        vt.append([float(x) for x in line.split()[1:3]])
    elif line.startswith("f "):
        c = [p.split("/") for p in line.split()[1:4]]
        fv.append([int(p[0]) - 1 for p in c])
        ft.append([int(p[1]) - 1 for p in c])
v, vt, fv, ft = np.array(v), np.array(vt), np.array(fv), np.array(ft)
tex = cv2.imread(str(out / "tete.png"))
size = 512
views = []
for yaw in (0, 35, 80):
    a = np.radians(yaw)
    R = np.array([[np.cos(a), 0, np.sin(a)], [0, 1, 0], [-np.sin(a), 0, np.cos(a)]])
    p = (v - v.mean(0)) @ R.T
    scale = size * 0.8 / (p[:, 1].max() - p[:, 1].min())
    xy = np.stack([p[:, 0] * scale + size / 2, -p[:, 1] * scale + size / 2], 1)
    fid, bary, _ = _raster(xy[fv], (size, size), p[fv, 2])
    img = np.full((size, size, 3), 235, np.uint8)
    m = fid >= 0
    uv = (vt[ft[fid[m]]] * bary[m][:, :, None]).sum(1)
    px = np.stack([uv[:, 0] * tex.shape[1], (1 - uv[:, 1]) * tex.shape[0]], 1)
    img[m] = np.clip(_sample(tex, px), 0, 255).astype(np.uint8)
    views.append(img)
cv2.imwrite(str(out / "apercu.jpg"), np.concatenate(views, 1))
print(out / "apercu.jpg")
