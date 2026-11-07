import os, subprocess
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen

ROOT = "/home/claude/DecoAR-DesignSystem"
FONTS = f"{ROOT}/fonts"
_fonts = {}

def font(weight):
    name = {400: "400Regular", 500: "500Medium", 600: "600SemiBold", 700: "700Bold"}[weight]
    if name not in _fonts:
        _fonts[name] = TTFont(f"{FONTS}/Outfit_{name}.ttf")
    return _fonts[name]

def text_path(text, size, weight=600, x=0, y=0, tracking_em=0.0):
    """Return (svg path d, width) for text with baseline at y."""
    f = font(weight)
    upm = f["head"].unitsPerEm
    cmap = f.getBestCmap(); gs = f.getGlyphSet(); hmtx = f["hmtx"]
    s = size / upm
    pen = SVGPathPen(gs)
    cx = 0.0
    kern = {}
    for ch in text:
        g = cmap[ord(ch)]
        tp = TransformPen(pen, (s, 0, 0, -s, x + cx * s, y))
        gs[g].draw(tp)
        cx += hmtx[g][0] + tracking_em * upm
    width = (cx - tracking_em * upm) * s
    return pen.getCommands(), width

def cap_height(size, weight=600):
    f = font(weight)
    return f["OS/2"].sCapHeight * size / f["head"].unitsPerEm

def write(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w").write(content)

def png(svg_path, png_path, w, h=None):
    os.makedirs(os.path.dirname(png_path), exist_ok=True)
    args = ["rsvg-convert", "-w", str(w)]
    if h: args += ["-h", str(h)]
    subprocess.run(args + ["-o", png_path, svg_path], check=True)
