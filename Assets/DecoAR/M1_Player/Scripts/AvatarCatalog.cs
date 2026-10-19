using System;
using UnityEngine;

namespace DecoAR.Player
{
    [Serializable]
    public class AvatarOption
    {
        public string displayName = "Avatar";
        public GameObject prefab;   // modele Mixamo (rig Humanoid)
    }

    // Liste des avatars proposes (F1.1) et des couleurs de tenue (F1.2)
    [CreateAssetMenu(fileName = "M1_AvatarCatalog", menuName = "DecoAR/M1/Avatar Catalog")]
    public class AvatarCatalog : ScriptableObject
    {
        public AvatarOption[] avatars = new AvatarOption[3];
        public Color[] outfitColors =
        {
            Color.white,
            new Color(0.15f, 0.15f, 0.18f),
            new Color(0.80f, 0.25f, 0.25f),
            new Color(0.25f, 0.45f, 0.80f),
            new Color(0.30f, 0.65f, 0.40f),
            new Color(0.90f, 0.70f, 0.30f),
        };
        public RuntimeAnimatorController animatorController; // idle / marche / interaction
        public float avatarHeight = 1.75f;                   // metres, applique a tous les avatars

        public AvatarOption Get(int index)
        {
            if (avatars == null || avatars.Length == 0) return null;
            return avatars[Mathf.Clamp(index, 0, avatars.Length - 1)];
        }
    }
}
