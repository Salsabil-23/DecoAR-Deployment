using DecoAR.Core;
using UnityEngine;

namespace DecoAR.Player
{
    // Remplace la capsule provisoire par le personnage, avec le look choisi a la personnalisation
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAvatarLoader : MonoBehaviour
    {
        [SerializeField] CharacterProfile profile;
        [SerializeField] Transform avatarMount;
        [SerializeField] GameObject placeholder;

        void Awake()
        {
            var gm = GameManager.Instance;
            AvatarLook look = gm != null ? gm.Look : new AvatarLook();

            Animator anim = CharacterAppearance.Spawn(profile, look, avatarMount);
            if (anim == null)
            {
                Debug.LogWarning("[M1] Aucun personnage dans le profil : la capsule reste affichee");
                return;
            }

            if (placeholder != null) placeholder.SetActive(false);
            GetComponent<PlayerController>().SetAnimator(anim);
        }
    }
}
