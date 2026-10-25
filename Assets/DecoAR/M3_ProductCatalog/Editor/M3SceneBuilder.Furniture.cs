using System.Collections.Generic;
using DecoAR.Core;
using UnityEngine;

namespace DecoAR.M3.EditorTools
{
    // Produits des pieces (salon, chambre, cuisine, salle de bain) : donnees du catalogue et modeles en primitives.
    // Meme convention que les autres produits : taille reelle (m), pivot au sol, face avant vers +Z,
    // pieces "Variant..." teintees par la variante. Aucune piece ne depasse la hauteur du catalogue
    // (M4 met le modele a l'echelle sur sa hauteur).
    public static partial class M3SceneBuilder
    {
        static readonly Color Oak = new Color(0.72f, 0.55f, 0.36f);
        static readonly Color Walnut = new Color(0.40f, 0.27f, 0.18f);
        static readonly Color Black = new Color(0.10f, 0.10f, 0.10f);
        static readonly Color White = new Color(0.95f, 0.94f, 0.90f);
        static readonly Color Chrome = new Color(0.78f, 0.80f, 0.82f);
        static readonly Color Glass = new Color(0.80f, 0.86f, 0.90f);

        static List<ProductOption> RoomProducts() => new List<ProductOption>
        {
            // --- Salon
            Product("SAL-01", "Canapé 3 places", M3Categories.Salon, 1490, new Vector3(210, 85, 90), ProductPlacement.Sol,
                "Canapé droit en velours, pieds en chêne, assise profonde.", "canape sofa salon velours",
                V("Gris anthracite", 0.30f, 0.31f, 0.33f), V("Bleu canard", 0.12f, 0.36f, 0.42f), V("Moutarde", 0.80f, 0.60f, 0.20f)),
            Product("SAL-02", "Fauteuil scandinave", M3Categories.Salon, 620, new Vector3(75, 80, 80), ProductPlacement.Sol,
                "Fauteuil enveloppant en tissu bouclé, pieds compas.", "fauteuil chaise siege salon",
                V("Beige bouclé", 0.88f, 0.84f, 0.76f), V("Vert sauge", 0.55f, 0.65f, 0.52f)),
            Product("SAL-03", "Table basse ronde", M3Categories.Salon, 390, new Vector3(80, 45, 80), ProductPlacement.Sol,
                "Table basse ronde en bois massif.", "table basse ronde salon",
                V("Chêne", Oak.r, Oak.g, Oak.b), V("Noyer", Walnut.r, Walnut.g, Walnut.b)),
            Product("SAL-04", "Meuble TV", M3Categories.Salon, 690, new Vector3(160, 50, 40), ProductPlacement.Sol,
                "Meuble TV bas à deux tiroirs et niche centrale.", "meuble tele tv television",
                V("Blanc", White.r, White.g, White.b), V("Chêne", Oak.r, Oak.g, Oak.b)),
            Product("SAL-05", "Tapis berbère", M3Categories.Salon, 280, new Vector3(160, 1, 230), ProductPlacement.Sol,
                "Tapis tissé à la main en laine, motifs géométriques.", "tapis berbere laine",
                V("Crème", 0.93f, 0.89f, 0.80f), V("Terracotta", 0.78f, 0.45f, 0.33f)),
            Product("SAL-06", "Bibliothèque", M3Categories.Salon, 540, new Vector3(80, 180, 30), ProductPlacement.Sol,
                "Bibliothèque cinq niveaux pour livres et objets.", "bibliotheque etagere livres rangement",
                V("Blanc", White.r, White.g, White.b), V("Chêne", Oak.r, Oak.g, Oak.b)),

            // --- Chambre
            Product("CHA-01", "Lit double", M3Categories.Chambre, 1850, new Vector3(160, 100, 210), ProductPlacement.Sol,
                "Lit 160 × 200 avec tête de lit capitonnée, linge de lit inclus.", "lit double chambre couchage",
                V("Lin gris", 0.62f, 0.62f, 0.60f), V("Bleu nuit", 0.16f, 0.22f, 0.38f)),
            Product("CHA-02", "Table de chevet", M3Categories.Chambre, 210, new Vector3(45, 55, 40), ProductPlacement.Sol,
                "Chevet à un tiroir et niche ouverte.", "chevet table nuit chambre",
                V("Chêne", Oak.r, Oak.g, Oak.b), V("Blanc", White.r, White.g, White.b)),
            Product("CHA-03", "Armoire 2 portes", M3Categories.Chambre, 1290, new Vector3(100, 200, 55), ProductPlacement.Sol,
                "Armoire penderie avec étagère haute.", "armoire penderie dressing vetements",
                V("Blanc", White.r, White.g, White.b), V("Chêne", Oak.r, Oak.g, Oak.b)),
            Product("CHA-04", "Commode 3 tiroirs", M3Categories.Chambre, 760, new Vector3(90, 80, 45), ProductPlacement.Sol,
                "Commode trois tiroirs, poignées en laiton.", "commode tiroirs rangement",
                V("Vert sauge", 0.55f, 0.65f, 0.52f), V("Noyer", Walnut.r, Walnut.g, Walnut.b)),
            Product("CHA-05", "Miroir sur pied", M3Categories.Chambre, 330, new Vector3(50, 170, 30), ProductPlacement.Sol,
                "Grand miroir psyché à poser au sol.", "miroir psyche pied grand",
                V("Noir mat", Black.r, Black.g, Black.b), V("Laiton", 0.80f, 0.63f, 0.30f)),
            Product("CHA-06", "Bureau", M3Categories.Chambre, 590, new Vector3(120, 75, 60), ProductPlacement.Sol,
                "Bureau compact avec caisson un tiroir.", "bureau table travail",
                V("Chêne", Oak.r, Oak.g, Oak.b), V("Blanc", White.r, White.g, White.b)),

            // --- Cuisine
            Product("CUI-01", "Table à manger", M3Categories.Cuisine, 980, new Vector3(160, 75, 90), ProductPlacement.Sol,
                "Table six personnes en bois massif.", "table manger repas salle",
                V("Chêne", Oak.r, Oak.g, Oak.b), V("Noyer", Walnut.r, Walnut.g, Walnut.b)),
            Product("CUI-02", "Chaise en bois", M3Categories.Cuisine, 180, new Vector3(45, 85, 50), ProductPlacement.Sol,
                "Chaise à barreaux, assise galbée.", "chaise bois siege",
                V("Naturel", Oak.r, Oak.g, Oak.b), V("Noir", Black.r, Black.g, Black.b)),
            Product("CUI-03", "Tabouret de bar", M3Categories.Cuisine, 150, new Vector3(40, 75, 40), ProductPlacement.Sol,
                "Tabouret haut avec repose-pieds.", "tabouret bar haut comptoir",
                V("Noir", Black.r, Black.g, Black.b), V("Laiton", 0.80f, 0.63f, 0.30f)),
            Product("CUI-04", "Îlot central", M3Categories.Cuisine, 1650, new Vector3(140, 90, 70), ProductPlacement.Sol,
                "Îlot de cuisine avec plan en marbre et rangements.", "ilot cuisine plan travail",
                V("Blanc", White.r, White.g, White.b), V("Vert sauge", 0.55f, 0.65f, 0.52f)),
            Product("CUI-05", "Étagère à épices", M3Categories.Cuisine, 85, new Vector3(60, 40, 15), ProductPlacement.Mur,
                "Étagère murale deux niveaux avec bocaux.", "etagere epices murale bocaux",
                V("Chêne", Oak.r, Oak.g, Oak.b), V("Noir", Black.r, Black.g, Black.b)),
            Product("CUI-06", "Corbeille à fruits", M3Categories.Cuisine, 40, new Vector3(30, 14, 30), ProductPlacement.Surface,
                "Corbeille en rotin tressé.", "corbeille fruits panier",
                V("Rotin", 0.76f, 0.60f, 0.38f), V("Céramique", 0.93f, 0.90f, 0.84f)),

            // --- Salle de bain
            Product("SDB-01", "Meuble vasque", M3Categories.SalleDeBain, 890, new Vector3(80, 85, 46), ProductPlacement.Sol,
                "Meuble sous vasque deux portes, vasque en céramique.", "vasque lavabo meuble salle bain",
                V("Blanc", White.r, White.g, White.b), V("Chêne", Oak.r, Oak.g, Oak.b)),
            Product("SDB-02", "Miroir de salle de bain", M3Categories.SalleDeBain, 160, new Vector3(60, 80, 3), ProductPlacement.Mur,
                "Miroir rectangulaire au cadre fin.", "miroir salle bain glace",
                V("Noir mat", Black.r, Black.g, Black.b), V("Laiton", 0.80f, 0.63f, 0.30f)),
            Product("SDB-03", "Porte-serviettes", M3Categories.SalleDeBain, 75, new Vector3(60, 30, 12), ProductPlacement.Mur,
                "Porte-serviettes mural double barre.", "porte serviette barre",
                V("Chromé", Chrome.r, Chrome.g, Chrome.b), V("Noir", Black.r, Black.g, Black.b)),
            Product("SDB-04", "Colonne de rangement", M3Categories.SalleDeBain, 420, new Vector3(35, 170, 30), ProductPlacement.Sol,
                "Colonne haute à deux portes.", "colonne rangement armoire salle bain",
                V("Blanc", White.r, White.g, White.b), V("Chêne", Oak.r, Oak.g, Oak.b)),
            Product("SDB-05", "Tapis de bain", M3Categories.SalleDeBain, 45, new Vector3(50, 2, 80), ProductPlacement.Sol,
                "Tapis de bain en coton épais.", "tapis bain coton",
                V("Gris perle", 0.78f, 0.78f, 0.76f), V("Sauge", 0.62f, 0.69f, 0.58f)),
            Product("SDB-06", "Panier à linge", M3Categories.SalleDeBain, 65, new Vector3(40, 55, 40), ProductPlacement.Sol,
                "Panier à linge avec couvercle.", "panier linge corbeille",
                V("Rotin", 0.76f, 0.60f, 0.38f), V("Gris", 0.55f, 0.56f, 0.58f)),
        };

