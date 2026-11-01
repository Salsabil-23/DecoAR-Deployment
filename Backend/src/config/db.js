const dns = require('dns');
const mongoose = require('mongoose');

// Atlas (mongodb+srv) a besoin d'une requete DNS SRV. Sur certains PC Windows, Node recoit
// 127.0.0.1 comme serveur DNS, qui refuse ces requetes (querySrv ECONNREFUSED) :
// on passe alors par des DNS publics. DNS_SERVERS dans .env permet de forcer d'autres serveurs.
function fixDnsForSrv(uri) {
  if (!uri.startsWith('mongodb+srv://')) return;
  const custom = (process.env.DNS_SERVERS || '').split(',').map((s) => s.trim()).filter(Boolean);
  const onlyLocal = dns.getServers().every((s) => s === '127.0.0.1' || s === '::1');
  if (custom.length === 0 && !onlyLocal) return;
  dns.setServers(custom.length ? custom : ['8.8.8.8', '1.1.1.1']);
  console.log(`[DB] Serveurs DNS utilises : ${dns.getServers().join(', ')}`);
}

async function connectDb(uri) {
  if (!uri) throw new Error('MONGODB_URI manquant (voir .env.example)');
  fixDnsForSrv(uri);
  await mongoose.connect(uri);
  console.log(`[DB] Connecte a MongoDB (${mongoose.connection.name})`);
}

module.exports = { connectDb };
