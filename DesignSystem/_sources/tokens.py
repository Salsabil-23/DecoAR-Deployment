"""DecoAR design tokens — single source of truth. Run: python3 tokens.py OUTDIR"""
import json, sys, os

OUT = sys.argv[1] if len(sys.argv) > 1 else "."

# ---------------------------------------------------------------- PRIMITIVES
PALETTE = {
    "terracotta": {  # couleur de marque, alignée sur l'icône d'application
        "50": "#FFF4EC", "100": "#FDE3D2", "200": "#FBC5A3", "300": "#F7A574", "400": "#EE824B",
        "500": "#DF5F2A", "600": "#C04A1E", "700": "#983A19", "800": "#6E2C16", "900": "#491E11"},
    "sauge": {
        "50": "#F3F7F1", "100": "#E2ECDE", "200": "#C6D8BF", "300": "#A8C29F", "400": "#8DAB83",
        "500": "#73946E", "600": "#5D7B59", "700": "#486146", "800": "#354734", "900": "#232F22"},
    "laiton": {
        "50": "#FCF7EC", "100": "#F7ECD3", "200": "#F0DDB1", "300": "#E7CB8C", "400": "#DDB86C",
        "500": "#D1A652", "600": "#B08739", "700": "#8A682C", "800": "#634A20", "900": "#3F2F15"},
    "lin": {  # neutres chauds (crème -> encre)
        "0": "#FFFFFF", "50": "#FDF9F3", "100": "#FAF2E3", "150": "#F4EADA", "200": "#EDE1CD",
        "300": "#DDCDB5", "400": "#C4B19A", "500": "#A8937E", "600": "#8C7866", "700": "#6B5B4D",
        "800": "#4A3E34", "850": "#352C24", "900": "#29211A", "950": "#1F1A14", "1000": "#15110D"},
    "brique": {"300": "#F2A08F", "500": "#D15C45", "600": "#B94A35", "700": "#9E3F2D"},
    "ardoise": {"300": "#9DB7CB", "500": "#5F7E96", "700": "#3F576A"},
    "succes": {"300": "#9ECC94", "500": "#5E9A58", "700": "#3F6E3B"},
}

POSE = {"mur": "#DB9E80", "sol": "#9EC794", "table": "#E6C275"}

GRADIENTS = {
    "halo-ar": ["#EE824B", "#DDB86C"],          # moments AR / génération 3D
    "aube": ["#FFF4EC", "#FDE3D2"],             # fonds papier (splash, auth)
    "braise": ["#EE824B", "#C04A1E"],           # icône d'app, grands aplats marque
}

def P(path):
    fam, step = path.split(".")
    return PALETTE[fam][step]

