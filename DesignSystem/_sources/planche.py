"""Planche visuelle autonome du design system (un seul fichier HTML, polices et images intégrées)."""
import base64, html
from common import *
import tokens as T
import logo as L
from icons import ICONS
from assets import EMPTY

P = T.PALETTE
def b64(path, mime):
    return f"data:{mime};base64," + base64.b64encode(open(path, "rb").read()).decode()
def img(path): return b64(path, "image/png")
def strip(svg): return svg[svg.index("<svg"):]

fonts = "".join(
    f"@font-face{{font-family:'Outfit';font-weight:{w};src:url({b64(f'{ROOT}/fonts/Outfit_{n}.ttf','font/ttf')}) format('truetype');font-display:swap}}"
    for w, n in [(400, "400Regular"), (500, "500Medium"), (600, "600SemiBold"), (700, "700Bold")])

def ic(name, cls="ic"):
    return f'<svg class="{cls}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">{ICONS[name][2]}</svg>'

css_vars = open(f"{ROOT}/tokens/decoar-tokens.css").read()

# ------------------------------------------------------------------ sections
def palette_html():
    out = []
    for fam, steps in P.items():
        cells = "".join(
            f'<div class="sw" style="background:{h};color:{"#29211A" if T.lum(h) > 0.33 else "#FAF2E3"}"><b>{s}</b><span>{h}</span></div>'
            for s, h in steps.items())
        name = {"succes": "succès"}.get(fam, fam)
        out.append(f'<div class="ramp"><h4>{name}</h4><div class="cells">{cells}</div></div>')
    pose = "".join(f'<div class="pose"><span style="background:{h}">{ic("pose-"+k,"ic s")}</span>{k} <code>{h}</code></div>' for k, h in T.POSE.items())
    grads = "".join(f'<div class="grad" style="background:linear-gradient(135deg,{a},{b});{"color:#6B5B4D;text-shadow:none" if k=="aube" else ""}"><b>{k}</b></div>' for k, (a, b) in T.GRADIENTS.items())
    return "".join(out), pose, grads

def type_html():
    rows = []
    sample = {"display": "Essaie. Pose. Adopte.", "titre-1": "Crée ton personnage", "titre-2": "Mon panier",
              "titre-3": "Fauteuil Lina", "corps-l": "Balaie lentement le sol", "corps": "Velours côtelé, pieds en chêne massif.",
              "label": "Salon", "bouton": "Ajouter au panier", "legende": "101 × 94 × 105 cm", "surtitre": "RAYON SALON",
              "prix-xl": "1 890 TND", "prix": "890 TND"}
    for k, (s, w, lh, ls, u) in T.TYPE.items():
        px = round(s * T.REF_TO_DP * 1.15)
        rows.append(f'<div class="trow"><div class="tmeta"><b>{k}</b><span>{s} ref · ≈{s*T.REF_TO_DP:.0f} dp · {w}</span></div>'
                    f'<div class="tsample" style="font-size:{px}px;font-weight:{w};line-height:{lh};letter-spacing:{ls}em">{sample[k]}</div></div>')
    return "".join(rows)

def contrast_html():
    rows = "".join(f'<tr><td>{t}</td><td><span class="dot" style="background:{fh}"></span>{fg}</td><td><span class="dot" style="background:{bh}"></span>{html.escape(bl)}</td><td><b>{c:.2f}</b></td><td class="{"ok" if c>=4.5 else "mid"}">{lv}</td></tr>'
                   for t, fg, fh, bl, bh, c, lv in T.contrast_report())
    return rows

def icons_html():
    return "".join(f'<div class="icell">{ic(n)}<span>{n}</span></div>' for n in ICONS)

def empty_html():
    return "".join(f'<figure><img src="{img(f"{ROOT}/assets/etats-vides/{n}.png")}" alt=""><figcaption><b>{t}</b><span>{s}</span></figcaption></figure>'
                   for n, (t, s, _) in EMPTY.items())

