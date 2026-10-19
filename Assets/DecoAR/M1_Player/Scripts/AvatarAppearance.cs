using UnityEngine;

namespace DecoAR.Player
{
    // A ajouter sur un prefab d'avatar pour indiquer quels meshes forment la tenue.
    // Sans ce composant : on teinte les materiaux dont le nom evoque un haut (ex. "Topmat" de Remy),
    // et a defaut tout l'avatar (X Bot, Y Bot).
    public class AvatarAppearance : MonoBehaviour
    {
        [SerializeField] Renderer[] outfitRenderers;

        static readonly string[] OutfitKeywords = { "top", "shirt", "outfit", "cloth" };

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in

        public static void ApplyOutfitColor(GameObject avatar, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);

            if (avatar.TryGetComponent<AvatarAppearance>(out var a) && a.outfitRenderers.Length > 0)
            {
                foreach (var r in a.outfitRenderers)
                    if (r != null) r.SetPropertyBlock(block);
                return;
            }

            var renderers = avatar.GetComponentsInChildren<Renderer>();
            bool found = false;
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !IsOutfit(mats[i].name)) continue;
                    r.SetPropertyBlock(block, i);
                    found = true;
                }
            }
            if (found) return;

            foreach (var r in renderers) r.SetPropertyBlock(block);
        }

        static bool IsOutfit(string materialName)
        {
            string n = materialName.ToLowerInvariant();
            foreach (var k in OutfitKeywords)
                if (n.Contains(k)) return true;
            return false;
        }

        // Instancie l'avatar sous parent, avec le controleur d'animation commun
        public static Animator Spawn(AvatarCatalog catalog, int index, Color color, Transform parent)
        {
            var option = catalog != null ? catalog.Get(index) : null;
            if (option == null || option.prefab == null) return null;

            GameObject model = Object.Instantiate(option.prefab, parent);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            FitToHeight(model, catalog.avatarHeight);
            ApplyOutfitColor(model, color);

            var anim = model.GetComponentInChildren<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            if (catalog.animatorController != null) anim.runtimeAnimatorController = catalog.animatorController;
            anim.applyRootMotion = false;
            return anim;
        }

        // Les FBX Mixamo n'ont pas tous la meme echelle : on ramene l'avatar a une taille humaine, pieds au sol
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
