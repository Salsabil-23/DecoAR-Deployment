// Erreur avec code HTTP ; "field" indique a Unity quel champ surligner
class HttpError extends Error {
  constructor(status, message, field) {
    super(message);
    this.status = status;
    this.field = field;
  }
}

// Toutes les erreurs ont la forme { message, field } (lisible par JsonUtility)
function errorHandler(err, req, res, next) {
  if (err.type === 'entity.parse.failed') {
    return res.status(400).json({ message: 'JSON invalide', field: '' });
  }
  const status = err.status || 500;
  if (status === 500) console.error(err);
  res.status(status).json({
    message: status === 500 ? 'Erreur serveur' : err.message,
    field: err.field || '',
  });
}

module.exports = { HttpError, errorHandler };
