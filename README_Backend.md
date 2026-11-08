# DecoAR - Projet Unity & Services Backend

Application mobile Unity de réalité augmentée (**DecoAR**) accompagnée de ses services backend prêts pour le déploiement sur **Render** via `render.yaml`.

## Architecture du projet

```text
DecoAR-Deployment/
├── Assets/                  # Projet Unity (Auth, Core, M1_Player, M4_AR...)
├── Packages/                # Packages Unity
├── ProjectSettings/         # Configuration du projet Unity
├── Backend/                 # Service 1 : API principale (Node.js + Express + MongoDB) - Port 3000
├── face-service/            # Service 2 : Reconnaissance faciale (Python + Flask + OpenCV) - Port 5001
├── Backend_M6/              # Service 3 : Génération 3D Image-to-GLB (Python + Flask + TRELLIS) - Port 5000
├── render.yaml              # Configuration de déploiement Render (Blueprint des 3 services)
└── README.md                # Documentation générale
```

## Rôle de chaque service backend

| Service | Dossier | Technologie | Port local | Rôle |
| --- | --- | --- | --- | --- |
| **`decoar-api`** | `Backend/` | Node.js (>= 20), Express 5, Mongoose | `3000` | Inscription, connexion (mot de passe / Face ID), vérification e-mail, profil utilisateur |
| **`decoar-face`** | `face-service/` | Python 3.13, Flask, OpenCV (YuNet + SFace) | `5001` | Détection de visage, défi de mouvements (liveness) et extraction d'embeddings 128D |
| **`decoar-m6`** | `Backend_M6/` | Python 3.12/3.13, Flask, Gradio Client, HF Hub | `5000` | Module M6 : génération asynchrone de modèles 3D `.glb` à partir d'images JPG/PNG via Hugging Face `trellis-community/TRELLIS` |

Aucun conflit de port n'existe entre les trois services (`3000`, `5001`, `5000`).

---

## Prérequis

- **Node.js** >= 20 et `npm`
- **Python** 3.12 ou 3.13 et `pip`
- **MongoDB** (local sur `mongodb://127.0.0.1:27017/decoar` ou cluster MongoDB Atlas)
- **Token Hugging Face** (`HF_TOKEN`) pour la génération 3D via `trellis-community/TRELLIS`

---

##Variables d'environnement

### 1. `Backend/.env` (copier depuis `Backend/.env.example`)
- `PORT=3000`
- `MONGODB_URI` : URI MongoDB locale ou Atlas
- `JWT_SECRET` : clé secrète pour signer les jetons JWT
- `FACE_SERVICE_URL=http://127.0.0.1:5001`
- `FACE_SERVICE_KEY` : clé partagée avec `face-service` (optionnelle en local, requise en ligne)
- `BREVO_API_KEY` / `MAIL_FROM` (en ligne) ou `SMTP_USER` / `SMTP_PASS` (en local)

### 2. `face-service`
- `PORT=5001` (par défaut)
- `FACE_SERVICE_KEY` : même valeur que dans `Backend/.env`

### 3. `Backend_M6/.env` (copier depuis `Backend_M6/.env.example`)
- `PORT=5000` (par défaut)
- `HF_TOKEN` : token Hugging Face (`hf_...`)

---

## Installation et lancement en local

### 1. Service `Backend` (Port 3000)

```bash
cd Backend
npm install
cp .env.example .env
# Renseigner MONGODB_URI et JWT_SECRET dans .env
npm run dev
```

Test : `http://localhost:3000/api/health`

### 2. Service `face-service` (Port 5001)

```bash
cd face-service
python -m venv .venv
.venv\Scripts\python -m pip install -r requirements.txt
.venv\Scripts\python download_models.py
.venv\Scripts\python app.py
```

Test : `http://localhost:5001/health`

### 3. Service `Backend_M6` (Port 5000)

```bash
cd Backend_M6
python -m venv .venv
.venv\Scripts\python -m pip install -r requirements.txt
copy .env.example .env
# Renseigner HF_TOKEN dans Backend_M6/.env
.venv\Scripts\python app.py
```

Test : `http://localhost:5000/health`

---

## Endpoints disponibles

### `Backend` (`http://localhost:3000`)
- `GET /api/health` : état de santé de l'API (réveille aussi `face-service`)
- `POST /api/auth/register` : inscription + vérification du défi facial
- `POST /api/auth/login` : connexion par e-mail et mot de passe
- `POST /api/auth/verify-email` / `POST /api/auth/verify-email/resend` : validation de l'e-mail
- `POST /api/auth/forgot-password` / `POST /api/auth/reset-password` : réinitialisation du mot de passe
- `POST /api/auth/face/challenge` / `POST /api/auth/face/pose` / `POST /api/auth/face/login` : authentification Face ID avec détection du vivant
- `GET /api/users/me` / `PATCH /api/users/me` / `POST /api/users/me/password` : gestion du profil
- `PUT /api/users/me/photo` / `GET /api/users/me/photo` / `DELETE /api/users/me/photo` : photo de profil
- `PUT /api/users/me/face` : réenregistrement du visage

### `face-service` (`http://localhost:5001`)
- `GET /health` : état du service OpenCV (`{ "status": "ok", "opencv": "..." }`)
- `POST /embed` : analyse 1 à 5 images base64 et retourne l'embedding 128D + orientation (`yaw`, `pitch`) + miniature

### `Backend_M6` (`http://localhost:5000`)
- `GET /health` : état du service M6 (`{ "mode": "trellis", "status": "ok" }`)
- `POST /generate` : envoi d'une image (`multipart/form-data`, champ `image` en `.jpg`/`.jpeg`/`.png`, max 6 Mo) -> `202 { "job_id": "...", "status": "processing" }`
- `GET /status/<job_id>` : état d'avancement (`processing`, `completed`, `failed` + message d'erreur éventuel)
- `GET /model/<job_id>` : téléchargement du modèle 3D généré au format `.glb` (`Content-Type: model/gltf-binary`)
- `GET /test-model` : téléchargement immédiat d'un modèle `.glb` de test pour Unity

---

## Procédure de déploiement (Render Blueprint)

Le fichier `render.yaml` à la racine déploie simultanément les 3 services sur [Render](https://render.com) :

1. Sur Render : **New > Blueprint** et sélectionner le dépôt Git.
2. Render détecte `render.yaml` et prépare les 3 services web :
   - **`decoar-api`** (`rootDir: Backend`)
   - **`decoar-face`** (`rootDir: face-service`)
   - **`decoar-m6`** (`rootDir: Backend_M6`)
3. Renseigner les variables d'environnement demandées (`sync: false`, jamais stockées dans Git) :
   - Pour `decoar-api` : `MONGODB_URI`, `BREVO_API_KEY`, `MAIL_FROM`, `FACE_SERVICE_URL`, `FACE_SERVICE_KEY`
   - Pour `decoar-face` : `FACE_SERVICE_KEY`
   - Pour `decoar-m6` : `HF_TOKEN`
