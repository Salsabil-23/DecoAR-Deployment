"""Assets DecoAR : AR, états vides, splash, store, sprites 9-slice Unity, enseigne 3D."""
from PIL import Image, ImageDraw, ImageFilter
from common import *
import logo as L
import brand_raster as BR
_SYM = BR.data_uri(BR.symbol(512, shadow=False))
_SYMS = BR.data_uri(BR.symbol(512, shadow=True))
def SYM(op=1, shadow=False):
    u = _SYMS if shadow else _SYM
    return f'<image x="0" y="3" width="120" height="120" opacity="{op}" xlink:href="{u}" href="{u}"/>'
import tokens as T

C = T.PALETTE
lin, ter, sau, lai = C["lin"], C["terracotta"], C["sauge"], C["laiton"]
OUT = f"{ROOT}/assets"

def doc(w, h, body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}" viewBox="0 0 {w} {h}">{body}</svg>\n'

def both(name, w, h, body, px_w=None):
    p = f"{OUT}/{name}.svg"; write(p, doc(w, h, body)); png(p, f"{OUT}/{name}.png", px_w or w)

# ===================================================================== AR
def reticle(state):
    col = {"recherche": lin["100"], "valide": lai["400"], "refuse": C["brique"]["300"]}[state]
    dash = 'stroke-dasharray="118 39" stroke-dashoffset="59"' if state != "valide" else ""
    inner = (f'<circle cx="256" cy="256" r="10" fill="{col}"/>' if state != "refuse" else
             f'<path d="M232 232 L280 280 M280 232 L232 280" stroke="{col}" stroke-width="14" stroke-linecap="round"/>')
    return f'''<defs><radialGradient id="g" cx=".5" cy=".5" r=".5">
    <stop offset=".55" stop-color="{col}" stop-opacity="0"/><stop offset=".82" stop-color="{col}" stop-opacity=".28"/><stop offset="1" stop-color="{col}" stop-opacity="0"/></radialGradient></defs>
  <circle cx="256" cy="256" r="250" fill="url(#g)"/>
  <circle cx="256" cy="256" r="200" fill="none" stroke="{col}" stroke-width="14" stroke-linecap="round" {dash}/>
  <circle cx="256" cy="256" r="150" fill="none" stroke="{col}" stroke-width="3" opacity=".45"/>
  {inner}'''

def surface_tile():
    dots = "".join(f'<circle cx="{x}" cy="{y}" r="2.6" fill="{lin["100"]}" fill-opacity=".38"/>'
                   for x in range(16, 256, 32) for y in range(16, 256, 32))
    return dots

# ===================================================================== ÉTATS VIDES (480x360, sans contour : lisibles sur verre et papier)
FLOORE = f'<ellipse cx="240" cy="300" rx="150" ry="22" fill="{ter["300"]}" opacity=".28"/>'
def spark(x, y, s, col):
    return f'<path d="M{x} {y-s} C{x+.15*s} {y-.15*s} {x+.15*s} {y-.15*s} {x+s} {y} C{x+.15*s} {y+.15*s} {x+.15*s} {y+.15*s} {x} {y+s} C{x-.15*s} {y+.15*s} {x-.15*s} {y+.15*s} {x-s} {y} C{x-.15*s} {y-.15*s} {x-.15*s} {y-.15*s} {x} {y-s}Z" fill="{col}"/>'
def iso_cube(cx, cy, r, top, left, right, op=1, dash=False):
    h = r * .577; k = r * 1.15
    t = f"M{cx} {cy-2*h} L{cx+r} {cy-h} L{cx} {cy} L{cx-r} {cy-h}Z"
    l = f"M{cx-r} {cy-h} L{cx} {cy} L{cx} {cy+k} L{cx-r} {cy-h+k}Z"
    rr = f"M{cx} {cy} L{cx+r} {cy-h} L{cx+r} {cy-h+k} L{cx} {cy+k}Z"
    if dash:
        st = f'fill="none" stroke="{top}" stroke-width="5" stroke-dasharray="12 9" stroke-linejoin="round" stroke-linecap="round"'
        return f'<path d="{t} {l} {rr}" {st}/>'
    return f'<g opacity="{op}"><path d="{l}" fill="{left}"/><path d="{rr}" fill="{right}"/><path d="{t}" fill="{top}"/></g>'
