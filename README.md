# DecoAR

**DecoAR** est un prototype d'application mobile de décoration qui associe une boutique 3D explorée avec un avatar à l'essai de produits dans son intérieur en réalité augmentée.

Le projet est développé avec **Unity et C#**. Il vise à rendre l'achat de décoration plus concret : découvrir les produits dans un environnement immersif, puis visualiser leur taille et leur apparence chez soi avant de prendre une décision.

> **État du dépôt :** le socle du module M1 (avatar et contrôles), le module M3 (bibliothèque de produits), le module M4 (réalité augmentée) et une première caisse M5 sont présents. Le module M2 (boutique) n'est pas encore intégré : la scène de test M1 sert de boutique. Le parcours d'achat complet décrit ci-dessous est l'objectif du projet, et non une fonctionnalité actuellement disponible de bout en bout.

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
| M3 — Bibliothèque de produits | Disponible : catalogue partagé (15 produits, 3 zones, 2 variantes chacun), étagère 3D par zone, sortie animée du produit, inspection 360° et zoom par pincement, variantes en direct, fiche produit, « Voir en AR » et « Ajouter au panier », recherche texte et vocale (iOS), secousse pour reposer, vibrations légères. |
| M4 — Réalité augmentée | Disponible : produit et variante reçus de M3, placement selon le type (mur, sol, sol ou table), taille réelle et retour à la taille réelle, ombre, estimation de lumière, changement de variante, photo vers la galerie, « Ajouter au panier », retour au rayon ou à la boutique, vibration à la pose. |
| M5 — Caisse et commande | À développer : panier persistant, formulaire de livraison et historique des commandes ne sont pas encore intégrés. |

M3 transmet le produit choisi (prefab, dimensions, type de pose, variante) à M4 via le `GameManager`. Lancée seule, la scène AR utilise le produit de test `TAB-01`. Les vibrations passent par le `HapticManager` commun : intensités distinctes sur iOS, vibration système unique sur Android.

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

Pour le rayon M3, ouvrir `Assets/DecoAR/M3_ProductCatalog/Scenes/M3.unity` (ou interagir avec l'étagère de `M1_Test`). Dans l'éditeur : clic pour choisir un produit, glisser pour le tourner, molette pour zoomer, touche **R** pour simuler la secousse. La recherche vocale ne fonctionne que sur iPhone ; ailleurs, utiliser le champ de recherche.

Pour essayer l'AR, choisir un produit dans M3 puis « Voir en AR », ou ouvrir `Assets/DecoAR/M4_AR/Scenes/M4_AR.unity` directement sur un appareil compatible. Déplacer lentement l'appareil pour détecter les surfaces, puis toucher une surface compatible avec le type de pose du produit.

### Scènes principales

| Scène | Chemin |
| --- | --- |
| Sélection d'avatar | `Assets/DecoAR/M1_Player/Scenes/M1_AvatarSelection.unity` |
| Scène de test M1 (environnement simplifié) | `Assets/DecoAR/M1_Player/Scenes/M1_Test.unity` |
| Rayon et catalogue M3 | `Assets/DecoAR/M3_ProductCatalog/Scenes/M3.unity` |
| Réalité augmentée M4 | `Assets/DecoAR/M4_AR/Scenes/M4_AR.unity` |

### Génération M3 et M4

- **DecoAR > M3 > Tout generer** : regénère les modèles des produits, le catalogue `M3_ProductCatalog.asset`, les prefabs de boutons et la scène `M3` ;
- **DecoAR > M4 > Reparer et completer la scene AR** : recrée dans `M4_AR` les objets DecoAR (placement, lumière, photo, interface) et rebranche l'événement de placement du template XRI. La commande peut être relancée sans risque.

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
