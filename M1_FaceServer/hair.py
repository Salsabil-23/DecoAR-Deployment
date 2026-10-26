"""Cheveux en 3D : FLAME n'a pas de cheveux, on genere une calotte qui suit le crane.
- epaisseur mesuree sur la photo de face (du haut du crane FLAME au haut des cheveux) ;
- la calotte s'affine vers la limite du cuir chevelu (front, tempes, nuque) ;
- texture prise sur les photos la ou on voit les cheveux, meches generees ailleurs ;
- bord transparent effile (alpha), pour que la limite ne fasse pas un casque."""
import cv2
import numpy as np
from scipy import sparse

from fit import Fit, View
from flame_model import Flame
from texture import _raster, _sample, _vertex_normals

LUM = np.array([0.114, 0.587, 0.299])  # BGR


def _smooth(values, faces, n, iterations):
    """Moyenne sur les voisins du maillage : un masque binaire devient un degrade."""
    i = np.concatenate([faces[:, 0], faces[:, 1], faces[:, 2], faces[:, 1], faces[:, 2], faces[:, 0]])
    j = np.concatenate([faces[:, 1], faces[:, 2], faces[:, 0], faces[:, 0], faces[:, 1], faces[:, 2]])
    adj = sparse.coo_matrix((np.ones(len(i)), (i, j)), shape=(n, n)).tocsr()
    adj.data[:] = 1
    deg = np.asarray(adj.sum(1)).ravel() + 1
    out = values.astype(np.float64)
    for _ in range(iterations):
        out = (adj @ out + out) / deg
    return out


def _project(fit_result: Fit, vertices, k):
    cam = vertices @ fit_result.rotations[k].T
    xy = np.stack([cam[:, 0], -cam[:, 1]], 1) * fit_result.scales[k] + fit_result.offsets[k]
    return xy, cam[:, 2] * fit_result.scales[k]


def measure_thickness(flame: Flame, fit_result: Fit, view: View, skin, hair):
    """Epaisseur des cheveux (m) au sommet du crane, mesuree sur la photo de face."""
    v = fit_result.vertices
    scalp = flame.masks.get("scalp")
    if scalp is None:
        return 0.015
    top = scalp[np.argmax(v[scalp, 1])]
    xy, _ = _project(fit_result, v[top:top + 1], 0)
    x, y = xy[0]
    img = view.image
    h, w = img.shape[:2]
    if not (0 <= x < w and 0 <= y < h):
        return 0.015
    # Colonne de pixels au-dessus du sommet du crane : le plus haut pixel de cheveux donne l'epaisseur
    s = fit_result.scales[0]
    y0, y1 = int(max(0, y - 0.07 * s)), int(min(h - 1, y + 0.03 * s))
    column = img[y0:y1, max(0, int(x) - 4):min(w, int(x) + 5)].astype(np.float32).mean(1)
    is_hair = _is_hair(column, skin, hair)
    # Bande continue de cheveux qui part du crane (on s'arrete au premier trou de 3 mm : fond, mur...)
    start = int(y) - y0
    rows = [r for r in range(min(start + int(0.01 * s), len(is_hair) - 1), -1, -1)]
    top, gap = None, 0
    for r in rows:
        if is_hair[r]:
            top, gap = r, 0
        elif top is not None:
            gap += 1
            if gap > 0.003 * s:
                break
    if top is None:
        return 0.008
    return float(np.clip((y - (y0 + top)) / s, 0.006, 0.035))


def _is_hair(colors, skin, hair):
    lum = colors @ LUM
    return (np.linalg.norm(colors - hair, axis=1) < 55) & (lum < float(skin @ LUM) * 0.82)


def has_long_hair(view: View, skin, hair):
    """Cheveux qui tombent a cote de la machoire sur la photo de face (mi-longs ou longs)."""
    p = view.points[:, :2]
    width = np.linalg.norm(p[454] - p[234])
    samples = []
    for edge, other in ((234, 454), (454, 234)):
        out = (p[edge] - p[other]) / (np.linalg.norm(p[edge] - p[other]) + 1e-6)
        for t in (0.3, 0.5, 0.7):                       # entre l'oreille et le menton
            y = p[edge, 1] + (p[152, 1] - p[edge, 1]) * t
            for d in (0.08, 0.14):
                samples.append([p[edge, 0] + out[0] * width * d, y])
    h, w = view.image.shape[:2]
    s = np.clip(np.asarray(samples, np.float32), 0, [w - 1, h - 1])
    return _is_hair(_sample(view.image, s), skin, hair).mean() > 0.5


