const crypto = require('crypto');
const jwt = require('jsonwebtoken');

function signJwt(user) {
  return jwt.sign({ sub: user._id.toString() }, process.env.JWT_SECRET, {
    expiresIn: process.env.JWT_EXPIRES_IN || '7d',
  });
}

function verifyJwt(token) {
  return jwt.verify(token, process.env.JWT_SECRET);
}

// Jeton d'appareil Face ID : aleatoire, on ne stocke que son SHA-256
function newDeviceToken() {
  return crypto.randomBytes(32).toString('hex');
}

function hashDeviceToken(token) {
  return crypto.createHash('sha256').update(token).digest('hex');
}

function sameHash(a, b) {
  const ba = Buffer.from(a, 'hex');
  const bb = Buffer.from(b, 'hex');
  return ba.length === bb.length && crypto.timingSafeEqual(ba, bb);
}

module.exports = { signJwt, verifyJwt, newDeviceToken, hashDeviceToken, sameHash };