pal, pose, grads = palette_html()
logo_h = strip(open(f"{ROOT}/logo/horizontal/decoar-logo-signature.svg").read())
logo_dark = strip(open(f"{ROOT}/logo/horizontal/decoar-logo-horizontal-sur-sombre.svg").read())
sizes = "".join(f'<figure class="piste"><img src="{img(f"{ROOT}/app-icon/ios/tailles-unity/Icon-{px}.png")}" width="{px}" height="{px}" style="width:{px}px;height:{px}px;border-radius:22%" alt=""><figcaption>{px} px</figcaption></figure>' for px in (180, 120, 60, 40, 29))

page = f'''<!doctype html><html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>DecoAR Design System</title>
<style>{fonts}
{css_vars}
:root{{--bg:{P["lin"]["50"]};--card:#fff;--ink:{P["lin"]["900"]};--sub:{P["lin"]["700"]};--line:{P["lin"]["200"]};--ter:{P["terracotta"]["600"]};}}
*{{box-sizing:border-box;margin:0}}
body{{font-family:Outfit,system-ui,sans-serif;background:var(--bg);color:var(--ink);-webkit-font-smoothing:antialiased}}
.wrap{{max-width:1180px;margin:0 auto;padding:0 32px}}
header.hero{{background:radial-gradient(120% 90% at 50% 110%,{P["laiton"]["100"]},transparent 60%),linear-gradient({P["lin"]["50"]},{P["terracotta"]["100"]});padding:72px 0 64px;border-bottom:1px solid var(--line)}}
.hero .wrap{{display:grid;grid-template-columns:1.2fr 1fr;gap:48px;align-items:center}}
.hero svg{{width:100%;height:auto;max-width:520px}}
.hero h1{{font-size:15px;letter-spacing:.14em;text-transform:uppercase;color:var(--ter);font-weight:600;margin-bottom:18px}}
.hero p{{font-size:21px;line-height:1.5;color:var(--sub);margin-top:22px;max-width:520px}}
.hero .appicons{{display:flex;gap:18px;align-items:flex-end;justify-content:center}}
.hero .appicons img{{border-radius:22.5%;box-shadow:0 18px 40px -12px rgba(68,34,26,.35)}}
nav.toc{{position:sticky;top:0;z-index:5;background:rgba(253,249,243,.92);backdrop-filter:blur(8px);border-bottom:1px solid var(--line)}}
nav.toc .wrap{{display:flex;gap:22px;overflow-x:auto;padding-top:14px;padding-bottom:14px;font-weight:500;font-size:15px}}
nav.toc a{{color:var(--sub);text-decoration:none;white-space:nowrap}} nav.toc a:hover{{color:var(--ter)}}
section{{padding:72px 0;border-bottom:1px solid var(--line)}}
section h2{{font-size:40px;font-weight:600;letter-spacing:-.015em;margin-bottom:10px}}
section h2 small{{display:block;font-size:13px;letter-spacing:.14em;color:var(--ter);text-transform:uppercase;margin-bottom:10px}}
section .lead{{font-size:18px;color:var(--sub);max-width:720px;line-height:1.55;margin-bottom:36px}}
h3{{font-size:22px;font-weight:600;margin:40px 0 16px}}
code{{font-family:ui-monospace,SFMono-Regular,Menlo,monospace;font-size:.85em}}
.grid3{{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}}
.card{{background:#fff;border:1px solid var(--line);border-radius:22px;padding:24px}}
.ramp{{margin-bottom:18px}} .ramp h4{{font-size:14px;font-weight:600;text-transform:capitalize;margin-bottom:8px;color:var(--sub)}}
.cells{{display:flex;border-radius:16px;overflow:hidden}}
.sw{{flex:1;min-width:0;height:84px;padding:10px;display:flex;flex-direction:column;justify-content:flex-end;font-size:11px;gap:2px}}
.sw b{{font-size:13px}} .sw span{{opacity:.85;font-family:ui-monospace,monospace}}
.poses{{display:flex;gap:24px;flex-wrap:wrap}} .pose{{display:flex;align-items:center;gap:10px;font-weight:500}}
.pose span{{width:40px;height:40px;border-radius:50%;display:grid;place-items:center;color:{P["lin"]["900"]}}}
.grads{{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}} .grad{{height:120px;border-radius:22px;padding:16px;display:flex;align-items:flex-end;color:#fff;font-weight:600;text-shadow:0 1px 6px rgba(0,0,0,.25)}}
.ic{{width:26px;height:26px;flex:none}} .ic.s{{width:22px;height:22px}}
/* thèmes */
.themes{{display:grid;grid-template-columns:1.25fr 1fr;gap:24px}}
.scene{{position:relative;border-radius:28px;overflow:hidden;min-height:540px;padding:22px;color:{P["lin"]["100"]};
  background:radial-gradient(60% 50% at 70% 35%,rgba(255,236,200,.85),transparent 70%),linear-gradient(180deg,#d9cbb8 0%,#cdb79c 52%,#9c7d5f 52.2%,#7d6047 100%)}}
.scene .room{{position:absolute;left:33%;top:30%;width:34%;height:42%}}
.glass{{background:rgba(31,26,20,.80);border:1.5px solid rgba(255,255,255,.12);box-shadow:0 6px 16px rgba(0,0,0,.38);backdrop-filter:blur(10px)}}
.glass.strong{{background:rgba(31,26,20,.94)}}
.hud{{display:flex;justify-content:space-between;align-items:center;position:relative;z-index:2}}
.grp{{display:flex;gap:10px;align-items:center}}
.fab{{width:52px;height:52px;border-radius:50%;display:grid;place-items:center;position:relative}}
.badge{{position:absolute;top:-4px;right:-4px;min-width:20px;height:20px;border-radius:10px;background:{P["brique"]["600"]};color:#fff;font-size:12px;font-weight:700;display:grid;place-items:center;padding:0 5px}}
.prof{{display:flex;align-items:center;gap:10px;padding:5px 16px 5px 5px;border-radius:30px;font-weight:500;font-size:15px}}
.prof i{{width:42px;height:42px;border-radius:50%;background:{P["sauge"]["300"]};border:2px solid {P["laiton"]["500"]}}}
.toast{{position:absolute;left:22px;top:88px;padding:12px 22px;border-radius:28px;font-size:15px;display:flex;gap:10px;align-items:center;z-index:2;white-space:nowrap}}
.toast::before{{content:"";width:10px;height:10px;border-radius:50%;background:{P["succes"]["300"]}}}
.fiche{{position:absolute;right:22px;top:88px;bottom:22px;width:300px;border-radius:24px;padding:22px;display:flex;flex-direction:column;gap:12px;z-index:2}}
.fiche .sur{{font-size:11px;letter-spacing:.1em;color:{P["lin"]["300"]};font-weight:600}}
.fiche h5{{font-size:22px;font-weight:600}} .fiche .px{{font-size:30px;font-weight:700;color:{P["laiton"]["300"]}}}
.fiche .dim{{font-size:13px;color:{P["lin"]["300"]}}}
.chips{{display:flex;gap:10px}} .chips i{{width:34px;height:34px;border-radius:50%;display:block;outline-offset:3px}}
.chips i.on{{outline:2.5px solid {P["laiton"]["500"]}}}
.btn{{height:48px;border-radius:24px;border:0;font:600 15px Outfit;display:flex;align-items:center;justify-content:center;gap:8px;padding:0 20px;cursor:pointer}}
.btn .ic{{width:22px;height:22px}}
.b-pri{{background:{P["laiton"]["500"]};color:{P["lin"]["900"]}}} .b-com{{background:{P["sauge"]["600"]};color:#fff}}
.b-sec{{background:rgba(255,255,255,.12);color:{P["lin"]["100"]}}} .b-gh{{background:none;color:{P["lin"]["100"]}}}
.p-pri{{background:{P["terracotta"]["600"]};color:#fff}} .p-sec{{background:{P["lin"]["150"]};color:{P["lin"]["900"]}}} .p-gh{{background:none;color:{P["terracotta"]["700"]}}}
.cards{{position:absolute;left:22px;bottom:22px;display:flex;gap:10px;z-index:2}}
.pcard{{width:150px;height:78px;border-radius:14px;padding:10px 12px;display:flex;flex-direction:column;justify-content:space-between;font-size:13px}}
.pcard.on{{border-color:{P["laiton"]["500"]};border-width:2px}}
.pcard .r{{display:flex;justify-content:space-between;align-items:center}} .pcard b{{color:{P["laiton"]["300"]};font-size:14px}}
.pcard .pz{{width:20px;height:20px;border-radius:50%;display:inline-block;vertical-align:-5px;margin-right:6px}}
.paper{{background:linear-gradient({P["lin"]["50"]},{P["terracotta"]["50"]});border:1px solid var(--line);border-radius:28px;padding:32px;display:flex;flex-direction:column;gap:14px}}
.paper h4{{font-size:26px;font-weight:600}} .paper .s{{color:var(--sub);font-size:15px;margin-top:-6px}}
.fld label{{display:block;font-size:13px;font-weight:500;color:var(--sub);margin-bottom:6px}}
.fld > div:not(.meter){{height:46px;border-radius:14px;background:{P["lin"]["150"]};display:flex;align-items:center;gap:10px;padding:0 14px;color:{P["lin"]["600"]};font-size:15px}}
.fld.focus > div{{box-shadow:0 0 0 2px {P["terracotta"]["600"]},0 0 0 6px {P["terracotta"]["100"]};color:var(--ink)}}
.fld.err > div:not(.meter){{box-shadow:0 0 0 2px {P["brique"]["700"]}}} .fld .m{{font-size:13px;color:{P["brique"]["700"]};margin-top:6px;display:flex;gap:6px;align-items:center}}
.meter{{display:flex;gap:4px;margin-top:8px}} .meter i{{flex:1;height:4px;border-radius:2px;background:{P["lin"]["200"]}}}
.row{{display:flex;gap:10px;align-items:center}}
.tog{{width:52px;height:30px;border-radius:15px;background:{P["sauge"]["600"]};position:relative;flex:none}} .tog::after{{content:"";position:absolute;right:3px;top:3px;width:24px;height:24px;border-radius:50%;background:{P["lin"]["50"]}}}
.tog.off{{background:{P["lin"]["300"]}}} .tog.off::after{{right:auto;left:3px}}
.code{{display:flex;gap:8px}} .code i{{width:42px;height:50px;border-radius:12px;background:{P["lin"]["150"]};display:grid;place-items:center;font-style:normal;font-size:22px;font-weight:600}}
.code i.on{{box-shadow:0 0 0 2px {P["terracotta"]["600"]}}}
.steps{{display:flex;gap:18px;font-size:14px;font-weight:500}} .steps span{{display:flex;gap:8px;align-items:center}}
.steps b{{width:28px;height:28px;border-radius:50%;display:grid;place-items:center;font-size:13px}}
/* typo */
.trow{{display:grid;grid-template-columns:220px 1fr;gap:24px;align-items:baseline;padding:16px 0;border-top:1px solid var(--line)}}
.tmeta b{{display:block;font-weight:600}} .tmeta span{{font-size:13px;color:var(--sub)}}
table{{width:100%;border-collapse:collapse;font-size:14px}} td,th{{text-align:left;padding:10px 8px;border-top:1px solid var(--line)}}
.dot{{display:inline-block;width:12px;height:12px;border-radius:50%;margin-right:6px;vertical-align:-1px;border:1px solid rgba(0,0,0,.1)}}
td.ok{{color:{P["succes"]["700"]};font-weight:600}} td.mid{{color:{P["laiton"]["700"]};font-weight:600}}
.icons{{display:grid;grid-template-columns:repeat(auto-fill,minmax(112px,1fr));gap:8px}}
.icell{{background:#fff;border:1px solid var(--line);border-radius:16px;height:104px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;font-size:11.5px;color:var(--sub)}}
.icell .ic{{width:30px;height:30px;color:var(--ink)}}
.icons.dark .icell{{background:{P["lin"]["950"]};border-color:{P["lin"]["850"]};color:{P["lin"]["400"]}}} .icons.dark .icell .ic{{color:{P["lin"]["100"]}}}
.logos{{display:grid;grid-template-columns:1.4fr 1fr;gap:20px}}
.logos .card{{display:grid;place-items:center;min-height:220px}} .logos svg{{max-width:100%;height:auto}}
.dk{{background:{P["lin"]["900"]}!important;border-color:{P["lin"]["900"]}!important}}
.monos{{display:flex;gap:14px}} .monob{{border-radius:18px;padding:16px;border:1px solid var(--line)}} .monob svg{{width:86px;height:86px}}
.piste img{{width:100%;border-radius:18px;background:#fff;border:1px solid var(--line)}} .piste figcaption,.empties figcaption{{font-size:14px;margin-top:8px;color:var(--sub)}}
.empties{{display:grid;grid-template-columns:repeat(4,1fr);gap:16px}} .empties figure{{background:#fff;border:1px solid var(--line);border-radius:22px;padding:14px}}
.empties img{{width:100%}} .empties figcaption b{{display:block;color:var(--ink);font-weight:600}}
.aps{{display:flex;gap:24px;flex-wrap:wrap;align-items:flex-end}} .aps img{{border-radius:22%}}
.motion{{display:grid;grid-template-columns:repeat(4,1fr);gap:16px}}
.mv{{height:120px;border-radius:22px;background:#fff;border:1px solid var(--line);display:grid;place-items:center;font-size:13px;color:var(--sub);gap:6px;align-content:center}}
.breathe{{animation:br 2.4s ease-in-out infinite}} @keyframes br{{50%{{transform:scale(1.025)}}}}
.spin{{animation:sp 8s linear infinite}} @keyframes sp{{to{{transform:rotate(360deg)}}}}
.pulse{{width:14px;height:14px;border-radius:50%;background:{P["laiton"]["500"]};animation:pu 1.2s ease-out infinite}} @keyframes pu{{0%{{box-shadow:0 0 0 0 rgba(209,166,82,.6)}}100%{{box-shadow:0 0 0 18px rgba(209,166,82,0)}}}}
.pop{{animation:pp 1.6s infinite}} @keyframes pp{{0%,60%,100%{{transform:scale(1)}}10%{{transform:scale(1.28)}}25%{{transform:scale(.94)}}40%{{transform:scale(1.03)}}}}
@media (prefers-reduced-motion:reduce){{.breathe,.spin,.pulse,.pop{{animation:none}}}}
footer{{padding:48px 0 72px;color:var(--sub);font-size:14px}}
@media (max-width:900px){{.hero .wrap,.themes,.logos,.grid3,.grads{{grid-template-columns:1fr}} .empties,.motion{{grid-template-columns:1fr 1fr}} .trow{{grid-template-columns:1fr}} .fiche{{position:relative;inset:auto;width:auto;margin-top:16px}} .cards{{display:none}} .scene .room{{display:none}}}}
</style></head><body>
<header class="hero"><div class="wrap">
  <div><h1>Design system · v1.0</h1>{logo_h}
  <p>Ton intérieur, avant de l'acheter. Palette, typographie, icônes, composants et assets pour unifier DecoAR sur iPhone et Android.</p></div>
  <div class="appicons">
    <img src="{img(f'{ROOT}/app-icon/ios/AppIcon.appiconset/AppIcon-1024.png')}" width="190" height="190" alt="Icône DecoAR">
  </div>
</div></header>
<nav class="toc"><div class="wrap"><a href="#logo">Logo</a><a href="#couleurs">Couleurs</a><a href="#themes">Thèmes &amp; composants</a><a href="#typo">Typographie</a><a href="#icones">Icônes</a><a href="#assets">Assets</a><a href="#mouvement">Mouvement</a><a href="#contrastes">Contrastes</a></div></nav>

<section id="logo"><div class="wrap">
  <h2><small>01 · Identité</small>La Maison AR</h2>
  <p class="lead">Une maison, un fauteuil sur un socle lumineux, les coins de visée de l'AR et un anneau en orbite : le meuble arrive chez toi, à taille réelle. Le symbole est utilisé tel quel, sans retouche.</p>
  <div class="logos"><div class="card">{logo_h}</div><div class="card dk">{logo_dark}</div></div>
  <h3>Lisible en petit</h3><div class="aps">{sizes}</div>
</div></section>

<section id="couleurs"><div class="wrap">
  <h2><small>02 · Couleurs</small>Terracotta, sauge, laiton, lin</h2>
  <p class="lead">Une seule palette pour tous les modules. Laiton = « essaie-le », sauge = « je l'achète », terracotta = la marque. Pas de gris froid, pas de noir pur.</p>
  {pal}
  <h3>Types de pose</h3><div class="poses">{pose}</div>
  <h3>Dégradés</h3><div class="grads">{grads}</div>
</div></section>

<section id="themes"><div class="wrap">
  <h2><small>03 · Thèmes</small>Verre et papier</h2>
  <p class="lead">Ce qui flotte sur la 3D ou la caméra est en <b>verre fumé chaud</b>. Les écrans à soi (compte, personnage, administration) sont en <b>papier</b>.</p>
  <div class="themes">
    <div class="scene">
      <svg class="room" viewBox="0 0 200 200"><ellipse cx="100" cy="176" rx="80" ry="14" fill="rgba(60,40,25,.35)"/>
        <rect x="30" y="70" width="140" height="60" rx="22" fill="{P["sauge"]["500"]}"/><rect x="18" y="104" width="164" height="54" rx="20" fill="{P["terracotta"]["500"]}"/>
        <rect x="34" y="150" width="10" height="22" rx="3" fill="{P["lin"]["900"]}"/><rect x="156" y="150" width="10" height="22" rx="3" fill="{P["lin"]["900"]}"/>
        <ellipse cx="100" cy="180" rx="92" ry="17" fill="none" stroke="{P["laiton"]["400"]}" stroke-width="3"/></svg>
      <div class="hud"><div class="grp"><div class="prof glass"><i></i>Sarra</div><div class="fab glass">{ic("reglages")}</div><div class="fab glass">{ic("vue-1re")}</div></div>
        <div class="grp"><div class="fab glass">{ic("suggestions")}</div><div class="fab glass">{ic("panier")}<span class="badge pop">3</span></div></div></div>
      <div class="toast glass">Fauteuil Lina ajouté au panier</div>
      <div class="fiche glass strong"><span class="sur">SALON · POSE AU SOL</span><h5>Fauteuil Lina</h5><span class="px">1 890 TND</span>
        <span class="dim">101 × 94 × 105 cm</span>
        <div class="chips"><i class="on" style="background:{P["terracotta"]["500"]}"></i><i style="background:#cdbfa8"></i><i style="background:#3f2b22"></i><i style="background:#4a4a48"></i></div>
        <span class="dim">Velours côtelé, pieds en chêne massif.</span><div style="flex:1"></div>
        <button class="btn b-pri">{ic("ar")}Voir chez moi</button><button class="btn b-com breathe">{ic("panier")}Ajouter au panier</button><button class="btn b-gh">Reposer sur l'étagère</button></div>
      <div class="cards"><div class="pcard glass on"><span><i class="pz" style="background:{T.POSE["sol"]}"></i>Fauteuil Lina</span><div class="r"><b>1 890 TND</b></div></div>
</div>
    </div>
    <div class="paper">
      <img src="{img(f'{ROOT}/logo/symbole/decoar-symbole-sans-ombre-128.png')}" width="64" height="64" alt="">
      <h4>Content de te revoir</h4><p class="s">Connecte-toi pour retrouver ta boutique.</p>
      <div class="fld focus"><label>E-mail</label><div>{ic("email","ic s")}sarra@exemple.tn</div></div>
      <div class="fld err"><label>Mot de passe</label><div>{ic("cadenas","ic s")}••••••</div><p class="m">{ic("alerte","ic s")}8 caractères minimum, avec des lettres et des chiffres</p>
        <div class="meter"><i style="background:{P["brique"]["500"]}"></i><i></i><i></i><i></i></div></div>
      <button class="btn p-pri">Se connecter</button><button class="btn p-sec">{ic("visage")}Connexion par le visage</button><button class="btn p-gh">Mot de passe oublié ?</button>
      <div class="row" style="justify-content:space-between"><span>Vibrations</span><span class="tog"></span></div>
      <div class="code"><i>4</i><i>8</i><i class="on">2</i><i></i><i></i><i></i></div>
      <div class="steps"><span><b style="background:{P["sauge"]["600"]};color:#fff">✓</b>Photo</span><span><b style="background:{P["terracotta"]["600"]};color:#fff">2</b>Génération</span><span><b style="box-shadow:inset 0 0 0 2px {P["lin"]["300"]};color:{P["lin"]["600"]}">3</b>Catalogue</span></div>
    </div>
  </div>
</div></section>

<section id="typo"><div class="wrap">
  <h2><small>04 · Typographie</small>Outfit, partout</h2>
  <p class="lead">La police du logo devient celle de l'application. Les tailles sont relevées : l'ancien texte courant faisait ≈ 9,5 dp à l'écran, le nouveau ≈ 14,5 dp (iOS 17 pt, Android 14 sp).</p>
  {type_html()}
</div></section>

<section id="icones"><div class="wrap">
  <h2><small>05 · Icônes</small>{len(ICONS)} icônes, une seule famille</h2>
  <p class="lead">Grille 24, trait 2, angles arrondis. Monochromes et teintables. L'anneau pointillé du logo revient dans « Voir en AR », « Vue 3e personne » et « Double toucher ».</p>
  <div class="icons">{icons_html()}</div>
  <h3>Sur verre</h3><div class="icons dark">{"".join(f'<div class="icell">{ic(n)}<span>{n}</span></div>' for n in ["retour","panier","catalogue","suggestions","reglages","vue-1re","vue-3e","ar","coloris","taille-reelle","photo","geste-pincer"])}</div>
</div></section>

<section id="assets"><div class="wrap">
  <h2><small>06 · Assets</small>Illustrations, AR, icône d'application</h2>
  <p class="lead">Les illustrations n'ont pas de contour : elles restent lisibles sur le verre comme sur le papier.</p>
  <div class="empties">{empty_html()}</div>
  <h3>Réticule AR</h3><div class="card dk" style="display:flex;gap:28px;justify-content:center;padding:28px">
    {"".join(f'<img src="{img(f"{ROOT}/assets/ar/reticule-{s}.png")}" width="150" alt="">' for s in ["recherche","valide","refuse"])}</div>
  <h3>Icônes d'application</h3><div class="aps">
    <img src="{img(f'{ROOT}/app-icon/apercu-icones.png')}" style="border-radius:22px;width:100%" alt=""></div>
  <h3>Enseigne de l'étagère</h3><img src="{img(f'{ROOT}/assets/3d/enseigne-etagere.png')}" style="width:100%;max-width:640px;border-radius:12px" alt="">
</div></section>

<section id="mouvement"><div class="wrap">
  <h2><small>07 · Mouvement</small>Doux, amorti, rare</h2>
  <p class="lead">Une seule chose bouge à la fois. Respiration ±2,5 % sur 2,4 s, rebond amorti, point qui pulse. Tout s'arrête si « réduire les animations » est activé.</p>
  <div class="motion">
    <div class="mv"><button class="btn b-com breathe">Payer · 890 TND</button>Respiration</div>
    <div class="mv"><svg class="spin" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="{P["laiton"]["500"]}" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="9" stroke-dasharray="5.5 2.6"/></svg>Anneau de chargement</div>
    <div class="mv"><span class="pulse"></span>Consigne AR</div>
    <div class="mv"><span class="badge pop" style="position:static;width:30px;height:30px;border-radius:15px;font-size:15px">4</span>Pastille</div>
  </div>
</div></section>

<section id="contrastes"><div class="wrap">
  <h2><small>08 · Accessibilité</small>Contrastes vérifiés</h2>
  <p class="lead">Pour le verre, pire cas : surface à 80 % posée sur un mur blanc filmé en AR.</p>
  <table><tr><th>Thème</th><th>Texte</th><th>Fond</th><th>Ratio</th><th>Niveau</th></tr>{contrast_html()}</table>
</div></section>
<footer><div class="wrap">DecoAR Design System v1.0 · Police Outfit (SIL OFL) · Détails dans le dossier <code>docs/</code>.</div></footer>
</body></html>'''
write(f"{ROOT}/design-system.html", page)
print(round(len(page) / 1024), "Ko")
