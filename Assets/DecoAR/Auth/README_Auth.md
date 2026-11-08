# Auth - Compte, reconnaissance faciale et profil

Tout se genere par le menu **DecoAR > Auth > Tout generer** :
- `Resources/DecoAR_AuthSettings.asset` : adresse de l'API (telephone / editeur), delai de verrouillage, noms des scenes
- `Resources/DecoAR_AuthOverlay.prefab` : bouton profil + ecran de verrouillage, charges automatiquement dans toutes les scenes
- `Scenes/Auth.unity` (premiere scene du build) et `Scenes/Profile.unity`

A lancer avant de tester : le backend (`Backend/`, `npm run dev`) **et** le service visage
(`face-service/`, `.venv\Scripts\python app.py`). Voir leurs README.

## Parcours

```
Ouverture -> session encore valide ? -> oui : connexion automatique -> boutique
                                     -> non : inscription / connexion
Inscription -> formulaire -> defi de mouvements (face, 2 mouvements, face) -> compte cree -> code e-mail -> boutique
Connexion   -> mot de passe, ou "Se connecter avec mon visage" (defi : face + 1 mouvement)
            -> "Mot de passe oublie ?" -> code par e-mail -> nouveau mot de passe
App en arriere-plan > 5 min -> ecran verrouille -> defi de mouvements (ou se reconnecter)
Bouton rond en haut a gauche -> Profil : photo, nom/telephone, mot de passe, reenregistrer le visage, deconnexion
```

Le visage n'est pas analyse dans Unity : `FaceCamera` affiche la camera frontale (en miroir), `FaceChallenge` fait
le defi tire au sort par le serveur (fleche + consignes, une image toutes les ~0,35 s envoyee a `/face/pose`),
puis le serveur (service Python OpenCV) revérifie les mouvements et compare le visage dans MongoDB.
`SelfieCapture` sert seulement a la photo de profil.
Avec un e-mail saisi, la connexion compare au visage de ce compte (1:1) ; sans, elle cherche parmi tous les comptes.

## Espace admin

- Compte admin : `npm run make-admin -- <email>` dans `Backend/` (voir son README). `AuthSession.IsAdmin`.
- Ouverture : PC derriere la caisse (M5AdminTerminal) ou Profil > "Espace administration (3D)" (admins seulement)
  -> `AuthOverlay.ConfirmAdmin` : defi de mouvements avec le visage du compte -> jeton admin 30 min
  (`AuthSession.HasAdminAccess`, en-tete `X-Admin-Token` via `AuthApi.Authorize(req, admin: true)`).
- Sans jeton valide, AdminScene et M6_Generator renvoient a la boutique ; apres le verrouillage automatique,
  l'espace admin se reconfirme.
- Catalogue 3D partage sur le serveur (`M6Catalog` -> `/api/catalog`), generation 3D par `/api/m6/*`.

## Pour les autres modules

| Quoi | Comment |
| --- | --- |
| Utilisateur connecte | `AuthSession.User` (`firstName`, `email`, `faceEnrolled`, `hasPhoto`...), `AuthSession.IsLoggedIn`, evenement `AuthSession.Changed` |
| Appeler une route protegee | en-tete `Authorization: Bearer ` + `AuthSession.Token` |
| Ouvrir le profil / revenir | `ProfileNavigation.Open()` / `ProfileNavigation.Back()` (la position de l'avatar M1 est gardee) |
| Verrouiller (test) | menu DecoAR > Auth > Verrouiller maintenant (en Play) |

## Adresse de l'API

Menu **DecoAR > Auth > Reglages** :
- `Device Api Url` : `http://localhost:3000` avec le telephone branche en USB (renvoi `adb reverse` refait
  automatiquement par `Editor/AndroidUsbReverse.cs`), ou URL Render, ou IP du PC sur le Wi-Fi
- `Editor Api Url` : `http://localhost:3000`
- HTTP non chiffre : Player Settings > Other Settings > **Allow downloads over HTTP** = Always allowed

## Camera

Android demande l'autorisation camera a la premiere utilisation. Dans l'editeur, la webcam du PC est utilisee.
Conseils affiches : visage dans le cercle, bien eclaire, une seule personne.
