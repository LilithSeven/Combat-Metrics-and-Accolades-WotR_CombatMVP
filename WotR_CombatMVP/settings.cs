using System;
using System.IO;
using Newtonsoft.Json;

namespace WotR_CombatMVP
{
    public class MVPSettings
    {
        // Définit la vue Analyse Tactique par défaut pour les nouvelles installations, 
        // tout en préservant le fichier settings.json existant des anciens utilisateurs lors de la désérialisation.
        public bool ShowDebriefView { get; set; } = true; 
        public float UiScale { get; set; } = 0.9f; // --- NOUVEAU : Option d'échelle d'affichage de l'UI --- 
    }

    public static class SettingsManager
    {
        private static string s_SettingsPath;
        public static MVPSettings Current { get; private set; } = new MVPSettings();

        public static void Init(string modPath)
        {
            try
            {
                var dir = Path.Combine(modPath, "UserSettings");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                s_SettingsPath = Path.Combine(dir, "settings.json");
                Load();
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur lors de l'initialisation des reglages : " + ex.Message);
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(s_SettingsPath))
                {
                    string json = File.ReadAllText(s_SettingsPath);
                    var loaded = JsonConvert.DeserializeObject<MVPSettings>(json);
                    if (loaded != null)
                    {
                        Current = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur lors du chargement des reglages : " + ex.Message);
            }
        }

        public static void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(s_SettingsPath)) return;
                string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(s_SettingsPath, json);
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur lors de l'enregistrement des reglages : " + ex.Message);
            }
        }
    }
}