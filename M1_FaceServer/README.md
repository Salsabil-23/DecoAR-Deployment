# M1_FaceServer – tête 3D FLAME à partir de 3 photos

Le téléphone prend 3 photos du visage (face, gauche, droite) et les envoie à ce serveur, qui renvoie la tête 3D de la personne :
- **MediaPipe** repère 478 points du visage sur chaque photo ;
- **FLAME** (tête complète : crâne, oreilles, cou, yeux) est déformé pour correspondre à ces points sur les 3 angles ;
- les photos sont plaquées sur la tête ; les zones jamais vues prennent la couleur de la peau ou des cheveux.

## Installation (une fois)

1. Python 3.11, puis dans ce dossier :
   ```
   py -3.11 -m venv .venv
   .venv\Scripts\python.exe -m pip install torch --index-url https://download.pytorch.org/whl/cpu
   .venv\Scripts\python.exe -m pip install -r requirements.txt
   ```
2. `face_landmarker.task` (MediaPipe) dans ce dossier.
3. Fichiers FLAME dans `flame/` (compte gratuit sur https://flame.is.tue.mpg.de, licence non commerciale, **ne pas les commiter**) :
   - `flame2023_Open.pkl` (archive « FLAME 2023 Open », CC-BY-4.0) ;
   - `FLAME_masks.pkl` (archive « FLAME Vertex Masks ») ;
   - facultatif : `head_template_mesh.obj` (archive « FLAME texture space », 1,2 Go) ; sans lui, le serveur génère ses propres UV ;
   - `mediapipe_landmark_embedding.npz` (archive « FLAME Mediapipe Landmark Embedding »).

## Utilisation

- Test sur PC : `.venv\Scripts\python.exe test_local.py face.jpg gauche.jpg droite.jpg` → `sortie/tete.obj`.
- Serveur : `.venv\Scripts\python.exe server.py`, puis le téléphone sur le même Wi-Fi appelle `http://<ip-du-pc>:8000/head`.
