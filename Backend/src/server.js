require('dotenv').config();
const { createApp } = require('./app');
const { connectDb } = require('./config/db');

async function main() {
  if (!process.env.JWT_SECRET) throw new Error('JWT_SECRET manquant (voir .env.example)');
  await connectDb(process.env.MONGODB_URI);

  const port = Number(process.env.PORT) || 3000;
  // 0.0.0.0 : accessible depuis le telephone sur le meme Wi-Fi
  const server = createApp().listen(port, '0.0.0.0', () => {
    console.log(`[API] DecoAR en ecoute sur http://localhost:${port}`);
  });
  // Node ferme les connexions inactives apres 5 s ; Unity (Android) les reutilise plus tard
  // et recoit "Connection reset by peer". On les garde ouvertes plus longtemps que le client.
  server.keepAliveTimeout = 65_000;
  server.headersTimeout = 66_000;
}

main().catch((err) => {
  console.error('[API] Demarrage impossible :', err.message);
  process.exit(1);
});