def pedestal(cx=240, y=250):
    return (f'<ellipse cx="{cx}" cy="{y+40}" rx="96" ry="22" fill="{lin["600"]}"/>'
            f'<rect x="{cx-96}" y="{y}" width="192" height="40" fill="{lin["500"]}"/>'
            f'<ellipse cx="{cx}" cy="{y}" rx="96" ry="22" fill="{lin["300"]}"/>')

EMPTY = {
  "panier-vide": ("Ton panier est vide", "Ajoute un coup de cœur depuis le catalogue.",
    FLOORE + f'''<path d="M160 130 H320 L334 282 Q336 300 318 300 H162 Q144 300 146 282 Z" fill="{ter["400"]}"/>
    <path d="M160 130 H320 L322 152 H158 Z" fill="{ter["500"]}"/>
    <path d="M196 160 V118 A44 44 0 0 1 284 118 V160" fill="none" stroke="{ter["600"]}" stroke-width="12" stroke-linecap="round"/>
    <path d="M206 222 Q240 250 274 222" fill="none" stroke="{lin["100"]}" stroke-width="10" stroke-linecap="round" opacity=".9"/>
    <circle cx="208" cy="200" r="7" fill="{lin["100"]}"/><circle cx="272" cy="200" r="7" fill="{lin["100"]}"/>
    {spark(360, 110, 18, lai["400"])}{spark(118, 170, 11, sau["400"])}'''),
  "catalogue-3d-vide": ("Aucun produit 3D pour l'instant", "Génère ton premier modèle à partir d'une photo.",
    pedestal() + iso_cube(240, 196, 52, lai["300"], "", "", dash=True) + spark(330, 92, 20, ter["400"]) + spark(150, 120, 12, sau["400"])),
  "modele-3d-attente": ("Ton modèle 3D apparaîtra ici", "Prends une photo du produit pour commencer.",
    pedestal() + iso_cube(240, 196, 52, lin["400"], "", "", dash=True) +
    f'<path d="M150 150 A100 34 0 0 0 330 150" fill="none" stroke="{ter["400"]}" stroke-width="7" stroke-linecap="round"/><path d="M330 150 L317 137 M330 150 L342 138" stroke="{ter["400"]}" stroke-width="7" stroke-linecap="round"/>'),
  "aucun-resultat": ("Aucun résultat", "Essaie un autre mot ou un autre rayon.",
    FLOORE + f'''<rect x="120" y="96" width="240" height="196" rx="10" fill="{lin["300"]}"/>
    <rect x="136" y="112" width="208" height="70" fill="{sau["500"]}"/><rect x="136" y="200" width="208" height="76" fill="{sau["500"]}"/>
    <rect x="152" y="140" width="22" height="42" rx="3" fill="{ter["400"]}"/><rect x="180" y="128" width="18" height="54" rx="3" fill="{lai["300"]}"/>
    <rect x="276" y="232" width="48" height="44" rx="6" fill="{ter["300"]}"/>
    <circle cx="268" cy="176" r="56" fill="{lin["50"]}" fill-opacity=".85"/><circle cx="268" cy="176" r="56" fill="none" stroke="{ter["600"]}" stroke-width="14"/>
    <path d="M308 216 L350 258" stroke="{ter["600"]}" stroke-width="20" stroke-linecap="round"/>
    <path d="M248 166 Q268 150 288 166" fill="none" stroke="{lin["700"]}" stroke-width="7" stroke-linecap="round"/><path d="M252 194 H284" stroke="{lin["700"]}" stroke-width="7" stroke-linecap="round"/>'''),
  "camera-refusee": ("La caméra est désactivée", "Autorise-la dans les réglages pour essayer en AR.",
    FLOORE + f'''<rect x="130" y="120" width="220" height="160" rx="30" fill="{lin["600"]}"/>
    <path d="M190 120 L208 92 H272 L290 120Z" fill="{lin["600"]}"/>
    <circle cx="240" cy="200" r="50" fill="{lin["300"]}"/><circle cx="240" cy="200" r="28" fill="{lin["700"]}"/>
    <path d="M120 300 L360 90" stroke="{C["brique"]["500"]}" stroke-width="18" stroke-linecap="round"/>'''),
  "hors-ligne": ("Pas de connexion", "Vérifie ton réseau puis réessaie.",
    FLOORE + f'''<path d="M150 250 Q110 250 110 212 Q110 176 150 172 Q156 116 214 112 Q258 110 276 146 Q296 132 320 140 Q356 152 352 190 Q382 198 380 226 Q378 252 346 252 Z" fill="{lin["300"]}"/>
    <path d="M196 214 Q240 184 284 214" fill="none" stroke="{lin["600"]}" stroke-width="12" stroke-linecap="round"/>
    <circle cx="240" cy="226" r="9" fill="{lin["600"]}"/>
    <path d="M170 280 L310 120" stroke="{C["brique"]["500"]}" stroke-width="14" stroke-linecap="round"/>'''),
  "paiement-confirme": ("Merci pour ta commande !", "Ton colis est en préparation.",
    FLOORE + iso_cube(240, 210, 92, lai["200"], lai["400"], lai["300"]) +
    f'''<path d="M194 150 L286 203" stroke="{ter["500"]}" stroke-width="14"/>
    <circle cx="336" cy="116" r="40" fill="{sau["600"]}"/><path d="M318 117 L332 131 L356 104" fill="none" stroke="#fff" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>
    {spark(132, 120, 16, ter["400"])}{spark(392, 210, 11, lai["500"])}'''),
  "aucune-photo": ("Aucune photo", "Prends une photo nette du produit, sur un fond uni.",
    FLOORE + f'''<rect x="140" y="96" width="200" height="190" rx="22" fill="{lin["300"]}"/>
    <rect x="160" y="116" width="160" height="124" rx="12" fill="{lin["150"]}"/>
    <circle cx="200" cy="152" r="16" fill="{lai["400"]}"/>
    <path d="M160 226 L216 178 L254 212 L280 192 L320 226 V228 Q320 240 308 240 H172 Q160 240 160 228Z" fill="{sau["400"]}"/>
    <circle cx="330" cy="104" r="34" fill="{ter["500"]}"/><path d="M330 88 V120 M314 104 H346" stroke="#fff" stroke-width="9" stroke-linecap="round"/>'''),
}