# ---------------------------------------------------------------- THEMES (semantic)
# alpha in 0..1. value = (hex, alpha)
THEMES = {
    "verre": {   # tout ce qui flotte sur la 3D ou la caméra
        "surface/glass":         (P("lin.950"), 0.80),
        "surface/glass-strong":  (P("lin.950"), 0.94),
        "surface/tile":          ("#FFFFFF", 0.07),
        "surface/tile-active":   ("#FFFFFF", 0.16),
        "surface/scrim":         ("#000000", 0.40),
        "border/hairline":       ("#FFFFFF", 0.12),
        "border/strong":         ("#FFFFFF", 0.22),
        "text/primary":          (P("lin.100"), 1),
        "text/secondary":        (P("lin.300"), 1),
        "text/tertiary":         (P("lin.400"), 1),
        "icon/default":          (P("lin.100"), 1),
        "action/primary":        (P("laiton.500"), 1),
        "action/on-primary":     (P("lin.900"), 1),
        "action/commerce":       (P("sauge.600"), 1),
        "action/on-commerce":    (P("lin.0"), 1),
        "action/accent":         (P("terracotta.400"), 1),
        "action/on-accent":      (P("lin.1000"), 1),
        "action/secondary":      ("#FFFFFF", 0.12),
        "action/on-secondary":   (P("lin.100"), 1),
        "state/selected":        (P("laiton.500"), 1),
        "state/focus-ring":      (P("laiton.300"), 1),
        "feedback/success":      (P("succes.300"), 1),
        "feedback/error":        (P("brique.300"), 1),
        "feedback/warning":      (P("laiton.400"), 1),
        "feedback/info":         (P("ardoise.300"), 1),
        "badge/bg":              (P("brique.600"), 1),
        "badge/text":            ("#FFFFFF", 1),
        "price":                 (P("laiton.300"), 1),
    },
    "papier": {  # formulaires : compte, profil, personnage, administration
        "surface/background":    (P("lin.50"), 1),
        "surface/card":          (P("lin.0"), 1),
        "surface/sunken":        (P("lin.150"), 1),
        "surface/scrim":         (P("lin.1000"), 0.45),
        "border/hairline":       (P("lin.200"), 1),
        "border/strong":         (P("lin.300"), 1),
        "text/primary":          (P("lin.900"), 1),
        "text/secondary":        (P("lin.700"), 1),
        "text/tertiary":         (P("lin.600"), 1),
        "icon/default":          (P("lin.800"), 1),
        "action/primary":        (P("terracotta.600"), 1),
        "action/on-primary":     (P("lin.0"), 1),
        "action/commerce":       (P("sauge.600"), 1),
        "action/on-commerce":    (P("lin.0"), 1),
        "action/accent":         (P("laiton.500"), 1),
        "action/on-accent":      (P("lin.900"), 1),
        "action/secondary":      (P("lin.150"), 1),
        "action/on-secondary":   (P("lin.900"), 1),
        "action/link":           (P("terracotta.700"), 1),
        "state/selected":        (P("terracotta.600"), 1),
        "state/focus-ring":      (P("terracotta.300"), 1),
        "feedback/success":      (P("succes.700"), 1),
        "feedback/error":        (P("brique.700"), 1),
        "feedback/warning":      (P("laiton.700"), 1),
        "feedback/info":         (P("ardoise.700"), 1),
        "badge/bg":              (P("brique.600"), 1),
        "badge/text":            ("#FFFFFF", 1),
        "price":                 (P("terracotta.700"), 1),
    },
}

# ---------------------------------------------------------------- SCALES
# Unités « ref » = points du Canvas Unity 1920x1080 (match 0.5).
# Sur un téléphone 20:9 typique : 1 ref ≈ 0.43 dp/pt.
REF_TO_DP = 0.426

TYPE = {  # name: (size_ref, weight, line_height_ratio, letter_spacing_em, usage)
    "display":  (72, 700, 1.05, -0.02, "Splash, grands titres d'onboarding"),
    "titre-1":  (56, 600, 1.10, -0.015, "Titre d'écran (Auth, Profil, Admin)"),
    "titre-2":  (46, 600, 1.15, -0.01, "Titre de tiroir, de fiche, de fenêtre"),
    "titre-3":  (40, 600, 1.20, -0.005, "Section, nom de produit dans la fiche"),
    "corps-l":  (38, 400, 1.40, 0, "Texte d'introduction, consignes AR"),
    "corps":    (34, 400, 1.40, 0, "Texte courant, champs, lignes de liste"),
    "label":    (32, 500, 1.25, 0.005, "Onglets, boutons secondaires, chips"),
    "bouton":   (36, 600, 1.00, 0.005, "Texte des boutons principaux"),
    "legende":  (28, 500, 1.30, 0.01, "Légendes, étiquettes d'outils, mentions"),
    "surtitre": (24, 600, 1.20, 0.08, "SURTITRE en capitales (rayon, étape)"),
    "prix-xl":  (64, 700, 1.00, -0.02, "Prix dans la fiche produit"),
    "prix":     (36, 600, 1.00, 0, "Prix dans les cartes et lignes"),
}

