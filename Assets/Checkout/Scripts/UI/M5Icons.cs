using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.M5
{
    /// Icônes du HUD dessinées par code (champs de distance signée) : nettes, sans fichier image, teintables.
    public static class M5Icons
    {
        public enum Kind { Catalogue, Panier, Reglages, Profil, Fermer, Plus, Moins, Position, Chevron, Recentrer }

        const int Size = 128;
        static readonly Dictionary<Kind, Sprite> cache = new Dictionary<Kind, Sprite>();

        public static Sprite Get(Kind k)
        {
            if (cache.TryGetValue(k, out var s) && s) return s;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float a = Mathf.Clamp01(0.5f - Shape(k, p) * Size);
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100);
            cache[k] = s;
            return s;
        }

        /// Distance signée (négative à l'intérieur), coordonnées 0..1, y vers le haut.
        static float Shape(Kind k, Vector2 p)
        {
            switch (k)
            {
                case Kind.Catalogue: // grille 2 × 2
                    return Min(Box(p, new Vector2(0.31f, 0.31f), V(0.135f), 0.05f), Box(p, new Vector2(0.69f, 0.31f), V(0.135f), 0.05f),
                               Box(p, new Vector2(0.31f, 0.69f), V(0.135f), 0.05f), Box(p, new Vector2(0.69f, 0.69f), V(0.135f), 0.05f));
                case Kind.Panier: // sac avec anse
                {
                    float body = Box(p, new Vector2(0.5f, 0.37f), new Vector2(0.28f, 0.23f), 0.06f);
                    float handle = Mathf.Max(Ring(p, new Vector2(0.5f, 0.6f), 0.14f, 0.055f), 0.6f - p.y);
                    return Mathf.Min(body, handle);
                }
                case Kind.Reglages: // curseurs
                    return Min(Seg(p, new Vector2(0.17f, 0.30f), new Vector2(0.83f, 0.30f), 0.028f), Circle(p, new Vector2(0.64f, 0.30f), 0.08f),
                               Seg(p, new Vector2(0.17f, 0.50f), new Vector2(0.83f, 0.50f), 0.028f), Circle(p, new Vector2(0.35f, 0.50f), 0.08f),
                               Seg(p, new Vector2(0.17f, 0.70f), new Vector2(0.83f, 0.70f), 0.028f), Circle(p, new Vector2(0.70f, 0.70f), 0.08f));
                case Kind.Profil: // tête + épaules
                    return Mathf.Min(Circle(p, new Vector2(0.5f, 0.66f), 0.155f), Box(p, new Vector2(0.5f, 0.24f), new Vector2(0.29f, 0.14f), 0.14f));
                case Kind.Fermer:
                    return Mathf.Min(Seg(p, V(0.3f), V(0.7f), 0.045f), Seg(p, new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.3f), 0.045f));
                case Kind.Plus:
                    return Mathf.Min(Seg(p, new Vector2(0.5f, 0.27f), new Vector2(0.5f, 0.73f), 0.05f), Seg(p, new Vector2(0.27f, 0.5f), new Vector2(0.73f, 0.5f), 0.05f));
                case Kind.Moins:
                    return Seg(p, new Vector2(0.27f, 0.5f), new Vector2(0.73f, 0.5f), 0.05f);
                case Kind.Position: // repère de carte
                {
                    float pin = Mathf.Min(Circle(p, new Vector2(0.5f, 0.6f), 0.22f), Triangle(p, new Vector2(0.31f, 0.5f), new Vector2(0.69f, 0.5f), new Vector2(0.5f, 0.12f)));
                    return Mathf.Max(pin, -Circle(p, new Vector2(0.5f, 0.6f), 0.085f));
                }
                case Kind.Chevron:
                    return Mathf.Min(Seg(p, new Vector2(0.4f, 0.27f), new Vector2(0.63f, 0.5f), 0.055f), Seg(p, new Vector2(0.63f, 0.5f), new Vector2(0.4f, 0.73f), 0.055f));
                case Kind.Recentrer: // viseur
                    return Min(Ring(p, V(0.5f), 0.24f, 0.05f), Circle(p, V(0.5f), 0.07f),
                               Seg(p, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.22f), 0.03f), Seg(p, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.92f), 0.03f),
                               Seg(p, new Vector2(0.08f, 0.5f), new Vector2(0.22f, 0.5f), 0.03f), Seg(p, new Vector2(0.78f, 0.5f), new Vector2(0.92f, 0.5f), 0.03f));
            }
            return 1f;
        }

        static Vector2 V(float v) => new Vector2(v, v);
        static float Min(params float[] d) { float m = d[0]; for (int i = 1; i < d.Length; i++) m = Mathf.Min(m, d[i]); return m; }
        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Ring(Vector2 p, Vector2 c, float r, float thickness) => Mathf.Abs((p - c).magnitude - r) - thickness * 0.5f;

        static float Box(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + V(r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        /// Triangle (sommets dans n'importe quel ordre) : distance aux côtés, suffisante pour l'anticrénelage.
        static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s = Mathf.Sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
            return Mathf.Max(Edge(p, a, b, s), Edge(p, b, c, s), Edge(p, c, a, s));
        }

        static float Edge(Vector2 p, Vector2 a, Vector2 b, float s)
        {
            var e = b - a;
            var n = new Vector2(e.y, -e.x).normalized * s; // normale extérieure
            return Vector2.Dot(p - a, n);
        }
    }
}
