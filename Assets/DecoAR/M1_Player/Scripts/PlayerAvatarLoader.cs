using DecoAR.Core;
using UnityEngine;

namespace DecoAR.Player
{
    // Remplace la capsule provisoire par l'avatar choisi dans l'ecran de selection
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAvatarLoader : MonoBehaviour
    {
        [SerializeField] AvatarCatalog catalog;
        [SerializeField] Transform avatarMount;
        [SerializeField] GameObject placeholder;

        void Awake()
        {
            var gm = GameManager.Instance;
            int index = gm != null ? gm.AvatarIndex : 0;
            Color color = gm != null ? gm.OutfitColor : Color.white;

            Animator anim = AvatarAppearance.Spawn(catalog, index, color, avatarMount);
            if (anim == null)
            {
                Debug.LogWarning("[M1] Aucun avatar dans le catalogue : la capsule reste affichee");
                return;
            }

            if (placeholder != null) placeholder.SetActive(false);
            GetComponent<PlayerController>().SetAnimator(anim);
        }
    }
}
