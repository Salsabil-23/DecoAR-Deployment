const { HttpError } = require('./errors');

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
const PHONE_RE = /^\+?[0-9 ]{8,15}$/;

const str = (v) => (typeof v === 'string' ? v.trim() : '');
const raw = (v) => (typeof v === 'string' ? v : '');

function name(value, field, label) {
  const v = str(value);
  if (v.length < 2) throw new HttpError(400, `Le ${label} doit contenir au moins 2 caractères`, field);
  if (v.length > 50) throw new HttpError(400, `Le ${label} est trop long`, field);
  return v;
}

function email(value, field = 'email') {
  const v = str(value).toLowerCase();
  if (!EMAIL_RE.test(v)) throw new HttpError(400, 'Adresse e-mail invalide', field);
  return v;
}

function phone(value, field = 'phone') {
  const v = str(value);
  if (v && !PHONE_RE.test(v)) throw new HttpError(400, 'Numéro de téléphone invalide', field);
  return v;
}

function password(value, field = 'password') {
  const v = raw(value);
  if (v.length < 8 || !/[A-Za-z]/.test(v) || !/[0-9]/.test(v))
    throw new HttpError(400, 'Mot de passe : 8 caractères minimum avec lettres et chiffres', field);
  return v;
}

module.exports = { str, raw, name, email, phone, password };
