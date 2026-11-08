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
| POST | `/api/auth/register` | `firstName, lastName, email, phone?, password, face: { challengeToken, frames[4] }` | `201 { token, user }` + code par e-mail |
| POST | `/api/auth/login` | `email, password` | `{ token, user }` |
| POST | `/api/auth/verify-email` (JWT) | `code` | `{ user }` (`emailVerified: true`) |
| POST | `/api/auth/verify-email/resend` (JWT) | - | `{ message }` (1 envoi / minute) |
| POST | `/api/auth/forgot-password` | `email` | `{ message }` (toujours le meme, ne revele pas si le compte existe) |
| POST | `/api/auth/reset-password` | `email, code, password` | `{ token, user }` |
| POST | `/api/auth/face/challenge` | `purpose` (`enroll` / `login`) | `{ challengeToken, steps, yawDelta, pitchDelta, centerMaxYaw }` |
| POST | `/api/auth/face/pose` | `image` | `{ ok, yaw, pitch }` (retour en direct pendant le defi) |
| POST | `/api/auth/face/login` | `face: { challengeToken, frames[2] }, email?` | `{ token, user }` (avec e-mail : 1:1, sans : cherche parmi tous) |
| PUT | `/api/users/me/face` (JWT) | `face: { challengeToken, frames[4] }` | `{ user }` (reenregistrer son visage) |
| GET | `/api/users/me` (JWT) | - | `{ user }` (sert aussi a la connexion automatique) |
| PATCH | `/api/users/me` (JWT) | `firstName?, lastName?, phone?` | `{ user }` |
| POST | `/api/users/me/password` (JWT) | `currentPassword, newPassword` | `{ message }` |
| PUT | `/api/users/me/photo` (JWT) | `image` (JPEG/PNG base64, 300 Ko max) | `{ user }` |
| GET | `/api/users/me/photo` (JWT) | - | l'image |
| DELETE | `/api/users/me/photo` (JWT) | - | `{ user }` |
| POST | `/api/m6/generate` | multipart `image` (JPG/PNG, 6 Mo max) | `202 { job_id, status }` (transmis a decoar-m6) |
| GET | `/api/m6/status/:jobId` | - | `{ job_id, status: processing / completed / failed, error }` |
| GET | `/api/m6/model/:jobId` | - | fichier `.glb` (`409` si pas pret) |
| GET | `/api/m6/test-model` | - | fichier `.glb` de test (sans quota Hugging Face) |
| POST | `/api/admin/confirm` (JWT) | `face: { challengeToken, frames }` | `{ adminToken, expiresIn }` (compte admin + son visage) |
| GET | `/api/catalog` (JWT) | - | `{ categories, products: [{ id, name, category, modelSize, createdAt }] }` |
| GET | `/api/catalog/:id/model` (JWT) | - | fichier `.glb` du produit |
| POST | `/api/catalog?name=&category=` (admin) | fichier `.glb` (`application/octet-stream`, 15 Mo max) | `201 { product }` |
| PATCH | `/api/catalog/:id` (admin) | `name?, category?` | `{ product }` |
| DELETE | `/api/catalog/:id` (admin) | - | `{ message }` |

Routes "(admin)" et `/api/m6/generate`, `/status`, `/model` : en plus du JWT, en-tete `X-Admin-Token` (jeton de
`/api/admin/confirm`, 30 min). Le compte doit avoir `role: "admin"` dans MongoDB.

## Comptes administrateurs

Un compte devient admin uniquement depuis le PC (jamais depuis l'application), avec le `.env` qui pointe sur Atlas :

```bash
npm run make-admin -- prenom.nom@gmail.com
```

`npm run make-admin -- prenom.nom@gmail.com --remove` retire le role, `npm run make-admin -- --list` liste les admins.
Le compte doit avoir un visage enregistre : l'espace admin s'ouvre avec le defi de reconnaissance faciale.

Routes `/api/m6/*` : passerelle vers le module 3D (`Backend_M6`, service `decoar-m6`, adresse `M6_SERVICE_URL`) :
Unity n'utilise qu'une seule adresse, `https://decoar-api.onrender.com`.

Routes "(JWT)" : en-tete `Authorization: Bearer <token>`.
Erreurs : `{ message, field }` (`field` = champ du formulaire a surligner dans Unity, ex. `email`).

## Codes par e-mail

Codes a 6 chiffres (verification du compte, mot de passe oublie) : valables 15 min, 5 essais, 1 envoi par minute,
seul leur hash SHA-256 est stocke. Envoi selon le `.env` :

| Fournisseur | Quand | Reglage |
| --- | --- | --- |
| `console` | rien de configure | le code s'affiche dans le terminal : `[MAIL] ...` |
| `smtp` (Gmail) | en local | `SMTP_USER` + `SMTP_PASS` (mot de passe d'application Google, validation en 2 etapes requise) |
| `mailjet` | en ligne | `MAILJET_API_KEY` + `MAILJET_SECRET_KEY` + `MAIL_FROM` (Render gratuit bloque le SMTP, Mailjet passe par HTTPS) |
| `brevo` | en ligne (autre choix) | `BREVO_API_KEY` + `MAIL_FROM` |

## Mettre en ligne (Render, gratuit)

1. Atlas > Network Access : autoriser `0.0.0.0/0` (les IP de Render changent).
2. [render.com](https://render.com) > New > **Blueprint** > choisir le depot : `render.yaml` (racine) cree le service `decoar-api`.
   Le depot est prive : la proprietaire (Maya-Marzouki) doit autoriser l'application Render GitHub sur le depot.
3. Renseigner `MONGODB_URI`, `BREVO_API_KEY`, `MAIL_FROM`, `FACE_SERVICE_URL` (URL de `decoar-face`) et la meme
   `FACE_SERVICE_KEY` dans les deux services (`JWT_SECRET` est genere automatiquement).
4. URL obtenue (ex. `https://decoar-api.onrender.com`) -> Unity : menu DecoAR > Auth > Reglages > `Device Api Url`.

Le service gratuit s'endort apres 15 min sans requete : le premier appel prend ~1 min (l'app reveille le serveur
des l'ouverture de l'ecran de connexion).

## Reconnaissance faciale

Le service Python `face-service/` (OpenCV YuNet + SFace) doit tourner a cote (voir son README).

1. **Defi de mouvements** (detection du vivant) : `face/challenge` tire au sort les mouvements et signe le defi ;
   Unity envoie des images a `face/pose` jusqu'a ce que chaque mouvement soit fait, puis une image par etape.
   Le backend revérifie : bons mouvements, meme personne sur toutes les images, defi pas expire ni deja utilise.
2. Inscription : vecteur moyen des images de face stocke dans `users.faceEmbedding` (refus si ce visage a deja un compte).
3. Connexion / deverrouillage : similarite cosinus avec le compte (ou tous les comptes) >= 0.40 -> JWT.

## Collection `users`

`firstName, lastName, email (unique), phone, passwordHash (bcrypt), emailVerified, verifyCode / resetCode
({ hash, expiresAt, attempts, sentAt }), faceEmbedding[128], faceEnrolledAt, photo ({ data, contentType }),
photoUpdatedAt, createdAt, updatedAt`
