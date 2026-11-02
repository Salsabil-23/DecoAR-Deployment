using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.M5
{
    /// Effet « argent qui s'envole » après le paiement : billets qui virevoltent et pièces dorées qui rebondissent,
    /// depuis la caisse enregistreuse. Tout est créé par code puis détruit.
    public class M5MoneyBurst : MonoBehaviour
    {
        class Bit
        {
            public Transform t; public Vector3 v, axis, scale; public float spin, phase, life, ttl; public bool coin; public int bounces;
        }

        static Material noteMat, coinMat;
        readonly List<Bit> bits = new List<Bit>();
        float floorY, elapsed;

        /// origin : point de départ (haut de la caisse), toward : vers le joueur (les billets partent un peu vers lui).
        public static void Spawn(Vector3 origin, Vector3 toward, float surfaceY, int notes = 26, int coins = 14)
        {
            var go = new GameObject("M5_ArgentQuiSenvole");
            go.transform.position = origin;
            go.AddComponent<M5MoneyBurst>().Init(origin, toward, surfaceY, notes, coins);
        }

        void Init(Vector3 origin, Vector3 toward, float surfaceY, int notes, int coins)
        {
            floorY = surfaceY;
            EnsureMaterials();
            toward.y = 0; toward = toward.sqrMagnitude > 0.001f ? toward.normalized : Vector3.forward;
            for (int i = 0; i < notes; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "Billet";
                q.GetComponent<Renderer>().sharedMaterial = noteMat;
                q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                q.transform.SetParent(transform, false);
                q.transform.position = origin + Random.insideUnitSphere * 0.08f;
                q.transform.rotation = Random.rotation;
                var s = new Vector3(0.16f, 0.08f, 1f) * Random.Range(0.85f, 1.1f);
                q.transform.localScale = s;
                var horiz = Random.insideUnitCircle * 1.1f;
                bits.Add(new Bit
                {
                    t = q.transform, scale = s, coin = false,
                    v = new Vector3(horiz.x, Random.Range(1.7f, 2.5f), horiz.y) * 0.8f + toward * Random.Range(1.0f, 1.8f),
                    axis = Random.onUnitSphere, spin = Random.Range(180f, 520f), phase = Random.value * 6f,
                    ttl = Random.Range(2.3f, 3.0f),
                });
            }
            for (int i = 0; i < coins; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(c.GetComponent<Collider>());
                c.name = "Piece";
                c.GetComponent<Renderer>().sharedMaterial = coinMat;
                c.transform.SetParent(transform, false);
                c.transform.position = origin + Random.insideUnitSphere * 0.05f;
                c.transform.rotation = Random.rotation;
                var s = new Vector3(0.05f, 0.004f, 0.05f);
                c.transform.localScale = s;
                var horiz = Random.insideUnitCircle * 1.3f;
                bits.Add(new Bit
                {
                    t = c.transform, scale = s, coin = true,
                    v = new Vector3(horiz.x, Random.Range(2.2f, 3.4f), horiz.y) + toward * Random.Range(0.2f, 0.6f),
                    axis = Random.onUnitSphere, spin = Random.Range(600f, 1100f), ttl = Random.Range(1.6f, 2.2f),
                });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            bool alive = false;
            foreach (var b in bits)
            {
                if (!b.t) continue;
                b.life += dt;
                if (b.coin)
                {
                    b.v.y -= 9.8f * dt;
                    b.t.position += b.v * dt;
                    if (b.t.position.y < floorY && b.v.y < 0 && b.bounces < 2)
                    {
                        var p = b.t.position; p.y = floorY; b.t.position = p;
                        b.v = new Vector3(b.v.x * 0.5f, -b.v.y * 0.35f, b.v.z * 0.5f); b.bounces++;
                    }
                }
                else
                {
                    // Billet : monte, ralentit dans l'air, plane en se balançant
                    b.v.y -= 2.8f * dt;
                    b.v.x *= 1f - 1.1f * dt; b.v.z *= 1f - 1.1f * dt;
                    if (b.v.y < -0.6f) b.v.y = -0.6f;
                    var sway = new Vector3(Mathf.Sin(b.phase + b.life * 5f), 0, Mathf.Cos(b.phase + b.life * 4f)) * 0.35f;
                    b.t.position += (b.v + sway) * dt;
                }
                b.t.Rotate(b.axis, b.spin * dt, Space.World);
                float fade = Mathf.Clamp01((b.ttl - b.life) / 0.5f);
                b.t.localScale = b.scale * fade;
                if (b.life < b.ttl) alive = true; else { Destroy(b.t.gameObject); b.t = null; }
            }
            if (!alive || elapsed > 5f) Destroy(gameObject);
        }

        static void EnsureMaterials()
        {
            if (noteMat && coinMat) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            noteMat = new Material(lit) { name = "M5_Billet" };
            noteMat.SetTexture("_BaseMap", NoteTexture());
            noteMat.SetColor("_BaseColor", Color.white);
            noteMat.SetFloat("_Cull", 0f); // visible des deux côtés
            noteMat.SetFloat("_Smoothness", 0.25f);
            coinMat = new Material(lit) { name = "M5_Piece" };
            var gold = new Color(1f, 0.78f, 0.3f);
            coinMat.SetColor("_BaseColor", gold);
            coinMat.SetFloat("_Metallic", 1f);
            coinMat.SetFloat("_Smoothness", 0.8f);
            coinMat.EnableKeyword("_EMISSION");
            coinMat.SetColor("_EmissionColor", gold * 0.6f); // scintille avec le bloom
        }

        /// Billet stylisé (vert sauge, cadre, médaillon doré, motifs).
        static Texture2D NoteTexture()
        {
            const int w = 128, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            var baseC = new Color(0.42f, 0.62f, 0.48f); var dark = new Color(0.22f, 0.40f, 0.30f);
            var light = new Color(0.80f, 0.88f, 0.76f); var gold = new Color(0.86f, 0.70f, 0.32f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = baseC;
                    bool border = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    bool inner = x > 10 && x < w - 10 && y > 10 && y < h - 10;
                    if (border) c = dark;
                    else if (inner) c = Color.Lerp(baseC, light, 0.45f + 0.08f * Mathf.Sin(x * 0.6f) * Mathf.Sin(y * 0.6f));
                    float dm = Vector2.Distance(new Vector2(x, y), new Vector2(36, h / 2));
                    if (dm < 15) c = dm > 12 ? dark : gold;
                    if (x > 64 && x < 112 && (y == 22 || y == 30 || y == 38)) c = dark;
                    float dn = Vector2.Distance(new Vector2(x, y), new Vector2(104, 16));
                    if (dn < 6) c = gold;
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }
    }
}
