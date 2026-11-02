const express = require('express');
const cors = require('cors');
const authRoutes = require('./routes/auth');
const userRoutes = require('./routes/users');
const { HttpError, errorHandler } = require('./utils/errors');
const faceService = require('./services/faceService');

function createApp() {
  const app = express();
  // En ligne (Render), les requetes passent par un proxy : on fait confiance a son en-tete X-Forwarded-For
  if (process.env.RENDER || process.env.TRUST_PROXY) app.set('trust proxy', 1);
  app.use(cors());
  app.use(express.json({ limit: '1mb' })); // 1 Mo : la photo de profil arrive en base64

  // Journal des requetes : permet de voir si le telephone atteint bien le serveur
  if (process.env.NODE_ENV !== 'test') {
    app.use((req, res, next) => {
      res.on('finish', () => console.log(`[API] ${req.ip} ${req.method} ${req.originalUrl} -> ${res.statusCode}`));
      next();
    });
  }

  app.get('/api/health', (req, res) => {
    // En ligne, le service visage s'endort aussi : l'app appelle /health a l'ouverture, on le reveille en meme temps
    if (process.env.NODE_ENV !== 'test')
      fetch(`${faceService.url()}/health`, { signal: AbortSignal.timeout(60_000) }).catch(() => {});
    res.json({ status: 'ok' });
  });
  app.use('/api/auth', authRoutes);
  app.use('/api/users', userRoutes);

  app.use((req, res, next) => next(new HttpError(404, 'Route introuvable')));
  app.use(errorHandler);
  return app;
}

module.exports = { createApp };
