"""Texture de la tete : chaque point de la texture FLAME prend sa couleur dans la photo ou il est le
mieux vu (face pour le visage, cotes pour les joues et les oreilles). Ce qu'aucune photo ne voit
(arriere du crane) prend la couleur des cheveux ou de la peau mesuree."""
import cv2
import numpy as np

from fit import Fit, View, project_vertices
from flame_model import Flame


def _raster(tri, size, depth=None):
    """Rasterise des triangles 2D (pixels). Retourne l'indice du triangle et les barycentriques par pixel ;
    avec `depth`, garde le plus proche de la camera (z-buffer)."""
    h, w = size
    face_id = np.full((h, w), -1, np.int64)
    bary = np.zeros((h, w, 3), np.float32)
    zbuf = np.full((h, w), -np.inf, np.float32)
    for i, (a, b, c) in enumerate(tri):
        x0, y0 = np.floor(np.minimum(np.minimum(a, b), c)).astype(int)
        x1, y1 = np.ceil(np.maximum(np.maximum(a, b), c)).astype(int)
        x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, w - 1), min(y1, h - 1)
        if x0 > x1 or y0 > y1:
            continue
        den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(den) < 1e-9:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        l0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / den
        l1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / den
        l2 = 1 - l0 - l1
        inside = (l0 >= -1e-4) & (l1 >= -1e-4) & (l2 >= -1e-4)
        if not inside.any():
            continue
        yy, xx = ys[inside].astype(int), xs[inside].astype(int)
        lb = np.stack([l0[inside], l1[inside], l2[inside]], 1)
        if depth is not None:
            z = lb @ depth[i]
            closer = z > zbuf[yy, xx]
            yy, xx, lb, z = yy[closer], xx[closer], lb[closer], z[closer]
            zbuf[yy, xx] = z
        face_id[yy, xx] = i
        bary[yy, xx] = lb
    return face_id, bary, zbuf


def _sample(image, points):
    """Couleur bilineaire aux positions (x, y) en pixels (cv2.remap refuse plus de 32767 points d'un coup)."""
    h, w = image.shape[:2]
    x = np.clip(points[:, 0] - 0.5, 0, w - 1.001)
    y = np.clip(points[:, 1] - 0.5, 0, h - 1.001)
    x0, y0 = x.astype(int), y.astype(int)
    fx, fy = (x - x0)[:, None], (y - y0)[:, None]
    img = image.astype(np.float32)
    top = img[y0, x0] * (1 - fx) + img[y0, x0 + 1] * fx
    bottom = img[y0 + 1, x0] * (1 - fx) + img[y0 + 1, x0 + 1] * fx
    return top * (1 - fy) + bottom * fy


def _vertex_normals(v, faces):
    n = np.zeros_like(v)
    fn = np.cross(v[faces[:, 1]] - v[faces[:, 0]], v[faces[:, 2]] - v[faces[:, 0]])
    for k in range(3):
        np.add.at(n, faces[:, k], fn)
    return n / (np.linalg.norm(n, axis=1, keepdims=True) + 1e-9)


def _median_color(image, points):
    h, w = image.shape[:2]
    pts = np.clip(points.astype(int), 0, [w - 1, h - 1])
    return np.median(image[pts[:, 1], pts[:, 0]].astype(np.float32), axis=0)


def measure_colors(view: View):
    """Peau (joues) et cheveux (au-dessus du front, si plus sombres que la peau)."""
    p = view.points[:, :2]
    cheeks = np.concatenate([p[[50, 101, 118, 117]], p[[280, 330, 347, 346]]])
    skin = _median_color(view.image, cheeks)
    top, chin = p[10], p[152]
    up = (top - chin) / (np.linalg.norm(top - chin) + 1e-6)
    side = np.array([up[1], -up[0]])
    height = np.linalg.norm(top - chin)
    samples = [top + up * height * d + side * height * s for d in (0.08, 0.14, 0.2) for s in (-0.2, -0.1, 0, 0.1, 0.2)]
    h, w = view.image.shape[:2]
    pts = np.clip(np.asarray(samples).astype(int), 0, [w - 1, h - 1])
    colors = view.image[pts[:, 1], pts[:, 0]].astype(np.float32)
    lum = colors @ np.array([0.114, 0.587, 0.299])        # BGR
    skin_lum = skin @ np.array([0.114, 0.587, 0.299])
    dark = colors[lum < skin_lum * 0.75]
    has_hair = len(dark) >= 5
    hair = np.median(dark, axis=0) if has_hair else skin * 0.9
    return skin, hair, has_hair


