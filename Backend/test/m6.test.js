process.env.NODE_ENV = 'test';
process.env.JWT_SECRET = 'secret-de-test';

const { test, before, after } = require('node:test');
const assert = require('node:assert');
const http = require('http');
const zlib = require('zlib');
const request = require('supertest');
const { createApp } = require('../src/app');

// Faux service M6 : renvoie ce qu'il a recu pour verifier que la passerelle transmet tout
const glb = Buffer.concat([Buffer.from('glTF'), Buffer.alloc(2000, 7)]);
let fake;
let received;

before(async () => {
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

after(() => fake.close());

const app = createApp();

test('M6 via decoar-api : envoi de l\'image (multipart) puis suivi et modele', async () => {
  const image = Buffer.from([0xff, 0xd8, 0xff, 0xe0, 1, 2, 3, 4, 5]);
  const gen = await request(app).post('/api/m6/generate').attach('image', image, 'canape.jpg').expect(202);
  assert.deepStrictEqual(gen.body, { job_id: 'abc123', status: 'processing' });
  assert.strictEqual(received.method, 'POST');
  assert.match(received.type, /^multipart\/form-data; boundary=/);
  assert.ok(received.body.includes(image), 'l\'image doit arriver intacte');

  const status = await request(app).get('/api/m6/status/abc123').expect(200);
  assert.strictEqual(status.body.status, 'completed');

  const model = await request(app).get('/api/m6/model/abc123').buffer(true).parse((res, cb) => {
    const c = [];
    res.on('data', (d) => c.push(d));
    res.on('end', () => cb(null, Buffer.concat(c)));
  }).expect(200);
  assert.strictEqual(model.headers['content-type'], 'model/gltf-binary');
  assert.deepStrictEqual(model.body, glb);

  const missing = await request(app).get('/api/m6/status/inconnu').expect(404);
  assert.strictEqual(missing.body.error, 'Tâche introuvable');
});

test('M6 via decoar-api : service 3D injoignable -> 503', async () => {
  const saved = process.env.M6_SERVICE_URL;
  process.env.M6_SERVICE_URL = 'http://127.0.0.1:1';
  const res = await request(app).get('/api/m6/test-model').expect(503);
  assert.match(res.body.error, /indisponible/);
  process.env.M6_SERVICE_URL = saved;
});