def hair_region(flame: Flame, fit_result: Fit, views, skin, hair):
    """1 la ou la personne a des cheveux, d'apres les photos ; sans photo : le crane oui, le reste non.
    Donne la vraie ligne du front, les tempes et la longueur (cheveux mi-longs le long du cou)."""
    v = fit_result.vertices
    n = len(v)
    normals = _vertex_normals(v, flame.faces)
    region = np.zeros(n, bool)
    for key in ("scalp", "neck", "boundary"):
        if key in flame.masks:
            region[flame.masks[key]] = True
    for key in ("face", "left_ear", "right_ear", "eye_region", "nose", "lips", "left_eyeball", "right_eyeball"):
        if key in flame.masks:
            region[flame.masks[key]] = False
    face_idx = flame.masks.get("face")
    chin = v[face_idx, 1].min() if face_idx is not None else v[:, 1].min()
    scalp = np.zeros(n, bool)
    if "scalp" in flame.masks:
        scalp[flame.masks["scalp"]] = True
    # Cou : seulement l'arriere et les cotes (devant et sous le menton, c'est la barbe), pas trop bas
    t_normals = _vertex_normals(flame.v_template, flame.faces)
    neck_zone = region & ~scalp
    region &= ~neck_zone | ((t_normals[:, 2] < 0.1) & (v[:, 1] > chin - 0.03))

    votes = np.zeros(n)
    seen = np.zeros(n)
    probe = v + normals * 0.01               # juste au-dessus de la peau, la ou seraient les cheveux
    for k, view in enumerate(views):
        xy, _ = _project(fit_result, probe, k)
        facing = (normals @ fit_result.rotations[k].T)[:, 2]
        h, w = view.image.shape[:2]
        # pres de la tete dans la photo uniquement (le fond sombre de la piece n'est pas des cheveux)
        hull = cv2.convexHull(view.points[:, :2].astype(np.float32))
        center = view.points[:, :2].mean(0)
        near = np.zeros((h, w), np.uint8)
        cv2.fillConvexPoly(near, ((hull - center) * 1.45 + center).astype(np.int32), 1)
        px = np.clip(xy[:, 0].astype(int), 0, w - 1)
        py = np.clip(xy[:, 1].astype(int), 0, h - 1)
        ok = region & (facing > 0.35) & (xy[:, 0] >= 0) & (xy[:, 0] < w) & (xy[:, 1] >= 0) & (xy[:, 1] < h)
        ok &= near[py, px] > 0
        if not ok.any():
            continue
        colors = _sample(view.image, xy[ok])
        votes[ok] += _is_hair(colors, skin, hair) * facing[ok]
        seen[ok] += facing[ok]

    ratio = votes / np.maximum(seen, 1e-6)
    need = np.where(scalp, 0.5, 0.75)        # plus de preuves pour mettre des cheveux sur le cou
    seen_hair = (seen > 0.2) & (ratio > need)
    # Derriere la tete, aucune photo ne voit : cheveux courts (jusqu'aux oreilles) sauf si les photos
    # montrent des cheveux longs a cote de la machoire
    long_hair = (seen_hair & neck_zone).sum() > 30 or has_long_hair(views[0], skin, hair)
    unseen = (scalp | neck_zone) & region if long_hair else scalp & region
    if not long_hair:
        ears = [flame.masks[k] for k in ("left_ear", "right_ear") if k in flame.masks]
        if ears:
            ear_y = v[np.concatenate(ears), 1].mean()
            unseen &= v[:, 1] > ear_y - 0.01
    indicator = np.where(seen > 0.2, seen_hair, unseen) & region
    return indicator.astype(np.float64)


