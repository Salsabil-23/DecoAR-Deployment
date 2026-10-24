const express = require('express');
const cors = require('cors');
const authRoutes = require('./routes/auth');
const userRoutes = require('./routes/users');
const { HttpError, errorHandler } = require('./utils/errors');

function createApp() {
  const app = express();
  app.use(cors());
  app.use(express.json({ limit: '100kb' }));

  app.get('/api/health', (req, res) => res.json({ status: 'ok' }));
  app.use('/api/auth', authRoutes);
  app.use('/api/users', userRoutes);

  app.use((req, res, next) => next(new HttpError(404, 'Route introuvable')));
  app.use(errorHandler);
  return app;
}

module.exports = { createApp };
