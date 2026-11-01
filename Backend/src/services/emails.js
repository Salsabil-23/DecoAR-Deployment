const mailer = require('./mailer');

// Gabarit commun : carte creme, accent terracotta (memes couleurs que l'app)
function layout(title, intro, code, outro) {
  return `<!doctype html><html><body style="margin:0;background:#1C1613;font-family:Arial,sans-serif">
  <table width="100%" cellpadding="0" cellspacing="0"><tr><td align="center" style="padding:32px 12px">
    <table width="100%" style="max-width:480px;background:#ffffff;border-radius:18px" cellpadding="0" cellspacing="0">
      <tr><td style="padding:28px 32px 8px;font-size:28px;font-weight:bold;color:#2E2A26">Deco<span style="color:#C8734A">AR</span></td></tr>
      <tr><td style="padding:8px 32px;font-size:20px;font-weight:bold;color:#2E2A26">${title}</td></tr>
      <tr><td style="padding:4px 32px 16px;font-size:15px;line-height:22px;color:#5c554e">${intro}</td></tr>
      <tr><td align="center" style="padding:8px 32px">
        <div style="display:inline-block;background:#F8F4EF;border:2px solid #C8734A;border-radius:12px;padding:14px 26px;font-size:34px;letter-spacing:10px;font-weight:bold;color:#2E2A26">${code}</div>
      </td></tr>
      <tr><td style="padding:16px 32px 28px;font-size:13px;line-height:20px;color:#8A8178">${outro}</td></tr>
    </table>
  </td></tr></table></body></html>`;
}

async function sendVerificationCode(user, code) {
  await mailer.send({
    to: user.email,
    subject: `${code} est ton code DecoAR`,
    text: `Bonjour ${user.firstName},\n\nTon code de vérification DecoAR : ${code}\nIl est valable 15 minutes.`,
    html: layout(
      `Bienvenue ${user.firstName} !`,
      'Saisis ce code dans l\'application pour confirmer ton adresse e-mail :',
      code,
      'Ce code est valable 15 minutes. Si tu n\'as pas créé de compte DecoAR, ignore cet e-mail.'
    ),
  });
}

async function sendResetCode(user, code) {
  await mailer.send({
    to: user.email,
    subject: `${code} : réinitialisation de ton mot de passe DecoAR`,
    text: `Bonjour ${user.firstName},\n\nTon code pour choisir un nouveau mot de passe : ${code}\nIl est valable 15 minutes.`,
    html: layout(
      'Mot de passe oublié ?',
      `Bonjour ${user.firstName}, saisis ce code dans l'application pour choisir un nouveau mot de passe :`,
      code,
      'Ce code est valable 15 minutes. Si tu n\'as rien demandé, ignore cet e-mail : ton mot de passe ne change pas.'
    ),
  });
}

// L'envoi ne doit pas faire echouer l'inscription : on journalise l'erreur
async function trySend(fn, user, code) {
  try {
    await fn(user, code);
    console.log(`[MAIL] Code envoye a ${user.email}`);
    return true;
  } catch (err) {
    console.error(`[MAIL] Echec d'envoi a ${user.email} : ${err.message}`);
    return false;
  }
}

module.exports = { sendVerificationCode, sendResetCode, trySend };
