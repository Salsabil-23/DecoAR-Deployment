# 02 · Couleurs

Une seule palette pour toute l'application. Elle remplace les couleurs du logo, du HUD, de M6 et d'Auth, qui avaient chacune leurs variantes (deux terracotta, deux crèmes, deux sauges).

La palette a deux niveaux :

- **Primitives** : les teintes brutes, numérotées de 50 (très clair) à 900 (très foncé). On ne les utilise pas directement dans les écrans.
- **Sémantiques** : des rôles (« texte principal », « action principale »…) qui pointent vers une primitive, différente selon le thème. **Les écrans n'utilisent que les rôles.**

## 1. Primitives

### Terracotta

Couleur de marque. Logo, action principale du thème papier, accents.

| Pas | Hex | |
| --- | --- | --- |
| 50 | `#FFF4EC` | ![#FFF4EC](https://placehold.co/18x18/FFF4EC/FFF4EC.png) |
| 100 | `#FDE3D2` | ![#FDE3D2](https://placehold.co/18x18/FDE3D2/FDE3D2.png) |
| 200 | `#FBC5A3` | ![#FBC5A3](https://placehold.co/18x18/FBC5A3/FBC5A3.png) |
| 300 | `#F7A574` | ![#F7A574](https://placehold.co/18x18/F7A574/F7A574.png) |
| 400 | `#EE824B` | ![#EE824B](https://placehold.co/18x18/EE824B/EE824B.png) |
| 500 | `#DF5F2A` | ![#DF5F2A](https://placehold.co/18x18/DF5F2A/DF5F2A.png) ★ référence |
| 600 | `#C04A1E` | ![#C04A1E](https://placehold.co/18x18/C04A1E/C04A1E.png) |
| 700 | `#983A19` | ![#983A19](https://placehold.co/18x18/983A19/983A19.png) |
| 800 | `#6E2C16` | ![#6E2C16](https://placehold.co/18x18/6E2C16/6E2C16.png) |
| 900 | `#491E11` | ![#491E11](https://placehold.co/18x18/491E11/491E11.png) |

### Sauge

Le vivant, le « oui » : ajouter au panier, payer, interrupteur actif.

| Pas | Hex | |
| --- | --- | --- |
| 50 | `#F3F7F1` | ![#F3F7F1](https://placehold.co/18x18/F3F7F1/F3F7F1.png) |
| 100 | `#E2ECDE` | ![#E2ECDE](https://placehold.co/18x18/E2ECDE/E2ECDE.png) |
| 200 | `#C6D8BF` | ![#C6D8BF](https://placehold.co/18x18/C6D8BF/C6D8BF.png) |
| 300 | `#A8C29F` | ![#A8C29F](https://placehold.co/18x18/A8C29F/A8C29F.png) |
| 400 | `#8DAB83` | ![#8DAB83](https://placehold.co/18x18/8DAB83/8DAB83.png) |
| 500 | `#73946E` | ![#73946E](https://placehold.co/18x18/73946E/73946E.png) ★ référence |
| 600 | `#5D7B59` | ![#5D7B59](https://placehold.co/18x18/5D7B59/5D7B59.png) |
| 700 | `#486146` | ![#486146](https://placehold.co/18x18/486146/486146.png) |
| 800 | `#354734` | ![#354734](https://placehold.co/18x18/354734/354734.png) |
| 900 | `#232F22` | ![#232F22](https://placehold.co/18x18/232F22/232F22.png) |

### Laiton

La lumière, le précieux : action principale sur verre (AR), prix, sélection.

| Pas | Hex | |
| --- | --- | --- |
| 50 | `#FCF7EC` | ![#FCF7EC](https://placehold.co/18x18/FCF7EC/FCF7EC.png) |
| 100 | `#F7ECD3` | ![#F7ECD3](https://placehold.co/18x18/F7ECD3/F7ECD3.png) |
| 200 | `#F0DDB1` | ![#F0DDB1](https://placehold.co/18x18/F0DDB1/F0DDB1.png) |
| 300 | `#E7CB8C` | ![#E7CB8C](https://placehold.co/18x18/E7CB8C/E7CB8C.png) |
| 400 | `#DDB86C` | ![#DDB86C](https://placehold.co/18x18/DDB86C/DDB86C.png) |
| 500 | `#D1A652` | ![#D1A652](https://placehold.co/18x18/D1A652/D1A652.png) ★ référence |
| 600 | `#B08739` | ![#B08739](https://placehold.co/18x18/B08739/B08739.png) |
| 700 | `#8A682C` | ![#8A682C](https://placehold.co/18x18/8A682C/8A682C.png) |
| 800 | `#634A20` | ![#634A20](https://placehold.co/18x18/634A20/634A20.png) |
| 900 | `#3F2F15` | ![#3F2F15](https://placehold.co/18x18/3F2F15/3F2F15.png) |

### Lin

Neutres chauds : fonds, textes, verre. Jamais de gris froid ni de noir pur.

| Pas | Hex | |
| --- | --- | --- |
| 0 | `#FFFFFF` | ![#FFFFFF](https://placehold.co/18x18/FFFFFF/FFFFFF.png) |
| 50 | `#FDF9F3` | ![#FDF9F3](https://placehold.co/18x18/FDF9F3/FDF9F3.png) |
| 100 | `#FAF2E3` | ![#FAF2E3](https://placehold.co/18x18/FAF2E3/FAF2E3.png) ★ référence |
| 150 | `#F4EADA` | ![#F4EADA](https://placehold.co/18x18/F4EADA/F4EADA.png) |
| 200 | `#EDE1CD` | ![#EDE1CD](https://placehold.co/18x18/EDE1CD/EDE1CD.png) |
| 300 | `#DDCDB5` | ![#DDCDB5](https://placehold.co/18x18/DDCDB5/DDCDB5.png) |
| 400 | `#C4B19A` | ![#C4B19A](https://placehold.co/18x18/C4B19A/C4B19A.png) |
| 500 | `#A8937E` | ![#A8937E](https://placehold.co/18x18/A8937E/A8937E.png) |
| 600 | `#8C7866` | ![#8C7866](https://placehold.co/18x18/8C7866/8C7866.png) |
| 700 | `#6B5B4D` | ![#6B5B4D](https://placehold.co/18x18/6B5B4D/6B5B4D.png) |
| 800 | `#4A3E34` | ![#4A3E34](https://placehold.co/18x18/4A3E34/4A3E34.png) |
| 850 | `#352C24` | ![#352C24](https://placehold.co/18x18/352C24/352C24.png) |
| 900 | `#29211A` | ![#29211A](https://placehold.co/18x18/29211A/29211A.png) |
| 950 | `#1F1A14` | ![#1F1A14](https://placehold.co/18x18/1F1A14/1F1A14.png) ★ référence |
| 1000 | `#15110D` | ![#15110D](https://placehold.co/18x18/15110D/15110D.png) |

### Brique

Pastille compteur, erreurs.

| Pas | Hex | |
| --- | --- | --- |
| 300 | `#F2A08F` | ![#F2A08F](https://placehold.co/18x18/F2A08F/F2A08F.png) |
| 500 | `#D15C45` | ![#D15C45](https://placehold.co/18x18/D15C45/D15C45.png) |
| 600 | `#B94A35` | ![#B94A35](https://placehold.co/18x18/B94A35/B94A35.png) |
| 700 | `#9E3F2D` | ![#9E3F2D](https://placehold.co/18x18/9E3F2D/9E3F2D.png) |

### Ardoise

Information neutre (rare).

| Pas | Hex | |
| --- | --- | --- |
| 300 | `#9DB7CB` | ![#9DB7CB](https://placehold.co/18x18/9DB7CB/9DB7CB.png) |
| 500 | `#5F7E96` | ![#5F7E96](https://placehold.co/18x18/5F7E96/5F7E96.png) |
| 700 | `#3F576A` | ![#3F576A](https://placehold.co/18x18/3F576A/3F576A.png) |

### Succès

Confirmations, « Taille réelle ».

| Pas | Hex | |
| --- | --- | --- |
| 300 | `#9ECC94` | ![#9ECC94](https://placehold.co/18x18/9ECC94/9ECC94.png) |
| 500 | `#5E9A58` | ![#5E9A58](https://placehold.co/18x18/5E9A58/5E9A58.png) |
| 700 | `#3F6E3B` | ![#3F6E3B](https://placehold.co/18x18/3F6E3B/3F6E3B.png) |

### Types de pose (pastilles des cartes produit)

| Pose | Hex | |
| --- | --- | --- |
| mur | `#DB9E80` | ![#DB9E80](https://placehold.co/18x18/DB9E80/DB9E80.png) |
| sol | `#9EC794` | ![#9EC794](https://placehold.co/18x18/9EC794/9EC794.png) |
| table | `#E6C275` | ![#E6C275](https://placehold.co/18x18/E6C275/E6C275.png) |

Ces trois couleurs sont **toujours accompagnées de l'icône** `pose-sol`, `pose-mur` ou `pose-table` : la couleur seule ne suffit pas (daltonisme).

### Dégradés

| Nom | De → à | Usage |
| --- | --- | --- |
| halo-ar | `#EE824B` → `#DDB86C` | Moments « magiques » : génération 3D, première pose AR, bouton AR pressé. |
| aube | `#FFF4EC` → `#FDE3D2` | Fond des écrans papier (splash, connexion, personnage). |
| braise | `#EE824B` → `#C04A1E` | Grands aplats de marque (icône, bannière). |

## 2. Les deux thèmes

| Thème | Où | Idée |
| --- | --- | --- |
| **Verre** | Boutique M5, catalogue M3, AR M4, HUD, tiroirs, fiche produit | Verre fumé chaud au-dessus de la 3D ou de la caméra. Texte crème. |
| **Papier** | Connexion, inscription, profil, personnage M1, administration M6 | Lin clair, cartes blanches, texte encre. |

Règle simple : **si ça flotte sur une image vivante, c'est Verre ; si c'est un écran à soi, c'est Papier.**

### Rôles sémantiques

| Rôle | Verre | Papier |
| --- | --- | --- |
| `surface/glass` | `#1F1A14` à 80 % | — |
| `surface/glass-strong` | `#1F1A14` à 94 % | — |
| `surface/tile` | `#FFFFFF` à 7 % | — |
| `surface/tile-active` | `#FFFFFF` à 16 % | — |
| `surface/scrim` | `#000000` à 40 % | `#15110D` à 45 % |
| `border/hairline` | `#FFFFFF` à 12 % | `#EDE1CD` |
| `border/strong` | `#FFFFFF` à 22 % | `#DDCDB5` |
| `text/primary` | `#FAF2E3` | `#29211A` |
| `text/secondary` | `#DDCDB5` | `#6B5B4D` |
| `text/tertiary` | `#C4B19A` | `#8C7866` |
| `icon/default` | `#FAF2E3` | `#4A3E34` |
| `action/primary` | `#D1A652` | `#C04A1E` |
| `action/on-primary` | `#29211A` | `#FFFFFF` |
| `action/commerce` | `#5D7B59` | `#5D7B59` |
| `action/on-commerce` | `#FFFFFF` | `#FFFFFF` |
| `action/accent` | `#EE824B` | `#D1A652` |
| `action/on-accent` | `#15110D` | `#29211A` |
| `action/secondary` | `#FFFFFF` à 12 % | `#F4EADA` |
| `action/on-secondary` | `#FAF2E3` | `#29211A` |
| `state/selected` | `#D1A652` | `#C04A1E` |
| `state/focus-ring` | `#E7CB8C` | `#F7A574` |
| `feedback/success` | `#9ECC94` | `#3F6E3B` |
| `feedback/error` | `#F2A08F` | `#9E3F2D` |
| `feedback/warning` | `#DDB86C` | `#8A682C` |
| `feedback/info` | `#9DB7CB` | `#3F576A` |
| `badge/bg` | `#B94A35` | `#B94A35` |
| `badge/text` | `#FFFFFF` | `#FFFFFF` |
| `price` | `#E7CB8C` | `#983A19` |
| `surface/background` | — | `#FDF9F3` |
| `surface/card` | — | `#FFFFFF` |
| `surface/sunken` | — | `#F4EADA` |
| `action/link` | — | `#983A19` |

### Hiérarchie des actions

| Action | Verre | Papier | Exemples |
| --- | --- | --- | --- |
| Principale | Laiton + texte encre | Terracotta 600 + texte blanc | « Voir chez moi en AR », « Se connecter », « + Générer un modèle 3D » |
| Commerce | Sauge 600 + texte blanc | Sauge 600 + texte blanc | « Ajouter au panier », « Payer · 1 890 TND › » |
| Accent | Terracotta 400 | Laiton | Micro actif, étape en cours |
| Secondaire | Blanc 12 % + texte crème | Lin 150 + texte encre | « Reposer sur l'étagère », « Galerie » |
| Fantôme | Texte crème seul | Texte terracotta 700 | « Mot de passe oublié ? », « Vider le panier » |

Une seule action principale par écran. Sauge est réservé à l'argent et au panier : l'utilisateur apprend que « vert = j'achète ».

## 3. Contrastes vérifiés

Calculés selon WCAG 2.2. Pour le verre, on prend le **pire cas** : verre posé sur une image blanche (mur clair filmé en AR).

| Thème | Texte | Sur | Ratio | Niveau |
| --- | --- | --- | --- | --- |
| verre | `text/primary` `#FAF2E3` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 8.15 | AAA |
| verre | `text/secondary` `#DDCDB5` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 5.82 | AA |
| verre | `text/tertiary` `#C4B19A` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 4.36 | AA grand texte |
| verre | `price` `#E7CB8C` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 5.75 | AA |
| verre | `feedback/success` `#9ECC94` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 4.97 | AA |
| verre | `feedback/error` `#F2A08F` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 4.41 | AA grand texte |
| verre | `feedback/info` `#9DB7CB` | verre 80 % sur fond blanc (pire cas) `#4C4843` | 4.35 | AA grand texte |
| verre | `action/on-primary` `#29211A` | action/primary `#D1A652` | 7.00 | AAA |
| verre | `action/on-commerce` `#FFFFFF` | action/commerce `#5D7B59` | 4.73 | AA |
| verre | `action/on-accent` `#15110D` | action/accent `#EE824B` | 7.09 | AAA |
| verre | `badge/text` `#FFFFFF` | badge/bg `#B94A35` | 5.13 | AA |
| papier | `text/primary` `#29211A` | surface/background `#FDF9F3` | 15.09 | AAA |
| papier | `text/secondary` `#6B5B4D` | surface/background `#FDF9F3` | 6.20 | AA |
| papier | `text/tertiary` `#8C7866` | surface/card `#FFFFFF` | 4.21 | AA grand texte |
| papier | `action/link` `#983A19` | surface/background `#FDF9F3` | 6.78 | AA |
| papier | `price` `#983A19` | surface/card `#FFFFFF` | 7.11 | AAA |
| papier | `feedback/error` `#9E3F2D` | surface/card `#FFFFFF` | 6.56 | AA |
| papier | `feedback/success` `#3F6E3B` | surface/card `#FFFFFF` | 5.99 | AA |
| papier | `action/on-primary` `#FFFFFF` | action/primary `#C04A1E` | 4.95 | AA |
| papier | `action/on-commerce` `#FFFFFF` | action/commerce `#5D7B59` | 4.73 | AA |
| papier | `action/on-accent` `#29211A` | action/accent `#D1A652` | 7.00 | AAA |
| papier | `text/primary` `#29211A` | surface/sunken `#F4EADA` | 13.29 | AAA |

**Règles qui en découlent :**

- Sur verre, `text/tertiary` et les couleurs de retour (succès, erreur) ne servent qu'au texte ≥ 34 ref en semi-gras, ou à un point / une icône accompagné d'un texte crème.
- **Aucun texte directement sur la caméra ou la 3D.** Toujours une surface de verre dessous (opacité ≥ 80 %), ou le voile `dc-voile-haut` pour les bandeaux.
- Le prix sur verre est en laiton 300 (pas 500) pour garder 4,5:1.

## 4. Ancien → nouveau

| Avant | Après |
| --- | --- |
| Terracotta logo `#C5603F` / interface `#C7734A` | `terracotta.500` `#DF5F2A` (orange du symbole) |
| Crème `#FBF6EE` / `#FAF2E3` / `#FAF7F2` | `lin.50` `#FDF9F3` (fond papier), `lin.100` `#FAF2E3` (texte sur verre) |
| Sauge `#A9C2A0` / `#73946E` | `sauge.300` / `sauge.500` (boutons : `sauge.600`) |
| Laiton `#D1A652` | `laiton.500` (inchangé) |
| Verre `#1F1A14` à 72 % | `lin.950` à **80 %** (lisibilité) |
| Doux `#B8A38F` | `lin.300` `#DDCDB5` (texte secondaire sur verre) |
| Rouge brique `#D15C45` | `brique.600` `#B94A35` (pastille : blanc lisible) |
| Fond M6 `#2B241C` | M6 passe en thème Papier (`lin.50`) |
| Succès `#9ECC94` | `succes.300` (inchangé) |

## 5. Décor 3D

Les matériaux 3D du catalogue reprennent la palette (sRGB, à saisir tels quels dans les matériaux URP) :

| Élément | Primitive | Hex |
| --- | --- | --- |
| Mur | lin.200 | `#EDE1CD` |
| Étagère laquée | lin.150 | `#F4EADA` |
| Fond d'étagère | sauge.500 | `#73946E` |
| Panneau | terracotta.500 | `#DF5F2A` |
| Enseigne | lin.900 + texture `assets/3d/enseigne-etagere.png` | `#29211A` |
| Liseré, rubans lumineux | laiton.500 (émissif ×1,5) | `#D1A652` |
