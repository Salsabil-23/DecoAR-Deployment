const express = require('express');
const rateLimit = require('express-rate-limit');
const User = require('../models/User');
const { HttpError } = require('../utils/errors');
const { requireAuth } = require('../middleware/auth');
const { signAdminToken, ADMIN_TTL_SECONDS } = require('../utils/tokens');
const face = require('../utils/face');

const router = express.Router();

const limiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 20,
  skip: () => process.env.NODE_ENV === 'test',
  handler: (req, res) => res.status(429).json({ message: 'Trop de tentatives, réessaie plus tard', field: 'face' }),
});

// POST /api/admin/confirm  { face: { challengeToken, frames } }
// Ouvre l'espace admin : compte admin + defi de mouvements avec le visage de CE compte.
// Renvoie un jeton admin (30 min) a envoyer dans l'en-tete X-Admin-Token des routes admin.
router.post('/confirm', requireAuth, limiter, async (req, res) => {
  const user = await User.findById(req.userId).select('+faceEmbedding');
  if (!user) throw new HttpError(401, 'Compte introuvable');
  if (user.role !== 'admin') throw new HttpError(403, 'Accès réservé aux administrateurs', 'admin');
  if (!user.faceEmbedding?.length)
    throw new HttpError(400, 'Enregistre d\'abord ton visage (Profil > Mon visage)', 'face');

  const { embedding } = await face.verifyChallenge(req.body?.face, 'login');
  if (!face.bestMatch(embedding, [user])) throw new HttpError(401, 'Visage non reconnu', 'face');

  console.log(`[ADMIN] Espace admin ouvert par ${user.email}`);
  res.json({ adminToken: signAdminToken(user), expiresIn: ADMIN_TTL_SECONDS, user: user.toPublic() });
});

module.exports = router;
