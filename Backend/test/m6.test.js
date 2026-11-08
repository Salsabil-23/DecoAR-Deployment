process.env.NODE_ENV = 'test';
process.env.JWT_SECRET = 'secret-de-test';

const { test, before, after } = require('node:test');
const assert = require('node:assert');
const http = require('http');
const zlib = require('zlib');
const request = require('supertest');
const mongoose = require('mongoose');
const { MongoMemoryServer } = require('mongodb-memory-server');
const { createApp } = require('../src/app');
const User = require('../src/models/User');
const { signJwt, signAdminToken } = require('../src/utils/tokens');

// Faux service M6 : renvoie ce qu'il a recu pour verifier que la passerelle transmet tout
const glb = Buffer.concat([Buffer.from('glTF'), Buffer.alloc(2000, 7)]);
let fake;
let received;
let mongo;
let adminHeaders;
let userHeaders;

before(async () => {
  mongo = await MongoMemoryServer.create({ instance: { launchTimeout: 60000 } });
  await mongoose.connect(mongo.getUri());
  const admin = await User.create({ firstName: 'Ada', lastName: 'Admin', email: 'ada@ex.com', passwordHash: 'x', role: 'admin' });
  const user = await User.create({ firstName: 'Bob', lastName: 'User', email: 'bob@ex.com', passwordHash: 'x' });
  adminHeaders = { Authorization: `Bearer ${signJwt(admin)}`, 'X-Admin-Token': signAdminToken(admin) };
  userHeaders = { Authorization: `Bearer ${signJwt(user)}` };

  fake = http.createServer((req, res) => {
    const chunks = [];
    req.on('data', (c) => chunks.push(c));
    req.on('end', () => {
      received = { method: req.method, url: req.url, type: req.headers['content-type'], body: Buffer.concat(chunks) };
      if (req.url === '/generate') {
        res.writeHead(202, { 'content-type': 'application/json' });
        return res.end(JSON.stringify({ job_id: 'abc123', status: 'processing' }));
      }
      if (req.url === '/status/abc123') {
        // Comme Render : reponse compressee (gzip) avec la taille compressee
        const gz = zlib.gzipSync(JSON.stringify({ job_id: 'abc123', status: 'completed', error: '' }));
        res.writeHead(200, { 'content-type': 'application/json', 'content-encoding': 'gzip', 'content-length': gz.length });
        return res.end(gz);
      }
      if (req.url === '/model/abc123' || req.url === '/test-model') {
        res.writeHead(200, { 'content-type': 'model/gltf-binary' });
        return res.end(glb);
      }
      res.writeHead(404, { 'content-type': 'application/json' });
      res.end(JSON.stringify({ error: 'Tâche introuvable' }));
    });
  });
  await new Promise((r) => fake.listen(0, '127.0.0.1', r));
  process.env.M6_SERVICE_URL = `http://127.0.0.1:${fake.address().port}`;
});

after(async () => {
  fake.close();
  await mongoose.disconnect();
  await mongo.stop();
});

const app = createApp();

test('M6 via decoar-api : envoi de l\'image (multipart) puis suivi et modele', async () => {
  const image = Buffer.from([0xff, 0xd8, 0xff, 0xe0, 1, 2, 3, 4, 5]);
  const gen = await request(app).post('/api/m6/generate').set(adminHeaders).attach('image', image, 'canape.jpg').expect(202);
  assert.deepStrictEqual(gen.body, { job_id: 'abc123', status: 'processing' });
  assert.strictEqual(received.method, 'POST');
  assert.match(received.type, /^multipart\/form-data; boundary=/);
  assert.ok(received.body.includes(image), 'l\'image doit arriver intacte');

  const status = await request(app).get('/api/m6/status/abc123').set(adminHeaders).expect(200);
  assert.strictEqual(status.body.status, 'completed');

  const model = await request(app).get('/api/m6/model/abc123').set(adminHeaders).buffer(true).parse((res, cb) => {
    const c = [];
    res.on('data', (d) => c.push(d));
    res.on('end', () => cb(null, Buffer.concat(c)));
  }).expect(200);
  assert.strictEqual(model.headers['content-type'], 'model/gltf-binary');
  assert.deepStrictEqual(model.body, glb);

  const missing = await request(app).get('/api/m6/status/inconnu').set(adminHeaders).expect(404);
  assert.strictEqual(missing.body.error, 'Tâche introuvable');
});

test('M6 via decoar-api : generation reservee a l\'espace admin', async () => {
  received = null;
  await request(app).post('/api/m6/generate').attach('image', Buffer.from([0xff, 0xd8]), 'a.jpg').expect(401);
  const user = await request(app).post('/api/m6/generate').set(userHeaders).attach('image', Buffer.from([0xff, 0xd8]), 'a.jpg').expect(403);
  assert.match(user.body.message, /administrateurs/);
  // Admin sans confirmation par le visage (pas de jeton admin)
  await request(app).get('/api/m6/status/abc123').set({ Authorization: adminHeaders.Authorization }).expect(403);
  assert.strictEqual(received, null, 'rien ne doit atteindre decoar-m6');
  await request(app).get('/api/m6/test-model').expect(200); // modele de test : libre
});

test('M6 via decoar-api : service 3D injoignable -> 503', async () => {
  const saved = process.env.M6_SERVICE_URL;
  process.env.M6_SERVICE_URL = 'http://127.0.0.1:1';
  const res = await request(app).get('/api/m6/test-model').expect(503);
  assert.match(res.body.error, /indisponible/);
  process.env.M6_SERVICE_URL = saved;
});
