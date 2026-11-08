"""DecoAR — logo : 3 pistes + version finale « Le Portail »."""
from common import *
import tokens as T

C = T.PALETTE
TERRA, TERRA_D, TERRA_L = C["terracotta"]["500"], C["terracotta"]["700"], C["terracotta"]["400"]
CREME, CREME_2, LAITON, LAITON_L = C["lin"]["100"], C["lin"]["200"], C["laiton"]["500"], C["laiton"]["200"]
INK, SAUGE = C["lin"]["900"], C["sauge"]["500"]

# --------------------------------------------------------------- géométrie partagée (boîte 120x120)
ARCH = "M24 106 V56 A36 36 0 0 1 96 56 V106 Z"
def cube(cx, cy, r):
    """cube isométrique ; (cx, cy) = sommet central, r = demi-diagonale horizontale."""
    h = r * 0.577  # tan 30°
    top = f"M{cx} {cy-2*h} L{cx+r} {cy-h} L{cx} {cy} L{cx-r} {cy-h} Z"
    left = f"M{cx-r} {cy-h} L{cx} {cy} L{cx} {cy+r*1.15} L{cx-r} {cy-h+r*1.15} Z"
    right = f"M{cx} {cy} L{cx+r} {cy-h} L{cx+r} {cy-h+r*1.15} L{cx} {cy+r*1.15} Z"
    return top, left, right

def portail(arch_fill="url(#dcArch)", top=C["lin"]["50"], left=C["lin"]["200"], right=C["laiton"]["300"], ring=CREME, defs=True, mono=None, glow=True):
    """Symbole final : une arche (porte, fenêtre — clin d'œil aux portes de Sidi Bou Saïd)
    qui s'ouvre sur un objet 3D posé sur un anneau de placement AR."""
    t, l, r = cube(60, 66, 17)
    if mono:  # version monochrome : arche pleine, cube et anneau évidés
        return f'''<path d="{ARCH}" fill="{mono}"/>
  <g fill="none" stroke="#000" stroke-width="0"/>
  <path d="{ARCH} {t} {l} {r}" fill="{mono}" fill-rule="evenodd"/>'''
    g = ""
    if defs:
        g += f'''<defs>
    <linearGradient id="dcArch" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{TERRA_L}"/><stop offset="1" stop-color="{TERRA}"/>
    </linearGradient>
    <radialGradient id="dcGlow" cx="60" cy="88" r="34" gradientUnits="userSpaceOnUse" gradientTransform="translate(0 60) scale(1 .32) translate(0 -60)">
      <stop offset="0" stop-color="#FFF4DC" stop-opacity=".55"/><stop offset="1" stop-color="#FFF4DC" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="dcBeam" x1="0" y1="1" x2="0" y2="0">
      <stop offset="0" stop-color="#FFF4DC" stop-opacity=".30"/><stop offset="1" stop-color="#FFF4DC" stop-opacity="0"/>
    </linearGradient>
  </defs>
  '''
    g += f'''<path d="{ARCH}" fill="{arch_fill}"/>
  ''' + ('''<path d="M37 92 L44 40 L76 40 L83 92 Z" fill="url(#dcBeam)"/>
  <ellipse cx="60" cy="92" rx="34" ry="11" fill="url(#dcGlow)"/>
  ''' if glow else "") + f'''  <ellipse cx="60" cy="92" rx="23" ry="6.5" fill="none" stroke="{ring}" stroke-width="3.2" stroke-dasharray="20.5 6" stroke-dashoffset="23.5" stroke-linecap="round" opacity=".9"/>
  <path d="{l}" fill="{left}"/>
  <path d="{r}" fill="{right}"/>
  <path d="{t}" fill="{top}"/>'''
    return g

def portail_mono(color):
    """Version une couleur : arche pleine + cube et anneau en réserve (découpe)."""
    t, l, r = cube(60, 66, 17)
    return f'''<defs><mask id="dcCut"><rect width="120" height="120" fill="#fff"/>
    <path d="{t}" fill="#000"/><path d="{l}" fill="#000"/><path d="{r}" fill="#000"/>
    <ellipse cx="60" cy="92" rx="23" ry="6.5" fill="none" stroke="#000" stroke-width="3.2" stroke-dasharray="20.5 6" stroke-dashoffset="23.5" stroke-linecap="round"/>
    <path d="M60 66 L60 85.5 M43 56.2 L60 66 L77 56.2" stroke="#fff" stroke-width="2.6" fill="none" stroke-linejoin="round"/>
  </mask></defs>
  <path d="{ARCH}" fill="{color}" mask="url(#dcCut)"/>'''

# --------------------------------------------------------------- pistes alternatives
def piste_coin():
    """Piste B — « Le Coin » : un coin de pièce isométrique (2 murs + sol) qui dessine un D."""
    return f'''<path d="M30 30 L60 16 L60 70 L30 84 Z" fill="{TERRA}"/>
  <path d="M60 16 L92 32 Q100 36 100 46 L100 70 L60 70 Z" fill="{TERRA_L}"/>
  <path d="M30 84 L60 70 L100 70 L70 98 Z" fill="{C['sauge']['300']}"/>
  <circle cx="66" cy="82" r="6" fill="{LAITON}"/>'''