SPACE = {"0": 0, "1": 8, "2": 12, "3": 16, "4": 24, "5": 32, "6": 48, "7": 64, "8": 96, "9": 128}
RADIUS = {"xs": 12, "sm": 20, "md": 28, "lg": 40, "xl": 56, "pill": 999}
SIZE = {
    "touch-min": 104,          # ≈ 44 pt iOS / 48 dp Android
    "hud-button": 112,
    "icon-button": 96,
    "button-height": 112,
    "button-height-compact": 88,
    "field-height": 104,
    "swatch": 88,
    "badge": 40,
    "icon-sm": 40, "icon-md": 52, "icon-lg": 64,
    "screen-margin": 48,
    "gap-hud": 20,
    "drawer-width": 760,
    "product-card": [360, 184],
    "stroke-hairline": 2.5,
    "stroke-icon": 2,          # sur grille 24
}
MOTION = {
    "duration": {"instant": 90, "rapide": 150, "base": 240, "lent": 360, "scene": 450},
    "easing": {
        "standard": [0.2, 0.0, 0.0, 1.0],
        "entree":   [0.05, 0.7, 0.1, 1.0],
        "sortie":   [0.3, 0.0, 0.8, 0.15],
        "ressort":  {"stiffness": 380, "damping": 22},
    },
    "respiration": {"amplitude": 0.025, "periode_ms": 2400},
    "toast_ms": 2600,
}
HAPTIC = {"leger": "toucher, coloris, onglet", "moyen": "pose AR, ajout panier",
          "fort": "paiement confirmé, erreur bloquante"}
ELEVATION = {
    "glass": {"shadow": ("#000000", 0.38), "offset_y": 6, "blur": 16},
    "float": {"shadow": ("#000000", 0.45), "offset_y": 12, "blur": 32},
    "card":  {"shadow": (P("lin.900"), 0.10), "offset_y": 4, "blur": 16},
}

# ---------------------------------------------------------------- HELPERS
def hex_rgb(h):
    h = h.lstrip("#"); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def lum(h):
    def c(v):
        v /= 255; return v/12.92 if v <= 0.03928 else ((v+0.055)/1.055)**2.4
    r, g, b = hex_rgb(h); return 0.2126*c(r)+0.7152*c(g)+0.0722*c(b)

def blend(fg, a, bg):
    f, b = hex_rgb(fg), hex_rgb(bg)
    return "#%02X%02X%02X" % tuple(round(f[i]*a + b[i]*(1-a)) for i in range(3))

def contrast(a, b):
    la, lb = sorted([lum(a), lum(b)], reverse=True); return (la+0.05)/(lb+0.05)

def rgba(h, a):
    r, g, b = hex_rgb(h); return f"rgba({r}, {g}, {b}, {a:g})" if a < 1 else h

# ---------------------------------------------------------------- EXPORTS
def export_json():
    data = {
        "$schema": "DecoAR tokens v1.0", "unit": "ref (Canvas Unity 1920x1080, match 0.5) ; 1 ref ≈ 0.43 dp",
        "palette": PALETTE, "pose": POSE, "gradients": GRADIENTS,
        "themes": {t: {k: {"hex": v[0], "alpha": v[1]} for k, v in d.items()} for t, d in THEMES.items()},
        "typography": {"family": "Outfit", **{k: {"size": s, "weight": w, "lineHeight": lh, "letterSpacingEm": ls, "usage": u, "dp": round(s*REF_TO_DP, 1)} for k, (s, w, lh, ls, u) in TYPE.items()}},
        "space": SPACE, "radius": RADIUS, "size": SIZE, "motion": MOTION, "haptic": HAPTIC,
        "elevation": {k: {"color": v["shadow"][0], "alpha": v["shadow"][1], "offsetY": v["offset_y"], "blur": v["blur"]} for k, v in ELEVATION.items()},
    }
    json.dump(data, open(os.path.join(OUT, "decoar-tokens.json"), "w"), indent=2, ensure_ascii=False)

