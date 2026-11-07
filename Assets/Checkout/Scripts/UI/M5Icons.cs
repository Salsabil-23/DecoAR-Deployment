using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.M5
{
    /// Icônes du HUD dessinées par code (champs de distance signée) : nettes, sans fichier image, teintables.
    public static class M5Icons
    {
        public enum Kind { Catalogue, Panier, Reglages, Profil, Fermer, Plus, Moins, Position, Chevron, Recentrer, Idee, Oeil, Personnage,
            Loupe, Micro, Cube, Photo, Variante, Taille, Retour, Deplacer, Pivoter, Pincer, Toucher, Cadenas }

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
                case Kind.Idee: // étincelles (suggestions)
                    return Min(Diamond(p, new Vector2(0.42f, 0.44f), 0.30f, 0.075f), Diamond(p, new Vector2(0.42f, 0.44f), 0.075f, 0.30f),
                               Diamond(p, new Vector2(0.76f, 0.78f), 0.13f, 0.035f), Diamond(p, new Vector2(0.76f, 0.78f), 0.035f, 0.13f),
                               Circle(p, new Vector2(0.78f, 0.22f), 0.05f));
                case Kind.Oeil: // vue 1ère personne : contour d'œil + pupille
                {
                    float lens = Mathf.Max(Circle(p, new Vector2(0.5f, 0.16f), 0.47f), Circle(p, new Vector2(0.5f, 0.84f), 0.47f));
                    return Mathf.Min(Mathf.Abs(lens) - 0.03f, Circle(p, V(0.5f), 0.11f));
                }
                case Kind.Personnage: // vue 3ème personne : silhouette en pied
                    return Min(Circle(p, new Vector2(0.5f, 0.8f), 0.105f), Box(p, new Vector2(0.5f, 0.52f), new Vector2(0.15f, 0.15f), 0.07f),
                               Seg(p, new Vector2(0.44f, 0.38f), new Vector2(0.43f, 0.1f), 0.055f), Seg(p, new Vector2(0.56f, 0.38f), new Vector2(0.57f, 0.1f), 0.055f));
                case Kind.Loupe:
                    return Mathf.Min(Ring(p, new Vector2(0.44f, 0.56f), 0.2f, 0.075f), Seg(p, new Vector2(0.59f, 0.41f), new Vector2(0.8f, 0.2f), 0.05f));
                case Kind.Micro:
                {
                    float arc = Mathf.Max(Ring(p, new Vector2(0.5f, 0.56f), 0.23f, 0.055f), p.y - 0.56f);
                    return Min(Seg(p, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.7f), 0.12f), arc,
                               Seg(p, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.17f), 0.03f), Seg(p, new Vector2(0.36f, 0.16f), new Vector2(0.64f, 0.16f), 0.03f));
                }
                case Kind.Cube: // AR : cube en perspective
                {
                    Vector2 t = new Vector2(0.5f, 0.86f), ur = new Vector2(0.81f, 0.68f), lr = new Vector2(0.81f, 0.32f), b = new Vector2(0.5f, 0.14f),
                            ll = new Vector2(0.19f, 0.32f), ul = new Vector2(0.19f, 0.68f), c = new Vector2(0.5f, 0.5f);
                    const float w = 0.036f;
                    return Min(Seg(p, t, ur, w), Seg(p, ur, lr, w), Seg(p, lr, b, w), Seg(p, b, ll, w), Seg(p, ll, ul, w), Seg(p, ul, t, w),
                               Seg(p, c, ul, w), Seg(p, c, ur, w), Seg(p, c, b, w));
                }
                case Kind.Photo:
                {
                    float body = Mathf.Min(Box(p, new Vector2(0.5f, 0.44f), new Vector2(0.34f, 0.24f), 0.07f), Box(p, new Vector2(0.4f, 0.71f), new Vector2(0.11f, 0.06f), 0.03f));
                    return Mathf.Min(Mathf.Max(body, -Circle(p, new Vector2(0.5f, 0.44f), 0.15f)), Circle(p, new Vector2(0.5f, 0.44f), 0.085f));
                }
                case Kind.Variante: // trois pastilles de couleur
                    return Min(Circle(p, new Vector2(0.37f, 0.6f), 0.15f), Circle(p, new Vector2(0.63f, 0.6f), 0.15f), Circle(p, new Vector2(0.5f, 0.37f), 0.15f));
                case Kind.Taille: // double flèche en diagonale
                    return Min(Seg(p, new Vector2(0.27f, 0.27f), new Vector2(0.73f, 0.73f), 0.04f),
                               Seg(p, new Vector2(0.76f, 0.76f), new Vector2(0.52f, 0.76f), 0.04f), Seg(p, new Vector2(0.76f, 0.76f), new Vector2(0.76f, 0.52f), 0.04f),
                               Seg(p, new Vector2(0.24f, 0.24f), new Vector2(0.48f, 0.24f), 0.04f), Seg(p, new Vector2(0.24f, 0.24f), new Vector2(0.24f, 0.48f), 0.04f));
                case Kind.Retour:
                    return Mathf.Min(Seg(p, new Vector2(0.6f, 0.27f), new Vector2(0.37f, 0.5f), 0.055f), Seg(p, new Vector2(0.37f, 0.5f), new Vector2(0.6f, 0.73f), 0.055f));
                case Kind.Deplacer: // croix fléchée
                {
                    const float w = 0.035f;
                    return Min(Seg(p, new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.5f), w), Seg(p, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.85f), w),
                               Seg(p, new Vector2(0.15f, 0.5f), new Vector2(0.26f, 0.61f), w), Seg(p, new Vector2(0.15f, 0.5f), new Vector2(0.26f, 0.39f), w),
                               Seg(p, new Vector2(0.85f, 0.5f), new Vector2(0.74f, 0.61f), w), Seg(p, new Vector2(0.85f, 0.5f), new Vector2(0.74f, 0.39f), w),
                               Seg(p, new Vector2(0.5f, 0.85f), new Vector2(0.39f, 0.74f), w), Seg(p, new Vector2(0.5f, 0.85f), new Vector2(0.61f, 0.74f), w),
                               Seg(p, new Vector2(0.5f, 0.15f), new Vector2(0.39f, 0.26f), w), Seg(p, new Vector2(0.5f, 0.15f), new Vector2(0.61f, 0.26f), w));
                }
                case Kind.Pivoter: // flèche circulaire
                {
                    float arc = Mathf.Max(Ring(p, V(0.5f), 0.27f, 0.06f), -Mathf.Max(0.5f - p.x, 0.5f - p.y)); // quart haut droit ouvert
                    return Mathf.Min(arc, Triangle(p, new Vector2(0.66f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0.77f, 0.67f)));
                }
                case Kind.Pincer: // deux doigts qui s'écartent
                    return Min(Circle(p, new Vector2(0.4f, 0.4f), 0.085f), Circle(p, new Vector2(0.6f, 0.6f), 0.085f),
                               Seg(p, new Vector2(0.29f, 0.29f), new Vector2(0.14f, 0.14f), 0.035f), Seg(p, new Vector2(0.14f, 0.14f), new Vector2(0.28f, 0.14f), 0.035f), Seg(p, new Vector2(0.14f, 0.14f), new Vector2(0.14f, 0.28f), 0.035f),
                               Seg(p, new Vector2(0.71f, 0.71f), new Vector2(0.86f, 0.86f), 0.035f), Seg(p, new Vector2(0.86f, 0.86f), new Vector2(0.72f, 0.86f), 0.035f), Seg(p, new Vector2(0.86f, 0.86f), new Vector2(0.86f, 0.72f), 0.035f));
                case Kind.Cadenas:
                {
                    float body = Mathf.Max(Box(p, new Vector2(0.5f, 0.36f), new Vector2(0.27f, 0.21f), 0.06f), -Circle(p, new Vector2(0.5f, 0.38f), 0.055f));
                    float arc = Mathf.Max(Ring(p, new Vector2(0.5f, 0.6f), 0.16f, 0.065f), 0.6f - p.y);
                    return Min(body, arc, Seg(p, new Vector2(0.34f, 0.6f), new Vector2(0.34f, 0.52f), 0.0325f), Seg(p, new Vector2(0.66f, 0.6f), new Vector2(0.66f, 0.52f), 0.0325f));
                }
                case Kind.Toucher: // doigt + onde
                    return Mathf.Min(Circle(p, V(0.5f), 0.13f), Ring(p, V(0.5f), 0.3f, 0.045f));
            }
            return 1f;
        }

        /// Losange (demi-largeur a, demi-hauteur b).
        static float Diamond(Vector2 p, Vector2 c, float a, float b)
        {
            float k = (Mathf.Abs(p.x - c.x) / a + Mathf.Abs(p.y - c.y) / b - 1f);
            return k * a * b / Mathf.Sqrt(a * a + b * b);
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
