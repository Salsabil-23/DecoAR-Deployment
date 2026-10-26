using DecoAR.Core;
using UnityEngine;

namespace DecoAR.Player
{
    // Le personnage unique du jeu et les couleurs proposees pour chaque partie (F1.1, F1.2)
    [CreateAssetMenu(fileName = "M1_CharacterProfile", menuName = "DecoAR/M1/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        public GameObject prefab;                            // modele Mixamo (rig Humanoid)
        public RuntimeAnimatorController animatorController; // idle / marche / interaction
        public float height = 1.75f;                         // metres

        [Tooltip("Decocher si le modele est deja un avatar realiste de la personne (ex. MetaPerson) : pas de visage scanne par-dessus")]
        public bool useFaceScan = true;

        [Header("Palettes (le premier blanc = couleur d'origine)")]
        public Color[] skinColors =
        {
            Color.white,
            new Color(0.97f, 0.88f, 0.82f),
            new Color(0.88f, 0.72f, 0.60f),
            new Color(0.74f, 0.56f, 0.43f),
            new Color(0.56f, 0.40f, 0.30f),
            new Color(0.40f, 0.28f, 0.21f),
            new Color(0.28f, 0.20f, 0.16f),
        };
        public Color[] hairColors =
        {
            Color.white,
            new Color(0.62f, 0.45f, 0.28f),
            new Color(0.38f, 0.26f, 0.16f),
            new Color(0.20f, 0.14f, 0.10f),
            new Color(0.10f, 0.09f, 0.09f),
            new Color(0.85f, 0.42f, 0.22f),
            new Color(0.82f, 0.80f, 0.78f),
        };
        public Color[] topColors =
        {
            Color.white,
            new Color(0.95f, 0.93f, 0.89f),
            new Color(0.78f, 0.40f, 0.29f),
            new Color(0.54f, 0.60f, 0.48f),
            new Color(0.85f, 0.65f, 0.28f),
            new Color(0.22f, 0.29f, 0.45f),
            new Color(0.82f, 0.56f, 0.60f),
            new Color(0.20f, 0.20f, 0.22f),
        };
        public Color[] bottomColors =
        {
            Color.white,
            new Color(0.88f, 0.82f, 0.72f),
            new Color(0.25f, 0.33f, 0.50f),
            new Color(0.38f, 0.42f, 0.32f),
            new Color(0.45f, 0.32f, 0.24f),
            new Color(0.18f, 0.18f, 0.20f),
        };
        public Color[] shoesColors =
        {
            Color.white,
            new Color(0.96f, 0.95f, 0.93f),
            new Color(0.48f, 0.32f, 0.22f),
            new Color(0.16f, 0.16f, 0.17f),
            new Color(0.78f, 0.40f, 0.29f),
            new Color(0.25f, 0.33f, 0.50f),
        };

        [Header("Visage scanne : projection sur la tete (reglable en Play)")]
        public Material faceMaskMaterial;                          // visage 3D : URP Lit transparent
        public Material faceDecalMaterial;                         // scan photo : shader URP "Shader Graphs/Decal"
        public Vector2 faceScale = Vector2.one;          // largeur, hauteur (1 = cale automatiquement sur le visage)
        [Range(-0.3f, 0.3f)] public float faceLift = 0f;  // monter / descendre, en part de la hauteur du visage
        public float faceDepth = 0.12f;                   // profondeur de projection (m)
        [Range(0f, 180f)] public float faceAngleFadeStart = 40f;   // le visage s'estompe sur les cotes de la tete
        [Range(0f, 180f)] public float faceAngleFadeEnd = 72f;

        [Header("Tete complete FLAME (M1_FaceServer) : reglage sur le personnage")]
        public Material headMaterial;                    // vide = URP Lit
        public Material hairMaterial;                    // URP Lit, decoupe alpha, double face (cree par le menu M1)
        [Range(0.6f, 1.6f)] public float headScale = 1f;
        public Vector3 headOffset = Vector3.zero;        // metres, repere du visage (x droite, y haut, z avant)

        [Header("Couleurs moyennes des textures d'origine (pour le scan facial)")]
        public Color textureSkin = new Color(0.86f, 0.67f, 0.56f);
        public Color textureHair = new Color(0.70f, 0.55f, 0.36f);

        // Modifier le profil pendant le Play met a jour le personnage tout de suite (reglage du visage)
        public static event System.Action<CharacterProfile> Changed;
        void OnValidate() => Changed?.Invoke(this);

        public Color[] Palette(LookSlot slot)
        {
            switch (slot)
            {
                case LookSlot.Peau: return skinColors;
                case LookSlot.Cheveux: return hairColors;
                case LookSlot.Haut: return topColors;
                case LookSlot.Bas: return bottomColors;
                default: return shoesColors;
            }
        }
    }
}
