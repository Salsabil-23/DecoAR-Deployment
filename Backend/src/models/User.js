const mongoose = require('mongoose');

// Code a 6 chiffres envoye par e-mail (hash uniquement)
const emailCodeSchema = new mongoose.Schema(
  {
    hash: String,
    expiresAt: Date,
    attempts: { type: Number, default: 0 },
    sentAt: Date,
  },
  { _id: false }
);

const userSchema = new mongoose.Schema(
  {
    firstName: { type: String, required: true, trim: true, maxlength: 50 },
    lastName: { type: String, required: true, trim: true, maxlength: 50 },
    email: { type: String, required: true, unique: true, lowercase: true, trim: true },
    phone: { type: String, trim: true, default: '' },
    passwordHash: { type: String, required: true, select: false },
    emailVerified: { type: Boolean, default: false },
    verifyCode: { type: emailCodeSchema, select: false },
    resetCode: { type: emailCodeSchema, select: false },
    // Reconnaissance faciale (OpenCV SFace) : vecteur de 128 nombres calcule a l'inscription.
    // On ne garde pas les photos du visage, seulement ce vecteur.
    faceEmbedding: { type: [Number], default: undefined, select: false },
    faceEnrolledAt: { type: Date },
    // Photo de profil (JPEG 256 px), servie par GET /api/users/me/photo
    photo: {
      type: new mongoose.Schema({ data: Buffer, contentType: String }, { _id: false }),
      select: false,
    },
    photoUpdatedAt: { type: Date },
  },
  { timestamps: true }
);

// Format renvoye a Unity (jamais les hash, le vecteur visage ni la photo elle-meme)
userSchema.methods.toPublic = function () {
  return {
    id: this._id.toString(),
    firstName: this.firstName,
    lastName: this.lastName,
    email: this.email,
    phone: this.phone,
    emailVerified: this.emailVerified,
    faceEnrolled: Boolean(this.faceEnrolledAt),
    faceEnrolledAt: this.faceEnrolledAt,
    hasPhoto: Boolean(this.photoUpdatedAt),
    photoUpdatedAt: this.photoUpdatedAt,
    createdAt: this.createdAt,
  };
};

module.exports = mongoose.model('User', userSchema);
