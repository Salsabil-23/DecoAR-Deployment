"""
Service de reconnaissance faciale DecoAR (Python + OpenCV).

Le backend Node lui envoie des photos ; il renvoie pour chacune un "vecteur visage" (embedding) :
  1. YuNet (cv2.FaceDetectorYN) trouve le visage et ses 5 points (yeux, nez, bouche)
  2. SFace (cv2.FaceRecognizerSF) aligne le visage et le transforme en 128 nombres
Deux photos de la meme personne donnent des vecteurs proches (similarite cosinus elevee).
La comparaison et le stockage (MongoDB) sont faits par le backend Node.

Lancer :  python app.py   (port 5001)
"""
import base64
import os
import threading

import cv2
import numpy as np
from flask import Flask, jsonify, request

MODELS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "models")
YUNET = os.path.join(MODELS_DIR, "face_detection_yunet_2023mar.onnx")
SFACE = os.path.join(MODELS_DIR, "face_recognition_sface_2021dec.onnx")

MAX_SIDE = 640          # les photos plus grandes sont reduites (plus rapide)
MIN_FACE_PX = 70        # visage trop petit = trop loin de la camera
MIN_SCORE = 0.85        # confiance minimale de la detection
THUMB_SIZE = 256        # photo de profil renvoyee (carre centre sur le visage)
API_KEY = os.environ.get("FACE_SERVICE_KEY", "")

if not (os.path.exists(YUNET) and os.path.exists(SFACE)):
    raise SystemExit("Modeles absents : lance d'abord  python download_models.py")

detector = cv2.FaceDetectorYN.create(YUNET, "", (320, 320), MIN_SCORE, 0.3, 5000)
recognizer = cv2.FaceRecognizerSF.create(SFACE, "")
lock = threading.Lock()  # les modeles OpenCV ne sont pas partages entre threads

app = Flask(__name__)
app.config["MAX_CONTENT_LENGTH"] = 8 * 1024 * 1024


class FaceError(Exception):
    pass


def decode(b64):
    try:
        data = np.frombuffer(base64.b64decode(b64), np.uint8)
        img = cv2.imdecode(data, cv2.IMREAD_COLOR)
    except Exception:
        img = None
    if img is None:
        raise FaceError("Image illisible")
    h, w = img.shape[:2]
    scale = MAX_SIDE / max(h, w)
    if scale < 1:
        img = cv2.resize(img, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_AREA)
    return img


def thumbnail(img, face):
    """Carre centre sur le visage (avec de la marge), en JPEG base64 : sert de photo de profil."""
    x, y, w, h = face[:4]
    cx, cy, side = x + w / 2, y + h / 2, max(w, h) * 1.9
    H, W = img.shape[:2]
    side = min(side, W, H)
    x0 = int(np.clip(cx - side / 2, 0, W - side))
    y0 = int(np.clip(cy - side / 2, 0, H - side))
    crop = img[y0:y0 + int(side), x0:x0 + int(side)]
    crop = cv2.resize(crop, (THUMB_SIZE, THUMB_SIZE), interpolation=cv2.INTER_AREA)
    ok, jpg = cv2.imencode(".jpg", crop, [cv2.IMWRITE_JPEG_QUALITY, 85])
    return base64.b64encode(jpg.tobytes()).decode() if ok else None


def analyze(b64):
    img = decode(b64)
    h, w = img.shape[:2]
    with lock:
        detector.setInputSize((w, h))
        _, faces = detector.detect(img)
        if faces is None or len(faces) == 0:
            raise FaceError("Aucun visage détecté : regarde la caméra, bien éclairé")

        faces = sorted(faces, key=lambda f: f[2] * f[3], reverse=True)
        face = faces[0]
        # Un deuxieme visage presque aussi grand : on ne sait pas lequel est l'utilisateur
        if len(faces) > 1 and faces[1][2] * faces[1][3] > 0.5 * face[2] * face[3]:
            raise FaceError("Plusieurs visages détectés : une seule personne devant la caméra")
        if min(face[2], face[3]) < MIN_FACE_PX:
            raise FaceError("Visage trop petit : approche-toi de la caméra")

        aligned = recognizer.alignCrop(img, face)
        feature = recognizer.feature(aligned).flatten().astype(np.float64)

    feature /= np.linalg.norm(feature)
    yaw, pitch = head_pose(face)
    return {
        "ok": True,
        "embedding": [round(float(v), 6) for v in feature],
        "score": round(float(face[-1]), 3),
        "yaw": yaw,
        "pitch": pitch,
        "thumbnail": thumbnail(img, face),
    }


def head_pose(face):
    """
    Orientation de la tete a partir des 5 points de YuNet (yeux, nez, coins de la bouche).
      yaw   : decalage horizontal du nez par rapport au milieu des yeux, divise par l'ecart des yeux.
              ~0 de face ; negatif = tete tournee vers la GAUCHE de l'image, positif = vers la droite.
      pitch : position verticale du nez entre la ligne des yeux (0) et celle de la bouche (1).
              ~0.5 de face ; diminue quand on leve la tete, augmente quand on la baisse.
    Les images recues sont "en miroir" (comme l'apercu) : gauche de l'image = gauche de l'utilisateur.
    """
    right_eye, left_eye = face[4:6], face[6:8]
    nose = face[8:10]
    mouth = (face[10:12] + face[12:14]) / 2
    eyes = (right_eye + left_eye) / 2
    eye_dist = max(float(np.linalg.norm(left_eye - right_eye)), 1e-3)
    yaw = (nose[0] - eyes[0]) / eye_dist
    pitch = (nose[1] - eyes[1]) / max(float(mouth[1] - eyes[1]), 1e-3)
    return round(float(yaw), 3), round(float(pitch), 3)


@app.before_request
def check_key():
    # En ligne, seul le backend Node (qui connait la cle) peut appeler le service
    if API_KEY and request.path != "/health" and request.headers.get("X-Api-Key") != API_KEY:
        return jsonify({"message": "Clé du service invalide"}), 401


@app.get("/health")
def health():
    return jsonify({"status": "ok", "opencv": cv2.__version__})


@app.post("/embed")
def embed():
    images = (request.get_json(silent=True) or {}).get("images")
    if not isinstance(images, list) or not 1 <= len(images) <= 5:
        return jsonify({"message": "images : liste de 1 a 5 photos en base64"}), 400

    results = []
    for b64 in images:
        try:
            results.append(analyze(b64 if isinstance(b64, str) else ""))
        except FaceError as e:
            results.append({"ok": False, "error": str(e)})
    return jsonify({"results": results})


if __name__ == "__main__":
    port = int(os.environ.get("PORT", 5001))
    print(f"[FACE] OpenCV {cv2.__version__} - service visage sur http://localhost:{port}")
    app.run(host="127.0.0.1", port=port)
