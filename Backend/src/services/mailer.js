const dns = require('dns');
const nodemailer = require('nodemailer');

// Envoi des e-mails. Fournisseur choisi par MAIL_PROVIDER (ou deduit du .env) :
//   console : le code s'affiche dans le terminal (developpement, rien a configurer)
//   smtp    : Gmail ou autre via SMTP (SMTP_USER + SMTP_PASS = mot de passe d'application)
//   brevo   : API HTTP Brevo (BREVO_API_KEY). A utiliser en ligne : Render/Railway gratuits bloquent le SMTP.
function provider() {
  if (process.env.MAIL_PROVIDER) return process.env.MAIL_PROVIDER;
  // Une vraie cle API Brevo commence par "xkeysib-" (une valeur provisoire comme "aucune" est ignoree)
  if (process.env.BREVO_API_KEY?.startsWith('xkeysib-')) return 'brevo';
  if (process.env.SMTP_USER && process.env.SMTP_PASS) return 'smtp';
  return 'console';
}

function from() {
  return process.env.MAIL_FROM || process.env.SMTP_USER || 'no-reply@decoar.local';
}

// L'adresse du serveur SMTP est resolue par le systeme (comme le navigateur) : le resolveur DNS
// interne de Node echoue sur certains reseaux (ETIMEOUT) et bloquait l'envoi pendant ~1 min.
async function smtp() {
  const host = process.env.SMTP_HOST || 'smtp.gmail.com';
  const port = Number(process.env.SMTP_PORT) || 465;
  const { address } = await dns.promises.lookup(host, { family: 4 });
  return nodemailer.createTransport({
    host: address,
    port,
    secure: port === 465,
    tls: { servername: host }, // certificat verifie pour smtp.gmail.com, pas pour l'IP
    auth: { user: process.env.SMTP_USER, pass: process.env.SMTP_PASS },
    connectionTimeout: 15_000,
    greetingTimeout: 15_000,
    socketTimeout: 20_000,
  });
}

async function sendBrevo({ to, subject, text, html }) {
  const res = await fetch('https://api.brevo.com/v3/smtp/email', {
    method: 'POST',
    headers: { 'api-key': process.env.BREVO_API_KEY, 'content-type': 'application/json', accept: 'application/json' },
    body: JSON.stringify({
      sender: { name: 'DecoAR', email: from() },
      to: [{ email: to }],
      subject,
      textContent: text,
      htmlContent: html,
    }),
  });
  if (!res.ok) throw new Error(`Brevo ${res.status} : ${await res.text()}`);
}

const mailer = {
  provider,

  // Remplacable dans les tests (mailer.send = ...)
  async send(message) {
    switch (provider()) {
      case 'smtp':
        await (await smtp()).sendMail({ from: `DecoAR <${from()}>`, ...message });
        break;
      case 'brevo':
        await sendBrevo(message);
        break;
      default:
        console.log(`[MAIL] A : ${message.to} | ${message.subject}\n${message.text}`);
    }
  },
};

module.exports = mailer;
