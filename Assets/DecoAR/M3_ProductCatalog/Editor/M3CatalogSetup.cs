using System.Collections.Generic;
using System.IO;
using System.Linq;
using DecoAR.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DecoAR.M3.EditorTools
{
    // Catalogue M3 construit avec les meubles des deux packs (ithappy Cute Furniture, Low Poly Living Room) :
    //  1. les prefabs utilisés et leurs dépendances quittent les dossiers des packs (jamais sur GitHub)
    //     pour M3_ProductCatalog/Meubles ;
    //  2. quelques ensembles sont assemblés (rangée de livres, magazines, brosse à dents, clavier + souris) ;
    //  3. le catalogue (M3_ProductCatalog.asset) est réécrit : 106 produits, 10 rayons ;
    //  4. la scène M3 reçoit le décor, l'étagère DecoAR (deux bibliothèques du pack retravaillées) et la caméra.
    public static class M3CatalogSetup
    {
        const string Root = "Assets/DecoAR/M3_ProductCatalog";
        const string Meubles = Root + "/Meubles";
        const string Ensembles = Root + "/Prefabs/Ensembles";
        const string MatFolder = Root + "/Materials";
        const string CatalogPath = Root + "/Data/M3_ProductCatalog.asset";
        const string ScenePath = Root + "/Scenes/M3.unity";
        const string LogoPath = "Assets/DecoAR/Core/Icons/DecoAR_Icon.png";

        static readonly (string from, string to)[] Packs =
        {
            ("Assets/ithappy/Cute_Furniture_Free", Meubles + "/ithappy"),
            ("Assets/LowPolyLivingRoomPack", Meubles + "/LowPolyLivingRoom"),
        };
        static readonly string[] SearchRoots = { "Assets/ithappy", "Assets/LowPolyLivingRoomPack", "Assets/Checkout/Boutique", Meubles, Ensembles };

        enum Tint { Aucune, Tissu, Bois, Deco }

        // prefab, id, nom, rayon, prix (TND), plus grande dimension réelle (cm), pose, teintes, mots-clés
        static readonly (string prefab, string id, string name, string cat, float price, float cm, ProductPlacement place, Tint tint, string kw)[] Products =
        {
            ("Couch_08", "SAL-01", "Canapé Lounge 3 places", M3Categories.Salon, 1890, 221, ProductPlacement.Sol, Tint.Tissu, "canape sofa lounge trois places"),
            ("Couch_11", "SAL-02", "Canapé Cosy 2 places", M3Categories.Salon, 1390, 160, ProductPlacement.Sol, Tint.Tissu, "canape sofa cosy deux places"),
            ("Sofa_2Seat", "SAL-03", "Canapé rétro", M3Categories.Salon, 1190, 150, ProductPlacement.Sol, Tint.Tissu, "canape sofa retro vintage"),
            ("Armchair_02", "SAL-04", "Fauteuil club", M3Categories.Salon, 690, 100, ProductPlacement.Sol, Tint.Tissu, "fauteuil club siege"),
            ("Armchair_18", "SAL-05", "Fauteuil bergère", M3Categories.Salon, 790, 105, ProductPlacement.Sol, Tint.Tissu, "fauteuil bergere siege"),
            ("Armchair_Classic", "SAL-06", "Fauteuil classique", M3Categories.Salon, 590, 85, ProductPlacement.Sol, Tint.Tissu, "fauteuil classique siege"),
            ("Armchair_Classic_2", "SAL-07", "Fauteuil à oreilles", M3Categories.Salon, 640, 95, ProductPlacement.Sol, Tint.Tissu, "fauteuil oreilles siege"),
            ("Coffee_Table_03", "SAL-08", "Table basse carrée", M3Categories.Salon, 390, 80, ProductPlacement.Sol, Tint.Bois, "table basse carree"),
            ("Coffee_table_1", "SAL-09", "Table basse nordique", M3Categories.Salon, 340, 100, ProductPlacement.Sol, Tint.Bois, "table basse nordique scandinave"),
            ("Coffee_table_2", "SAL-10", "Table basse rectangulaire", M3Categories.Salon, 290, 100, ProductPlacement.Sol, Tint.Bois, "table basse rectangulaire"),
            ("Coffee_table_3", "SAL-11", "Table basse en chêne", M3Categories.Salon, 320, 100, ProductPlacement.Sol, Tint.Bois, "table basse chene bois"),
            ("Coffee_table_4", "SAL-12", "Table basse ronde", M3Categories.Salon, 360, 90, ProductPlacement.Sol, Tint.Bois, "table basse ronde"),
            ("Coffee_table_5", "SAL-13", "Table basse ovale", M3Categories.Salon, 380, 110, ProductPlacement.Sol, Tint.Bois, "table basse ovale"),
            ("Side_table_1", "SAL-14", "Console d'entrée", M3Categories.Salon, 420, 100, ProductPlacement.Sol, Tint.Bois, "console entree meuble"),
            ("Table", "SAL-15", "Table de salon", M3Categories.Salon, 520, 120, ProductPlacement.Sol, Tint.Bois, "table salon"),
            ("Clock_03", "SAL-16", "Horloge comtoise", M3Categories.Salon, 480, 190, ProductPlacement.Sol, Tint.Bois, "horloge comtoise pendule"),

            ("Bed_02", "CHA-01", "Lit double à tiroirs", M3Categories.Chambre, 1850, 214, ProductPlacement.Sol, Tint.Bois, "lit double chambre tiroirs couchage"),
            ("Bed_07", "CHA-02", "Lit enfant", M3Categories.Chambre, 1290, 210, ProductPlacement.Sol, Tint.Bois, "lit enfant chambre couchage"),
            ("Closet_01", "CHA-03", "Armoire 1 porte", M3Categories.Chambre, 990, 190, ProductPlacement.Sol, Tint.Bois, "armoire penderie rangement"),
            ("Closet_02", "CHA-04", "Armoire 2 portes", M3Categories.Chambre, 1290, 200, ProductPlacement.Sol, Tint.Bois, "armoire penderie dressing rangement"),
            ("Nightstand_02", "CHA-05", "Table de chevet ronde", M3Categories.Chambre, 210, 57, ProductPlacement.Sol, Tint.Bois, "chevet table nuit"),
            ("Night_stand_1", "CHA-06", "Chevet à tiroir", M3Categories.Chambre, 190, 50, ProductPlacement.Sol, Tint.Bois, "chevet tiroir table nuit"),
            ("Night_stand_2", "CHA-07", "Chevet carré", M3Categories.Chambre, 170, 45, ProductPlacement.Sol, Tint.Bois, "chevet carre table nuit"),
            ("Night_stand_3", "CHA-08", "Chevet deux tiroirs", M3Categories.Chambre, 230, 55, ProductPlacement.Sol, Tint.Bois, "chevet tiroirs table nuit"),
            ("Night_stand_4", "CHA-09", "Petite commode", M3Categories.Chambre, 390, 80, ProductPlacement.Sol, Tint.Bois, "commode tiroirs rangement"),
            ("Nightstand_1", "CHA-10", "Banc de lit", M3Categories.Chambre, 340, 120, ProductPlacement.Sol, Tint.Bois, "banc bout de lit"),
            ("Nightstand_2", "CHA-11", "Commode basse", M3Categories.Chambre, 560, 120, ProductPlacement.Sol, Tint.Bois, "commode basse rangement"),
            ("Hanger", "CHA-12", "Portant à vêtements", M3Categories.Chambre, 150, 170, ProductPlacement.Sol, Tint.Bois, "portant vetements porte manteau"),

            ("Kitchen_D_01", "CUI-01", "Meuble évier", M3Categories.Cuisine, 890, 92, ProductPlacement.Sol, Tint.Bois, "meuble evier cuisine"),
            ("Kitchen_D_06", "CUI-02", "Meuble d'angle", M3Categories.Cuisine, 760, 87, ProductPlacement.Sol, Tint.Bois, "meuble angle cuisine"),
            ("Kitchen_D_08", "CUI-03", "Élément haut 1 porte", M3Categories.Cuisine, 290, 60, ProductPlacement.Mur, Tint.Bois, "element haut placard cuisine"),
            ("Kitchen_D_09", "CUI-04", "Élément haut 2 portes", M3Categories.Cuisine, 390, 90, ProductPlacement.Mur, Tint.Bois, "element haut placard cuisine"),
            ("Kitchen_D_10", "CUI-05", "Élément haut double", M3Categories.Cuisine, 420, 90, ProductPlacement.Mur, Tint.Bois, "element haut placard cuisine"),
            ("Fridge_01", "CUI-06", "Réfrigérateur rétro", M3Categories.Cuisine, 1490, 170, ProductPlacement.Sol, Tint.Deco, "refrigerateur frigo retro"),
            ("Microwave_01", "CUI-07", "Four micro-ondes", M3Categories.Cuisine, 320, 45, ProductPlacement.Surface, Tint.Deco, "micro ondes four"),
            ("Kitchen_Table_09", "CUI-08", "Table à manger", M3Categories.Cuisine, 980, 160, ProductPlacement.Sol, Tint.Bois, "table manger repas"),
            ("Chair_17", "CUI-09", "Chaise en bois", M3Categories.Cuisine, 180, 85, ProductPlacement.Sol, Tint.Bois, "chaise bois siege"),
            ("Stool_1", "CUI-10", "Tabouret bas", M3Categories.Cuisine, 120, 45, ProductPlacement.Sol, Tint.Bois, "tabouret bas"),
            ("Stool_2", "CUI-11", "Tabouret rond", M3Categories.Cuisine, 110, 45, ProductPlacement.Sol, Tint.Bois, "tabouret rond"),
            ("Stool_3", "CUI-12", "Tabouret de bar", M3Categories.Cuisine, 150, 75, ProductPlacement.Sol, Tint.Bois, "tabouret bar haut comptoir"),
            ("Stool_4", "CUI-13", "Banc de cuisine", M3Categories.Cuisine, 260, 100, ProductPlacement.Sol, Tint.Bois, "banc cuisine"),
            ("Cutting_board_02", "CUI-14", "Planche à découper", M3Categories.Cuisine, 45, 46, ProductPlacement.Surface, Tint.Bois, "planche decouper bois"),
            ("Utensils_01", "CUI-15", "Set d'ustensiles", M3Categories.Cuisine, 65, 39, ProductPlacement.Surface, Tint.Aucune, "ustensiles cuisine"),
            ("Mug", "CUI-16", "Mug en céramique", M3Categories.Cuisine, 25, 12, ProductPlacement.Surface, Tint.Deco, "mug tasse ceramique"),

            ("Bath_03", "SDB-01", "Baignoire avec douche", M3Categories.SalleDeBain, 2490, 214, ProductPlacement.Sol, Tint.Aucune, "baignoire bain douche"),
            ("Wash_Basin_07", "SDB-02", "Meuble vasque", M3Categories.SalleDeBain, 890, 111, ProductPlacement.Sol, Tint.Bois, "vasque lavabo meuble"),
            ("Toilet_03", "SDB-03", "WC suspendu", M3Categories.SalleDeBain, 690, 80, ProductPlacement.Sol, Tint.Aucune, "toilettes wc"),
            ("Mixer_08", "SDB-04", "Robinet mitigeur", M3Categories.SalleDeBain, 160, 27, ProductPlacement.Surface, Tint.Aucune, "robinet mitigeur"),
            ("M3_Ensemble_BrosseADents", "SDB-05", "Set brosse à dents", M3Categories.SalleDeBain, 25, 26, ProductPlacement.Surface, Tint.Aucune, "brosse dents dentifrice"),

            ("Lamp_Tall", "LUM-01", "Lampadaire tissu", M3Categories.Luminaires, 260, 160, ProductPlacement.Sol, Tint.Deco, "lampadaire lampe pied"),
            ("Light_05", "LUM-02", "Lampadaire design", M3Categories.Luminaires, 340, 160, ProductPlacement.Sol, Tint.Deco, "lampadaire lampe design"),
            ("Night_light_1", "LUM-03", "Lampe de chevet", M3Categories.Luminaires, 95, 45, ProductPlacement.Surface, Tint.Deco, "lampe chevet"),
            ("Night_light_2", "LUM-04", "Lampe champignon", M3Categories.Luminaires, 75, 35, ProductPlacement.Surface, Tint.Deco, "lampe champignon"),
            ("Night_light_3", "LUM-05", "Lampe à poser", M3Categories.Luminaires, 110, 50, ProductPlacement.Surface, Tint.Deco, "lampe poser table"),

            ("Painting_Modern_1", "TAB-01", "Toile abstraite", M3Categories.Tableaux, 180, 90, ProductPlacement.Mur, Tint.Aucune, "toile peinture abstrait art"),
            ("Painting_Modern_2", "TAB-02", "Toile géométrique", M3Categories.Tableaux, 160, 80, ProductPlacement.Mur, Tint.Aucune, "toile peinture geometrique art"),
            ("Painting_Modern_3", "TAB-03", "Toile verticale", M3Categories.Tableaux, 150, 100, ProductPlacement.Mur, Tint.Aucune, "toile peinture verticale art"),
            ("Picture_08", "TAB-04", "Affiche botanique", M3Categories.Tableaux, 85, 81, ProductPlacement.Mur, Tint.Aucune, "affiche poster plante"),
            ("Picture_17", "TAB-05", "Grand tableau paysage", M3Categories.Tableaux, 240, 110, ProductPlacement.Mur, Tint.Aucune, "tableau paysage grand"),
            ("Picture_21", "TAB-06", "Cadre carré", M3Categories.Tableaux, 70, 47, ProductPlacement.Mur, Tint.Aucune, "cadre carre photo"),
            ("Mirror", "TAB-07", "Miroir mural", M3Categories.Tableaux, 140, 80, ProductPlacement.Mur, Tint.Deco, "miroir glace mural"),
            ("Clock_WallRound", "TAB-08", "Horloge murale", M3Categories.Tableaux, 65, 40, ProductPlacement.Mur, Tint.Deco, "horloge murale pendule"),

            ("PottedPlant_Small_1", "PLA-01", "Succulente en pot", M3Categories.Plantes, 35, 25, ProductPlacement.Surface, Tint.Aucune, "succulente plante pot"),
            ("PottedPlant_Small_2", "PLA-02", "Cactus en pot", M3Categories.Plantes, 40, 30, ProductPlacement.Surface, Tint.Aucune, "cactus plante pot"),
            ("PottedPlant_Small_3", "PLA-03", "Petite fougère", M3Categories.Plantes, 45, 35, ProductPlacement.Surface, Tint.Aucune, "fougere plante pot"),
            ("PottedPlant_Tall_1", "PLA-04", "Monstera", M3Categories.Plantes, 160, 110, ProductPlacement.Sol, Tint.Aucune, "monstera plante grande"),
            ("PottedPlant_Tall_2", "PLA-05", "Ficus", M3Categories.Plantes, 140, 120, ProductPlacement.Sol, Tint.Aucune, "ficus plante grande"),
            ("PottedPlant_Tall_3", "PLA-06", "Palmier d'intérieur", M3Categories.Plantes, 180, 130, ProductPlacement.Sol, Tint.Aucune, "palmier plante grande"),
            ("Plants_05", "PLA-07", "Plante de table", M3Categories.Plantes, 30, 24, ProductPlacement.Surface, Tint.Aucune, "plante table petite"),
            ("Plants_15", "PLA-08", "Strelitzia", M3Categories.Plantes, 210, 116, ProductPlacement.Sol, Tint.Aucune, "strelitzia oiseau paradis plante"),
            ("Plants_19", "PLA-09", "Olivier d'intérieur", M3Categories.Plantes, 390, 200, ProductPlacement.Sol, Tint.Aucune, "olivier arbre plante grande"),

            ("Vase_1", "DEC-01", "Coupe en céramique", M3Categories.ObjetsDeco, 35, 25, ProductPlacement.Surface, Tint.Deco, "coupe ceramique bol"),
            ("Vase_2", "DEC-02", "Vase terracotta", M3Categories.ObjetsDeco, 45, 35, ProductPlacement.Surface, Tint.Deco, "vase terracotta fleur"),
            ("Vase_3", "DEC-03", "Vase boule", M3Categories.ObjetsDeco, 50, 30, ProductPlacement.Surface, Tint.Deco, "vase boule fleur"),
            ("Pillow_Square_1", "DEC-04", "Coussin en lin", M3Categories.ObjetsDeco, 60, 45, ProductPlacement.Surface, Tint.Tissu, "coussin lin canape"),
            ("Pillow_Square_2", "DEC-05", "Coussin rectangulaire", M3Categories.ObjetsDeco, 65, 55, ProductPlacement.Surface, Tint.Tissu, "coussin rectangulaire canape"),
            ("Carpet_1", "DEC-06", "Tapis berbère", M3Categories.ObjetsDeco, 280, 160, ProductPlacement.Sol, Tint.Tissu, "tapis berbere laine"),
            ("Carpet_2", "DEC-07", "Grand tapis", M3Categories.ObjetsDeco, 420, 240, ProductPlacement.Sol, Tint.Tissu, "tapis grand salon"),
            ("M3_Ensemble_Livres", "DEC-08", "Rangée de livres", M3Categories.ObjetsDeco, 90, 45, ProductPlacement.Surface, Tint.Aucune, "livres bibliotheque rangee"),
            ("Book_03", "DEC-09", "Pile de beaux livres", M3Categories.ObjetsDeco, 75, 70, ProductPlacement.Surface, Tint.Aucune, "livres pile beaux"),
            ("Book_08", "DEC-10", "Livres empilés", M3Categories.ObjetsDeco, 55, 34, ProductPlacement.Surface, Tint.Aucune, "livres empiles"),
            ("Box_1", "DEC-11", "Boîte de rangement", M3Categories.ObjetsDeco, 45, 40, ProductPlacement.Surface, Tint.Deco, "boite rangement"),
            ("Box_2", "DEC-12", "Panier tressé", M3Categories.ObjetsDeco, 55, 35, ProductPlacement.Surface, Tint.Deco, "panier tresse rangement"),
            ("Box_Open", "DEC-13", "Caisse en bois", M3Categories.ObjetsDeco, 60, 40, ProductPlacement.Surface, Tint.Bois, "caisse bois rangement"),
            ("DeskClock_1", "DEC-14", "Réveil rétro", M3Categories.ObjetsDeco, 40, 20, ProductPlacement.Surface, Tint.Deco, "reveil horloge retro"),
            ("DeskClock_2", "DEC-15", "Horloge de table", M3Categories.ObjetsDeco, 55, 25, ProductPlacement.Surface, Tint.Deco, "horloge table pendule"),
            ("M3_Ensemble_Magazines", "DEC-16", "Lot de magazines", M3Categories.ObjetsDeco, 30, 36, ProductPlacement.Surface, Tint.Aucune, "magazines journal papier"),

            ("TV_Modern", "MUL-01", "Téléviseur 55 pouces", M3Categories.Multimedia, 1990, 123, ProductPlacement.Mur, Tint.Aucune, "television tele tv ecran"),
            ("TV_Retro", "MUL-02", "Téléviseur vintage", M3Categories.Multimedia, 690, 60, ProductPlacement.Surface, Tint.Deco, "television tele tv vintage retro"),
            ("Stereo_MainUnit", "MUL-03", "Chaîne hi-fi", M3Categories.Multimedia, 590, 45, ProductPlacement.Surface, Tint.Aucune, "chaine hifi stereo musique"),
            ("Stereo_Speaker", "MUL-04", "Enceinte bibliothèque", M3Categories.Multimedia, 290, 40, ProductPlacement.Surface, Tint.Aucune, "enceinte haut parleur"),
            ("Speaker_Floor", "MUL-05", "Enceinte colonne", M3Categories.Multimedia, 490, 100, ProductPlacement.Sol, Tint.Aucune, "enceinte colonne haut parleur"),
            ("Speaker_Soundbar", "MUL-06", "Barre de son", M3Categories.Multimedia, 390, 90, ProductPlacement.Surface, Tint.Aucune, "barre son soundbar"),
            ("DVD_Player", "MUL-07", "Lecteur Blu-ray", M3Categories.Multimedia, 220, 43, ProductPlacement.Surface, Tint.Aucune, "lecteur bluray dvd"),
            ("Console_Modern", "MUL-08", "Console de salon", M3Categories.Multimedia, 1290, 30, ProductPlacement.Surface, Tint.Aucune, "console jeux video"),
            ("TV_remote_control", "MUL-09", "Télécommande universelle", M3Categories.Multimedia, 45, 20, ProductPlacement.Surface, Tint.Aucune, "telecommande"),

            ("Work_Table_06", "BUR-01", "Bureau en chêne", M3Categories.BureauLoisirs, 690, 131, ProductPlacement.Sol, Tint.Bois, "bureau table travail chene"),
            ("Chair_PC_04", "BUR-02", "Chaise de bureau", M3Categories.BureauLoisirs, 390, 105, ProductPlacement.Sol, Tint.Tissu, "chaise bureau siege"),
            ("Computer_01", "BUR-03", "Ordinateur tout-en-un", M3Categories.BureauLoisirs, 2490, 81, ProductPlacement.Surface, Tint.Aucune, "ordinateur pc ecran"),
            ("M3_Ensemble_ClavierSouris", "BUR-04", "Clavier et souris", M3Categories.BureauLoisirs, 120, 45, ProductPlacement.Surface, Tint.Aucune, "clavier souris ordinateur"),
            ("GameConsole_01", "BUR-05", "Console portable", M3Categories.BureauLoisirs, 890, 21, ProductPlacement.Surface, Tint.Aucune, "console portable jeux"),
            ("Gamepad_Classic", "BUR-06", "Manette de jeu", M3Categories.BureauLoisirs, 150, 15, ProductPlacement.Surface, Tint.Aucune, "manette jeu gamepad"),
            ("Guitar_01", "BUR-07", "Guitare acoustique", M3Categories.BureauLoisirs, 450, 100, ProductPlacement.Sol, Tint.Bois, "guitare musique instrument"),
            ("ExerciseBike_01", "BUR-08", "Vélo d'appartement", M3Categories.BureauLoisirs, 1290, 141, ProductPlacement.Sol, Tint.Aucune, "velo appartement sport"),
            ("Toy_02", "BUR-09", "Voiture en bois", M3Categories.BureauLoisirs, 45, 24, ProductPlacement.Surface, Tint.Deco, "voiture jouet enfant"),
            ("Toy_03", "BUR-10", "Peluche ourson", M3Categories.BureauLoisirs, 55, 29, ProductPlacement.Surface, Tint.Deco, "peluche ourson jouet enfant"),
        };

        static readonly Dictionary<string, string> Blurbs = new Dictionary<string, string>
        {
            { M3Categories.Salon, "Pour un salon chaleureux et accueillant. Livraison et montage inclus." },
            { M3Categories.Chambre, "Une chambre douce et bien rangée. Bois issu de forêts gérées durablement." },
            { M3Categories.Cuisine, "Pratique au quotidien, facile à entretenir." },
            { M3Categories.SalleDeBain, "Résiste à l'humidité, finitions soignées." },
            { M3Categories.Luminaires, "Lumière chaude et tamisée, ampoule LED fournie." },
            { M3Categories.Tableaux, "Prêt à accrocher, fixations incluses." },
            { M3Categories.Plantes, "Plante d'intérieur facile, livrée avec son pot." },
            { M3Categories.ObjetsDeco, "La petite touche qui change tout." },
            { M3Categories.Multimedia, "Garantie 2 ans, installation possible à domicile." },
            { M3Categories.BureauLoisirs, "Pour travailler et se détendre à la maison." },
        };

        [MenuItem("DecoAR/M3/Reconstruire le catalogue (meubles des packs)")]
        public static void RunAll()
        {
            ImportAssets();
            BuildEnsembles();
            BuildCatalog();
            BuildScene();
        }

        // ------------------------------------------------------------ 1. assets

        static string FindPrefab(string name)
        {
            foreach (var g in AssetDatabase.FindAssets(name + " t:Prefab", SearchRoots))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name) return p;
            }
            return null;
        }

        /// Déplace un prefab des packs et tout ce qu'il utilise (maillages, matériaux, textures) dans M3_ProductCatalog/Meubles.
        public static void ImportAssets()
        {
            var names = Products.Select(p => p.prefab).Where(n => !n.StartsWith("M3_Ensemble"))
                .Concat(EnsemblePieces).Concat(new[] { "Bookshelf_Tall" }).Distinct();
            int moved = 0;
            {
                foreach (var n in names)
                {
                    var path = FindPrefab(n);
                    if (path == null) { Debug.LogWarning("[M3] Prefab introuvable : " + n); continue; }
                    foreach (var dep in AssetDatabase.GetDependencies(path, true))
                        foreach (var (from, to) in Packs)
                            if (dep.StartsWith(from + "/"))
                            {
                                var target = to + dep.Substring(from.Length);
                                EnsureFolder(Path.GetDirectoryName(target).Replace('\\', '/'));
                                var err = AssetDatabase.MoveAsset(dep, target);
                                if (string.IsNullOrEmpty(err)) moved++; else Debug.LogWarning("[M3] " + err);
                            }
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[M3] {moved} fichiers des packs déplacés dans {Meubles}");
        }

        // ------------------------------------------------------------ 2. ensembles

        static readonly string[] EnsemblePieces =
        {
            "Book_1", "Book_2", "Book_3", "Book_4", "Book_5", "Book_6", "Book_7", "Book_8", "Book_9", "Book_10",
            "Newspaper", "Paper_01", "Paper_02", "Toothbrush_01", "Toothpaste_01", "Keyboard_01", "Mouse_01",
        };

        public static void BuildEnsembles()
        {
            EnsureFolder(Ensembles);

            // Rangée de livres : les dix livres du pack, debout, un dernier penché
            var row = new GameObject("M3_Ensemble_Livres");
            float x = 0f;
            for (int i = 1; i <= 10; i++)
            {
                var b = Place(row, "Book_" + i);
                if (b == null) continue;
                var bb = Bounds(b);
                b.transform.position += new Vector3(x - bb.min.x, -bb.min.y, -bb.center.z);
                if (i == 10) { b.transform.RotateAround(new Vector3(x, 0, 0), Vector3.forward, 14f); b.transform.position += Vector3.right * 0.012f; }
                x += bb.size.x + 0.002f;
            }
            Save(row);

            // Magazines : journal (pack low poly, plus petit) posé sur deux magazines
            var mag = new GameObject("M3_Ensemble_Magazines");
            var p1 = Place(mag, "Paper_01");
            var p2 = Place(mag, "Paper_02");
            var np = Place(mag, "Newspaper");
            if (p1 && p2 && np)
            {
                Ground(p1, Vector3.zero);
                Ground(p2, new Vector3(0.03f, Bounds(p1).max.y, 0.02f)); p2.transform.Rotate(0, 12f, 0);
                np.transform.localScale *= 2.6f;
                Ground(np, new Vector3(-0.02f, Bounds(p2).max.y, -0.01f)); np.transform.Rotate(0, -8f, 0);
            }
            Save(mag);

            var tooth = new GameObject("M3_Ensemble_BrosseADents");
            var paste = Place(tooth, "Toothpaste_01");
            var brush = Place(tooth, "Toothbrush_01");
            if (paste && brush) { Ground(paste, Vector3.zero); Ground(brush, new Vector3(0.02f, 0f, 0.08f)); brush.transform.Rotate(0, 18f, 0); }
            Save(tooth);

            var desk = new GameObject("M3_Ensemble_ClavierSouris");
            var kb = Place(desk, "Keyboard_01");
            var ms = Place(desk, "Mouse_01");
            if (kb && ms) { Ground(kb, Vector3.zero); Ground(ms, new Vector3(Bounds(kb).max.x + 0.08f, 0f, 0f)); }
            Save(desk);
        }

        static GameObject Place(GameObject parent, string prefab)
        {
            var path = FindPrefab(prefab);
            if (path == null) { Debug.LogWarning("[M3] Pièce introuvable : " + prefab); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        /// Pose l'objet : centre en x/z sur la position donnée, dessous à la hauteur donnée.
        static void Ground(GameObject go, Vector3 at)
        {
            var b = Bounds(go);
            go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        }

        static void Save(GameObject go)
        {
            PrefabUtility.SaveAsPrefabAsset(go, $"{Ensembles}/{go.name}.prefab");
            Object.DestroyImmediate(go);
        }

        // ------------------------------------------------------------ 3. catalogue

        public static void BuildCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<M3ProductCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<M3ProductCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.products = new List<ProductOption>();
            foreach (var d in Products)
            {
                var path = FindPrefab(d.prefab);
                var prefab = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
                if (prefab == null) { Debug.LogError("[M3] Produit sans modèle : " + d.prefab); continue; }

                // Dimensions réelles : proportions du modèle, plus grande dimension = valeur du catalogue
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var size = Bounds(inst).size;
                Object.DestroyImmediate(inst);
                float k = d.cm / Mathf.Max(size.x, size.y, size.z, 0.0001f);

                catalog.products.Add(new ProductOption
                {
                    id = d.id,
                    displayName = d.name,
                    category = d.cat,
                    price = d.price,
                    sizeCm = new Vector3(Mathf.Max(1f, Mathf.Round(size.x * k)), Mathf.Max(1f, Mathf.Round(size.y * k)), Mathf.Max(1f, Mathf.Round(size.z * k))),
                    prefab = prefab,
                    description = Blurbs.TryGetValue(d.cat, out var bl) ? bl : "",
                    placement = d.place,
                    variants = Variants(d.tint),
                    keywords = d.kw,
                });
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[M3] Catalogue : {catalog.products.Count} produits");
        }

        // Teintes multipliées sur les textures du pack : « Original » garde les couleurs d'origine
        static List<ProductVariant> Variants(Tint t)
        {
            var list = new List<ProductVariant> { new ProductVariant { name = "Original", color = Color.white } };
            switch (t)
            {
                // Les teintes assombrissent ou réchauffent la couleur d'origine (multiplication) : finitions crédibles
                // quelle que soit la couleur du modèle
                case Tint.Tissu:
                    list.Add(new ProductVariant { name = "Lin chiné", color = new Color(1f, 0.93f, 0.84f) });
                    list.Add(new ProductVariant { name = "Velours foncé", color = new Color(0.62f, 0.58f, 0.6f) });
                    list.Add(new ProductVariant { name = "Anthracite", color = new Color(0.45f, 0.46f, 0.5f) });
                    break;
                case Tint.Bois:
                    list.Add(new ProductVariant { name = "Chêne clair", color = new Color(1f, 0.95f, 0.86f) });
                    list.Add(new ProductVariant { name = "Wengé", color = new Color(0.55f, 0.47f, 0.42f) });
                    break;
                case Tint.Deco:
                    list.Add(new ProductVariant { name = "Sable", color = new Color(1f, 0.93f, 0.82f) });
                    list.Add(new ProductVariant { name = "Ardoise", color = new Color(0.6f, 0.62f, 0.66f) });
                    break;
            }
            return list;
        }

        // ------------------------------------------------------------ 4. scène

        public static void BuildScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var name in new[] { "Decor", "M3_Etagere", "M3_CatalogCanvas", "Spot rayon", "M3_Eclairage" })
            {
                var old = GameObject.Find(name);
                if (old != null) Object.DestroyImmediate(old);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<M3ProductCatalog>(CatalogPath);
            BuildRoom();
            var shelf = BuildShelf();
            BuildLights();

            // Caméra : l'étagère entière entre la barre du haut et la liste des produits
            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 1.42f, -2.75f), Quaternion.Euler(1.5f, 0f, 0f));
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.94f, 0.90f, 0.84f);
            var inspect = cam.transform.Find("M3_InspectPoint");
            if (inspect != null) inspect.localPosition = new Vector3(-0.36f, -0.06f, 1.15f);

            var manager = Object.FindAnyObjectByType<M3CatalogSelector>();
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("catalog").objectReferenceValue = catalog;
                so.FindProperty("shelf").objectReferenceValue = shelf;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var inspector = Object.FindAnyObjectByType<M3Inspector>();
            if (inspector != null) { var so = new SerializedObject(inspector); so.FindProperty("inspectSize").floatValue = 0.4f; so.ApplyModifiedPropertiesWithoutUndo(); }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[M3] Scène du rayon reconstruite");
        }

        static void BuildRoom()
        {
            var decor = new GameObject("Decor");
            Cube("Sol", decor.transform, new Vector3(0, -0.05f, 0.5f), new Vector3(14f, 0.1f, 10f), Mat("M3_Sol_Chene", new Color(0.70f, 0.55f, 0.42f), 0.35f));
            Cube("Mur du fond", decor.transform, new Vector3(0, 2f, 1.45f), new Vector3(14f, 4f, 0.1f), Mat("M3_Mur_Creme", new Color(0.93f, 0.89f, 0.82f), 0.1f));
            Cube("Plinthe", decor.transform, new Vector3(0, 0.05f, 1.39f), new Vector3(14f, 0.1f, 0.03f), Mat("M3_Plinthe", new Color(0.97f, 0.95f, 0.91f), 0.3f));
            // Soubassement terracotta derrière l'étagère (couleur DecoAR)
            Cube("Panneau DecoAR", decor.transform, new Vector3(0, 1.3f, 1.39f), new Vector3(4.3f, 2.6f, 0.02f), Mat("M3_Panneau_Terracotta", new Color(0.74f, 0.46f, 0.33f), 0.15f));

            // Tapis sable bordé terracotta devant l'étagère
            Cube("Tapis bordure", decor.transform, new Vector3(0f, 0.004f, -0.25f), new Vector3(4.4f, 0.008f, 2.4f), Mat("M3_Tapis_Bordure", new Color(0.74f, 0.46f, 0.33f), 0.05f))
                .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Cube("Tapis", decor.transform, new Vector3(0f, 0.006f, -0.25f), new Vector3(4.2f, 0.008f, 2.2f), Mat("M3_Tapis", new Color(0.91f, 0.85f, 0.74f), 0.05f))
                .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Prop(decor.transform, "Plants_19", new Vector3(-2.9f, 0f, 0.95f), 20f, 2.3f);
            Prop(decor.transform, "PottedPlant_Tall_1", new Vector3(2.85f, 0f, 0.95f), -30f, 1.2f);
            Prop(decor.transform, "Lamp_Tall", new Vector3(2.3f, 0f, 1.15f), 0f, 1.7f);
            Prop(decor.transform, "Plants_15", new Vector3(-2.3f, 0f, 1.15f), 0f, 1.2f);
        }

        /// Deux bibliothèques du pack, laquées crème, fond vert sauge, rubans lumineux, enseigne DecoAR.
        static M3Shelf BuildShelf()
        {
            var root = new GameObject("M3_Etagere");
            root.transform.position = new Vector3(0f, 0f, 1.0f);
            var shelf = root.AddComponent<M3Shelf>();
            var laque = Mat("M3_Etagere_Laque", new Color(0.95f, 0.92f, 0.86f), 0.45f);
            var fond = Mat("M3_Etagere_Fond", new Color(0.52f, 0.61f, 0.50f), 0.1f);
            var led = Mat("M3_Etagere_Lumiere", new Color(1f, 0.86f, 0.62f), 0.2f, emission: new Color(1f, 0.78f, 0.5f) * 2.2f);
            var tagMat = Mat("M3_Etiquette", new Color(0.99f, 0.97f, 0.93f), 0.2f);

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefab("Bookshelf_Tall"));
            var slots = new List<Transform>[3] { new List<Transform>(), new List<Transform>(), new List<Transform>() };
            float[] levels = null;
            foreach (float ux in new[] { -0.86f, 0.86f })
            {
                var unit = (GameObject)PrefabUtility.InstantiatePrefab(src);
                unit.name = ux < 0 ? "Bibliotheque gauche" : "Bibliotheque droite";
                unit.transform.SetParent(root.transform, false);
                unit.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // face ouverte vers le client
                var b = Bounds(unit);
                unit.transform.localScale *= 2.0f / b.size.y;
                b = Bounds(unit);
                unit.transform.position += new Vector3(root.transform.position.x + ux - b.center.x, -b.min.y, root.transform.position.z - b.center.z);
                foreach (var r in unit.GetComponentsInChildren<Renderer>()) r.sharedMaterial = laque;
                b = Bounds(unit);

                // Planches : on cherche le dessus de chaque niveau au rayon
                if (levels == null) levels = FindLevels(unit, b);
                float innerW = b.size.x * 0.94f;
                float backZ = InnerBack(unit, b, (levels[0] + levels[1]) * 0.5f) - 0.006f;
                for (int l = 0; l < levels.Length; l++)
                {
                    float y = levels[l];
                    float next = l + 1 < levels.Length ? levels[l + 1] - 0.04f : b.max.y - 0.04f;
                    var back = Cube($"Fond niveau {l + 1}", unit.transform.parent, new Vector3(b.center.x, (y + next) * 0.5f, backZ), new Vector3(innerW, next - y, 0.01f), fond);
                    back.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var strip = Cube($"Ruban lumineux {l + 1}", unit.transform.parent, new Vector3(b.center.x, next - 0.008f, b.min.z + 0.06f), new Vector3(innerW, 0.012f, 0.025f), led);
                    strip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    for (int c = 0; c < 3; c++)
                    {
                        var s = new GameObject($"Emplacement {l + 1}.{c + 1}").transform;
                        s.SetParent(root.transform, true);
                        s.position = new Vector3(b.center.x + (c - 1) * innerW / 3f, y + 0.002f, b.center.z - 0.02f);
                        s.localRotation = Quaternion.identity;
                        slots[l].Add(s);
                    }
                }
            }

            // Ordre de remplissage : en haut à gauche d'abord, ligne par ligne (le bas est le moins visible)
            var ordered = new List<Transform>();
            for (int l = slots.Length - 1; l >= 0; l--) ordered.AddRange(slots[l].OrderBy(t => t.position.x));

            var title = BuildSign(root.transform, 2.0f);
            var so = new SerializedObject(shelf);
            var arr = so.FindProperty("slots");
            arr.arraySize = ordered.Count;
            for (int i = 0; i < ordered.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            so.FindProperty("slotSize").floatValue = 0.4f;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("tagMaterial").objectReferenceValue = tagMat;
            so.FindProperty("tagDepth").floatValue = 0.22f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return shelf;
        }

        /// Face intérieure du fond de la bibliothèque (rayon depuis l'avant).
        static float InnerBack(GameObject unit, Bounds b, float y)
        {
            var mf = unit.GetComponentInChildren<MeshFilter>();
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            Physics.SyncTransforms();
            float z = b.max.z - 0.03f;
            if (Physics.Raycast(new Vector3(b.center.x, y, b.min.z - 0.5f), Vector3.forward, out var hit, 3f) && hit.point.z > b.center.z) z = hit.point.z;
            else if (Physics.Raycast(new Vector3(b.center.x, y, b.min.z + 0.05f), Vector3.forward, out hit, 3f)) z = hit.point.z;
            Object.DestroyImmediate(mc);
            return z;
        }

        static float[] FindLevels(GameObject unit, Bounds b)
        {
            var mf = unit.GetComponentInChildren<MeshFilter>();
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            Physics.SyncTransforms();
            var tops = new List<float>();
            float y = b.max.y - 0.05f;
            for (int guard = 0; guard < 12 && Physics.Raycast(new Vector3(b.center.x, y, b.center.z), Vector3.down, out var hit, 5f); guard++)
            {
                tops.Add(hit.point.y);
                y = hit.point.y - 0.03f;
            }
            Object.DestroyImmediate(mc);
            tops.Sort();
            // Les trois niveaux ouverts (au-dessus du placard du bas)
            var open = tops.Where(t => t > b.min.y + 0.3f && t < b.max.y - 0.1f).ToArray();
            if (open.Length == 0) open = new[] { b.min.y + 0.56f, b.min.y + 1.06f, b.min.y + 1.56f };
            return open;
        }

        /// Enseigne posée sur l'étagère : logo DecoAR, nom de la boutique, rayon affiché.
        static TMP_Text BuildSign(Transform shelf, float top)
        {
            var sign = new GameObject("Enseigne DecoAR").transform;
            sign.SetParent(shelf, false);
            sign.localPosition = new Vector3(0f, top + 0.22f, -0.05f);
            Cube("Panneau", sign, sign.position, new Vector3(2.3f, 0.4f, 0.05f), Mat("M3_Enseigne", new Color(0.20f, 0.16f, 0.12f), 0.35f));
            Cube("Liseré", sign, sign.position + new Vector3(0, -0.2f, -0.02f), new Vector3(2.3f, 0.018f, 0.02f), Mat("M3_Laiton", new Color(0.82f, 0.65f, 0.32f), 0.7f, metallic: 0.8f));
            Cube("Pied gauche", sign, sign.position + new Vector3(-0.8f, -0.13f, 0.03f), new Vector3(0.04f, 0.26f, 0.03f), Mat("M3_Laiton", new Color(0.82f, 0.65f, 0.32f), 0.7f, metallic: 0.8f));
            Cube("Pied droit", sign, sign.position + new Vector3(0.8f, -0.13f, 0.03f), new Vector3(0.04f, 0.26f, 0.03f), Mat("M3_Laiton", new Color(0.82f, 0.65f, 0.32f), 0.7f, metallic: 0.8f));

            var logoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logoTex != null)
            {
                var logo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                logo.name = "Logo DecoAR";
                Object.DestroyImmediate(logo.GetComponent<Collider>());
                logo.transform.SetParent(sign, false);
                logo.transform.localPosition = new Vector3(-0.86f, 0f, -0.03f);
                logo.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                m.SetTexture("_BaseMap", logoTex);
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                SaveMat(m, "M3_Logo");
                logo.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/M3_Logo.mat");
            }

            var brand = Text3D(sign, "Marque", "<b>DecoAR</b>", new Vector3(-0.22f, 0.055f, -0.03f), new Vector2(1.0f, 0.2f), 1.45f, new Color(0.98f, 0.95f, 0.89f), TextAlignmentOptions.Left);
            brand.characterSpacing = 4f;
            var title = Text3D(sign, "Rayon", "Salon", new Vector3(-0.22f, -0.085f, -0.03f), new Vector2(1.0f, 0.12f), 0.85f, new Color(0.86f, 0.70f, 0.42f), TextAlignmentOptions.Left);
            Text3D(sign, "Slogan", "Le catalogue en 3D", new Vector3(0.62f, -0.085f, -0.03f), new Vector2(0.7f, 0.12f), 0.6f, new Color(0.72f, 0.64f, 0.56f), TextAlignmentOptions.Right);
            return title;
        }

        static TextMeshPro Text3D(Transform parent, string name, string text, Vector3 local, Vector2 size, float fontSize, Color c, TextAlignmentOptions align)
        {
            var t = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshPro>();
            t.transform.SetParent(parent, false);
            t.transform.localPosition = local;
            t.rectTransform.sizeDelta = size;
            t.text = text; t.fontSize = fontSize; t.color = c; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        static void BuildLights()
        {
            var lights = new GameObject("M3_Eclairage").transform;
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
                sun.color = new Color(1f, 0.95f, 0.88f);
                sun.intensity = 0.9f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.45f;
            }
            foreach (float x in new[] { -1.0f, 1.0f })
            {
                var spot = new GameObject(x < 0 ? "Spot gauche" : "Spot droit").AddComponent<Light>();
                spot.transform.SetParent(lights, false);
                spot.type = LightType.Spot;
                spot.transform.position = new Vector3(x, 3.1f, -0.6f);
                spot.transform.LookAt(new Vector3(x * 0.8f, 1.1f, 1.0f));
                spot.spotAngle = 62f; spot.innerSpotAngle = 30f;
                spot.range = 6f; spot.intensity = 2.4f;
                spot.color = new Color(1f, 0.9f, 0.76f);
                spot.shadows = LightShadows.None;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.92f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.72f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.38f, 0.32f);
        }

        // ------------------------------------------------------------ outils

        static GameObject Prop(Transform parent, string prefab, Vector3 pos, float yaw, float height)
        {
            var path = FindPrefab(prefab);
            if (path == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            float biggest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (biggest > 0.0001f) go.transform.localScale *= height / biggest;
            Ground(go, pos);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        static GameObject Cube(string name, Transform parent, Vector3 worldPos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = parent != null ? parent.rotation : Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        static Material Mat(string name, Color c, float smooth, Color? emission = null, float metallic = 0f)
        {
            var path = $"{MatFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SaveMat(Material m, string name)
        {
            var path = $"{MatFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { existing.CopyPropertiesFromMaterial(m); existing.shader = m.shader; EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(m, path);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
