"""Telecharge les modeles OpenCV (opencv_zoo) utilises par le service : a lancer une fois."""
import os
import urllib.request

MODELS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "models")
BASE = "https://github.com/opencv/opencv_zoo/raw/main/models"
MODELS = {
    # Detection de visage (YuNet, ~230 Ko)
    "face_detection_yunet_2023mar.onnx": f"{BASE}/face_detection_yunet/face_detection_yunet_2023mar.onnx",
    # Reconnaissance de visage (SFace, ~37 Mo) : visage -> vecteur de 128 nombres
    "face_recognition_sface_2021dec.onnx": f"{BASE}/face_recognition_sface/face_recognition_sface_2021dec.onnx",
}


def main():
    os.makedirs(MODELS_DIR, exist_ok=True)
    for name, url in MODELS.items():
        path = os.path.join(MODELS_DIR, name)
        if os.path.exists(path) and os.path.getsize(path) > 100_000:
            print(f"[modeles] {name} deja present")
            continue
        print(f"[modeles] telechargement de {name}...")
        urllib.request.urlretrieve(url, path)
        print(f"[modeles] {name} : {os.path.getsize(path) // 1024} Ko")


if __name__ == "__main__":
    main()
