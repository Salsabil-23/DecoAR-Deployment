process.env.NODE_ENV = 'test';
process.env.JWT_SECRET = 'secret-de-test';

const { test, before, after } = require('node:test');
const assert = require('node:assert');
const mongoose = require('mongoose');
const request = require('supertest');
const { MongoMemoryServer } = require('mongodb-memory-server');
const { createApp } = require('../src/app');
const mailer = require('../src/services/mailer');
const User = require('../src/models/User');

let mongo;
const app = createApp();
// Faux service visage : "visage:<nom>:<pose>" -> vecteur propre a cette personne + orientation de la tete,
// "vide" -> pas de visage
const faceService = require('../src/services/faceService');
const POSES = { center: [0, 0.55], left: [-0.35, 0.55], right: [0.35, 0.55], up: [0, 0.35], down: [0, 0.75] };
function vectorFor(name, noise) {
  let seed = [...name].reduce((h, c) => (h * 31 + c.charCodeAt(0)) >>> 0, 7);
  const rand = () => (seed = (seed * 1103515245 + 12345) >>> 0) / 2 ** 32 - 0.5;
  return Array.from({ length: 128 }, () => rand() + noise * (Math.random() - 0.5));
}
faceService.embed = async (images) => images.map((img) => {
  const [kind, name, pose = 'center'] = img.split(':');
  if (kind !== 'visage') return { ok: false, error: 'Aucun visage détecté' };
  const [yaw, pitch] = POSES[pose];
  return { ok: true, embedding: vectorFor(name, 0.05), yaw, pitch, thumbnail: Buffer.from([0xff, 0xd8, 0xff]).toString('base64') };
});

// Fait le defi demande par le serveur avec le visage de <name> ; poses forcees pour simuler une triche
async function proof(name, purpose, forcePoses) {
  const { body } = await request(app).post('/api/auth/face/challenge').send({ purpose }).expect(200);
  const poses = forcePoses || body.steps;
  return { challengeToken: body.challengeToken, frames: poses.map((p) => `visage:${name}:${p}`), steps: body.steps };
}

const alice = { firstName: 'Alice', lastName: 'Martin', email: 'Alice@Example.com', phone: '+216 22 333 444', password: 'motdepasse1' };

// Les e-mails sont captures au lieu d'etre envoyes ; on lit le code dedans
const sent = [];
mailer.send = async (message) => { sent.push(message); };
const lastCode = () => sent.at(-1).text.match(/\b(\d{6})\b/)[1];

// Permet de redemander un code sans attendre la minute anti-spam
async function expireCooldown(field) {
  await User.updateOne({ email: 'alice@example.com' }, { [`${field}.sentAt`]: new Date(0) });
}

let token;
const auth = () => ({ Authorization: `Bearer ${token}` });

before(async () => {
  mongo = await MongoMemoryServer.create({ instance: { launchTimeout: 60000 } });
  await mongoose.connect(mongo.getUri());
});

after(async () => {
  await mongoose.disconnect();
  await mongo.stop();
});

test('inscription : cree le compte, envoie le code et renvoie un jeton', async () => {
  const res = await request(app).post('/api/auth/register').send({ ...alice, face: await proof('alice', 'enroll') }).expect(201);
  token = res.body.token;
  assert.strictEqual(res.body.user.email, 'alice@example.com');
  assert.strictEqual(res.body.user.emailVerified, false);
  assert.strictEqual(res.body.user.faceEnrolled, true);
  assert.strictEqual(res.body.user.hasPhoto, true); // photo de profil = visage recadre
  assert.strictEqual(res.body.user.passwordHash, undefined);
  assert.strictEqual(sent.at(-1).to, 'alice@example.com');
});

test('inscription : refuse un e-mail deja utilise et un mot de passe faible', async () => {
  const dup = await request(app).post('/api/auth/register').send(alice).expect(409);
  assert.strictEqual(dup.body.field, 'email');
  const weak = await request(app).post('/api/auth/register').send({ ...alice, email: 'b@ex.com', password: 'court' }).expect(400);
  assert.strictEqual(weak.body.field, 'password');
});

