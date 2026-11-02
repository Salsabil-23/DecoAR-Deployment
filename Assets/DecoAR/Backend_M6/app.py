
import logging
import threading
import uuid
from pathlib import Path

from flask import Flask, jsonify, request, send_file
from werkzeug.utils import secure_filename
from trellis_service import generate_3d

BASE_DIR = Path(__file__).resolve().parent
UPLOAD_DIR = BASE_DIR / "uploads"
MODEL_DIR = BASE_DIR / "generated_models"

for folder in (UPLOAD_DIR, MODEL_DIR):
    folder.mkdir(parents=True, exist_ok=True)

app = Flask(__name__)
app.config["MAX_CONTENT_LENGTH"] = 6 * 1024 * 1024

jobs = {}
lock = threading.Lock()


def update_job(job_id, **fields):
    with lock:
        jobs[job_id].update(fields)


def worker(job_id, image_path, model_path):
    try:
        generate_3d(image_path, model_path)
        update_job(job_id, status="completed")

    except Exception as exc:
        logging.exception(
            "[M6] Échec génération %s", job_id
        )

        message = str(exc)

        if "quota" in message.lower():
            message = (
                "Quota Hugging Face ZeroGPU dépassé. "
                "Réessayer après réinitialisation."
            )

        update_job(
            job_id,
            status="failed",
            error=message[:500]
        )


@app.get("/health")
def health():
    return jsonify(status="ok", mode="trellis")


@app.post("/generate")
def generate():
    uploaded = request.files.get("image")

    if uploaded is None or not uploaded.filename:
        return jsonify(
            error="Champ multipart 'image' manquant"
        ), 400

    ext = Path(
        secure_filename(uploaded.filename)
    ).suffix.lower()

    if ext not in (".jpg", ".jpeg", ".png"):
        return jsonify(
            error="Image JPG ou PNG requise"
        ), 400

    job_id = uuid.uuid4().hex

    image_path = UPLOAD_DIR / f"{job_id}{ext}"
    model_path = MODEL_DIR / f"{job_id}.glb"

    uploaded.save(image_path)

    with lock:
        jobs[job_id] = {
            "status": "processing",
            "error": ""
        }

    threading.Thread(
        target=worker,
        args=(job_id, image_path, model_path),
        daemon=True
    ).start()

    print(
        "[M6] Début génération :", job_id,
        flush=True
    )

    return jsonify(
        job_id=job_id,
        status="processing"
    ), 202


@app.get("/status/<job_id>")
def status(job_id):
    with lock:
        job = jobs.get(job_id)

        if job is None:
            return jsonify(
                error="Tâche introuvable"
            ), 404

        return jsonify(job_id=job_id, **job)


@app.get("/model/<job_id>")
def model(job_id):
    with lock:
        job = jobs.get(job_id)

        if job is None:
            return jsonify(
                error="Tâche introuvable"
            ), 404

        if job["status"] != "completed":
            return jsonify(
                error="Modèle non prêt"
            ), 409

    return send_file(
        MODEL_DIR / f"{job_id}.glb",
        mimetype="model/gltf-binary"
    )


@app.get("/test-model")
def test_model():
    path = BASE_DIR / "test.glb"

    if not path.is_file():
        return jsonify(
            error="Déposer test.glb dans Backend_M6"
        ), 404

    return send_file(
        path,
        mimetype="model/gltf-binary"
    )


if __name__ == "__main__":
    print(
        "[M6] Backend : http://0.0.0.0:5000",
        flush=True
    )

    app.run(
        host="0.0.0.0",
        port=5000,
        debug=False,
        threaded=True
    )
