const mongoose = require('mongoose');

// Produit du catalogue 3D, ajoute par un admin depuis la generation M6 (photo -> modele .glb)
const CATEGORIES = ['Salon', 'Chambre', 'Cuisine', 'Décoration', 'Luminaires', 'Tableaux'];

const productSchema = new mongoose.Schema(
  {
    name: { type: String, required: true, trim: true, maxlength: 80 },
    category: { type: String, required: true, enum: CATEGORIES },
    // Le fichier .glb lui-meme (quelques Mo ; limite MongoDB : 16 Mo par document)
    model: {
      type: new mongoose.Schema({ data: Buffer, contentType: String }, { _id: false }),
      select: false,
    },
    modelSize: { type: Number, default: 0 },
    createdBy: { type: mongoose.Schema.Types.ObjectId, ref: 'User' },
  },
  { timestamps: true }
);

productSchema.methods.toPublic = function () {
  return {
    id: this._id.toString(),
    name: this.name,
    category: this.category,
    modelSize: this.modelSize,
    createdAt: this.createdAt,
    updatedAt: this.updatedAt,
  };
};

module.exports = mongoose.model('Product', productSchema);
module.exports.CATEGORIES = CATEGORIES;