test('verification e-mail : mauvais code, anti-spam, puis bon code', async () => {
  const code = lastCode();
  const bad = await request(app).post('/api/auth/verify-email').set(auth()).send({ code: code === '000000' ? '111111' : '000000' }).expect(400);
  assert.match(bad.body.message, /4 essai/);

  await request(app).post('/api/auth/verify-email/resend').set(auth()).expect(429);
  await expireCooldown('verifyCode');
  await request(app).post('/api/auth/verify-email/resend').set(auth()).expect(200);

  const ok = await request(app).post('/api/auth/verify-email').set(auth()).send({ code: lastCode() }).expect(200);
  assert.strictEqual(ok.body.user.emailVerified, true);
});

test('connexion par mot de passe et connexion automatique (/me)', async () => {
  await request(app).post('/api/auth/login').send({ email: 'alice@example.com', password: 'mauvais1' }).expect(401);
  const res = await request(app).post('/api/auth/login').send({ email: 'alice@example.com', password: alice.password }).expect(200);
  token = res.body.token;
  const me = await request(app).get('/api/users/me').set(auth()).expect(200);
  assert.strictEqual(me.body.user.firstName, 'Alice');
  await request(app).get('/api/users/me').set({ Authorization: 'Bearer faux' }).expect(401);
});

test('profil : modifier nom / telephone et changer le mot de passe', async () => {
  const res = await request(app).patch('/api/users/me').set(auth()).send({ firstName: 'Alicia', phone: '+216 99 888 777' }).expect(200);
  assert.strictEqual(res.body.user.firstName, 'Alicia');
  await request(app).patch('/api/users/me').set(auth()).send({ lastName: 'X' }).expect(400);

  await request(app).post('/api/users/me/password').set(auth()).send({ currentPassword: 'faux', newPassword: 'nouveau123' }).expect(400);
  await request(app).post('/api/users/me/password').set(auth()).send({ currentPassword: alice.password, newPassword: 'nouveau123' }).expect(200);
  await request(app).post('/api/auth/login').send({ email: alice.email, password: 'nouveau123' }).expect(200);
});

test('mot de passe oublie : code par e-mail puis nouveau mot de passe', async () => {
  const unknown = await request(app).post('/api/auth/forgot-password').send({ email: 'personne@ex.com' }).expect(200);
  const known = await request(app).post('/api/auth/forgot-password').send({ email: alice.email }).expect(200);
  assert.strictEqual(unknown.body.message, known.body.message); // ne revele pas si le compte existe
  assert.match(sent.at(-1).subject, /réinitialisation/);

  await request(app).post('/api/auth/reset-password').send({ email: alice.email, code: '12345', password: 'reset1234' }).expect(400);
  const res = await request(app).post('/api/auth/reset-password').send({ email: alice.email, code: lastCode(), password: 'reset1234' }).expect(200);
  assert.ok(res.body.token);
  await request(app).post('/api/auth/login').send({ email: alice.email, password: 'reset1234' }).expect(200);
  // le code ne sert qu'une fois
  await request(app).post('/api/auth/reset-password').send({ email: alice.email, code: lastCode(), password: 'encore1234' }).expect(400);
});

test('photo de profil : envoi, lecture, refus si invalide', async () => {
  const jpeg = Buffer.from([0xff, 0xd8, 0xff, 0xe0, 1, 2, 3, 4]);
  const res = await request(app).put('/api/users/me/photo').set(auth()).send({ image: jpeg.toString('base64') }).expect(200);
  assert.strictEqual(res.body.user.hasPhoto, true);
  const img = await request(app).get('/api/users/me/photo').set(auth()).expect(200);
  assert.strictEqual(img.headers['content-type'], 'image/jpeg');
  assert.deepStrictEqual(Buffer.from(img.body), jpeg);
  await request(app).put('/api/users/me/photo').set(auth()).send({ image: 'pas une image' }).expect(400);
});

