const crypto = require('crypto');
const jwt = require('jsonwebtoken');
const faceService = require('../services/faceService');
const { HttpError } = require('./errors');

// ------------------------------------------------------------ reglages (modifiables dans .env)

// Similarite cosinus SFace pour dire "c'est la meme personne" (OpenCV recommande 0.363)
const threshold = () => Number(process.env.FACE_MATCH_THRESHOLD) || 0.4;
// Entre deux images du meme defi (tete tournee) la similarite baisse : seuil plus souple
const sameDuringChallenge = () => Number(process.env.FACE_CHALLENGE_SAME_THRESHOLD) || 0.3;
// Mouvement detecte si le yaw / pitch change d'au moins cette valeur par rapport a la pose de face
const yawDelta = () => Number(process.env.FACE_YAW_DELTA) || 0.2;
const pitchDelta = () => Number(process.env.FACE_PITCH_DELTA) || 0.12;
// Pose "de face" : yaw proche de 0
const centerMaxYaw = () => Number(process.env.FACE_CENTER_MAX_YAW) || 0.2;
const moves = () => (process.env.FACE_CHALLENGE_MOVES || 'left,right,up,down').split(',').map((m) => m.trim()).filter(Boolean);

const MOVE_LABELS = {
  left: 'tourne la tête à gauche',
  right: 'tourne la tête à droite',
  up: 'lève la tête',
  down: 'baisse la tête',
  center: 'regarde la caméra de face',
};

// ------------------------------------------------------------ outils vecteurs

function cosine(a, b) {
  let dot = 0;
  let na = 0;
  let nb = 0;
  for (let i = 0; i < a.length; i++) {
    dot += a[i] * b[i];
    na += a[i] * a[i];
    nb += b[i] * b[i];
  }
  return dot / (Math.sqrt(na) * Math.sqrt(nb) || 1);
}

function mean(vectors) {
  const m = new Array(vectors[0].length).fill(0);
  for (const v of vectors) v.forEach((x, i) => { m[i] += x; });
  const norm = Math.sqrt(m.reduce((s, x) => s + x * x, 0)) || 1;
  return m.map((x) => x / norm);
}

// Meilleure correspondance parmi des utilisateurs (avec faceEmbedding charge)
function bestMatch(embedding, users) {
  let best = null;
  for (const user of users) {
    if (!user.faceEmbedding?.length) continue;
    const score = cosine(embedding, user.faceEmbedding);
    if (!best || score > best.score) best = { user, score };
  }
  return best && best.score >= threshold() ? best : null;
}

// ------------------------------------------------------------ defi de mouvements (detection du vivant)

// Jetons de defi deja utilises (un defi ne sert qu'une fois)
const usedNonces = new Map();

function pickMoves(count) {
  const pool = [...moves()];
  const picked = [];
  while (picked.length < count && pool.length) picked.push(pool.splice(crypto.randomInt(pool.length), 1)[0]);
  return picked;
}

// Inscription : face, 2 mouvements, face ; connexion / deverrouillage : face, 1 mouvement
function issueChallenge(purpose) {
  if (purpose !== 'enroll' && purpose !== 'login') throw new HttpError(400, 'purpose : enroll ou login');
  const steps = purpose === 'enroll' ? ['center', ...pickMoves(2), 'center'] : ['center', ...pickMoves(1)];
  const challengeToken = jwt.sign({ purpose, steps, nonce: crypto.randomUUID() }, process.env.JWT_SECRET, {
    expiresIn: '3m',
    audience: 'face-challenge',
  });
  return {
    challengeToken,
    steps,
    // Memes seuils que la verification finale : l'app peut avancer des que le mouvement est fait
    yawDelta: yawDelta(),
    pitchDelta: pitchDelta(),
    centerMaxYaw: centerMaxYaw(),
  };
}

// Le mouvement de l'image i correspond-il a l'etape demandee ? (par rapport a la premiere image, de face)
function moveDone(step, base, frame) {
  const dy = frame.yaw - base.yaw;
  const dp = frame.pitch - base.pitch;
  switch (step) {
    case 'left': return dy <= -yawDelta();
    case 'right': return dy >= yawDelta();
    case 'up': return dp <= -pitchDelta();
    case 'down': return dp >= pitchDelta();
    default: return Math.abs(frame.yaw) <= centerMaxYaw();
  }
}

// Verifie un defi termine : une image par etape, dans l'ordre, toutes de la meme personne.
// Renvoie le vecteur du visage (images de face) et une photo de profil.
async function verifyChallenge(proof, purpose) {
  const fail = (message) => new HttpError(400, message, 'face');
  let claims;
  try {
    claims = jwt.verify(proof?.challengeToken || '', process.env.JWT_SECRET, { audience: 'face-challenge' });
  } catch {
    throw fail('Défi expiré, recommence');
  }
  if (claims.purpose !== purpose) throw fail('Défi invalide, recommence');

  const now = Date.now();
  for (const [nonce, exp] of usedNonces) if (exp < now) usedNonces.delete(nonce);
  if (usedNonces.has(claims.nonce)) throw fail('Défi déjà utilisé, recommence');
  usedNonces.set(claims.nonce, claims.exp * 1000);

  const frames = proof?.frames;
  if (!Array.isArray(frames) || frames.length !== claims.steps.length || frames.some((f) => typeof f !== 'string' || !f))
    throw fail('Images du défi manquantes, recommence');

  const results = await faceService.embed(frames);
  results.forEach((r, i) => {
    if (!r.ok) throw fail(`Étape ${i + 1} : ${r.error}`);
  });

  const base = results[0];
  claims.steps.forEach((step, i) => {
    if (!moveDone(step, base, results[i])) throw fail(`Mouvement non reconnu à l'étape ${i + 1} : ${MOVE_LABELS[step]}`);
    if (i > 0 && cosine(results[i].embedding, base.embedding) < sameDuringChallenge())
      throw fail('Le visage a changé pendant le défi, recommence');
  });

  const centers = results.filter((_, i) => claims.steps[i] === 'center');
  if (centers.length > 1 && cosine(centers[0].embedding, centers[1].embedding) < threshold())
    throw fail('Le visage a changé pendant le défi, recommence');

  return {
    embedding: mean(centers.map((r) => r.embedding)),
    photo: base.thumbnail ? Buffer.from(base.thumbnail, 'base64') : null,
  };
}

// Pendant le defi : orientation de la tete sur une image (retour en direct dans l'app)
async function pose(image) {
  if (typeof image !== 'string' || !image) throw new HttpError(400, 'Image manquante', 'face');
  const [result] = await faceService.embed([image]);
  if (!result?.ok) return { ok: false, error: result?.error || 'Visage non détecté' };
  return { ok: true, yaw: result.yaw, pitch: result.pitch };
}

module.exports = { issueChallenge, verifyChallenge, pose, bestMatch, cosine, threshold };