def piste_ancre():
    """Piste C — « L'Ancre » : un fauteuil dans des coins de visée AR."""
    br = f'stroke="{TERRA}" stroke-width="7" stroke-linecap="round" fill="none"'
    return f'''<path d="M18 38 V22 H34 M86 22 H102 V38 M102 82 V98 H86 M34 98 H18 V82" {br}/>
  <rect x="38" y="44" width="44" height="26" rx="10" fill="{SAUGE}"/>
  <rect x="32" y="58" width="56" height="22" rx="9" fill="{TERRA}"/>
  <rect x="38" y="78" width="6" height="10" rx="2" fill="{INK}"/><rect x="76" y="78" width="6" height="10" rx="2" fill="{INK}"/>'''

def svg(content, w=120, h=120, vb=None, bg=None):
    vb = vb or f"0 0 {w} {h}"
    b = f'<rect width="100%" height="100%" fill="{bg}"/>' if bg else ""
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{vb}">{b}\n  {content}\n</svg>\n'

# --------------------------------------------------------------- wordmark
def wordmark(x, baseline, size, deco=INK, ar=TERRA):
    d1, w1 = text_path("Deco", size, 600, x, baseline, -0.01)
    d2, w2 = text_path("AR", size, 600, x + w1 - size * 0.035, baseline, -0.01)
    return f'<path d="{d1}" fill="{deco}"/><path d="{d2}" fill="{ar}"/>', w1 + w2 - size * 0.035

def lockup_h(deco=INK, ar=TERRA, symbol=None, tagline=None, tag_col=None):
    """Logo horizontal : symbole 120 + mot-symbole."""
    size = 64
    wm, w = wordmark(132, 84, size, deco, ar)
    sym = symbol if symbol is not None else portail()
    extra = ""
    H = 120
    if tagline:
        td, tw = text_path(tagline, 19, 500, 134, 110, 0.06)
        extra = f'<path d="{td}" fill="{tag_col or deco}" opacity=".75"/>'
    W = round(132 + w + 8)
    return svg(f'{sym}\n  {wm}{extra}', W, H)

def lockup_v(deco=INK, ar=TERRA, symbol=None):
    size = 52
    _, w = wordmark(0, 0, size)
    W = max(round(w) + 20, 200)
    wm, _ = wordmark((W - w) / 2, 178, size, deco, ar)
    sym = symbol if symbol is not None else portail()
    return svg(f'<g transform="translate({(W-120)/2} 0)">{sym}</g>\n  {wm}', W, 196)

if __name__ == "__main__":
    out = f"{ROOT}/logo"
    # pistes
    write(f"{out}/pistes/piste-A-le-portail.svg", svg(portail()))
    write(f"{out}/pistes/piste-B-le-coin.svg", svg(piste_coin()))
    write(f"{out}/pistes/piste-C-l-ancre.svg", svg(piste_ancre()))
    # symbole
    write(f"{out}/symbole/decoar-symbole.svg", svg(portail()))
    write(f"{out}/symbole/decoar-symbole-sur-sombre.svg", svg(portail(), bg=INK))
    write(f"{out}/symbole/decoar-symbole-mono-encre.svg", svg(portail_mono(INK)))
    write(f"{out}/symbole/decoar-symbole-mono-creme.svg", svg(portail_mono(CREME)))
    write(f"{out}/symbole/decoar-symbole-mono-terracotta.svg", svg(portail_mono(TERRA)))
    # lockups
    write(f"{out}/horizontal/decoar-logo-horizontal.svg", lockup_h())
    write(f"{out}/horizontal/decoar-logo-horizontal-sur-sombre.svg", lockup_h(deco=CREME, ar=TERRA_L))
    write(f"{out}/horizontal/decoar-logo-horizontal-mono-encre.svg", lockup_h(deco=INK, ar=INK, symbol=portail_mono(INK)))
    write(f"{out}/horizontal/decoar-logo-horizontal-mono-creme.svg", lockup_h(deco=CREME, ar=CREME, symbol=portail_mono(CREME)))
    write(f"{out}/horizontal/decoar-logo-signature.svg", lockup_h(tagline="ESSAIE · POSE · ADOPTE", tag_col=INK))
    write(f"{out}/vertical/decoar-logo-vertical.svg", lockup_v())
    write(f"{out}/vertical/decoar-logo-vertical-sur-sombre.svg", lockup_v(deco=CREME, ar=TERRA_L))
    # mot-symbole seul
    wm, w = wordmark(4, 66, 64)
    write(f"{out}/mot-symbole/decoar-mot-symbole.svg", svg(wm, round(w + 8), 84))
    wm, w = wordmark(4, 66, 64, CREME, TERRA_L)
    write(f"{out}/mot-symbole/decoar-mot-symbole-creme.svg", svg(wm, round(w + 8), 84))
    print("ok")
