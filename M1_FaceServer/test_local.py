"""Test sans telephone : python test_local.py face.jpg [gauche.jpg droite.jpg]
Ecrit tete.obj + tete.png dans le dossier sortie/ (a ouvrir dans Blender ou Unity)."""
import sys
from pathlib import Path

import cv2

from server import build_head, flame

images = [cv2.imread(p) for p in sys.argv[1:]]
if not images or any(i is None for i in images):
    sys.exit("usage : python test_local.py face.jpg [gauche.jpg droite.jpg]")

result, texture, skin, hair, used = build_head(images)
out = Path(__file__).parent / "sortie"
out.mkdir(exist_ok=True)
cv2.imwrite(str(out / "tete.png"), texture)
with open(out / "tete.obj", "w", encoding="utf-8") as f:
    f.write("mtllib tete.mtl\nusemtl tete\n")
    for x, y, z in result.vertices:
        f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
    for u, v in flame.uv:
        f.write(f"vt {u:.6f} {v:.6f}\n")
    for tv, tt in zip(flame.obj_faces, flame.uv_faces):
        f.write("f " + " ".join(f"{a + 1}/{b + 1}" for a, b in zip(tv, tt)) + "\n")
(out / "tete.mtl").write_text("newmtl tete\nmap_Kd tete.png\n", encoding="utf-8")
print(f"{used} vue(s), erreur {result.error:.3f} -> {out}")