def build_hair(flame: Flame, fit_result: Fit, views, skin, hair, size=1024, indicator=None):
    """Maillage de la calotte (memes UV que la tete) + texture RGBA. None si la personne n'a pas de cheveux."""
    v = fit_result.vertices
    n = len(v)
    if indicator is None:
        indicator = hair_region(flame, fit_result, views, skin, hair)
    if indicator.sum() < 50:
        return None
    # Degrade vers le bord, sans deborder au-dela de la zone de cheveux
    weight = np.clip(_smooth(indicator, flame.faces, n, 3) * 1.5 - 0.25, 0, 1) * \
        np.clip(_smooth(indicator, flame.faces, n, 1) * 2, 0, 1)

    thickness = measure_thickness(flame, fit_result, views[0], skin, hair)
    normals = _vertex_normals(v, flame.faces)
    # Epaisseur pleine sur le dessus, plus fine sur les cotes et la nuque
    up = np.clip((v[:, 1] - v[:, 1].min()) / (v[:, 1].max() - v[:, 1].min()), 0, 1)
    offset = thickness * weight * (0.45 + 0.55 * up ** 2) + 0.002 * weight
    shell = (v + normals * offset[:, None]).astype(np.float32)

    keep = weight[flame.obj_faces].min(1) > 0.02
    faces = flame.obj_faces[keep]
    uv_faces = flame.uv_faces[keep]

    # Texture : rasterisation dans les UV de la tete, couleur des photos la ou les cheveux sont vus
    uv_px = np.stack([flame.uv[:, 0] * size, (1 - flame.uv[:, 1]) * size], 1)
    face_id, bary, _ = _raster(uv_px[uv_faces], (size, size))
    covered = face_id >= 0
    fid, b = face_id[covered], bary[covered]
    corners = faces[fid]
    shell_n = _vertex_normals(shell, flame.faces)

    total = np.zeros((covered.sum(), 3), np.float32)
    wsum = np.zeros(covered.sum(), np.float32)
    for k, view in enumerate(views):
        xy, _ = _project(fit_result, shell, k)
        p = (xy[corners] * b[:, :, None]).sum(1)
        nn = (shell_n[corners] * b[:, :, None]).sum(1) @ fit_result.rotations[k].T
        facing = np.clip((nn[:, 2] - 0.2) / 0.6, 0, 1)
        h, w = view.image.shape[:2]
        inside = (p[:, 0] >= 0) & (p[:, 0] < w) & (p[:, 1] >= 0) & (p[:, 1] < h)
        color = _sample(view.image, p)
        # On ne prend que des pixels qui ressemblent aux cheveux (pas le mur derriere)
        hair_like = _is_hair(color, skin, hair)
        wk = (facing ** 2 * inside * hair_like).astype(np.float32)
        total += color * wk[:, None]
        wsum += wk

    # Meches generees : bruit etire vers le bas, a la couleur mesuree
    rng = np.random.default_rng(7)
    noise = rng.random((size // 4, size)).astype(np.float32)
    noise = cv2.resize(cv2.GaussianBlur(noise, (0, 0), sigmaX=0.6, sigmaY=6), (size, size))
    noise = (noise - noise.mean()) / (noise.std() + 1e-6)
    strands = np.clip(hair[None, :] * (1 + 0.18 * noise[covered][:, None]), 0, 255)

    seen = np.clip(wsum / 0.3, 0, 1)[:, None]
    photo = total / np.maximum(wsum, 1e-6)[:, None]
    rgb = photo * seen + strands * (1 - seen)

    # Bord effile : transparence qui suit le degrade du cuir chevelu, decoupee en meches
    alpha_v = (weight[corners] * b).sum(1)
    edge = np.clip((alpha_v - 0.15) / 0.5, 0, 1)
    alpha = np.clip(edge * 1.25 + 0.35 * noise[covered] * (1 - edge), 0, 1)

    tex = np.zeros((size, size, 4), np.float32)
    tex[covered, :3] = rgb
    tex[covered, 3] = alpha * 255
    tex = np.clip(tex, 0, 255).astype(np.uint8)
    mask = (~covered).astype(np.uint8) * 255
    tex[:, :, :3] = cv2.inpaint(np.ascontiguousarray(tex[:, :, :3]), mask, 3, cv2.INPAINT_TELEA)
    return {"vertices": shell, "faces": faces, "uv_faces": uv_faces, "texture": tex, "thickness": thickness}