# ===================================================================== SPLASH / STORE / ENSEIGNE
def splash(w, h):
    s = min(w, h) / 1080 * 3.4
    cy = h * 0.43
    wm_size = 124 * min(w, h) / 1080
    _, ww = L.wordmark(0, 0, wm_size)
    wm, _ = L.wordmark((w - ww) / 2, cy + 88 * s + wm_size * 0.2, wm_size)
    tg, tw = text_path("ESSAIE · POSE · ADOPTE", wm_size * 0.27, 500, 0, 0, 0.12)
    tg, _ = text_path("ESSAIE · POSE · ADOPTE", wm_size * 0.27, 500, (w - tw) / 2, cy + 88 * s + wm_size * 0.75, 0.12)
    return f'''<defs><linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{lin["50"]}"/><stop offset="1" stop-color="{ter["100"]}"/></linearGradient>
  <radialGradient id="sun" cx=".5" cy=".9" r=".75"><stop offset="0" stop-color="{lai["100"]}"/><stop offset="1" stop-color="{lai["100"]}" stop-opacity="0"/></radialGradient></defs>
  <rect width="{w}" height="{h}" fill="url(#bg)"/><rect width="{w}" height="{h}" fill="url(#sun)"/>
  <g transform="translate({w/2 - 60*s} {cy - 63*s}) scale({s})">{SYM(shadow=True)}</g>
  {wm}<path d="{tg}" fill="{lin["700"]}"/>'''

def feature_graphic():
    w, h = 1024, 500
    wm, ww = L.wordmark(470, 250, 104, lin["50"], ter["300"])
    t1, _ = text_path("Essaie tes meubles chez toi,", 34, 500, 474, 318)
    t2, _ = text_path("à taille réelle, avant d'acheter.", 34, 500, 474, 362)
    return f'''<defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{lin["850"]}"/><stop offset="1" stop-color="{lin["1000"]}"/></linearGradient>
  <radialGradient id="glow" cx=".24" cy=".6" r=".45"><stop offset="0" stop-color="{ter["500"]}" stop-opacity=".45"/><stop offset="1" stop-color="{ter["500"]}" stop-opacity="0"/></radialGradient></defs>
  <rect width="{w}" height="{h}" fill="url(#bg)"/><rect width="{w}" height="{h}" fill="url(#glow)"/>
  <g transform="translate({250 - 60*3.3} {250 - 63*3.3}) scale(3.3)">{SYM(shadow=True)}</g>
  {wm}<path d="{t1}" fill="{lin["300"]}"/><path d="{t2}" fill="{lin["300"]}"/>'''

