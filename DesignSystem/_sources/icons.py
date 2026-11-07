"""Jeu d'icônes DecoAR — grille 24, trait 2, extrémités et angles arrondis, teintables."""
from PIL import Image, ImageDraw, ImageFont
from common import *

DOT = lambda x, y, r=1.15: f'<circle cx="{x}" cy="{y}" r="{r}" fill="currentColor" stroke="none"/>'
SPARK = lambda x, y, s: (f'<path d="M{x} {y-s} C{x+0.13*s} {y-0.13*s} {x+0.13*s} {y-0.13*s} {x+s} {y} '
                         f'C{x+0.13*s} {y+0.13*s} {x+0.13*s} {y+0.13*s} {x} {y+s} C{x-0.13*s} {y+0.13*s} {x-0.13*s} {y+0.13*s} {x-s} {y} '
                         f'C{x-0.13*s} {y-0.13*s} {x-0.13*s} {y-0.13*s} {x} {y-s} Z"/>')

ICONS = {
  # ---------------------------------------------------------------- navigation
  "retour":      ("Retour", "Navigation", '<path d="M15 5 L8 12 L15 19"/>'),
  "chevron":     ("Chevron", "Navigation", '<path d="M9 5 L16 12 L9 19"/>'),
  "chevron-bas": ("Dérouler", "Navigation", '<path d="M5 9 L12 16 L19 9"/>'),
  "fermer":      ("Fermer", "Navigation", '<path d="M6 6 L18 18 M18 6 L6 18"/>'),
  "boutique":    ("Boutique", "Navigation", '<path d="M4 10.5 L12 4 L20 10.5 V19 Q20 20 19 20 H5 Q4 20 4 19 Z"/><path d="M9.5 20 V16 A2.5 2.5 0 0 1 14.5 16 V20"/>'),
  "rayon":       ("Rayon", "Navigation", '<path d="M4 3.5 V20.5 M20 3.5 V20.5 M4 12 H20 M4 20.5 H20 M7.5 12 V7 M10.5 12 V8 M14 12 H18 L17.4 8.6 H14.6 Z M7.5 20.5 V16 H12 V20.5"/>'),
  # ---------------------------------------------------------------- boutique
  "panier":      ("Panier", "Boutique", '<path d="M5.5 8 H18.5 L19.6 18.6 A2 2 0 0 1 17.6 20.8 H6.4 A2 2 0 0 1 4.4 18.6 Z"/><path d="M9 10.5 V7 A3 3 0 0 1 15 7 V10.5"/>'),
  "catalogue":   ("Catalogue", "Boutique", '<rect x="4" y="4" width="7" height="7" rx="2"/><rect x="13" y="4" width="7" height="7" rx="2"/><rect x="4" y="13" width="7" height="7" rx="2"/><rect x="13" y="13" width="7" height="7" rx="2"/>'),
  "suggestions": ("Suggestions", "Boutique", SPARK(10, 11, 7.5) + SPARK(18.5, 18, 3)),
  "favori":      ("Favori", "Boutique", '<path d="M12 20 C12 20 3.5 15 3.5 9.3 A4.3 4.3 0 0 1 12 7.4 A4.3 4.3 0 0 1 20.5 9.3 C20.5 15 12 20 12 20 Z"/>'),
  "paiement":    ("Paiement", "Boutique", '<rect x="3" y="5.5" width="18" height="13" rx="2.5"/><path d="M3 10 H21 M7 14.5 H10.5"/>'),
  "colis":       ("Livraison", "Boutique", '<path d="M12 3 L20 7.5 V16.5 L12 21 L4 16.5 V7.5 Z"/><path d="M4 7.5 L12 12 L20 7.5 M12 12 V21 M8 5.25 L16 9.75"/>'),
  "position":    ("Ma position", "Boutique", '<path d="M12 21 C12 21 5 14.6 5 9.6 A7 7 0 0 1 19 9.6 C19 14.6 12 21 12 21 Z"/><circle cx="12" cy="9.6" r="2.6"/>'),
  "profil":      ("Profil", "Boutique", '<circle cx="12" cy="8.5" r="3.7"/><path d="M4.8 20 A7.2 7.2 0 0 1 19.2 20"/>'),
  "reglages":    ("Réglages", "Boutique", '<path d="M4 7 H10.5 M15.5 7 H20 M4 17 H7 M12 17 H20"/><circle cx="13" cy="7" r="2.4"/><circle cx="9.5" cy="17" r="2.4"/>'),
  "recentrer":   ("Recentrer", "Boutique", '<circle cx="12" cy="12" r="6.5"/><path d="M12 2.5 V5.5 M12 18.5 V21.5 M2.5 12 H5.5 M18.5 12 H21.5"/>' + DOT(12, 12, 1.6)),
  # ---------------------------------------------------------------- vue
  "vue-1re":     ("Vue 1re personne", "Vue", '<path d="M2.5 12 C5 7.2 8.4 5 12 5 C15.6 5 19 7.2 21.5 12 C19 16.8 15.6 19 12 19 C8.4 19 5 16.8 2.5 12 Z"/><circle cx="12" cy="12" r="3"/>'),
  "vue-3e":      ("Vue 3e personne", "Vue", '<circle cx="12" cy="6" r="3"/><path d="M6.6 16.6 A5.4 5.4 0 0 1 17.4 16.6"/><path d="M3.5 19.2 A8.5 2.8 0 0 0 20.5 19.2" stroke-dasharray="3.4 2.4"/>'),
  "gyroscope":   ("Gyroscope", "Vue", '<rect x="9" y="5" width="6" height="14" rx="1.8"/><path d="M5.2 6.8 A8 8 0 0 0 5.2 17.2 M18.8 17.2 A8 8 0 0 0 18.8 6.8 M5.2 17.2 L3 16.9 M5.2 17.2 L5.4 15 M18.8 6.8 L21 7.1 M18.8 6.8 L18.6 9"/>'),
  # ---------------------------------------------------------------- quantité / recherche
  "plus":        ("Plus", "Quantité", '<path d="M12 5 V19 M5 12 H19"/>'),
  "moins":       ("Moins", "Quantité", '<path d="M5 12 H19"/>'),
  "loupe":       ("Rechercher", "Recherche", '<circle cx="10.8" cy="10.8" r="6.3"/><path d="M15.6 15.6 L20.5 20.5"/>'),
  "micro":       ("Micro", "Recherche", '<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5.5 11 A6.5 6.5 0 0 0 18.5 11 M12 17.5 V21 M9 21 H15"/>'),
  "micro-actif": ("Micro (écoute)", "Recherche", '<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5.5 11 A6.5 6.5 0 0 0 18.5 11 M12 17.5 V21"/><path d="M2.5 7.5 V11.5 M21.5 7.5 V11.5"/>'),
  # ---------------------------------------------------------------- AR & produit
  "ar":          ("Voir en AR", "AR et produit", '<path d="M12 2.8 L18 6.3 V12.7 L12 16.2 L6 12.7 V6.3 Z"/><path d="M6 6.3 L12 9.8 L18 6.3 M12 9.8 V16.2"/><path d="M4.2 17.2 A8.2 3 0 0 0 19.8 17.2" stroke-dasharray="4.2 2.4"/>'),
  "generer-3d":  ("Générer en 3D", "AR et produit", '<path d="M10 7 L15.5 10.1 V16.4 L10 19.5 L4.5 16.4 V10.1 Z"/><path d="M4.5 10.1 L10 13.2 L15.5 10.1 M10 13.2 V19.5"/>' + SPARK(18.5, 5.5, 3.2)),
  "photo":       ("Photo", "AR et produit", '<rect x="3" y="6.8" width="18" height="13.2" rx="3"/><path d="M8.3 6.8 L9.8 4.2 H14.2 L15.7 6.8"/><circle cx="12" cy="13.3" r="3.6"/>'),
  "galerie":     ("Galerie", "AR et produit", '<rect x="3" y="4" width="18" height="16" rx="2.8"/><circle cx="9" cy="9.5" r="1.8"/><path d="M3.5 17.5 L9 12.6 L13 16 L16 13.6 L20.5 17.5"/>'),
  "coloris":     ("Coloris", "AR et produit", '<circle cx="8.3" cy="9" r="4.3"/><circle cx="15.7" cy="9" r="4.3"/><circle cx="12" cy="15.4" r="4.3"/>'),
  "taille":      ("Taille", "AR et produit", '<path d="M5 19 L19 5 M5 19 V13.5 M5 19 H10.5 M19 5 H13.5 M19 5 V10.5"/>'),
  "taille-reelle": ("Taille réelle", "AR et produit", '<rect x="2.5" y="8" width="19" height="8.5" rx="1.8"/><path d="M6.5 8 V11 M10.5 8 V12.5 M14.5 8 V11 M18.5 8 V12.5"/>'),
  "pose-sol":    ("Pose au sol", "AR et produit", '<path d="M2.5 21 H21.5"/><path d="M6.5 12.2 V8.5 A2 2 0 0 1 8.5 6.5 H15.5 A2 2 0 0 1 17.5 8.5 V12.2"/><path d="M3.8 16.8 V12.8 A1.6 1.6 0 0 1 7 12.8 V13.8 H17 V12.8 A1.6 1.6 0 0 1 20.2 12.8 V16.8 Z M6 16.8 V18.8 M18 16.8 V18.8"/>'),
  "pose-mur":    ("Pose au mur", "AR et produit", '<path d="M4 3 V21"/><rect x="8.5" y="6" width="11" height="8.5" rx="1.2"/><path d="M8.5 12.5 L12 9.5 L15 12 L17 10.5 L19.5 12.5 M14 3.5 L11.5 6 M14 3.5 L16.5 6"/>'),
  "pose-table":  ("Pose sur table", "AR et produit", '<path d="M3 11.5 H21 M6 11.5 V20 M18 11.5 V20"/><path d="M10 11.5 V8.6 Q10 7.4 11 6.8 V4 H13 V6.8 Q14 7.4 14 8.6 V11.5"/>'),
  "surface":     ("Surface détectée", "AR et produit", '<path d="M2.5 16.5 L7.5 9 H21.5 L16.5 16.5 Z"/><path d="M12 9 L9.5 16.5 M5.1 12.75 H19"/>'),
  # ---------------------------------------------------------------- gestes
  "geste-deplacer": ("Déplacer", "Gestes", '<path d="M12 3 V21 M3 12 H21 M9.5 5.5 L12 3 L14.5 5.5 M9.5 18.5 L12 21 L14.5 18.5 M5.5 9.5 L3 12 L5.5 14.5 M18.5 9.5 L21 12 L18.5 14.5"/>'),
  "geste-tourner":  ("Tourner", "Gestes", '<path d="M19.6 12 A7.6 7.6 0 1 1 17.4 6.6"/><path d="M17.4 6.6 V2.8 M17.4 6.6 H13.6"/>'),
  "geste-pincer":   ("Pincer", "Gestes", '<path d="M14 10 L20 4 M20 4 H15.5 M20 4 V8.5 M10 14 L4 20 M4 20 H8.5 M4 20 V15.5"/>'),
  "geste-toucher":  ("Toucher", "Gestes", '<path d="M9.5 13.5 V6.2 A1.75 1.75 0 0 1 13 6.2 V11.2 L16.6 11.9 A2 2 0 0 1 18.2 14.2 L17.5 17.8 A3 3 0 0 1 14.6 20.2 H11.6 A3 3 0 0 1 9.2 19 L6.2 15 A1.6 1.6 0 0 1 8.6 12.9 Z"/><path d="M6.6 5.6 A5 5 0 0 1 15.9 5.6"/>'),
  "geste-secouer":  ("Secouer", "Gestes", '<rect x="8" y="3" width="8" height="18" rx="2.2"/><path d="M11 17.5 H13 M4.5 8 L3 12 L4.5 16 M19.5 8 L21 12 L19.5 16"/>'),
  "geste-double":   ("Double toucher", "Gestes", '<circle cx="12" cy="12" r="2.6"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="9.3" stroke-dasharray="2.6 2.6"/>'),
  # ---------------------------------------------------------------- retours d'information
  "valide":      ("Validé", "Retours", '<path d="M5 12.5 L10 17.5 L19 7"/>'),
  "info":        ("Information", "Retours", '<circle cx="12" cy="12" r="9"/><path d="M12 11 V16.5"/>' + DOT(12, 7.8)),
  "alerte":      ("Attention", "Retours", '<path d="M10.3 4.5 Q12 1.8 13.7 4.5 L20.8 17.2 Q22.2 20 19.1 20 H4.9 Q1.8 20 3.2 17.2 Z"/><path d="M12 9.5 V13.5"/>' + DOT(12, 16.6)),
  "erreur":      ("Erreur", "Retours", '<circle cx="12" cy="12" r="9"/><path d="M9.2 9.2 L14.8 14.8 M14.8 9.2 L9.2 14.8"/>'),
  "aide":        ("Aide", "Retours", '<circle cx="12" cy="12" r="9"/><path d="M9.4 9.6 A2.6 2.6 0 1 1 13.2 11.9 C12.4 12.3 12 12.9 12 13.7 V14"/>' + DOT(12, 17)),
  "chrono":      ("Chronomètre", "Retours", '<circle cx="12" cy="13.2" r="7.8"/><path d="M12 9 V13.2 L14.6 15.8 M9.8 2.6 H14.2 M18.6 6.6 L19.8 5.4"/>'),
  "lumiere":     ("Lumière", "Retours", '<circle cx="12" cy="12" r="4"/><path d="M12 2.5 V4.5 M12 19.5 V21.5 M2.5 12 H4.5 M19.5 12 H21.5 M5.3 5.3 L6.7 6.7 M17.3 17.3 L18.7 18.7 M5.3 18.7 L6.7 17.3 M17.3 6.7 L18.7 5.3"/>'),
  "hors-ligne":  ("Hors ligne", "Retours", '<path d="M2.5 9 A14 14 0 0 1 21.5 9 M5.6 12.6 A9.5 9.5 0 0 1 18.4 12.6 M8.8 16 A4.8 4.8 0 0 1 15.2 16 M4 3.5 L20 20.5"/>' + DOT(12, 19.6)),
  # ---------------------------------------------------------------- compte
  "visage":      ("Visage", "Compte", '<path d="M4 8 V6.2 A2.2 2.2 0 0 1 6.2 4 H8 M16 4 H17.8 A2.2 2.2 0 0 1 20 6.2 V8 M20 16 V17.8 A2.2 2.2 0 0 1 17.8 20 H16 M8 20 H6.2 A2.2 2.2 0 0 1 4 17.8 V16"/><path d="M9 9.2 V10.4 M15 9.2 V10.4 M12.2 9.4 V13 H11.2 M9.2 15.2 A4 4 0 0 0 14.8 15.2"/>'),
  "cadenas":     ("Mot de passe", "Compte", '<rect x="5" y="10.5" width="14" height="10" rx="2.6"/><path d="M8 10.5 V8 A4 4 0 0 1 16 8 V10.5 M12 14.5 V16.5"/>'),
  "email":       ("E-mail", "Compte", '<rect x="3" y="5.5" width="18" height="13" rx="2.6"/><path d="M4 7.2 L12 13 L20 7.2"/>'),
  "telephone":   ("Téléphone", "Compte", '<rect x="7" y="2.5" width="10" height="19" rx="2.6"/><path d="M11 18.3 H13"/>'),
  "modifier":    ("Modifier", "Compte", '<path d="M4 20 L4.6 16.1 L15.4 5.3 A2.1 2.1 0 0 1 18.7 8.6 L7.9 19.4 Z M13.6 7.1 L16.9 10.4"/>'),
  "supprimer":   ("Supprimer", "Compte", '<path d="M4 7 H20 M9.5 7 V4.6 H14.5 V7 M6 7 L7 19.4 A1.6 1.6 0 0 0 8.6 21 H15.4 A1.6 1.6 0 0 0 17 19.4 L18 7 M10 11 V17 M14 11 V17"/>'),
  "deconnexion": ("Déconnexion", "Compte", '<path d="M10 4 H6.2 A2.2 2.2 0 0 0 4 6.2 V17.8 A2.2 2.2 0 0 0 6.2 20 H10 M14.5 8 L18.5 12 L14.5 16 M18.5 12 H9"/>'),
  "admin":       ("Administration", "Compte", '<path d="M12 3 L19 5.8 V11.2 C19 15.6 16 19 12 20.8 C8 19 5 15.6 5 11.2 V5.8 Z"/><path d="M12 7.6 L15.2 9.4 V13 L12 14.8 L8.8 13 V9.4 Z M8.8 9.4 L12 11.2 L15.2 9.4 M12 11.2 V14.8"/>'),
  "musique":     ("Musique", "Réglages", '<path d="M9 18 V5.6 L19 3.6 V16"/><circle cx="6.5" cy="18" r="2.5"/><circle cx="16.5" cy="16" r="2.5"/>'),
  "vibration":   ("Vibrations", "Réglages", '<rect x="8" y="4" width="8" height="16" rx="2.2"/><path d="M5 8 V16 M2.5 10 V14 M19 8 V16 M21.5 10 V14"/>'),
}

