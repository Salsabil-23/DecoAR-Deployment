const { verifyJwt, verifyAdminToken } = require('../utils/tokens');
const { HttpError } = require('../utils/errors');
const User = require('../models/User');

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

// Espace admin (apres requireAuth) : compte "admin" dans MongoDB ET jeton admin recent,
// obtenu par POST /api/admin/confirm (defi de reconnaissance faciale), dans l'en-tete X-Admin-Token
async function requireAdmin(req, res, next) {
  const user = await User.findById(req.userId).select('role');
  if (!user || user.role !== 'admin') throw new HttpError(403, 'Accès réservé aux administrateurs', 'admin');
  try {
    if (verifyAdminToken(req.get('X-Admin-Token') || '').sub !== req.userId) throw new Error();
  } catch {
    throw new HttpError(403, 'Confirme ton identité avec ton visage pour accéder à l\'espace admin', 'admin');
  }
  next();
}

module.exports = { requireAuth, requireAdmin };
