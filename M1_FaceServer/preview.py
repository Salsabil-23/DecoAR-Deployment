"""Apercu de sortie/tete.obj (+ cheveux.obj) sans Unity : face, trois quarts et profil -> sortie/apercu.jpg"""
from pathlib import Path

import cv2
import numpy as np

from texture import _raster, _sample

out = Path(__file__).parent / "sortie"


def load(name):
    v, vt, fv, ft = [], [], [], []
    for line in open(out / f"{name}.obj", encoding="utf-8"):
        if line.startswith("v "):
            v.append([float(x) for x in line.split()[1:4]])
        elif line.startswith("vt "):
            vt.append([float(x) for x in line.split()[1:3]])
        elif line.startswith("f "):
            c = [p.split("/") for p in line.split()[1:4]]
            fv.append([int(p[0]) - 1 for p in c])
            ft.append([int(p[1]) - 1 for p in c])
    tex = cv2.imread(str(out / f"{name}.png"), cv2.IMREAD_UNCHANGED)
    return np.array(v), np.array(vt), np.array(fv), np.array(ft), tex


def render(mesh, R, center, scale, size):
    v, vt, fv, ft, tex = mesh
    p = (v - center) @ R.T
    xy = np.stack([p[:, 0] * scale + size / 2, -p[:, 1] * scale + size / 2], 1)
    fid, bary, zbuf = _raster(xy[fv], (size, size), p[fv, 2])
    m = fid >= 0
    uv = (vt[ft[fid[m]]] * bary[m][:, :, None]).sum(1)
    px = np.stack([uv[:, 0] * tex.shape[1], (1 - uv[:, 1]) * tex.shape[0]], 1)
    color = np.zeros((size, size, tex.shape[2]), np.float32)
    color[m] = _sample(tex, px)
    return color, np.where(m, zbuf, -np.inf)


head = load("tete")
hair = load("cheveux") if (out / "cheveux.obj").exists() else None
center = head[0].mean(0)
size = 512
scale = size * 0.75 / (head[0][:, 1].max() - head[0][:, 1].min())
views = []
for yaw in (0, 35, 80):
    a = np.radians(yaw)
    R = np.array([[np.cos(a), 0, np.sin(a)], [0, 1, 0], [-np.sin(a), 0, np.cos(a)]])
    img = np.full((size, size, 3), 235, np.float32)
    color, z = render(head, R, center, scale, size)
    img[np.isfinite(z)] = color[np.isfinite(z)][:, :3]
    if hair is not None:
        hc, hz = render(hair, R, center, scale, size)
        on = np.isfinite(hz) & (hz >= z - 1e-4) & (hc[:, :, 3] > 127)   # decoupe alpha comme dans Unity
        img[on] = hc[on][:, :3]
    views.append(np.clip(img, 0, 255).astype(np.uint8))
cv2.imwrite(str(out / "apercu.jpg"), np.concatenate(views, 1))
print(out / "apercu.jpg")