def export_css():
    L = ["/* DecoAR — tokens CSS (web, Figma, maquettes). Unités en px = ref × 0.43 */", ":root {"]
    for fam, steps in PALETTE.items():
        for s, h in steps.items(): L.append(f"  --dc-{fam}-{s}: {h};")
    for k, h in POSE.items(): L.append(f"  --dc-pose-{k}: {h};")
    for k, (a, b) in GRADIENTS.items(): L.append(f"  --dc-gradient-{k}: linear-gradient(135deg, {a}, {b});")
    L.append("  --dc-font: 'Outfit', system-ui, sans-serif;")
    for k, (s, w, lh, ls, u) in TYPE.items():
        L.append(f"  --dc-type-{k}: {w} {round(s*REF_TO_DP)}px/{lh} var(--dc-font);")
    for k, v in SPACE.items(): L.append(f"  --dc-space-{k}: {round(v*REF_TO_DP)}px;")
    for k, v in RADIUS.items(): L.append(f"  --dc-radius-{k}: {round(v*REF_TO_DP) if v < 999 else 999}px;")
    for k, v in MOTION["duration"].items(): L.append(f"  --dc-duration-{k}: {v}ms;")
    for k, v in MOTION["easing"].items():
        if isinstance(v, list): L.append(f"  --dc-ease-{k}: cubic-bezier({', '.join(map(str, v))});")
    L.append("}")
    for t, d in THEMES.items():
        L.append(f"\n[data-dc-theme=\"{t}\"] {{")
        for k, (h, a) in d.items(): L.append(f"  --dc-{k.replace('/', '-')}: {rgba(h, a)};")
        L.append("}")
    open(os.path.join(OUT, "decoar-tokens.css"), "w").write("\n".join(L) + "\n")

def cs_name(s):
    parts = s.replace("/", "-").split("-"); return "".join(p[:1].upper()+p[1:] for p in parts if p)

def cs_color(h, a=1.0):
    r, g, b = hex_rgb(h); return f"new Color32({r}, {g}, {b}, {round(a*255)})"

def export_cs():
    L = ["// DecoAR Design System — tokens générés (ne pas modifier à la main, voir decoar-tokens.json)",
         "// Unités : points du Canvas (référence 1920x1080, match 0.5).", "using UnityEngine;", "",
         "namespace DecoAR.Design", "{",
         "    /// <summary>Couleurs primitives de la palette DecoAR.</summary>",
         "    public static class DecoPalette", "    {"]
    for fam, steps in PALETTE.items():
        for s, h in steps.items():
            L.append(f"        public static readonly Color32 {cs_name(fam)}{s} = {cs_color(h)};")
    for k, h in POSE.items():
        L.append(f"        public static readonly Color32 Pose{cs_name(k)} = {cs_color(h)};")
    L += ["    }", ""]
    for t, d in THEMES.items():
        L += [f"    /// <summary>Thème « {t} » — couleurs sémantiques.</summary>", f"    public static class Theme{cs_name(t)}", "    {"]
        for k, (h, a) in d.items():
            L.append(f"        public static readonly Color32 {cs_name(k)} = {cs_color(h, a)};")
        L += ["    }", ""]
    L += ["    /// <summary>Échelle typographique (Outfit).</summary>", "    public static class DecoType", "    {",
          "        public readonly struct Style", "        {",
          "            public readonly float Size; public readonly int Weight; public readonly float LineHeight; public readonly float LetterSpacingEm;",
          "            public Style(float s, int w, float lh, float ls) { Size = s; Weight = w; LineHeight = lh; LetterSpacingEm = ls; }",
          "            /// <summary>Espacement TMP : TMP exprime characterSpacing en 1/100 em.</summary>",
          "            public float TmpCharacterSpacing => LetterSpacingEm * 100f;",
          "            /// <summary>Interligne TMP (lineSpacing est relatif à l'interligne natif de la police).</summary>",
          "            public float TmpLineSpacing => (LineHeight - 1.26f) * 100f;",
          "        }"]
    for k, (s, w, lh, ls, u) in TYPE.items():
        L.append(f"        /// <summary>{u}</summary>")
        L.append(f"        public static readonly Style {cs_name(k)} = new Style({s}f, {w}, {lh}f, {ls}f);")
    L += ["    }", "", "    public static class DecoSpace", "    {"]
    for k, v in SPACE.items(): L.append(f"        public const float S{k} = {v}f;")
    L += ["    }", "", "    public static class DecoRadius", "    {"]
    for k, v in RADIUS.items(): L.append(f"        public const float {cs_name(k)} = {v}f;")
    L += ["    }", "", "    public static class DecoSize", "    {"]
    for k, v in SIZE.items():
        if isinstance(v, list): L.append(f"        public static readonly Vector2 {cs_name(k)} = new Vector2({v[0]}f, {v[1]}f);")
        else: L.append(f"        public const float {cs_name(k)} = {v}f;")
    L += ["    }", "", "    public static class DecoMotion", "    {"]
    for k, v in MOTION["duration"].items(): L.append(f"        public const float {cs_name(k)} = {v/1000:g}f;")
    L.append(f"        public const float RespirationAmplitude = {MOTION['respiration']['amplitude']}f;")
    L.append(f"        public const float RespirationPeriode = {MOTION['respiration']['periode_ms']/1000:g}f;")
    L.append(f"        public const float ToastDuree = {MOTION['toast_ms']/1000:g}f;")
    L += ["        /// <summary>Courbe « standard » cubic-bezier(0.2,0,0,1) approchée.</summary>",
          "        public static float EaseStandard(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3.2f); }",
          "        /// <summary>Courbe « entrée » expressive (décélération forte).</summary>",
          "        public static float EaseEntree(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 5f); }",
          "        /// <summary>Courbe « sortie » (accélération).</summary>",
          "        public static float EaseSortie(float t) { t = Mathf.Clamp01(t); return t * t * t; }",
          "        /// <summary>Rebond amorti (pastille compteur, chute dans le panier).</summary>",
          "        public static float Ressort(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Exp(-6f * t) * Mathf.Cos(12f * t); }",
          "    }", "}"]
    open(os.path.join(OUT, "..", "unity", "Scripts", "Runtime", "DecoTokens.cs"), "w").write("\n".join(L) + "\n")

