using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace WotR_CombatMVP
{
    public class Keybind
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public KeyCode Key { get; set; } = KeyCode.None;

        public Keybind() { }

        public Keybind(bool ctrl, bool alt, bool shift, KeyCode key)
        {
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
            Key = key;
        }

        [JsonIgnore]
        public bool IsBound => Key != KeyCode.None;

        public bool ModifiersHeld()
        {
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Ctrl != ctrlHeld) return false;
            if (Alt != altHeld) return false;
            if (Shift != shiftHeld) return false;
            return true;
        }

        public bool IsTriggeredThisFrame()
        {
            if (!IsBound) return false;
            if (!Input.GetKeyDown(Key)) return false;
            return ModifiersHeld();
        }

        public string ToDisplayString()
        {
            if (!IsBound) return "—";
            string s = "";
            if (Ctrl) s += "Ctrl + ";
            if (Alt) s += "Alt + ";
            if (Shift) s += "Shift + ";
            s += Key.ToString();
            return s;
        }
    }

    public class MVPSettings
    {
        // Définit la vue Analyse Tactique par défaut pour les nouvelles installations,
        // tout en préservant le fichier settings.json existant des anciens utilisateurs lors de la désérialisation.
        public bool ShowDebriefView { get; set; } = true;
        public float UiScale { get; set; } = 0.9f;

        // Raccourcis configurables d'ouverture/fermeture du tableau de bord.
        // Valeurs par défaut historiques : Alt+M et Alt+Espace.
        // ObjectCreationHandling.Replace empêche Newtonsoft d'ajouter les éléments désérialisés
        // à la liste déjà pré-remplie (ce qui dupliquerait les raccourcis par défaut au rechargement).
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Keybind> ToggleKeybinds { get; set; } = new List<Keybind>
        {
            new Keybind(false, true, false, KeyCode.M),
            new Keybind(false, true, false, KeyCode.Space)
        };

        // --- OVERLAY TEMPS RÉEL (compteur de type "DPS meter") ---
        public bool OverlayEnabled { get; set; } = false;
        // 0 = Alliés, 1 = Tout le monde (alliés + ennemis), 2 = Cible épinglée (OverlayPinnedName)
        public int OverlayMode { get; set; } = 0;
        // 0 = Dégâts totaux, 1 = DPS, 2 = Soins
        public int OverlayMetric { get; set; } = 1;
        public float OverlayX { get; set; } = 24f;
        public float OverlayY { get; set; } = 220f;
        public float OverlayWidth { get; set; } = 300f;
        public float OverlayOpacity { get; set; } = 0.82f;
        public float OverlayScale { get; set; } = 1.0f;
        public int OverlayMaxRows { get; set; } = 6;
        public bool OverlayCombatOnly { get; set; } = true;
        public string OverlayPinnedName { get; set; } = "";

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Keybind> OverlayToggleKeybinds { get; set; } = new List<Keybind>
        {
            new Keybind(false, true, false, KeyCode.O)
        };

        // --- TEINTE D'AMBIANCE SELON L'ALIGNEMENT (cadre lumineux pour le Bien, sombre pour le Mal) ---
        public bool AlignmentTintEnabled { get; set; } = true;

        // --- MODE DALTONIEN : remplace le vert/rouge (allié/ennemi, notes) par bleu/orange distinguables ---
        public bool ColorblindMode { get; set; } = false;

        // --- REGISTRE DE CAMPAGNE (statistiques persistantes de tout un run) ---
        public bool LedgerEnabled { get; set; } = true;

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Keybind> LedgerToggleKeybinds { get; set; } = new List<Keybind>
        {
            new Keybind(false, true, false, KeyCode.L)
        };

        // --- TABLEAU COMPARATIF DU COMBAT (tous les combattants côte à côte) ---
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Keybind> CompareToggleKeybinds { get; set; } = new List<Keybind>
        {
            new Keybind(false, true, false, KeyCode.K)
        };
    }

    public static class SettingsManager
    {
        private static string s_SettingsPath;
        public static MVPSettings Current { get; private set; } = new MVPSettings();

        public static void Init(string modPath)
        {
            try
            {
                if (string.IsNullOrEmpty(modPath)) return; // PROTECTION AJOUTÉE
                
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