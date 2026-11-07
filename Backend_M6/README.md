# DecoAR - Backend Module M6 (Python + Flask + Hugging Face TRELLIS)

Service de génération de modèles 3D (`.glb`) à partir d'une image 2D (`.jpg` / `.png`) via l'espace Hugging Face `trellis-community/TRELLIS`.

## Prérequis

- Python 3.12 ou 3.13
- Un compte Hugging Face avec un token d'accès (`HF_TOKEN`) : [huggingface.co/settings/tokens](https://huggingface.co/settings/tokens)

## Lancer (Windows)

Dans `Backend_M6/`, une seule fois :

```bash
python -m venv .venv
```

```bash
.venv\Scripts\python -m pip install -r requirements.txt
```

```bash
copy .env.example .env
```

Puis renseigner `HF_TOKEN=hf_...` dans `Backend_M6/.env`.

Démarrer le serveur :

```bash
.venv\Scripts\python app.py
```

-> `[M6] Backend : http://0.0.0.0:5000`. Test rapide : `http://localhost:5000/health`

## Variables d'environnement

| Variable | Description | Défaut |
| --- | --- | --- |
| `PORT` | Port d'écoute du serveur Flask | `5000` |
| `HF_TOKEN` | Token Hugging Face pour appeler l'espace `trellis-community/TRELLIS` | *(requis)* |

## Routes

| Méthode | Route | Corps / Paramètres | Réponse |
| --- | --- | --- | --- |
| `GET` | `/health` | - | `200 { "mode": "trellis", "status": "ok" }` |
| `POST` | `/generate` | `multipart/form-data` champ `image` (`.jpg`, `.jpeg`, `.png`, max 6 Mo) | `202 { "job_id": "<id>", "status": "processing" }` |
| `GET` | `/status/<job_id>` | `job_id` dans l'URL | `200 { "job_id": "<id>", "status": "processing" \| "completed" \| "failed", "error": "" }` ou `404` |
| `GET` | `/model/<job_id>` | `job_id` dans l'URL | `200` fichier binaire `.glb` (`model/gltf-binary`), `409` si non prêt, `404` si introuvable |
| `GET` | `/test-model` | - | `200` fichier binaire `test.glb` (`model/gltf-binary`) pour tester le chargement dans Unity sans consommer le quota ZeroGPU |

## Déploiement (Render Blueprint)

Le fichier `render.yaml` à la racine du dépôt configure automatiquement le service `decoar-m6` à côté de `decoar-api` et `decoar-face` :
- **Root Directory** : `Backend_M6`
- **Build Command** : `pip install -r requirements.txt`
- **Start Command** : `gunicorn -w 1 --threads 4 --timeout 120 -b 0.0.0.0:$PORT app:app`
- **Health Check Path** : `/health`
- **Variable secrète à renseigner sur Render** : `HF_TOKEN`
