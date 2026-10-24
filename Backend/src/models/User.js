const mongoose = require('mongoose');

// Un appareil autorise a se connecter par Face ID / empreinte.
// Le visage ne quitte jamais le telephone : le capteur valide localement,
// puis le telephone envoie un jeton d'appareil dont on ne garde que le hash.
const biometricDeviceSchema = new mongoose.Schema(
  {
    deviceId: { type: String, required: true },
    deviceName: { type: String, default: '' },
    tokenHash: { type: String, required: true },
    lastUsedAt: { type: Date },
  },
  { _id: false, timestamps: { createdAt: true, updatedAt: false } }
);

const userSchema = new mongoose.Schema(
  {
    firstName: { type: String, required: true, trim: true, maxlength: 50 },
    lastName: { type: String, required: true, trim: true, maxlength: 50 },
    email: { type: String, required: true, unique: true, lowercase: true, trim: true },
    phone: { type: String, trim: true, default: '' },
    passwordHash: { type: String, required: true, select: false },
    biometricDevices: { type: [biometricDeviceSchema], default: [], select: false },
    faceIdEnabled: { type: Boolean, default: false },
  },
  { timestamps: true }
);

// Format renvoye a Unity (jamais le hash du mot de passe)
userSchema.methods.toPublic = function () {
  return {
    id: this._id.toString(),
    firstName: this.firstName,
    lastName: this.lastName,
    email: this.email,
    phone: this.phone,
    faceIdEnabled: this.faceIdEnabled,
    createdAt: this.createdAt,
  };
};

module.exports = mongoose.model('User', userSchema);
