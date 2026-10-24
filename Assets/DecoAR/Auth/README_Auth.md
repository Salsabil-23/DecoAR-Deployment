# Auth - Inscription et connexion (Face ID)

Scene : `Scenes/Auth.unity`, generee par le menu **DecoAR > Auth > Creer la scene inscription + connexion**
(elle est placee en premiere scene du build). Backend : dossier `Backend/` a la racine du depot (voir son README).

Test rapide : lancer le backend (`npm run dev`), ouvrir la scene, Play. Dans l'editeur Face ID est simule
(`EditorBiometricSimulator.NextResult = false` pour simuler un echec).

## Ecran

- Inscription : prenom, nom, e-mail, telephone (optionnel), mot de passe + confirmation, jauge de robustesse,
  interrupteur "Activer Face ID", erreurs surlignees champ par champ (validation locale puis serveur).
- Connexion : e-mail + mot de passe, ou bouton "Se connecter avec Face ID" si Face ID est deja active sur ce telephone.
- S'adapte au paysage (marque a gauche, carte a droite) et au portrait (marque en haut).
- Apres succes : scene `nextSceneName` (par defaut `M1_AvatarSelection`).

## Pour les autres modules

| Quoi | Comment |
| --- | --- |
| Utilisateur connecte | `AuthSession.User` (`firstName`, `email`...), `AuthSession.IsLoggedIn`, evenement `AuthSession.Changed` |
| Appeler une route protegee | en-tete `Authorization: Bearer ` + `AuthSession.Token` |
| Deconnexion | `AuthSession.Logout()` |

## Adresse de l'API

`AuthScreen.apiBaseUrl` (inspecteur du Canvas `UI`) :
- editeur : `http://localhost:3000`
- telephone : IP du PC sur le meme Wi-Fi, ex. `http://192.168.1.20:3000` (`ipconfig` pour la trouver,
  autoriser le port 3000 dans le pare-feu Windows)
- HTTP non chiffre : Player Settings > Other Settings > **Allow downloads over HTTP** = Always allowed (dev uniquement)

## Capteur Face ID (a brancher)

Tout passe par `IBiometricAuthenticator` (`Scripts/BiometricAuth.cs`) :

```csharp
BiometricAuth.Current = new MonPluginFaceId(); // au demarrage, avant la scene Auth
```

- iOS : `LAContext.evaluatePolicy(.deviceOwnerAuthenticationWithBiometrics)` (plugin `.mm`) ;
  Player Settings : renseigner **NSFaceIDUsageDescription** (sinon l'app plante a la demande Face ID).
- Android : `androidx.biometric.BiometricPrompt` (plugin `.aar` ou `AndroidJavaObject`).
- Rappeler `done` sur le thread principal de Unity.
- Ranger le jeton d'appareil dans le Keychain / Keystore : `BiometricVault` utilise PlayerPrefs en attendant.
