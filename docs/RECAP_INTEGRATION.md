# DecoAR : récapitulatif de l'intégration

Branche d'intégration : `integration-modules`, dernier commit `cf5d888`, poussée sur GitHub.

Dépôt : https://github.com/Maya-Marzouki/DecoAR-2026

## 1. Travail intégré

| Branche | Module | Contenu | État |
| --- | --- | --- | --- |
| `melek-Mouelhi` | M1 : personnage | 3 avatars MetaPerson au choix, couleurs et tenue, scan 3D du visage calculé sur le téléphone, icône et logo, system design | ✅ intégré |
| `Maya-Marzouki` | M4 : réalité augmentée | Essai des produits en AR, logo en AR, permission caméra | ✅ intégré |
| `Medkarim-ouertatani` | M5 : boutique et caisse | Boutique en première personne, panier, paiement, liens vers M3 et M4 | ✅ intégré (sert de base) |
| `salsabil-hafsi` | Auth | Inscription, connexion, Face ID, profil, verrouillage, backend Node/MongoDB, service visage | ✅ intégré |
| `ali-belkadhi` | M6 : génération 3D | Écran d'administration, génération d'un modèle GLB à partir d'une photo (Flask + Trellis), visionneuse 3D | ✅ intégré et relié |
| `main` | Rien | Premier commit seulement | Rien à reprendre |

## 2. Parcours de l'application (selon le cahier des charges)

```
Connexion / Inscription (Auth)
        │
        ▼
Crée ton personnage (M1_Personnalisation) ──► Scan du visage (M1_ScanVisage)
        │
        ▼
Boutique (caisse, M5) ──► Rayon / fiche produit (M3) ──► Essai en AR (M4_AR)
        │
        ▼
Caisse et paiement (M5)

Bouton profil (en haut à gauche, sur tous les écrans)
        │
        ▼
Profil ──► Espace administration (3D) (AdminScene) ──► Génération 3D (M6_Generator)
```

Ordre des scènes dans le build :

1. Auth
2. Profile
3. M1_Personnalisation
4. M1_ScanVisage
5. caisse
6. M3
7. M4_AR
8. AdminScene
9. M6_Generator

## 3. Ce qui a été modifié pour relier les modules

- **Auth** : après la connexion, l'application ouvre `M1_Personnalisation` (`firstSceneAfterLogin`).
- **Profil** : quand tu reviens du profil, le joueur de la boutique M5 se retrouve à l'endroit où il était (`ProfileNavigation`).
- **M1** : « Entrer dans la boutique » ouvre la scène `caisse` (`SceneNames.Shop`). Le champ d'adresse du serveur a été retiré de l'écran de scan, et le visage est calculé sur le téléphone.
- **M6** : le script `Assets/DecoAR/m6_3D/Scripts/M6Navigation.cs` ajoute les boutons de navigation sans modifier les scènes d'Ali :
  - Profil → « Espace administration (3D) » ;
  - Administration → « Générer 3D » et « ‹ Boutique » ;
  - Génération 3D → « ‹ Administration ».
- **Build** : menu `DecoAR > Application complète` (`Assets/DecoAR/Core/Editor/DecoARAppBuild.cs`) :
  - « 1. Mettre la liste des scènes du parcours » ;
  - « 2. Construire et lancer sur le téléphone ». Pendant le build, il choisit le pipeline de la boutique et autorise la rotation automatique.
- **Paquets** : ajout de `com.unity.cloud.gltfast` (6.20.0) et de `com.yasirkula.nativecamera` pour le module M6.

## 4. Vérifications

- Compilation Unity (batch mode, Android) : **0 erreur**.
- Tests du backend Auth (`npm test`) : **11/11 réussis**.
- Toutes les scènes ouvertes par `LoadScene` sont dans la liste du build.
- ⚠️ Le parcours complet n'a pas encore été testé sur le téléphone.

## 5. Lancer le projet

1. Unity : ouvrir le projet sur la branche `integration-modules` et laisser l'import des paquets se terminer.
2. Menu `DecoAR > Appliquer l'icône de l'application`.
3. Lancer le serveur Auth :
   ```bash
   cd Backend
   npm install
   npm run dev
   ```
4. Lancer le serveur M6 :
   ```bash
   cd Assets/DecoAR/Backend_M6
   python -m venv .venv
   .venv\Scripts\activate
   pip install -r requirements.txt
   python app.py
   ```
   Puis mettre l'adresse du PC dans `serverUrl` du composant `M6Controller`.
5. Menu `DecoAR > Application complète > 2. Construire et lancer sur le téléphone`.

Téléphone : le Samsung A57 est compatible ARCore. Le A16 ne l'est pas : le module AR (M4) n'y fonctionne pas.

## 6. Laissés hors de Git volontairement

Ces fichiers restent seulement sur le PC d'Ali :

- `Backend_M6/.venv` : l'environnement Python, à recréer avec `requirements.txt` ;
- `Backend_M6/.env` : il peut contenir une clé secrète ;
- `Backend_M6/uploads` : les photos envoyées.

Ils sont listés dans `Assets/DecoAR/Backend_M6/.gitignore`.

Les fichiers suivants ne sont pas commités non plus :

- les fichiers FLAME et `face_landmarker.task` ;
- les réglages locaux `URP-Performant.asset`, `ProjectSettings.asset` et `ProjectVersion.txt`.

## 7. Reste à faire

- [ ] **Ali doit changer la clé de son `.env`** : le fichier est encore visible sur GitHub dans l'historique de la branche `ali-belkadhi`.
- [ ] Tester tout le parcours sur le téléphone (A57).
- [ ] Ouvrir une pull request `integration-modules` → `main` et la faire valider par Maya et l'équipe.
- [ ] Récupérer les réglages locaux de M1 mis de côté :
  ```bash
  git switch melek-Mouelhi
  git stash pop
  ```

## 8. Documents liés

- System design : [`docs/SYSTEM_DESIGN.md`](SYSTEM_DESIGN.md)
- Logo et icône : [`docs/logo/`](logo/), design system complet : [`DesignSystem/`](../DesignSystem/README.md)
