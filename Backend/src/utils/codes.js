const crypto = require('crypto');
const { HttpError } = require('./errors');

// Codes a 6 chiffres envoyes par e-mail (verification du compte, mot de passe oublie).
// Comme pour les mots de passe, on ne stocke que leur hash.
const TTL_MS = 15 * 60 * 1000;
const RESEND_AFTER_MS = 60 * 1000;
const MAX_ATTEMPTS = 5;

const hash = (code) => crypto.createHash('sha256').update(code).digest('hex');

// Cree un nouveau code dans user[field] et le renvoie en clair (pour l'e-mail).
// Renvoie null si un code vient d'etre envoye (anti-spam).
function issueCode(user, field) {
  const current = user[field];
  if (current?.sentAt && Date.now() - current.sentAt.getTime() < RESEND_AFTER_MS) return null;
  const code = crypto.randomInt(0, 1_000_000).toString().padStart(6, '0');
  user[field] = { hash: hash(code), expiresAt: new Date(Date.now() + TTL_MS), attempts: 0, sentAt: new Date() };
  return code;
}

// Verifie le code ; leve une erreur 400 sinon. Le code est efface apres succes.
function consumeCode(user, field, code) {
  const entry = user[field];
  const fail = (message) => new HttpError(400, message, 'code');
  if (!entry?.hash) throw fail('Aucun code en attente, demande un nouveau code');
  if (entry.expiresAt < new Date()) throw fail('Code expiré, demande un nouveau code');
  if (entry.attempts >= MAX_ATTEMPTS) throw fail('Trop d\'essais, demande un nouveau code');
  if (typeof code !== 'string' || hash(code.trim()) !== entry.hash) {
    entry.attempts += 1;
    throw fail(`Code incorrect (${MAX_ATTEMPTS - entry.attempts} essai(s) restant(s))`);
  }
  user[field] = undefined;
}

module.exports = { issueCode, consumeCode, RESEND_AFTER_MS };
