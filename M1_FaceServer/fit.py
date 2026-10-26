"""Forme de la tete : FLAME est deforme pour que ses points du visage tombent sur ceux que MediaPipe
trouve dans chaque photo (face, cote gauche, cote droit). Plusieurs angles donnent la profondeur
(nez, menton, front) qu'une seule photo ne donne pas."""
from dataclasses import dataclass
from pathlib import Path

import cv2
import mediapipe as mp
import numpy as np
import torch
from mediapipe.tasks import python as mp_tasks
from mediapipe.tasks.python import vision

from flame_model import Flame


@dataclass
class View:
    image: np.ndarray        # BGR, deja remis a l'endroit (pas en miroir)
    points: np.ndarray       # (478, 3) MediaPipe en pixels : x, y vers le bas, z en pixels (negatif = proche)


@dataclass
class Fit:
    vertices: np.ndarray     # (5023, 3) tete de la personne, metres, repere FLAME
    rotations: list          # par vue : matrice 3x3
    scales: list             # par vue : pixels par metre
    offsets: list            # par vue : (2,) decalage en pixels
    error: float             # erreur moyenne en part de la taille du visage


# Modele MediaPipe Face Landmarker (478 points), a deposer a cote de ce fichier
LANDMARKER_PATH = Path(__file__).parent / "face_landmarker.task"
_landmarker = None


def detect(image):
    """Points MediaPipe en pixels, ou None si aucun visage."""
    global _landmarker
    if _landmarker is None:
        options = vision.FaceLandmarkerOptions(
            base_options=mp_tasks.BaseOptions(model_asset_path=str(LANDMARKER_PATH)),
            running_mode=vision.RunningMode.IMAGE, num_faces=1)
        _landmarker = vision.FaceLandmarker.create_from_options(options)
    h, w = image.shape[:2]
    rgb = np.ascontiguousarray(cv2.cvtColor(image, cv2.COLOR_BGR2RGB))
    result = _landmarker.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb))
    if not result.face_landmarks:
        return None
    return np.array([[p.x * w, p.y * h, p.z * w] for p in result.face_landmarks[0]], np.float32)


def _rotation(aa):
    """Matrice de rotation depuis un axe-angle (formule de Rodrigues), derivable."""
    theta = torch.linalg.norm(aa) + 1e-8
    k = aa / theta
    K = torch.zeros(3, 3)
    K[0, 1], K[0, 2], K[1, 0] = -k[2], k[1], k[2]
    K[1, 2], K[2, 0], K[2, 1] = -k[0], -k[1], k[0]
    return torch.eye(3) + torch.sin(theta) * K + (1 - torch.cos(theta)) * (K @ K)


def fit(flame: Flame, views, iterations=700):
    template = torch.from_numpy(flame.v_template)
    shape_dirs = torch.from_numpy(flame.shape_dirs)
    expr_dirs = torch.from_numpy(flame.expr_dirs)
    lmk_faces = torch.from_numpy(flame.faces[flame.lmk_faces])
    lmk_bary = torch.from_numpy(flame.lmk_bary)
    targets = [torch.from_numpy(v.points[flame.lmk_mediapipe]) for v in views]

    shape = torch.zeros(flame.n_shape, requires_grad=True)
    expr = torch.zeros(flame.n_expr, requires_grad=True)

    # Initialisation de chaque vue : taille et position du visage dans la photo
    rot, scale, offset = [], [], []
    for k, (t, view) in enumerate(zip(targets, views)):
        span = (t[:, :2].max(0).values - t[:, :2].min(0).values).max()
        # Rotation de la tete estimee par la position du bout du nez (1) entre les bords du visage (234, 454)
        p = view.points
        mid, half = (p[234, 0] + p[454, 0]) / 2, abs(p[454, 0] - p[234, 0]) / 2 + 1e-3
        yaw = float(np.arcsin(np.clip((p[1, 0] - mid) / (1.3 * half), -0.9, 0.9)))
        # (jamais exactement 0 : la derivee de la rotation n'existe pas en 0)
        rot.append(torch.tensor([1e-4, yaw + 1e-4, 0.0], requires_grad=True))
        scale.append(torch.tensor(float(span / 0.16), requires_grad=True))  # visage ~16 cm
        offset.append(t[:, :2].mean(0).clone().requires_grad_(True))
    params = [shape, expr] + rot + scale + offset
    optim = torch.optim.Adam(params, lr=0.02)

    def head():
        return template + shape_dirs @ shape + expr_dirs @ expr

    def landmarks(v):
        tri = v[lmk_faces]                      # (n, 3, 3)
        return (tri * lmk_bary[:, :, None]).sum(1)

    def project(points, k):
        cam = points @ _rotation(rot[k]).T
        xy = torch.stack([cam[:, 0], -cam[:, 1]], 1) * scale[k] + offset[k]   # image : y vers le bas
        depth = cam[:, 2] * scale[k]                                           # vers la camera
        return xy, depth

    for it in range(iterations):
        optim.param_groups[0]["lr"] = 0.02 if it < iterations * 0.7 else 0.005
        optim.zero_grad()
        v = head()
        lm = landmarks(v)
        loss = 0.0
        for k, t in enumerate(targets):
            xy, depth = project(lm, k)
            size = (t[:, :2].max(0).values - t[:, :2].min(0).values).max()
            loss = loss + (((xy - t[:, :2]) / size) ** 2).sum(1).mean()
            # profondeur MediaPipe (relative) : aide pour la vue de face
            d_t = -t[:, 2]
            loss = loss + 0.1 * (((depth - depth.mean()) - (d_t - d_t.mean())) / size).pow(2).mean()
        # Une tete plausible : formes et expressions proches de la moyenne
        loss = loss + 1e-4 * shape.pow(2).sum() + 1e-3 * expr.pow(2).sum()
        loss.backward()
        optim.step()

    with torch.no_grad():
        v = head()
        lm = landmarks(v)
        errors = []
        for k, t in enumerate(targets):
            xy, _ = project(lm, k)
            size = (t[:, :2].max(0).values - t[:, :2].min(0).values).max()
            errors.append(float(((xy - t[:, :2]).norm(dim=1) / size).mean()))
        return Fit(
            vertices=v.numpy(),
            rotations=[_rotation(r).numpy() for r in rot],
            scales=[float(s) for s in scale],
            offsets=[o.numpy() for o in offset],
            error=float(np.mean(errors)),
        )


def project_vertices(fit_result: Fit, k):
    """Position de chaque sommet dans la photo k (pixels) et profondeur vers la camera."""
    cam = fit_result.vertices @ fit_result.rotations[k].T
    xy = np.stack([cam[:, 0], -cam[:, 1]], 1) * fit_result.scales[k] + fit_result.offsets[k]
    return xy, cam[:, 2] * fit_result.scales[k]