def enseigne():
    """Texture pour l'enseigne 3D de l'étagère (M3) : bois sombre, liseré laiton."""
    w, h = 1024, 256
    wm, ww = L.wordmark(306, 150, 96, lin["100"], ter["300"])
    sub, _ = text_path("RAYON", 26, 600, 312, 196, 0.2)
    grain = "".join(f'<path d="M0 {y} C300 {y-6} 700 {y+6} 1024 {y-2}" stroke="#000" stroke-opacity=".10" stroke-width="2" fill="none"/>' for y in range(20, 256, 22))
    return f'''<rect width="{w}" height="{h}" rx="28" fill="{lin["900"]}"/>{grain}
  <rect x="10" y="10" width="{w-20}" height="{h-20}" rx="20" fill="none" stroke="{lai["500"]}" stroke-width="6"/>
  <g transform="translate(56 26) scale(1.65)">{SYM()}</g>{wm}<path d="{sub}" fill="{lai["400"]}"/>'''

# ===================================================================== SPRITES 9-SLICE (blancs, à teinter)
def sprites():
    d = f"{OUT}/unity-sprites"; os.makedirs(d, exist_ok=True)
    def rrect(name, size, radius, stroke=0):
        S = 4; im = Image.new("L", (size*S, size*S), 0); dr = ImageDraw.Draw(im)
        if stroke:
            dr.rounded_rectangle((0, 0, size*S-1, size*S-1), radius=radius*S, outline=255, width=stroke*S)
        else:
            dr.rounded_rectangle((0, 0, size*S-1, size*S-1), radius=radius*S, fill=255)
        a = im.resize((size, size), Image.LANCZOS)
        out = Image.new("RGBA", (size, size), (255, 255, 255, 0)); out.putalpha(a); out.save(f"{d}/{name}.png")
    for r in (12, 20, 28, 40, 56):
        rrect(f"dc-rect-r{r}", r*2+4, r)
        rrect(f"dc-rect-r{r}-contour", r*2+4, r, 3)
    rrect("dc-cercle-128", 128, 64); rrect("dc-cercle-128-contour", 128, 64, 4)
    # ombre douce (verre) : rectangle arrondi flou
    for r in (28, 56):
        pad = 40; size = r*2 + 4 + pad*2
        im = Image.new("L", (size, size), 0); dr = ImageDraw.Draw(im)
        dr.rounded_rectangle((pad, pad, size-pad, size-pad), radius=r, fill=255)
        im = im.filter(ImageFilter.GaussianBlur(16))
        out = Image.new("RGBA", (size, size), (0, 0, 0, 0)); out.putalpha(im); out.save(f"{d}/dc-ombre-r{r}.png")
    # dégradé vertical (voile de lisibilité pour bandeaux sur caméra)
    g = Image.new("L", (4, 256)); g.putdata([int(255*(1-y/255)**1.6) for y in range(256) for _ in range(4)])
    out = Image.new("RGBA", (4, 256), (0, 0, 0, 0)); out.putalpha(g); out.save(f"{d}/dc-voile-haut.png")

if __name__ == "__main__":
    for st in ("recherche", "valide", "refuse"):
        both(f"ar/reticule-{st}", 512, 512, reticle(st))
    both("ar/motif-surface", 256, 256, surface_tile())
    both("ar/surface-logo", 512, 512, f'<g opacity=".55" transform="translate(16 16) scale(4)">{SYM(.5)}</g>')
    for n, (title, sub, body) in EMPTY.items():
        both(f"etats-vides/{n}", 480, 360, body, 960)
    both("splash/splash-portrait", 1080, 1920, splash(1080, 1920))
    both("splash/splash-paysage", 1920, 1080, splash(1920, 1080))
    both("store/play-store-banniere-1024x500", 1024, 500, feature_graphic())
    both("3d/enseigne-etagere", 1024, 256, enseigne())
    from icons import ICONS, icon_svg
    both("avatar/avatar-invite", 256, 256,
         f'<circle cx="128" cy="128" r="128" fill="{lin["300"]}"/><g transform="translate(56 50) scale(6)" color="{lin["700"]}" fill="none" stroke="{lin["700"]}" stroke-width="2" stroke-linecap="round">{ICONS["profil"][2]}</g>')
    sprites()
    import json
    json.dump({k: {"titre": v[0], "texte": v[1]} for k, v in EMPTY.items()}, open(f"{OUT}/etats-vides/textes.json", "w"), ensure_ascii=False, indent=2)
    print("ok")
