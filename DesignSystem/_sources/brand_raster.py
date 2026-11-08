"""Logo et icônes d'application à partir de l'image originale (aucun redessin)."""
import base64, io, json, os, shutil
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import numpy as np
from common import ROOT, write, text_path
import tokens as T

P = T.PALETTE
SRC = "/home/claude/build/orig.png"
PLATE = (80, 76, 1174, 1155)   # plaque arrondie dans l'image fournie
PR = 275                        # rayon des coins de la plaque
LOGO, APP = f"{ROOT}/logo", f"{ROOT}/app-icon"

orig = Image.open(SRC).convert("RGB")

def plate_mask(size=orig.size, box=PLATE, r=PR, ss=4):
    m = Image.new("L", (size[0] * ss, size[1] * ss), 0)
    ImageDraw.Draw(m).rounded_rectangle([v * ss for v in box], radius=r * ss, fill=255)
    return m.resize(size, Image.LANCZOS)

def fill_outside(img, mask):
    """Prolonge les couleurs de la plaque dans les coins (remplissage par flou normalisé)."""
    a = np.asarray(mask).astype(float) / 255
    rgb = np.asarray(img).astype(float)
    out = rgb * a[..., None]
    acc_rgb, acc_a = np.zeros_like(rgb), np.zeros_like(a)
    for rad in (8, 24, 60, 140):
        pm = Image.fromarray(np.uint8(np.clip(rgb * a[..., None], 0, 255)))
        am = Image.fromarray(np.uint8(a * 255))
        br = np.asarray(pm.filter(ImageFilter.GaussianBlur(rad))).astype(float)
        ba = np.asarray(am.filter(ImageFilter.GaussianBlur(rad))).astype(float) / 255
        need = (1 - acc_a) * (ba > 0.02)
        acc_rgb += br / np.maximum(ba, 1e-3)[..., None] * need[..., None]
        acc_a += need
    filled = acc_rgb / np.maximum(acc_a, 1e-3)[..., None]
    res = rgb * a[..., None] + filled * (1 - a[..., None])
    return Image.fromarray(np.uint8(np.clip(res, 0, 255)))

mask = plate_mask()
fullbleed_src = fill_outside(orig, mask)

def icon_square(size):
    """Icône pleine (sans coins), pour iOS / Play Store : le système applique son masque."""
    box = (107, 98, 1143, 1134)
    return fullbleed_src.crop(box).resize((size, size), Image.LANCZOS)

def symbol(size, shadow=True):
    """La plaque seule, coins arrondis transparents (+ ombre douce optionnelle)."""
    plate = orig.crop(PLATE); m = mask.crop(PLATE)
    plate = plate.convert("RGBA"); plate.putalpha(m)
    W = PLATE[2] - PLATE[0]; H = PLATE[3] - PLATE[1]
    pad = 90 if shadow else 0
    canvas = Image.new("RGBA", (W + pad * 2, H + pad * 2), (0, 0, 0, 0))
    if shadow:
        sh = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        sm = Image.new("L", canvas.size, 0); sm.paste(m, (pad, pad + 26))
        sm = sm.filter(ImageFilter.GaussianBlur(34)).point(lambda v: int(v * 0.32))
        sh.putalpha(sm)
        col = Image.new("RGBA", canvas.size, (110, 44, 22, 0)); col.putalpha(sm)
        canvas.alpha_composite(col)
    canvas.alpha_composite(plate, (pad, pad))
    s = size / max(canvas.size)
    return canvas.resize((round(canvas.width * s), round(canvas.height * s)), Image.LANCZOS)

def data_uri(im):
    b = io.BytesIO(); im.save(b, "PNG"); return "data:image/png;base64," + base64.b64encode(b.getvalue()).decode()

def wordmark(x, baseline, size, deco, ar):
    d1, w1 = text_path("Deco", size, 600, x, baseline, -0.01)
    d2, w2 = text_path("AR", size, 600, x + w1 - size * 0.035, baseline, -0.01)
    return f'<path d="{d1}" fill="{deco}"/><path d="{d2}" fill="{ar}"/>', w1 + w2 - size * 0.035

def lockup_h(deco, ar, tagline=False):
    sym = symbol(300, shadow=False); uri = data_uri(sym)
    H = 160; s = 150; size = 82
    wm, w = wordmark(s + 34, 104 if not tagline else 94, size, deco, ar)
    tg = ""
    if tagline:
        td, _ = text_path("ESSAIE · POSE · ADOPTE", 22, 500, s + 37, 132, 0.08)
        tg = f'<path d="{td}" fill="{deco}" opacity=".72"/>'
    W = round(s + 34 + w + 10)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
            f'<image x="0" y="5" width="{s}" height="{s}" xlink:href="{uri}" href="{uri}"/>{wm}{tg}</svg>\n')

