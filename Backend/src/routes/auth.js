const express = require('express');
const bcrypt = require('bcryptjs');
const rateLimit = require('express-rate-limit');
const User = require('../models/User');
const { HttpError } = require('../utils/errors');
const { requireAuth } = require('../middleware/auth');
const { signJwt, newDeviceToken, hashDeviceToken, sameHash } = require('../utils/tokens');

const router = express.Router();

// Limite les essais de mot de passe / jeton (desactive pendant les tests)
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 30,
  skip: () => process.env.NODE_ENV === 'test',
  handler: (req, res) => res.status(429).json({ message: 'Trop de tentatives, réessaie plus tard', field: '' }),
});

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
const PHONE_RE = /^\+?[0-9 ]{8,15}$/;
const MAX_DEVICES = 5;

const str = (v) => (typeof v === 'string' ? v.trim() : '');

function validateRegister(body) {
  const firstName = str(body.firstName);
  const lastName = str(body.lastName);
  const email = str(body.email).toLowerCase();
  const phone = str(body.phone);
  const password = typeof body.password === 'string' ? body.password : '';

  if (firstName.length < 2) throw new HttpError(400, 'Le prénom doit contenir au moins 2 caractères', 'firstName');
  if (lastName.length < 2) throw new HttpError(400, 'Le nom doit contenir au moins 2 caractères', 'lastName');
  if (!EMAIL_RE.test(email)) throw new HttpError(400, 'Adresse e-mail invalide', 'email');
  if (phone && !PHONE_RE.test(phone)) throw new HttpError(400, 'Numéro de téléphone invalide', 'phone');
  if (password.length < 8 || !/[A-Za-z]/.test(password) || !/[0-9]/.test(password))
    throw new HttpError(400, 'Mot de passe : 8 caractères minimum avec lettres et chiffres', 'password');

  return { firstName, lastName, email, phone, password };
}

function session(user) {
  return { token: signJwt(user), user: user.toPublic() };
}

// POST /api/auth/register
router.post('/register', limiter, async (req, res) => {
  const data = validateRegister(req.body || {});
  if (await User.exists({ email: data.email }))
    throw new HttpError(409, 'Un compte existe déjà avec cet e-mail', 'email');

  const user = await User.create({
    firstName: data.firstName,
    lastName: data.lastName,
    email: data.email,
    phone: data.phone,
    passwordHash: await bcrypt.hash(data.password, 10),
  });
  res.status(201).json(session(user));
});

// POST /api/auth/login  (e-mail + mot de passe)
router.post('/login', limiter, async (req, res) => {
  const email = str(req.body?.email).toLowerCase();
  const password = typeof req.body?.password === 'string' ? req.body.password : '';
  const user = await User.findOne({ email }).select('+passwordHash');
  if (!user || !(await bcrypt.compare(password, user.passwordHash)))
    throw new HttpError(401, 'E-mail ou mot de passe incorrect');
  res.json(session(user));
});

// POST /api/auth/biometric/enroll  (connecte) : appele APRES une validation Face ID reussie
// Renvoie un jeton d'appareil a garder dans le Keychain / Keystore du telephone.
router.post('/biometric/enroll', requireAuth, async (req, res) => {
  const deviceId = str(req.body?.deviceId);
  const deviceName = str(req.body?.deviceName).slice(0, 80);
  if (!deviceId) throw new HttpError(400, 'deviceId manquant', 'deviceId');

  const user = await User.findById(req.userId).select('+biometricDevices');
  if (!user) throw new HttpError(401, 'Compte introuvable');

  const biometricToken = newDeviceToken();
  user.biometricDevices = user.biometricDevices.filter((d) => d.deviceId !== deviceId);
  user.biometricDevices.push({ deviceId, deviceName, tokenHash: hashDeviceToken(biometricToken) });
  if (user.biometricDevices.length > MAX_DEVICES) user.biometricDevices.shift();
  user.faceIdEnabled = true;
  await user.save();

  res.status(201).json({ biometricToken, user: user.toPublic() });
});

// POST /api/auth/biometric/login  : le telephone a valide le visage, il envoie son jeton
router.post('/biometric/login', limiter, async (req, res) => {
  const email = str(req.body?.email).toLowerCase();
  const deviceId = str(req.body?.deviceId);
  const biometricToken = str(req.body?.biometricToken);
  const fail = new HttpError(401, 'Face ID non activé sur cet appareil, connecte-toi avec ton mot de passe');
  if (!email || !deviceId || !/^[0-9a-f]{64}$/.test(biometricToken)) throw fail;

  const user = await User.findOne({ email }).select('+biometricDevices');
  const device = user?.biometricDevices.find((d) => d.deviceId === deviceId);
  if (!device || !sameHash(device.tokenHash, hashDeviceToken(biometricToken))) throw fail;

  device.lastUsedAt = new Date();
  await user.save();
  res.json(session(user));
});

// DELETE /api/auth/biometric/:deviceId  (connecte) : desactive Face ID sur cet appareil
router.delete('/biometric/:deviceId', requireAuth, async (req, res) => {
  const user = await User.findById(req.userId).select('+biometricDevices');
  if (!user) throw new HttpError(401, 'Compte introuvable');
  user.biometricDevices = user.biometricDevices.filter((d) => d.deviceId !== req.params.deviceId);
  user.faceIdEnabled = user.biometricDevices.length > 0;
  await user.save();
  res.json({ user: user.toPublic() });
});

module.exports = router;
