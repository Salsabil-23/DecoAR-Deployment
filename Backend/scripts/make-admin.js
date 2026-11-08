// Donne (ou retire) le role admin a un compte existant, directement dans MongoDB (meme base que Render).
//   npm run make-admin -- prenom.nom@gmail.com
//   npm run make-admin -- prenom.nom@gmail.com --remove
//   npm run make-admin -- --list
// Un compte ne peut pas devenir admin depuis l'application : seulement avec ce script.
require('dotenv').config();
const mongoose = require('mongoose');
const dns = require('dns');
const User = require('../src/models/User');

async function main() {
  const args = process.argv.slice(2);
  const uri = process.env.MONGODB_URI;
  if (!uri) throw new Error('MONGODB_URI manquant dans Backend/.env');
  // Meme correctif DNS que le serveur pour les adresses Atlas (mongodb+srv)
  if (uri.startsWith('mongodb+srv://') && dns.getServers().every((s) => s === '127.0.0.1' || s === '::1'))
    dns.setServers(['8.8.8.8', '1.1.1.1']);
  await mongoose.connect(uri);

  if (args.includes('--list')) {
    const admins = await User.find({ role: 'admin' }).select('email firstName lastName');
    console.log(admins.length ? admins.map((u) => `- ${u.email} (${u.firstName} ${u.lastName})`).join('\n') : 'Aucun admin');
    return;
  }

  const email = (args.find((a) => !a.startsWith('--')) || '').trim().toLowerCase();
  if (!email) throw new Error('Usage : npm run make-admin -- <email> [--remove] | --list');
  const remove = args.includes('--remove');
  const user = await User.findOneAndUpdate({ email }, { role: remove ? 'user' : 'admin' }, { new: true });
  if (!user) throw new Error(`Aucun compte avec l'e-mail ${email} (crée-le d'abord dans l'application)`);
  console.log(remove ? `${email} n'est plus admin` : `${email} est maintenant admin`);
  if (!remove && !user.faceEnrolledAt)
    console.log('Attention : ce compte n\'a pas de visage enregistré (Profil > Mon visage), il ne pourra pas ouvrir l\'espace admin.');
}

main()
  .catch((err) => {
    console.error('Erreur :', err.message);
    process.exitCode = 1;
  })
  .finally(() => mongoose.disconnect());