        // ---------------------------------------------------------------- modeles

        static bool BuildFurniture(string id, GameObject root, Vector3 s, Color v0)
        {
            switch (id)
            {
                case "SAL-01": Sofa(root, s, v0, 0.10f, 0.25f, 0.15f); return true;
                case "SAL-02": Sofa(root, s, v0, 0.18f, 0.15f, 0.12f); return true;
                case "SAL-03":
                    Cyl(root, "VariantPlateau", new Vector3(0, s.y - 0.02f, 0), s.x, 0.04f, v0);
                    for (int i = 0; i < 4; i++)
                    {
                        var d = Quaternion.Euler(0, 45 + 90 * i, 0) * Vector3.forward * s.x * 0.3f;
                        Cyl(root, "Pied " + (i + 1), new Vector3(d.x, (s.y - 0.04f) / 2, d.z), 0.04f, s.y - 0.04f, v0 * 0.85f);
                    }
                    return true;
                case "SAL-04":
                    Legs(root, s, 0.1f, Black, 0.03f);
                    Box(root, "VariantCaisson", new Vector3(0, 0.1f + (s.y - 0.1f) / 2, 0), new Vector3(s.x, s.y - 0.1f, s.z), v0);
                    Box(root, "Niche", new Vector3(0, 0.1f + (s.y - 0.1f) / 2, s.z / 2 - 0.01f), new Vector3(s.x * 0.3f, s.y * 0.55f, 0.022f), Black);
                    foreach (float x in new[] { -s.x * 0.33f, s.x * 0.33f })
                        Box(root, "Poignee", new Vector3(x, s.y * 0.6f, s.z / 2 + 0.005f), new Vector3(0.12f, 0.015f, 0.01f), Black);
                    return true;
                case "SAL-05":
                    Box(root, "Bordure", new Vector3(0, s.y * 0.4f, 0), new Vector3(s.x, s.y * 0.8f, s.z), White);
                    Box(root, "VariantMotif", new Vector3(0, s.y / 2, 0), new Vector3(s.x * 0.8f, s.y, s.z * 0.85f), v0);
                    return true;
                case "SAL-06":
                    Shelving(root, s, v0, 5, true);
                    return true;

                case "CHA-01":
                    {
                        float frameH = 0.3f, mattH = 0.2f;
                        Box(root, "Sommier", new Vector3(0, frameH / 2, 0.04f), new Vector3(s.x, frameH, s.z - 0.08f), Oak);
                        Box(root, "Matelas", new Vector3(0, frameH + mattH / 2, 0.04f), new Vector3(s.x - 0.04f, mattH, s.z - 0.12f), White);
                        Box(root, "VariantCouette", new Vector3(0, frameH + mattH + 0.03f, 0.04f + s.z * 0.12f), new Vector3(s.x - 0.02f, 0.06f, s.z * 0.68f), v0);
                        foreach (float x in new[] { -s.x * 0.24f, s.x * 0.24f })
                            Box(root, "Oreiller", new Vector3(x, frameH + mattH + 0.06f, -s.z / 2 + 0.3f), new Vector3(s.x * 0.4f, 0.12f, 0.35f), White);
                        Box(root, "VariantTete", new Vector3(0, s.y / 2, -s.z / 2 + 0.04f), new Vector3(s.x + 0.04f, s.y, 0.08f), v0);
                        return true;
                    }
                case "CHA-02":
                    Legs(root, s, 0.12f, v0 * 0.85f, 0.025f);
                    Box(root, "VariantCaisson", new Vector3(0, 0.12f + (s.y - 0.12f) / 2, 0), new Vector3(s.x, s.y - 0.12f, s.z), v0);
                    Box(root, "Niche", new Vector3(0, 0.12f + (s.y - 0.12f) * 0.3f, s.z / 2 - 0.01f), new Vector3(s.x * 0.85f, (s.y - 0.12f) * 0.4f, 0.022f), Black * 1.5f);
                    Box(root, "Bouton", new Vector3(0, s.y * 0.78f, s.z / 2 + 0.01f), new Vector3(0.03f, 0.03f, 0.02f), Brass);
                    return true;
                case "CHA-03":
                    Box(root, "VariantCaisson", new Vector3(0, s.y / 2, 0), s, v0);
                    Box(root, "Joint portes", new Vector3(0, s.y / 2, s.z / 2 + 0.002f), new Vector3(0.006f, s.y * 0.94f, 0.005f), Black);
                    foreach (float x in new[] { -0.05f, 0.05f })
                        Box(root, "Poignee", new Vector3(x, s.y * 0.5f, s.z / 2 + 0.012f), new Vector3(0.015f, 0.25f, 0.02f), Brass);
                    return true;
                case "CHA-04":
                    Legs(root, s, 0.1f, Black, 0.025f);
                    Box(root, "VariantCaisson", new Vector3(0, 0.1f + (s.y - 0.1f) / 2, 0), new Vector3(s.x, s.y - 0.1f, s.z), v0);
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 0.1f + (s.y - 0.1f) * (i + 0.5f) / 3f;
                        Box(root, "Joint tiroir", new Vector3(0, 0.1f + (s.y - 0.1f) * (i + 1) / 3f, s.z / 2 + 0.002f), new Vector3(s.x * 0.96f, 0.006f, 0.005f), Black);
                        Box(root, "Poignee", new Vector3(0, y, s.z / 2 + 0.01f), new Vector3(0.14f, 0.015f, 0.02f), Brass);
                    }
                    return true;
                case "CHA-05":
                    Box(root, "VariantCadre", new Vector3(0, s.y / 2, s.z - 0.03f), new Vector3(s.x, s.y, 0.03f), v0);
                    Box(root, "Miroir", new Vector3(0, s.y / 2, s.z - 0.012f), new Vector3(s.x - 0.05f, s.y - 0.05f, 0.006f), Glass, 0.95f);
                    Box(root, "VariantBequille", new Vector3(0, s.y * 0.35f, s.z * 0.35f), new Vector3(0.03f, s.y * 0.7f, 0.02f), v0, new Vector3(-12, 0, 0));
                    return true;
                case "CHA-06":
                    Box(root, "VariantPlateau", new Vector3(0, s.y - 0.015f, 0), new Vector3(s.x, 0.03f, s.z), v0);
                    foreach (var c in Corners(s, 0.04f))
                        if (c.x < 0) Box(root, "Pied", new Vector3(c.x, (s.y - 0.03f) / 2, c.z), new Vector3(0.04f, s.y - 0.03f, 0.04f), Black);
                    Box(root, "VariantCaisson", new Vector3(s.x * 0.32f, (s.y - 0.03f) / 2, 0), new Vector3(s.x * 0.33f, s.y - 0.03f, s.z * 0.95f), v0 * 0.92f);
                    Box(root, "Poignee", new Vector3(s.x * 0.32f, s.y * 0.8f, s.z / 2 + 0.005f), new Vector3(0.12f, 0.015f, 0.01f), Black);
                    return true;

                case "CUI-01":
                    Box(root, "VariantPlateau", new Vector3(0, s.y - 0.02f, 0), new Vector3(s.x, 0.04f, s.z), v0);
                    foreach (var c in Corners(s, 0.06f))
                        Box(root, "VariantPied", new Vector3(c.x, (s.y - 0.04f) / 2, c.z), new Vector3(0.06f, s.y - 0.04f, 0.06f), v0);
                    return true;
                case "CUI-02":
                    {
                        float seatY = 0.45f;
                        Box(root, "VariantAssise", new Vector3(0, seatY - 0.02f, 0.02f), new Vector3(s.x, 0.04f, s.z - 0.04f), v0);
                        foreach (var c in Corners(new Vector3(s.x, 0, s.z), 0.035f))
                            Box(root, "VariantPied", new Vector3(c.x, (seatY - 0.04f) / 2, c.z), new Vector3(0.035f, seatY - 0.04f, 0.035f), v0);
                        foreach (float x in new[] { -s.x / 2 + 0.02f, s.x / 2 - 0.02f })
                            Box(root, "VariantMontant", new Vector3(x, (seatY + s.y) / 2, -s.z / 2 + 0.02f), new Vector3(0.035f, s.y - seatY, 0.035f), v0);
                        for (int i = 0; i < 3; i++)
                            Box(root, "VariantBarreau", new Vector3(0, s.y - 0.04f - i * 0.11f, -s.z / 2 + 0.02f), new Vector3(s.x - 0.04f, 0.035f, 0.02f), v0);
                        return true;
                    }
                case "CUI-03":
                    Cyl(root, "Socle", new Vector3(0, 0.01f, 0), s.x * 0.9f, 0.02f, Black);
                    Cyl(root, "Pied", new Vector3(0, s.y / 2, 0), 0.05f, s.y - 0.04f, Black);
                    Cyl(root, "VariantRepose-pieds", new Vector3(0, s.y * 0.38f, 0), s.x * 0.75f, 0.02f, v0);
                    Cyl(root, "VariantAssise", new Vector3(0, s.y - 0.03f, 0), s.x, 0.06f, v0);
                    return true;
                case "CUI-04":
                    Box(root, "VariantCaisson", new Vector3(0, (s.y - 0.04f) / 2, 0), new Vector3(s.x - 0.04f, s.y - 0.04f, s.z - 0.04f), v0);
                    Box(root, "Plan marbre", new Vector3(0, s.y - 0.02f, 0), new Vector3(s.x, 0.04f, s.z), new Color(0.92f, 0.92f, 0.90f), 0.8f);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = (i - 1) * (s.x - 0.04f) / 3f;
                        Box(root, "Poignee", new Vector3(x, s.y * 0.75f, s.z / 2 - 0.01f), new Vector3(0.18f, 0.015f, 0.02f), Brass);
                    }
                    return true;
                case "CUI-05":
                    Box(root, "VariantDos", new Vector3(0, s.y / 2, 0.01f), new Vector3(s.x, s.y, 0.02f), v0);
                    foreach (float y in new[] { 0.01f, s.y * 0.5f })
                    {
                        Box(root, "VariantTablette", new Vector3(0, y + 0.01f, s.z / 2), new Vector3(s.x, 0.02f, s.z - 0.02f), v0);
                        for (int i = 0; i < 4; i++)
                            Cyl(root, "Bocal", new Vector3(-s.x * 0.36f + i * s.x * 0.24f, y + 0.02f + 0.06f, s.z * 0.55f), 0.06f, 0.12f, Glass, 0.9f);
                    }
                    return true;
                case "CUI-06":
                    Part(root, "VariantCorbeille", PrimitiveType.Sphere, new Vector3(0, s.y * 0.35f, 0), new Vector3(s.x, s.y * 0.7f, s.z), v0);
                    Part(root, "Pomme", PrimitiveType.Sphere, new Vector3(-0.05f, s.y * 0.72f, 0.02f), Vector3.one * 0.07f, new Color(0.75f, 0.15f, 0.12f));
                    Part(root, "Orange", PrimitiveType.Sphere, new Vector3(0.05f, s.y * 0.72f, -0.02f), Vector3.one * 0.07f, new Color(0.95f, 0.55f, 0.10f));
                    Part(root, "Citron", PrimitiveType.Sphere, new Vector3(0, s.y * 0.75f, 0.06f), new Vector3(0.07f, 0.06f, 0.06f), new Color(0.95f, 0.85f, 0.25f));
                    return true;

