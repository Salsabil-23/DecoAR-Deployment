process.env.NODE_ENV = 'test';
process.env.JWT_SECRET = 'secret-de-test';

const { test, before, after } = require('node:test');
const assert = require('node:assert');
const mongoose = require('mongoose');
const request = require('supertest');
const { MongoMemoryServer } = require('mongodb-memory-server');
const { createApp } = require('../src/app');

let mongo;
const app = createApp();
const alice = { firstName: 'Alice', lastName: 'Martin', email: 'Alice@Example.com', phone: '+216 22 333 444', password: 'motdepasse1' };

before(async () => {
  mongo = await MongoMemoryServer.create({ instance: { launchTimeout: 60000 } });
  await mongoose.connect(mongo.getUri());
});

after(async () => {
  await mongoose.disconnect();
  await mongo.stop();
});

test('inscription : cree le compte et renvoie un jeton', async () => {
  const res = await request(app).post('/api/auth/register').send(alice).expect(201);
  assert.ok(res.body.token);
  assert.strictEqual(res.body.user.email, 'alice@example.com');
  assert.strictEqual(res.body.user.passwordHash, undefined);
});

test('inscription : refuse un e-mail deja utilise', async () => {
  const res = await request(app).post('/api/auth/register').send(alice).expect(409);
  assert.strictEqual(res.body.field, 'email');
});

test('inscription : refuse un mot de passe faible', async () => {
  const res = await request(app).post('/api/auth/register').send({ ...alice, email: 'b@ex.com', password: 'court' }).expect(400);
  assert.strictEqual(res.body.field, 'password');
});

test('connexion par mot de passe', async () => {
  await request(app).post('/api/auth/login').send({ email: 'alice@example.com', password: 'mauvais1' }).expect(401);
  const res = await request(app).post('/api/auth/login').send({ email: 'alice@example.com', password: alice.password }).expect(200);
  const me = await request(app).get('/api/users/me').set('Authorization', `Bearer ${res.body.token}`).expect(200);
  assert.strictEqual(me.body.user.firstName, 'Alice');
});

test('Face ID : activation puis connexion avec le jeton d\'appareil', async () => {
  const login = await request(app).post('/api/auth/login').send({ email: alice.email, password: alice.password });
  const auth = `Bearer ${login.body.token}`;

  const enroll = await request(app).post('/api/auth/biometric/enroll').set('Authorization', auth)
    .send({ deviceId: 'iphone-1', deviceName: 'iPhone de test' }).expect(201);
  assert.strictEqual(enroll.body.user.faceIdEnabled, true);
  const biometricToken = enroll.body.biometricToken;

  const ok = await request(app).post('/api/auth/biometric/login')
    .send({ email: alice.email, deviceId: 'iphone-1', biometricToken }).expect(200);
  assert.ok(ok.body.token);

  await request(app).post('/api/auth/biometric/login')
    .send({ email: alice.email, deviceId: 'autre', biometricToken }).expect(401);
  await request(app).post('/api/auth/biometric/login')
    .send({ email: alice.email, deviceId: 'iphone-1', biometricToken: '0'.repeat(64) }).expect(401);

  const off = await request(app).delete('/api/auth/biometric/iphone-1').set('Authorization', auth).expect(200);
  assert.strictEqual(off.body.user.faceIdEnabled, false);
  await request(app).post('/api/auth/biometric/login')
    .send({ email: alice.email, deviceId: 'iphone-1', biometricToken }).expect(401);
});
