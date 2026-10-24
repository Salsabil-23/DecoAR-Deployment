# DecoAR - Backend (Node.js + Express + MongoDB)

API d'inscription et de connexion (mot de passe ou Face ID) utilisee par la scene Unity `Assets/DecoAR/Auth`.

## Lancer

1. Avoir une base MongoDB :
   - en local : installer [MongoDB Community](https://www.mongodb.com/try/download/community) (service `mongod` sur le port 27017),
   - ou en ligne : un cluster gratuit MongoDB Atlas (copier l'URI `mongodb+srv://...`).
2. Dans `Backend/` :

```bash
npm install
```

```bash
cp .env.example .env
```

   puis renseigner `MONGODB_URI` et `JWT_SECRET` dans `.env`.
3. Demarrer :

```bash
npm run dev
```

   -> `[API] DecoAR en ecoute sur http://localhost:3000`. Test rapide : http://localhost:3000/api/health

Tests automatiques (MongoDB en memoire, rien a installer) :

```bash
npm test
```

## Routes

| Methode | Route | Corps | Reponse |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | `firstName, lastName, email, phone?, password` | `201 { token, user }` |
| POST | `/api/auth/login` | `email, password` | `{ token, user }` |
| POST | `/api/auth/biometric/enroll` (JWT) | `deviceId, deviceName` | `201 { biometricToken, user }` |
| POST | `/api/auth/biometric/login` | `email, deviceId, biometricToken` | `{ token, user }` |
| DELETE | `/api/auth/biometric/:deviceId` (JWT) | - | `{ user }` |
| GET | `/api/users/me` (JWT) | - | `{ user }` |

Routes "(JWT)" : en-tete `Authorization: Bearer <token>`.
Erreurs : `{ message, field }` (`field` = champ du formulaire a surligner dans Unity, ex. `email`).

## Comment marche Face ID

Le visage n'est **jamais** envoye au serveur : c'est le telephone (Secure Enclave / Keystore) qui le verifie.

1. Activation (apres inscription ou connexion) : le telephone demande Face ID -> si OK, Unity appelle
   `biometric/enroll`. Le serveur cree un jeton aleatoire, n'en garde que le hash SHA-256 dans
   `users.biometricDevices` et renvoie le jeton au telephone, qui le range (Keychain / Keystore).
2. Connexion : Face ID OK -> Unity envoie `email + deviceId + biometricToken` a `biometric/login` -> JWT.

## Collection `users`

`firstName, lastName, email (unique), phone, passwordHash (bcrypt), faceIdEnabled,
biometricDevices[{ deviceId, deviceName, tokenHash, createdAt, lastUsedAt }], createdAt, updatedAt`
