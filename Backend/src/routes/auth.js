const express = require('express');
const bcrypt = require('bcryptjs');
const rateLimit = require('express-rate-limit');
const User = require('../models/User');
const { HttpError } = require('../utils/errors');
const { requireAuth } = require('../middleware/auth');
const { signJwt } = require('../utils/tokens');
const face = require('../utils/face');
const { issueCode, consumeCode } = require('../utils/codes');
const { sendVerificationCode, sendResetCode, trySend } = require('../services/emails');
const v = require('../utils/validation');

const router = express.Router();

// Limite les essais de mot de passe / jeton / code (desactive pendant les tests)
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 30,
  skip: () => process.env.NODE_ENV === 'test',
  handler: (req, res) => res.status(429).json({ message: 'Trop de tentatives, réessaie plus tard', field: '' }),
});

function session(user) {
  return { token: signJwt(user), user: user.toPublic() };
}

async function currentUser(req, select = '') {
  const user = await User.findById(req.userId).select(select);
  if (!user) throw new HttpError(401, 'Compte introuvable');
  return user;
}

// ------------------------------------------------------------ inscription / connexion

// POST /api/auth/register  { ...infos, password, face: { challengeToken, frames } }
// -> verifie le defi de mouvements, enregistre le visage et envoie le code de verification par e-mail
router.post('/register', limiter, async (req, res) => {
  const body = req.body || {};
  const data = {
    firstName: v.name(body.firstName, 'firstName', 'prénom'),
    lastName: v.name(body.lastName, 'lastName', 'nom'),
    email: v.email(body.email),
    phone: v.phone(body.phone),
  };
  const password = v.password(body.password);
  if (await User.exists({ email: data.email }))
    throw new HttpError(409, 'Un compte existe déjà avec cet e-mail', 'email');

  // Visage obligatoire, capture pendant un defi de mouvements (preuve que la personne est vivante)
  const enrolled = await face.verifyChallenge(body.face, 'enroll');
  const others = await User.find({ faceEmbedding: { $exists: true } }).select('+faceEmbedding');
  if (face.bestMatch(enrolled.embedding, others))
    throw new HttpError(409, 'Ce visage est déjà associé à un autre compte', 'face');

  const user = new User({
    ...data,
    passwordHash: await bcrypt.hash(password, 10),
    faceEmbedding: enrolled.embedding,
    faceEnrolledAt: new Date(),
  });
  if (enrolled.photo) {
    user.photo = { data: enrolled.photo, contentType: 'image/jpeg' };
    user.photoUpdatedAt = new Date();
  }
  const code = issueCode(user, 'verifyCode');
  await user.save();
  trySend(sendVerificationCode, user, code); // en arriere-plan : la reponse n'attend pas l'e-mail
  res.status(201).json(session(user));
});

// POST /api/auth/login  (e-mail + mot de passe)
router.post('/login', limiter, async (req, res) => {
  const email = v.str(req.body?.email).toLowerCase();
  const password = v.raw(req.body?.password);
  const user = await User.findOne({ email }).select('+passwordHash');
  if (!user || !(await bcrypt.compare(password, user.passwordHash)))
    throw new HttpError(401, 'E-mail ou mot de passe incorrect');
  res.json(session(user));
});

// ------------------------------------------------------------ verification de l'e-mail

// POST /api/auth/verify-email  { code }
router.post('/verify-email', requireAuth, limiter, async (req, res) => {
  const user = await currentUser(req, '+verifyCode');
  if (!user.emailVerified) {
    try {
      consumeCode(user, 'verifyCode', req.body?.code);
    } finally {
      await user.save(); // enregistre aussi le compteur d'essais en cas d'echec
    }
    user.emailVerified = true;
    await user.save();
  }
  res.json({ user: user.toPublic() });
});

// POST /api/auth/verify-email/resend
router.post('/verify-email/resend', requireAuth, limiter, async (req, res) => {
  const user = await currentUser(req, '+verifyCode');
  if (user.emailVerified) return res.json({ message: 'E-mail déjà vérifié' });
  const code = issueCode(user, 'verifyCode');
  if (!code) throw new HttpError(429, 'Attends une minute avant de redemander un code');
  await user.save();
  if (!(await trySend(sendVerificationCode, user, code)))
    throw new HttpError(502, 'Impossible d\'envoyer l\'e-mail, réessaie plus tard');
  res.json({ message: 'Nouveau code envoyé' });
});

// ------------------------------------------------------------ mot de passe oublie

// POST /api/auth/forgot-password  { email }
// Repond toujours pareil : on ne revele pas si un compte existe pour cet e-mail
router.post('/forgot-password', limiter, async (req, res) => {
  const email = v.email(req.body?.email);
  const user = await User.findOne({ email }).select('+resetCode');
  if (user) {
    const code = issueCode(user, 'resetCode');
    if (code) {
      await user.save();
      trySend(sendResetCode, user, code); // en arriere-plan
    }
  }
  res.json({ message: 'Si un compte existe pour cet e-mail, un code vient d\'être envoyé' });
});

// POST /api/auth/reset-password  { email, code, password }  -> connecte directement
router.post('/reset-password', limiter, async (req, res) => {
  const email = v.email(req.body?.email);
  const password = v.password(req.body?.password);
  const user = await User.findOne({ email }).select('+resetCode');
  if (!user) throw new HttpError(400, 'Code incorrect', 'code');
  try {
    consumeCode(user, 'resetCode', req.body?.code);
  } finally {
    await user.save();
  }
  user.passwordHash = await bcrypt.hash(password, 10);
  user.emailVerified = true; // le code recu par e-mail prouve que l'adresse est a lui
  await user.save();
  res.json(session(user));
});

// ------------------------------------------------------------ reconnaissance faciale

// Limite large : pendant un defi, l'app envoie une image toutes les ~0,4 s
const poseLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 900,
  skip: () => process.env.NODE_ENV === 'test',
  handler: (req, res) => res.status(429).json({ message: 'Trop de tentatives, réessaie plus tard', field: 'face' }),
});

// POST /api/auth/face/challenge  { purpose: "enroll" | "login" }
// -> mouvements tires au hasard ("center", "left", "right", "up", "down") + jeton signe valable 3 min
router.post('/face/challenge', poseLimiter, (req, res) => {
  res.json(face.issueChallenge(v.str(req.body?.purpose)));
});

// POST /api/auth/face/pose  { image } -> { ok, yaw, pitch } : retour en direct pendant le defi
router.post('/face/pose', poseLimiter, async (req, res) => {
  res.json(await face.pose(req.body?.image));
});

// POST /api/auth/face/login  { face: { challengeToken, frames }, email? }
// Avec e-mail : on compare au visage de ce compte (1:1). Sans : on cherche parmi tous les comptes (1:N).
router.post('/face/login', limiter, async (req, res) => {
  const { embedding } = await face.verifyChallenge(req.body?.face, 'login');
  const email = v.str(req.body?.email).toLowerCase();
  const candidates = await User.find(email ? { email } : { faceEmbedding: { $exists: true } }).select('+faceEmbedding');
  const match = face.bestMatch(embedding, candidates);
  if (!match) {
    const known = candidates.some((u) => u.faceEmbedding?.length);
    throw new HttpError(401, email && !known
      ? 'Aucun visage enregistré pour ce compte : connecte-toi avec ton mot de passe'
      : 'Visage non reconnu', 'face');
  }
  console.log(`[FACE] ${match.user.email} reconnu (similarite ${match.score.toFixed(3)})`);
  res.json(session(match.user));
});

module.exports = router;
