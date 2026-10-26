using System.Collections.Generic;
using DecoAR.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DecoAR.Player
{
    // Applique un AvatarLook aux materiaux du personnage, reperes par leur nom
    // (Remy : Bodymat, Hairmat, Eyelashmat, Topmat, Bottommat, Shoesmat).
    public static class CharacterAppearance
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        public static Animator Spawn(CharacterProfile profile, AvatarLook look, Transform parent)
        {
            if (profile == null || profile.prefab == null) return null;

            GameObject model = Object.Instantiate(profile.prefab, parent);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            FitToHeight(model, profile.height);
            Apply(model, look, profile);

            var anim = model.GetComponentInChildren<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            if (profile.animatorController != null) anim.runtimeAnimatorController = profile.animatorController;
            anim.applyRootMotion = false;
            return anim;
        }

        public static void Apply(GameObject character, AvatarLook look, CharacterProfile profile)
        {
            // Visage scanne : tete complete FLAME (serveur) en priorite, puis masque 3D (ARCore / ARKit),
            // sinon photo projetee
            bool scan = look.visagePhoto && (profile == null || profile.useFaceScan);
            bool head3D = scan && HeadStore.Exists;
            bool mask3D = scan && !head3D && FaceMaskStore.Exists;
            Texture2D face = scan && !head3D && !mask3D ? FaceStore.Load() : null;
            bool hasFace = head3D || mask3D || face != null;

            // Avant les couleurs : les cheveux 3D de la tete scannee se teintent avec l'onglet Cheveux
            ApplyHead(character, head3D, profile);

            foreach (var r in character.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;

                // Les cils 3D du modele flotteraient devant les yeux du visage scanne
                bool lashesOnly = mats.Length > 0 && System.Array.TrueForAll(mats, m => m != null && m.name.ToLowerInvariant().Contains("eyelash"));
                if (lashesOnly) r.enabled = !hasFace;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !TryGetSlot(mats[i].name, out var slot)) continue;

                    var block = new MaterialPropertyBlock(); // bloc vide = materiau d'origine
                    Color c = look.Get(slot);
                    if (c != Color.white)
                    {
                        block.SetColor(BaseColorId, c);
                        block.SetColor(ColorId, c);
                        // Vetements : couleur unie (le relief du tissu reste grace a la normal map).
                        // Peau et cheveux : on teinte la texture pour garder les details du visage.
                        if (IsClothing(slot) && !IsOutfit(mats[i].name))
                        {
                            block.SetTexture(BaseMapId, Texture2D.whiteTexture);
                            block.SetTexture(MainTexId, Texture2D.whiteTexture);
                        }
                    }
                    r.SetPropertyBlock(block, i);
                }
            }

            ApplyMask(character, mask3D, profile);
            ApplyFace(character, face, profile);
        }

        const string HeadObjectName = "TeteFLAME";
        const float HiddenHeadScale = 0.02f;

        // Tete originale du personnage, gardee pour pouvoir la reafficher
        class HiddenHead : MonoBehaviour { public Vector3 originalScale; }

        // Remplace la tete du personnage par la tete FLAME de la personne : la tete d'origine (et ses cheveux,
        // cils) est reduite a presque rien sous l'os Head, la tete FLAME y est accrochee et suit les animations.
        // Calage : memes reperes que le masque (centre du visage au bout du nez, hauteur menton -> front).
        static void ApplyHead(GameObject character, bool show, CharacterProfile profile)
        {
            var anim = character.GetComponentInChildren<Animator>();
            Transform head = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null) return;

            var hidden = head.GetComponent<HiddenHead>();
            if (hidden != null) head.localScale = hidden.originalScale; // les reperes se mesurent tete normale
            Transform existing = head.Find(HeadObjectName);

            if (!show || !HeadStore.TryLoad(out var data, out var texture, out var mesh)
                || !TryFindFace(character, head, profile, out var anchor))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                if (show) Debug.LogWarning("[M1] Tete 3D non affichee (fichier ou visage du personnage manquant)");
                return;
            }

            if (hidden == null)
            {
                hidden = head.gameObject.AddComponent<HiddenHead>();
                hidden.originalScale = head.localScale;
            }

            MeshRenderer mr;
            if (existing == null)
            {
                var go = new GameObject(HeadObjectName);
                go.transform.SetParent(head, false);
                go.AddComponent<MeshFilter>();
                mr = go.AddComponent<MeshRenderer>();
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mr.sharedMaterial = profile != null && profile.headMaterial != null
                    ? new Material(profile.headMaterial) : new Material(shader);
                mr.sharedMaterial.SetFloat("_Smoothness", 0.3f);
                existing = go.transform;
            }
            else
            {
                existing.gameObject.SetActive(true);
                mr = existing.GetComponent<MeshRenderer>();
            }
            existing.GetComponent<MeshFilter>().sharedMesh = mesh;
            mr.sharedMaterial.SetTexture(MaskBaseMapId, texture);
            mr.sharedMaterial.SetTexture(MainTexId, texture);

            // Taille : le visage FLAME prend la hauteur du visage du personnage
            float worldScale = anchor.worldHeight / Mathf.Max(data.FaceHeight, 0.01f) * (profile != null ? profile.headScale : 1f);
            Quaternion rot = anchor.maskLocalRotation;
            float boneScale = Mathf.Abs(head.lossyScale.x);
            Vector3 local = anchor.maskLocalPosition - rot * (data.FaceOrigin * (worldScale / boneScale));
            if (profile != null) local += rot * (profile.headOffset / boneScale);

            // On reduit la tete d'origine, puis on compense pour que la tete FLAME garde sa taille
            head.localScale = hidden.originalScale * HiddenHeadScale;
            existing.localRotation = rot;
            existing.localPosition = local / HiddenHeadScale;
            existing.localScale = Vector3.one * (worldScale / boneScale / HiddenHeadScale);

            ApplyHair(existing, profile);
        }

        const string HairObjectName = "CheveuxFLAME";

        // Cheveux 3D generes par le serveur : meme repere que la tete, bord decoupe par la transparence
        static void ApplyHair(Transform headObject, CharacterProfile profile)
        {
            Transform hair = headObject.Find(HairObjectName);
            if (!HeadStore.TryLoadHair(out var texture, out var mesh) || profile == null || profile.hairMaterial == null)
            {
                if (hair != null) hair.gameObject.SetActive(false);
                return;
            }

            if (hair == null)
            {
                var go = new GameObject(HairObjectName);
                go.transform.SetParent(headObject, false);
                go.AddComponent<MeshFilter>();
                var renderer = go.AddComponent<MeshRenderer>();
                // "Hair" dans le nom : l'onglet Cheveux de la personnalisation le teinte
                renderer.sharedMaterial = new Material(profile.hairMaterial) { name = "CheveuxFLAME_Hair" };
                hair = go.transform;
            }
            hair.gameObject.SetActive(true);
            hair.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mat = hair.GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetTexture(MaskBaseMapId, texture);
            mat.SetTexture(MainTexId, texture);
        }

        static readonly int DecalBaseMapId = Shader.PropertyToID("Base_Map"); // shader URP "Shader Graphs/Decal"
        const string FaceObjectName = "VisageScanne";

        // Projette la photo du visage sur la tete (decal URP accroche a l'os Head : il suit les animations
        // et epouse le relief du visage). Necessite la feature "Decal" sur le renderer URP.
        static void ApplyFace(GameObject character, Texture2D face, CharacterProfile profile)
        {
            var anim = character.GetComponentInChildren<Animator>();
            Transform head = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform existing = head != null ? head.Find(FaceObjectName) : null;

            if (face == null || head == null || profile == null || profile.faceDecalMaterial == null)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            DecalProjector decal;
            if (existing == null)
            {
                var go = new GameObject(FaceObjectName);
                decal = go.AddComponent<DecalProjector>();
                decal.material = new Material(profile.faceDecalMaterial);
                decal.scaleMode = DecalScaleMode.ScaleInvariant;
                go.transform.SetParent(head, false);
            }
            else
            {
                existing.gameObject.SetActive(true);
                decal = existing.GetComponent<DecalProjector>();
            }

            if (!TryFindFace(character, head, profile, out var anchor))
            {
                Debug.LogWarning("[M1] Visage du personnage introuvable dans le maillage (FBX non lisible ?)");
                decal.gameObject.SetActive(false);
                return;
            }

            // Pose en pose de liaison (bind pose) : independant de l'animation en cours
            decal.transform.localPosition = anchor.localPosition;
            decal.transform.localRotation = anchor.localRotation;

            float h = anchor.worldHeight;
            decal.size = new Vector3(h * 0.74f * profile.faceScale.x, h * 1.12f * profile.faceScale.y, profile.faceDepth);
            decal.pivot = new Vector3(0f, 0f, profile.faceDepth * 0.5f);
            decal.startAngleFade = profile.faceAngleFadeStart;
            decal.endAngleFade = profile.faceAngleFadeEnd;
            decal.material.SetTexture(DecalBaseMapId, face);
        }

        const string MaskObjectName = "VisageScanne3D";
        static readonly int MaskBaseMapId = Shader.PropertyToID("_BaseMap");

        // Accroche le visage 3D scanne sur la tete : meme reperes (nez, menton, front) que le scan,
        // mis a l'echelle du visage du personnage. Il suit les animations via l'os Head.
        static void ApplyMask(GameObject character, bool show, CharacterProfile profile)
        {
            var anim = character.GetComponentInChildren<Animator>();
            Transform head = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null) return;
            Transform existing = head.Find(MaskObjectName);

            if (!show || profile == null || profile.faceMaskMaterial == null
                || !FaceMaskStore.TryLoad(out var data, out var texture, out var mesh)
                || !TryFindFace(character, head, profile, out var anchor))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                if (show) Debug.LogWarning("[M1] Visage 3D non affiche (materiau, fichier ou visage du personnage manquant)");
                return;
            }

            MeshRenderer mr;
            if (existing == null)
            {
                var go = new GameObject(MaskObjectName);
                go.transform.SetParent(head, false);
                go.AddComponent<MeshFilter>();
                mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(profile.faceMaskMaterial);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                existing = go.transform;
            }
            else
            {
                existing.gameObject.SetActive(true);
                mr = existing.GetComponent<MeshRenderer>();
            }

            existing.GetComponent<MeshFilter>().sharedMesh = mesh;
            mr.sharedMaterial.SetTexture(MaskBaseMapId, texture);

            existing.localPosition = anchor.maskLocalPosition;
            existing.localRotation = anchor.maskLocalRotation;
            // Largeur et hauteur calees separement sur le visage du personnage
            float k = 1f / Mathf.Abs(head.lossyScale.x);
            float sy = anchor.worldHeight / Mathf.Max(data.height, 0.01f);
            float sx = data.width > 0.01f && anchor.worldWidth > 0.01f ? anchor.worldWidth / data.width : sy;
            existing.localScale = new Vector3(sx * profile.faceScale.x, sy * profile.faceScale.y, (sx + sy) * 0.5f) * k;
        }

        struct FaceAnchor
        {
            public Vector3 localPosition;    // dans le repere de l'os Head
            public Quaternion localRotation;
            public float worldHeight;        // menton -> front, en metres
            public float worldWidth;         // largeur au niveau des pommettes, en metres
            public Vector3 maskLocalPosition; // centre du visage au ras du bout du nez
            public Quaternion maskLocalRotation;
        }

        // Repere le visage dans la geometrie du personnage (sous-maillage de la peau, en bind pose) :
        // bout du nez = point le plus en avant, menton et front = points avant le plus bas / le plus haut.
        static bool TryFindFace(GameObject character, Transform head, CharacterProfile profile, out FaceAnchor anchor)
        {
            anchor = default;
            Transform root = character.transform;

            foreach (var smr in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;

                int boneIndex = System.Array.IndexOf(smr.bones, head);
                int sub = System.Array.FindIndex(smr.sharedMaterials,
                    m => m != null && TryGetSlot(m.name, out var s) && s == LookSlot.Peau);
                if (boneIndex < 0 || sub < 0 || sub >= mesh.subMeshCount) continue;

                Matrix4x4 meshToHead = mesh.bindposes[boneIndex];
                Vector3 headPos = meshToHead.inverse.MultiplyPoint3x4(Vector3.zero);

                // Axes du personnage dans l'espace du maillage
                Vector3 f = smr.transform.InverseTransformDirection(root.forward).normalized;
                Vector3 u = smr.transform.InverseTransformDirection(root.up).normalized;
                Vector3 r = Vector3.Cross(u, f);
                float m = 1f / Mathf.Abs(smr.transform.lossyScale.x); // unites du maillage par metre

                var verts = mesh.vertices;
                var indices = new HashSet<int>(mesh.GetTriangles(sub));
                float hx = Vector3.Dot(headPos, r), hy = Vector3.Dot(headPos, u);

                float noseZ = float.MinValue;
                foreach (int i in indices)
                {
                    Vector3 v = verts[i];
                    float x = Vector3.Dot(v, r) - hx, y = Vector3.Dot(v, u) - hy;
                    if (y < -0.04f * m || Mathf.Abs(x) > 0.035f * m) continue;
                    noseZ = Mathf.Max(noseZ, Vector3.Dot(v, f));
                }
                if (noseZ == float.MinValue) continue;

                float chin = float.MaxValue, brow = float.MinValue;
                foreach (int i in indices)
                {
                    Vector3 v = verts[i];
                    float x = Vector3.Dot(v, r) - hx, y = Vector3.Dot(v, u) - hy;
                    if (y < -0.12f * m || Mathf.Abs(x) > 0.05f * m || Vector3.Dot(v, f) < noseZ - 0.045f * m) continue;
                    chin = Mathf.Min(chin, y);
                    brow = Mathf.Max(brow, y);
                }
                float height = brow - chin;
                if (height < 0.08f * m) continue;

                // Largeur au niveau des pommettes (meme mesure que le scan 3D)
                float minX = float.MaxValue, maxX = float.MinValue;
                foreach (int i in indices)
                {
                    Vector3 v = verts[i];
                    float y = Vector3.Dot(v, u) - hy - chin;
                    if (Vector3.Dot(v, f) < noseZ - 0.06f * m || y < height * 0.4f || y > height * 0.6f) continue;
                    float x = Vector3.Dot(v, r) - hx;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                }

                // Centre du visage, decale devant le nez : la projection part de la et va vers l'arriere
                float centerY = hy + (chin + brow) * 0.5f + profile.faceLift * height;
                Vector3 center = r * hx + u * centerY + f * (noseZ + 0.03f * m);

                anchor.localPosition = meshToHead.MultiplyPoint3x4(center);
                anchor.localRotation = Quaternion.LookRotation(meshToHead.MultiplyVector(-f), meshToHead.MultiplyVector(u));
                anchor.worldHeight = height / m;
                anchor.worldWidth = maxX > minX ? (maxX - minX) / m : 0f;
                anchor.maskLocalPosition = meshToHead.MultiplyPoint3x4(r * hx + u * centerY + f * (noseZ + 0.004f * m));
                anchor.maskLocalRotation = Quaternion.LookRotation(meshToHead.MultiplyVector(f), meshToHead.MultiplyVector(u));
                return true;
            }
            return false;
        }

        static bool IsClothing(LookSlot slot) => slot == LookSlot.Haut || slot == LookSlot.Bas || slot == LookSlot.Chaussures;

        // Tenue d'une seule piece (MetaPerson : "outfit") : on teinte sa texture, une couleur unie effacerait
        // la difference entre haut, pantalon et chaussures
        static bool IsOutfit(string materialName) => Has(materialName.ToLowerInvariant(), "outfit", "cloth");

        // Parties que le modele permet vraiment de changer (les cils seuls ne font pas des cheveux)
        public static bool HasSlot(GameObject character, LookSlot slot)
        {
            foreach (var r in character.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !TryGetSlot(m.name, out var s) || s != slot) continue;
                    if (slot == LookSlot.Cheveux && m.name.ToLowerInvariant().Contains("eyelash")) continue;
                    return true;
                }
            return false;
        }

        static bool TryGetSlot(string materialName, out LookSlot slot)
        {
            string n = materialName.ToLowerInvariant();
            if (Has(n, "hair", "eyelash", "brow")) slot = LookSlot.Cheveux;
            else if (Has(n, "shoe", "boot", "sneaker")) slot = LookSlot.Chaussures;
            else if (Has(n, "bottom", "pant", "short", "jean", "trouser")) slot = LookSlot.Bas;
            else if (Has(n, "top", "shirt", "jacket", "hoodie", "outfit", "cloth")) slot = LookSlot.Haut;
            else if (Has(n, "body", "skin", "head", "face")) slot = LookSlot.Peau;
            else { slot = LookSlot.Peau; return false; }
            return true;
        }

        static bool Has(string s, params string[] keys)
        {
            foreach (var k in keys)
                if (s.Contains(k)) return true;
            return false;
        }

        // Les FBX Mixamo n'ont pas tous la meme echelle : on ramene le personnage a une taille humaine, pieds au sol
        static void FitToHeight(GameObject model, float height)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || height <= 0f) return;

            Bounds b = GetBounds(renderers);
            if (b.size.y < 0.01f) return;

            Transform t = model.transform;
            t.localScale *= height / b.size.y;

            float ground = t.parent != null ? t.parent.position.y : 0f;
            t.position += Vector3.up * (ground - GetBounds(renderers).min.y);
        }

        static Bounds GetBounds(Renderer[] renderers)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
