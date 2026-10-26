"""Chargement du modele FLAME (tete 3D complete : crane, oreilles, cou, yeux).

Fichiers a deposer dans M1_FaceServer/flame/ (telecharges sur https://flame.is.tue.mpg.de) :
  - flame2023_Open.pkl (FLAME 2023 Open, CC-BY-4.0) ou generic_model.pkl (FLAME 2020)
  - head_template_mesh.obj          (facultatif : UV officielles, archive "FLAME texture space" ;
                                     sans lui, les UV sont generees)
  - FLAME_masks.pkl                 (regions : crane, visage, oreilles...)
  - mediapipe_landmark_embedding.npz (correspondance MediaPipe -> FLAME)
"""
import pickle
import sys
import types
from pathlib import Path

import numpy as np

FLAME_DIR = Path(__file__).parent / "flame"


# Les .pkl FLAME 2020 contiennent des objets chumpy (bibliotheque abandonnee) : on les relit comme des tableaux
class _Ch:
    def __setstate__(self, state):
        self.__dict__.update(state)


def _install_chumpy_stub():
    if "chumpy" in sys.modules:
        return
    for name in ("chumpy", "chumpy.ch", "chumpy.ch_ops", "chumpy.reordering", "chumpy.utils", "chumpy.logic"):
        mod = types.ModuleType(name)
        mod.__getattr__ = lambda attr: _Ch
        sys.modules[name] = mod


def _array(obj):
    if isinstance(obj, _Ch):
        for key in ("x", "a"):
            if key in obj.__dict__:
                return _array(obj.__dict__[key])
        raise ValueError("objet chumpy illisible")
    if hasattr(obj, "toarray"):  # matrice creuse scipy
        return obj.toarray()
    return np.asarray(obj)


def _load_pickle(path):
    _install_chumpy_stub()
    with open(path, "rb") as f:
        return pickle.load(f, encoding="latin1")


class Flame:
    def __init__(self, n_shape=100, n_expr=20):
        # FLAME 2023 Open (CC-BY-4.0) de preference, sinon FLAME 2020 / 2023
        names = ("flame2023_Open.pkl", "flame2023_open.pkl", "flame2023.pkl", "generic_model.pkl")
        model_path = next((FLAME_DIR / n for n in names if (FLAME_DIR / n).exists()), None)
        if model_path is None:
            raise FileNotFoundError(f"Modele FLAME introuvable dans {FLAME_DIR}")
        m = _load_pickle(model_path)

        self.v_template = _array(m["v_template"]).astype(np.float32)        # (5023, 3) metres
        self.faces = _array(m["f"]).astype(np.int64)                         # (9976, 3)
        shapedirs = _array(m["shapedirs"]).astype(np.float32)                # (5023, 3, 400)
        self.shape_dirs = shapedirs[:, :, :n_shape]
        self.expr_dirs = shapedirs[:, :, 300:300 + n_expr]
        self.joints = _array(m["J_regressor"]).astype(np.float32)            # (5, 5023)
        self.n_shape, self.n_expr = n_shape, n_expr

        self._load_masks()
        self._load_landmarks()
        if (FLAME_DIR / "head_template_mesh.obj").exists():
            self._load_uv()
        else:
            self._generate_uv()

    def _generate_uv(self):
        """Sans le .obj de FLAME (archive de 1,2 Go) : projection cylindrique autour de la tete,
        resserree a l'arriere pour donner plus de texels au visage. Les yeux ont leur propre ilot."""
        v = self.v_template
        center = v.mean(0)
        d = v - center
        s = np.arctan2(d[:, 0], d[:, 2]) / np.pi                  # 0 = face, +-1 = arriere du crane
        u = 0.5 + 0.5 * np.sign(s) * np.abs(s) ** 0.6
        y0, y1 = v[:, 1].min(), v[:, 1].max()
        uv = np.stack([u, 0.12 + 0.88 * (v[:, 1] - y0) / (y1 - y0)], 1).astype(np.float32)

        # Globes oculaires : vue de face a plat, chacun dans un coin du bas de la texture
        for key, x0 in (("left_eyeball", 0.02), ("right_eyeball", 0.13)):
            idx = self.masks.get(key)
            if idx is None:
                continue
            p = v[idx, :2]
            p = (p - p.min(0)) / (p.max(0) - p.min(0) + 1e-9)
            uv[idx] = np.stack([x0 + 0.09 * p[:, 0], 0.01 + 0.09 * p[:, 1]], 1)

        faces = self.faces.copy()
        uv_list = list(uv)
        uv_faces = faces.copy()
        # Couture a l'arriere : un triangle a cheval sur u=0 / u=1 est ramene d'un seul cote
        for t, tri in enumerate(faces):
            us = uv[tri, 0]
            if us.max() - us.min() > 0.5:
                for c in range(3):
                    if us[c] < 0.5:
                        uv_list.append(np.array([1.0, uv[tri[c], 1]], np.float32))
                        uv_faces[t, c] = len(uv_list) - 1
        self.uv = np.asarray(uv_list, np.float32)
        self.uv_faces = uv_faces
        self.obj_faces = faces

    def _load_uv(self):
        vt, corner_v, corner_vt = [], [], []
        with open(FLAME_DIR / "head_template_mesh.obj", encoding="utf-8") as f:
            for line in f:
                if line.startswith("vt "):
                    vt.append([float(x) for x in line.split()[1:3]])
                elif line.startswith("f "):
                    corners = [c.split("/") for c in line.split()[1:4]]
                    corner_v.append([int(c[0]) - 1 for c in corners])
                    corner_vt.append([int(c[1]) - 1 for c in corners])
        self.uv = np.asarray(vt, np.float32)                   # (nvt, 2), origine en bas a gauche
        self.uv_faces = np.asarray(corner_vt, np.int64)        # meme ordre que les triangles du .obj
        self.obj_faces = np.asarray(corner_v, np.int64)

    def _load_landmarks(self):
        e = np.load(FLAME_DIR / "mediapipe_landmark_embedding.npz")
        self.lmk_faces = e["lmk_face_idx"].astype(np.int64)
        self.lmk_bary = e["lmk_b_coords"].astype(np.float32)
        self.lmk_mediapipe = e["landmark_indices"].astype(np.int64)  # indices des points MediaPipe

    def _load_masks(self):
        path = FLAME_DIR / "FLAME_masks.pkl"
        self.masks = {k: np.asarray(v, np.int64) for k, v in _load_pickle(path).items()} if path.exists() else {}

    def joint(self, vertices, index=1):
        """Articulation FLAME (1 = cou) : sert a poser la tete sur le personnage."""
        return self.joints[index] @ vertices
