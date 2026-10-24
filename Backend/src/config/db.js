const mongoose = require('mongoose');

async function connectDb(uri) {
  if (!uri) throw new Error('MONGODB_URI manquant (voir .env.example)');
  await mongoose.connect(uri);
  console.log(`[DB] Connecte a MongoDB (${mongoose.connection.name})`);
}

module.exports = { connectDb };
