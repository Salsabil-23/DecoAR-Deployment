const { HttpError } = require('../utils/errors');

// Client du service Python de reconnaissance faciale (dossier face-service/, OpenCV YuNet + SFace).
// Il renvoie pour chaque photo un vecteur de 128 nombres (ou une erreur : pas de visage, trop loin...).
const faceService = {
  url: () => (process.env.FACE_SERVICE_URL || 'http://127.0.0.1:5001').replace(/\/$/, ''),

  // Remplacable dans les tests (faceService.embed = ...)
  async embed(images) {
    let res;
    try {
      res = await fetch(`${faceService.url()}/embed`, {
        method: 'POST',
        headers: { 'content-type': 'application/json', 'x-api-key': process.env.FACE_SERVICE_KEY || '' },
        body: JSON.stringify({ images }),
        signal: AbortSignal.timeout(30_000),
      });
    } catch (err) {
      console.error(`[FACE] Service injoignable (${faceService.url()}) : ${err.message}`);
      throw new HttpError(503, 'Reconnaissance faciale indisponible, réessaie dans un instant', 'face');
    }
    const body = await res.json().catch(() => ({}));
    if (!res.ok) throw new HttpError(502, body.message || 'Erreur du service de reconnaissance faciale', 'face');
    return body.results;
  },
};

module.exports = faceService;