def bake(flame: Flame, fit_result: Fit, views, size=1024):
    v = fit_result.vertices
    faces = flame.obj_faces
    normals = _vertex_normals(v, flame.faces)

    # Chaque texel : son triangle et sa position sur ce triangle. Interieur de la bouche et des paupieres
    # tombent au meme endroit de la texture que la peau : la surface la plus exterieure l'emporte.
    uv_px = np.stack([flame.uv[:, 0] * size, (1 - flame.uv[:, 1]) * size], 1)
    t = flame.v_template
    radius = np.linalg.norm((t - t.mean(0))[:, [0, 2]], axis=1).astype(np.float32)
    face_id, bary, _ = _raster(uv_px[flame.uv_faces], (size, size), radius[faces])
    covered = face_id >= 0
    fid, b = face_id[covered], bary[covered]
    corners = faces[fid]                                    # (n, 3) sommets du triangle

    # Le cou est souvent cache par un col sur les photos : il prend la couleur de la peau
    neck = np.zeros(len(v), bool)
    for key in ("neck", "boundary"):
        if key in flame.masks:
            neck[flame.masks[key]] = True
    no_photo = neck[corners].mean(1) > 0.3

    total = np.zeros((covered.sum(), 3), np.float32)
    weight = np.zeros(covered.sum(), np.float32)
    for k, view in enumerate(views):
        img = view.image
        h, w = img.shape[:2]
        xy, depth = project_vertices(fit_result, k)

        # Visibilite : z-buffer de la tete vue depuis cette photo (en basse resolution)
        down = 4
        zid, _, zbuf = _raster(xy[flame.faces] / down, (h // down + 1, w // down + 1), depth[flame.faces])

        p = (xy[corners] * b[:, :, None]).sum(1)
        z = (depth[corners] * b).sum(1)
        n = (normals[corners] * b[:, :, None]).sum(1) @ fit_result.rotations[k].T
        facing = np.clip(n[:, 2], 0, 1)
        qx = np.clip((p[:, 0] / down).astype(int), 0, zbuf.shape[1] - 1)
        qy = np.clip((p[:, 1] / down).astype(int), 0, zbuf.shape[0] - 1)
        visible = z >= zbuf[qy, qx] - 0.004 * fit_result.scales[k]
        inside = (p[:, 0] >= 0) & (p[:, 0] < w) & (p[:, 1] >= 0) & (p[:, 1] < h)
        wk = facing ** 4 * visible * inside
        wk[facing < 0.2] = 0
        wk[no_photo] = 0

        color = _sample(img, p)
        total += color * wk[:, None]
        weight += wk

    # Zones jamais vues : crane -> cheveux, le reste -> peau
    skin, hair, _ = measure_colors(views[0])
    scalp = np.zeros(len(v), bool)
    if "scalp" in flame.masks:
        scalp[flame.masks["scalp"]] = True
    # Frontiere peau / cheveux adoucie (sinon elle suit les triangles en escalier)
    hair_map = np.zeros((size, size), np.float32)
    hair_map[covered] = scalp[corners].mean(1)
    hair_map = cv2.GaussianBlur(hair_map, (0, 0), size / 120)[covered][:, None]
    fill = (hair * hair_map + skin * (1 - hair_map)).astype(np.float32)

    seen = np.clip(weight / 0.25, 0, 1)[:, None]
    baked = total / np.maximum(weight, 1e-6)[:, None]
    texels = baked * seen + fill * (1 - seen)

    tex = np.zeros((size, size, 3), np.float32)
    tex[covered] = texels
    # Bords des ilots UV : on deborde un peu pour eviter les coutures noires
    tex = tex.astype(np.uint8)
    mask = (~covered).astype(np.uint8) * 255
    tex = cv2.inpaint(tex, mask, 3, cv2.INPAINT_TELEA)
    return tex, skin, hair