def export_gpl():
    L = ["GIMP Palette", "Name: DecoAR", "Columns: 10", "#"]
    for fam, steps in PALETTE.items():
        for s, h in steps.items():
            r, g, b = hex_rgb(h); L.append(f"{r:3d} {g:3d} {b:3d}\t{fam}-{s}")
    open(os.path.join(OUT, "decoar-palette.gpl"), "w").write("\n".join(L) + "\n")

def contrast_report():
    rows = []
    pairs = {
        "verre": [("text/primary", None), ("text/secondary", None), ("text/tertiary", None), ("price", None),
                  ("feedback/success", None), ("feedback/error", None), ("feedback/info", None),
                  ("action/on-primary", "action/primary"), ("action/on-commerce", "action/commerce"),
                  ("action/on-accent", "action/accent"), ("badge/text", "badge/bg")],
        "papier": [("text/primary", "surface/background"), ("text/secondary", "surface/background"),
                   ("text/tertiary", "surface/card"), ("action/link", "surface/background"), ("price", "surface/card"),
                   ("feedback/error", "surface/card"), ("feedback/success", "surface/card"),
                   ("action/on-primary", "action/primary"), ("action/on-commerce", "action/commerce"),
                   ("action/on-accent", "action/accent"), ("text/primary", "surface/sunken")],
    }
    for t, lst in pairs.items():
        th = THEMES[t]
        for fg, bg in lst:
            fgh = th[fg][0]
            if bg is None:
                # pire cas : verre 80 % sur un fond caméra très clair (blanc)
                bgh = blend(th["surface/glass"][0], th["surface/glass"][1], "#FFFFFF"); bgl = "verre 80 % sur fond blanc (pire cas)"
            else:
                bgh = th[bg][0]; bgl = bg
            c = contrast(fgh, bgh)
            lvl = "AAA" if c >= 7 else "AA" if c >= 4.5 else "AA grand texte" if c >= 3 else "ÉCHEC"
            rows.append((t, fg, fgh, bgl, bgh, c, lvl))
    return rows

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    export_json(); export_css(); export_cs(); export_gpl()
    for r in contrast_report(): print(f"{r[0]:7} {r[1]:22} {r[2]} on {r[4]} ({r[3]}) -> {r[5]:.2f} {r[6]}")
