# DecoAR - Service de reconnaissance faciale (Python + OpenCV)

Appele uniquement par le backend Node (`Backend/`). Pour chaque photo, il renvoie un **vecteur visage**
(128 nombres) ; le backend le compare a ceux stockes dans MongoDB.

1. **Detection** : YuNet (`cv2.FaceDetectorYN`) trouve le visage et 5 points (yeux, nez, coins de la bouche).
   Refuse : aucun visage, plusieurs visages, visage trop petit (trop loin).
2. **Reconnaissance** : SFace (`cv2.FaceRecognizerSF`) aligne le visage sur les yeux puis le transforme en vecteur.
   **Orientation de la tete** (pour le defi) : a partir des 5 points, `yaw` = decalage du nez par rapport au milieu
   des yeux (0 de face, negatif = tete tournee a gauche), `pitch` = position du nez entre les yeux et la bouche
   (~0.55 de face, diminue tete levee, augmente tete baissee).
3. **Comparaison** (dans Node, `Backend/src/utils/face.js`) : similarite cosinus. Meme personne ~0.6 a 0.95,
   personnes differentes ~0.0 a 0.3 ; seuil `FACE_MATCH_THRESHOLD` = 0.40 (OpenCV conseille 0.363).

## Detection du vivant : defi de mouvements

Le serveur tire au sort des mouvements (gauche, droite, haut, bas) et signe le defi (jeton valable 3 min, une seule fois) :
inscription = face, 2 mouvements, face ; connexion / deverrouillage = face, 1 mouvement. L'app envoie une image
toutes les ~0,35 s et avance des que le mouvement est fait ; a la fin, le backend revérifie chaque image (bon mouvement,
meme personne) avant d'inscrire ou de connecter. Une photo imprimee ou un ecran fixe ne peut pas tourner la tete,
et une video enregistree ne connait pas l'ordre tire au sort.

Les photos ne sont pas gardees : seul le vecteur moyen des images de face est stocke (`users.faceEmbedding`),
plus une miniature recadree utilisee comme photo de profil.

## Lancer (Windows)

Dans `face-service/`, une seule fois :

```bash
py -3.13 -m venv .venv
```

```bash
.venv\Scripts\python -m pip install -r requirements.txt
```

```bash
.venv\Scripts\python download_models.py
```

Puis a chaque fois (laisser le terminal ouvert, en plus de `npm run dev` pour le backend) :

```bash
.venv\Scripts\python app.py
```

-> `[FACE] OpenCV 4.x - service visage sur http://localhost:5001`. Test : http://localhost:5001/health

## Route

`POST /embed` `{ images: ["<jpeg base64>", ...] }` (1 a 5) ->
`{ results: [{ ok, embedding[128], score, yaw, pitch, thumbnail } | { ok: false, error }] }`

En ligne, `FACE_SERVICE_KEY` (meme valeur dans les deux services) protege la route : en-tete `X-Api-Key`.

## Limites (a dire a la soutenance)

- Le defi arrete les photos et les videos, pas un masque 3D realiste (il faudrait un capteur de profondeur).
- Les seuils de mouvement se reglent dans `Backend/.env` (`FACE_YAW_DELTA`, `FACE_PITCH_DELTA`) ; si "lever / baisser
  la tete" est difficile a detecter, garder seulement `FACE_CHALLENGE_MOVES=left,right`.
- La lumiere compte : visage bien eclaire, de face, sans contre-jour.
