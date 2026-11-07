# DecoAR : l'application et ses fonctionnalités

Ce document décrit ce que fait l'application DecoAR, écran par écran, et le style visuel actuel. Il sert de base pour créer le design system complet de l'application.

## Sommaire

1. [L'application en bref](#1-lapplication-en-bref)
2. [Contraintes de la plateforme](#2-contraintes-de-la-plateforme)
3. [Parcours de l'utilisateur](#3-parcours-de-lutilisateur)
4. [Écrans et fonctionnalités](#4-écrans-et-fonctionnalités)
5. [Éléments communs à tous les écrans](#5-éléments-communs-à-tous-les-écrans)
6. [Langage visuel actuel](#6-langage-visuel-actuel)
7. [Inventaire des composants](#7-inventaire-des-composants)
8. [Ton et rédaction](#8-ton-et-rédaction)
9. [États à prévoir](#9-états-à-prévoir)
10. [Ce que le design system doit unifier](#10-ce-que-le-design-system-doit-unifier)

---

## 1. L'application en bref

**DecoAR** est une application mobile de décoration intérieure. Elle mélange une boutique 3D, que l'on visite avec un avatar à son image, et l'essai des produits chez soi en réalité augmentée (AR).

**Promesse :** voir un meuble ou un objet déco à sa vraie taille dans sa propre pièce avant de l'acheter, dans une expérience de shopping qui ressemble à un jeu.

**Public :** des particuliers qui aménagent ou décorent leur logement, sur téléphone Android. L'application est en français, avec le tutoiement dans les écrans de compte et le vouvoiement dans la boutique.

**Univers :** un intérieur chaleureux et naturel, avec du bois clair, du lin crème, de la terre cuite (terracotta), du vert sauge, des touches de laiton et des plantes. Les modèles 3D sont en « low poly » (formes simples et facettes douces, couleurs franches).

**Modules :**

| Module | Rôle |
| --- | --- |
| Auth | Inscription, connexion, reconnaissance faciale, profil |
| M1 | Création du personnage (avatar, couleurs, scan du visage) |
| M5 | Boutique 3D en vue 1ère ou 3ème personne, panier, caisse et paiement |
| M3 | Catalogue : rayons, étagère 3D, fiche produit, recherche |
| M4 | Essai du produit chez soi en réalité augmentée |
| M6 | Espace administration : générer un modèle 3D à partir d'une photo, gérer le catalogue 3D |

---

## 2. Contraintes de la plateforme

Ces contraintes doivent guider le design system.

- **Moteur :** Unity 6, avec une interface construite avec uGUI et TextMeshPro. Les écrans récents (boutique, catalogue, AR, administration) sont construits par code à partir de briques communes.
- **Appareil :** smartphone Android.
- **Orientation :**
  - la boutique et le catalogue sont en **paysage** ;
  - les écrans de compte et de personnage s'adaptent au paysage et au portrait ;
  - l'AR et l'administration doivent fonctionner dans les deux.
- **Grille de référence :** 1920 × 1080 points en paysage, mise à l'échelle avec un équilibre 50 % largeur / 50 % hauteur. Toutes les mesures de ce document sont dans ce repère.
- **Zone sûre :** rien d'important sous l'encoche ni dans les coins arrondis du téléphone.
- **Tactile d'abord :**
  - cibles de 64 points minimum (84 pour les boutons principaux du HUD) ;
  - gestes : glisser, pincer, tourner à deux doigts, double toucher, secouer le téléphone.
- **Interface par-dessus la 3D ou la caméra :** presque tout le HUD flotte au-dessus d'une scène 3D animée ou de l'image de la caméra. Les surfaces doivent donc rester lisibles sur des fonds clairs comme foncés, d'où le « verre fumé » sombre.
- **Icônes vectorielles :** elles sont dessinées par code, sans fichiers image, monochromes et teintables (voir 6.5).
- **Police :** LiberationSans (police par défaut de TextMeshPro) dans l'application. Le logo utilise **Outfit**.
- **Retours physiques :** vibrations légères, moyennes ou fortes à chaque action importante.
- **Capteurs utilisés :** caméra frontale (visage), caméra arrière (AR, photo produit), gyroscope, accéléromètre (secousse), GPS (adresse), micro (recherche vocale).

---

## 3. Parcours de l'utilisateur

```
Connexion / Inscription (Auth)
   │   (lien « Espace administration » sur l'écran de connexion)
   ▼
Crée ton personnage (M1) ──► Scan du visage (M1)
   │
   ▼
Boutique 3D (M5) ──► Télé « Voir le catalogue » ──► Rayon (M3) ──► Essai en AR (M4)
   │                                                   │                │
   │◄────────────────── Panier ◄──────────────────────┴────────────────┘
   ▼
Caisse et paiement (M5)

Profil (bouton en haut à gauche) ──► Mon profil ──► Espace administration (M6) ──► Génération 3D (M6)
```

Le panier, le produit choisi, sa couleur, la position du joueur dans la boutique et les réglages (gyroscope, vibrations, vue) sont conservés quand on passe d'un écran à l'autre.

---

## 4. Écrans et fonctionnalités

### 4.1 Connexion et inscription (Auth)

**Connexion**
- E-mail et mot de passe. Le dernier e-mail utilisé est mémorisé.
- **Connexion par le visage** : un défi en direct où l'utilisateur suit des consignes (« Tourne la tête à gauche », « Lève la tête », « Regarde la caméra, de face »), puis « Reconnaissance en cours… ».
- « Mot de passe oublié » : code à 6 chiffres envoyé par e-mail.
- Lien « Pas encore de compte ? S'inscrire ».
- Lien **« Espace administration (catalogue 3D) › »** en terracotta, qui mène au module M6.

**Inscription**
- Prénom, nom, e-mail, téléphone, mot de passe et confirmation, avec une jauge de force (« Trop faible » … « Excellent »).
- **Enregistrement du visage** obligatoire : aperçu de la caméra frontale dans un cercle, défi de mouvements de tête, puis « Visage enregistré ».
- Vérification de l'e-mail par code.

**Verrouillage :** après une période d'inactivité, l'écran « Pour ta sécurité, reconnecte-toi » propose de se déverrouiller par le visage ou par le mot de passe.

**Messages types :**
- « Adresse e-mail invalide »
- « Mot de passe : 8 caractères minimum avec lettres et chiffres »
- « Session expirée, reconnecte-toi »
- « Place ton visage dans le cercle, bien éclairé »

### 4.2 Profil (Auth)

- Photo de profil (prendre une photo, galerie), prénom, nom, e-mail, téléphone.
- Changer le mot de passe.
- Réenregistrer son visage.
- Entrée **« Espace administration (3D) »**.
- Déconnexion.
- Retour à l'écran précédent : le joueur retrouve sa place dans la boutique.

### 4.3 Création du personnage (M1)

- Aperçu 3D du personnage, que l'on fait tourner au doigt.
- **Onglets :** Avatar, Couleur de peau, Cheveux, T-shirt (ou Tenue), Pantalon, Chaussures.
  - **Avatar :** 3 avatars réalistes au choix (« Avatar 1 / 3 »).
  - **Couleurs :** une rangée de pastilles rondes. La première garde la couleur d'origine, et un anneau entoure la couleur choisie.
- **Scanner / Refaire** le visage : ouvre l'écran de scan.
- **« Entrer dans la boutique »** : bouton principal.

### 4.4 Scan du visage (M1)

- Caméra frontale avec un **ovale** guide.
- **Consignes progressives :**
  - « Place ton visage dans l'ovale »
  - « Tourne lentement la tête d'un côté… »
  - « Maintenant, tourne-la lentement de l'autre côté… »
- Le visage 3D est calculé sur le téléphone : « Création de ton visage 3D… », puis « Visage 3D enregistré ! ».
- La couleur de peau et celle des cheveux sont détectées automatiquement (« Scan terminé ! Voici les couleurs détectées. »).
- **Erreurs :** « Il fait trop sombre : mets-toi face à une lumière. », « Scan incomplet, réessaie face à la caméra. », « Aucune caméra détectée ».

### 4.5 Boutique 3D et caisse (M5, scène « caisse »)

Une vraie boutique de décoration en 3D, avec une caisse, un vendeur, un panier sur le comptoir, une télé « Voir le catalogue », des zones Salon, Salle à manger, Cuisine, Rangement et de la musique d'ambiance. L'ambiance suit le jour ou la nuit.

**Déplacement :**
- moitié gauche de l'écran = joystick (un cercle apparaît sous le doigt) ;
- moitié droite = regarder autour de soi en glissant ;
- **gyroscope** : on tourne le téléphone pour regarder ;
- clavier ZQSD et souris sur PC.

**Vues :** 1ère personne par défaut. Un bouton bascule en **3ème personne** : on voit alors son personnage de M1, qui marche avec ses animations. Le choix est mémorisé.

**HUD :**
- **En haut à gauche :**
  - carte profil (avatar rond et prénom, ou « Invité ») ;
  - bouton Réglages ;
  - bouton Vue (icône œil en 1ère personne, silhouette en 3ème personne).
- **En haut à droite :**
  - bouton Suggestions (étincelles) ;
  - bouton Panier avec une pastille rouge qui compte les articles.
- **En haut au centre :** notification en pastille (aide, bienvenue, confirmations).
- **En bas au centre**, au comptoir avec des articles : bouton vert « Payer · total › » qui « respire ». Le bouton Panier clignote en laiton pour attirer l'œil.

**Tiroir latéral** (s'ouvre à droite, ou à gauche pour Réglages et Profil). Il a quatre pages :
- **Suggestions :** produits conseillés, avec un bouton « + » et « Voir le catalogue complet ».
- **Mon panier :**
  - les lignes d'articles, avec les boutons − / + ;
  - le total ;
  - l'adresse de livraison (saisie, ou « Utiliser ma position » par GPS) ;
  - « Confirmer et payer » et « Vider le panier ».
- **Réglages :** interrupteurs « Vue au gyroscope » et « Musique d'ambiance », « Recentrer la vue », aide des commandes.
- **Profil :** avatar, nom, e-mail, adresse, « Mon profil » ou « Se connecter ».

**Panier 3D :** chaque article ajouté tombe dans le panier posé sur le comptoir, avec un petit rebond, et un compteur flotte au-dessus.

**Paiement :**
- Paiement Stripe dans une page web intégrée, avec une fenêtre « Paiement sécurisé par Stripe » (« Rouvrir la page de paiement », « Annuler »).
- Une fois payé : animation de billets qui s'envolent de la caisse, colis animé et remerciement du vendeur.

**Transitions :** fondu vers le noir entre les scènes.

### 4.6 Catalogue (M3, scène « M3 »)

**Décor :** un mur crème avec un grand panneau terracotta, et au centre l'**étagère DecoAR**. Elle est faite de deux bibliothèques laquées crème, avec un fond vert sauge, des rubans lumineux et une **enseigne** en bois sombre (logo DecoAR, « DecoAR » et le nom du rayon). Autour : plantes, lampadaire et tapis sable bordé de terracotta.

**106 produits répartis en 10 rayons :**

| Rayon | Produits |
| --- | --- |
| Salon | 16 |
| Chambre | 12 |
| Cuisine | 16 |
| Salle de bain | 5 |
| Luminaires | 5 |
| Tableaux | 8 |
| Plantes | 9 |
| Objets déco | 16 |
| Multimédia | 9 |
| Bureau & loisirs | 10 |

**Fiche produit (données) :**
- nom, rayon et prix en TND ;
- dimensions réelles (l × p × h en cm) ;
- type de pose : **au sol**, **au mur** ou **sur une table ou au sol** ;
- description courte ;
- coloris ;
- mots-clés pour la recherche.

**Coloris :** « Original », plus des finitions selon la matière :
- tissu : Lin chiné, Velours foncé, Anthracite ;
- bois : Chêne clair, Wengé ;
- déco : Sable, Ardoise.

**Fonctionnalités :**
- **Rayons :** barre d'onglets en haut, défilante. Le rayon actif est en laiton.
- **Étagère 3D :** les produits y sont posés en miniature, avec une étiquette de prix devant chacun.
- **Liste en bas :** des cartes de produit qui défilent horizontalement. Chaque carte montre le type de pose (pastille de couleur), le nom et le prix.
- **Inspection :** toucher un produit (sur l'étagère ou dans la liste) le fait sortir de l'étagère en arc et le place devant la caméra.
  - On le fait tourner à 360° en glissant, et on zoome en pinçant.
  - **On secoue le téléphone pour le reposer.**
  - Une pastille rappelle ces trois gestes.
- **Fiche produit :** un panneau latéral à droite avec :
  - le nom, le rayon et le type de pose ;
  - le prix en laiton, en grand ;
  - les dimensions ;
  - les pastilles de coloris (changement en direct sur le modèle 3D) ;
  - la description ;
  - **« Voir chez moi en AR »** (bouton laiton avec une icône cube) ;
  - **« Ajouter au panier »** (bouton sauge) ;
  - « Reposer sur l'étagère ».
- **Recherche :**
  - une loupe ouvre une barre de saisie ;
  - un bouton **micro** lance la recherche vocale, avec une explication avant la première autorisation ;
  - les résultats s'illuminent et sautillent sur l'étagère ;
  - si aucun résultat n'est dans le rayon ouvert, on passe au bon rayon.
- **Panier :** bouton avec une pastille. Il mène à la caisse M5.
- **Retour :** « ‹ Boutique », avec un fondu.

### 4.7 Essai en réalité augmentée (M4, scène « M4_AR »)

**Avant la pose :**
- La fiche du produit est en haut au centre (nom, coloris, prix).
- Une consigne de scan est en bas, avec un point qui pulse :
  - « Balayez lentement le sol / le mur » ;
  - puis « Touchez la surface pour poser le produit ».
- Les surfaces détectées sont affichées avec le logo DecoAR.

**Pose :**
- Le produit apparaît **à sa taille réelle**, avec une ombre au sol et un éclairage qui suit la lumière de la pièce. Une vibration confirme la pose.
- Le produit refuse une mauvaise surface. Exemples : « Ce produit se pose sur un mur », « Ce produit se pose sur le sol ».

**Guide des gestes** (fenêtre au centre, la première fois, et rouvrable avec le bouton « ? ») :
- **Déplacer :** glissez le produit avec un doigt.
- **Changer la taille :** pincez ou écartez deux doigts.
- **Tourner :** faites pivoter deux doigts.
- **Changer de place :** touchez une autre zone.
- Le bouton « C'est compris » ferme le guide.

**Taille en direct :** sous le nom du produit s'affiche « ● Taille réelle » (vert) ou « ● 125 % de la taille réelle » (laiton).

**Outils à droite** (boutons ronds avec une étiquette) :
- Coloris (coloris suivant) ;
- Taille réelle (remet le produit à 100 %) ;
- Photo (enregistrée dans la galerie, sans l'interface).

**Bas de l'écran :** « Ajouter au panier », un bouton sauge qui respire.

**Haut de l'écran :** « ‹ Rayon », « Boutique », aide et panier.

### 4.8 Espace administration et génération 3D (M6)

**Administration (catalogue 3D) :**
- **En-tête :** logo rond « D », « Administration » et le sous-titre « Catalogue des produits 3D de la boutique ».
- **Tableau :** produit (nom et référence), catégorie (en terracotta), date d'ajout, et les boutons **Voir en 3D** et **Supprimer**. Supprimer demande une confirmation (« Confirmer ? » au deuxième toucher).
- **État vide :** « Aucun produit 3D pour l'instant ».
- **Bouton principal :** « + Générer un modèle 3D ».
- **Retour :** « ‹ Connexion » ou « ‹ Boutique », selon l'écran d'où l'on vient.

**Génération 3D, en trois étapes numérotées** (pastilles terracotta 1, 2, 3) :
1. **Photo du produit :** aperçu, « Prendre une photo », « Galerie ».
2. **Génération 3D :**
   - « Générer le modèle 3D » ;
   - indicateur qui tourne, chronomètre et messages du serveur (« Génération 3D en cours… », « Modèle 3D affiché ! »).
3. **Ajouter au catalogue :** nom du produit, choix de la catégorie en pastilles, « Ajouter au catalogue », puis confirmation.

**Grand cadre 3D :**
- le modèle est posé sur un socle, avec un éclairage de studio, et tourne tout seul ;
- on le fait tourner au doigt, on zoome en pinçant, et un double toucher le recentre ;
- boutons « Rotation : oui / non » et « Recentrer » ;
- état vide : « Votre modèle 3D apparaîtra ici ».

**Mise en page :** en paysage, les étapes sont à gauche et le modèle à droite. En portrait, le modèle est en haut et les étapes en dessous.

---

## 5. Éléments communs à tous les écrans

- **Bulle profil :** avatar rond en haut à gauche dans les écrans où le HUD de la boutique n'est pas présent.
- **Notification (toast) :** pastille de verre en haut au centre, avec un point coloré (laiton pour une information, vert pour une réussite). Elle glisse depuis le haut puis disparaît après environ 2,5 s.
- **Fondu de transition :** noir chaud, 0,3 s.
- **Vibrations :** légère (toucher, coloris), moyenne (pose AR), forte (paiement).
- **Fond assombri :** quand un tiroir est ouvert, un fond noir à 22 % le referme si on le touche.
- **Pastille de compteur :** rond rouge brique (#D15C45), chiffre blanc en gras, « 9+ » au-delà de 9. Elle rebondit à chaque ajout.

---

## 6. Langage visuel actuel

### 6.1 Couleurs de la marque (logo)

| Nom | Hex | Usage |
| --- | --- | --- |
| Terracotta logo | `#C5603F` | Couleur principale de la marque, icône |
| Sauge logo | `#A9C2A0` | Accent naturel |
| Crème logo | `#FBF6EE` | Fond clair, symbole |
| Brun foncé | `#2E2621` | Texte du logo |
| Gris brun | `#6B5E55` | Texte secondaire du logo |

### 6.2 Couleurs de l'interface (HUD boutique, catalogue, AR)

| Nom | Hex | Usage |
| --- | --- | --- |
| Verre | `#1F1A14` à 72 % d'opacité | Fond des boutons flottants, cartes et pastilles |
| Verre opaque | `#1F1A14` à 94 % d'opacité | Tiroirs, fiche produit, fenêtres |
| Crème | `#FAF2E3` | Texte et icônes sur verre |
| Doux | `#B8A38F` | Texte secondaire sur verre |
| Texte clair secondaire | `#D9CDBF` | Détails dans le texte riche |
| Laiton | `#D1A652` | Action principale (AR), onglet actif, prix, sélection |
| Sauge | `#73946E` | Ajouter au panier, Payer, interrupteur actif |
| Terracotta | `#C7734A` | Accent (micro actif, admin, catégories) |
| Encre | `#29211A` | Texte sur fond laiton ou crème |
| Atténué | `#8C7866` | Boutons secondaires |
| Rouge brique | `#D15C45` | Pastille de compteur |
| Succès | `#9ECC94` | Point de notification « réussite », « Taille réelle » |
| Tuile | blanc à 7 % (16 % si active) | Lignes de liste, champs sur verre |
| Liseré | blanc à 10–16 % | Contour fin des pastilles de verre |

**Types de pose (pastilles des cartes produit) :**

| Pose | Hex |
| --- | --- |
| Mur | `#DB9E80` |
| Sol | `#9EC794` |
| Table ou sol | `#E6C275` |

**Espace administration M6 (fond sombre, cartes claires) :**

| Élément | Hex |
| --- | --- |
| Fond | `#2B241C` |
| Carte | `#FAF7F2` |
| Champ | `#F2EDE3` |
| Accent | `#C7734A` |
| Texte | `#33291F` |

**Décor 3D du catalogue :**

| Élément | Hex |
| --- | --- |
| Mur | `#EDE3D1` |
| Étagère laquée | `#F2EBDB` |
| Fond d'étagère | `#859C80` |
| Panneau | `#BD7554` |
| Enseigne | `#33291F` |
| Liseré de l'enseigne | laiton |

### 6.3 Formes, tailles et effets

| Élément | Valeur |
| --- | --- |
| Rayon des pastilles et boutons pilule | 32 |
| Rayon des cartes et lignes | 22 |
| Bouton flottant rond du HUD | 84 |
| Marge au bord de l'écran | 24 |
| Espace entre boutons | 14 |
| Bouton principal | hauteur 72–92 |
| Bouton icône rond | 54–64 |
| Pastille de coloris | 64 |
| Tiroir et fiche produit | largeur 600–620, du haut (sous la barre) jusqu'à 24 du bas |
| Carte produit (catalogue) | 250 × 128 |

**Effets :**
- **Pastille de verre :** ombre douce sous l'élément (noir à 38 %, débord de 14–18), fond de verre et liseré clair de 2,5.
- **Animations :** amorties (≈ 14 « images » d'interpolation par seconde).
  - Rebond de la pastille compteur.
  - « Respiration » des boutons d'action (± 2,5 % d'échelle).
  - Clignotement laiton du panier au comptoir.
  - Point de consigne qui pulse.

### 6.4 Typographie

- **Application :** LiberationSans (régulier et gras), via TextMeshPro.
- **Logo :** Outfit.

**Échelle utilisée (en points, repère 1920 × 1080) :**

| Taille | Usage |
| --- | --- |
| 40 | Grand prix |
| 34–36 | Titres de tiroir et de fiche |
| 28–30 | Boutons principaux |
| 25–26 | Texte de bouton, notification, nom dans une liste |
| 21–23 | Texte courant, onglets, cartes |
| 17–20 | Légendes, étiquettes d'outils, petites mentions |

- Le texte riche mélange gras, 70–82 % de taille et une couleur secondaire pour les sous-titres.

### 6.5 Icônes (vectorielles, monochromes)

Il y a 26 icônes, dessinées par code et teintables :

| Groupe | Icônes |
| --- | --- |
| Navigation | Retour (‹), Chevron (›), Fermer (×) |
| Boutique | Panier (sac), Catalogue (grille 2 × 2), Suggestions (étincelles), Profil (tête et épaules), Réglages (curseurs), Recentrer (viseur), Position (repère de carte) |
| Vue | Œil (1ère personne), Personnage (3ème personne) |
| Quantité | Plus, Moins |
| Recherche | Loupe, Micro |
| AR et produit | Cube (AR), Photo (appareil), Coloris (trois pastilles), Taille (double flèche) |
| Gestes | Déplacer (croix fléchée), Tourner (flèche circulaire), Pincer (deux doigts qui s'écartent), Toucher (doigt et onde) |

### 6.6 Logo et icône d'application

Les fichiers sont dans `docs/logo/` (`decoar-logo.svg`, `decoar-icone.svg`) et `Assets/DecoAR/Core/Icons/`.

Le symbole est une maison et un cube AR, en crème sur un fond terracotta. L'icône Android est adaptative (avant-plan et arrière-plan séparés).

---

## 7. Inventaire des composants

Voici la liste des composants que le design system doit couvrir. Ils existent déjà dans l'application, parfois sous plusieurs formes.

**Navigation**
- Bouton retour pilule (icône et texte)
- Barre d'onglets défilante (rayons)
- Barre du haut avec groupes gauche et droite
- Tiroir latéral (gauche ou droite) avec titre et bouton fermer
- Fond assombri
- Fondu de transition

**Boutons**
- Bouton flottant rond de verre (icône)
- Bouton pilule plein : principal laiton, succès sauge, secondaire, fantôme (texte seul)
- Bouton icône rond dans une liste
- Bouton avec étiquette dessous (outils AR)
- Lien texte coloré

**Données et listes**
- Carte produit (pose, nom, prix)
- Ligne de liste (icône, texte, action)
- Ligne de panier (quantité − / +)
- Tableau d'administration (en-tête, lignes, actions)
- Ligne de total

**Saisie**
- Champ texte sur verre
- Champ texte clair (Auth, admin)
- Champ mot de passe et jauge de force
- Code à 6 chiffres
- Barre de recherche (loupe, champ, micro, valider)
- Interrupteur
- Pastilles de couleur (coloris, couleurs du personnage) avec anneau de sélection
- Pastilles de catégorie (choix unique)

**Retours d'information**
- Notification (toast) avec point d'état
- Pastille de compteur
- Indicateur de chargement qui tourne et chronomètre
- Consigne avec point qui pulse
- Fenêtre d'aide (guide des gestes)
- Fenêtre de paiement
- États vides (illustration simple et deux lignes de texte)
- Messages d'erreur de champ

**Caméra et 3D**
- Aperçu caméra dans un cercle ou un ovale (visage)
- Cadre de visionneuse 3D (coins arrondis, boutons dans le cadre, aide en bas)
- Étapes numérotées (pastille ronde numérotée et titre)
- Étiquette de prix 3D, enseigne 3D

**Profil**
- Carte profil compacte (avatar rond et prénom)
- En-tête de profil (grand avatar, nom, e-mail)

---

## 8. Ton et rédaction

- **Langue :** français. On tutoie dans le compte et le personnage (« Place ton visage… »), et on vouvoie dans la boutique, le catalogue et l'AR (« Touchez un produit… »). À harmoniser (voir section 10).
- **Phrases courtes et concrètes, qui disent quoi faire :** « Balayez lentement le sol », « Pincez pour zoomer ».
- **Confirmations positives :** « Visage enregistré », « Monstera ajouté au panier », « Parfait ! ».
- **Prix :** « 1 890 TND ». **Dimensions :** « 101 × 94 × 105 cm (l × p × h) ».
- **Ponctuation :** guillemets français « », points de suspension « … » pour les actions en cours.

---

## 9. États à prévoir

Pour chaque écran, prévoir :

- **Chargement :** serveur, génération 3D, reconnaissance du visage, création du visage 3D.
- **Vide :** panier vide, catalogue 3D vide, aucun résultat de recherche, aucune photo, modèle 3D pas encore généré.
- **Erreur :** réseau ou serveur injoignable, caméra refusée ou absente, trop sombre, surface AR incompatible, champ invalide, paiement annulé.
- **Succès :** ajout au panier, visage enregistré, produit posé, paiement confirmé, photo enregistrée.
- **Désactivé :** bouton indisponible pendant un traitement (opacité 55 %).
- **Sélection :** onglet actif, coloris choisi, carte du produit inspecté (liseré laiton).
- **Permissions :** caméra, micro, position, réseau local, chacune avec une phrase d'explication avant la demande système.

---

## 10. Ce que le design system doit unifier

Les modules ont été faits par des personnes différentes. Le style n'est donc pas encore identique partout.

| Écrans | Style actuel |
| --- | --- |
| Boutique M5, catalogue M3, AR M4 | Verre fumé sombre sur la 3D, crème, laiton, sauge (style de référence le plus abouti) |
| Administration M6 | Fond brun foncé, cartes crème, accent terracotta |
| Connexion, inscription, profil (Auth) et personnage (M1) | Style plus clair, avec leurs propres boutons et champs |

**Points à décider :**
1. Une seule palette de référence. Le terracotta du logo (`#C5603F`) et celui de l'interface (`#C7734A`) sont proches mais différents, de même pour les deux crèmes et les deux sauges.
2. Deux thèmes officiels :
   - **« verre sombre »** pour tout ce qui flotte sur la 3D ou la caméra ;
   - **« papier clair »** pour les écrans de formulaire (compte, profil, administration).
3. Une échelle typographique unique, et peut-être Outfit dans toute l'application pour reprendre le logo.
4. Les mêmes composants partout : bouton retour, champ texte, interrupteur, notification, pastille de couleur.
5. Un seul registre : le tutoiement ou le vouvoiement.
6. Des règles de lisibilité sur la caméra AR : contraste minimal et fond de verre obligatoire sous chaque texte.
