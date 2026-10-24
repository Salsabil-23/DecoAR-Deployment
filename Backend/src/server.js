require('dotenv').config();
const { createApp } = require('./app');
const { connectDb } = require('./config/db');

async function main() {
  if (!process.env.JWT_SECRET) throw new Error('JWT_SECRET manquant (voir .env.example)');
  await connectDb(process.env.MONGODB_URI);

  const port = Number(process.env.PORT) || 3000;
  // 0.0.0.0 : accessible depuis le telephone sur le meme Wi-Fi
  createApp().listen(port, '0.0.0.0', () => {
    console.log(`[API] DecoAR en ecoute sur http://localhost:${port}`);
  });
}

main().catch((err) => {
  console.error('[API] Demarrage impossible :', err.message);
  process.exit(1);
});