test('defi : mouvements tires au hasard, jeton signe', async () => {
  const enroll = await request(app).post('/api/auth/face/challenge').send({ purpose: 'enroll' }).expect(200);
  assert.strictEqual(enroll.body.steps.length, 4);
  assert.strictEqual(enroll.body.steps[0], 'center');
  assert.strictEqual(enroll.body.steps[3], 'center');
  assert.notStrictEqual(enroll.body.steps[1], enroll.body.steps[2]);
  const login = await request(app).post('/api/auth/face/challenge').send({ purpose: 'login' }).expect(200);
  assert.strictEqual(login.body.steps.length, 2);
  await request(app).post('/api/auth/face/challenge').send({ purpose: 'autre' }).expect(400);

  const pose = await request(app).post('/api/auth/face/pose').send({ image: 'visage:alice:left' }).expect(200);
  assert.deepStrictEqual([pose.body.ok, pose.body.yaw], [true, -0.35]);
  const none = await request(app).post('/api/auth/face/pose').send({ image: 'vide' }).expect(200);
  assert.strictEqual(none.body.ok, false);
});

test('inscription : defi obligatoire, mouvements et visage verifies', async () => {
  const base = { ...alice, email: 'bob@ex.com' };
  const missing = await request(app).post('/api/auth/register').send(base).expect(400);
  assert.strictEqual(missing.body.field, 'face');

  // Photo immobile (toujours de face) : refusee, c'est le but du defi
  const still = await proof('bob', 'enroll', ['center', 'center', 'center', 'center']);
  const r1 = await request(app).post('/api/auth/register').send({ ...base, face: still }).expect(400);
  assert.match(r1.body.message, /Mouvement non reconnu/);

  // Visage remplace pendant le defi
  const swap = await proof('bob', 'enroll');
  swap.frames[2] = swap.frames[2].replace('bob', 'carl');
  const r2 = await request(app).post('/api/auth/register').send({ ...base, face: swap }).expect(400);
  assert.match(r2.body.message, /changé/);

  // Un defi ne sert qu'une fois
  const once = await proof('bob', 'enroll');
  await request(app).post('/api/auth/register').send({ ...base, face: { ...once, frames: once.frames.map(() => 'vide') } }).expect(400);
  const replay = await request(app).post('/api/auth/register').send({ ...base, face: once }).expect(400);
  assert.match(replay.body.message, /déjà utilisé/);

  // Ce visage a deja un compte
  const dup = await request(app).post('/api/auth/register').send({ ...base, face: await proof('alice', 'enroll') }).expect(409);
  assert.strictEqual(dup.body.field, 'face');

  await request(app).post('/api/auth/register').send({ ...base, face: await proof('bob', 'enroll') }).expect(201);
});

test('connexion par le visage : defi + reconnaissance (1:1 et 1:N)', async () => {
  const withEmail = await request(app).post('/api/auth/face/login').send({ email: alice.email, face: await proof('alice', 'login') }).expect(200);
  assert.strictEqual(withEmail.body.user.email, 'alice@example.com');
  const without = await request(app).post('/api/auth/face/login').send({ face: await proof('bob', 'login') }).expect(200);
  assert.strictEqual(without.body.user.email, 'bob@ex.com');

  await request(app).post('/api/auth/face/login').send({ email: alice.email, face: await proof('bob', 'login') }).expect(401);
  await request(app).post('/api/auth/face/login').send({ face: await proof('inconnu', 'login') }).expect(401);
  // Un defi d'inscription ne sert pas a se connecter
  await request(app).post('/api/auth/face/login').send({ face: await proof('alice', 'enroll') }).expect(400);
});

test('profil : reenregistrer son visage avec un defi', async () => {
  const res = await request(app).put('/api/users/me/face').set(auth()).send({ face: await proof('alice2', 'enroll') }).expect(200);
  assert.strictEqual(res.body.user.faceEnrolled, true);
  await request(app).post('/api/auth/face/login').send({ email: alice.email, face: await proof('alice2', 'login') }).expect(200);
  await request(app).post('/api/auth/face/login').send({ email: alice.email, face: await proof('alice', 'login') }).expect(401);
  await request(app).put('/api/users/me/face').set(auth()).send({ face: await proof('bob', 'enroll') }).expect(409);
});
