using UnityEngine;

namespace DecoAR.Player
{
    // Rapproche les couleurs mesurees a la camera des teintes de la palette du personnage.
    // La camera fausse les couleurs (balance des blancs, exposition) : on compare surtout la clarte.
    public static class SkinTone
    {
        const float Exposure = 1.15f; // la camera frontale sous-expose souvent le visage

        public static Color NearestSkin(CharacterProfile profile, Color measured)
        {
            float target = Lum(measured) * Exposure;
            Color best = Color.white;
            float bestDist = float.MaxValue;
            foreach (var t in profile.skinColors)
            {
                float d = Mathf.Abs(Lum(Mul(profile.textureSkin, t)) - target);
                if (d < bestDist) { bestDist = d; best = t; }
            }
            return best;
        }

        // Cheveux : clarte surtout, et un peu la couleur (roux, blond, brun)
        public static Color NearestHair(CharacterProfile profile, Color measured)
        {
            Color m = measured * Exposure;
            Color best = Color.white;
            float bestDist = float.MaxValue;
            foreach (var t in profile.hairColors)
            {
                Color p = Mul(profile.textureHair, t);
                float dl = Lum(p) - Lum(m);
                float dc = (p.r - p.b) - (m.r - m.b);  // chaud / froid
                float d = dl * dl * 3f + dc * dc;
                if (d < bestDist) { bestDist = d; best = t; }
            }
            return best;
        }

        // Correction de la photo pour que sa peau se raccorde a celle du corps.
        // La luminosite est corrigee librement ; la teinte de +-12 % seulement, sinon la barbe,
        // les cheveux et les yeux prennent une dominante (verte, rose...) bien visible.
        public static Vector3 Gain(Color from, Color to)
        {
            float k = Mathf.Clamp(Lum(to) / Mathf.Max(Lum(from), 0.02f), 0.6f, 2f);
            float Channel(float t, float f) => k * Mathf.Clamp(t / Mathf.Max(f, 0.02f) / k, 0.88f, 1.12f);
            return new Vector3(Channel(to.r, from.r), Channel(to.g, from.g), Channel(to.b, from.b));
        }

        public static Color Mul(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b);
        public static float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}
