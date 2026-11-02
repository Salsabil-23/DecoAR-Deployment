const { verifyJwt } = require('../utils/tokens');
const { HttpError } = require('../utils/errors');

// Exige "Authorization: Bearer <jwt>" et place l'id utilisateur dans req.userId
function requireAuth(req, res, next) {
  const header = req.get('Authorization') || '';
  const [scheme, token] = header.split(' ');
  if (scheme !== 'Bearer' || !token) return next(new HttpError(401, 'Connexion requise'));
  try {
    req.userId = verifyJwt(token).sub;
    next();
  } catch {
    next(new HttpError(401, 'Session expirée, reconnecte-toi'));
  }
}

module.exports = { requireAuth };
