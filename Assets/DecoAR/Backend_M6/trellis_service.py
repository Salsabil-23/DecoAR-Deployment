
import os
import shutil
from pathlib import Path

from dotenv import load_dotenv
from gradio_client import Client, handle_file
from huggingface_hub import HfApi

BASE_DIR = Path(__file__).resolve().parent
SPACE = "trellis-community/TRELLIS"


def generate_3d(image_path, output_path):
    image_path = Path(image_path).resolve()
    output_path = Path(output_path).resolve()

    if not image_path.is_file():
        raise FileNotFoundError(
            f"Image introuvable : {image_path}"
        )

    load_dotenv(BASE_DIR / ".env", override=True)

    token = os.getenv("HF_TOKEN")

    if not token:
        raise RuntimeError(
            "HF_TOKEN manquant dans .env"
        )

    account = HfApi(token=token).whoami()["name"]

    print(
        "[TRELLIS] Compte :", account,
        flush=True
    )

    client = Client(
        SPACE,
        token=token,
        httpx_kwargs={"timeout": 600.0}
    )

    print(
        "[TRELLIS] Initialisation de session",
        flush=True
    )

    client.predict(api_name="/start_session")

    print("[TRELLIS] Prétraitement", flush=True)

    processed = client.predict(
        image=handle_file(str(image_path)),
        api_name="/preprocess_image"
    )

    if isinstance(processed, dict):
        processed = processed.get("path")

    if not processed or not Path(processed).is_file():
        raise RuntimeError(
            f"Image prétraitée introuvable : {processed}"
        )

    print(
        "[TRELLIS] Génération du GLB",
        flush=True
    )

    result = client.predict(
        image=handle_file(str(processed)),
        multiimages=[],
        seed=0,
        ss_guidance_strength=7.5,
        ss_sampling_steps=12,
        slat_guidance_strength=3.0,
        slat_sampling_steps=12,
        multiimage_algo="stochastic",
        mesh_simplify=0.95,
        texture_size=1024,
        api_name="/generate_and_extract_glb"
    )

    if not isinstance(result, (list, tuple)) or len(result) < 3:
        raise RuntimeError(
            "Réponse TRELLIS inattendue"
        )

    source = result[2]

    if isinstance(source, dict):
        source = source.get("path")

    if not source or not Path(source).is_file():
        raise FileNotFoundError(
            f"GLB TRELLIS introuvable : {source}"
        )

    output_path.parent.mkdir(
        parents=True,
        exist_ok=True
    )

    shutil.copyfile(source, output_path)

    with output_path.open("rb") as f:
        if f.read(4) != b"glTF":
            output_path.unlink(missing_ok=True)
            raise RuntimeError(
                "Fichier GLB invalide"
            )

    print(
        "[TRELLIS] Modèle créé :",
        output_path,
        flush=True
    )

    return str(output_path)
