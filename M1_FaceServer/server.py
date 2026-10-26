"""Serveur DecoAR M1 : 3 photos du visage -> tete 3D FLAME de la personne (maillage + texture).

Lancement (sur le PC, meme Wi-Fi que le telephone) :
    .venv\\Scripts\\python.exe server.py
Le telephone envoie POST http://<ip-du-pc>:8000/head avec les photos front, left, right (JPEG).
"""
import base64
import time
import traceback
from pathlib import Path

import cv2
import numpy as np
import uvicorn
from fastapi import FastAPI, File, Form, HTTPException, UploadFile

from fit import View, detect, fit
from flame_model import Flame
from texture import bake

app = FastAPI(title="DecoAR M1 - tete FLAME")
flame = Flame()


def _read(data: bytes, mirrored: bool):
    image = cv2.imdecode(np.frombuffer(data, np.uint8), cv2.IMREAD_COLOR)
    if image is None:
        raise HTTPException(400, "image illisible")
    # La camera frontale s'affiche en miroir : on remet le visage a l'endroit
    return cv2.flip(image, 1) if mirrored else image


def build_head(images):
    views = []
    for image in images:
        points = detect(image)
        if points is not None:
            views.append(View(image=image, points=points))
    if not views:
        raise HTTPException(422, "aucun visage trouve sur les photos")

    result = fit(flame, views)
    texture, skin, hair = bake(flame, result, views, size=2048)
    return result, texture, skin, hair, len(views)


def to_unity(result, texture, skin, hair):
    """Un sommet par coin (position + UV), repere Unity (main gauche : x inverse, triangles retournes)."""
    v = result.vertices
    pairs = {}
    vertices, uvs, triangles = [], [], []
    for tri_v, tri_vt in zip(flame.obj_faces, flame.uv_faces):
        ids = []
        for vi, ti in zip(tri_v, tri_vt):
            key = (int(vi), int(ti))
            if key not in pairs:
                pairs[key] = len(vertices)
                x, y, z = v[vi]
                vertices.append([-float(x), float(y), float(z)])
                uvs.append([float(flame.uv[ti, 0]), float(flame.uv[ti, 1])])
            ids.append(pairs[key])
        triangles += [ids[0], ids[2], ids[1]]

    neck = flame.joint(v, 1)
    face = flame.masks.get("face")
    fv = v[face] if face is not None else v
    chin, brow = float(fv[:, 1].min()), float(fv[:, 1].max())
    # Bout du nez : point le plus en avant pres du milieu du visage
    mid = fv[np.abs(fv[:, 0]) < 0.01]
    nose = mid[np.argmax(mid[:, 2])]
    ok, png = cv2.imencode(".png", texture)
    bgr_to_rgb = lambda c: [float(c[2]) / 255, float(c[1]) / 255, float(c[0]) / 255]
    return {
        "vertices": sum(vertices, []),
        "uv": sum(uvs, []),
        "triangles": triangles,
        "neck": [-float(neck[0]), float(neck[1]), float(neck[2])],
        "top": float(v[:, 1].max()),
        "chin": chin,
        "brow": brow,
        "nose": [-float(nose[0]), float(nose[1]), float(nose[2])],
        "skin": bgr_to_rgb(skin),
        "hair": bgr_to_rgb(hair),
        "texture": base64.b64encode(png.tobytes()).decode("ascii"),
    }


@app.get("/")
def status():
    return {"ok": True, "model": "FLAME", "vertices": int(len(flame.v_template))}


LAST_REQUEST = Path(__file__).parent / "sortie" / "derniere_requete"


@app.post("/head")
async def head(front: UploadFile = File(...), left: UploadFile = File(None), right: UploadFile = File(None),
               mirrored: bool = Form(True)):
    start = time.time()
    images = [_read(await f.read(), mirrored) for f in (front, left, right) if f is not None]
    # Photos gardees sur le PC pour rejouer un probleme : python test_local.py sortie/derniere_requete/*.jpg
    LAST_REQUEST.mkdir(parents=True, exist_ok=True)
    for i, image in enumerate(images):
        cv2.imwrite(str(LAST_REQUEST / f"vue{i}.jpg"), image)
    print(f"[M1] {len(images)} photo(s) recue(s), {images[0].shape[1]}x{images[0].shape[0]}")
    try:
        result, texture, skin, hair, used = build_head(images)
        data = to_unity(result, texture, skin, hair)
    except HTTPException:
        raise
    except Exception:
        traceback.print_exc()
        raise HTTPException(500, traceback.format_exc(limit=3))
    data["views"] = used
    data["error"] = result.error
    print(f"[M1] tete creee en {time.time() - start:.1f} s, {used} vue(s), erreur {result.error:.3f}")
    return data


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
