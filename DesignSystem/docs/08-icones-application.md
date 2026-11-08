# 08 · Icônes d'application (iOS et Android)

![Aperçu](../app-icon/apercu-icones.png)

L'icône est l'image « Maison AR », recadrée **sans sa plaque** : iOS et Android dessinent eux-mêmes les coins arrondis. Si on gardait la plaque, on aurait un carré arrondi dans un carré arrondi, avec une ombre coupée.

Les coins extérieurs à la plaque ont été prolongés avec ses propres couleurs, pour qu'aucun liseré ne dépasse sous le masque du système.

## iOS (iPhone)

Dossier `app-icon/ios/AppIcon.appiconset/` : à glisser tel quel dans `Assets.xcassets` du projet Xcode généré par Unity.

| Fichier | Apparence |
| --- | --- |
| `AppIcon-1024.png` | Normale (App Store et écran d'accueil). Sans transparence. |
| `AppIcon-1024-tinted.png` | Teintée (iOS 18) : niveaux de gris, iOS applique la teinte choisie. |

Pas de variante sombre dédiée : en mode sombre, iOS 18 garde l'icône normale.

Dans **Unity** (`Player Settings › iOS › Icon`) : renseigner les tailles demandées avec `app-icon/ios/tailles-unity/Icon-NNN.png`.

## Android

### Avec Unity

`Player Settings › Android › Icon › Adaptive` :

| Couche | Fichier (432 × 432) |
| --- | --- |
| Background | `app-icon/android/unity-adaptive/background-432.png` |
| Foreground | `app-icon/android/unity-adaptive/foreground-432.png` |

`Legacy` et `Round` : `app-icon/android/res/mipmap-xxxhdpi/ic_launcher.png` et `ic_launcher_round.png`.

Le premier plan est l'icône réduite à la zone visible (72 dp sur 108), aux bords fondus dans un arrière-plan de la même couleur : le rond, le carré arrondi et la goutte ne coupent ni le toit ni l'anneau.

Option sans Unity : `app-icon/android/res/` contient les mipmaps de toutes les densités et le XML adaptatif, à copier dans `Assets/Plugins/Android/DecoARIcons.androidlib/res/`.

### Play Store

- Icône : `app-icon/android/play-store-512.png`.
- Bannière : `assets/store/play-store-banniere-1024x500.png`.

## Écran de démarrage

- `assets/splash/splash-portrait.png` (1080 × 1920) et `splash-paysage.png` (1920 × 1080).
- Unity : `Player Settings › Splash Image`, couleur de fond `#FDF9F3`.
- Android 12+ (écran système) : icône = `foreground-432.png`, fond `#FCE6D4`.