def icon_svg(inner, color="currentColor", size=24):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 24 24" '
            f'fill="none" stroke="{color}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" color="{color}">'
            f'{inner}</svg>\n')

if __name__ == "__main__":
    out = f"{ROOT}/icons"
    sprite = ['<svg xmlns="http://www.w3.org/2000/svg" style="display:none">']
    for name, (label, group, inner) in ICONS.items():
        write(f"{out}/svg/{name}.svg", icon_svg(inner))
        tmp = f"/home/claude/build/tmp/ic-{name}.svg"
        write(tmp, icon_svg(inner, "#FFFFFF"))
        for px in (64, 128, 256):   # blanc pur : teinter dans Unity via Image.color
            png(tmp, f"{out}/png-unity/{px}/dc-icon-{name}.png", px)
        sprite.append(f'  <symbol id="dc-{name}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" '
                      f'stroke-linecap="round" stroke-linejoin="round"><title>{label}</title>{inner}</symbol>')
    sprite.append("</svg>")
    write(f"{out}/decoar-icons-sprite.svg", "\n".join(sprite) + "\n")

    # aperçu : sur verre sombre et sur papier
    import tokens as T
    names = list(ICONS)
    cols = 10; cell = 140; rows = -(-len(names) // cols)
    W = cols * cell; H = rows * cell
    ft = ImageFont.truetype(f"{ROOT}/fonts/Outfit_500Medium.ttf", 15)
    for theme, bg, fg, sub in [("sombre", T.PALETTE["lin"]["950"], T.PALETTE["lin"]["100"], T.PALETTE["lin"]["400"]),
                               ("clair", T.PALETTE["lin"]["50"], T.PALETTE["lin"]["900"], T.PALETTE["lin"]["600"])]:
        sheet = Image.new("RGB", (W, H), bg); d = ImageDraw.Draw(sheet)
        for i, n in enumerate(names):
            tmp = f"/home/claude/build/tmp/ic-{n}-{theme}.svg"
            write(tmp, icon_svg(ICONS[n][2], fg)); png(tmp, tmp + ".png", 56)
            im = Image.open(tmp + ".png").convert("RGBA")
            x, y = (i % cols) * cell, (i // cols) * cell
            sheet.paste(im, (x + 42, y + 26), im)
            lbl = ICONS[n][0]; tw = d.textlength(lbl, font=ft)
            d.text((x + (cell - tw) / 2, y + 96), lbl, fill=sub, font=ft)
        sheet.save(f"{out}/apercu-icones-{theme}.png")
    print(len(ICONS), "icônes")
