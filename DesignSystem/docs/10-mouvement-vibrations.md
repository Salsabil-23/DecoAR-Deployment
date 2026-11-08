# 10 · Mouvement et vibrations

## Durées

| Token | ms | Usage |
| --- | --- | --- |
| `instant` | 90 | Bouton pressé, changement de couleur |
| `rapide` | 150 | Interrupteur, sortie d'un toast, coloris |
| `base` | 240 | Apparition d'un élément, voile, toast |
| `lent` | 360 | Tiroir, fenêtre, rebond de pastille |
| `scene` | 450 | Fondu d'entrée de scène, produit qui sort de l'étagère |

## Courbes

| Token | Courbe | Usage |
| --- | --- | --- |
| `standard` | cubic-bezier(0.2, 0, 0, 1) | Par défaut |
| `entree` | cubic-bezier(0.05, 0.7, 0.1, 1) | Ce qui arrive à l'écran |
| `sortie` | cubic-bezier(0.3, 0, 0.8, 0.15) | Ce qui part |
| `ressort` | raideur 380, amortissement 22 | Pastille compteur, chute dans le panier 3D, pose AR |

En C# : `DecoMotion.EaseStandard / EaseEntree / EaseSortie / Ressort(t)`. Toujours `Time.unscaledDeltaTime` pour l'interface (fonctionne en pause).

## Animations signatures

| Animation | Description |
| --- | --- |
| **Respiration** | Échelle ±2,5 %, période 2,4 s (`DecoBreathe`). Un seul bouton à la fois. |
| **Anneau de pose** | L'anneau pointillé tourne lentement (8 s / tour) pendant la recherche de surface, se ferme en un trait plein laiton à la pose. |
| **Sortie de l'étagère** | Le produit suit un arc (450 ms, `entree`), les autres produits s'assombrissent à 60 %. Secousse = trajet inverse. |
| **Pose AR** | L'objet tombe de 6 cm avec `ressort`, l'ombre apparaît en 240 ms, vibration moyenne. |
| **Ajout au panier** | Le bouton rétrécit à 0,95 puis revient, la pastille fait `DecoPop`, le panier du HUD clignote laiton deux fois. |
| **Génération 3D** | Halo AR qui respire derrière le symbole, anneau qui tourne. À la fin : fondu du cube pointillé vers le modèle. |
| **Paiement** | Billets, colis, merci du vendeur (existant) ; ajouter le toast « Paiement confirmé. Merci ! » et la vibration forte. |

## Réduire les animations

Si l'appareil a « réduire les animations » activé (ou un réglage DecoAR « Animations réduites ») : pas de respiration, pas de rebond, transitions en fondu simple de 150 ms.

## Vibrations

| Niveau | Quand | Android | iOS |
| --- | --- | --- | --- |
| Légère | Toucher, coloris, onglet, interrupteur | `EFFECT_TICK` (~10 ms) | `UIImpactFeedbackGenerator(.light)` |
| Moyenne | Pose AR, ajout panier, scan réussi | `EFFECT_CLICK` (~20 ms) | `.medium` |
| Forte | Paiement confirmé, erreur bloquante | `EFFECT_HEAVY_CLICK` (~40 ms) | `.heavy` / `UINotificationFeedbackGenerator(.success / .error)` |

Règles : jamais deux vibrations à moins de 100 ms ; respecter le réglage « Vibrations » de l'application ; pas de vibration sur un bouton désactivé.
