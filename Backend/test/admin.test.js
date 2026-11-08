process.env.NODE_ENV = 'test';
process.env.JWT_SECRET = 'secret-de-test';

const { test, before, after } = require('node:test');
const assert = require('node:assert');
const mongoose = require('mongoose');
const request = require('supertest');
const { MongoMemoryServer } = require('mongodb-memory-server');
const { createApp } = require('../src/app');
const User = require('../src/models/User');
const faceService = require('../src/services/faceService');
const mailer = require('../src/services/mailer');

// Faux service visage (comme auth.test.js) : "visage:<nom>:<pose>"
const POSES = { center: [0, 0.55], left: [-0.35, 0.55], right: [0.35, 0.55], up: [0, 0.35], down: [0, 0.75] };
function vectorFor(name) {
  let seed = [...name].reduce((h, c) => (h * 31 + c.charCodeAt(0)) >>> 0, 7);
  const rand = () => (seed = (seed * 1103515245 + 12345) >>> 0) / 2 ** 32 - 0.5;
  return Array.from({ length: 128 }, () => rand() + 0.05 * (Math.random() - 0.5));
}
faceService.embed = async (images) => images.map((img) => {
  const [kind, name, pose = 'center'] = img.split(':');
  if (kind !== 'visage') return { ok: false, error: 'Aucun visage détecté' };
  const [yaw, pitch] = POSES[pose];
  return { ok: true, embedding: vectorFor(name), yaw, pitch, thumbnail: Buffer.from([0xff, 0xd8, 0xff]).toString('base64') };
});
mailer.send = async () => {};

const app = createApp();
let mongo;

async function proof(name, purpose) {
  const { body } = await request(app).post('/api/auth/face/challenge').send({ purpose }).expect(200);
  return { challengeToken: body.challengeToken, frames: body.steps.map((p) => `visage:${name}:${p}`) };
}

async function signup(name, email) {
  const res = await request(app).post('/api/auth/register')
    .send({ firstName: name, lastName: 'Test', email, password: 'motdepasse1', face: await proof(name, 'enroll') }).expect(201);
  return { Authorization: `Bearer ${res.body.token}` };
}

const glb = Buffer.concat([Buffer.from('glTF'), Buffer.alloc(3000, 1)]);
let admin;  // en-tetes de session de l'admin
let user;   // en-tetes d'un utilisateur normal
let adminToken;
const asAdmin = () => ({ ...admin, 'X-Admin-Token': adminToken });

before(async () => {
  mongo = await MongoMemoryServer.create({ instance: { launchTimeout: 60000 } });
  await mongoose.connect(mongo.getUri());
  admin = await signup('karim', 'karim@ex.com');
  user = await signup('lina', 'lina@ex.com');
  await User.updateOne({ email: 'karim@ex.com' }, { role: 'admin' }); // ce que fait npm run make-admin
});

after(async () => {
  await mongoose.disconnect();
  await mongo.stop();
});

test('role visible par l\'app', async () => {
  const me = await request(app).get('/api/users/me').set(admin).expect(200);
  assert.strictEqual(me.body.user.role, 'admin');
  const other = await request(app).get('/api/users/me').set(user).expect(200);
  assert.strictEqual(other.body.user.role, 'user');
});

test('ouvrir l\'espace admin : compte admin + son propre visage', async () => {
  const notAdmin = await request(app).post('/api/admin/confirm').set(user).send({ face: await proof('lina', 'login') }).expect(403);
  assert.match(notAdmin.body.message, /administrateurs/);

  // Le visage d'une autre personne ne suffit pas, meme sur le telephone de l'admin
  const wrong = await request(app).post('/api/admin/confirm').set(admin).send({ face: await proof('lina', 'login') }).expect(401);
  assert.strictEqual(wrong.body.message, 'Visage non reconnu');

  const ok = await request(app).post('/api/admin/confirm').set(admin).send({ face: await proof('karim', 'login') }).expect(200);
  assert.ok(ok.body.adminToken);
  assert.strictEqual(ok.body.expiresIn, 1800);
  adminToken = ok.body.adminToken;

  // Le jeton admin n'est pas un jeton de session
  await request(app).get('/api/users/me').set({ Authorization: `Bearer ${adminToken}` }).expect(401);
});

test('admin sans visage enregistre : message clair', async () => {
  await User.create({ firstName: 'Sans', lastName: 'Visage', email: 'sv@ex.com', passwordHash: 'x', role: 'admin' });
  const login = await request(app).post('/api/auth/login').send({ email: 'sv@ex.com', password: 'x' });
  assert.strictEqual(login.status, 401); // mot de passe "x" non hache : on signe une session directement
  const { signJwt } = require('../src/utils/tokens');
  const sv = await User.findOne({ email: 'sv@ex.com' });
  const res = await request(app).post('/api/admin/confirm').set({ Authorization: `Bearer ${signJwt(sv)}` })
    .send({ face: await proof('sv', 'login') }).expect(400);
  assert.match(res.body.message, /Enregistre d'abord ton visage/);
});

test('catalogue : ajout, liste, modele, modification, suppression', async () => {
  const add = await request(app).post('/api/catalog').query({ name: 'Fauteuil Lin', category: 'Salon' })
    .set(asAdmin()).set('Content-Type', 'application/octet-stream').send(glb).expect(201);
  const id = add.body.product.id;
  assert.strictEqual(add.body.product.modelSize, glb.length);

  // Tout utilisateur connecte voit le catalogue et telecharge les modeles
  const list = await request(app).get('/api/catalog').set(user).expect(200);
  assert.deepStrictEqual(list.body.products.map((p) => p.name), ['Fauteuil Lin']);
  assert.ok(list.body.categories.includes('Luminaires'));
  const model = await request(app).get(`/api/catalog/${id}/model`).set(user).buffer(true).parse((res, cb) => {
    const c = []; res.on('data', (d) => c.push(d)); res.on('end', () => cb(null, Buffer.concat(c)));
  }).expect(200);
  assert.deepStrictEqual(model.body, glb);

  const edit = await request(app).patch(`/api/catalog/${id}`).set(asAdmin()).send({ name: 'Fauteuil Lin beige', category: 'Chambre' }).expect(200);
  assert.strictEqual(edit.body.product.category, 'Chambre');
  await request(app).patch(`/api/catalog/${id}`).set(asAdmin()).send({ category: 'Garage' }).expect(400);

  await request(app).delete(`/api/catalog/${id}`).set(asAdmin()).expect(200);
  const empty = await request(app).get('/api/catalog').set(user).expect(200);
  assert.strictEqual(empty.body.products.length, 0);
  await request(app).get(`/api/catalog/${id}/model`).set(user).expect(404);
});

test('catalogue : modification refusee sans espace admin', async () => {
  const send = (headers) => request(app).post('/api/catalog').query({ name: 'Lampe', category: 'Luminaires' })
    .set(headers).set('Content-Type', 'application/octet-stream').send(glb);
  await send({}).expect(401);
  await send(user).expect(403);
  await send(admin).expect(403); // admin connecte mais visage pas confirme
  await send({ ...user, 'X-Admin-Token': adminToken }).expect(403); // jeton d'un autre compte
  await request(app).post('/api/catalog').query({ name: 'Lampe', category: 'Luminaires' })
    .set(asAdmin()).set('Content-Type', 'application/octet-stream').send(Buffer.from('pas un glb')).expect(400);
  await request(app).get('/api/catalog').expect(401);
});
