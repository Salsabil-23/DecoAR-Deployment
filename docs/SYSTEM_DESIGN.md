# DecoAR — System Design

Ce document décrit l'architecture technique de DecoAR : les composants de l'application, leurs responsabilités, la façon dont ils communiquent, les données manipulées et les technologies utilisées. Il sert de référence commune à toute l'équipe.

> Les diagrammes sont écrits en Mermaid : GitHub les affiche directement. Dans un autre éditeur, utiliser un aperçu Markdown compatible Mermaid.

## Sommaire

1. [Présentation du système](#1-présentation-du-système)
2. [Architecture globale](#2-architecture-globale)
3. [Technologies utilisées](#3-technologies-utilisées)
4. [Modules et responsabilités](#4-modules-et-responsabilités)
5. [Diagramme de cas d'utilisation](#5-diagramme-de-cas-dutilisation)
6. [Diagramme de composants](#6-diagramme-de-composants)
7. [Diagramme de classes](#7-diagramme-de-classes)
8. [Diagrammes de séquence](#8-diagrammes-de-séquence)
9. [Diagrammes d'activité](#9-diagrammes-dactivité)
10. [Données et sauvegarde](#10-données-et-sauvegarde)
11. [Capteurs](#11-capteurs)
12. [Organisation des dossiers Unity](#12-organisation-des-dossiers-unity)
13. [Principes de conception appliqués](#13-principes-de-conception-appliqués)
14. [Game design et system design](#14-game-design-et-system-design)
15. [Limites et évolutions](#15-limites-et-évolutions)

---

## 1. Présentation du système

DecoAR est une application mobile 3D et AR de vente de décoration. Le client :

1. se connecte (mot de passe ou Face ID) ;
2. crée un personnage à son image : couleurs, tenue, scan 3D de son visage ;
3. se déplace dans une boutique 3D avec ce personnage ;
4. inspecte les produits d'un rayon en 3D et choisit une variante ;
5. pose le produit dans sa pièce en réalité augmentée, à taille réelle ;
6. ajoute les produits au panier et confirme une commande simulée.

Hors périmètre : paiement réel, back-office marchand, synchronisation des commandes en ligne.

## 2. Architecture globale

L'application est un **client Unity unique** découpé en modules. Les modules ne s'appellent pas directement : ils partagent un état commun, le `GameManager`, et passent d'une scène à l'autre par leur nom (`SceneNames`). Deux services tournent sur un PC du réseau local pour ce que le téléphone ne fait pas seul.

```mermaid
flowchart TB
  subgraph Tel["Téléphone : application Unity"]
    direction TB
    AUTH["Auth : inscription, connexion, Face ID"]
    M1["M1 Personnage : personnalisation, scan visage, déplacement, HUD"]
    M2["M2 Boutique : zones, mini-carte (scène de test M1_Test)"]
    M3["M3 Catalogue : étagères, inspection 3D, variantes, recherche"]
    M4["M4 Réalité augmentée : pose à taille réelle, lumière, photo"]
    M5["M5 Caisse : panier 3D, commande simulée"]
    CORE[("Core : GameManager, HapticManager, ShopTypes")]
    AUTH --> M1 --> M2 --> M3 --> M4
    M3 --> M5
    M4 --> M5
    AUTH -.-> CORE
    M1 -.-> CORE
    M2 -.-> CORE
    M3 -.-> CORE
    M4 -.-> CORE
    M5 -.-> CORE
  end
  subgraph PC["PC sur le réseau local"]
    API["Backend Auth : Node.js + Express, port 3000"]
    DB[("MongoDB")]
    FACE["M1_FaceServer (optionnel) : Python + FLAME, port 8000"]
    API --> DB
  end
  AUTH -- "HTTP JSON + jeton JWT" --> API
  M1 -. "HTTP, 3 photos JPEG, seulement si une adresse est saisie" .-> FACE
```

Traits pleins : enchaînement des scènes. Pointillés vers Core : lecture et écriture de l'état partagé.

## 3. Technologies utilisées

| Domaine | Technologie |
| --- | --- |
| Moteur | Unity 6 (6000.6) |
| Langage | C# |
| IDE | Visual Studio (solution `DecoAR-2026.slnx`) |
| Rendu | Universal Render Pipeline 17.6 |
| Réalité augmentée | AR Foundation 6.6.2, ARCore 6.6.2 (Android), ARKit 6.6.2 (iOS), XR Management 4.7 |
| Interaction XR | XR Interaction Toolkit 3.6.1 |
| Entrées | Input System 1.20 (tactile, joystick virtuel, clavier dans l'éditeur) |
| Interface | Unity UI (uGUI) et TextMeshPro |
| Personnage | Avatar MetaPerson, rig Humanoid, animations Mixamo |
| Galerie | Unity Native Gallery (photos prises en AR) |
| Données de jeu | ScriptableObject (catalogue produits, profil du personnage), PlayerPrefs, fichiers JSON / PNG locaux |
| Backend Auth | Node.js, Express, MongoDB, JWT |
| Serveur de visage (optionnel) | Python, FastAPI, Uvicorn, OpenCV, NumPy, MediaPipe Face Landmarker, modèle FLAME |
| Versionnage | Git, GitHub, Git LFS (modèles, textures, sons) |
| Cibles | Android (APK), iOS (projet Xcode) |

## 4. Modules et responsabilités

| Module | Responsabilité | Scènes | Scripts principaux | État |
| --- | --- | --- | --- | --- |
| **Core** | État partagé entre scènes, vibrations, types communs | aucune (créé automatiquement) | `GameManager`, `AvatarLook`, `HapticManager`, `ShopTypes` | Disponible |
| **Auth** | Inscription, connexion, Face ID, session | `Auth` | `AuthScreen`, `AuthApi`, `AuthSession`, `BiometricAuth` | Branche `salsabil-hafsi` |
| **M1 Personnage** | Personnalisation, scan du visage, déplacement, caméra 3e personne, interactions, HUD | `M1_Personnalisation`, `M1_ScanVisage`, `M1_Test` | `CharacterCustomizer`, `FaceScan3D`, `CharacterAppearance`, `PlayerController`, `ThirdPersonCamera`, `PlayerInteractor`, `HUDController` | Disponible |
| **M2 Boutique** | Boutique en zones, ambiance, mini-carte | `M1_Test` sert de boutique | `Interactable`, `M3ShelfEntrance` | À intégrer |
| **M3 Catalogue** | 39 produits en 7 zones, étagère, inspection 360°, variantes, fiche, recherche texte et vocale, secousse | `M3` | `M3ProductCatalog`, `M3CatalogSelector`, `M3Shelf`, `M3Inspector`, `M3VariantApplier`, `M3VoiceSearch`, `M3ShakeDetector` | Disponible |
| **M4 AR** | Pose sur sol, mur ou table à taille réelle, ombre, lumière, variante, photo, panier | `M4_AR` | `M4PlacementGate`, `M4Sizing`, `M4LightMatcher`, `M4Photo`, `M4ShopActions` | Disponible |
| **M5 Caisse** | Vendeur, panier 3D, colis animé, commande simulée en TND | `caisse` | `M5CheckoutUI`, `Basket3D`, `ParcelAnimation`, `VendorNPC` | Première version |

Chaque module a aussi ses **outils d'éditeur** (dossier `Editor/`) qui regénèrent ses scènes et ses ressources : menus `DecoAR > M1`, `M3`, `M4`, `M5`, `Auth`.

## 5. Diagramme de cas d'utilisation

```mermaid
flowchart LR
  C(("Client"))
  subgraph App["Application DecoAR"]
    UC1(["S'inscrire / se connecter"])
    UC2(["Se connecter avec Face ID"])
    UC3(["Personnaliser son personnage"])
    UC4(["Scanner son visage en 3D"])
    UC5(["Se déplacer dans la boutique"])
    UC6(["Ouvrir un rayon"])
    UC7(["Inspecter un produit en 3D"])
    UC8(["Changer de variante"])
    UC9(["Rechercher un produit (texte ou voix)"])
    UC10(["Voir le produit en AR chez soi"])
    UC11(["Prendre une photo AR"])
    UC12(["Ajouter au panier"])
    UC13(["Confirmer la commande"])
    UC14(["Régler gyroscope et vibrations"])
  end
  FS["Serveur de visage (optionnel)"]
  API["Backend Auth"]
  C --- UC1 & UC3 & UC5 & UC6 & UC9 & UC10 & UC12 & UC13 & UC14
  UC1 -. extend .-> UC2
  UC3 -. extend .-> UC4
  UC6 -. include .-> UC7
  UC7 -. extend .-> UC8
  UC10 -. extend .-> UC11
  UC1 --- API
  UC4 --- FS
```

## 6. Diagramme de composants

```mermaid
flowchart TB
  subgraph Core["Core"]
    GM["GameManager (singleton)"]
    HM["HapticManager"]
    ST["ShopTypes : SceneNames, ProductPlacement, CartItem"]
    AL["AvatarLook"]
  end
  subgraph M1["M1_Player"]
    CC["CharacterCustomizer"]
    FS3["FaceScan3D"]
    STO["HeadStore / FaceMaskStore / FaceStore"]
    CA["CharacterAppearance"]
    PC["PlayerController + ThirdPersonCamera"]
    PI["PlayerInteractor + Interactable"]
    HUD["HUDController"]
  end
  subgraph M3["M3_ProductCatalog"]
    CAT[("M3ProductCatalog (ScriptableObject)")]
    SEL["M3CatalogSelector"]
    INS["M3Inspector + M3Shelf"]
    VAR["M3VariantApplier"]
    VOX["M3VoiceSearch + M3ShakeDetector"]
  end
  subgraph M4["M4_AR"]
    PG["M4PlacementGate"]
    SZ["M4Sizing"]
    LM["M4LightMatcher"]
    PH["M4Photo"]
    SA["M4ShopActions"]
  end
  subgraph M5["Checkout"]
    CO["M5CheckoutUI"]
    BK["Basket3D + ParcelAnimation"]
  end
  CC --> AL --> GM
  FS3 --> STO --> CA
  CA --> AL
  PC --> HM
  PI --> SEL
  HUD --> GM
  SEL --> CAT
  SEL --> INS --> VAR
  SEL --> GM
  GM --> PG
  PG --> SZ & LM & VAR
  SA --> GM
  PH --> HM
  GM --> CO --> BK
```

## 7. Diagramme de classes

Classes principales et leurs relations (attributs et méthodes simplifiés).

```mermaid
classDiagram
  class GameManager {
    +static GameManager Instance
    +AvatarLook Look
    +bool GyroEnabled
    +bool HapticsEnabled
    +string SelectedProductId
    +GameObject SelectedProductPrefab
    +Vector3 SelectedProductSizeCm
    +ProductPlacement SelectedProductPlacement
    +int SelectedVariantIndex
    +string CurrentCategory
    +int CartCount
    +SetLook(AvatarLook)
    +SelectProduct(id, prefab, sizeCm, placement, variant)
    +AddToCart(CartItem)
    +TakeCartItems() List~CartItem~
    +event CartCountChanged
    +event ProductSelected
  }
  class AvatarLook {
    +Color peau
    +Color cheveux
    +Color haut
    +Color bas
    +Color chaussures
    +bool visagePhoto
    +Get(LookSlot) Color
    +Set(LookSlot, Color)
  }
  class CartItem {
    +string productId
    +string name
    +string variantName
    +Color color
    +float price
    +int quantity
  }
  class ProductPlacement {
    <<enumeration>>
    Sol
    Mur
    Surface
  }
  class HapticManager {
    <<static>>
    +Vibrate(HapticIntensity)
  }
  class CharacterProfile {
    <<ScriptableObject>>
    +GameObject prefab
    +float height
    +Color[] skinColors
    +Material faceMaskMaterial
  }
  class CharacterAppearance {
    <<static>>
    +Spawn(profile, look, parent) Animator
    +Apply(character, look, profile)
  }
  class FaceScan3D {
    -string defaultServer
    -Scan(ARFace)
    -SendToServer(views)
    -Build(...) bool
  }
  class FaceMaskStore {
    <<static>>
    +Save(FaceMaskData, Texture2D)
    +TryLoad(out data, out texture, out mesh) bool
    +Delete()
  }
  class HeadStore {
    <<static>>
    +Save(json) bool
    +TryLoad(out data, out texture, out mesh) bool
    +Delete()
  }
  class M3ProductCatalog {
    <<ScriptableObject>>
    +List~ProductOption~ products
    +Get(id) ProductOption
    +GetByCategory(category) List~ProductOption~
  }
  class ProductOption {
    +string id
    +string displayName
    +string category
    +float price
    +Vector3 sizeCm
    +GameObject prefab
    +ProductPlacement placement
    +List~ProductVariant~ variants
    +GetVariant(index) ProductVariant
  }
  class ProductVariant {
    +string name
    +Color color
    +Material material
  }
  class M3CatalogSelector
  class M4PlacementGate {
    +SetProduct(ProductOption, variant)
    +NextVariant()
    +ResetToRealSize()
    +OnSpawnTriggered(position, normal)
  }
  class M5CheckoutUI {
    +AddProduct(name, price, color, prefab)
  }

  GameManager *-- AvatarLook
  GameManager o-- CartItem
  GameManager --> ProductPlacement
  ProductOption --> ProductPlacement
  M3ProductCatalog *-- ProductOption
  ProductOption *-- ProductVariant
  M3CatalogSelector --> M3ProductCatalog
  M3CatalogSelector --> GameManager : SelectProduct / AddToCart
  M4PlacementGate --> GameManager : lit le produit
  M4PlacementGate --> ProductOption
  M5CheckoutUI --> GameManager : TakeCartItems
  FaceScan3D --> FaceMaskStore
  FaceScan3D --> HeadStore
  FaceScan3D --> GameManager : SetLook
  CharacterAppearance --> CharacterProfile
  CharacterAppearance --> AvatarLook
  CharacterAppearance --> FaceMaskStore
  CharacterAppearance --> HeadStore
```

## 8. Diagrammes de séquence

### 8.1 Scan du visage (M1)

Le visage est calculé sur le téléphone par défaut. Le serveur FLAME n'est appelé que si une adresse est saisie sur l'écran du scan ; en cas d'échec, l'application revient au calcul local.

```mermaid
sequenceDiagram
  actor U as Client
  participant App as FaceScan3D
  participant AR as ARCore / ARKit
  participant S as M1_FaceServer (optionnel)
  participant GM as GameManager
  U->>App: Scanner mon visage
  App->>AR: suivi du visage, caméra frontale
  AR-->>App: maillage du visage (~470 points)
  App->>App: 3 captures : face, gauche, droite
  alt adresse de serveur saisie
    App->>S: POST /head (3 photos JPEG)
    S-->>App: tête FLAME, texture, cheveux (JSON)
    App->>App: HeadStore.Save
  else pas d'adresse ou serveur injoignable
    App->>App: texture des 3 vues sur le maillage
    App->>App: FaceMaskStore.Save
  end
  App->>GM: SetLook (peau mesurée, visage scanné)
  App-->>U: retour à la personnalisation
```

### 8.2 Du rayon à l'AR puis à la caisse (M3, M4, M5)

```mermaid
sequenceDiagram
  actor U as Client
  participant B as Boutique (M1/M2)
  participant M3 as M3CatalogSelector
  participant GM as GameManager
  participant M4 as M4PlacementGate
  participant M5 as M5CheckoutUI
  U->>B: touche une étagère
  B->>GM: CurrentCategory = zone
  B->>M3: charge la scène M3
  U->>M3: choisit un produit et une variante
  M3->>GM: SelectProduct(id, prefab, sizeCm, placement, variant)
  U->>M3: Voir en AR
  M3->>M4: charge la scène M4_AR
  M4->>GM: lit le produit sélectionné
  U->>M4: touche une surface détectée
  M4->>M4: pose à taille réelle, ombre, lumière
  U->>M4: Ajouter au panier
  M4->>GM: AddToCart(CartItem)
  GM-->>B: CartCountChanged (compteur du HUD)
  U->>B: bouton Panier
  B->>M5: charge la scène caisse
  M5->>GM: TakeCartItems()
  U->>M5: Confirmer
  M5-->>U: colis animé, vibration
```

### 8.3 Connexion (Auth)

```mermaid
sequenceDiagram
  actor U as Client
  participant A as AuthScreen
  participant API as Backend Auth
  participant DB as MongoDB
  U->>A: e-mail + mot de passe (ou Face ID)
  A->>API: POST /api/auth/login
  API->>DB: recherche de l'utilisateur
  DB-->>API: utilisateur (mot de passe haché)
  API-->>A: { token JWT, user }
  A->>A: AuthSession.Token = token
  A-->>U: scène de personnalisation
```

## 9. Diagrammes d'activité

### 9.1 Pose d'un produit en AR (M4)

```mermaid
flowchart TD
  S([Ouverture de M4_AR]) --> P{Produit sélectionné dans le GameManager ?}
  P -- non --> T[Produit de test TAB-01] --> D
  P -- oui --> D[Détection des plans : déplacer lentement le téléphone]
  D --> Q{Plan compatible avec le type de pose ?}
  Q -- non --> M[Message : chercher un sol, un mur ou une table] --> D
  Q -- oui --> TAP[Le client touche la surface]
  TAP --> SZ[Taille réelle à partir de sizeCm]
  SZ --> SH[Ombre et lumière accordées à la pièce]
  SH --> V[Vibration de pose]
  V --> ACT{Action du client}
  ACT -- Variante --> VAR[Variante suivante] --> ACT
  ACT -- Photo --> PH[Photo vers la galerie] --> ACT
  ACT -- Ajouter au panier --> CART[GameManager.AddToCart] --> ACT
  ACT -- Retour --> R([Retour au rayon ou à la boutique])
```

### 9.2 Personnalisation et scan (M1)

```mermaid
flowchart TD
  A([Crée ton personnage]) --> B[Choisir peau, cheveux, tenue]
  B --> C{Scanner le visage ?}
  C -- non --> G
  C -- oui --> D{Suivi du visage disponible ?}
  D -- non --> E[Scan photo : couleur de peau et cheveux] --> G
  D -- oui --> F[3 captures : face, gauche, droite]
  F --> H{Adresse de serveur saisie et serveur joignable ?}
  H -- oui --> I[Tête FLAME complète sur l'avatar]
  H -- non --> J[Visage calculé sur le téléphone, plaqué sur l'avatar]
  I --> G
  J --> G
  G[Enregistrer le look dans le GameManager] --> K([Entrer dans la boutique])
```

## 10. Données et sauvegarde

| Donnée | Emplacement | Format | Durée de vie |
| --- | --- | --- | --- |
| Catalogue des produits | `Assets/DecoAR/M3_ProductCatalog/Data/M3_ProductCatalog.asset` | ScriptableObject | Livré avec l'application |
| Profil du personnage | `Assets/DecoAR/M1_Player/Data` | ScriptableObject | Livré avec l'application |
| Apparence, gyroscope, vibrations | `PlayerPrefs` (`DecoAR.Look`, `DecoAR.GyroEnabled`, `DecoAR.HapticsEnabled`) | JSON et entiers | Persistante |
| Adresse du serveur de visage | `PlayerPrefs` (`DecoAR.HeadServer.v2`), vide par défaut | Texte | Persistante |
| Visage calculé sur le téléphone | Stockage privé de l'application (`FaceMaskStore`) | Maillage JSON + PNG | Persistante |
| Tête FLAME (option serveur) | Stockage privé : `decoar_tete3d.json`, `decoar_tete3d.png`, `decoar_cheveux3d.png` | JSON + PNG | Persistante |
| Photos reçues par le serveur de visage | PC : `M1_FaceServer/sortie/derniere_requete/` | JPEG | Écrasées à chaque scan |
| Comptes utilisateurs | MongoDB | Documents, mot de passe haché | Persistante |
| Session de connexion | `AuthSession` (jeton JWT) | Texte | Session |
| Produit sélectionné, panier, position dans la boutique | `GameManager` (mémoire) | Objets C# | Session |
| Photos AR | Galerie du téléphone | Image | Persistante |

## 11. Capteurs

| Capteur | Utilisation | Module |
| --- | --- | --- |
| Caméra frontale + suivi du visage | Maillage et texture du visage | M1 |
| Caméra arrière + détection des plans | Pose des produits | M4 |
| Estimation de la lumière | Éclairage du produit accordé à la pièce | M4 |
| Gyroscope | Orientation de la caméra (activable dans les réglages) | M1 |
| Accéléromètre | Secouer pour reposer le produit inspecté | M3 |
| Microphone | Recherche vocale (iOS) | M3 |
| Vibreur | Collision, pose AR, scan, panier | Tous, via `HapticManager` |
| Biométrie (Face ID) | Connexion sans mot de passe | Auth |

## 12. Organisation des dossiers Unity

Le projet est rangé **par module** plutôt que par type de fichier : chaque module contient ses propres scripts, scènes, prefabs et ressources, ce qui permet à chaque membre de l'équipe de travailler sans toucher aux dossiers des autres.

```text
Assets/
├── DecoAR/
│   ├── Core/
│   │   └── Scripts/          # GameManager, AvatarLook, HapticManager, ShopTypes
│   ├── Auth/                 # (branche salsabil-hafsi) inscription, connexion, Face ID
│   ├── M1_Player/
│   │   ├── Animations/       # contrôleur d'animation (idle, marche, interaction)
│   │   ├── Data/             # CharacterProfile
│   │   ├── Editor/           # générateurs de scènes, import MetaPerson / Mixamo
│   │   ├── Materials/
│   │   ├── Mixamo/           # avatar et animations
│   │   ├── Prefabs/          # M1_PlayerRig
│   │   ├── Scenes/           # M1_Personnalisation, M1_ScanVisage, M1_Test
│   │   ├── Scripts/          # personnage, scan, déplacement, caméra, HUD
│   │   ├── Shaders/
│   │   └── UI/
│   ├── M3_ProductCatalog/
│   │   ├── Data/             # M3_ProductCatalog.asset
│   │   ├── Editor/           # génération des modèles, du catalogue et de la scène
│   │   ├── Materials/
│   │   ├── Prefabs/
│   │   ├── Scenes/           # M3
│   │   └── Scripts/
│   └── M4_AR/
│       ├── Editor/
│       ├── Materials/
│       ├── Prefabs/
│       ├── Scenes/           # M4_AR
│       ├── Scripts/
│       └── Shaders/
├── Checkout/                 # M5 : Scenes/caisse, Scripts, Prefabs, Audio, Animations
├── Plugins/                  # code natif iOS / Android (reconnaissance vocale, galerie)
└── Settings/                 # profils URP
Backend/                      # (branche salsabil-hafsi) API Node.js + MongoDB
M1_FaceServer/                # serveur Python de tête 3D (optionnel)
docs/                         # ce document
```

## 13. Principes de conception appliqués

| Principe | Où il est appliqué |
| --- | --- |
| **État partagé unique (singleton)** | `GameManager` créé avant la première scène et conservé entre les scènes : chaque scène peut être lancée seule dans l'éditeur. |
| **Faible couplage entre modules** | Les modules ne se référencent pas : ils passent par le `GameManager` et chargent la scène suivante par `SceneNames`. Un module peut être remplacé (par exemple `M1_Test` par la future boutique M2) en changeant un seul nom. |
| **Communication par événements** | `CartCountChanged`, `ProductSelected`, `CartItemAdded`, `GyroChanged`, `HapticsChanged` : le HUD et les autres écrans réagissent sans être appelés directement. |
| **Données séparées du code (ScriptableObject)** | Catalogue des produits et profil du personnage modifiables dans l'éditeur sans toucher au code. |
| **Service unique pour un besoin transversal** | `HapticManager` pour toutes les vibrations, avec respect du réglage de l'utilisateur. |
| **Dégradation progressive** | Scan : visage sur le téléphone si pas de serveur ; scan photo si pas de suivi du visage ; produit de test si M4 est lancé seul. |
| **Scènes générées par l'éditeur** | Les menus `DecoAR > ...` reconstruisent les scènes : moins de conflits Git sur les fichiers `.unity`. |
| **Adaptation à l'écran** | `ResponsiveLayout` et `M3ResponsiveLayout` changent la disposition en portrait et en paysage. |

## 14. Game design et system design

| Game design (pour le joueur) | System design (technique) |
| --- | --- |
| Créer un personnage à son image | `CharacterCustomizer`, `FaceScan3D`, `CharacterAppearance`, stockage du visage |
| Se promener dans une boutique | `PlayerController`, `ThirdPersonCamera`, `VirtualJoystick`, `Interactable` |
| Examiner un produit sous tous les angles | `M3Inspector`, gestes tactiles, `M3VariantApplier` |
| Voir le produit chez soi, à la bonne taille | AR Foundation, `M4PlacementGate`, `M4Sizing`, `M4LightMatcher` |
| Remplir son panier et commander | `GameManager.AddToCart`, `M5CheckoutUI`, `Basket3D` |
| Retrouver son personnage à chaque lancement | `PlayerPrefs`, fichiers du visage dans le stockage privé |

## 15. Limites et évolutions

- **M2** n'est pas encore intégré : `M1_Test` sert de boutique (`SceneNames.Shop`).
- **Panier en mémoire** : il disparaît à la fermeture ; M5 devra le rendre persistant (livraison, historique).
- **Auth** et son backend sont sur la branche `salsabil-hafsi`, pas encore fusionnés.
- **Réseau** : HTTP non chiffré sur le réseau local ; une version publiée demanderait HTTPS et un serveur hébergé.
- **Identifiant Android** encore celui du modèle Unity (`com.unity.template.ar_mobile`) ; à remplacer avant distribution.
- **Serveur FLAME** : tête complète mais un PC et environ une minute sont nécessaires ; le visage calculé sur le téléphone est donc le choix par défaut.
