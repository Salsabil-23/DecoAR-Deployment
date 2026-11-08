const express = require('express');
const mongoose = require('mongoose');
const Product = require('../models/Product');
const { HttpError } = require('../utils/errors');
const { requireAuth, requireAdmin } = require('../middleware/auth');
const v = require('../utils/validation');

// Catalogue 3D partage : tous les utilisateurs connectes le voient, seuls les admins le modifient.
const router = express.Router();
const MAX_MODEL_BYTES = 15 * 1024 * 1024;

function productName(value) {
  const name = v.str(value);
  if (name.length < 2 || name.length > 80) throw new HttpError(400, 'Nom du produit : 2 à 80 caractères', 'name');
  return name;
}

function category(value) {
  const c = v.str(value);
  if (!Product.CATEGORIES.includes(c)) throw new HttpError(400, `Catégorie inconnue (${Product.CATEGORIES.join(', ')})`, 'category');
  return c;
}

async function findProduct(id, select = '') {
  if (!mongoose.isValidObjectId(id)) throw new HttpError(404, 'Produit introuvable');
  const product = await Product.findById(id).select(select);
  if (!product) throw new HttpError(404, 'Produit introuvable');
  return product;
}

// GET /api/catalog  -> { categories, products: [{ id, name, category, modelSize, createdAt }] }
router.get('/', requireAuth, async (req, res) => {
  const products = await Product.find().sort({ createdAt: -1 });
  res.json({ categories: Product.CATEGORIES, products: products.map((p) => p.toPublic()) });
});

// GET /api/catalog/:id/model  -> le fichier .glb
router.get('/:id/model', requireAuth, async (req, res) => {
  const product = await findProduct(req.params.id, '+model');
  res.set('Cache-Control', 'no-store').type('model/gltf-binary').send(product.model.data);
});

// POST /api/catalog?name=...&category=...   corps : le fichier .glb (application/octet-stream)
router.post('/', requireAuth, requireAdmin, express.raw({ type: () => true, limit: MAX_MODEL_BYTES }), async (req, res) => {
  const name = productName(req.query.name);
  const cat = category(req.query.category);
  const data = Buffer.isBuffer(req.body) ? req.body : Buffer.alloc(0);
  if (data.length < 12 || data.subarray(0, 4).toString() !== 'glTF')
    throw new HttpError(400, 'Fichier .glb invalide', 'model');

  const product = await Product.create({
    name,
    category: cat,
    model: { data, contentType: 'model/gltf-binary' },
    modelSize: data.length,
    createdBy: req.userId,
  });
  console.log(`[CATALOGUE] Ajout « ${name} » (${cat}, ${Math.round(data.length / 1024)} Ko)`);
  res.status(201).json({ product: product.toPublic() });
});

// PATCH /api/catalog/:id  { name?, category? }
router.patch('/:id', requireAuth, requireAdmin, async (req, res) => {
  const product = await findProduct(req.params.id);
  if (req.body?.name !== undefined) product.name = productName(req.body.name);
  if (req.body?.category !== undefined) product.category = category(req.body.category);
  await product.save();
  res.json({ product: product.toPublic() });
});

// DELETE /api/catalog/:id
router.delete('/:id', requireAuth, requireAdmin, async (req, res) => {
  const product = await findProduct(req.params.id);
  await product.deleteOne();
  console.log(`[CATALOGUE] Suppression « ${product.name} »`);
  res.json({ message: 'Produit supprimé' });
});

module.exports = router;
