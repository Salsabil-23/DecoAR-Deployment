const express = require('express');
const bcrypt = require('bcryptjs');
const User = require('../models/User');
const { HttpError } = require('../utils/errors');
const { requireAuth } = require('../middleware/auth');
const v = require('../utils/validation');
const face = require('../utils/face');

const router = express.Router();
const MAX_PHOTO_BYTES = 300 * 1024;

async function currentUser(req, select = '') {
  const user = await User.findById(req.userId).select(select);
  if (!user) throw new HttpError(401, 'Compte introuvable');
  return user;
}

// GET /api/users/me : profil (sert aussi a verifier le jeton pour la connexion automatique)
router.get('/me', requireAuth, async (req, res) => {
  res.json({ user: (await currentUser(req)).toPublic() });
});

// PATCH /api/users/me  { firstName?, lastName?, phone? }
router.patch('/me', requireAuth, async (req, res) => {
  const body = req.body || {};
  const user = await currentUser(req);
  if (body.firstName !== undefined) user.firstName = v.name(body.firstName, 'firstName', 'prénom');
  if (body.lastName !== undefined) user.lastName = v.name(body.lastName, 'lastName', 'nom');
  if (body.phone !== undefined) user.phone = v.phone(body.phone);
  await user.save();
  res.json({ user: user.toPublic() });
});

// POST /api/users/me/password  { currentPassword, newPassword }
router.post('/me/password', requireAuth, async (req, res) => {
  const user = await currentUser(req, '+passwordHash');
  if (!(await bcrypt.compare(v.raw(req.body?.currentPassword), user.passwordHash)))
    throw new HttpError(400, 'Mot de passe actuel incorrect', 'currentPassword');
  const next = v.password(req.body?.newPassword, 'newPassword');
  user.passwordHash = await bcrypt.hash(next, 10);
  await user.save();
  res.json({ message: 'Mot de passe modifié' });
});

// PUT /api/users/me/photo  { image: "<jpeg ou png en base64>" }
router.put('/me/photo', requireAuth, async (req, res) => {
  const data = Buffer.from(v.str(req.body?.image), 'base64');
  const isJpeg = data[0] === 0xff && data[1] === 0xd8;
  const isPng = data[0] === 0x89 && data[1] === 0x50;
  if (!isJpeg && !isPng) throw new HttpError(400, 'Image invalide (JPEG ou PNG attendu)', 'image');
  if (data.length > MAX_PHOTO_BYTES) throw new HttpError(413, 'Photo trop lourde (300 Ko maximum)', 'image');

  const user = await currentUser(req);
  user.photo = { data, contentType: isJpeg ? 'image/jpeg' : 'image/png' };
  user.photoUpdatedAt = new Date();
  await user.save();
  res.json({ user: user.toPublic() });
});

// GET /api/users/me/photo  -> l'image elle-meme
router.get('/me/photo', requireAuth, async (req, res) => {
  const user = await currentUser(req, '+photo');
  if (!user.photo?.data) throw new HttpError(404, 'Pas de photo de profil');
  res.set('Cache-Control', 'no-store').type(user.photo.contentType).send(user.photo.data);
});

// DELETE /api/users/me/photo
router.delete('/me/photo', requireAuth, async (req, res) => {
  const user = await currentUser(req);
  user.photo = undefined;
  user.photoUpdatedAt = undefined;
  await user.save();
  res.json({ user: user.toPublic() });
});

// PUT /api/users/me/face  { face: { challengeToken, frames } } : (re)enregistrer son visage
router.put('/me/face', requireAuth, async (req, res) => {
  const enrolled = await face.verifyChallenge(req.body?.face, 'enroll');
  const others = await User.find({ _id: { $ne: req.userId }, faceEmbedding: { $exists: true } }).select('+faceEmbedding');
  if (face.bestMatch(enrolled.embedding, others))
    throw new HttpError(409, 'Ce visage est déjà associé à un autre compte', 'face');

  const user = await currentUser(req);
  user.faceEmbedding = enrolled.embedding;
  user.faceEnrolledAt = new Date();
  await user.save();
  res.json({ user: user.toPublic() });
});

module.exports = router;
