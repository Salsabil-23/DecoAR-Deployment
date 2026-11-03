using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DecoAR.M6
{
    // Catalogue de l'administration : les modeles 3D generes et valides par l'admin.
    // Stocke sur l'appareil (persistentDataPath/m6_catalogue) : un GLB par produit + catalogue.json.
    public static class M6Catalog
    {
        [Serializable]
        public class Item
        {
            public string id;
            public string name;
            public string category;
            public string glb;   // nom du fichier GLB dans le dossier du catalogue
            public long createdAt; // secondes Unix
            public string Path => System.IO.Path.Combine(Folder, glb);
            public DateTime Created => DateTimeOffset.FromUnixTimeSeconds(createdAt).LocalDateTime;
        }

        [Serializable] class Store { public List<Item> items = new List<Item>(); }

        public static readonly string[] Categories = { "Salon", "Chambre", "Cuisine", "Décoration", "Luminaires", "Tableaux" };

        static string Folder => System.IO.Path.Combine(Application.persistentDataPath, "m6_catalogue");
        static string File_ => System.IO.Path.Combine(Folder, "catalogue.json");

        public static List<Item> All()
        {
            try
            {
                if (!File.Exists(File_)) return new List<Item>();
                var s = JsonUtility.FromJson<Store>(File.ReadAllText(File_));
                var list = s?.items ?? new List<Item>();
                list.RemoveAll(i => !File.Exists(i.Path)); // fichier supprime entre-temps
                list.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
                return list;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[M6] Catalogue illisible : " + e.Message);
                return new List<Item>();
            }
        }

        /// Copie le GLB dans le catalogue et l'enregistre. Retourne l'element ajoute.
        public static Item Add(string sourceGlb, string name, string category)
        {
            Directory.CreateDirectory(Folder);
            var all = All();
            var item = new Item
            {
                id = "P" + (all.Count + 1).ToString("000") + "-" + DateTime.Now.ToString("HHmmss"),
                name = string.IsNullOrWhiteSpace(name) ? "Produit sans nom" : name.Trim(),
                category = category,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            item.glb = item.id + ".glb";
            File.Copy(sourceGlb, item.Path, true);
            all.Add(item);
            File.WriteAllText(File_, JsonUtility.ToJson(new Store { items = all }, true));
            return item;
        }

        public static void Remove(Item item)
        {
            var all = All();
            all.RemoveAll(i => i.id == item.id);
            try { if (File.Exists(item.Path)) File.Delete(item.Path); } catch (Exception) { }
            File.WriteAllText(File_, JsonUtility.ToJson(new Store { items = all }, true));
        }
    }
}
