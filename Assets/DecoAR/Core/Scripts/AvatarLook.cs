using System;
using UnityEngine;

namespace DecoAR.Core
{
    // Parties du personnage personnalisables
    public enum LookSlot { Peau, Cheveux, Haut, Bas, Chaussures }

    // Couleurs choisies par le joueur. Blanc = couleur / motif d'origine du modele.
    [Serializable]
    public class AvatarLook
    {
        public Color peau = Color.white;
        public Color cheveux = Color.white;
        public Color haut = Color.white;
        public Color bas = Color.white;
        public Color chaussures = Color.white;
        public bool visagePhoto;   // le visage scanne remplace celui du personnage

        public Color Get(LookSlot slot)
        {
            switch (slot)
            {
                case LookSlot.Peau: return peau;
                case LookSlot.Cheveux: return cheveux;
                case LookSlot.Haut: return haut;
                case LookSlot.Bas: return bas;
                default: return chaussures;
            }
        }

        public void Set(LookSlot slot, Color color)
        {
            switch (slot)
            {
                case LookSlot.Peau: peau = color; break;
                case LookSlot.Cheveux: cheveux = color; break;
                case LookSlot.Haut: haut = color; break;
                case LookSlot.Bas: bas = color; break;
                default: chaussures = color; break;
            }
        }

        public AvatarLook Clone() => (AvatarLook)MemberwiseClone();
    }
}
