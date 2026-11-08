const express = require('express');
const { Readable } = require('stream');

// Passerelle vers le module M6 (generation de modeles 3D, service Python Backend_M6) :
// Unity n'appelle que decoar-api, qui transmet tel quel a decoar-m6.
//   /api/m6/generate, /api/m6/status/:id, /api/m6/model/:id, /api/m6/test-model, /api/m6/health
const router = express.Router();

const m6Url = () => (process.env.M6_SERVICE_URL || 'http://127.0.0.1:5000').replace(/\/$/, '');

// Generation TRELLIS : la reponse peut etre longue (envoi de l'image, modele .glb de plusieurs Mo)
const TIMEOUT_MS = 120_000;

async function forward(req, res, path) {
  const headers = {};
  for (const h of ['content-type', 'content-length']) if (req.headers[h]) headers[h] = req.headers[h];

  let upstream;
  try {
    upstream = await fetch(`${m6Url()}${path}`, {
      method: req.method,
      headers,
      // L'image (multipart) est retransmise en flux, sans la lire en memoire
      body: req.method === 'GET' || req.method === 'HEAD' ? undefined : req,
      duplex: 'half',
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });
  } catch (err) {
    console.error(`[M6] Service injoignable (${m6Url()}) : ${err.message}`);
    return res.status(503).json({ error: 'Service 3D indisponible, réessaie dans un instant' });
  }

  res.status(upstream.status);
  // Pas de content-length : fetch decompresse les reponses gzip de Render, la taille d'origine serait fausse
  for (const h of ['content-type', 'content-disposition']) {
    const value = upstream.headers.get(h);
    if (value) res.setHeader(h, value);
  }
  if (!upstream.body) return res.end();
  Readable.fromWeb(upstream.body).pipe(res);
}

const id = (req) => encodeURIComponent(req.params.jobId);

router.get('/health', (req, res) => forward(req, res, '/health'));
router.post('/generate', (req, res) => forward(req, res, '/generate'));
router.get('/status/:jobId', (req, res) => forward(req, res, `/status/${id(req)}`));
router.get('/model/:jobId', (req, res) => forward(req, res, `/model/${id(req)}`));
router.get('/test-model', (req, res) => forward(req, res, '/test-model'));

module.exports = { router, m6Url };
