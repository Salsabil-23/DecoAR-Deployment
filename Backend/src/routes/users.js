const express = require('express');
const User = require('../models/User');
const { HttpError } = require('../utils/errors');
const { requireAuth } = require('../middleware/auth');

const router = express.Router();

// GET /api/users/me : profil de l'utilisateur connecte
router.get('/me', requireAuth, async (req, res) => {
  const user = await User.findById(req.userId);
  if (!user) throw new HttpError(401, 'Compte introuvable');
  res.json({ user: user.toPublic() });
});

module.exports = router;
