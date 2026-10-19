# DecoAR

**DecoAR** est un prototype d'application mobile de décoration qui associe une boutique 3D explorée avec un avatar à l'essai de produits dans son intérieur en réalité augmentée.

Le projet est développé avec **Unity et C#**. Il vise à rendre l'achat de décoration plus concret : découvrir les produits dans un environnement immersif, puis visualiser leur taille et leur apparence chez soi avant de prendre une décision.

> **État du dépôt :** le socle du module M1 (avatar et contrôles) et un prototype du module M4 (réalité augmentée) sont présents. Les modules M2 (boutique), M3 (catalogue de produits) et M5 (caisse et commande) ne sont pas encore intégrés. Le parcours d'achat complet décrit ci-dessous est l'objectif du projet, et non une fonctionnalité actuellement disponible de bout en bout.

## Sommaire

- [Objectifs](#objectifs)
- [Périmètre fonctionnel](#périmètre-fonctionnel)
- [Technologies](#technologies)
- [Prérequis](#prérequis)
- [Installation](#installation)
- [Lancer le projet](#lancer-le-projet)
- [Générer une application mobile](#générer-une-application-mobile)
- [Organisation du projet](#organisation-du-projet)
- [Notes sur les capteurs et les données](#notes-sur-les-capteurs-et-les-données)
- [Licences et ressources](#licences-et-ressources)

## Objectifs

Le projet vise à proposer une expérience de vente de décoration mobile autour de cinq modules :

1. **M1 — Personnage et contrôles :** choisir et personnaliser un avatar, puis se déplacer dans la boutique.
2. **M2 — Boutique et zones :** explorer une boutique 3D organisée par catégories.
3. **M3 — Bibliothèque de produits :** consulter les fiches produits, inspecter les modèles 3D et sélectionner des variantes.
4. **M4 — Réalité augmentée :** visualiser un produit dans une pièce, à l'échelle réelle, et enregistrer une photo.
5. **M5 — Caisse et commande :** gérer un panier et confirmer une commande simulée.

Le périmètre exclut le paiement réel, le back-office marchand ainsi que les comptes et la synchronisation en ligne.

## Périmètre fonctionnel

| Module | État dans le dépôt |
| --- | --- |
| M1 — Personnage et contrôles | Socle disponible : sélection et personnalisation d'avatar, déplacement, caméra à la troisième personne et scènes de test. |
| M2 — Boutique et zones | À développer : seule une scène de test M1 avec un environnement et des éléments interactifs simples est présente ; les trois zones et l'ambiance complètes ne sont pas intégrées. |
| M3 — Bibliothèque de produits | À développer : l'intégration du catalogue et le transfert de produits depuis M3 vers M4 restent à faire. |
| M4 — Réalité augmentée | Prototype disponible : scène AR, placement conditionné par le type de surface, mise à l'échelle, estimation de lumière, ombre et capture vers la galerie. |
| M5 — Caisse et commande | À développer : panier persistant, formulaire de livraison et historique des commandes ne sont pas encore intégrés. |

Le prototype M4 utilise un produit de test configuré dans la scène. Il ne constitue pas encore un flux connecté au catalogue M3. Les vibrations passent par un service commun, mais utilisent actuellement une vibration système unique plutôt que des intensités distinctes.

## Technologies

- **Unity Editor :** `6000.6.2f1`
- **Langage :** C#
- **Rendu :** Universal Render Pipeline (URP)
- **Réalité augmentée :** AR Foundation `6.6.2`, ARKit et ARCore
- **Entrées :** Unity Input System `1.20.0`
- **Interaction XR :** XR Interaction Toolkit `3.6.0`
- **Interface :** Unity UI et TextMeshPro
- **Animations d'avatar :** modèles et animations Humanoid importés depuis Mixamo
- **Galerie native :** Unity Native Gallery
- **Versionnage des ressources volumineuses :** Git LFS

## Prérequis

- Unity Hub et Unity Editor **`6000.6.2f1`**.
- Modules de build Unity selon la cible :
  - **iOS Build Support** pour générer un projet Xcode ;
  - **Android Build Support**, avec Android SDK & NDK Tools et OpenJDK, pour Android.
- **macOS et Xcode** pour compiler, signer et installer une version iOS.
- Un téléphone compatible ARKit ou ARCore pour valider les fonctions de réalité augmentée. La caméra et l'AR ne sont pas représentatives dans l'éditeur Unity.
- **Git LFS** pour récupérer les modèles, textures et fichiers audio versionnés avec LFS.

## Installation

```bash
git lfs install
git clone https://github.com/Maya-Marzouki/DecoAR-2026.git
cd DecoAR-2026
git lfs pull
```

Ouvrir ensuite le dossier cloné dans Unity Hub et lancer le projet avec la version d'éditeur `6000.6.2f1`. Unity résoudra les dépendances déclarées dans `Packages/manifest.json` à l'ouverture du projet.

> Si Git LFS n'est pas installé ou si les fichiers LFS ne sont pas récupérés, certains modèles ou textures peuvent être absents ou remplacés par des fichiers pointeurs.

## Lancer le projet

1. Dans Unity, ouvrir `Assets/DecoAR/M1_Player/Scenes/M1_AvatarSelection.unity`.
2. Appuyer sur **Play**.
3. Choisir un avatar, sélectionner une couleur de tenue et valider pour entrer dans la scène de test `M1_Test`.
4. Dans l'éditeur, utiliser le joystick affiché à l'écran ou les touches **WASD / flèches** pour déplacer l'avatar. La caméra peut être orientée avec la souris (clic droit) ou par glissement tactile.

Pour essayer le prototype AR, ouvrir `Assets/DecoAR/M4_AR/Scenes/M4_AR.unity` et lancer la scène sur un appareil mobile compatible. Déplacer lentement l'appareil pour détecter les surfaces, puis toucher une surface compatible avec le type de placement configuré dans la scène.

### Scènes principales

| Scène | Chemin |
| --- | --- |
| Sélection d'avatar | `Assets/DecoAR/M1_Player/Scenes/M1_AvatarSelection.unity` |
| Scène de test M1 (environnement simplifié) | `Assets/DecoAR/M1_Player/Scenes/M1_Test.unity` |
| Prototype AR M4 | `Assets/DecoAR/M4_AR/Scenes/M4_AR.unity` |

### Génération des scènes M1

Les outils de génération de scènes et de ressources M1 sont disponibles dans l'éditeur Unity, sous **DecoAR > M1** :

- **Tout générer** : regénère les scènes de test et de sélection d'avatar ;
- **Mettre à jour animations et catalogue** : actualise le contrôleur d'animation et le catalogue d'avatars ;
- des entrées séparées permettent de générer chaque scène.

La génération reconstruit les scènes M1. Sauvegarder toute modification manuelle de ces scènes avant d'utiliser ces commandes.

## Générer une application mobile

1. Ouvrir **File > Build Profiles**.
2. Sélectionner **iOS** ou **Android**, puis installer les modules de plateforme correspondants si Unity le demande.
3. Vérifier la liste **Scene List** et placer la scène d'entrée souhaitée en premier. Pour le parcours M1, utiliser `M1_AvatarSelection.unity`.
4. Créer le build. Pour iOS, ouvrir le projet généré dans Xcode afin de le compiler et de le signer.
5. Tester l'application sur un appareil réel, en particulier les permissions caméra et l'expérience AR.

La configuration du dépôt inclut ARKit et ARCore, mais les builds, permissions et compatibilités doivent être vérifiés sur les appareils visés. L'identifiant Android est encore celui du template Unity (`com.unity.template.ar_mobile`) ; il doit être remplacé avant toute distribution. Aucun appareil mobile ni build de publication n'est garanti par ce dépôt.

## Organisation du projet

```text
Assets/
└── DecoAR/
    ├── Core/
    │   └── Scripts/       # État partagé et services communs
    ├── M1_Player/
    │   ├── Data/          # Catalogue d'avatars
    │   ├── Editor/         # Outils de génération Unity
    │   ├── Mixamo/        # Modèles et animations d'avatar
    │   ├── Prefabs/
    │   ├── Scenes/
    │   └── Scripts/       # Avatar, déplacement, caméra et interface
    └── M4_AR/
        ├── Materials/
        ├── Prefabs/
        ├── Scenes/
        ├── Scripts/       # Placement, taille, lumière, photo et haptique
        └── Shaders/
Packages/
    ├── manifest.json      # Dépendances Unity déclarées
    └── packages-lock.json # Versions résolues des dépendances
ProjectSettings/           # Configuration du projet Unity
```

Le `GameManager` conserve l'avatar, sa tenue et les préférences de gyroscope et de vibrations dans `PlayerPrefs`. La position de l'avatar et le compteur de panier sont actuellement gérés en mémoire pendant la session ; cela ne remplace pas la persistance du panier et des commandes prévue pour M5.

## Notes sur les capteurs et les données

- **AR et caméra :** nécessitent un appareil compatible et l'autorisation caméra.
- **Gyroscope :** utilisé par la caméra M1 lorsqu'il est activé et disponible ; l'orientation tactile reste utilisable.
- **Vibrations :** le réglage utilisateur permet de les désactiver. L'implémentation actuelle s'appuie sur la vibration native du téléphone.
- **Galerie :** le prototype M4 enregistre les captures AR dans la galerie via Unity Native Gallery ; le résultat dépend des permissions du système.
- **GPS, reconnaissance vocale, capteur de luminosité ambiante, catalogue complet et commandes :** ces éléments du cahier des charges ne sont pas encore intégrés dans les modules présents.

## Licences et ressources

- Le projet dépend notamment de packages Unity et du plugin **Unity Native Gallery**. Consulter leurs licences respectives et les fichiers de licence fournis avec les dépendances.
- Les ressources issues de Mixamo et tout modèle, texture ou son ajouté au projet doivent être utilisés conformément à leur licence d'origine.
- Aucune licence globale du projet n'est déclarée dans ce dépôt. Vérifier les droits sur les ressources avant toute redistribution ou publication.
