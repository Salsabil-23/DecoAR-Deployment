# 12 · Intégration dans Unity 6

Compter une demi-journée pour la mise en place, puis une migration écran par écran.

## 1. Copier les fichiers

```
Assets/DecoAR/Design/
├── Scripts/Runtime/DecoTokens.cs        ← unity/Scripts/Runtime/
├── Scripts/Runtime/DecoUI.cs
├── Scripts/Editor/DecoSpriteImporter.cs ← unity/Scripts/Editor/ (doit rester dans un dossier « Editor »)
├── Fonts/Outfit_*.ttf                   ← fonts/
└── Resources/DecoAR/
    ├── UI/      ← assets/unity-sprites/*.png
    ├── Icons/   ← icons/png-unity/128/*.png
    └── Fonts/   ← les font assets TMP créés à l'étape 2
```

Importer `DecoSpriteImporter.cs` **avant** les PNG : il règle automatiquement Sprite, 9-slice et compression pour tout fichier `dc-*`. Pour des PNG déjà importés : clic droit sur le dossier › *Reimport*.

Si le projet a une assembly definition, ajouter les références `Unity.TextMeshPro` et `UnityEngine.UI`.

## 2. Police

Voir `03-typographie.md` § « Mise en place dans TextMeshPro ». Noms attendus par `DecoUI.Font()` :
`Outfit-Regular SDF`, `Outfit-Medium SDF`, `Outfit-SemiBold SDF`, `Outfit-Bold SDF`.

## 3. Canvas

Garder le réglage actuel : *Scale With Screen Size*, 1920 × 1080, *Match* 0,5, *Reference Pixels Per Unit* 100. Ajouter un `RectTransform` « SafeArea » ajusté à `Screen.safeArea` comme parent du HUD.

## 4. Remplacer les couleurs

Chercher les `new Color(` et les hex dans le code des briques communes, et les remplacer par les rôles :

```csharp
using DecoAR.Design;

// avant
priceText.color = new Color32(0xD1, 0xA6, 0x52, 255);
glass.color = new Color(0.12f, 0.10f, 0.08f, 0.72f);

// après
priceText.color = ThemeVerre.Price;
glass.color = ThemeVerre.SurfaceGlass;
```

Table de correspondance : `02-couleurs.md` § 4.

## 5. Construire avec DecoUI

```csharp
using DecoAR.Design;
using UnityEngine;

public class FicheProduitView : MonoBehaviour
{
    [SerializeField] RectTransform actions;   // VerticalLayoutGroup, spacing = DecoSpace.S3

    void Start()
    {
        var ar = DecoUI.Button(actions, "Voir chez moi", DecoButtonKind.Principal, DecoTheme.Verre, icon: "ar");
        ar.onClick.AddListener(OpenAR);

        var add = DecoUI.Button(actions, "Ajouter au panier", DecoButtonKind.Commerce, DecoTheme.Verre, icon: "panier");
        add.onClick.AddListener(() => DecoToast.Show(transform.root, "Monstera ajouté au panier", DecoToast.Kind.Succes));

        DecoUI.Button(actions, "Reposer sur l'étagère", DecoButtonKind.Fantome, DecoTheme.Verre, compact: true);
    }

    void OpenAR() { /* … */ }
}
```

Bouton du HUD avec pastille :

```csharp
var cart = DecoUI.GlassIconButton(hudRight, "panier");
var badge = DecoUI.Badge(cart.transform, cartCount);
badge.transform.parent.gameObject.AddComponent<DecoPop>();
```

Bouton qui respire : `payButton.gameObject.AddComponent<DecoBreathe>();`

## 6. Matériaux 3D

Mettre à jour les matériaux du décor M3 (mur, étagère, panneau, enseigne) avec les hex de `02-couleurs.md` § 5. Remplacer la texture de l'enseigne par `assets/3d/enseigne-etagere.png`.

## 7. AR

- Réticule : remplacer le visuel du plan de pose par un `Quad` portant `assets/ar/reticule-recherche.png` (matériau *Unlit/Transparent*), et changer la texture en `valide` / `refuse` selon la surface.
- Surfaces détectées : matériau des plans AR Foundation avec `assets/ar/motif-surface.png` en tuile (tiling ≈ 4 par mètre) et `surface-logo.png` au centre du premier plan détecté.

## 8. Ordre de migration conseillé

1. Tokens + police + sprites (aucun changement visible si les couleurs restent).
2. HUD boutique, catalogue, AR (thème Verre, déjà le plus proche).
3. M6 (passage en Papier).
4. Auth, Profil, M1 (Papier, nouveaux champs).
5. Textes au tutoiement (`09-redaction.md`).
6. Icône d'application et splash (`08-icones-application.md`).

## 9. Vérifier

- Tester sur un vrai téléphone, en extérieur, devant un mur blanc : tout le texte du HUD doit rester lisible.
- Tester sur un écran 20:9 et un 4:3 (tablette) en paysage et en portrait.
- Tester avec les noms de produits les plus longs : rien ne doit déborder (`TextOverflowModes.Ellipsis` sur les noms, deux lignes maximum).
