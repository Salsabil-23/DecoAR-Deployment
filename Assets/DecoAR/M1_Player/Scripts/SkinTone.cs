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

        // Correction par canal pour que la peau de la photo ait exactement la teinte du corps
        public static Vector3 Gain(Color from, Color to) => new Vector3(
            Mathf.Clamp(to.r / Mathf.Max(from.r, 0.02f), 0.5f, 2.5f),
            Mathf.Clamp(to.g / Mathf.Max(from.g, 0.02f), 0.5f, 2.5f),
            Mathf.Clamp(to.b / Mathf.Max(from.b, 0.02f), 0.5f, 2.5f));

        public static Color Mul(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b);
        public static float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}
