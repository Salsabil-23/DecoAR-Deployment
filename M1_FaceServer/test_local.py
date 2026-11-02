"""Test sans telephone : python test_local.py face.jpg [gauche.jpg droite.jpg]
Ecrit tete.obj + tete.png (et cheveux.obj + cheveux.png) dans sortie/ (a ouvrir dans Blender ou Unity)."""
import sys
from pathlib import Path

import cv2
import numpy as np

from server import build_head, flame

images = [cv2.imread(p) for p in sys.argv[1:]]
if not images or any(i is None for i in images):
    sys.exit("usage : python test_local.py face.jpg [gauche.jpg droite.jpg]")

result, texture, skin, hair, used, hair_mesh = build_head(images)
out = Path(__file__).parent / "sortie"
out.mkdir(exist_ok=True)


def write_obj(name, vertices, faces, uv_faces, image):
    cv2.imwrite(str(out / f"{name}.png"), image)
    with open(out / f"{name}.obj", "w", encoding="utf-8") as f:
        f.write(f"mtllib {name}.mtl\nusemtl {name}\n")
        for x, y, z in vertices:
            f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
        for u, v in flame.uv:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        for tv, tt in zip(faces, uv_faces):
            f.write("f " + " ".join(f"{a + 1}/{b + 1}" for a, b in zip(tv, tt)) + "\n")
    (out / f"{name}.mtl").write_text(f"newmtl {name}\nmap_Kd {name}.png\n", encoding="utf-8")


from server import shape_neck
tucked, cut = shape_neck(result.vertices)
tucked[:, 1] = np.maximum(tucked[:, 1], cut)
keep = tucked[flame.obj_faces, 1].max(1) > cut + 1e-6
write_obj("tete", tucked, flame.obj_faces[keep], flame.uv_faces[keep], texture)
if hair_mesh is not None:
    write_obj("cheveux", hair_mesh["vertices"], hair_mesh["faces"], hair_mesh["uv_faces"], hair_mesh["texture"])
    print(f"cheveux : epaisseur {hair_mesh['thickness'] * 1000:.0f} mm")
elif (out / "cheveux.obj").exists():
    (out / "cheveux.obj").unlink()
print(f"{used} vue(s), erreur {result.error:.3f} -> {out}")