def lockup_v(deco, ar):
    sym = symbol(300, shadow=False); uri = data_uri(sym)
    size = 64; _, w = wordmark(0, 0, size, deco, ar)
    W = max(round(w) + 40, 260); s = 220
    wm, _ = wordmark((W - w) / 2, s + 82, size, deco, ar)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{W}" height="{s + 100}" viewBox="0 0 {W} {s + 100}">'
            f'<image x="{(W - s) / 2}" y="0" width="{s}" height="{s}" xlink:href="{uri}" href="{uri}"/>{wm}</svg>\n')

def svg2png(svg_path, png_path, h):
    os.makedirs(os.path.dirname(png_path), exist_ok=True)
    os.system(f'rsvg-convert -h {h} -o "{png_path}" "{svg_path}"')

if __name__ == "__main__":
    # ------------------------------------------------------------ logo
    shutil.rmtree(LOGO, ignore_errors=True)
    os.makedirs(f"{LOGO}/symbole", exist_ok=True)
    shutil.copy(SRC, f"{LOGO}/symbole/decoar-icone-originale.png")
    for sz in (1024, 512, 256, 128, 64):
        symbol(sz).save(f"{LOGO}/symbole/decoar-symbole-{sz}.png")
        symbol(sz, shadow=False).save(f"{LOGO}/symbole/decoar-symbole-sans-ombre-{sz}.png")
    INK, CR, TER, TERL = P["lin"]["900"], P["lin"]["100"], P["terracotta"]["500"], P["terracotta"]["300"]
    variants = {
        "horizontal/decoar-logo-horizontal": lockup_h(INK, TER),
        "horizontal/decoar-logo-horizontal-sur-sombre": lockup_h(CR, TERL),
        "horizontal/decoar-logo-signature": lockup_h(INK, TER, tagline=True),
        "horizontal/decoar-logo-signature-sur-sombre": lockup_h(CR, TERL, tagline=True),
        "vertical/decoar-logo-vertical": lockup_v(INK, TER),
        "vertical/decoar-logo-vertical-sur-sombre": lockup_v(CR, TERL),
    }
    for name, svg in variants.items():
        write(f"{LOGO}/{name}.svg", svg)
        svg2png(f"{LOGO}/{name}.svg", f"{LOGO}/{name}.png", 512)
    for name, (deco, ar) in {"decoar-mot-symbole": (INK, TER), "decoar-mot-symbole-creme": (CR, TERL)}.items():
        wm, w = wordmark(4, 66, 64, deco, ar)
        write(f"{LOGO}/mot-symbole/{name}.svg", f'<svg xmlns="http://www.w3.org/2000/svg" width="{round(w + 8)}" height="84" viewBox="0 0 {round(w + 8)} 84">{wm}</svg>\n')
        svg2png(f"{LOGO}/mot-symbole/{name}.svg", f"{LOGO}/mot-symbole/{name}.png", 256)

    # ------------------------------------------------------------ iOS
    shutil.rmtree(APP, ignore_errors=True)
    ios = f"{APP}/ios/AppIcon.appiconset"; os.makedirs(ios)
    master = icon_square(1024); master.save(f"{ios}/AppIcon-1024.png")
    tinted = master.convert("L"); tinted.save(f"{ios}/AppIcon-1024-tinted.png")
    json.dump({"images": [
        {"filename": "AppIcon-1024.png", "idiom": "universal", "platform": "ios", "size": "1024x1024"},
        {"appearances": [{"appearance": "luminosity", "value": "tinted"}], "filename": "AppIcon-1024-tinted.png", "idiom": "universal", "platform": "ios", "size": "1024x1024"}],
        "info": {"author": "xcode", "version": 1}}, open(f"{ios}/Contents.json", "w"), indent=2)
    leg = f"{APP}/ios/tailles-unity"; os.makedirs(leg)
    for px in (180, 167, 152, 120, 87, 80, 76, 60, 58, 40, 29, 20):
        master.resize((px, px), Image.LANCZOS).save(f"{leg}/Icon-{px}.png")

    # ------------------------------------------------------------ Android
    A = f"{APP}/android"; os.makedirs(f"{A}/unity-adaptive")
    edge = np.asarray(master).reshape(-1, 3)
    bg_col = tuple(int(v) for v in np.median(np.concatenate([np.asarray(master)[:12].reshape(-1, 3), np.asarray(master)[-12:].reshape(-1, 3)]), axis=0))
    # arrière-plan : la couleur de bord de l'icône, légère lumière au centre
    bg = Image.new("RGB", (432, 432), bg_col)
    inner = master.resize((300, 300), Image.LANCZOS).filter(ImageFilter.GaussianBlur(26))
    bm = Image.new("L", (300, 300), 0); ImageDraw.Draw(bm).rounded_rectangle((0, 0, 299, 299), radius=80, fill=255)
    bg.paste(inner, (66, 66), bm.filter(ImageFilter.GaussianBlur(30)))
    bg = bg.filter(ImageFilter.GaussianBlur(18))
    bg.save(f"{A}/unity-adaptive/background-432.png")
    # premier plan : l'icône réduite à la zone visible (72 dp sur 108), bords fondus
    fgsize = 300
    fg_img = master.resize((fgsize, fgsize), Image.LANCZOS).convert("RGBA")
    fm = Image.new("L", (fgsize, fgsize), 0); ImageDraw.Draw(fm).rounded_rectangle((10, 10, fgsize - 10, fgsize - 10), radius=70, fill=255)
    fg_img.putalpha(fm.filter(ImageFilter.GaussianBlur(9)))
    fg = Image.new("RGBA", (432, 432), (0, 0, 0, 0)); fg.alpha_composite(fg_img, ((432 - fgsize) // 2, (432 - fgsize) // 2))
    fg.save(f"{A}/unity-adaptive/foreground-432.png")
    comp = bg.convert("RGBA"); comp.alpha_composite(fg)
    for d, f in {"mdpi": 1, "hdpi": 1.5, "xhdpi": 2, "xxhdpi": 3, "xxxhdpi": 4}.items():
        dd = f"{A}/res/mipmap-{d}"; os.makedirs(dd)
        layer = round(108 * f); lp = round(48 * f)
        fg.resize((layer, layer), Image.LANCZOS).save(f"{dd}/ic_launcher_foreground.png")
        bg.resize((layer, layer), Image.LANCZOS).save(f"{dd}/ic_launcher_background.png")
        big = master.resize((lp * 4, lp * 4), Image.LANCZOS)
        for kind in ("", "_round"):
            m = Image.new("L", big.size, 0); dr = ImageDraw.Draw(m)
            if kind: dr.ellipse((0, 0, big.width - 1, big.height - 1), fill=255)
            else: dr.rounded_rectangle((0, 0, big.width - 1, big.height - 1), radius=big.width * 0.22, fill=255)
            o = big.convert("RGBA"); o.putalpha(m); o.resize((lp, lp), Image.LANCZOS).save(f"{dd}/ic_launcher{kind}.png")
    xml = ('<?xml version="1.0" encoding="utf-8"?>\n<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">\n'
           '    <background android:drawable="@mipmap/ic_launcher_background"/>\n    <foreground android:drawable="@mipmap/ic_launcher_foreground"/>\n</adaptive-icon>\n')
    write(f"{A}/res/mipmap-anydpi-v26/ic_launcher.xml", xml); write(f"{A}/res/mipmap-anydpi-v26/ic_launcher_round.xml", xml)
    master.resize((512, 512), Image.LANCZOS).save(f"{A}/play-store-512.png")

    # ------------------------------------------------------------ aperçu
    prev = Image.new("RGB", (1500, 640), P["lin"]["150"])
    def paste(img, x, y, s, shape):
        img = img.resize((s, s), Image.LANCZOS).convert("RGBA")
        m = Image.new("L", (s * 4, s * 4), 0); d = ImageDraw.Draw(m)
        if shape == "circle": d.ellipse((0, 0, s * 4, s * 4), fill=255)
        else: d.rounded_rectangle((0, 0, s * 4, s * 4), radius=s * 4 * (0.225 if shape == "ios" else 0.3), fill=255)
        prev.paste(img, (x, y), m.resize((s, s), Image.LANCZOS))
    paste(master, 40, 60, 260, "ios")
    tint = Image.composite(Image.new("RGB", (1024, 1024), (236, 190, 150)), Image.new("RGB", (1024, 1024), (22, 18, 16)), tinted)
    paste(tint, 330, 60, 260, "ios")
    vis = comp.crop((36, 36, 396, 396)).convert("RGB")
    paste(vis, 640, 60, 260, "circle"); paste(vis, 940, 60, 260, "rounded")
    x = 60
    for s in (180, 120, 76, 60, 40, 29):
        paste(master, x, 610 - s, s, "ios"); x += s + 40
    prev.save(f"{APP}/apercu-icones.png")
    print("ok", bg_col)