                case "SDB-01":
                    Box(root, "VariantCaisson", new Vector3(0, (s.y - 0.06f) / 2, 0), new Vector3(s.x, s.y - 0.06f, s.z), v0);
                    Box(root, "Joint portes", new Vector3(0, (s.y - 0.06f) / 2, s.z / 2 + 0.002f), new Vector3(0.006f, s.y * 0.85f, 0.005f), Black);
                    Box(root, "Vasque", new Vector3(0, s.y - 0.03f, 0), new Vector3(s.x, 0.06f, s.z), White, 0.85f);
                    Box(root, "Cuvette", new Vector3(0, s.y - 0.005f, 0.03f), new Vector3(s.x * 0.6f, 0.01f, s.z * 0.55f), new Color(0.85f, 0.85f, 0.85f), 0.85f);
                    Cyl(root, "Robinet", new Vector3(0, s.y - 0.005f, -s.z / 2 + 0.06f), 0.03f, 0.01f, Chrome, 0.9f);
                    return true;
                case "SDB-02":
                    Box(root, "VariantCadre", new Vector3(0, s.y / 2, s.z / 2), s, v0);
                    Box(root, "Miroir", new Vector3(0, s.y / 2, s.z - 0.004f), new Vector3(s.x - 0.04f, s.y - 0.04f, 0.008f), Glass, 0.95f);
                    return true;
                case "SDB-03":
                    foreach (float x in new[] { -s.x / 2 + 0.02f, s.x / 2 - 0.02f })
                        Box(root, "VariantSupport", new Vector3(x, s.y / 2, s.z / 2), new Vector3(0.03f, s.y, s.z), v0);
                    foreach (float y in new[] { s.y * 0.25f, s.y * 0.8f })
                        Cyl(root, "VariantBarre", new Vector3(0, y, s.z * 0.8f), 0.02f, s.x - 0.04f, v0, new Vector3(0, 0, 90), 0.9f);
                    Box(root, "Serviette", new Vector3(0, s.y * 0.5f, s.z * 0.85f), new Vector3(s.x * 0.5f, s.y * 0.6f, 0.02f), new Color(0.90f, 0.86f, 0.78f));
                    return true;
                case "SDB-04":
                    Box(root, "VariantCaisson", new Vector3(0, s.y / 2, 0), s, v0);
                    Box(root, "Joint portes", new Vector3(0, s.y * 0.45f, s.z / 2 + 0.002f), new Vector3(s.x * 0.95f, 0.006f, 0.005f), Black);
                    foreach (float y in new[] { s.y * 0.38f, s.y * 0.55f })
                        Box(root, "Poignee", new Vector3(s.x * 0.35f, y, s.z / 2 + 0.01f), new Vector3(0.015f, 0.1f, 0.02f), Chrome);
                    return true;
                case "SDB-05":
                    Box(root, "VariantTapis", new Vector3(0, s.y / 2, 0), s, v0);
                    return true;
                case "SDB-06":
                    Cyl(root, "VariantPanier", new Vector3(0, (s.y - 0.03f) / 2, 0), s.x * 0.95f, s.y - 0.03f, v0);
                    Cyl(root, "VariantCouvercle", new Vector3(0, s.y - 0.015f, 0), s.x, 0.03f, v0 * 0.9f);
                    return true;
            }
            return false;
        }

        // Canape / fauteuil : pieds, base, coussin d'assise, dossier et accoudoirs
        static void Sofa(GameObject root, Vector3 s, Color fabric, float legH, float baseH, float armW)
        {
            Legs(root, s, legH, Oak, 0.04f);
            float baseTop = legH + baseH;
            Box(root, "VariantBase", new Vector3(0, legH + baseH / 2, 0), new Vector3(s.x, baseH, s.z), fabric);
            Box(root, "VariantCoussin", new Vector3(0, baseTop + 0.06f, 0.08f), new Vector3(s.x - 2 * armW - 0.02f, 0.12f, s.z - 0.24f), fabric * 1.08f);
            Box(root, "VariantDossier", new Vector3(0, (baseTop + s.y) / 2, -s.z / 2 + 0.1f), new Vector3(s.x, s.y - baseTop, 0.2f), fabric);
            float armTop = baseTop + (s.y - baseTop) * 0.45f;
            foreach (float x in new[] { -s.x / 2 + armW / 2, s.x / 2 - armW / 2 })
                Box(root, "VariantAccoudoir", new Vector3(x, (baseTop + armTop) / 2, 0), new Vector3(armW, armTop - baseTop, s.z), fabric);
        }

        // Etagere ouverte : montants, fond, tablettes et quelques livres
        static void Shelving(GameObject root, Vector3 s, Color wood, int levels, bool books)
        {
            const float t = 0.025f;
            foreach (float x in new[] { -s.x / 2 + t / 2, s.x / 2 - t / 2 })
                Box(root, "VariantMontant", new Vector3(x, s.y / 2, 0), new Vector3(t, s.y, s.z), wood);
            Box(root, "VariantFond", new Vector3(0, s.y / 2, -s.z / 2 + 0.005f), new Vector3(s.x, s.y, 0.01f), wood * 0.92f);
            Color[] spines = { new Color(0.62f, 0.25f, 0.20f), new Color(0.20f, 0.33f, 0.45f), new Color(0.85f, 0.75f, 0.50f), new Color(0.30f, 0.45f, 0.30f) };
            for (int i = 0; i <= levels; i++)
            {
                float y = Mathf.Lerp(t / 2, s.y - t / 2, i / (float)levels);
                Box(root, "VariantTablette", new Vector3(0, y, 0), new Vector3(s.x - 2 * t, t, s.z), wood);
                if (!books || i == levels || i % 2 == 1) continue;
                float h = s.y / levels * 0.7f;
                for (int b = 0; b < 6; b++)
                    Box(root, "Livre", new Vector3(-s.x / 2 + 0.08f + b * 0.045f, y + t / 2 + h / 2, 0.01f), new Vector3(0.035f, h * (0.85f + 0.15f * (b % 3) / 2f), s.z * 0.7f), spines[(b + i) % spines.Length]);
            }
        }

        static void Legs(GameObject root, Vector3 s, float height, Color color, float thickness)
        {
            foreach (var c in Corners(s, thickness + 0.03f))
                Box(root, "Pied", new Vector3(c.x, height / 2, c.z), new Vector3(thickness, height, thickness), color);
        }

        static IEnumerable<Vector3> Corners(Vector3 s, float inset)
        {
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1f, 1f })
                    yield return new Vector3(x * (s.x / 2 - inset), 0, z * (s.z / 2 - inset));
        }

        static GameObject Box(GameObject root, string name, Vector3 center, Vector3 size, Color color, float smoothness = 0.25f)
            => Part(root, name, PrimitiveType.Cube, center, size, color, default, smoothness);

        static GameObject Box(GameObject root, string name, Vector3 center, Vector3 size, Color color, Vector3 euler)
            => Part(root, name, PrimitiveType.Cube, center, size, color, euler);

        // Cylindre : diametre et hauteur en metres (la primitive Unity mesure 2 m de haut)
        static GameObject Cyl(GameObject root, string name, Vector3 center, float diameter, float height, Color color,
            Vector3 euler = default, float smoothness = 0.25f)
            => Part(root, name, PrimitiveType.Cylinder, center, new Vector3(diameter, height / 2, diameter), color, euler, smoothness);

        static GameObject Cyl(GameObject root, string name, Vector3 center, float diameter, float height, Color color, float smoothness)
            => Cyl(root, name, center, diameter, height, color, default, smoothness);
    }
}
