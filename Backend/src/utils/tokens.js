const jwt = require('jsonwebtoken');

function signJwt(user) {
  return jwt.sign({ sub: user._id.toString() }, process.env.JWT_SECRET, {
    expiresIn: process.env.JWT_EXPIRES_IN || '7d',
  });
}

// Jeton de session uniquement : les jetons a usage special (defi visage, admin) ont une "audience"
function verifyJwt(token) {
  const claims = jwt.verify(token, process.env.JWT_SECRET);
  if (claims.aud) throw new Error('Jeton non valable pour une session');
  return claims;
}

// Espace admin : delivre apres une confirmation par le visage, valable peu de temps
const ADMIN_TTL_SECONDS = 30 * 60;

function signAdminToken(user) {
  return jwt.sign({ sub: user._id.toString() }, process.env.JWT_SECRET, {
    expiresIn: ADMIN_TTL_SECONDS,
    audience: 'admin',
  });
}

function verifyAdminToken(token) {
  return jwt.verify(token, process.env.JWT_SECRET, { audience: 'admin' });
}

module.exports = { signJwt, verifyJwt, signAdminToken, verifyAdminToken, ADMIN_TTL_SECONDS };
