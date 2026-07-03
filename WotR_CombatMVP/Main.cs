using Kingmaker.Items;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using System;
using System.IO; 
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;
using HarmonyLib;
using UnityEngine.UI;
using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Blueprints;
using Kingmaker.RuleSystem.Rules.Abilities; 
using Kingmaker.UnitLogic.Buffs; 
using Kingmaker.Enums.Damage;
using UnityEngine;
using Kingmaker.Blueprints.Classes.Spells;

namespace WotR_CombatMVP
{
    public static class Main
    {
        public static bool Unload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.UnpatchAll(modEntry.Info.Id);

                if (Tracker != null)
                {
                    EventBus.Unsubscribe(Tracker);
                    Tracker = null;
                }

                var uiObject = GameObject.Find("CombatMVP_UI_Manager");
                if (uiObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(uiObject);
                }

                var glassWall = GameObject.Find("CombatMVP_GlassWall");
                if (glassWall != null)
                {
                    UnityEngine.Object.DestroyImmediate(glassWall);
                }

                Logger.Log("Combat Metrics & Accolades decharge avec succes pour Rechargement !");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Erreur lors du dechargement du mod : {ex}");
                return false;
            }
        }

        public static UnityModManager.ModEntry.ModLogger Logger;
        public static CombatTracker Tracker; 
        public static string ModPath; 

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            ModPath = modEntry.Path; 

            // Initialisation des ressources de localisation (Localization/enGB.json, frFR.json, deDE.json, etc.)
            Localization.Init(ModPath);

            // INITIALISATION DES PARAMÈTRES UTILISATEUR
            SettingsManager.Init(ModPath);

            // INITIALISATION DU REGISTRE DE CAMPAGNE (persistance sûre pour les sauvegardes)
            LedgerManager.Init(ModPath);

            try
            {
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                modEntry.OnGUI = ModSettingsUI.OnGUI;
                modEntry.OnSaveGUI = ModSettingsUI.OnSaveGUI;

                Tracker = new CombatTracker();
                EventBus.Subscribe(Tracker);

                var uiObject = new GameObject("CombatMVP_UI_Manager");
                UnityEngine.Object.DontDestroyOnLoad(uiObject);
                uiObject.AddComponent<CombatMVP_UI>();

                Logger.Log("Combat Metrics & Accolades (Compilation Parfaite) initialisé !");
            }
            catch (Exception ex) 
            { 
                Logger.Error($"Erreur lors de l'initialisation du mod : {ex}"); 
                return false; 
            }

            return true;
        }
    }

    public static class ModSettingsUI
    {
        private static int s_ListeningListId = -1; // 0 = tableau de bord, 1 = overlay
        private static int s_ListeningIndex = -1;
        private static string s_ScaleText = null;

        private static List<Keybind> ListeningList(MVPSettings s)
        {
            if (s_ListeningListId == 0) return s.ToggleKeybinds;
            if (s_ListeningListId == 1) return s.OverlayToggleKeybinds;
            if (s_ListeningListId == 2) return s.LedgerToggleKeybinds;
            if (s_ListeningListId == 3) return s.CompareToggleKeybinds;
            return null;
        }

        private static bool IsModifierKey(KeyCode kc)
        {
            return kc == KeyCode.LeftControl || kc == KeyCode.RightControl ||
                   kc == KeyCode.LeftAlt || kc == KeyCode.RightAlt ||
                   kc == KeyCode.LeftShift || kc == KeyCode.RightShift ||
                   kc == KeyCode.LeftCommand || kc == KeyCode.RightCommand ||
                   kc == KeyCode.AltGr;
        }

        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            SettingsManager.Save();
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            var s = SettingsManager.Current;
            if (s == null) return;
            if (s.ToggleKeybinds == null) s.ToggleKeybinds = new List<Keybind>();
            if (s.OverlayToggleKeybinds == null) s.OverlayToggleKeybinds = new List<Keybind>();
            if (s.LedgerToggleKeybinds == null) s.LedgerToggleKeybinds = new List<Keybind>();
            if (s.CompareToggleKeybinds == null) s.CompareToggleKeybinds = new List<Keybind>();

            // Capture d'une touche pendant le mode d'écoute (rebind), pour la liste actuellement ciblée.
            var listening = ListeningList(s);
            if (listening != null && s_ListeningIndex >= 0 && s_ListeningIndex < listening.Count &&
                Event.current != null && Event.current.type == EventType.KeyDown)
            {
                KeyCode kc = Event.current.keyCode;
                if (kc == KeyCode.Escape)
                {
                    s_ListeningListId = -1;
                    s_ListeningIndex = -1;
                    Event.current.Use();
                }
                else if (kc != KeyCode.None && !IsModifierKey(kc))
                {
                    var kb = listening[s_ListeningIndex];
                    kb.Ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Event.current.control;
                    kb.Alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Event.current.alt;
                    kb.Shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Event.current.shift;
                    kb.Key = kc;
                    s_ListeningListId = -1;
                    s_ListeningIndex = -1;
                    SettingsManager.Save();
                    Event.current.Use();
                }
            }

            // --- ÉCHELLE DE L'INTERFACE (SAISIE NUMÉRIQUE + PALIERS) ---
            GUILayout.Label(Localization.GetStringById("ui.settings.section_display") ?? "Display", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            if (s_ScaleText == null) s_ScaleText = s.UiScale.ToString("0.00");

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.GetStringById("ui.settings.ui_scale") ?? "UI Scale:", GUILayout.Width(120));
            s_ScaleText = GUILayout.TextField(s_ScaleText, GUILayout.Width(70));
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                s.UiScale = ClampScale(s.UiScale - 0.05f);
                s_ScaleText = s.UiScale.ToString("0.00");
                SettingsManager.Save();
            }
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                s.UiScale = ClampScale(s.UiScale + 0.05f);
                s_ScaleText = s.UiScale.ToString("0.00");
                SettingsManager.Save();
            }
            if (GUILayout.Button(Localization.GetStringById("ui.settings.apply") ?? "Apply", GUILayout.Width(80)))
            {
                if (float.TryParse(s_ScaleText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed) ||
                    float.TryParse(s_ScaleText, out parsed))
                {
                    s.UiScale = ClampScale(parsed);
                    s_ScaleText = s.UiScale.ToString("0.00");
                    SettingsManager.Save();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(Localization.GetStringById("ui.settings.ui_scale_hint") ?? "Allowed range: 0.50 to 2.00 (numpad supported).", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic });

            bool tint = GUILayout.Toggle(s.AlignmentTintEnabled, "  " + (Localization.GetStringById("ui.settings.alignment_tint") ?? "Tint the frame by the character's alignment (light for Good, dark for Evil)"));
            if (tint != s.AlignmentTintEnabled) { s.AlignmentTintEnabled = tint; SettingsManager.Save(); }

            bool cbm = GUILayout.Toggle(s.ColorblindMode, "  " + (Localization.GetStringById("ui.settings.colorblind") ?? "Colorblind mode (blue/orange instead of green/red)"));
            if (cbm != s.ColorblindMode) { s.ColorblindMode = cbm; SettingsManager.Save(); }

            GUILayout.Space(12);

            // --- RACCOURCIS DU TABLEAU DE BORD ---
            DrawKeybindSection(Localization.GetStringById("ui.settings.section_shortcuts") ?? "Dashboard open/close shortcuts",
                s.ToggleKeybinds, 0,
                () => new List<Keybind> { new Keybind(false, true, false, KeyCode.M), new Keybind(false, true, false, KeyCode.Space) });

            GUILayout.Space(14);

            // --- OVERLAY TEMPS RÉEL (COMPTEUR DPS) ---
            GUILayout.Label(Localization.GetStringById("ui.settings.section_overlay") ?? "Real-time overlay (DPS meter)", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            bool en = GUILayout.Toggle(s.OverlayEnabled, "  " + (Localization.GetStringById("ui.settings.overlay_enabled") ?? "Enable overlay"));
            if (en != s.OverlayEnabled) { s.OverlayEnabled = en; SettingsManager.Save(); }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.GetStringById("ui.settings.overlay_mode") ?? "Tracks:", GUILayout.Width(140));
            string[] modes = {
                Localization.GetStringById("ui.settings.overlay_mode_allies") ?? "Allies",
                Localization.GetStringById("ui.settings.overlay_mode_all") ?? "Everyone",
                Localization.GetStringById("ui.settings.overlay_mode_pinned") ?? "Pinned"
            };
            int nm = GUILayout.Toolbar(Mathf.Clamp(s.OverlayMode, 0, 2), modes, GUILayout.Width(300));
            if (nm != s.OverlayMode) { s.OverlayMode = nm; SettingsManager.Save(); }
            GUILayout.EndHorizontal();

            if (s.OverlayMode == 2)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.GetStringById("ui.settings.overlay_pinned") ?? "Pinned name:", GUILayout.Width(140));
                string pn = GUILayout.TextField(s.OverlayPinnedName ?? "", GUILayout.Width(200));
                if (pn != s.OverlayPinnedName) { s.OverlayPinnedName = pn; SettingsManager.Save(); }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.GetStringById("ui.settings.overlay_metric") ?? "Metric:", GUILayout.Width(140));
            string[] metrics = {
                Localization.GetStringById("ui.overlay.metric_damage") ?? "Damage",
                "DPS",
                Localization.GetStringById("ui.overlay.metric_healing") ?? "Healing"
            };
            int nmet = GUILayout.Toolbar(Mathf.Clamp(s.OverlayMetric, 0, 2), metrics, GUILayout.Width(300));
            if (nmet != s.OverlayMetric) { s.OverlayMetric = nmet; SettingsManager.Save(); }
            GUILayout.EndHorizontal();

            s.OverlayWidth = DrawSlider(Localization.GetStringById("ui.settings.overlay_width") ?? "Width", s.OverlayWidth, 180f, 600f, "0");
            s.OverlayOpacity = DrawSlider(Localization.GetStringById("ui.settings.overlay_opacity") ?? "Opacity", s.OverlayOpacity, 0.2f, 1f, "0.00");
            s.OverlayScale = DrawSlider(Localization.GetStringById("ui.settings.overlay_scale") ?? "Scale", s.OverlayScale, 0.6f, 2f, "0.00");
            s.OverlayMaxRows = (int)Math.Round(DrawSlider(Localization.GetStringById("ui.settings.overlay_max_rows") ?? "Max rows", s.OverlayMaxRows, 1f, 15f, "0"));

            bool co = GUILayout.Toggle(s.OverlayCombatOnly, "  " + (Localization.GetStringById("ui.settings.overlay_combat_only") ?? "Show only during combat"));
            if (co != s.OverlayCombatOnly) { s.OverlayCombatOnly = co; SettingsManager.Save(); }

            GUILayout.Space(6);
            DrawKeybindSection(Localization.GetStringById("ui.settings.section_overlay_shortcuts") ?? "Overlay toggle shortcuts",
                s.OverlayToggleKeybinds, 1,
                () => new List<Keybind> { new Keybind(false, true, false, KeyCode.O) });

            GUILayout.Space(14);

            // --- REGISTRE DE CAMPAGNE ---
            GUILayout.Label(Localization.GetStringById("ui.settings.section_ledger") ?? "Campaign ledger (persistent run stats)", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            bool le = GUILayout.Toggle(s.LedgerEnabled, "  " + (Localization.GetStringById("ui.settings.ledger_enabled") ?? "Record campaign statistics (safe, stored in the mod folder)"));
            if (le != s.LedgerEnabled) { s.LedgerEnabled = le; SettingsManager.Save(); }
            GUILayout.Space(4);
            DrawKeybindSection(Localization.GetStringById("ui.settings.section_ledger_shortcuts") ?? "Campaign ledger shortcuts",
                s.LedgerToggleKeybinds, 2,
                () => new List<Keybind> { new Keybind(false, true, false, KeyCode.L) });

            GUILayout.Space(14);

            // --- TABLEAU COMPARATIF DU COMBAT ---
            GUILayout.Label(Localization.GetStringById("ui.settings.section_compare") ?? "Combat comparison table", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            DrawKeybindSection(Localization.GetStringById("ui.settings.section_compare_shortcuts") ?? "Comparison table shortcuts",
                s.CompareToggleKeybinds, 3,
                () => new List<Keybind> { new Keybind(false, true, false, KeyCode.K) });
        }

        private static float DrawSlider(string label, float val, float min, float max, string fmt)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(140));
            float nv = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(220), GUILayout.Height(18));
            GUILayout.Label(nv.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            return nv;
        }

        // Éditeur de liste de raccourcis réutilisable (tableau de bord et overlay partagent la même logique).
        private static void DrawKeybindSection(string title, List<Keybind> binds, int listId, Func<List<Keybind>> defaults)
        {
            GUILayout.Label(title, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            for (int i = 0; i < binds.Count; i++)
            {
                var kb = binds[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label((i + 1) + ".", GUILayout.Width(25));
                GUILayout.Label(kb != null ? kb.ToDisplayString() : "—", GUILayout.Width(200));
                if (s_ListeningListId == listId && s_ListeningIndex == i)
                {
                    GUILayout.Label(Localization.GetStringById("ui.settings.press_key") ?? "Press a key... (Esc to cancel)");
                }
                else
                {
                    if (GUILayout.Button(Localization.GetStringById("ui.settings.rebind") ?? "Rebind", GUILayout.Width(90)))
                    {
                        s_ListeningListId = listId;
                        s_ListeningIndex = i;
                    }
                    if (GUILayout.Button(Localization.GetStringById("ui.settings.remove") ?? "Remove", GUILayout.Width(90)))
                    {
                        binds.RemoveAt(i);
                        SettingsManager.Save();
                        GUILayout.EndHorizontal();
                        break;
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localization.GetStringById("ui.settings.add_shortcut") ?? "Add a shortcut", GUILayout.Width(150)))
            {
                binds.Add(new Keybind());
                s_ListeningListId = listId;
                s_ListeningIndex = binds.Count - 1;
                SettingsManager.Save();
            }
            if (GUILayout.Button(Localization.GetStringById("ui.settings.reset_shortcuts") ?? "Restore defaults", GUILayout.Width(150)))
            {
                binds.Clear();
                binds.AddRange(defaults());
                s_ListeningListId = -1;
                s_ListeningIndex = -1;
                SettingsManager.Save();
            }
            GUILayout.EndHorizontal();
        }

        private static float ClampScale(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return 1.0f;
            if (v < 0.5f) v = 0.5f;
            if (v > 2.0f) v = 2.0f;
            return (float)Math.Round(v / 0.05f) * 0.05f;
        }
    }

    public class MVPAchievement
    {
        public string Tier;
        public string Title;
        public string FlavorText;
        public Color TitleColor;
        public int Weight; 

        public MVPAchievement(string tier, string title, string flavor, Color color, int weight)
        {
            Tier = tier; 
            Title = title; 
            FlavorText = flavor; 
            TitleColor = color; 
            Weight = weight;
        }
    }

    public class SummonCombatStats
    {
        public Dictionary<string, int> DamageBySource = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Dictionary<string, int>> DamageModifiersAudit = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SavesFortFailedSources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SavesRefFailedSources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SavesWillFailedSources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> SufferedDebuffs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int Damage = 0;
        public int HealingDone = 0;
        public int VampiricHealing = 0;
        public int StatDamage = 0;
        public int NegativeLevels = 0;
        public int Kills = 0;
        public int InstaKills = 0;
        public int DamageTaken = 0;
        public int PhysicalDmgTaken = 0;
        public int HitsPhysicalTaken = 0;
        public int AttacksDodged = 0;
        public int TimesDowned = 0;
        public int FriendlyFireDmg = 0;
        public int SavesSucceeded = 0;
        public int SavesFailed = 0;
        public int SavesFortFailed = 0;
        public int SavesRefFailed = 0;
        public int SavesWillFailed = 0;
        public int SavesFortSucceeded = 0;
        public int SavesRefSucceeded = 0;
        public int SavesWillSucceeded = 0;

        public bool HasAny =>
            Damage > 0 ||
            HealingDone > 0 ||
            VampiricHealing > 0 ||
            StatDamage > 0 ||
            NegativeLevels > 0 ||
            Kills > 0 ||
            InstaKills > 0 ||
            DamageTaken > 0 ||
            AttacksDodged > 0 ||
            HitsPhysicalTaken > 0 ||
            TimesDowned > 0 ||
            FriendlyFireDmg > 0 ||
            SavesSucceeded > 0 ||
            SavesFailed > 0 ||
            SufferedDebuffs.Count > 0;
    }

public class UnitCombatStats
    {
        public string Name;
        public UnitEntityData UnitData;
        public bool IsAlly;
		public bool OriginallyAlly; // --- NOUVEAU : Allégeance d'origine gelée à la création ---
        public int Level = 1;
        public int CR = 0;
		// --- NOUVEAU : Suivi précis et nominatif de la source des dégâts ---
        public Dictionary<string, int> DamageBySource = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, Dictionary<string, int>> DamageModifiersAudit = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        public string MythicPathName = "";
        public string MythicPathInternalName = ""; // Ajout du nom de classe interne	
        public bool IsEvil = false; 
        public Gender Gender = Gender.Male; 
        public int TotalDamage = 0;
        public int HealingDone = 0;
        public int StatDamage = 0;
        public int NegativeLevels = 0;
        public int Kills = 0;
		// --- MÉTRIQUES DE DÉBRIEFING ET CONSEILS TACTIQUES ---
        public int SavesSucceeded = 0;
        public int SavesFailed = 0;
        public int SavesFortFailed = 0;
        public int SavesRefFailed = 0;
        public int SavesWillFailed = 0;
        public int SpellsResistedCount = 0;
		public int SpellsPenetratedCount = 0; // <-- NOUVEAU : Sorts ayant physiquement percé la RM ennemie
        public int TimesDowned = 0;
        
        // --- NOUVELLES MÉTRIQUES ULTRA-CIBLÉES ---
        public int PhysicalDmgTaken = 0;       // Dégâts physiques réels cumulés subis
        public int HitsPhysicalTaken = 0;      // Nombre de coups physiques qui ont touché l'unité
        public int SavesFortSucceeded = 0;     // Nombre de succès sur les JS Vigueur
        public int SavesRefSucceeded = 0;      // Nombre de succès sur les JS Réflexes
        public int SavesWillSucceeded = 0;      // Nombre de succès sur les JS Volonté
		
		public bool HasRealContribution => 
            TotalDamage > 0 || 
            HealingDone > 0 || 
            Kills > 0 || 
            InstaKills > 0 ||
            StatDamage > 0 ||
            NegativeLevels > 0 ||
            CC_Paralyzed > 0 || CC_Stunned > 0 || CC_Frightened > 0 || CC_Nauseated > 0 || CC_Confused > 0 || CC_Blinded > 0 || CC_Prone > 0 || CC_Entangled > 0 || CC_Exhausted > 0 || CC_Fatigued > 0 || CC_Shaken > 0 || CC_Sickened > 0 || CC_Asleep > 0 || CC_Petrified > 0 || CC_Slowed > 0 || CC_Staggered > 0 || CC_Dazed > 0 || CC_Dazzled > 0 || CC_Helpless > 0 || CC_Cowering > 0 || CC_DeathsDoor > 0 ||
            DamageTaken > 0 ||
            AttacksDodged > 0 ||
            SummonDamage > 0 ||
            SummonKills > 0 ||
            Summons.HasAny ||
            SupportBuffsCast > 0 ||
            FriendlyFireDmg > 0 ||
            DispelledCount > 0 ||
            TrippedCount > 0 ||
            ResurrectedCount > 0 ||
            ScrollsCastCount > 0 ||
            IconicSpellsCast > 0;
        
        // --- SUIVI CONTEXTUEL MONO-CIBLE VS AOE ---
        public float WeightedNegativeLevels = 0f;
        public float WeightedStatDamage = 0f;
        public float WeightedInstaKills = 0f;
        public Dictionary<string, int> StatDamagePerTarget = new Dictionary<string, int>();
        public Dictionary<string, int> NegativeLevelsPerTarget = new Dictionary<string, int>();
        public Dictionary<string, int> InstaKillsPerTarget = new Dictionary<string, int>();

		// --- VARIABLES POUR LES INSTA-KILLS ET LE FILTRE CC ---
        public int InstaKills = 0;
        public int StatDamageKills = 0;
        public int EnergyDrainKills = 0;
        public HashSet<string> uniqueInflictedCCs = new HashSet<string>();

        // Dégâts Spécifiques (Montants réels post-armure)
        public int SlashingDmg = 0, PiercingDmg = 0, BludgeoningDmg = 0;
        public int SlashPierceDmg = 0, SlashBludgeonDmg = 0, PierceBludgeonDmg = 0, AllPhysDmg = 0;
        public int SneakAttackDmg = 0; 
        public int FireDmg = 0, ColdDmg = 0, AcidDmg = 0, ElectricDmg = 0, SonicDmg = 0;
        public int NegativeDmg = 0, HolyDmg = 0, UnholyDmg = 0;

        // Les 21 Altérations d'état spécifiques (CC)
        public int CC_Paralyzed = 0, CC_Stunned = 0, CC_Frightened = 0;
        public int CC_Nauseated = 0, CC_Confused = 0, CC_Blinded = 0;
        public int CC_Prone = 0, CC_Entangled = 0, CC_Exhausted = 0;
        public int CC_Fatigued = 0, CC_Shaken = 0, CC_Sickened = 0;
        public int CC_Asleep = 0, CC_Petrified = 0, CC_Slowed = 0;
        public int CC_Staggered = 0, CC_Dazed = 0, CC_Dazzled = 0;
        public int CC_Helpless = 0, CC_Cowering = 0, CC_DeathsDoor = 0;

        // Séparation des Soins
        public int VampiricHealing = 0;
        public int IconicSpellsCast = 0; 
        public int Crits = 0;
        public int AoOs = 0;
        public int MaxSingleHit = 0;
        // Plus gros pic de dégâts sur une fenêtre glissante de 6 s (~1 round). Fenêtre transitoire non sérialisée.
        public int MaxBurstDamage = 0;
        public List<KeyValuePair<float, int>> RecentDamageWindow = new List<KeyValuePair<float, int>>();
        public int MaxMountedChargeHit = 0;
        // Dégâts infligés lors d'une charge (RuleAttackWithWeapon.IsCharge). Sert à valider
        // les accolades liées à la charge (ex. Pounce Provider) : elles n'ont de sens que si
        // une charge a réellement eu lieu.
        public long ChargeDamage = 0;
		// --- NOUVELLES VARIABLES DE TÉLÉMÉTRIE OFFENSIVE & PRÉCISION ---
        public int AttacksAttempted = 0;       // Nombre total d'attaques physiques tentées
        public int AttacksLanded = 0;          // Nombre total d'attaques physiques ayant touché la cible

        // --- REGISTRES NOMINATIFS DES JETS DE SAUVEGARDE ÉCHOUÉS ---
        // Associe le nom de l'effet (ex: "Weird") au nombre d'échecs
        public Dictionary<string, int> SavesFortFailedSources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SavesRefFailedSources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SavesWillFailedSources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // --- REGISTRE DE RÉSISTANCE MAGIQUE (SR) NOMINATIF ---
        // Associe le nom du sort à un sous-dictionnaire contenant le nom de l'ennemi et son nombre d'occurrences
        public Dictionary<string, Dictionary<string, int>> SpellsResistedSources = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        // --- REGISTRE DES EFFETS NÉFASTES SUBIS ---
        // Liste unique des altérations d'états et débuffs subis par le personnage
        public HashSet<string> SufferedDebuffs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		// --- SUIVI DES ÉLIMINATIONS NOMINATIVES GROUPÉES ---
        // Associe le nom localisé de la victime au nombre de fois qu'elle a été éliminée par ce personnage
        public Dictionary<string, int> KilledUnits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // --- REGISTRE DES SORTS ET EFFETS ÉVITÉS (SAVED) PAR LES ENNEMIS ---
        // Associe le nom du sort à un sous-dictionnaire contenant le nom de l'ennemi et le nombre d'échecs constatés
        public Dictionary<string, Dictionary<string, int>> SpellsSavedSources = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        public int SummonDamage = 0;
        public int SummonKills = 0;
        public SummonCombatStats Summons = new SummonCombatStats();

        // --- COMPTEURS COMPLÉMENTAIRES v1.4.1 ---
        public int SummonsCount = 0;
        public int DispelledCount = 0;
        public Dictionary<string, int> DispelledSpells = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public int TrippedCount = 0;
        public int ResurrectedCount = 0;
        public int GuardianAngelHealing = 0;
        public int ScrollsCastCount = 0;

        // --- VARIABLES DE LA PHASE 5 ---
        public int DamageTaken = 0; // Dégâts subis
        public int AttacksDodged = 0; // Coups esquivés
        public int FriendlyFireDmg = 0; // Tir Allié
        public int OverkillDmg = 0; // Acharnement (dégâts sous 0 PV)

        // --- VARIABLES DE LA PHASE 6 (SOUTIEN & BUFFS) ---
        public int SupportBuffsCast = 0; // Nombre de buffs de soutien lancés sur les alliés
        public float WeightedSupportBuffs = 0f; // Poids pondéré des buffs de soutien pour le score

        // --- VARIABLES DE LA PHASE 7 (SUIVI DE HAUT DANGER) ---
        public int HighDangerInstaKills = 0; // Instakills sur cibles CR >= Niveau groupe
        public int HighDangerKills = 0; // Éliminations sur cibles CR >= Niveau groupe
        public int HighDangerCrits = 0; // Coups critiques sur cibles CR >= Niveau groupe
        public int HighDangerCCs = 0; // CC appliqués sur cibles CR >= Niveau groupe

        // --- VARIABLES DE LA PHASE 8 (SCORING) ---
        public float WeightedDamageDone = 0f;
        public float WeightedKills = 0f;
        public float WeightedCCs = 0f;
        public float TotalScore = 0;
        public string Grade = "F-";
        public List<MVPAchievement> Achievements = new List<MVPAchievement>();

        // --- ALIGNEMENT (0 = neutre, 1 = bon/lumineux, 2 = mauvais/sombre) ---
        // Utilisé pour teinter l'ambiance de la fiche et, à terme, le ton des hauts faits.
        public int AlignmentTone = 0;

        // --- NOUVEAU (PHASE DE DOMINATION) ---
        public bool IsDominatedSheet = false;

        // --- RÉANIMATION (SERVITEURS MORTS-VIVANTS DE LA LICHE) ---
        // Une créature réanimée est consolidée dans le bloc dédié de son réanimateur et retirée
        // du pager principal ; ReanimatorUniqueId fait le lien avec la fiche du maître.
        public bool IsReanimated = false;
        public string ReanimatorUniqueId = null;

        // --- NOUVEAU : Cache de portrait ---
        public Texture2D CachedPortrait = null;
    }

    public class CombatMVP_UI : MonoBehaviour
    {
        public static CombatMVP_UI Instance;
        public bool showWindow = false;
        public bool showLedger = false;
        private Vector2 ledgerScroll = Vector2.zero;
        private string lastExportPath = null;
        private bool ledgerResetArmed = false;
        public bool showCompare = false;
        private Vector2 compareScroll = Vector2.zero;
        private Rect fullScreenRect;
        private Texture2D darkBackground;
        private List<UnitCombatStats> allCombatants = new List<UnitCombatStats>();
        private int currentPageIndex = 0;
        private int historyIndex = -1; // -1 = combat courant (live), 0..n = instantané d'historique
        private Vector2 scrollPosition;
		private Vector2 leftScrollPosition; // --- NOUVEAU : Position de défilement de la colonne de gauche ---

        // --- NOUVEAU : Le Mur de Verre ---
        private GameObject invisibleGlassWall;
		// --- NOUVEAU : Barre de recherche et filtrage dynamique ---
        private string searchQuery = "";
        private List<UnitCombatStats> filteredCombatants = new List<UnitCombatStats>();

        // --- OVERLAY TEMPS RÉEL (compteur "DPS meter" déplaçable) ---
        private Rect overlayRect;
        private bool overlayRectInit = false;
        private bool overlayPosDirty = false;
        private Texture2D overlayPixel;
        private class OverlayRow { public string Name; public float Value; public bool IsAlly; public float Frac; }
        private readonly List<OverlayRow> overlayRows = new List<OverlayRow>();
        private string overlayMetricLabel = "";

        private void EnsureUIInitialized()
        {
            if (invisibleGlassWall != null) return;
            try
            {
                if (darkBackground == null)
                {
                    string imagePath = Path.Combine(Main.ModPath, "background.png");
                    if (File.Exists(imagePath))
                    {
                        byte[] fileData = File.ReadAllBytes(imagePath);
                        darkBackground = new Texture2D(2, 2);
                        darkBackground.LoadImage(fileData); 
                    }
                    else
                    {
                        darkBackground = new Texture2D(1, 1);
                        darkBackground.SetPixel(0, 0, new Color(0.04f, 0.04f, 0.05f, 0.98f)); 
                        darkBackground.Apply();
                    }
                }

                invisibleGlassWall = new GameObject("CombatMVP_GlassWall");
                UnityEngine.Object.DontDestroyOnLoad(invisibleGlassWall);
                
                Canvas canvas = invisibleGlassWall.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 29999;
                
                invisibleGlassWall.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                UnityEngine.UI.Image blocker = invisibleGlassWall.AddComponent<UnityEngine.UI.Image>();
                blocker.color = new Color(0f, 0f, 0f, 0f);
                blocker.raycastTarget = true;
                
                RectTransform rectTransform = blocker.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                
                invisibleGlassWall.SetActive(showWindow);
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur lors de l'initialisation de l'interface graphique : " + ex.Message);
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (Main.Tracker == null) return;

            // Raccourcis d'ouverture/fermeture entièrement configurables (voir la page de réglages UMM).
            bool toggleRequested = false;
            var binds = SettingsManager.Current?.ToggleKeybinds;
            if (binds != null)
            {
                foreach (var kb in binds)
                {
                    if (kb != null && kb.IsTriggeredThisFrame())
                    {
                        toggleRequested = true;
                        break;
                    }
                }
            }

            if (toggleRequested)
            {
                // CORRECTION UX : On ne bloque plus l'ouverture si combatStats.Count == 0.
                // On laisse l'UI s'ouvrir pour montrer au joueur que le mod fonctionne bien !
                showWindow = !showWindow;
                if (showWindow) { showLedger = false; showCompare = false; }
                EnsureUIInitialized();
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(showWindow || showLedger || showCompare);
                if (showWindow) { historyIndex = -1; RefreshPagination(); }
            }
            else if ((showWindow || showLedger || showCompare) && Input.GetKeyDown(KeyCode.Escape))
            {
                showWindow = false;
                showLedger = false;
                showCompare = false;
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(false);
            }

            // Bascule du Registre de Campagne (raccourci configurable, défaut Alt+L).
            var ledgerBinds = SettingsManager.Current?.LedgerToggleKeybinds;
            if (ledgerBinds != null)
            {
                foreach (var kb in ledgerBinds)
                {
                    if (kb != null && kb.IsTriggeredThisFrame())
                    {
                        showLedger = !showLedger;
                        if (showLedger) { showWindow = false; showCompare = false; ledgerScroll = Vector2.zero; ledgerResetArmed = false; EnsureLedgerRunLoaded(); }
                        EnsureUIInitialized();
                        if (invisibleGlassWall != null) invisibleGlassWall.SetActive(showWindow || showLedger || showCompare);
                        break;
                    }
                }
            }

            // Bascule du Tableau comparatif du combat (raccourci configurable, défaut Alt+K).
            var compareBinds = SettingsManager.Current?.CompareToggleKeybinds;
            if (compareBinds != null)
            {
                foreach (var kb in compareBinds)
                {
                    if (kb != null && kb.IsTriggeredThisFrame())
                    {
                        showCompare = !showCompare;
                        if (showCompare) { showWindow = false; showLedger = false; compareScroll = Vector2.zero; historyIndex = -1; RefreshPagination(); }
                        EnsureUIInitialized();
                        if (invisibleGlassWall != null) invisibleGlassWall.SetActive(showWindow || showLedger || showCompare);
                        break;
                    }
                }
            }

            // Bascule de l'overlay temps réel (raccourci indépendant, entièrement configurable).
            var overlayBinds = SettingsManager.Current?.OverlayToggleKeybinds;
            if (overlayBinds != null)
            {
                foreach (var kb in overlayBinds)
                {
                    if (kb != null && kb.IsTriggeredThisFrame())
                    {
                        SettingsManager.Current.OverlayEnabled = !SettingsManager.Current.OverlayEnabled;
                        SettingsManager.Save();
                        break;
                    }
                }
            }
        }

        void RefreshPagination()
        {
            if (Main.Tracker == null) return;
            if (Game.Instance?.Player == null) return; // Garde-fou d'initialisation tardive
            Localization.Init(Main.ModPath);
            
            var activeParty = Game.Instance.Player.PartyAndPets;

            // Source des données : combat courant (live) ou instantané d'historique de session.
            bool viewingHistory = historyIndex >= 0 && Main.Tracker.sessionHistory != null && historyIndex < Main.Tracker.sessionHistory.Count;
            IEnumerable<UnitCombatStats> source;
            if (viewingHistory)
            {
                source = Main.Tracker.sessionHistory[historyIndex].Stats;
            }
            else
            {
                historyIndex = -1;
                source = Main.Tracker.combatStats.Values;

                // --- SYNCHRONISATION EN TEMPS RÉEL (TOYBOX & RESPEC PROOF), sur le combat courant uniquement ---
                foreach (var stat in Main.Tracker.combatStats.Values)
                {
                    if (stat.UnitData != null)
                    {
                        try
                        {
                            stat.Level = stat.UnitData.Progression?.CharacterLevel ?? stat.Level;
                            stat.CR = stat.UnitData.Blueprint?.CR ?? stat.CR;

                            string mythicInternal;
                            string mythic = CombatTracker.ExtractMythicPath(stat.UnitData, out mythicInternal);
                            stat.MythicPathName = mythic;
                            stat.MythicPathInternalName = mythicInternal;

                            string mythicLower = mythic.ToLower();
                            string internalLower = mythicInternal.ToLower();
                            stat.IsEvil = mythicLower.Contains("lich") || mythicLower.Contains("demon") || mythicLower.Contains("swarm") || mythicLower.Contains("devil") || mythicLower.Contains("diable")
                                || internalLower.Contains("lich") || internalLower.Contains("demon") || internalLower.Contains("swarm") || internalLower.Contains("devil");
                        }
                        catch (Exception) { }
                    }
                }
            }

            // On filtre les alliés pour n'afficher que ceux qui ont réellement contribué au combat.
            // En historique, on n'exige pas la présence dans le groupe actuel (la composition a pu changer).
            List<UnitCombatStats> allies;
            if (viewingHistory)
            {
                allies = source
                    .Where(s => s.IsAlly && !s.IsReanimated && s.HasRealContribution)
                    .OrderByDescending(s => s.TotalScore)
                    .ToList();
            }
            else
            {
                // On n'exige plus la présence dans le groupe actif : un PNJ allié temporaire qui combat
                // à nos côtés et contribue réellement doit apparaître dans le rapport.
                allies = source
                    .Where(s => s.IsAlly && !s.IsReanimated && s.TotalScore > 0 && s.HasRealContribution && s.UnitData != null)
                    .OrderByDescending(s => s.TotalScore)
                    .ToList();
            }

            var enemies = source
                .Where(s => !s.IsAlly && s.TotalScore > 0 && s.HasRealContribution)
                .OrderByDescending(s => s.TotalScore)
                .Take(5)
                .ToList();

            if (enemies.Count < 3)
            {
                var fallbackEnemies = source
                    .Where(s => !s.IsAlly && !enemies.Contains(s) && s.HasRealContribution)
                    .OrderByDescending(s => s.DamageTaken)
                    .Take(3 - enemies.Count);
                enemies.AddRange(fallbackEnemies);
            }
            
            allCombatants = allies.Concat(enemies).ToList();
            searchQuery = "";
            filteredCombatants = new List<UnitCombatStats>(allCombatants);
            currentPageIndex = 0;
            scrollPosition = Vector2.zero;
			leftScrollPosition = Vector2.zero; // --- NOUVEAU : Réinitialisation au rafraîchissement ---
            
            foreach (var stat in allCombatants)
            {
                if (stat.CachedPortrait == null)
                {
                    stat.CachedPortrait = GetPortraitTexture(stat);
                }
            }
        }

        private Texture2D GetPortraitTexture(UnitCombatStats stat)
        {
            if (stat.UnitData == null || stat.UnitData.Portrait == null) return null;
            
            // Accès direct natif. Le moteur d'Owlcat gère lui-même le chargement (SpriteLink).
            // Zéro réflexion, zéro allocation de mémoire superflue.
            var portrait = stat.UnitData.Portrait;

            if (portrait.FullLengthPortrait != null && portrait.FullLengthPortrait.texture != null)
                return portrait.FullLengthPortrait.texture;

            if (portrait.HalfLengthPortrait != null && portrait.HalfLengthPortrait.texture != null)
                return portrait.HalfLengthPortrait.texture;

            if (portrait.SmallPortrait != null && portrait.SmallPortrait.texture != null)
                return portrait.SmallPortrait.texture;

            return null;
        }
		
		private void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                filteredCombatants = new List<UnitCombatStats>(allCombatants);
            }
            else
            {
                string query = searchQuery.ToLower();
                filteredCombatants = allCombatants
                    .Where(s => (s.Name != null && s.Name.ToLower().Contains(query)) || 
                                (s.MythicPathName != null && s.MythicPathName.ToLower().Contains(query)))
                    .ToList();
            }
            currentPageIndex = 0;
            scrollPosition = Vector2.zero;
        }

        private Texture2D overlayTexture; 
        private Dictionary<string, Texture2D> backgroundCache = new Dictionary<string, Texture2D>();

        private Texture2D GetCachedTexture(string filename)
        {
            if (backgroundCache.TryGetValue(filename, out var tex)) return tex;
            string path = Path.Combine(Main.ModPath, filename);
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D newTex = new Texture2D(2, 2);
                newTex.LoadImage(fileData);
                backgroundCache[filename] = newTex;
                return newTex;
            }
            return darkBackground; 
        }

        private Texture2D GetDynamicBackground(UnitCombatStats stat)
        {
            if (stat == null || stat.UnitData?.Progression == null)
                return darkBackground; 
            int mythicLevel = stat.UnitData.Progression.MythicLevel;
            if (mythicLevel == 0) return GetCachedTexture("none.png");
            
            string path = stat.MythicPathName.ToLower();
            string internalPath = (stat.MythicPathInternalName ?? "").ToLower();

            if (path.Contains("ange") || path.Contains("angel") || internalPath.Contains("angel")) return GetCachedTexture("angel.png");
            if (path.Contains("démon") || path.Contains("demon") || internalPath.Contains("demon")) return GetCachedTexture("demon.png");
            if (path.Contains("liche") || path.Contains("lich") || internalPath.Contains("lich")) return GetCachedTexture("lich.png");
            if (path.Contains("grivois") || path.Contains("trickster") || path.Contains("mystificateur") || internalPath.Contains("trickster")) return GetCachedTexture("trickster.png");
            if (path.Contains("éon") || path.Contains("aeon") || internalPath.Contains("aeon")) return GetCachedTexture("aeon.png");
            if (path.Contains("azata") || internalPath.Contains("azata")) return GetCachedTexture("azata.png");
            if (path.Contains("dragon") || path.Contains("gold") || internalPath.Contains("dragon")) return GetCachedTexture("dragon.png");
            if (path.Contains("légende") || path.Contains("legend") || internalPath.Contains("legend")) return GetCachedTexture("legend.png");
            if (path.Contains("essaim") || path.Contains("swarm") || internalPath.Contains("swarm")) return GetCachedTexture("swarm.png");
            if (path.Contains("diable") || path.Contains("devil") || internalPath.Contains("devil")) return GetCachedTexture("devil.png");
            return GetCachedTexture("mythic_base.png"); 
        }

        void OnGUI()
        {
            // Overlay temps réel : dessiné indépendamment du tableau de bord principal.
            DrawOverlayIfEnabled();

            // Registre de Campagne (écran plein, prioritaire sur le tableau de bord).
            if (showLedger) { DrawLedgerScreen(); return; }

            // Tableau comparatif du combat (écran plein).
            if (showCompare) { DrawCompareScreen(); return; }

            // CORRECTION UX : On ne fait plus de return si allCombatants.Count == 0
            if (!showWindow) return;
            EnsureUIInitialized();
            
            // Sécurisation de la cible courante
            UnitCombatStats currentStat = null;
            if (filteredCombatants.Count > 0) currentStat = filteredCombatants[currentPageIndex];
            else if (allCombatants.Count > 0) currentStat = allCombatants[0];

            // 1. Dessin du fond d'écran à la résolution native d'origine (empêche le flou de texture)
            fullScreenRect = new Rect(0, 0, Screen.width, Screen.height);
            Texture2D activeBg = GetDynamicBackground(currentStat);
            GUI.DrawTexture(fullScreenRect, activeBg);

            if (overlayTexture == null)
            {
                overlayTexture = new Texture2D(1, 1);
                overlayTexture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.03f, 0.75f)); 
                overlayTexture.Apply();
            }

            // 2. Application de l'échelle d'UI proportionnelle via la matrice native de Unity
            float scale = SettingsManager.Current.UiScale;
            if (scale < 0.5f || scale > 2.0f) scale = 1.0f; // Sécurité de garde-fou

            Matrix4x4 originalMatrix = GUI.matrix;

            float virtualWidth = (float)Screen.width / scale;
            float virtualHeight = (float)Screen.height / scale;

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            float marginX = virtualWidth * 0.12f; 
            float marginY = virtualHeight * 0.10f; 
            float safeWidth = virtualWidth - (marginX * 2);
            float safeHeight = virtualHeight - (marginY * 2);
            Rect safeArea = new Rect(marginX, marginY, safeWidth, safeHeight);

            // Rendu du panneau et des bordures à l'échelle virtuelle
            GUI.DrawTexture(safeArea, overlayTexture);

            Color wotrGold = HexToColor("#C4A265");
            Color wotrDarkSlate = HexToColor("#1F1F24");

            // Teinte d'ambiance selon l'axe Bien/Mal de la fiche affichée (activable dans UMM).
            Color accent = wotrGold;
            if (SettingsManager.Current.AlignmentTintEnabled && currentStat != null)
            {
                if (currentStat.AlignmentTone == 1) accent = HexToColor("#D9C07A");      // Bien : or lumineux
                else if (currentStat.AlignmentTone == 2) accent = HexToColor("#8B2E4E"); // Mal : cramoisi sombre
                else accent = HexToColor("#597F96");                                     // Neutre : ardoise
            }

            DrawProceduralFrame(safeArea, wotrDarkSlate, 2f);

            Rect innerGoldRect = new Rect(safeArea.x + 5f, safeArea.y + 5f, safeArea.width - 10f, safeArea.height - 10f);
            DrawProceduralFrame(innerGoldRect, accent, 1f);

            DrawWotRCornerBrackets(innerGoldRect, accent, 2f, 15f);

            Rect contentArea = new Rect(safeArea.x + 20f, safeArea.y + 15f, safeArea.width - 40f, safeArea.height - 30f);

            GUILayout.BeginArea(contentArea);
            if (currentStat != null)
            {
                DrawBookLayout(contentArea.width, contentArea.height);
            }
            else
            {
                // Affiche l'écran "Vide" élégant si aucun combat n'a eu lieu
                DrawEmptyState(contentArea.width, contentArea.height);
            }
            GUILayout.EndArea();
			
            // Dessin des boutons fléchés également mis à l'échelle pour éviter tout débordement d'écran
            DrawNavigationButtons(virtualWidth, virtualHeight);

            // 3. Restauration obligatoire de la matrice d'origine pour ne pas interférer avec l'interface du jeu
            GUI.matrix = originalMatrix;
        }
		
		// ============================================================================
        // REGISTRE DE CAMPAGNE (statistiques cumulées de tout un run)
        // ============================================================================
        private void EnsureLedgerRunLoaded()
        {
            try
            {
                string runId = "default", camp = "";
                var mc = Game.Instance?.Player?.MainCharacter.Value;
                if (mc != null)
                {
                    if (!string.IsNullOrEmpty(mc.UniqueId)) runId = mc.UniqueId;
                    camp = mc.CharacterName ?? "";
                }
                LedgerManager.EnsureRun(runId, camp);
            }
            catch (Exception) { }
        }

        private void DrawLedgerScreen()
        {
            EnsureUIInitialized();
            Localization.Init(Main.ModPath);

            Rect full = new Rect(0, 0, Screen.width, Screen.height);
            if (darkBackground != null) GUI.DrawTexture(full, darkBackground);
            if (overlayTexture == null)
            {
                overlayTexture = new Texture2D(1, 1);
                overlayTexture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.03f, 0.75f));
                overlayTexture.Apply();
            }

            float scale = SettingsManager.Current.UiScale;
            if (scale < 0.5f || scale > 2.0f) scale = 1.0f;
            Matrix4x4 orig = GUI.matrix;
            float vw = (float)Screen.width / scale;
            float vh = (float)Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            float mx = vw * 0.12f, my = vh * 0.10f;
            Rect safe = new Rect(mx, my, vw - mx * 2, vh - my * 2);
            GUI.DrawTexture(safe, overlayTexture);
            Color gold = HexToColor("#C4A265");
            Color slate = HexToColor("#1F1F24");
            DrawProceduralFrame(safe, slate, 2f);
            Rect inner = new Rect(safe.x + 5f, safe.y + 5f, safe.width - 10f, safe.height - 10f);
            DrawProceduralFrame(inner, gold, 1f);
            DrawWotRCornerBrackets(inner, gold, 2f, 15f);

            Rect content = new Rect(safe.x + 25f, safe.y + 20f, safe.width - 50f, safe.height - 40f);
            GUILayout.BeginArea(content);
            DrawLedgerContent(content.width, content.height);
            GUILayout.EndArea();

            GUIStyle expStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            if (GUI.Button(new Rect(safe.xMax - 425f, safe.y + 15f, 180f, 38f), ledgerResetArmed ? (Localization.GetStringById("ui.ledger.reset_confirm") ?? "Confirm reset?") : (Localization.GetStringById("ui.ledger.reset") ?? "Reset ledger"), expStyle))
            {
                if (ledgerResetArmed)
                {
                    LedgerManager.ResetCurrent();
                    lastExportPath = null;
                    ledgerResetArmed = false;
                }
                else
                {
                    ledgerResetArmed = true;
                }
            }
            if (GUI.Button(new Rect(safe.xMax - 240f, safe.y + 15f, 180f, 38f), Localization.GetStringById("ui.ledger.export") ?? "Export to .txt", expStyle))
            {
                ExportLedgerReport();
            }

            GUIStyle closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
            closeStyle.normal.textColor = new Color(0.6196f, 0.1059f, 0.1059f);
            if (GUI.Button(new Rect(safe.xMax - 55f, safe.y + 12f, 44f, 44f), "X", closeStyle))
            {
                showLedger = false;
                ledgerResetArmed = false;
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(showWindow);
            }

            GUI.matrix = orig;
        }

        // ============================================================================
        // TABLEAU COMPARATIF DU COMBAT (tous les combattants côte à côte)
        // ============================================================================
        private void DrawCompareScreen()
        {
            EnsureUIInitialized();
            Localization.Init(Main.ModPath);

            Rect full = new Rect(0, 0, Screen.width, Screen.height);
            if (darkBackground != null) GUI.DrawTexture(full, darkBackground);
            if (overlayTexture == null)
            {
                overlayTexture = new Texture2D(1, 1);
                overlayTexture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.03f, 0.75f));
                overlayTexture.Apply();
            }

            float scale = SettingsManager.Current.UiScale;
            if (scale < 0.5f || scale > 2.0f) scale = 1.0f;
            Matrix4x4 orig = GUI.matrix;
            float vw = (float)Screen.width / scale;
            float vh = (float)Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            float mx = vw * 0.12f, my = vh * 0.10f;
            Rect safe = new Rect(mx, my, vw - mx * 2, vh - my * 2);
            GUI.DrawTexture(safe, overlayTexture);
            Color gold = HexToColor("#C4A265");
            Color slate = HexToColor("#1F1F24");
            DrawProceduralFrame(safe, slate, 2f);
            Rect inner = new Rect(safe.x + 5f, safe.y + 5f, safe.width - 10f, safe.height - 10f);
            DrawProceduralFrame(inner, gold, 1f);
            DrawWotRCornerBrackets(inner, gold, 2f, 15f);

            Rect content = new Rect(safe.x + 25f, safe.y + 20f, safe.width - 50f, safe.height - 40f);
            GUILayout.BeginArea(content);
            DrawCompareContent(content.width, content.height);
            GUILayout.EndArea();

            GUIStyle closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
            closeStyle.normal.textColor = new Color(0.6196f, 0.1059f, 0.1059f);
            if (GUI.Button(new Rect(safe.xMax - 55f, safe.y + 12f, 44f, 44f), "X", closeStyle))
            {
                showCompare = false;
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(showWindow);
            }

            GUI.matrix = orig;
        }

        private void DrawCompareContent(float width, float height)
        {
            bool showGrades = !SettingsManager.Current.ShowDebriefView;
            string colTitle = "#C4A265", colText = "#E2D5B5", colSub = "#597F96", colDanger = "#9E1B1B";
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, richText = true };
            GUIStyle rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
            GUIStyle hdrStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };

            string title = Localization.GetStringById("ui.compare.title") ?? "COMBAT COMPARISON";
            GUILayout.Label($"<color={colTitle}><b>{title}</b></color>", titleStyle);

            // Aide : les pourcentages indiquent la part de chaque combattant dans le total
            // de dégâts de son camp (allié ou ennemi), pour comparer les contributions d'un coup d'oeil.
            GUILayout.Label($"<color={colSub}><i>{Localization.GetStringById("ui.compare.pct_hint") ?? "Percentages show each combatant's share of their side's total damage."}</i></color>", new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic, richText = true });
            GUILayout.Space(10);

            if (allCombatants == null || allCombatants.Count == 0)
            {
                GUILayout.Label($"<color={colSub}><i>{Localization.GetStringById("ui.compare.empty") ?? "No combatants to compare yet."}</i></color>", new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Italic, richText = true });
                return;
            }

            // Totaux de dégâts par camp pour convertir chaque contribution en pourcentage.
            long allyDamageTotal = allCombatants.Where(c => c != null && c.IsAlly).Sum(c => (long)(c.TotalDamage + c.SummonDamage));
            long enemyDamageTotal = allCombatants.Where(c => c != null && !c.IsAlly).Sum(c => (long)(c.TotalDamage + c.SummonDamage));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_name") ?? "Character"}</color>", hdrStyle, GUILayout.Width(width * 0.24f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_damage") ?? "Damage"}</color>", hdrStyle, GUILayout.Width(width * 0.13f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_healing") ?? "Healing"}</color>", hdrStyle, GUILayout.Width(width * 0.12f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_taken") ?? "Taken"}</color>", hdrStyle, GUILayout.Width(width * 0.12f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_kills") ?? "Kills"}</color>", hdrStyle, GUILayout.Width(width * 0.08f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.compare.col_cc") ?? "CC"}</color>", hdrStyle, GUILayout.Width(width * 0.08f));
            if (showGrades) GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_best") ?? "Best"}</color>", hdrStyle, GUILayout.Width(width * 0.1f));
            GUILayout.EndHorizontal();

            Color sepPrev = GUI.color;
            GUI.color = HexToColor("#3A3A3F");
            GUILayout.Box(GUIContent.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = sepPrev;

            compareScroll = GUILayout.BeginScrollView(compareScroll, GUILayout.Width(width), GUILayout.Height(Mathf.Max(80f, height - 120f)));
            foreach (var s in allCombatants)
            {
                if (s == null) continue;
                int cc = s.CC_Paralyzed + s.CC_Stunned + s.CC_Frightened + s.CC_Nauseated + s.CC_Confused + s.CC_Blinded + s.CC_Prone + s.CC_Entangled + s.CC_Exhausted + s.CC_Fatigued + s.CC_Shaken + s.CC_Sickened + s.CC_Asleep + s.CC_Petrified + s.CC_Slowed + s.CC_Staggered + s.CC_Dazed + s.CC_Dazzled + s.CC_Helpless + s.CC_Cowering + s.CC_DeathsDoor;
                int dmg = s.TotalDamage + s.SummonDamage;
                int heal = s.HealingDone + s.VampiricHealing;
                int kills = s.Kills + s.SummonKills;
                string nameCol = s.IsAlly ? colText : colDanger;

                long factionDamageTotal = s.IsAlly ? allyDamageTotal : enemyDamageTotal;
                int dmgPct = factionDamageTotal > 0 ? (int)Math.Round(dmg * 100.0 / factionDamageTotal) : 0;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color={nameCol}>{s.Name}</color>", rowStyle, GUILayout.Width(width * 0.24f));
                GUILayout.Label($"<color={colTitle}>{dmg}</color> <size=11><color={colSub}>({dmgPct}%)</color></size>", rowStyle, GUILayout.Width(width * 0.13f));
                GUILayout.Label($"<color={colText}>{heal}</color>", rowStyle, GUILayout.Width(width * 0.12f));
                GUILayout.Label($"<color={colDanger}>{s.DamageTaken}</color>", rowStyle, GUILayout.Width(width * 0.12f));
                GUILayout.Label($"<color={colText}>{kills}</color>", rowStyle, GUILayout.Width(width * 0.08f));
                GUILayout.Label($"<color={colSub}>{cc}</color>", rowStyle, GUILayout.Width(width * 0.08f));
                if (showGrades) GUILayout.Label($"<color={ColorToHex(GetGradeColor(s.Grade))}><b>{s.Grade}</b></color>", rowStyle, GUILayout.Width(width * 0.1f));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        // Export du registre dans un fichier texte lisible (UserSettings/reports/). Sûr pour les sauvegardes.
        private void ExportLedgerReport()
        {
            try
            {
                var L = LedgerManager.Current;
                if (L == null) return;
                string dir = Path.Combine(Path.Combine(Main.ModPath, "UserSettings"), "reports");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "campaign_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Combat Metrics & Accolades - Campaign Ledger ===");
                sb.AppendLine("Campaign: " + (L.CampaignName ?? ""));
                sb.AppendLine("Battles recorded: " + L.TotalCombats);
                sb.AppendLine("Exported: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                sb.AppendLine();
                sb.AppendLine(string.Format("{0,-24} {1,10} {2,10} {3,10} {4,6} {5,8} {6,6}", "Character", "Damage", "Healing", "Taken", "Kills", "Battles", "Best"));
                sb.AppendLine(new string('-', 82));
                foreach (var c in L.Characters.Values.OrderByDescending(x => x.TotalDamage))
                {
                    string nm = c.Name ?? "?";
                    if (nm.Length > 24) nm = nm.Substring(0, 24);
                    sb.AppendLine(string.Format("{0,-24} {1,10} {2,10} {3,10} {4,6} {5,8} {6,6}", nm, c.TotalDamage, c.TotalHealing, c.TotalDamageTaken, c.TotalKills, c.Combats, c.BestGrade));
                }
                var nem = LedgerManager.GetNemesis();
                if (nem != null && nem.DamageToParty > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Nemesis: " + nem.Name + " (" + nem.DamageToParty + " dmg to the party, " + nem.KillsOnParty + " kills)");
                }
                File.WriteAllText(file, sb.ToString());
                lastExportPath = file;
                if (Main.Logger != null) Main.Logger.Log("[CombatMVP] Registre exporté : " + file);
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[CombatMVP] Erreur d'export du registre : " + ex.Message);
            }
        }

        private void DrawLedgerContent(float width, float height)
        {
            var L = LedgerManager.Current;
            bool showGrades = !SettingsManager.Current.ShowDebriefView;
            string colTitle = "#C4A265", colText = "#E2D5B5", colSub = "#597F96", colDanger = "#9E1B1B";
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, richText = true };
            GUIStyle sub = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
            GUIStyle rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true };
            GUIStyle hdrStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, richText = true };

            string title = Localization.GetStringById("ui.ledger.title") ?? "CAMPAIGN LEDGER";
            GUILayout.Label($"<color={colTitle}><b>{title}</b></color>", titleStyle);

            string campaign = (L != null && !string.IsNullOrEmpty(L.CampaignName)) ? L.CampaignName : "—";
            string totalCombatsLbl = Localization.GetStringById("ui.ledger.total_combats") ?? "Battles recorded: {0}";
            GUILayout.Label($"<color={colSub}>{campaign}</color>   <color={colText}>{string.Format(totalCombatsLbl, L != null ? L.TotalCombats : 0)}</color>", sub);
            if (L != null && !string.IsNullOrEmpty(L.LastUpdated))
                GUILayout.Label($"<color={colSub}><i>{L.LastUpdated}</i></color>", new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true });
            GUILayout.Space(12);

            if (L == null || L.Characters == null || L.Characters.Count == 0)
            {
                GUILayout.Label($"<color={colSub}><i>{Localization.GetStringById("ui.ledger.empty") ?? "No campaign data yet — fight some battles!"}</i></color>", sub);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_name") ?? "Character"}</color>", hdrStyle, GUILayout.Width(width * 0.20f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_damage") ?? "Damage"}</color>", hdrStyle, GUILayout.Width(width * 0.12f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_healing") ?? "Healing"}</color>", hdrStyle, GUILayout.Width(width * 0.11f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_taken") ?? "Taken"}</color>", hdrStyle, GUILayout.Width(width * 0.12f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_kills") ?? "Kills"}</color>", hdrStyle, GUILayout.Width(width * 0.08f));
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_battles") ?? "Battles"}</color>", hdrStyle, GUILayout.Width(width * 0.09f));
            if (showGrades) GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.ledger.col_top3") ?? "Top 3 grades"}</color>", hdrStyle, GUILayout.Width(width * 0.26f));
            GUILayout.EndHorizontal();

            Color sepPrev = GUI.color;
            GUI.color = HexToColor("#3A3A3F");
            GUILayout.Box(GUIContent.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = sepPrev;

            ledgerScroll = GUILayout.BeginScrollView(ledgerScroll, GUILayout.Width(width), GUILayout.Height(Mathf.Max(80f, height - 190f)));
            foreach (var c in L.Characters.Values.OrderByDescending(x => x.TotalDamage))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color={colText}>{c.Name}</color>", rowStyle, GUILayout.Width(width * 0.20f));
                GUILayout.Label($"<color={colTitle}>{c.TotalDamage}</color>", rowStyle, GUILayout.Width(width * 0.12f));
                GUILayout.Label($"<color={colText}>{c.TotalHealing}</color>", rowStyle, GUILayout.Width(width * 0.11f));
                GUILayout.Label($"<color={colDanger}>{c.TotalDamageTaken}</color>", rowStyle, GUILayout.Width(width * 0.12f));
                GUILayout.Label($"<color={colText}>{c.TotalKills}</color>", rowStyle, GUILayout.Width(width * 0.08f));
                GUILayout.Label($"<color={colSub}>{c.Combats}</color>", rowStyle, GUILayout.Width(width * 0.09f));
                if (showGrades) GUILayout.Label($"<color={colTitle}><b>{BuildTop3Grades(c.GradeCounts)}</b></color>", rowStyle, GUILayout.Width(width * 0.26f));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            var nem = LedgerManager.GetNemesis();
            if (nem != null && nem.DamageToParty > 0)
            {
                GUILayout.Space(8);
                string nemLbl = Localization.GetStringById("ui.ledger.nemesis") ?? "Campaign nemesis: {0} — {1} damage to the party, {2} kills";
                GUILayout.Label($"<color={colDanger}><b>{string.Format(nemLbl, nem.Name, nem.DamageToParty, nem.KillsOnParty)}</b></color>", sub);
            }

            if (!string.IsNullOrEmpty(lastExportPath))
            {
                GUILayout.Space(4);
                string exportedLbl = Localization.GetStringById("ui.ledger.exported") ?? "Exported to:";
                GUILayout.Label($"<color=#6D8467><i>{exportedLbl} {lastExportPath}</i></color>", new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true });
            }
        }

		// ============================================================================
        // OVERLAY TEMPS RÉEL (compteur "DPS meter" déplaçable et entièrement configurable)
        // ============================================================================
        private void DrawOverlayIfEnabled()
        {
            var s = SettingsManager.Current;
            if (s == null || !s.OverlayEnabled || Main.Tracker == null) return;
            if (s.OverlayCombatOnly && !Main.Tracker.IsInCombat) return;

            if (overlayPixel == null)
            {
                overlayPixel = new Texture2D(1, 1);
                overlayPixel.SetPixel(0, 0, Color.white);
                overlayPixel.Apply();
            }

            float sc = Mathf.Clamp(s.OverlayScale, 0.6f, 2.0f);
            float width = Mathf.Clamp(s.OverlayWidth, 180f, 600f);

            BuildOverlayRows(s);

            float rowH = 22f * sc;
            float headerH = 26f * sc;
            float pad = 8f * sc;
            float height = headerH + pad + Mathf.Max(1, overlayRows.Count) * rowH + pad;

            if (!overlayRectInit)
            {
                overlayRect = new Rect(s.OverlayX, s.OverlayY, width, height);
                overlayRectInit = true;
            }
            overlayRect.width = width;
            overlayRect.height = height;
            overlayRect.x = Mathf.Clamp(overlayRect.x, 0f, Mathf.Max(0f, Screen.width - 40f));
            overlayRect.y = Mathf.Clamp(overlayRect.y, 0f, Mathf.Max(0f, Screen.height - 40f));

            Rect newRect = GUI.Window(0x4D5650, overlayRect, DrawOverlayContents, GUIContent.none, GUIStyle.none);
            if (Math.Abs(newRect.x - overlayRect.x) > 0.5f || Math.Abs(newRect.y - overlayRect.y) > 0.5f)
            {
                overlayRect.x = newRect.x;
                overlayRect.y = newRect.y;
                overlayPosDirty = true;
            }

            // Sauvegarde de la position seulement au relâchement de la souris (évite d'écrire chaque frame).
            if (overlayPosDirty && Input.GetMouseButtonUp(0))
            {
                s.OverlayX = overlayRect.x;
                s.OverlayY = overlayRect.y;
                SettingsManager.Save();
                overlayPosDirty = false;
            }
        }

        private void BuildOverlayRows(MVPSettings s)
        {
            overlayRows.Clear();
            float elapsed = Main.Tracker.CombatElapsedSeconds;
            if (s.OverlayMetric == 1) overlayMetricLabel = "DPS";
            else if (s.OverlayMetric == 2) overlayMetricLabel = Localization.GetStringById("ui.overlay.metric_healing") ?? "Healing";
            else overlayMetricLabel = Localization.GetStringById("ui.overlay.metric_damage") ?? "Damage";

            foreach (var st in Main.Tracker.combatStats.Values)
            {
                if (st == null || st.IsReanimated) continue;
                bool include;
                if (s.OverlayMode == 0) include = st.IsAlly;
                else if (s.OverlayMode == 1) include = true;
                else include = !string.IsNullOrEmpty(s.OverlayPinnedName) && st.Name != null && st.Name.IndexOf(s.OverlayPinnedName, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!include) continue;

                float dmg = st.TotalDamage + st.SummonDamage;
                float val;
                if (s.OverlayMetric == 1) val = dmg / elapsed;
                else if (s.OverlayMetric == 2) val = st.HealingDone + st.VampiricHealing;
                else val = dmg;
                if (val <= 0.001f) continue;

                overlayRows.Add(new OverlayRow { Name = st.Name ?? "?", Value = val, IsAlly = st.IsAlly });
            }

            overlayRows.Sort((a, b) => b.Value.CompareTo(a.Value));
            int max = Mathf.Clamp(s.OverlayMaxRows, 1, 15);
            if (overlayRows.Count > max) overlayRows.RemoveRange(max, overlayRows.Count - max);
            float top = overlayRows.Count > 0 ? overlayRows[0].Value : 1f;
            if (top <= 0f) top = 1f;
            foreach (var r in overlayRows) r.Frac = Mathf.Clamp01(r.Value / top);
        }

        private void DrawOverlayContents(int id)
        {
            var s = SettingsManager.Current;
            float sc = Mathf.Clamp(s.OverlayScale, 0.6f, 2.0f);
            float w = overlayRect.width;
            float h = overlayRect.height;
            float rowH = 22f * sc;
            float headerH = 26f * sc;
            float pad = 8f * sc;
            float op = Mathf.Clamp01(s.OverlayOpacity);

            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.055f, op);
            GUI.DrawTexture(new Rect(0, 0, w, h), overlayPixel);
            GUI.color = new Color(0.769f, 0.635f, 0.396f, op);
            GUI.DrawTexture(new Rect(0, 0, w, 1f), overlayPixel);
            GUI.DrawTexture(new Rect(0, h - 1f, w, 1f), overlayPixel);
            GUI.DrawTexture(new Rect(0, 0, 1f, h), overlayPixel);
            GUI.DrawTexture(new Rect(w - 1f, 0, 1f, h), overlayPixel);
            GUI.color = prev;

            GUIStyle hdr = new GUIStyle(GUI.skin.label) { fontSize = (int)(14 * sc), fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleLeft };
            string title = Localization.GetStringById("ui.overlay.title") ?? "Combat Meter";
            GUI.Label(new Rect(pad, 3f, w - pad * 2, headerH), $"<color=#C4A265>{title}</color>  <color=#8C8C8C>[{overlayMetricLabel}]</color>", hdr);

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(12 * sc), richText = true, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(12 * sc), fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleRight };

            bool cb = SettingsManager.Current.ColorblindMode;
            Color allyBar = cb ? new Color(0.306f, 0.475f, 0.655f, op) : new Color(0.427f, 0.518f, 0.404f, op);
            Color enemyBar = cb ? new Color(0.882f, 0.506f, 0.173f, op) : new Color(0.62f, 0.106f, 0.106f, op);
            float y = headerH + pad * 0.5f;
            for (int i = 0; i < overlayRows.Count; i++)
            {
                var r = overlayRows[i];
                float barW = (w - pad * 2) * r.Frac;
                GUI.color = r.IsAlly ? allyBar : enemyBar;
                GUI.DrawTexture(new Rect(pad, y, Mathf.Max(0f, barW), rowH - 3f), overlayPixel);
                GUI.color = prev;

                string valStr = (s.OverlayMetric == 1) ? r.Value.ToString("0.0") : ((int)r.Value).ToString();
                GUI.Label(new Rect(pad + 4f, y, w - pad * 2 - 62f, rowH - 3f), $"<color=#E2D5B5>{i + 1}. {r.Name}</color>", nameStyle);
                GUI.Label(new Rect(w - pad - 64f, y, 60f, rowH - 3f), $"<color=#C4A265>{valStr}</color>", valStyle);
                y += rowH;
            }

            if (overlayRows.Count == 0)
            {
                GUIStyle empty = new GUIStyle(GUI.skin.label) { fontSize = (int)(11 * sc), fontStyle = FontStyle.Italic, richText = true, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(pad, headerH, w - pad * 2, rowH), $"<color=#8C8C8C>{Localization.GetStringById("ui.overlay.waiting") ?? "Waiting for combat data..."}</color>", empty);
            }

            GUI.DragWindow(new Rect(0, 0, w, h));
        }

		// --- HELPER D'AFFICHAGE DES POURCENTAGES DE TÉLÉMÉTRIE (SÉCURISÉ) ---
        public static string FormatPercentage(float value, float total)
        {
            if (total <= 0f) return "0%";
            float pct = (value * 100f) / total;
            
            // Si la valeur est mathématiquement égale ou extrêmement proche de zéro
            if (Math.Abs(pct) < 0.0001f) return "0%";
            
            // Gestion des fractions positives et négatives proches de zéro
            if (pct > 0f && pct < 1f) return "< 1%";
            if (pct < 0f && pct > -1f) return "> -1%";
            
            // Retour standard pour tous les autres cas (positifs et négatifs)
            return $"{(int)Math.Round(pct)}%";
        }

        private void DrawNavigationButtons(float width, float height)
        {
            // SÉCURITÉ : Ne dessiner les flèches que s'il y a des données
            if (filteredCombatants != null && filteredCombatants.Count > 0)
            {
                GUIStyle navStyle = new GUIStyle(GUI.skin.button) { fontSize = 36, fontStyle = FontStyle.Bold };
                navStyle.normal.textColor = new Color(0.7686f, 0.6353f, 0.3961f); 
                if (currentPageIndex > 0)
                    if (GUI.Button(new Rect(30, height / 2 - 40, 50, 80), "<", navStyle)) { currentPageIndex--; scrollPosition = Vector2.zero; }
                if (currentPageIndex < filteredCombatants.Count - 1)
                    if (GUI.Button(new Rect(width - 80, height / 2 - 40, 50, 80), ">", navStyle)) { currentPageIndex++; scrollPosition = Vector2.zero; }
            }

            GUIStyle closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
            closeStyle.normal.textColor = new Color(0.6196f, 0.1059f, 0.1059f);
            if (GUI.Button(new Rect(width - 70, 20, 50, 50), "X", closeStyle)) 
            {
                showWindow = false;
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(false);
            }
        }
		
		private void DrawEmptyState(float width, float height)
        {
            GUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUIStyle emptyStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            
            // Appel propre au système de localisation
            string title = Localization.GetStringById("ui.empty.title") ?? "No combat data recorded.";
            string subtitle = Localization.GetStringById("ui.empty.subtitle") ?? "(The dashboard will populate automatically after your next battle)";
            
            GUILayout.Label($"<color=#C4A265><b>{title}</b></color>\n\n<size=24><color=#E2D5B5><i>{subtitle}</i></color></size>", emptyStyle);
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        public void ResetUI()
        {
            showWindow = false;
            if (invisibleGlassWall != null) invisibleGlassWall.SetActive(false);
            if (backgroundCache != null)
            {
                foreach (var tex in backgroundCache.Values)
                {
                    if (tex != null && tex != darkBackground)
                    {
                        UnityEngine.Object.Destroy(tex);
                    }
                }
                backgroundCache.Clear();
            }
        }

        void OnDestroy()
        {
            if (invisibleGlassWall != null) UnityEngine.Object.Destroy(invisibleGlassWall);
            if (darkBackground != null) UnityEngine.Object.Destroy(darkBackground);
            if (overlayTexture != null) UnityEngine.Object.Destroy(overlayTexture);
            if (backgroundCache != null)
            {
                foreach (var tex in backgroundCache.Values)
                {
                    if (tex != null && tex != darkBackground)
                    {
                        UnityEngine.Object.Destroy(tex);
                    }
                }
                backgroundCache.Clear();
            }
        }

        // Navigation dans l'historique des combats de la session (parcourir les fiches passées).
        private void DrawHistoryNav()
        {
            if (Main.Tracker == null || Main.Tracker.sessionHistory == null || Main.Tracker.sessionHistory.Count == 0) return;
            int count = Main.Tracker.sessionHistory.Count;

            string liveLabel = Localization.GetStringById("ui.history.live") ?? "Live combat";
            string label = (historyIndex >= 0 && historyIndex < count) ? Main.Tracker.sessionHistory[historyIndex].Label : liveLabel;

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUIStyle navBtn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            GUIStyle lblStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15, alignment = TextAnchor.MiddleCenter };

            if (GUILayout.Button("◄", navBtn, GUILayout.Width(30), GUILayout.Height(24)))
            {
                int ni = (historyIndex == -1) ? count - 1 : Math.Max(0, historyIndex - 1);
                if (ni != historyIndex) { historyIndex = ni; RefreshPagination(); }
            }
            GUILayout.Label($"<color=#597F96><b>{label}</b></color>", lblStyle, GUILayout.Width(170));
            if (GUILayout.Button("►", navBtn, GUILayout.Width(30), GUILayout.Height(24)))
            {
                int ni;
                if (historyIndex == -1 || historyIndex >= count - 1) ni = -1;
                else ni = historyIndex + 1;
                if (ni != historyIndex) { historyIndex = ni; RefreshPagination(); }
            }
            GUILayout.EndHorizontal();
        }

        void DrawBookLayout(float width, float height)
        {
            // 1. Déclaration unique des largeurs de zones pour toute la méthode
            float leftZoneWidth = width * 0.40f;
            float rightZoneWidth = width * 0.58f;

            string colTitle = "#C4A265";    // Muted Imperial Gold (Or officiel)
            string colSub = "#597F96";      // Ardoise Neutre (Gris unifié)
            string colText = "#E2D5B5";     // Warm Parchment (Parchemin off-white)
            string colDanger = "#9E1B1B";   // Deep Crimson (Rouge brique / Sang séché)
            string colSlate = "#597F96";    // Muted Slate Blue (Bleu ardoise feutré) 
            
            GUIStyle sectionTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, richText = true };
            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true, margin = new RectOffset(0,0,4,4) };
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, wordWrap = true, margin = new RectOffset(15,0,0,8) };

            GUILayout.BeginHorizontal(); 

            // ================= ZONE GAUCHE (Identité, Portrait & Recherche) =================
            GUILayout.BeginVertical(GUILayout.Width(leftZoneWidth));
            GUILayout.Space(20);

            // Rendu de la barre de recherche
            GUILayout.BeginHorizontal();
            string filterLabel = Localization.GetStringById("ui.search.filter_label") ?? "Filter:";
            GUILayout.Label($"<color=#8C8C8C><b>{filterLabel}</b></color> ", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 }, GUILayout.Width(70));
            string oldQuery = searchQuery;
            searchQuery = GUILayout.TextField(searchQuery, GUILayout.Width(150), GUILayout.Height(25));
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
            {
                searchQuery = "";
            }
            if (searchQuery != oldQuery)
            {
                ApplySearchFilter();
            }
            GUILayout.EndHorizontal();

            DrawHistoryNav();

            GUILayout.Space(15);

            // Début du conteneur de défilement gauche. Barres masquées (GUIStyle.none) tout en
            // conservant le défilement à la molette. On réserve de la place en bas pour la note épinglée.
            float gradeReserve = (!SettingsManager.Current.ShowDebriefView) ? 165f : 0f;
            leftScrollPosition = GUILayout.BeginScrollView(leftScrollPosition, false, false, GUIStyle.none, GUIStyle.none, GUILayout.Width(leftZoneWidth), GUILayout.Height(height - 80 - gradeReserve));

            // Si aucune fiche ne correspond à la recherche
            if (filteredCombatants.Count == 0)
            {
                GUILayout.EndScrollView(); // Fermeture de sécurité du ScrollView gauche !
                GUILayout.EndVertical(); // Fermeture propre de la colonne de gauche

                GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth)); // Utilisation directe sans redéclaration
                GUILayout.Space(height * 0.4f);
                GUIStyle noMatchStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 24,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                GUILayout.Label($"<color=#9E1B1B>{Localization.GetStringById("ui.search.no_match") ?? "No combat sheets found matching your search."}</color>", noMatchStyle);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                return;
            }

            UnitCombatStats currentStat = filteredCombatants[currentPageIndex]; 

            
            
            string allegiance = "";
            if (currentStat.IsAlly)
            {
                // Si l'analyse tactique est active (ShowDebriefView == true),
                // on masque le titre de "Vanguard" (MVP) et on le remplace par le label neutre "Compagnon / Compagnie Alliée".
                if (currentPageIndex == 0 && !SettingsManager.Current.ShowDebriefView)
                {
                    allegiance = $"<color={colTitle}>{Localization.GetStringById("ui.mvp")}</color>";
                }
                else
                {
                    allegiance = $"<color={colSlate}>{Localization.GetStringById("ui.ally_squad")}</color>";
                }

                // Masqué si l'analyse pure est active (ShowDebriefView == true)
                if (Main.Tracker != null && !SettingsManager.Current.ShowDebriefView)
                {
                    allegiance += $" | <color=#8C8C8C>{Localization.GetStringById("ui.group_efficiency")}</color> <color=#C4A265><b>{Main.Tracker.TeamGlobalGrade}</b></color>";
                }
            }
            else
            {
                allegiance = $"<color={colDanger}>{Localization.GetStringById("ui.enemy_threat")}</color>";
            }
			
            GUILayout.Label(allegiance, new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, richText = true });

            string displayName = currentStat.Name;
            if (currentStat.UnitData != null && Main.Tracker != null && Main.Tracker.factionSwappedUnitsThisCombat.Contains(currentStat.UnitData.UniqueId))
            {
                if (currentStat.IsDominatedSheet)
                {
                    // 1. Détection du buff technique de servitude "Repurpose" sur la créature
                    bool hasRepurposeBuff = false;
                    try
                    {
                        foreach (var b in currentStat.UnitData.Buffs)
                        {
                            if (b.Blueprint != null && (b.Blueprint.name ?? "").ToLower().Contains("repurpose"))
                            {
                                hasRepurposeBuff = true;
                                break;
                            }
                        }
                    }
                    catch (Exception) { }

                    // 2. Détection de la nature de Mort-Vivant (Undead)
                    bool isUndeadType = false;
                    try
                    {
                        string typeName = (currentStat.UnitData.Blueprint?.Type?.name ?? "").ToLower();
                        if (typeName.Contains("undead"))
                        {
                            isUndeadType = true;
                        }
                        else if (currentStat.UnitData.Descriptor?.Progression?.Classes != null) // <-- PROTECTION AJOUTÉE ICI
                        {
                            foreach (var cls in currentStat.UnitData.Descriptor.Progression.Classes)
                            {
                                if (cls.CharacterClass != null && (cls.CharacterClass.name ?? "").ToLower().Contains("undead"))
                                {
                                    isUndeadType = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception) { }

                    // La signature absolue d'une créature réanimée par la Liche :
                    // Elle est de type Mort-Vivant et possède le buff permanent de servitude "Repurpose".
                    if (hasRepurposeBuff && isUndeadType)
                    {
                        displayName += Localization.GetStringById("ui.suffix.reanimated") ?? " (Réanimé)";
                    }
                    else
                    {
                        displayName += Localization.GetStringById("ui.suffix.dominated") ?? " (Dominé)";
                    }
                }
                else
                {
                    displayName += Localization.GetStringById("ui.suffix.non_dominated") ?? " (Non dominé)";
                }
            }
            
            // Adaptation dynamique de la taille du nom : on réduit la police pour qu'un nom long
            // (ex: "Xaëra, Moissonneuse du Silence") tienne entièrement sur une ligne sans être tronqué.
            float nameAvailWidth = Mathf.Max(120f, leftZoneWidth - 20f);
            int nameFontSize = 42;
            GUIStyle nameSizer = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, wordWrap = false };
            Vector2 measuredName = nameSizer.CalcSize(new GUIContent(displayName ?? ""));
            if (measuredName.x > nameAvailWidth && measuredName.x > 0f)
            {
                nameFontSize = Mathf.Clamp((int)(42f * (nameAvailWidth / measuredName.x)), 18, 42);
            }
            GUILayout.Label($"<b><color={colText}>{displayName}</color></b>", new GUIStyle(GUI.skin.label) { fontSize = nameFontSize, richText = true, wordWrap = false }, GUILayout.Width(nameAvailWidth));

            string subTitle = string.Format(Localization.GetStringById("ui.level_short") ?? "Niveau {0}", currentStat.Level);
            if (currentStat.IsAlly && currentStat.MythicPathName != (Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique") && !string.IsNullOrEmpty(currentStat.MythicPathName)) 
            {
                subTitle += $" | {currentStat.MythicPathName}";
            }
            else if (!currentStat.IsAlly) 
            {
                int crDiff = currentStat.CR - Main.Tracker.currentPartyLevel;
                string threatKey = "ui.threat.minor";
                if (crDiff <= -3) threatKey = "ui.threat.insignificant";
                else if (crDiff <= 0) threatKey = "ui.threat.minor";
                else if (crDiff <= 3) threatKey = "ui.threat.high";
                else if (crDiff <= 6) threatKey = "ui.threat.severe";
                else if (crDiff <= 10) threatKey = "ui.threat.extreme";
                else threatKey = "ui.threat.mythic";

                string threatColored = "";
                if (threatKey == "ui.threat.insignificant") threatColored = $"<color={colSub}>{Localization.GetStringById(threatKey)}</color>";
                else if (threatKey == "ui.threat.minor") threatColored = $"<color=#A8D5BA>{Localization.GetStringById(threatKey)}</color>";
                else if (threatKey == "ui.threat.high") threatColored = $"<color=#F0E68C>{Localization.GetStringById(threatKey)}</color>";
                else if (threatKey == "ui.threat.severe") threatColored = $"<color=#FFA07A>{Localization.GetStringById(threatKey)}</color>";
                else if (threatKey == "ui.threat.extreme") threatColored = $"<color={colDanger}>{Localization.GetStringById(threatKey)}</color>";
                else threatColored = $"<color=#800080><b>{Localization.GetStringById(threatKey)}</b></color>";

                subTitle += " | " + string.Format(Localization.GetStringById("ui.danger_cr") ?? "Dangerosité (CR) : {0} | Menace : {1}", currentStat.CR, threatColored);
            }
            GUILayout.Label($"<i><color={colTitle}>{subTitle}</color></i>", new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true });
            GUILayout.Space(10);


            // Calcul de la largeur idéale proportionnelle à la colonne de gauche
            float targetWidth = Mathf.Clamp(leftZoneWidth - 30f, 300f, 350f);

            Texture2D portraitTex = currentStat.CachedPortrait;
            if (portraitTex != null)
            {
                float ratio = (float)portraitTex.width / portraitTex.height;
                GUILayout.Box(portraitTex, GUILayout.Width(targetWidth), GUILayout.Height(targetWidth / ratio));
            }
            else 
            {
                GUILayout.Box(Localization.GetStringById("ui.unknown_face") ?? "Visage Inconnu", GUILayout.Width(targetWidth), GUILayout.Height(targetWidth * 1.33f));
            }
            GUILayout.Space(20);

            GUILayout.EndScrollView(); // --- Fin du conteneur de défilement gauche ---

            // Note du personnage ÉPINGLÉE sous le conteneur défilant : toujours visible, sans avoir à scroller.
            if (!SettingsManager.Current.ShowDebriefView)
            {
                GUILayout.Space(6);
                GUILayout.Label(Localization.GetStringById("ui.operational_rank") ?? "RANG OPÉRATIONNEL", new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.3490f, 0.4980f, 0.5882f) } });
                GUILayout.Label($"{currentStat.Grade}", new GUIStyle(GUI.skin.label) { fontSize = 60, fontStyle = FontStyle.Bold, wordWrap = false, normal = { textColor = GetGradeColor(currentStat.Grade) } });
                string rankTitle = GetRankTitle(currentStat.Grade);
                if (!string.IsNullOrEmpty(rankTitle))
                {
                    GUILayout.Label($"<i><color={ColorToHex(GetGradeColor(currentStat.Grade))}>{rankTitle}</color></i>", new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true });
                }
            }
            GUILayout.EndVertical();

            // ================= ZONE DROITE (Stats Détaillées & Succès) =================
            GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth));
            GUILayout.Space(20);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(rightZoneWidth), GUILayout.Height(height - 40));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.combat_report")}</color>", sectionTitleStyle);
            GUILayout.FlexibleSpace();

            string rawIndexTemplate = Localization.GetStringById("ui.misc.folder_index") ?? "Dossier {0} / {1}";
            string formattedIndex = string.Format(rawIndexTemplate, currentPageIndex + 1, allCombatants.Count);
            string coloredIndex = $"<color={colSub}><b>{formattedIndex}</b></color>";
            GUIStyle pageStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleRight, richText = true };
            GUILayout.Label(coloredIndex, pageStyle, GUILayout.Height(30));
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth * 0.48f));
            GUILayout.Label(string.Format(Localization.GetStringById("ui.damage_done") ?? " <b>Dégâts Infligés :</b> <color={0}>{1}</color>", colText, currentStat.TotalDamage + currentStat.SummonDamage), statStyle);
            
            string instaKillText = currentStat.InstaKills > 0 ? string.Format(Localization.GetStringById("ui.instakills_text") ?? " <color=#C4A265><i>(including {0} Instant Fatalities)</i></color>", currentStat.InstaKills) : "";
            GUILayout.Label(string.Format(Localization.GetStringById("ui.kills_title") ?? " <b>Éliminations :</b> <color={0}>{1}</color>{2}{3}", colText, currentStat.Kills, instaKillText, ""), statStyle);
            GUILayout.Label(string.Format(Localization.GetStringById("ui.crits_aoo") ?? " <b>Critiques :</b> <color={0}>{1}</color> | <b>AoO :</b> <color={0}>{2}</color>", colText, currentStat.Crits, currentStat.AoOs), statStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth * 0.48f));
            GUILayout.Label(string.Format(Localization.GetStringById("ui.damage_taken") ?? " <b>Dégâts Subis :</b> <color={0}>{1}</color>", colText, currentStat.DamageTaken), statStyle);
            GUILayout.Label(string.Format(Localization.GetStringById("ui.attacks_dodged") ?? " <b>Esquives :</b> <color={0}>{1}</color>", colText, currentStat.AttacksDodged), statStyle);
            GUILayout.Label(string.Format(Localization.GetStringById("ui.healing_done") ?? " <b>Soins :</b> <color={0}>{1}</color>", colText, currentStat.HealingDone + currentStat.VampiricHealing), statStyle);
            if (currentStat.SupportBuffsCast > 0)
            {
                GUILayout.Label(string.Format(Localization.GetStringById("ui.support_cast") ?? " <b>Soutiens lancés :</b> <color={0}>{1}</color>", colText, currentStat.SupportBuffsCast), statStyle);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);
			
// ====================================================================
            // LOGIQUE DE SEGMENTATION TÉLÉMÉTRIQUE EN 4 SECTIONS CLAIRES ET STRUCTURÉES
            // ====================================================================
            int totalCC = currentStat.CC_Prone + currentStat.CC_Paralyzed + currentStat.CC_Stunned + currentStat.CC_Frightened + currentStat.CC_Shaken + currentStat.CC_Nauseated + currentStat.CC_Sickened + currentStat.CC_Blinded + currentStat.CC_Entangled + currentStat.CC_Confused + currentStat.CC_Exhausted + currentStat.CC_Fatigued + currentStat.CC_Slowed + currentStat.CC_Staggered + currentStat.CC_Petrified + currentStat.CC_Asleep + currentStat.CC_Dazed + currentStat.CC_Dazzled + currentStat.CC_Helpless + currentStat.CC_Cowering + currentStat.CC_DeathsDoor;

            bool drawOffense = currentStat.TotalDamage > 0 || currentStat.SummonDamage > 0 || currentStat.Kills > 0;
            bool drawDefense = currentStat.DamageTaken > 0 || currentStat.AttacksDodged > 0 || currentStat.TimesDowned > 0;
            bool drawSupport = currentStat.HealingDone > 0 || currentStat.VampiricHealing > 0 || currentStat.SupportBuffsCast > 0 || currentStat.FriendlyFireDmg > 0;
            bool drawMagic = currentStat.IconicSpellsCast > 0 || currentStat.StatDamage > 0 || currentStat.NegativeLevels > 0 || totalCC > 0 ||
                             currentStat.FireDmg > 0 || currentStat.ColdDmg > 0 || currentStat.AcidDmg > 0 || currentStat.ElectricDmg > 0 || currentStat.SonicDmg > 0 ||
                             currentStat.HolyDmg > 0 || currentStat.UnholyDmg > 0 || currentStat.NegativeDmg > 0;

            // Helper local pour tracer un diviseur d'ardoise sombre uniforme (Couleur #3A3A3F)
            Action drawBlockSeparator = () => {
                Color prevColor = GUI.color;
                GUI.color = HexToColor("#3A3A3F");
                GUILayout.Space(8);
                GUILayout.Box(GUIContent.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
                GUILayout.Space(8);
                GUI.color = prevColor;
            };

            // --- SECTION 1 : ATTRIBUTION OFFENSIVE (OFFENSE ET DEGATS INFLIGES) ---
            if (drawOffense)
            {
                string offenseTitle = Localization.GetStringById("ui.section.offense") ?? "[ OFFENSE & DAMAGE DEALT ]";
                GUILayout.Label($"<b><color={colTitle}>{offenseTitle}</color></b>", statStyle);
                GUILayout.Space(4);

                int masterTotalDmg = currentStat.TotalDamage + currentStat.SummonDamage;
                if (masterTotalDmg <= 0) masterTotalDmg = 1;

                var sortedSources = currentStat.DamageBySource
                    .OrderByDescending(kvp => kvp.Value)
                    .ToList();

                foreach (var src in sortedSources)
                {
                    string pctStr = FormatPercentage((float)src.Value, (float)masterTotalDmg);
                    GUILayout.Label($"  • <color={colText}>{src.Key}</color> : <color={colTitle}><b>{src.Value}</b></color> ({pctStr})", detailStyle);
                }

                GUILayout.Label(string.Format(Localization.GetStringById("ui.dmg.biggest_hit") ?? "<color={0}>Maximum Single Hit: {1}</color>", colSub, currentStat.MaxSingleHit), detailStyle);

                if (currentStat.MaxBurstDamage > currentStat.MaxSingleHit)
                {
                    GUILayout.Label(string.Format(Localization.GetStringById("ui.dmg.biggest_burst") ?? "<color={0}>Biggest burst (6s): {1}</color>", colSub, currentStat.MaxBurstDamage), detailStyle);
                }

                if (currentStat.SneakAttackDmg > 0)
                {
                    GUILayout.Label(string.Format(Localization.GetStringById("ui.dmg.sneak_bonus") ?? "<color={0}>(Of the total damage, {1} were dealt by Sneak Attacks)</color>", colSub, currentStat.SneakAttackDmg), detailStyle);
                }

                if (currentStat.OverkillDmg > 0)
                {
                    GUILayout.Label(string.Format(Localization.GetStringById("ui.overkill_text") ?? "<color={0}>Damage dealt to targets under 0 HP: <b>{1}</b></color>", colDanger, currentStat.OverkillDmg), detailStyle);
                }

                if (currentStat.KilledUnits.Count > 0)
                {
                    string killedFoesHeader = Localization.GetStringById("ui.telemetry.killed_foes") ?? "Eliminated Targets:";
                    string killedFoesDetails = string.Join(", ", currentStat.KilledUnits.Select(kvp =>
                        kvp.Value > 1 ? $"<color={colText}>{kvp.Key}</color> <color={colTitle}><b>(x{kvp.Value})</b></color>" : $"<color={colText}>{kvp.Key}</color>"
                    ));
                    GUILayout.Label($"  • <color={colSub}>{killedFoesHeader}</color> {killedFoesDetails}", detailStyle);
                }
            }

            // --- SECTION 2 : ATTRIBUTION DEFENSIVE (DEFENSE, EVITEMENT ET SURVIE) ---
            if (drawDefense)
            {
                if (drawOffense) drawBlockSeparator();
                string defenseTitle = Localization.GetStringById("ui.section.defense") ?? "[ DEFENSE & SURVIVAL ]";
                GUILayout.Label($"<b><color={colTitle}>{defenseTitle}</color></b>", statStyle);
                GUILayout.Space(4);

                int attacksDirected = currentStat.HitsPhysicalTaken + currentStat.AttacksDodged;
                int evaPct = attacksDirected > 0 ? (int)Math.Round((double)currentStat.AttacksDodged * 100 / attacksDirected) : 0;

                string dmgTakenLabel = Localization.GetStringById("ui.telemetry.damage_taken_label") ?? "Damage taken:";
                string dmgPhysLabel = string.Format(Localization.GetStringById("ui.telemetry.damage_physical_label") ?? "including {0} physical", currentStat.PhysicalDmgTaken);
                GUILayout.Label($"  • <color={colText}>{dmgTakenLabel}</color> <color={colDanger}><b>{currentStat.DamageTaken}</b></color> ({dmgPhysLabel})", detailStyle);
                
                if (attacksDirected > 0)
                {
                    string evasionLabel = Localization.GetStringById("ui.telemetry.evasion_label") ?? "Attacks evaded:";
                    string dodgesLabel = Localization.GetStringById("ui.telemetry.dodges_label") ?? "successes";
                    GUILayout.Label($"  • <color={colText}>{evasionLabel}</color> <color={colTitle}><b>{currentStat.AttacksDodged} / {attacksDirected}</b></color> {dodgesLabel} ({evaPct}%)", detailStyle);
                }
                if (currentStat.TimesDowned > 0)
                {
                    string knockdownsLabel = string.Format(Localization.GetStringById("ui.telemetry.knockdowns_label") ?? "Knockdowns (KO): {0} times", currentStat.TimesDowned);
                    GUILayout.Label($"  • <color={colDanger}><b>{knockdownsLabel}</b></color>", detailStyle);
                }
            }

            // --- SECTION 3 : ATTRIBUTION DE BIENFAITS (SOINS ET SOUTIEN) ---
            if (drawSupport)
            {
                if (drawOffense || drawDefense) drawBlockSeparator();
                string supportTitle = Localization.GetStringById("ui.section.support") ?? "[ HEALING & TACTICAL SUPPORT ]";
                GUILayout.Label($"<b><color={colTitle}>{supportTitle}</color></b>", statStyle);
                GUILayout.Space(4);

                if (currentStat.HealingDone > 0 || currentStat.VampiricHealing > 0)
                {
                    string healDetails = "";
                    if (currentStat.HealingDone > 0) healDetails += $"{Localization.GetStringById("ui.heal.spells")} ({currentStat.HealingDone}) | ";
                    if (currentStat.VampiricHealing > 0) healDetails += $"<color={colDanger}>{Localization.GetStringById("ui.heal.vampiric")} ({currentStat.VampiricHealing})</color> | ";
                    if (healDetails.Length > 0) healDetails = healDetails.Substring(0, healDetails.Length - 3);
                    
                    string restorationLabel = Localization.GetStringById("ui.telemetry.healing_label") ?? "Restorations:";
                    GUILayout.Label($"  • <color={colText}>{restorationLabel}</color> {healDetails}", detailStyle);
                }

                if (currentStat.SupportBuffsCast > 0)
                {
                    string supportLabel = Localization.GetStringById("ui.telemetry.support_label") ?? "Boons Bestowed:";
                    string buffsCastLabel = Localization.GetStringById("ui.telemetry.buffs_cast_label") ?? "buffs cast";
                    GUILayout.Label($"  • <color={colText}>{supportLabel}</color> <color={colSub}><b>{currentStat.SupportBuffsCast}</b></color> {buffsCastLabel}", detailStyle);
                }

                if (currentStat.FriendlyFireDmg > 0)
                {
                    GUILayout.Label(string.Format(Localization.GetStringById("ui.friendly_fire_warn") ?? "  • <b><color={0}>Tir Allié : {1} dégâts infligés au groupe</color></b>", colDanger, currentStat.FriendlyFireDmg), detailStyle);
                }
            }

            // --- SECTION 4 : ATTRIBUTION DES FLUX (MAGIE, ENERGIES ET ENTRAVES) ---
            if (drawMagic)
            {
                if (drawOffense || drawDefense || drawSupport) drawBlockSeparator();
                string magicTitle = Localization.GetStringById("ui.section.magic") ?? "[ MAGIC, ENERGIES & CROWD CONTROL ]";
                GUILayout.Label($"<b><color={colTitle}>{magicTitle}</color></b>", statStyle);
                GUILayout.Space(4);

                List<string> energyList = new List<string>();
                string holyLabel = Localization.GetStringById("ui.dmg.holy") ?? "Holy";
                string unholyLabel = Localization.GetStringById("ui.dmg.unholy") ?? "Unholy";
                string negativeLabel = Localization.GetStringById("ui.dmg.negative") ?? "Negative";
                string fireLabel = Localization.GetStringById("ui.dmg.fire") ?? "Fire";
                string coldLabel = Localization.GetStringById("ui.dmg.cold") ?? "Cold";
                string acidLabel = Localization.GetStringById("ui.dmg.acid") ?? "Acid";
                string electricLabel = Localization.GetStringById("ui.dmg.electricity") ?? "Electricity";
                string sonicLabel = Localization.GetStringById("ui.dmg.sonic") ?? "Sonic";

                if (currentStat.HolyDmg > 0) energyList.Add($"{holyLabel} ({currentStat.HolyDmg})");
                if (currentStat.UnholyDmg > 0) energyList.Add($"{unholyLabel} ({currentStat.UnholyDmg})");
                if (currentStat.NegativeDmg > 0) energyList.Add($"{negativeLabel} ({currentStat.NegativeDmg})");
                if (currentStat.FireDmg > 0) energyList.Add($"{fireLabel} ({currentStat.FireDmg})");
                if (currentStat.ColdDmg > 0) energyList.Add($"{coldLabel} ({currentStat.ColdDmg})");
                if (currentStat.AcidDmg > 0) energyList.Add($"{acidLabel} ({currentStat.AcidDmg})");
                if (currentStat.ElectricDmg > 0) energyList.Add($"{electricLabel} ({currentStat.ElectricDmg})");
                if (currentStat.SonicDmg > 0) energyList.Add($"{sonicLabel} ({currentStat.SonicDmg})");

                if (energyList.Count > 0)
                {
                    string energiesLabel = Localization.GetStringById("ui.telemetry.energies_label") ?? "Energies applied:";
                    GUILayout.Label($"  • <color={colText}>{energiesLabel}</color> {string.Join(", ", energyList)}", detailStyle);
                }

                if (currentStat.StatDamage > 0 || currentStat.NegativeLevels > 0)
                {
                    string essenceDrainLabel = Localization.GetStringById("ui.telemetry.essence_drain_label") ?? "Essence drain:";
                    string statDmgLabel = Localization.GetStringById("ui.telemetry.stat_dmg_label") ?? "Attributes:";
                    string levelsDrainedLabel = Localization.GetStringById("ui.telemetry.levels_drained_label") ?? "Levels siphoned:";
                    GUILayout.Label($"  • <color={colText}>{essenceDrainLabel}</color> {statDmgLabel} <color={colTitle}><b>{currentStat.StatDamage}</b></color> | {levelsDrainedLabel} <color={colTitle}><b>{currentStat.NegativeLevels}</b></color>", detailStyle);
                }

                if (currentStat.IconicSpellsCast > 0)
                {
                    string deathSpellsLabel = Localization.GetStringById("ui.telemetry.death_spells_label") ?? "Death or fear spells cast:";
                    GUILayout.Label($"  • <color={colText}>{deathSpellsLabel}</color> <color={colTitle}><b>{currentStat.IconicSpellsCast}</b></color>", detailStyle);
                }

                if (totalCC > 0)
                {
                    string ccDetails = "";
                    if (currentStat.CC_Prone > 0) ccDetails += $"{Localization.GetStringById("ui.cc.prone_label")} ({currentStat.CC_Prone}), ";
                    if (currentStat.CC_Paralyzed > 0 || currentStat.CC_Stunned > 0) ccDetails += $"{Localization.GetStringById("ui.cc.paralyzed_label")} ({currentStat.CC_Paralyzed + currentStat.CC_Stunned}), ";
                    if (currentStat.CC_Petrified > 0) ccDetails += $"{Localization.GetStringById("ui.cc.petrified_label")} ({currentStat.CC_Petrified}), ";
                    if (currentStat.CC_Asleep > 0) ccDetails += $"{Localization.GetStringById("ui.cc.asleep_label")} ({currentStat.CC_Asleep}), ";
                    if (currentStat.CC_Dazed > 0) ccDetails += $"{Localization.GetStringById("ui.cc.dazed_label")} ({currentStat.CC_Dazed}), ";
                    if (currentStat.CC_Frightened > 0 || currentStat.CC_Shaken > 0 || currentStat.CC_Cowering > 0) ccDetails += $"{Localization.GetStringById("ui.cc.frightened_label")} ({currentStat.CC_Frightened + currentStat.CC_Shaken + currentStat.CC_Cowering}), ";
                    if (currentStat.CC_Nauseated > 0 || currentStat.CC_Sickened > 0) ccDetails += $"{Localization.GetStringById("ui.cc.nauseated_label")} ({currentStat.CC_Nauseated + currentStat.CC_Sickened}), ";
                    if (currentStat.CC_Blinded > 0 || currentStat.CC_Dazzled > 0) ccDetails += $"{Localization.GetStringById("ui.cc.blinded_label")} ({currentStat.CC_Blinded + currentStat.CC_Dazzled}), ";
                    if (currentStat.CC_Entangled > 0) ccDetails += $"{Localization.GetStringById("ui.cc.entangled_label")} ({currentStat.CC_Entangled}), ";
                    if (currentStat.CC_Confused > 0) ccDetails += $"{Localization.GetStringById("ui.cc.confused_label")} ({currentStat.CC_Confused}), ";
                    if (currentStat.CC_Fatigued > 0 || currentStat.CC_Exhausted > 0) ccDetails += $"{Localization.GetStringById("ui.cc.exhausted_label")} ({currentStat.CC_Fatigued + currentStat.CC_Exhausted}), ";
                    if (currentStat.CC_Slowed > 0 || currentStat.CC_Staggered > 0) ccDetails += $"{Localization.GetStringById("ui.cc.slowed_label")} ({currentStat.CC_Slowed + currentStat.CC_Staggered}), ";
                    if (currentStat.CC_Helpless > 0) ccDetails += $"{Localization.GetStringById("ui.cc.helpless_label")} ({currentStat.CC_Helpless}), ";
                    if (ccDetails.Length > 0) ccDetails = ccDetails.Substring(0, ccDetails.Length - 2);

                    string ccAppliedLabel = Localization.GetStringById("ui.telemetry.cc_applied_label") ?? "Crowd Control (CC) applied:";
                    string detailsPrefix = Localization.GetStringById("ui.telemetry.details_prefix") ?? "Details:";
                    
                    GUILayout.Label($"  • <color={colText}><b>{ccAppliedLabel} {totalCC}</b></color>", detailStyle);
                    GUILayout.Label($"    <color={colSub}><i>{detailsPrefix} {ccDetails}</i></color>", detailStyle);
                }
            }

            // --- SECTION 5 : SOUS-ENSEMBLE DÉDIÉ AUX INVOCATIONS (FICHE LÉGÈRE) ---
            var sum = currentStat.Summons;
            bool drawSummons = sum.HasAny || currentStat.SummonDamage > 0 || currentStat.SummonKills > 0;
            if (drawSummons)
            {
                if (drawOffense || drawDefense || drawSupport || drawMagic) drawBlockSeparator();
                string summonSectionTitle = Localization.GetStringById("ui.section.summons") ?? "[ SUMMONS ]";
                GUILayout.Label($"<b><color={colTitle}>{summonSectionTitle}</color></b>", statStyle);
                GUILayout.Space(4);

                if (currentStat.SummonDamage > 0 || currentStat.SummonKills > 0)
                {
                    string sDmg = Localization.GetStringById("ui.summons.damage") ?? "Damage dealt:";
                    string sKills = Localization.GetStringById("ui.summons.kills") ?? "Kills:";
                    GUILayout.Label($"  • <color={colText}>{sDmg}</color> <color={colTitle}><b>{currentStat.SummonDamage}</b></color> | <color={colText}>{sKills}</color> <color={colTitle}><b>{currentStat.SummonKills}</b></color>", detailStyle);
                }

                int sumHeal = sum.HealingDone + sum.VampiricHealing;
                if (sumHeal > 0)
                {
                    string sHeal = Localization.GetStringById("ui.summons.healing") ?? "Healing:";
                    GUILayout.Label($"  • <color={colText}>{sHeal}</color> <color={colTitle}><b>{sumHeal}</b></color>", detailStyle);
                }

                if (sum.StatDamage > 0 || sum.NegativeLevels > 0)
                {
                    string sDrain = Localization.GetStringById("ui.summons.drain") ?? "Essence drain:";
                    string sAttr = Localization.GetStringById("ui.telemetry.stat_dmg_label") ?? "Attributes:";
                    string sLvl = Localization.GetStringById("ui.telemetry.levels_drained_label") ?? "Levels siphoned:";
                    GUILayout.Label($"  • <color={colText}>{sDrain}</color> {sAttr} <color={colTitle}><b>{sum.StatDamage}</b></color> | {sLvl} <color={colTitle}><b>{sum.NegativeLevels}</b></color>", detailStyle);
                }

                if (sum.DamageTaken > 0)
                {
                    string sTaken = Localization.GetStringById("ui.summons.damage_taken") ?? "Damage taken:";
                    string sPhys = string.Format(Localization.GetStringById("ui.telemetry.damage_physical_label") ?? "including {0} physical", sum.PhysicalDmgTaken);
                    GUILayout.Label($"  • <color={colText}>{sTaken}</color> <color={colDanger}><b>{sum.DamageTaken}</b></color> ({sPhys})", detailStyle);
                }

                int sumAttacksDirected = sum.HitsPhysicalTaken + sum.AttacksDodged;
                if (sumAttacksDirected > 0)
                {
                    string sEva = Localization.GetStringById("ui.summons.evasion") ?? "Attacks evaded:";
                    GUILayout.Label($"  • <color={colText}>{sEva}</color> <color={colTitle}><b>{sum.AttacksDodged} / {sumAttacksDirected}</b></color>", detailStyle);
                }

                if (sum.TimesDowned > 0)
                {
                    string sDowned = string.Format(Localization.GetStringById("ui.summons.downed") ?? "Summons downed: {0}", sum.TimesDowned);
                    GUILayout.Label($"  • <color={colDanger}>{sDowned}</color>", detailStyle);
                }

                int sumSavesFailed = sum.SavesFortFailed + sum.SavesRefFailed + sum.SavesWillFailed;
                if (sumSavesFailed > 0)
                {
                    string sSaves = Localization.GetStringById("ui.summons.saves_failed") ?? "Saves failed (F/R/W):";
                    GUILayout.Label($"  • <color={colText}>{sSaves}</color> <color={colSub}>{sum.SavesFortFailed} / {sum.SavesRefFailed} / {sum.SavesWillFailed}</color>", detailStyle);
                }

                if (sum.SufferedDebuffs.Count > 0)
                {
                    string sDebuffs = Localization.GetStringById("ui.summons.debuffs") ?? "Afflictions suffered:";
                    string debuffList = string.Join(", ", sum.SufferedDebuffs);
                    GUILayout.Label($"  • <color={colText}>{sDebuffs}</color> <color={colSub}>{debuffList}</color>", detailStyle);
                }
            }

            // --- BLOC DES SERVITEURS RÉANIMÉS (consolidés sur la fiche du maître réanimateur) ---
            if (currentStat.UnitData != null && Main.Tracker != null && !string.IsNullOrEmpty(currentStat.UnitData.UniqueId))
            {
                string masterId = currentStat.UnitData.UniqueId;
                var thralls = Main.Tracker.combatStats.Values
                    .Where(s => s.IsReanimated && s.ReanimatorUniqueId == masterId)
                    .OrderByDescending(s => s.TotalDamage + s.SummonDamage)
                    .ToList();

                if (thralls.Count > 0)
                {
                    if (drawOffense || drawDefense || drawSupport || drawMagic || drawSummons) drawBlockSeparator();
                    string reanimTitle = Localization.GetStringById("ui.section.reanimated") ?? "[ REANIMATED THRALLS ]";
                    GUILayout.Label($"<b><color={colTitle}>{reanimTitle}</color></b>", statStyle);
                    GUILayout.Space(4);

                    int totalThrallDmg = thralls.Sum(t => t.TotalDamage + t.SummonDamage);
                    int totalThrallKills = thralls.Sum(t => t.Kills + t.SummonKills);
                    int totalThrallTaken = thralls.Sum(t => t.DamageTaken);
                    string summaryLbl = Localization.GetStringById("ui.reanimated.summary") ?? "Risen servants: {0} | Damage dealt: {1} | Kills: {2} | Damage taken: {3}";
                    GUILayout.Label($"  • <color={colText}>{string.Format(summaryLbl, thralls.Count, totalThrallDmg, totalThrallKills, totalThrallTaken)}</color>", detailStyle);
                    GUILayout.Space(2);

                    string dmgWord = Localization.GetStringById("ui.reanimated.dmg_word") ?? "dmg";
                    string killsWord = Localization.GetStringById("ui.reanimated.kills_word") ?? "kills";
                    string takenWord = Localization.GetStringById("ui.reanimated.taken_word") ?? "taken";
                    foreach (var t in thralls)
                    {
                        int tDmg = t.TotalDamage + t.SummonDamage;
                        int tKills = t.Kills + t.SummonKills;
                        string line = $"      <color={colSub}>-</color> <color={colText}>{t.Name}</color> : <color={colTitle}><b>{tDmg}</b></color> {dmgWord}";
                        if (tKills > 0) line += $" | <color={colTitle}><b>{tKills}</b></color> {killsWord}";
                        if (t.DamageTaken > 0) line += $" | <color={colDanger}>{t.DamageTaken}</color> {takenWord}";
                        GUILayout.Label(line, detailStyle);
                    }
                }
            }

            // --- BLOC DES DISSIPATIONS (visible uniquement si le personnage a dissipé au moins un effet) ---
            if (currentStat.DispelledCount > 0)
            {
                if (drawOffense || drawDefense || drawSupport || drawMagic || drawSummons) drawBlockSeparator();
                string dispelTitle = Localization.GetStringById("ui.section.dispels") ?? "[ DISPELS ]";
                GUILayout.Label($"<b><color={colTitle}>{dispelTitle}</color></b>", statStyle);
                GUILayout.Space(4);

                string dispelCountLabel = string.Format(Localization.GetStringById("ui.dispels.count") ?? "Effects dispelled: {0}", currentStat.DispelledCount);
                GUILayout.Label($"  • <color={colText}>{dispelCountLabel}</color>", detailStyle);

                if (currentStat.DispelledSpells.Count > 0)
                {
                    string dispelList = string.Join(", ", currentStat.DispelledSpells
                        .OrderByDescending(kvp => kvp.Value)
                        .Select(kvp => kvp.Value > 1
                            ? $"<color={colText}>{kvp.Key}</color> <color={colTitle}><b>(x{kvp.Value})</b></color>"
                            : $"<color={colText}>{kvp.Key}</color>"));
                    GUILayout.Label($"    <color={colSub}>{dispelList}</color>", detailStyle);
                }
            }

            GUILayout.Space(25);

            // =========================================================
            // ZONE DE BASCULE : ACCOLADES VS DÉBRIEFING TACTIQUE (CORRIGÉE)
            // =========================================================
            GUILayout.BeginHorizontal();
            
            // Titre dynamique de la section
            if (!SettingsManager.Current.ShowDebriefView)
            {
                string achTitleKey = currentStat.MythicPathName == (Localization.GetStringById("ui.misc.heroic_ach") ?? "BATTLE ACCOLADES") || string.IsNullOrEmpty(currentStat.MythicPathName) ? "ui.misc.heroic_ach" : "ui.misc.mythic_ach";
                GUILayout.Label($"<color={colTitle}>{Localization.GetStringById(achTitleKey)}</color>", sectionTitleStyle);
            }
            else
            {
                GUILayout.Label($"<color={colTitle}>{Localization.GetStringById("ui.debrief.section_title") ?? "ANALYSE & DÉBRIEFING TACTIQUE"}</color>", sectionTitleStyle);
            }

            // Un espacement fixe de sécurité pour garantir que le bouton reste collé au titre et visible à l'écran
            GUILayout.Space(25);

            // Bouton interactif de permutation robuste
            string toggleBtnText = !SettingsManager.Current.ShowDebriefView 
                ? (Localization.GetStringById("ui.debrief.toggle_to_debrief") ?? " Analyse Tactique") 
                : (Localization.GetStringById("ui.debrief.toggle_to_ach") ?? " Voir les Accolades");

            // Rendu avec contraintes de taille IMGUI standard (Immunisé contre les bugs de layout)
            if (GUILayout.Button($"<color=#E2D5B5><b>{toggleBtnText}</b></color>", GUILayout.Width(180), GUILayout.Height(30)))
            {
                SettingsManager.Current.ShowDebriefView = !SettingsManager.Current.ShowDebriefView;
                SettingsManager.Save(); // Sauvegarde immédiate
            }

            GUILayout.Space(25);

            // Curseur de réglage d'échelle dynamique en temps réel (Incréments de 0.05x pour éviter les sauts brutaux)
            string scaleLabel = Localization.GetStringById("ui.settings.ui_scale") ?? "UI Scale:";
            GUILayout.Label($"<color={colTitle}><b>{scaleLabel}</b></color> <color={colText}>{SettingsManager.Current.UiScale:F2}x</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 }, GUILayout.Width(135));
            float newScale = GUILayout.HorizontalSlider(SettingsManager.Current.UiScale, 0.75f, 1.35f, GUILayout.Width(90), GUILayout.Height(30));
            newScale = (float)Math.Round(newScale / 0.05f) * 0.05f;
            if (Math.Abs(newScale - SettingsManager.Current.UiScale) > 0.01f)
            {
                SettingsManager.Current.UiScale = newScale;
                SettingsManager.Save(); // Enregistrement immédiat dans settings.json
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            // Rendu conditionnel du contenu
            if (!SettingsManager.Current.ShowDebriefView)
            {
                // --- VUE A : ACCOLADES TRADITIONNELLES ---
                // Anti-overdose : on ne met en avant que les 3 hauts faits les plus prestigieux.
                var topAchievements = currentStat.Achievements.OrderByDescending(a => a.Weight).Take(3).ToList();
                if (topAchievements.Count == 0)
                {
                    GUILayout.Label($"<color={colSub}><i>{Localization.GetStringById("ui.misc.no_feats") ?? "Aucun fait d'armes marquant durant cet engagement."}</i></color>", detailStyle);
                }
                else
                {
                    foreach (var ach in topAchievements)
                    {
                        Color achColor = GetAccoladeColor(ach.Tier);
                        GUILayout.Label($"<size=24><b><color={ColorToHex(achColor)}>[{ach.Tier}] {ach.Title}</color></b></size>", new GUIStyle(GUI.skin.label) { richText = true });
                        GUILayout.Label($"<size=18><color={colText}><i>\"{ach.FlavorText}\"</i></color></size>", detailStyle);
                        GUILayout.Space(15);
                    }
                }
            }
            else
            {
                // ====================================================================
                // MODULE I : TABLEAU DE BORD DE TÉLÉMÉTRIE BRUTE PERMANENTE
                // ====================================================================
                GUILayout.Label($"<color={colTitle}><b>" + (Localization.GetStringById("ui.telemetry.section_title") ?? "TELEMETRIE BRUTE DU COMBAT") + "</b></color>", sectionTitleStyle);
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                
                // --- COLONNE OFFENSIVE (Précision physique brute) ---
                GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth * 0.48f));
                GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.accuracy_title") ?? "PRECISION OFFENSIVE") + "</b></color>", statStyle);
                
                int accPct = currentStat.AttacksAttempted > 0 ? (int)Math.Round((double)currentStat.AttacksLanded * 100 / currentStat.AttacksAttempted) : 0;
                string accTemplate = Localization.GetStringById("ui.telemetry.accuracy_rate") ?? "Précision physique : <b>{attacksLanded} / {attacksAttempted}</b> au but ({accuracy}%)";
                string accText = accTemplate
                    .Replace("{attacksLanded}", currentStat.AttacksLanded.ToString())
                    .Replace("{attacksAttempted}", currentStat.AttacksAttempted.ToString())
                    .Replace("{accuracy}", accPct.ToString());
                GUILayout.Label($"<color={colText}>{accText}</color>", detailStyle);
                GUILayout.EndVertical();

                // --- COLONNE DÉFENSIVE (Ratio d'évitement de CA) ---
                GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth * 0.48f));
                GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.evasion_title") ?? "EVITEMENT DEFENSIF") + "</b></color>", statStyle);
                
                int attacksDirected = currentStat.HitsPhysicalTaken + currentStat.AttacksDodged;
                int evaPct = attacksDirected > 0 ? (int)Math.Round((double)currentStat.AttacksDodged * 100 / attacksDirected) : 0;
                string evaTemplate = Localization.GetStringById("ui.telemetry.evasion_rate") ?? "Taux d'évitement : <b>{attacksDodged} / {attacksDirected}</b> esquivés ({evasion}%)";
                string evaText = evaTemplate
                    .Replace("{attacksDodged}", currentStat.AttacksDodged.ToString())
                    .Replace("{attacksDirected}", attacksDirected.ToString())
                    .Replace("{evasion}", evaPct.ToString());
                GUILayout.Label($"<color={colText}>{evaText}</color>", detailStyle);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.Space(15);

                // --- REGISTRE DES JETS DE SAUVEGARDE ÉCHOUÉS (DÉTAILLÉ ET NOMINATIF) ---
                GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.saves_breakdown") ?? "REGISTRE DES SAUVEGARDES ECHOUEES") + "</b></color>", statStyle);
                
                string noneLabel = Localization.GetStringById("ui.telemetry.none") ?? "Aucun";
                string timesLabel = Localization.GetStringById("ui.telemetry.times") ?? "fois";

                // Vigueur (Fortitude)
                string fortHeader = Localization.GetStringById("ui.telemetry.saves_fort_failed") ?? "• <b>Vigueur</b> : ";
                string fortDetails = noneLabel;
                if (currentStat.SavesFortFailedSources.Count > 0)
                {
                    fortDetails = string.Join(", ", currentStat.SavesFortFailedSources.Select(kvp => $"{kvp.Key} ({kvp.Value} {timesLabel})"));
                }
                GUILayout.Label($"<color={colText}>{fortHeader}{fortDetails}</color>", detailStyle);

                // Réflexes (Reflex)
                string refHeader = Localization.GetStringById("ui.telemetry.saves_ref_failed") ?? "• <b>Réflexes</b> : ";
                string refDetails = noneLabel;
                if (currentStat.SavesRefFailedSources.Count > 0)
                {
                    refDetails = string.Join(", ", currentStat.SavesRefFailedSources.Select(kvp => $"{kvp.Key} ({kvp.Value} {timesLabel})"));
                }
                GUILayout.Label($"<color={colText}>{refHeader}{refDetails}</color>", detailStyle);

                // Volonté (Will)
                string willHeader = Localization.GetStringById("ui.telemetry.saves_will_failed") ?? "• <b>Volonté</b> : ";
                string willDetails = noneLabel;
                if (currentStat.SavesWillFailedSources.Count > 0)
                {
                    willDetails = string.Join(", ", currentStat.SavesWillFailedSources.Select(kvp => $"{kvp.Key} ({kvp.Value} {timesLabel})"));
                }
                GUILayout.Label($"<color={colText}>{willHeader}{willDetails}</color>", detailStyle);
                GUILayout.Space(15);

                // --- REGISTRE DES SORTS BLOQUÉS PAR LA RC ENNEMIE ---
                GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.sr_blocks") ?? "SORTS BLOQUES PAR LA RC ENNEMIE") + "</b></color>", statStyle);
                if (currentStat.SpellsResistedSources.Count > 0)
                {
                   string rawSep = Localization.GetStringById("ui.telemetry.target_separator") ?? " sur ";
                   string targetSep = " " + rawSep.Trim() + " ";
                   string srDetails = string.Join("\n", currentStat.SpellsResistedSources.Select(kvp => {
                   string enemiesList = string.Join(", ", kvp.Value.Select(e => e.Value > 1 ? $"{e.Key} (x{e.Value})" : e.Key));
                   int totalTimes = kvp.Value.Values.Sum();
                   return $"• <color={colText}>{kvp.Key}</color> ({totalTimes} {timesLabel}){targetSep}<color={colSub}>{enemiesList}</color>";
                   }));
                   GUILayout.Label(srDetails, detailStyle);
                }
                else
                {
                   GUILayout.Label($"• <color={colSub}>{noneLabel}</color>", detailStyle);
                }
                GUILayout.Space(15);
				
				// --- REGISTRE DES SORTS ET EFFETS ÉVITÉS PAR LES ENNEMIS (JETS DE SAUVEGARDE RÉUSSIS) ---
                GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.spells_saved_by_enemies") ?? "SORTS EVITES PAR LES SAUVEGARDES ENNEMIES") + "</b></color>", statStyle);
                if (currentStat.SpellsSavedSources.Count > 0)
                {
                    // Sécurisation d'aération forcée des espaces horizontaux du séparateur
                    string rawSep = Localization.GetStringById("ui.telemetry.target_separator") ?? " sur ";
                    string targetSep = " " + rawSep.Trim() + " ";

                    string savedDetails = string.Join("\n", currentStat.SpellsSavedSources.Select(kvp => {
                        string enemiesList = string.Join(", ", kvp.Value.Select(e => e.Value > 1 ? $"{e.Key} (x{e.Value})" : e.Key));
                        int totalTimes = kvp.Value.Values.Sum();
                        return $"• <color={colText}>{kvp.Key}</color> ({totalTimes} {timesLabel}){targetSep}<color={colSub}>{enemiesList}</color>";
                    }));
					
                    GUILayout.Label(savedDetails, detailStyle);
                }
                else
                {
                    GUILayout.Label($"• <color={colSub}>{noneLabel}</color>", detailStyle);
                }
                GUILayout.Space(15);

                // --- REGISTRE DES DÉBUFFS SUBIS ---
                GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.suffered_debuffs") ?? "ALTERATIONS D'ETAT SUBIES") + "</b></color>", statStyle);
                string debuffsText = noneLabel;
                if (currentStat.SufferedDebuffs.Count > 0)
                {
                    debuffsText = string.Join(", ", currentStat.SufferedDebuffs.Select(s => $"<color={colText}>{s}</color>"));
                }
                GUILayout.Label($"• {debuffsText}", detailStyle);
                GUILayout.Space(25);
				
				// --- NOUVEAU : AUDIT DÉTAILLÉ DES MODIFICATEURS DE DÉGÂTS D'OWLCAT (DÉBRIEFING) ---
                if (currentStat.DamageModifiersAudit.Count > 0)
                {
                    GUILayout.Label($"<color={colSlate}><b>" + (Localization.GetStringById("ui.telemetry.damage_modifiers_audit") ?? "AUDIT DETAILLE DES MODIFICATEURS DE DEGATS") + "</b></color>", statStyle);
                    
                    // Calcul du dénominateur global (Dégâts totaux du personnage incluant les invocations)
                    int masterTotalDmg = currentStat.TotalDamage + currentStat.SummonDamage;
                    if (masterTotalDmg <= 0) masterTotalDmg = 1;

                    foreach (var srcKvp in currentStat.DamageModifiersAudit)
                    {
                        string sourceKey = srcKvp.Key;
                        var auditDict = srcKvp.Value;
                        if (auditDict.Count > 0)
                        {
                            // En-tête de la source de dégâts
                            GUILayout.Label($"• <color={colTitle}><b>{sourceKey}</b></color> :", detailStyle);
                            
                            //  CORRECT
                         foreach (var modKvp in auditDict.OrderByDescending(m => m.Value))
                         {
                            string pctStr = FormatPercentage((float)modKvp.Value, (float)masterTotalDmg);
                            string sign = modKvp.Value >= 0 ? "+" : ""; // Déclaré en premier
                            GUILayout.Label($"  - <color={colText}>{modKvp.Key}</color> : <color={colTitle}><b>{sign}{modKvp.Value}</b></color> ({pctStr})", detailStyle);
                         }
                        }
                    }
                    GUILayout.Space(25);
                }

            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal(); 
        }

        string ColorToHex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

        // Titre évocateur associé à la note (ton dark-fantasy Owlcat, jamais frustrant même en bas de l'échelle).
        string GetRankTitle(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return "";
            if (grade == "SSS+") return Localization.GetStringById("ui.rank.apotheosis") ?? "Apotheosis";
            if (grade.StartsWith("SSS")) return Localization.GetStringById("ui.rank.legend") ?? "Legend";
            if (grade.StartsWith("SS")) return Localization.GetStringById("ui.rank.paragon") ?? "Paragon";
            if (grade.StartsWith("S")) return Localization.GetStringById("ui.rank.hero") ?? "Hero";
            if (grade.StartsWith("A")) return Localization.GetStringById("ui.rank.veteran") ?? "Veteran";
            if (grade.StartsWith("B")) return Localization.GetStringById("ui.rank.seasoned") ?? "Seasoned";
            if (grade.StartsWith("C")) return Localization.GetStringById("ui.rank.initiate") ?? "Initiate";
            if (grade.StartsWith("D")) return Localization.GetStringById("ui.rank.recruit") ?? "Recruit";
            if (grade.StartsWith("R")) return Localization.GetStringById("ui.rank.reservist") ?? "Reservist";
            return Localization.GetStringById("ui.rank.untested") ?? "Untested";
        }

        private static readonly string[] s_GradeOrder = { "SSS+", "SSS", "SSS-", "SS+", "SS", "SS-", "S+", "S", "S-", "A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D+", "D", "D-", "F", "FFF-", "R" };
        private static int GradeRankValue(string g)
        {
            int i = Array.IndexOf(s_GradeOrder, g);
            return i < 0 ? 999 : i;
        }

        // Construit le résumé "3 meilleures notes ×occurrences" d'un personnage pour le registre.
        private string BuildTop3Grades(Dictionary<string, int> gc)
        {
            if (gc == null || gc.Count == 0) return "—";
            var top = gc.Keys.OrderBy(GradeRankValue).Take(3).ToList();
            return string.Join("  ", top.Select(g => $"{g}<size=13> ×{gc[g]}</size>"));
        }

        Color GetGradeColor(string grade)
        {
            bool cb = SettingsManager.Current.ColorblindMode;
            if (grade.Contains("S")) return HexToColor("#C4A265"); // Or Royal
            if (grade.Contains("A")) return HexToColor("#597F96"); // Ardoise Neutre
            if (grade.Contains("B")) return HexToColor(cb ? "#4E79A7" : "#6D8467"); // Vert Sauge (bleu en mode daltonien)
            if (grade.Contains("C")) return HexToColor(cb ? "#4E79A7" : "#6D8467"); // Vert Sauge (bleu en mode daltonien)
            if (grade.Contains("F")) return HexToColor(cb ? "#E1812C" : "#9E1B1B"); // Rouge (orange en mode daltonien)
            if (grade.Contains("R")) return HexToColor("#597F96"); // Ardoise Neutre (Réserve)
            return HexToColor("#E2D5B5"); // Parchemin
        }
		
		// --- UTILITAIRES DE RENDU GÉOMÉTRIQUE SÉCURISÉS (WOTR-STYLE) ---
        private static Texture2D borderTexture;

        private static void DrawProceduralFrame(Rect rect, Color borderColor, float thickness)
        {
            if (borderTexture == null)
            {
                borderTexture = new Texture2D(1, 1);
                borderTexture.SetPixel(0, 0, Color.white);
                borderTexture.Apply();
            }
            Color savedColor = GUI.color;
            GUI.color = borderColor;
            
            // Bord supérieur
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), borderTexture);
            // Bord inférieur
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), borderTexture);
            // Bord gauche
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), borderTexture);
            // Bord droit
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), borderTexture);
            
            GUI.color = savedColor;
        }

        private static void DrawWotRCornerBrackets(Rect rect, Color goldColor, float thickness, float bracketSize)
        {
            if (borderTexture == null)
            {
                borderTexture = new Texture2D(1, 1);
                borderTexture.SetPixel(0, 0, Color.white);
                borderTexture.Apply();
            }
            Color savedColor = GUI.color;
            GUI.color = goldColor;

            // Angle supérieur gauche
            GUI.DrawTexture(new Rect(rect.x, rect.y, bracketSize, thickness), borderTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, bracketSize), borderTexture);

            // Angle supérieur droit
            GUI.DrawTexture(new Rect(rect.xMax - bracketSize, rect.y, bracketSize, thickness), borderTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, bracketSize), borderTexture);

            // Angle inférieur gauche
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, bracketSize, thickness), borderTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - bracketSize, thickness, bracketSize), borderTexture);

            // Angle inférieur droit
            GUI.DrawTexture(new Rect(rect.xMax - bracketSize, rect.yMax - thickness, bracketSize, thickness), borderTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMax - bracketSize, thickness, bracketSize), borderTexture);

            GUI.color = savedColor;
        }

        private Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            return Color.white;
        }
		
		private Color GetAccoladeColor(string tier)
        {
            if (tier.Contains("SSS") || tier.Contains("SS")) return HexToColor("#C4A265"); // Or royal
            if (tier.Contains("S") || tier.Contains("A")) return HexToColor("#597F96");   // Ardoise
            if (tier.Contains("B") || tier.Contains("C")) return HexToColor("#6D8467");   // Vert sauge
            return HexToColor("#9E1B1B"); // Rouge brique pour les échecs / faibles grades
        }
    }
	// Instantané léger d'un combat terminé, conservé en mémoire pour l'historique de session.
    public class CombatSnapshot
    {
        public string Label = "";
        public string TeamGrade = "";
        public List<UnitCombatStats> Stats = new List<UnitCombatStats>();
    }

	public class CombatTracker :
        IPartyCombatHandler, IGlobalSubscriber,
        IGlobalRulebookHandler<RuleDealDamage>, IGlobalRulebookHandler<RuleHealDamage>, 
        IGlobalRulebookHandler<RuleDealStatDamage>, IGlobalRulebookHandler<RuleAttackRoll>, 
        IGlobalRulebookHandler<RuleDrainEnergy>, IGlobalRulebookHandler<RuleCastSpell>,
        IGlobalRulebookHandler<RuleSavingThrow>,
        IGlobalRulebookHandler<RuleSpellResistanceCheck>,
        IUnitBuffHandler,
        IAreaHandler,
        IUnitHandler,
        IGlobalRulebookHandler<RuleDispelMagic>,
        IGlobalRulebookHandler<RuleCombatManeuver>,
        IUnitResurrectedHandler
    {
        // --- HELPER PRIVÉ D'INCRÉMENTATION SÉCURISÉE ---
        private void IncrementSourceCount(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (dict.ContainsKey(key))
            {
                dict[key]++;
            }
            else
            {
                dict[key] = 1;
            }
        }
		
		private bool isCombatActive = false;
        private float combatStartRealtime = 0f;
        // Accès public en lecture seule pour l'overlay temps réel.
        public bool IsInCombat => isCombatActive;
        public float CombatElapsedSeconds => Mathf.Max(0.1f, UnityEngine.Time.realtimeSinceStartup - combatStartRealtime);
        public Dictionary<string, UnitCombatStats> combatStats = new Dictionary<string, UnitCombatStats>();
        // Historique des combats de la session (les 20 derniers), pour permettre de reconsulter les fiches passées.
        public List<CombatSnapshot> sessionHistory = new List<CombatSnapshot>();
        private int sessionCombatCounter = 0;
        private HashSet<string> deadUnitsThisCombat = new HashSet<string>();
        public HashSet<string> factionSwappedUnitsThisCombat = new HashSet<string>();
        // Allégeance de référence figée par UniqueId (capturée au début du combat / à l'apparition),
        // afin de détecter de façon fiable les dominations et réanimations survenues en cours de combat.
        private Dictionary<string, bool> baselineAllegiance = new Dictionary<string, bool>();
        private int totalEnemyHPAtStart = 1;
        private float totalEnemyCombatWeight = 1f;
        // Références de l'équipe figées au début du combat, servant à normaliser la menace ennemie.
        private int totalPartyHPAtStart = 1;
        private int partySizeAtStart = 1;
		private Dictionary<string, UnitEntityData> doomedTargets = new Dictionary<string, UnitEntityData>();

		// --- NOTE GLOBALE DE L'ÉQUIPE ---
        public string TeamGlobalGrade = "C";

        // --- Référence absolue du niveau de l'équipe ---
        public int currentPartyLevel = 1;

        private float GetTargetMultiplier(UnitEntityData target)
        {
            if (target == null || target.Blueprint == null) return 1f;
            float cr = Math.Max(1f, target.Blueprint.CR);
            float level = Math.Max(1f, currentPartyLevel);
            int activeCount = Game.Instance.Player.PartyCharacters.Count;
            float partySizeFactor = 6f / Math.Max(1f, activeCount);
            float multiplier = (cr / level) * partySizeFactor;
            return Mathf.Clamp(multiplier, 0.2f, 3.0f);
        }

        private UnitEntityData GetTrueInitiator(UnitEntityData unit, out bool isSummon)
        {
            isSummon = false;
            if (unit == null) return null;
            if (unit.IsPet) return unit;
            var summonPart = unit.Get<UnitPartSummonedMonster>();
            if (summonPart != null && summonPart.Summoner != null) 
            {
                isSummon = true;
                return summonPart.Summoner;
            }
            return unit; 
        }

        private bool IsSummonUnit(UnitEntityData unit)
        {
            if (unit == null || unit.IsPet) return false;
            // Un serviteur réanimé n'est jamais traité comme une invocation générique :
            // il possède sa propre présentation dédiée.
            if (IsReanimatedThrall(unit)) return false;
            var summonPart = unit.Get<UnitPartSummonedMonster>();
            return summonPart != null && summonPart.Summoner != null;
        }

        // Signature déterministe d'un serviteur réanimé par la Liche :
        // créature de type MORT-VIVANT portant un buff de servitude (Repurpose / Doom of Servitude /
        // Flay for Purpose). Le buff undead permanent est appliqué uniquement APRÈS la mort et le relèvement,
        // ce qui distingue le serviteur relevé de l'ennemi vivant simplement maudit.
        private bool IsReanimatedThrall(UnitEntityData unit)
        {
            if (unit == null) return false;
            try
            {
                if (unit.Buffs == null) return false;
                string typeName = (unit.Blueprint?.Type?.name ?? "").ToLower();
                bool isUndeadType = typeName.Contains("undead");
                foreach (var b in unit.Buffs)
                {
                    string bn = (b?.Blueprint?.name ?? "").ToLower();
                    if (string.IsNullOrEmpty(bn)) continue;
                    bool servitude = bn.Contains("repurpose") || bn.Contains("servitude") || bn.Contains("flayforpurpose");
                    if (!servitude) continue;
                    // Serviteur RELEVÉ (et non ennemi vivant simplement maudit) : buff undead explicite
                    // (RepurposeBuffUndead), OU créature devenue de type mort-vivant tout en portant la servitude.
                    if (bn.Contains("undead") || isUndeadType) return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        // Lecture de l'alignement réel de la créature (0 = neutre, 1 = bon, 2 = mauvais).
        // Sert à teinter l'ambiance de la fiche selon l'axe Bien/Mal.
        public static int GetAlignmentTone(UnitEntityData unit)
        {
            try
            {
                if (unit == null || unit.Descriptor == null || unit.Descriptor.Alignment == null) return 0;
                string a = unit.Descriptor.Alignment.ValueVisible.ToString();
                if (a.IndexOf("Good", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
                if (a.IndexOf("Evil", StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            }
            catch (Exception) { }
            return 0;
        }

        // Résout le lanceur (la Liche) à l'origine de la réanimation, via le contexte du buff de servitude.
        private UnitEntityData GetReanimator(UnitEntityData unit)
        {
            if (unit == null) return null;
            try
            {
                if (unit.Buffs == null) return null;
                string typeName = (unit.Blueprint?.Type?.name ?? "").ToLower();
                bool isUndeadType = typeName.Contains("undead");
                foreach (var b in unit.Buffs)
                {
                    string bn = (b?.Blueprint?.name ?? "").ToLower();
                    if (string.IsNullOrEmpty(bn)) continue;
                    bool servitude = bn.Contains("repurpose") || bn.Contains("servitude") || bn.Contains("flayforpurpose");
                    if (servitude && (bn.Contains("undead") || isUndeadType) && b.Context != null && b.Context.MaybeCaster != null)
                    {
                        return b.Context.MaybeCaster;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        // Détermination robuste de l'allégeance relative à la FACTION DU JOUEUR (et non à un personnage
        // de référence potentiellement mort ou lui-même dominé). S'appuie sur IsPlayersEnemy d'Owlcat,
        // qui reflète en temps réel le groupe de combat courant de la créature.
        private bool IsCurrentlyAlly(UnitEntityData unit)
        {
            if (unit == null) return false;
            if (unit.IsPlayerFaction) return true;
            // Un serviteur réanimé combat pour la Liche : il est du côté du joueur, quel que soit
            // l'état exact de sa faction dans le moteur.
            if (IsReanimatedThrall(unit)) return true;
            try { return !unit.IsPlayersEnemy; }
            catch (Exception) { return false; }
        }

        // Renvoie l'allégeance de référence figée pour une créature. Si aucune référence n'existe encore
        // (créature observée pour la première fois), on fige son état courant comme référence.
        private bool GetBaselineAllegiance(UnitEntityData unit, bool currentlyAlly)
        {
            if (unit == null) return currentlyAlly;
            string id = unit.UniqueId;
            if (string.IsNullOrEmpty(id)) return currentlyAlly;
            if (baselineAllegiance.TryGetValue(id, out bool baseline)) return baseline;
            baselineAllegiance[id] = currentlyAlly;
            return currentlyAlly;
        }

        private UnitCombatStats GetSummonerStats(UnitEntityData unit)
        {
            if (!IsSummonUnit(unit)) return null;
            var summonPart = unit.Get<UnitPartSummonedMonster>();
            if (summonPart == null || summonPart.Summoner == null) return null;
            return GetOrAddStats(summonPart.Summoner, out _);
        }

        private void AddSavingThrowStats(UnitCombatStats stats, bool isSummon, RuleSavingThrow evt, string sourceName)
        {
            if (stats == null || evt == null) return;
            if (isSummon)
            {
                if (!evt.IsPassed)
                {
                    stats.Summons.SavesFailed++;
                    if (evt.Type == SavingThrowType.Fortitude)
                    {
                        stats.Summons.SavesFortFailed++;
                        IncrementSourceCount(stats.Summons.SavesFortFailedSources, sourceName);
                    }
                    else if (evt.Type == SavingThrowType.Reflex)
                    {
                        stats.Summons.SavesRefFailed++;
                        IncrementSourceCount(stats.Summons.SavesRefFailedSources, sourceName);
                    }
                    else if (evt.Type == SavingThrowType.Will)
                    {
                        stats.Summons.SavesWillFailed++;
                        IncrementSourceCount(stats.Summons.SavesWillFailedSources, sourceName);
                    }
                }
                else
                {
                    stats.Summons.SavesSucceeded++;
                    if (evt.Type == SavingThrowType.Fortitude) stats.Summons.SavesFortSucceeded++;
                    else if (evt.Type == SavingThrowType.Reflex) stats.Summons.SavesRefSucceeded++;
                    else if (evt.Type == SavingThrowType.Will) stats.Summons.SavesWillSucceeded++;
                }
                return;
            }

            if (!evt.IsPassed)
            {
                stats.SavesFailed++;
                if (evt.Type == SavingThrowType.Fortitude)
                {
                    stats.SavesFortFailed++;
                    IncrementSourceCount(stats.SavesFortFailedSources, sourceName);
                }
                else if (evt.Type == SavingThrowType.Reflex)
                {
                    stats.SavesRefFailed++;
                    IncrementSourceCount(stats.SavesRefFailedSources, sourceName);
                }
                else if (evt.Type == SavingThrowType.Will)
                {
                    stats.SavesWillFailed++;
                    IncrementSourceCount(stats.SavesWillFailedSources, sourceName);
                }
            }
            else
            {
                stats.SavesSucceeded++;
                if (evt.Type == SavingThrowType.Fortitude) stats.SavesFortSucceeded++;
                else if (evt.Type == SavingThrowType.Reflex) stats.SavesRefSucceeded++;
                else if (evt.Type == SavingThrowType.Will) stats.SavesWillSucceeded++;
            }
        }

		public static string ExtractMythicPath(UnitEntityData unit, out string internalName)
        {
            internalName = "MythicHero";
            if (unit?.Progression == null || unit.Progression.MythicLevel == 0) 
                return Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique";

            var mythicClasses = unit.Progression.Classes
                .Where(cls => cls.CharacterClass.IsMythic && cls.Level > 0)
                .ToList();

            if (mythicClasses.Count == 0)
                return Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique";

            // 1. Priorité absolue : Les voies mythiques dites "supérieures" (IsHigherMythic)
            var higherMythic = mythicClasses.FirstOrDefault(cls => cls.CharacterClass.IsHigherMythic);
            if (higherMythic != null)
            {
                internalName = higherMythic.CharacterClass.name;
                return higherMythic.CharacterClass.LocalizedName.ToString();
            }

            // 2. Priorité secondaire : Les classes spécialisées (non génériques)
            var specializedClass = mythicClasses.FirstOrDefault(cls => {
                string name = (cls.CharacterClass.name ?? "").ToLower();
                return !name.Contains("mythichero") && !name.Contains("mythiccompanion");
            });

            if (specializedClass != null)
            {
                internalName = specializedClass.CharacterClass.name;
                return specializedClass.CharacterClass.LocalizedName.ToString();
            }

            // 3. Repli : La première classe mythique trouvée (générique)
            internalName = mythicClasses[0].CharacterClass.name;
            return mythicClasses[0].CharacterClass.LocalizedName.ToString();
        }

        private UnitCombatStats GetOrAddStats(UnitEntityData unit, out bool isSummonAction)
        {
            isSummonAction = false;
            if (unit == null) return null;
            UnitEntityData trueInitiator = GetTrueInitiator(unit, out isSummonAction);
            if (trueInitiator == null) return null;

            // Allégeance courante robuste (relative à la faction du joueur) et comparaison à la
            // référence figée par UniqueId pour détecter une domination ou une réanimation.
            bool currentlyAlly = IsCurrentlyAlly(trueInitiator);
            bool baseline = GetBaselineAllegiance(trueInitiator, currentlyAlly);
            bool isReanimated = IsReanimatedThrall(trueInitiator);
            // La réanimation est un basculement d'allégeance déterministe, même si le moteur ne
            // reflète pas proprement le changement de faction ou réutilise un nouvel UniqueId.
            bool isSwapped = (baseline != currentlyAlly) || isReanimated;

            string name = trueInitiator.CharacterName ?? "Inconnu";
            string statsKey = isSwapped ? name + " [DOMINATED]" : name;

            if (!combatStats.ContainsKey(statsKey))
            {
                string mythicInternal;
                string mythic = ExtractMythicPath(trueInitiator, out mythicInternal);
                string mythicLower = mythic.ToLower();
                string internalLower = mythicInternal.ToLower();
                
                // La détection de l'alignement maléfique s'appuie également sur le nom interne
                bool isEvil = mythicLower.Contains("lich") || mythicLower.Contains("demon") || mythicLower.Contains("swarm") || mythicLower.Contains("devil") || mythicLower.Contains("diable")
                    || internalLower.Contains("lich") || internalLower.Contains("demon") || internalLower.Contains("swarm") || internalLower.Contains("devil");

                combatStats[statsKey] = new UnitCombatStats { 
                    Name = (trueInitiator.IsPet) ? $"{name}{Localization.GetStringById("ui.pet_suffix") ?? " (Familier)"}" : name,
                    UnitData = trueInitiator,
                    IsAlly = currentlyAlly,
                    OriginallyAlly = baseline, // Allégeance de référence figée par UniqueId
                    IsDominatedSheet = isSwapped,
                    Level = trueInitiator.Progression?.CharacterLevel ?? 1,
                    CR = trueInitiator.Blueprint?.CR ?? 1, 
                    MythicPathName = mythic,
                    MythicPathInternalName = mythicInternal, 
                    IsEvil = isEvil,
                    Gender = trueInitiator.Gender,
                    IsReanimated = isReanimated,
                    AlignmentTone = GetAlignmentTone(trueInitiator)
                };

                if (isReanimated)
                {
                    var reanimator = GetReanimator(trueInitiator);
                    if (reanimator != null) combatStats[statsKey].ReanimatorUniqueId = reanimator.UniqueId;
                }
            }
            else
            {
                // Mise à jour de l'attitude courante si la fiche existe déjà
                var existingStats = combatStats[statsKey];
                existingStats.IsAlly = currentlyAlly;
                // Préserve l'état de domination s'il a déjà été acté, ou si une nouvelle bascule survient
                existingStats.IsDominatedSheet = existingStats.IsDominatedSheet || isSwapped;
                if (isReanimated)
                {
                    existingStats.IsReanimated = true;
                    if (string.IsNullOrEmpty(existingStats.ReanimatorUniqueId))
                    {
                        var reanimator = GetReanimator(trueInitiator);
                        if (reanimator != null) existingStats.ReanimatorUniqueId = reanimator.UniqueId;
                    }
                }
            }

            if (combatStats[statsKey].IsDominatedSheet)
            {
                factionSwappedUnitsThisCombat.Add(trueInitiator.UniqueId);
            }

            return combatStats[statsKey];
        }

        public void HandlePartyCombatStateChanged(bool inCombat)
        {
            try
            {
                isCombatActive = inCombat;
                if (inCombat)
                {
                    combatStartRealtime = UnityEngine.Time.realtimeSinceStartup;
                    combatStats.Clear();
                    deadUnitsThisCombat.Clear();
                    doomedTargets.Clear();
                    factionSwappedUnitsThisCombat.Clear();
                    baselineAllegiance.Clear();
                    if (CombatMVP_UI.Instance != null) CombatMVP_UI.Instance.showWindow = false;
                    totalEnemyHPAtStart = 0;
                    totalEnemyCombatWeight = 0f; 
                    var mainChar = Game.Instance.Player.MainCharacter.Value;
                    if (mainChar != null && mainChar.Progression != null)
                    {
                        currentPartyLevel = mainChar.Progression.CharacterLevel;
                    }
                    if (currentPartyLevel <= 0) currentPartyLevel = 1;

                    // --- RÉFÉRENCES DE L'ÉQUIPE (réservoir de PV et effectif) POUR LA NORMALISATION DE LA MENACE ---
                    totalPartyHPAtStart = 0;
                    partySizeAtStart = 1;
                    try
                    {
                        partySizeAtStart = Math.Max(1, Game.Instance.Player.PartyCharacters.Count);
                        foreach (var pc in Game.Instance.Player.PartyAndPets)
                        {
                            if (pc != null && pc.Descriptor != null && !pc.Descriptor.State.IsDead)
                            {
                                totalPartyHPAtStart += pc.MaxHP;
                            }
                        }
                    }
                    catch (Exception) { }
                    if (totalPartyHPAtStart <= 0) totalPartyHPAtStart = currentPartyLevel * partySizeAtStart * 10;

                    // --- SECURISATION DU PRE-BUFFING ---
                    try
                    {
                        foreach (var unit in Game.Instance.Player.PartyAndPets)
                        {
                            foreach (var buff in unit.Buffs)
                            {
                                RecordBuffSafely(buff, forceDuringCombatStart: true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.Logger.Error("[CombatMVP] Erreur lors de la capture des pre-buffs : " + ex.Message);
                    }

                    foreach (var unit in Game.Instance.State.Units)
                    {
                        if (unit == null) continue;
                        // Fige l'allégeance de référence de chaque créature présente à l'ouverture du combat.
                        if (!string.IsNullOrEmpty(unit.UniqueId))
                        {
                            baselineAllegiance[unit.UniqueId] = IsCurrentlyAlly(unit);
                        }

                        if (!IsCurrentlyAlly(unit) && !unit.Descriptor.State.IsDead)
                        {
                            totalEnemyHPAtStart += unit.MaxHP;
                            int cr = unit.Blueprint?.CR ?? 1;
                            totalEnemyCombatWeight += unit.MaxHP * (1f + (cr / 5f));
                        }
                    }
                    if (totalEnemyHPAtStart <= 0) totalEnemyHPAtStart = 1;
                    if (totalEnemyCombatWeight <= 0) totalEnemyCombatWeight = 1f;
                }
                else
                {
                    AnalyzeCombatPerformance();
                    UpdateRunLedger();
                    CaptureSessionSnapshot();
#if DEBUG
                    ExportCombatLogToUMM();
#endif
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans HandlePartyCombatStateChanged : " + ex);
            }
        }
		// Échelle de rang propre aux ennemis, appliquée au score de menace normalisé.
        // SSS+ ne récompense qu'une domination réelle de la rencontre (dégâts massifs et/ou mises à mort).
        // Fige les fiches du combat qui vient de se terminer dans l'historique de session (mémoire seulement).
        private void CaptureSessionSnapshot()
        {
            try
            {
                var kept = combatStats.Values
                    .Where(s => s != null && s.HasRealContribution && ((s.IsAlly && !s.IsReanimated) || !s.IsAlly))
                    .ToList();
                if (kept.Count == 0) return;

                sessionCombatCounter++;
                var snap = new CombatSnapshot
                {
                    Label = string.Format(Localization.GetStringById("ui.history.combat_label") ?? "Combat #{0} — {1}", sessionCombatCounter, DateTime.Now.ToString("HH:mm")),
                    TeamGrade = TeamGlobalGrade,
                    Stats = kept
                };
                sessionHistory.Add(snap);
                while (sessionHistory.Count > 20) sessionHistory.RemoveAt(0);
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans CaptureSessionSnapshot : " + ex.Message);
            }
        }

        private void UpdateRunLedger()
        {
            try
            {
                if (!SettingsManager.Current.LedgerEnabled) return;
                if (combatStats.Count == 0) return;

                string runId = "default";
                string campaignName = "";
                try
                {
                    var mc = Game.Instance.Player.MainCharacter.Value;
                    if (mc != null)
                    {
                        if (!string.IsNullOrEmpty(mc.UniqueId)) runId = mc.UniqueId;
                        campaignName = mc.CharacterName ?? "";
                    }
                }
                catch (Exception) { }

                LedgerManager.EnsureRun(runId, campaignName);
                var ledger = LedgerManager.Current;
                ledger.TotalCombats++;

                foreach (var stat in combatStats.Values)
                {
                    if (stat == null || stat.UnitData == null) continue;

                    int myCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Nauseated + stat.CC_Confused + stat.CC_Blinded + stat.CC_Prone + stat.CC_Entangled + stat.CC_Exhausted + stat.CC_Fatigued + stat.CC_Shaken + stat.CC_Sickened + stat.CC_Asleep + stat.CC_Petrified + stat.CC_Slowed + stat.CC_Staggered + stat.CC_Dazed + stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;
                    int myDamage = stat.TotalDamage + stat.SummonDamage;

                    if (stat.IsAlly && !stat.IsReanimated)
                    {
                        if (!stat.HasRealContribution) continue;
                        string key = stat.Name ?? "?";
                        if (!ledger.Characters.TryGetValue(key, out var lc))
                        {
                            lc = new LedgerCharacter { Name = key };
                            ledger.Characters[key] = lc;
                        }
                        lc.TotalDamage += myDamage;
                        lc.TotalHealing += stat.HealingDone + stat.VampiricHealing;
                        lc.TotalDamageTaken += stat.DamageTaken;
                        lc.TotalKills += stat.Kills + stat.SummonKills;
                        lc.TotalCC += myCC;
                        lc.TimesDowned += stat.TimesDowned;
                        lc.Combats += 1;
                        if (stat.MaxSingleHit > lc.MaxSingleHit) lc.MaxSingleHit = stat.MaxSingleHit;
                        if (stat.TotalScore > lc.BestScore)
                        {
                            lc.BestScore = stat.TotalScore;
                            lc.BestGrade = stat.Grade;
                        }
                        if (!string.IsNullOrEmpty(stat.Grade))
                        {
                            if (lc.GradeCounts == null) lc.GradeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                            lc.GradeCounts.TryGetValue(stat.Grade, out int gc);
                            lc.GradeCounts[stat.Grade] = gc + 1;
                        }
                    }
                    else if (!stat.IsAlly)
                    {
                        if (myDamage <= 0 && stat.Kills <= 0) continue;
                        string key = stat.Name ?? "?";
                        if (!ledger.Enemies.TryGetValue(key, out var le))
                        {
                            le = new LedgerEnemy { Name = key };
                            ledger.Enemies[key] = le;
                        }
                        le.DamageToParty += myDamage;
                        le.KillsOnParty += stat.Kills;
                    }
                }

                LedgerManager.Save();
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans UpdateRunLedger : " + ex.Message);
            }
        }

        private string GradeEnemyThreat(UnitCombatStats stat)
        {
            float score = stat.TotalScore;
            if (score >= 300f) return "SSS+";
            if (score >= 245f) return "SSS";
            if (score >= 200f) return "SSS-";
            if (score >= 165f) return "SS+";
            if (score >= 135f) return "SS";
            if (score >= 110f) return "SS-";
            if (score >= 92f)  return "S+";
            if (score >= 76f)  return "S";
            if (score >= 62f)  return "S-";
            if (score >= 50f)  return "A+";
            if (score >= 40f)  return "A";
            if (score >= 32f)  return "A-";
            if (score >= 25f)  return "B+";
            if (score >= 19f)  return "B";
            if (score >= 14f)  return "B-";
            if (score >= 10f)  return "C+";
            if (score >= 7f)   return "C";
            if (score >= 4f)   return "C-";
            if (score >= 2f)   return "D";
            if (score >= 1f)   return "D-";

            int totalCC = stat.CC_Prone + stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened +
                          stat.CC_Shaken + stat.CC_Nauseated + stat.CC_Sickened + stat.CC_Blinded +
                          stat.CC_Entangled + stat.CC_Confused + stat.CC_Exhausted + stat.CC_Fatigued +
                          stat.CC_Slowed + stat.CC_Staggered + stat.CC_Petrified + stat.CC_Asleep +
                          stat.CC_Dazed + stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;
            bool didSomething = stat.TotalDamage > 0 || stat.SummonDamage > 0 || stat.Kills > 0 ||
                                stat.SummonKills > 0 || stat.StatDamage > 0 || stat.NegativeLevels > 0 ||
                                totalCC > 0 || stat.DamageTaken > 0;
            return didSomething ? "F" : "FFF-";
        }

		private void AnalyzeCombatPerformance()
        {
            try
            {
                // SÉCURITÉ INSTA-KILL : On ramasse les cadavres tués silencieusement par l'API
                foreach (var kvp in doomedTargets)
                {
                    var targetId = kvp.Key;
                    if (!deadUnitsThisCombat.Contains(targetId))
                    {
                        var unit = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == targetId);
                        if (unit != null && unit.Descriptor.State.IsDead) // Il est bien mort !
                        {
                            deadUnitsThisCombat.Add(targetId);
                            var execStats = GetOrAddStats(kvp.Value, out _);
                            if (execStats != null)
                            {
                                execStats.InstaKills++;
                                execStats.Kills++;
                                execStats.WeightedKills += 1f * GetTargetMultiplier(unit);
                            }
                        }
                    }
                }

                float highestScore = 0;
                UnitCombatStats absoluteMVP = null;

                // 1. PHASE DE RECONNAISSANCE GLOBALE
                float totalTeamDamage = 0f;
                float totalTeamHealing = 0f;
                int totalTeamCC = 0;

                foreach (var stat in combatStats.Values.Where(s => s.IsAlly && !s.IsReanimated))
                {
                    totalTeamDamage += stat.TotalDamage + stat.SummonDamage;
                    totalTeamHealing += stat.HealingDone + stat.VampiricHealing;
                    totalTeamCC += stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Nauseated + stat.CC_Confused + stat.CC_Blinded + stat.CC_Prone + stat.CC_Entangled + stat.CC_Exhausted + stat.CC_Fatigued + stat.CC_Shaken + stat.CC_Sickened + stat.CC_Asleep + stat.CC_Petrified + stat.CC_Slowed + stat.CC_Staggered + stat.CC_Dazed + stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;
                }

                if (totalTeamDamage <= 0) totalTeamDamage = 1f;
                if (totalTeamHealing <= 0) totalTeamHealing = 1f;
                if (totalTeamCC <= 0) totalTeamCC = 1;

                int activeAllies = combatStats.Values.Count(s => s.IsAlly && !s.IsReanimated);
                float divider = Math.Max(6f, activeAllies); 
                float baselineEffort = Math.Max(15f, totalTeamDamage / divider);

                // 2. PHASE D'ÉVALUATION INDIVIDUELLE
                foreach (var stat in combatStats.Values)
                {
                    float myTotalDmg = stat.TotalDamage + stat.SummonDamage;
                    float myTotalHeal = stat.HealingDone + stat.VampiricHealing;
                    int myTotalCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Nauseated + stat.CC_Confused + stat.CC_Blinded + stat.CC_Prone + stat.CC_Entangled + stat.CC_Exhausted + stat.CC_Fatigued + stat.CC_Shaken + stat.CC_Sickened + stat.CC_Asleep + stat.CC_Petrified + stat.CC_Slowed + stat.CC_Staggered + stat.CC_Dazed + stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;

                    if (stat.IsAlly)
                    {
                        float damageRatioScore = (myTotalDmg / totalTeamDamage) * 120f; 
                        // Le soin est 50% plus difficile à générer que les dégâts bruts. 
                        float healingRatioScore = (myTotalHeal / totalTeamHealing) * 150f;
                        float ccRatioScore = ((float)myTotalCC / totalTeamCC) * 80f;

                        // Ajout des buffs de soutien (poids équilibré à 4 points par buff ciblé)
                        float supportContribution = stat.WeightedSupportBuffs * 0.5f;
                        if (supportContribution > 40f) supportContribution = 40f;

                        // Les critiques sont plafonnés pour éviter qu'un seul stat gonfle artificiellement la note.
                        float critContribution = Math.Min(stat.Crits * 5f, 50f);
                        float staticScore = (stat.WeightedKills * 18f) +
                                            (stat.StatDamage * 8f) +
                                            (stat.NegativeLevels * 15f) +
                                            critContribution +
                                            (stat.WeightedCCs * 4f) +
                                            supportContribution;

                        // Plafond de la portion "stats brutes" : atteindre le sommet exige une domination
                        // ÉQUILIBRÉE (part de contribution ratio ET faits d'armes), pas un seul stat exploité.
                        staticScore = Math.Min(staticScore, 210f);

                        stat.TotalScore = damageRatioScore + healingRatioScore + ccRatioScore + staticScore;
                    }
                    else
                    {
                        // ============================================================================
                        // NOTATION DE MENACE ENNEMIE (indépendante des critères alliés)
                        // ----------------------------------------------------------------------------
                        // Un ennemi n'est pas jugé sur sa "performance de héros" mais sur la menace
                        // RÉELLEMENT infligée à l'équipe, normalisée par la robustesse du groupe (PV et
                        // effectif). Un SSS+ ennemi ne s'obtient qu'en dominant réellement la rencontre :
                        // dévaster le réservoir de PV du groupe et/ou mettre à terre des personnages.
                        // ============================================================================
                        float enemyCR = Math.Max(1f, stat.CR);
                        float partyLv = Math.Max(1f, currentPartyLevel);
                        float partyHP = Math.Max(1f, totalPartyHPAtStart);
                        float partySize = Math.Max(1f, partySizeAtStart);

                        // 1. AGRESSION : proportion du réservoir de PV total de l'équipe entamée par cet
                        //    ennemi (100 = a infligé l'équivalent des PV de toute l'équipe). Plafonnée.
                        float dmgToParty = myTotalDmg; // TotalDamage + SummonDamage (verrou de faction = dégâts au groupe)
                        float aggression = Mathf.Clamp((dmgToParty / partyHP) * 100f, 0f, 200f);

                        // 2. LÉTALITÉ : mises à mort de membres de l'équipe. Signal le plus fort, pondéré
                        //    par l'effectif (un mort dans un petit groupe est plus décisif) + bonus plat.
                        int enemyKills = stat.Kills + stat.SummonKills;
                        float killRatio = Mathf.Clamp((float)enemyKills / partySize, 0f, 1f);
                        float lethality = (killRatio * 220f) + (enemyKills * 35f);

                        // 3. CONTRÔLE & ATTRITION subis par l'équipe (entraves, drain de carac, niveaux négatifs).
                        float control = (myTotalCC * 7f) + (stat.StatDamage * 4f) + (stat.NegativeLevels * 12f);

                        // 4. MENACE INTRINSÈQUE : un adversaire de CR supérieur au niveau du groupe compte
                        //    davantage ; un sous-fifre surclassé est plafonné vers le bas.
                        float crRatio = Mathf.Clamp(enemyCR / partyLv, 0.4f, 2.5f);

                        stat.TotalScore = (aggression + lethality + control) * crRatio;
                    }

                    if (stat.IsAlly && !stat.IsReanimated && stat.TotalScore > highestScore)
                    {
                        highestScore = stat.TotalScore;
                        absoluteMVP = stat;
                    }

                    // 3. ATTRIBUTION DES PALIERS
                    bool isMinorSkirmish = totalEnemyHPAtStart < (currentPartyLevel * 40);

                    if (stat.IsAlly)
                    {
                        // UTILISATION DE LA PROPRIÉTÉ GLOBALE HasRealContribution POUR UNE VÉRIFICATION ÉTANCHE
                        if (isMinorSkirmish && !stat.HasRealContribution)
                        {
                            stat.Grade = "R"; // Réserve Tactique (Aucune pénalité)
                        }
                        else
                        {
                            float tierScale = 1f + (currentPartyLevel * 0.02f);
                            if (stat.TotalScore >= 380f * tierScale) stat.Grade = "SSS+";
                            else if (stat.TotalScore >= 320f * tierScale) stat.Grade = "SSS";
                            else if (stat.TotalScore >= 270f * tierScale) stat.Grade = "SSS-";
                            else if (stat.TotalScore >= 220f * tierScale) stat.Grade = "SS+";
                            else if (stat.TotalScore >= 190f * tierScale) stat.Grade = "SS";
                            else if (stat.TotalScore >= 160f * tierScale) stat.Grade = "SS-";
                            else if (stat.TotalScore >= 130f * tierScale) stat.Grade = "S+";
                            else if (stat.TotalScore >= 110f * tierScale) stat.Grade = "S";
                            else if (stat.TotalScore >= 90f * tierScale)  stat.Grade = "S-";
                            else if (stat.TotalScore >= 75f * tierScale)  stat.Grade = "A+";
                            else if (stat.TotalScore >= 60f * tierScale)  stat.Grade = "A";
                            else if (stat.TotalScore >= 50f * tierScale)  stat.Grade = "A-";
                            else if (stat.TotalScore >= 40f * tierScale)  stat.Grade = "B+";
                            else if (stat.TotalScore >= 32f * tierScale)  stat.Grade = "B";
                            else if (stat.TotalScore >= 25f * tierScale)  stat.Grade = "B-";
                            else if (stat.TotalScore >= 18f * tierScale)  stat.Grade = "C+";
                            else if (stat.TotalScore >= 12f * tierScale)  stat.Grade = "C";
                            else if (stat.TotalScore >= 8f * tierScale)   stat.Grade = "C-";
                            else if (stat.TotalScore >= 5f * tierScale)   stat.Grade = "D+";
                            else if (stat.TotalScore >= 3f)   stat.Grade = "D";
                            else if (stat.TotalScore >= 1f)   stat.Grade = "D-";
                            else if (stat.TotalScore > 0 || stat.TotalDamage > 0 || stat.HealingDone > 0 || stat.DamageTaken > 0) stat.Grade = "F";
                            else stat.Grade = "FFF-";
                        }
                    }
                    else
                    {
                        stat.Grade = GradeEnemyThreat(stat);
                    }
                }

                // CALCUL DE LA NOTE GLOBALE DE L'ÉQUIPE (AVEC PONDÉRATION DES PETS ET ÉCHELLE SSS+)
                var gradedAllies = combatStats.Values.Where(s => s.IsAlly && !s.IsReanimated && s.Grade != "R").ToList();
                float teamAverageScore = 0f;
                if (gradedAllies.Count > 0)
                {
                    float sumScore = 0f;
                    float totalWeight = 0f;
                    foreach (var ally in gradedAllies)
                    {
                        // Les familiers (pets) comptent pour un poids de 0.25 pour ne pas pénaliser la note globale
                        float weight = (ally.UnitData != null && ally.UnitData.IsPet) ? 0.25f : 1.0f;
                        sumScore += ally.TotalScore * weight;
                        totalWeight += weight;
                    }
                    teamAverageScore = totalWeight > 0 ? (sumScore / totalWeight) : 0f;
                }
                if (teamAverageScore >= 220f) TeamGlobalGrade = "SSS+";
                else if (teamAverageScore >= 185f) TeamGlobalGrade = "SSS";
                else if (teamAverageScore >= 150f) TeamGlobalGrade = "SS";
                else if (teamAverageScore >= 110f) TeamGlobalGrade = "S";
                else if (teamAverageScore >= 80f) TeamGlobalGrade = "A";
                else if (teamAverageScore >= 50f) TeamGlobalGrade = "B";
                else if (teamAverageScore >= 25f) TeamGlobalGrade = "C";
                else if (teamAverageScore >= 10f) TeamGlobalGrade = "D";
                else TeamGlobalGrade = "F";

                if (totalEnemyHPAtStart < (currentPartyLevel * 40)) TeamGlobalGrade += " (Escarmouche)";

                foreach (var stat in combatStats.Values)
                {
                    bool isTheMVP = (stat == absoluteMVP); 
                    stat.Achievements.Clear();

                    
                    string category = "Ally";
                    if (stat.UnitData != null)
                    {
                        if (stat.UnitData.IsPet) category = "Pet";
                        else if (!stat.IsAlly) category = "Enemy";
                    }
                    else if (!stat.IsAlly)
                    {
                        category = "Enemy";
                    }
                    AchievementDatabase.GrantAchievements(stat, baselineEffort, isTheMVP, category);
                    
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans AnalyzeCombatPerformance : " + ex);
            }
        }

#if DEBUG
        private void ExportCombatLogToUMM()
        {
            if (combatStats.Count == 0) return;
            Main.Logger.Log("=========================================================");
            Main.Logger.Log("[RAPPORT SECRET] - V11 (Détails Complets & Lore-Friendly)");
            Main.Logger.Log("=========================================================");
            foreach (var stat in combatStats.Values.OrderByDescending(s => s.TotalScore))
            {
                if (!stat.IsAlly && stat.TotalScore <= 0 && stat.Grade == "FFF-") continue;
                string faction = stat.IsAlly ? "[ALLIÉ]" : $"[ENNEMI (CR {stat.CR})]";
                Main.Logger.Log($"{faction} {stat.Name} | RANG: {stat.Grade} | SCORE EXACT: {stat.TotalScore:F2}");
                Main.Logger.Log($" -> Dégâts Totaux: {stat.TotalDamage} (Dégâts Pondérés pour le Score: {stat.WeightedDamageDone:F1})");
                string physLog = "";
                if (stat.SlashingDmg > 0) physLog += $"{stat.SlashingDmg} Tranchant | ";
                if (stat.PiercingDmg > 0) physLog += $"{stat.PiercingDmg} Perçant | ";
                if (stat.BludgeoningDmg > 0) physLog += $"{stat.BludgeoningDmg} Contondant | ";
                if (stat.SlashPierceDmg > 0) physLog += $"{stat.SlashPierceDmg} Tran/Perç | ";
                if (stat.SlashBludgeonDmg > 0) physLog += $"{stat.SlashBludgeonDmg} Tran/Cont | ";
                if (stat.PierceBludgeonDmg > 0) physLog += $"{stat.PierceBludgeonDmg} Perç/Cont | ";
                if (stat.AllPhysDmg > 0) physLog += $"{stat.AllPhysDmg} Tran/Perç/Cont | ";
                if (stat.SneakAttackDmg > 0) physLog += $"(Inclus: {stat.SneakAttackDmg} Sournoises) | ";
                if (physLog == "") physLog = "0"; else physLog = physLog.TrimEnd(' ', '|');
                Main.Logger.Log($" -> Phys: {physLog}");
                Main.Logger.Log($" -> Élem/Magie: {stat.FireDmg} Feu / {stat.ColdDmg} Froid / {stat.AcidDmg} Acide / {stat.ElectricDmg} Foudre / {stat.SonicDmg} Son | Sacré/Impie/Nég: {stat.HolyDmg}/{stat.UnholyDmg}/{stat.NegativeDmg}");
                Main.Logger.Log($" -> Soins: {stat.HealingDone + stat.VampiricHealing} | Dégâts Subis: {stat.DamageTaken} | Esquives: {stat.AttacksDodged}");
                Main.Logger.Log($" -> Kills: {stat.Kills + stat.SummonKills} (Kills Pondérés: {stat.WeightedKills:F1})");
                Main.Logger.Log($" -> CC Pondéré: {stat.WeightedCCs:F1} | Soutien Pondéré: {stat.WeightedSupportBuffs:F1} | Tir Allié: {stat.FriendlyFireDmg} | Overkill: {stat.OverkillDmg}");
                if (stat.Achievements.Count > 0)
                {
                    Main.Logger.Log(" -> SUCCÈS DÉBLOQUÉS :");
                    foreach (var ach in stat.Achievements.OrderByDescending(a => a.Weight))
                    {
                        Main.Logger.Log($" * [{ach.Tier}] {ach.Title}");
                    }
                }
                Main.Logger.Log("---------------------------------------------------------");
            }
        }
#endif

        public void OnEventAboutToTrigger(RuleDealDamage evt) { }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            try
            {
                // AJOUT CRITIQUE : Ignorer les événements "Fake" (prédictions des infobulles du jeu)
                if (!isCombatActive || evt.IsFake || evt.Initiator == null || evt.Target == null) return;
                
                int damage = evt.Result; 
                if (damage <= 0) return;

                // 1. TRACKING DU DÉFENSEUR (Tanking & Tir Allié)
                bool targetIsSummon = IsSummonUnit(evt.Target);
                var targetStats = GetOrAddStats(evt.Target, out _);
                if (targetStats != null)
                {
                    if (targetIsSummon) targetStats.Summons.DamageTaken += damage;
                    else targetStats.DamageTaken += damage;

                    // Capture ciblée des dégâts physiques reçus (Pour tout le monde : Alliés et Ennemis) !
                    if (evt.DamageBundle != null)
                    {
                        foreach (var dv in evt.ResultList ?? new List<Kingmaker.RuleSystem.Rules.Damage.DamageValue>())
                        {
                            int finalForChunk = dv.FinalValue;
                            if (finalForChunk <= 0) continue;
                            if (dv.Source is Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage)
                            {
                                if (targetIsSummon) targetStats.Summons.PhysicalDmgTaken += finalForChunk;
                                else targetStats.PhysicalDmgTaken += finalForChunk;
                            }
                        }
                    }

                    if (targetStats.IsAlly)
                    {
                        if (evt.Initiator.IsPlayerFaction && evt.Initiator != evt.Target)
                        {
                            var initStats = GetOrAddStats(evt.Initiator, out bool ffIsSummon);
                            if (initStats != null)
                            {
                                if (ffIsSummon) initStats.Summons.FriendlyFireDmg += damage;
                                else initStats.FriendlyFireDmg += damage;
                            }
                        }
                    }
                }

                // 2. TRACKING DE L'ATTAQUANT
                var stats = GetOrAddStats(evt.Initiator, out bool isSummon);
                if (stats == null || targetStats == null) return;

                // Verrou Factions : Monstres vs Monstres ignorés
                if (!stats.IsAlly && !targetStats.IsAlly) return;

                float multiplier = GetTargetMultiplier(evt.Target);
                if (!targetStats.IsAlly && evt.Target.HPLeft < 0)
                {
                    int hpBeforeHit = evt.Target.HPLeft + damage;
                    if (hpBeforeHit > 0) stats.OverkillDmg += (damage - hpBeforeHit);
                    else stats.OverkillDmg += damage; 
                }

                // --- DÉBUT DE LA DÉCONSTRUCTION FORENSIQUE ET RÉCONCILIATION MATHÉMATIQUE ---
                int accumulatedThisTrigger = 0;
                string lastProcessedKey = null;
                int sumAddedThisTrigger = 0;

                foreach (var dv in evt.ResultList ?? new List<Kingmaker.RuleSystem.Rules.Damage.DamageValue>())
                {
                    int finalForChunk = dv.FinalValue;
                    if (finalForChunk <= 0) continue;
                    accumulatedThisTrigger += finalForChunk;

                    string damageTypeLoc = GetDamageTypeLocalizedName(dv.Source);
                    string sourceName = GetDamageSourceName(evt);

                    if (isSummon)
                    {
                        string initiatorName = evt.Initiator.CharacterName ?? "Inconnu";
                        sourceName = $"{initiatorName} - {sourceName}";
                    }

                    string key = $"{sourceName} ({damageTypeLoc})";
                    lastProcessedKey = key;

                    if (stats.DamageBySource.ContainsKey(key))
                        stats.DamageBySource[key] += finalForChunk;
                    else
                        stats.DamageBySource[key] = finalForChunk;

                    if (!stats.DamageModifiersAudit.ContainsKey(key))
                    {
                        stats.DamageModifiersAudit[key] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    }
                    var auditDict = stats.DamageModifiersAudit[key];

                    // Extraction des variables de base de la source de dégâts
                    var sourceDamage = dv.Source;
                    int num5 = dv.RollResult; // Jet de dés brut issu de dv
                    int num = sourceDamage.CriticalModifier ?? 1;
                    
                    var difficultySettings = Kingmaker.Settings.SettingsRoot.Difficulty;
                    var enemyCriticalHits = difficultySettings != null 
                        ? difficultySettings.EnemyCriticalHits.GetValue() 
                        : Kingmaker.Settings.CriticalHitPower.Normal;
                    
                    int num2 = (!evt.Target.IsPlayerFaction || enemyCriticalHits != Kingmaker.Settings.CriticalHitPower.Off) ? num : 1;
                    int num3 = (!evt.Target.IsPlayerFaction || enemyCriticalHits == Kingmaker.Settings.CriticalHitPower.Normal || sourceDamage.Bonus + sourceDamage.BonusTargetRelated < 0) ? num : 1;
                    
                    bool critToDice = num2 > 1;
                    bool critToBonus = num3 > 1;
                    int critMult = num;

                    float num4 = sourceDamage.EmpowerBonus.Value; // Multiplicateur d'Extension d'effet (ex: 1.5f ou 1f)

                    // 1. Détermination du dé de base vs dé de coup critique
                    int baseDice = num5;
                    if (critToDice && critMult > 1)
                    {
                        baseDice = (int)Math.Round((double)num5 / critMult);
                    }
                    int critDiceContrib = num5 - baseDice;

                    // 2. Détermination du bonus d'effet de base (hors modificateurs de build)
                    int sumModifiers = 0;
                    if (sourceDamage.Modifiers != null)
                    {
                        foreach (var mod in sourceDamage.Modifiers) sumModifiers += mod.Value;
                    }
                    int baseEffectBonus = sourceDamage.Bonus - sumModifiers;

                    int unitsCount = evt.Calculate != null ? evt.Calculate.UnitsCount : 1;
                    float tacticalCritMod = sourceDamage.TacticalCriticalModifier;

                    // Calcul de la cascade d'atténuations et de divisions de dégâts (Halves & Declines)
                    float H = 1f;
                    if (sourceDamage.Half.Value && !sourceDamage.AlreadyHalved) H /= 2f;
                    if (evt.Half && !evt.AlreadyHalved) H /= 2f;
                    if (evt.HalfBecauseSavingThrow)
                    {
                        float num7 = evt.Initiator.State.Features.AzataFavorableMagic ? 0.75f : 0.5f;
                        H *= num7;
                    }
                    
                    var declineType = sourceDamage.Decline.Value;
                    if (declineType == DamageDeclineType.ByHalf) H *= 0.5f;
                    else if (declineType == DamageDeclineType.ByQuarter) H *= 0.75f;

                    // 3. Calcul des composants de base non-amplifiés par les pourcentages/vulnérabilités
                    float unscaledBaseDiceVal = (float)baseDice * tacticalCritMod * H;
                    float unscaledCritDiceVal = (float)critDiceContrib * tacticalCritMod * H;
                    
                    float unscaledBaseEffectVal = (float)baseEffectBonus * unitsCount * (critToBonus ? critMult : 1) * tacticalCritMod * H;
                    
                    // CORRECTION ABSOLUE : TargetRelatedBonus n'est PAS affecté par le TacticalCriticalModifier chez Owlcat
                    float unscaledTargetRelatedVal = (float)sourceDamage.BonusTargetRelated * unitsCount * (critToBonus ? critMult : 1) * H;
                    
                    // CORRECTION ABSOLUE : PostCritIncrements n'est affecté NI par le critique, NI par le TacticalCriticalModifier
                    float unscaledPostCritBonusVal = (float)sourceDamage.PostCritIncrements.OverallBonus * unitsCount * H;

                    // Apport brut du coup critique sur les bonus plats
                    float critBonusContrib = 0f;
                    if (critToBonus && critMult > 1)
                    {
                        // Séparation stricte car baseEffect prend le multiplicateur tactique, mais pas TargetRelated
                        float critOnBaseEffect = (float)baseEffectBonus * unitsCount * (critMult - 1) * tacticalCritMod * H;
                        float critOnTargetRelated = (float)sourceDamage.BonusTargetRelated * unitsCount * (critMult - 1) * H;
                        critBonusContrib = critOnBaseEffect + critOnTargetRelated;
                    }
                    float critUnempowered = unscaledCritDiceVal + critBonusContrib;

                    // Somme des modificateurs de build (qui font partie du "Bonus" standard, donc affectés par TacticalCritMod)
                    float modifiersSumUnempowered = 0f;
                    if (sourceDamage.Modifiers != null)
                    {
                        foreach (var mod in sourceDamage.Modifiers)
                        {
                            modifiersSumUnempowered += (float)mod.Value * unitsCount * (critToBonus ? critMult : 1) * tacticalCritMod * H;
                        }
                    }

                    // Somme totale pour le calcul de l'Empower (Extension d'effet)
                    float unempoweredSum = unscaledBaseDiceVal + unscaledBaseEffectVal + unscaledTargetRelatedVal + unscaledPostCritBonusVal + critUnempowered + modifiersSumUnempowered;

                    // 4. Isolation des apports des multiplicateurs finaux
                    float empowerContribution = 0f;
                    if (num4 > 1f)
                    {
                        empowerContribution = unempoweredSum * (num4 - 1f);
                    }

                    float percentContribution = 0f;
                    if (sourceDamage.BonusPercent != 0)
                    {
                        percentContribution = (unempoweredSum * num4) * (sourceDamage.BonusPercent / 100f);
                    }

                    float vulnerabilityContribution = 0f;
                    if (sourceDamage.Vulnerability != 1f)
                    {
                        vulnerabilityContribution = (unempoweredSum * num4 * (1f + (float)sourceDamage.BonusPercent / 100f)) * (sourceDamage.Vulnerability - 1f);
                    }

                    float num8 = unempoweredSum * num4 + percentContribution + vulnerabilityContribution;

                    // --- CORRECTION : Capture du multiplicateur global de l'événement (ModifierBonus) ---
                    float globalModifiersSum = 0f;
                    float evtModifierBonus = evt.ModifierBonus ?? 0f;
                    if (evtModifierBonus != 0f)
                    {
                        globalModifiersSum = num8 * evtModifierBonus;
                    }

                    // Somme théorique totale générée pour ce fragment de dégât
                    float sumFloat = num8 + globalModifiersSum;

                    // Résolution du ratio de contribution par rapport à ValueWithoutReduction d'Owlcat
                    float finalRatio = sumFloat > 0f ? (float)dv.ValueWithoutReduction / sumFloat : 0f;

                    // Application du coefficient de difficulté (uniquement pour les ennemis ciblant l'équipe et hors dégâts directs)
                    float diffMod = (evt.Initiator.IsPlayerFaction || sourceDamage.Type == DamageType.Direct) ? 1f : evt.DifficultyModifier;

                    int sumOfAuditedComponentsForChunk = 0;

                    // Délégué d'écriture et de traçage de contribution arrondie
                    Action<string, float> addValueToAudit = (labelKey, rawValue) =>
                    {
                        float scaledVal = rawValue * finalRatio * diffMod;
                        int roundedVal = (int)Math.Round(scaledVal);
                        if (roundedVal != 0)
                        {
                            string label = Localization.GetStringById(labelKey) ?? labelKey;
                            if (auditDict.ContainsKey(label)) auditDict[label] += roundedVal;
                            else auditDict[label] = roundedVal;
                            
                            sumAddedThisTrigger += roundedVal;
                            sumOfAuditedComponentsForChunk += roundedVal;
                        }
                    };

                    // Attribution de chaque ligne d'audit positive
                    addValueToAudit("ui.dmg.audit_base_roll", unscaledBaseDiceVal);
                    addValueToAudit("ui.dmg.audit_constructor_bonus", unscaledBaseEffectVal);
                    addValueToAudit("ui.dmg.audit_target_bonus", unscaledTargetRelatedVal);
                    addValueToAudit("ui.dmg.audit_post_crit_bonus", unscaledPostCritBonusVal);
                    addValueToAudit("ui.dmg.audit_critical", critUnempowered);
                    addValueToAudit("ui.dmg.audit_empower", empowerContribution);
                    addValueToAudit("ui.dmg.audit_percent_bonus", percentContribution);
                    addValueToAudit("ui.dmg.audit_vulnerability", vulnerabilityContribution);

                    // Attribution des modificateurs de build (Force, Don, Châtiment, etc.)
                    if (sourceDamage.Modifiers != null)
                    {
                        foreach (var mod in sourceDamage.Modifiers)
                        {
                            float modRawVal = (float)mod.Value * unitsCount * (critToBonus ? critMult : 1) * tacticalCritMod * H;
                            float scaledModVal = modRawVal * finalRatio * diffMod;
                            int roundedModVal = (int)Math.Round(scaledModVal);
                            if (roundedModVal != 0)
                            {
                                string modName = GetModifierName(mod);
                                if (auditDict.ContainsKey(modName)) auditDict[modName] += roundedModVal;
                                else auditDict[modName] = roundedModVal;
                                
                                sumAddedThisTrigger += roundedModVal;
                                sumOfAuditedComponentsForChunk += roundedModVal;
                            }
                        }
                    }

                    // --- CORRECTION : Attribution du multiplicateur global ---
                    if (evtModifierBonus != 0f)
                    {
                        float globalRawVal = num8 * evtModifierBonus;
                        float scaledGlobalVal = globalRawVal * finalRatio * diffMod;
                        int roundedGlobalVal = (int)Math.Round(scaledGlobalVal);
                        if (roundedGlobalVal != 0)
                        {
                            string modName = Localization.GetStringById("ui.dmg.audit_global_mod") ?? "Multiplicateur global d'événement";
                            
                            if (auditDict.ContainsKey(modName)) auditDict[modName] += roundedGlobalVal;
                            else auditDict[modName] = roundedGlobalVal;
                            
                            sumAddedThisTrigger += roundedGlobalVal;
                            sumOfAuditedComponentsForChunk += roundedGlobalVal;
                        }
                    }

                    // 5. Réconciliation locale du fragment (DR, Dureté, et arrondis internes)
                    int finalScaledChunkValue = (sourceDamage.Type == DamageType.Direct || evt.Initiator.IsPlayerFaction)
                        ? finalForChunk
                        : Math.Max(1, (int)(finalForChunk * evt.DifficultyModifier));

                    // --- NOUVEAU : Extraction absolue de la Réduction de Dégâts (DR / Résistance / Dureté) ---
                    int reductionValue = dv.Reduction;
                    if (reductionValue > 0)
                    {
                        int scaledReduction = (int)Math.Round((float)reductionValue * diffMod);
                        if (scaledReduction > 0)
                        {
                            string drLabel = Localization.GetStringById("ui.dmg.audit_dr") ?? "Absorption (Résistance / Dureté)";
                            if (auditDict.ContainsKey(drLabel)) auditDict[drLabel] -= scaledReduction;
                            else auditDict[drLabel] = -scaledReduction;
                            
                            sumAddedThisTrigger -= scaledReduction;
                            sumOfAuditedComponentsForChunk -= scaledReduction;
                        }
                    }

                    // Arrondis internes et ajustements structurels restants
                    int reconciliationDiff = finalScaledChunkValue - sumOfAuditedComponentsForChunk;
                    if (reconciliationDiff != 0)
                    {
                        string roundingLabel = Localization.GetStringById("ui.dmg.audit_rounding") ?? "Ajustements & Arrondis de calcul";
                        if (auditDict.ContainsKey(roundingLabel)) auditDict[roundingLabel] += reconciliationDiff;
                        else auditDict[roundingLabel] = reconciliationDiff;
                        
                        sumAddedThisTrigger += reconciliationDiff;
                    }
                }

                // 6. RÉCONCILIATION GLOBALE FINALE DE L'ÉVÉNEMENT (Capping & Redirections)
                int actualTriggerDamage = damage; // evt.Result
                int globalDiff = actualTriggerDamage - sumAddedThisTrigger;
                if (globalDiff != 0 && lastProcessedKey != null && stats.DamageModifiersAudit.ContainsKey(lastProcessedKey))
                {
                    var lastAuditDict = stats.DamageModifiersAudit[lastProcessedKey];
                    string cappingLabel = Localization.GetStringById("ui.dmg.audit_capping") ?? "Plafonnement des PV / Redirection";
                    if (lastAuditDict.ContainsKey(cappingLabel)) lastAuditDict[cappingLabel] += globalDiff;
                    else lastAuditDict[cappingLabel] = globalDiff;
                }

                

                if (isSummon)
                {
                    stats.SummonDamage += damage;
                    stats.Summons.Damage += damage;
                    stats.WeightedDamageDone += (damage * multiplier) * 0.7f;
                }
                else
                {
                    stats.TotalDamage += damage;
                    stats.WeightedDamageDone += damage * multiplier;
                    if (damage > stats.MaxSingleHit) stats.MaxSingleHit = damage;

                    // Détection d'une charge via le signal officiel d'Owlcat
                    // (RuleAttackRoll.RuleAttackWithWeapon.IsCharge), identique à celui utilisé
                    // par leurs propres hauts faits (AchievementLogicMassiveDamageMountedChargeKill).
                    if (evt.AttackRoll?.RuleAttackWithWeapon != null && evt.AttackRoll.RuleAttackWithWeapon.IsCharge)
                    {
                        stats.ChargeDamage += damage;
                        if (damage > stats.MaxMountedChargeHit) stats.MaxMountedChargeHit = damage;
                    }

                    // Suivi du plus gros pic de dégâts sur une fenêtre glissante de 6 s (~1 round).
                    float nowB = UnityEngine.Time.realtimeSinceStartup;
                    stats.RecentDamageWindow.Add(new KeyValuePair<float, int>(nowB, damage));
                    stats.RecentDamageWindow.RemoveAll(e => nowB - e.Key > 6f);
                    int windowSum = 0;
                    for (int wi = 0; wi < stats.RecentDamageWindow.Count; wi++) windowSum += stats.RecentDamageWindow[wi].Value;
                    if (windowSum > stats.MaxBurstDamage) stats.MaxBurstDamage = windowSum;

                    if (evt.DamageBundle != null && evt.Result > 0)
                    {
                        foreach (var dv in evt.ResultList ?? new List<Kingmaker.RuleSystem.Rules.Damage.DamageValue>())
                        {
                            int finalForChunk = dv.FinalValue;
                            if (finalForChunk <= 0) continue;
                            var source = dv.Source;

                            // Tracking de l'Attaque Sournoise & Dégâts de Précision
                            if (source != null && (source.Sneak || source.Precision))
                            {
                                stats.SneakAttackDmg += finalForChunk;
                            }

                            // Physiques
                            if (source is Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage physical)
                            {
                                bool s = physical.Form.HasFlag(Kingmaker.Enums.Damage.PhysicalDamageForm.Slashing);
                                bool p = physical.Form.HasFlag(Kingmaker.Enums.Damage.PhysicalDamageForm.Piercing);
                                bool b = physical.Form.HasFlag(Kingmaker.Enums.Damage.PhysicalDamageForm.Bludgeoning);

                                if (s && p && b) stats.AllPhysDmg += finalForChunk;
                                else if (s && p) stats.SlashPierceDmg += finalForChunk;
                                else if (s && b) stats.SlashBludgeonDmg += finalForChunk;
                                else if (p && b) stats.PierceBludgeonDmg += finalForChunk;
                                else if (s) stats.SlashingDmg += finalForChunk;
                                else if (p) stats.PiercingDmg += finalForChunk;
                                else if (b) stats.BludgeoningDmg += finalForChunk;
                            }
                            // Energies
                            else if (source is Kingmaker.RuleSystem.Rules.Damage.EnergyDamage energy)
                            {
                                switch (energy.EnergyType)
                                {
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Fire: stats.FireDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Cold: stats.ColdDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Acid: stats.AcidDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Electricity: stats.ElectricDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Sonic: stats.SonicDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.NegativeEnergy: stats.NegativeDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Holy: case Kingmaker.Enums.Damage.DamageEnergyType.Divine: stats.HolyDmg += finalForChunk; break;
                                    case Kingmaker.Enums.Damage.DamageEnergyType.Unholy: stats.UnholyDmg += finalForChunk; break;
                                }
                            }
                        }
                    }
                }

                // --- DÉTECTION DU MOMENT EXAC DE LA CHUTE DE L'ALLIÉ ---
                if (targetStats != null && targetStats.IsAlly && evt.Target.HPLeft <= 0 && (evt.Target.HPLeft + evt.Result) > 0)
                {
                    if (targetIsSummon) targetStats.Summons.TimesDowned++;
                    else targetStats.TimesDowned++;
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleDealDamage) : " + ex);
            }
        }
		public void OnEventAboutToTrigger(RuleAttackRoll evt) 
        { 
            // Sécurité Mémoire & Morts Fantômes : Dès qu'une nouvelle attaque physique démarre, 
            // la fenêtre d'opportunité d'une "mort magique subite" précédente est révolue. On purge.
            if (doomedTargets.Count > 0)
            {
                doomedTargets.Clear();
            }
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            try
            {
                if (!isCombatActive || evt.IsFake) return; // Ignore les pré-calculs ou attaques fictives

                if (evt.Initiator != null)
                {
                    var stats = GetOrAddStats(evt.Initiator, out bool initIsSummon);
                    // La précision offensive des invocations n'est pas fusionnée dans la fiche du maître.
                    if (stats != null && !initIsSummon)
                    {
                        // Enregistrement de la précision offensive (Tentées vs Touchées)
                        stats.AttacksAttempted++;
                        if (evt.IsHit)
                        {
                            stats.AttacksLanded++;
                        }

                        if (evt.IsCriticalConfirmed)
                        {
                            stats.Crits++;
                            if (evt.Target != null && evt.Target.Blueprint != null && evt.Target.Blueprint.CR >= currentPartyLevel)
                            {
                                stats.HighDangerCrits++;
                            }
                        }
                        if (evt.RuleAttackWithWeapon != null && evt.RuleAttackWithWeapon.IsAttackOfOpportunity)
                        {
                            stats.AoOs++;
                        }
                    }
                }

                if (evt.Target != null)
                {
                    bool targetIsSummon = IsSummonUnit(evt.Target);
                    var targetStats = GetOrAddStats(evt.Target, out _);
                    if (targetStats != null)
                    {
                        if (evt.IsHit)
                        {
                            if (targetIsSummon) targetStats.Summons.HitsPhysicalTaken++;
                            else targetStats.HitsPhysicalTaken++;
                        }
                        else
                        {
                            if (targetIsSummon) targetStats.Summons.AttacksDodged++;
                            else targetStats.AttacksDodged++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleAttackRoll) : " + ex);
            }
        }

        public void OnEventAboutToTrigger(RuleDrainEnergy evt) { }

        public void OnEventDidTrigger(RuleDrainEnergy evt)
        {
            try
            {
                if (!isCombatActive || evt.Initiator == null || evt.Target == null || evt.TargetIsImmune) return;
                if (evt.Initiator == evt.Target || evt.Initiator.IsPlayerFaction == evt.Target.IsPlayerFaction) return; 

                int appliedLevels = evt.Result;
                if (appliedLevels > 0)
                {
                    var stats = GetOrAddStats(evt.Initiator, out bool isSummon);
                    if (stats != null)
                    {
                        if (isSummon) stats.Summons.NegativeLevels += appliedLevels;
                        else stats.NegativeLevels += appliedLevels;

                        var negLevelsPart = evt.Target.Get<UnitPartNegativeLevels>();
                        int previousLevels = 0;
                        if (negLevelsPart != null)
                        {
                            previousLevels = Math.Max(0, negLevelsPart.Count - appliedLevels);
                        }

                        int charLevel = evt.Target.Progression?.CharacterLevel ?? 1;
                        if (previousLevels < charLevel && (previousLevels + appliedLevels) >= charLevel)
                        {
                            if (!deadUnitsThisCombat.Contains(evt.Target.UniqueId))
                            {
                                deadUnitsThisCombat.Add(evt.Target.UniqueId);
                                float multiplier = GetTargetMultiplier(evt.Target);
                                if (isSummon)
                                {
                                    stats.SummonKills++;
                                    stats.Summons.Kills++;
                                    stats.Summons.InstaKills++;
                                }
                                else
                                {
                                    stats.Kills++;
                                    stats.InstaKills++;
                                    stats.EnergyDrainKills++;
                                }
                                stats.WeightedKills += 1.5f * multiplier;

                                if (evt.Target.Blueprint != null && evt.Target.Blueprint.CR >= currentPartyLevel)
                                {
                                    stats.HighDangerInstaKills++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleDrainEnergy) : " + ex);
            }
        }

        private void RecordBuffSafely(Buff buff, bool forceDuringCombatStart = false)
        {
            try
            {
                if (buff == null || buff.Context == null || buff.Blueprint == null) return;
                if (!isCombatActive && !forceDuringCombatStart) return;

                var target = buff.Owner?.Unit; 
                var caster = buff.Context.MaybeCaster;
                if (target == null || caster == null) return;

                if (!caster.IsPlayerFaction || !target.IsPlayerFaction) return;

                // Verification stricte via IsFromSpell (issu des sources decompilees)
                if (!buff.Blueprint.IsFromSpell && !buff.IsFromSpell) return;

                string bName = buff.Blueprint.name != null ? buff.Blueprint.name.ToLower() : "";
                if (bName.Contains("aura") || bName.Contains("stance") || bName.Contains("toggle") || 
                    bName.Contains("rage") || bName.Contains("passive") || bName.Contains("effect") || 
                    bName.Contains("item") || bName.Contains("feat") || bName.Contains("focus") ||
                    bName.Contains("song") || bName.Contains("sing") || bName.Contains("dirge") ||
                    bName.Contains("ring") || bName.Contains("cloak") || bName.Contains("boots") ||
                    bName.Contains("armor") || bName.Contains("amulet") || bName.Contains("bracers") ||
                    bName.Contains("belt") || bName.Contains("headband") || bName.Contains("helm") ||
                    bName.Contains("goggles") || bName.Contains("glasses"))
                {
                    return;
                }

                if (caster != target && !buff.Blueprint.Harmful)
                {
                    string supportKey = $"{target.UniqueId}_{bName}";
                    var statsAlly = GetOrAddStats(caster, out bool supportCasterIsSummon);
                    if (supportCasterIsSummon) return;

                    if (statsAlly != null && !statsAlly.uniqueInflictedCCs.Contains(supportKey))
                    {
                        statsAlly.uniqueInflictedCCs.Add(supportKey);
                        statsAlly.SupportBuffsCast++;
                        float mult = GetTargetMultiplier(target);
                        statsAlly.WeightedSupportBuffs += 0.1f * mult;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans RecordBuffSafely : " + ex.Message);
            }
        }

        public void HandleBuffDidAdded(Buff buff)
        {
            try
            {
                if (!isCombatActive || buff == null || buff.Context == null || buff.Blueprint == null) return;
                var target = buff.Owner?.Unit; 
                var caster = buff.Context.MaybeCaster;
                if (target == null) return;

                // --- CAPTURE DES DEBUFFS (EFFETS NÉFASTES) SUBIS PAR UN MEMBRE DU GROUPE ---
                if (target.IsPlayerFaction && buff.Blueprint.Harmful)
                {
                    bool debuffTargetIsSummon = IsSummonUnit(target);
                    var targetStats = GetOrAddStats(target, out _);
                    if (targetStats != null)
                    {
                        string debuffName = buff.Blueprint.Name;
                        if (!string.IsNullOrEmpty(debuffName))
                        {
                            if (debuffTargetIsSummon) targetStats.Summons.SufferedDebuffs.Add(debuffName);
                            else targetStats.SufferedDebuffs.Add(debuffName);
                        }
                    }
                }

                if (caster == null) return;

                // --- CAS 1 : SOUTIEN ALLIE ---
                if (caster.IsPlayerFaction && target.IsPlayerFaction)
                {
                    RecordBuffSafely(buff);
                    return;
                }

                // --- CAS 2 : OFFENSIF / CONTROLE ---
                if (caster.IsPlayerFaction == target.IsPlayerFaction || caster == target) return;
                string buffName = buff.Blueprint.name != null ? buff.Blueprint.name.ToLower() : "";

                Func<string, string[], bool> Has = (eng, fr) => {
                    if (!string.IsNullOrEmpty(eng) && buffName.Contains(eng)) return true;
                    if (fr != null)
                    {
                        foreach (var s in fr)
                            if (!string.IsNullOrEmpty(s) && buffName.Contains(s)) return true;
                    }
                    return false;
                };

                string ccKey = $"{target.UniqueId}_{buffName}";
                var stats = GetOrAddStats(caster, out bool casterIsSummon);
                if (stats == null) return;
                // Le contrôle de foule appliqué par une invocation n'est pas fusionné dans la fiche du maître.
                if (casterIsSummon) return;
                if (stats.uniqueInflictedCCs.Contains(ccKey)) return;

                var descriptor = buff.Blueprint.SpellDescriptor;
                bool isCC = false;

                // Verification compile-safe stricte des drapeaux de SpellDescriptor d'origine
                if (descriptor.HasAnyFlag(SpellDescriptor.Paralysis) || Has("paralyz", new string[] { "paralys", "paralyse", "paralysé", "paralysie" })) { stats.CC_Paralyzed++; isCC = true; }
                if (descriptor.HasAnyFlag(SpellDescriptor.Stun) || Has("stun", new string[] { "etourdi", "étourdi", "etourdis", "stun" })) { stats.CC_Stunned++; isCC = true; }
                if (descriptor.HasAnyFlag(SpellDescriptor.Fear) || Has("frighten", new string[] { "effray", "effrayé", "peur" })) { stats.CC_Frightened++; isCC = true; }
                if (Has("naus", new string[] { "naus", "nausée", "nausee", "nauseat" })) { stats.CC_Nauseated++; isCC = true; }
                if (Has("confus", new string[] { "confus", "hébété", "hebet" })) { stats.CC_Confused++; isCC = true; }
                if (descriptor.HasAnyFlag(SpellDescriptor.Blindness) || Has("blind", new string[] { "aveug", "aveugl" })) { stats.CC_Blinded++; isCC = true; }
                if (Has("prone", new string[] { "à terre", "a terre", "prone" })) { stats.CC_Prone++; isCC = true; }
                if (Has("entangle", new string[] { "entrav", "enchev", "enchevêtr" })) { stats.CC_Entangled++; isCC = true; }
                if (Has("exhaust", new string[] { "épuis", "epuis" })) { stats.CC_Exhausted++; isCC = true; }
                if (Has("fatigue", new string[] { "fatigu", "fatigue" })) { stats.CC_Fatigued++; isCC = true; }
                if (descriptor.HasAnyFlag(SpellDescriptor.Shaken) || Has("shaken", new string[] { "secou", "secoué", "secoue" })) { stats.CC_Shaken++; isCC = true; }
                if (Has("sicken", new string[] { "sick", "fiévreux", "fievreux", "sicken" })) { stats.CC_Sickened++; isCC = true; }
                if (descriptor.HasAnyFlag(SpellDescriptor.Sleep) || Has("sleep", new string[] { "slumber", "endorm", "sommeil", "dorm" })) { stats.CC_Asleep++; isCC = true; }
                if (Has("petrifi", new string[] { "pétrifi", "petrifi" })) { stats.CC_Petrified++; isCC = true; }
                if (Has("slow", new string[] { "ralent", "lenteur", "slow" })) { stats.CC_Slowed++; isCC = true; }
                if (Has("stagger", new string[] { "chancel", "stagger" })) { stats.CC_Staggered++; isCC = true; }
                if (descriptor.HasAnyFlag(SpellDescriptor.Daze) || Has("daze", new string[] { "hébété", "hebet", "daze" })) { stats.CC_Dazed++; isCC = true; }
                if (Has("dazzle", new string[] { "éblou", "eblou", "dazzle" })) { stats.CC_Dazzled++; isCC = true; }
                if (Has("helpless", new string[] { "sans défense", "sansdefense" })) { stats.CC_Helpless++; isCC = true; }
                if (Has("cowering", new string[] { "recroquevill", "cowering" })) { stats.CC_Cowering++; isCC = true; }
                if (buffName.Contains("deathsdoor") || buffName.Contains("death's door")) { stats.CC_DeathsDoor++; isCC = true; }

                if (isCC)
                {
                    stats.uniqueInflictedCCs.Add(ccKey); 
                    float multiplier = GetTargetMultiplier(target);
                    stats.WeightedCCs += 1f * multiplier; 

                    if (target.Blueprint != null && target.Blueprint.CR >= currentPartyLevel)
                    {
                        stats.HighDangerCCs++;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans HandleBuffDidAdded : " + ex);
            }
        }

        public void HandleBuffDidRemoved(Buff buff) { }
		
		public void OnEventAboutToTrigger(RuleCastSpell evt) { }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            try
            {
                // On inclut bien la sécurité evt.IsCutscene de l'étape précédente
                if (!isCombatActive || evt.IsCutscene || evt.Initiator == null || evt.Spell?.Blueprint == null) return;
                
                string spellName = evt.Spell.Blueprint.name;
                if (string.IsNullOrEmpty(spellName)) return;

                // ZÉRO ALLOCATION MÉMOIRE : Utilisation de IndexOf sans ToLower()
                if (spellName.IndexOf("weird", StringComparison.OrdinalIgnoreCase) >= 0 || 
                    spellName.IndexOf("phantasmalkiller", StringComparison.OrdinalIgnoreCase) >= 0 || 
                    spellName.IndexOf("absolutedeath", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    spellName.IndexOf("wailof", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null) stats.IconicSpellsCast++; // <-- COMPTABILISÉ UNIQUEMENT ICI (À L'INCANTATION)
                }

                // Enregistrement des parchemins consommés de niveau 7+ et résurrections actives
                var statsCast = GetOrAddStats(evt.Initiator, out _);
                if (statsCast != null)
                {
                    if (spellName.IndexOf("raisedead", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        spellName.IndexOf("resurrection", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        spellName.IndexOf("breathoflife", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        statsCast.ResurrectedCount++;
                    }

                    var usable = evt.Spell?.SourceItem as Kingmaker.Items.ItemEntityUsable;
                    if (usable != null && usable.Blueprint != null && usable.Blueprint.Type == Kingmaker.Blueprints.Items.Equipment.UsableItemType.Scroll)
                    {
                        if (usable.Blueprint.SpellLevel >= 7)
                        {
                            statsCast.ScrollsCastCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleCastSpell) : " + ex);
            }
        }

        public void OnEventAboutToTrigger(RuleHealDamage evt) { }

        public void OnEventDidTrigger(RuleHealDamage evt)
        {
            try
            {
                // AJOUT CRITIQUE : Ignorer les soins "Fake" (prédictions d'infobulles)
                if (isCombatActive && !evt.IsFake && evt.Initiator != null && evt.Value > 0)
                {
                    var stats = GetOrAddStats(evt.Initiator, out bool isSummon);
                    if (stats != null)
                    {
                        bool isVampiric = false;
                        if (evt.Reason != null && evt.Reason.Context != null && evt.Reason.Context.SourceAbility != null)
                        {
                            string abilityName = evt.Reason.Context.SourceAbility.name.ToLower();
                            if (abilityName.Contains("vampir") || abilityName.Contains("siphon"))
                            {
                                isVampiric = true;
                            }
                        }

                        if (isSummon)
                        {
                            if (isVampiric) stats.Summons.VampiricHealing += evt.Value;
                            else stats.Summons.HealingDone += evt.Value;
                        }
                        else
                        {
                            if (isVampiric) stats.VampiricHealing += evt.Value;
                            else stats.HealingDone += evt.Value;

                            // Évaluation Ange Gardien : PV de la cible inférieurs à 30% lors du déclenchement
                            if (evt.Target != null && evt.Target.HPLeft > 0 && evt.Target.HPLeft < (evt.Target.MaxHP * 0.3f))
                            {
                                stats.GuardianAngelHealing += evt.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleHealDamage) : " + ex);
            }
        }

        public void OnEventAboutToTrigger(RuleDealStatDamage evt) { }

        public void OnEventDidTrigger(RuleDealStatDamage evt)
        {
            try
            {
                if (!isCombatActive || evt.Initiator == null || evt.Target == null || evt.Stat == null) return;
                if (evt.Initiator == evt.Target || evt.Initiator.IsPlayerFaction == evt.Target.IsPlayerFaction) return; 

                int damageApplied = evt.Result;
                if (damageApplied > 0)
                {
                    var stats = GetOrAddStats(evt.Initiator, out bool isSummon);
                    if (stats != null)
                    {
                        if (isSummon) stats.Summons.StatDamage += damageApplied;
                        else stats.StatDamage += damageApplied;
                        if (evt.Stat.ModifiedValueRaw < 1 && (evt.Stat.ModifiedValueRaw + damageApplied) >= 1)
                        {
                            if (!deadUnitsThisCombat.Contains(evt.Target.UniqueId))
                            {
                                deadUnitsThisCombat.Add(evt.Target.UniqueId);
                                float multiplier = GetTargetMultiplier(evt.Target);
                                if (isSummon)
                                {
                                    stats.SummonKills++;
                                    stats.Summons.Kills++;
                                    stats.Summons.InstaKills++;
                                }
                                else
                                {
                                    stats.Kills++;
                                    stats.InstaKills++;
                                    stats.StatDamageKills++;
                                }
                                stats.WeightedKills += 1.5f * multiplier;

                                if (evt.Target.Blueprint != null && evt.Target.Blueprint.CR >= currentPartyLevel)
                                {
                                    stats.HighDangerInstaKills++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleDealStatDamage) : " + ex);
            }
        }

        // ====================================================================
        // 3. LA MÉCANIQUE D'INSTA-KILL (LE COULOIR DE LA MORT)
        // ====================================================================

        public void OnEventAboutToTrigger(RuleSavingThrow evt) { }

        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
            try
            {
                if (!isCombatActive || evt.Initiator == null) return;
                
                // --- CAPTURE DES SÉCURITÉS POUR LE DÉBRIEFING (ALLIÉS, ENNEMIS ET INVOCATIONS) ---
                var statsSec = GetOrAddStats(evt.Initiator, out bool secIsSummon);
                if (statsSec != null)
                {
                    string sourceName = null;
                    if (!evt.IsPassed)
                    {
                        // Extraction sécurisée du nom de l'effet d'origine avec cascade de replis pour contrer les chaînes vides ""
                        if (evt.Reason != null)
                        {
                            string candidate = evt.Reason.Name;
                            if (!string.IsNullOrEmpty(candidate))
                            {
                                sourceName = candidate;
                            }
                            else if (evt.Reason.Ability != null && !string.IsNullOrEmpty(evt.Reason.Ability.Name))
                            {
                                sourceName = evt.Reason.Ability.Name;
                            }
                            else if (evt.Reason.Fact != null && !string.IsNullOrEmpty(evt.Reason.Fact.Name))
                            {
                                sourceName = evt.Reason.Fact.Name;
                            }
                            else if (evt.Reason.Context?.SourceAbility != null && !string.IsNullOrEmpty(evt.Reason.Context.SourceAbility.Name))
                            {
                                sourceName = evt.Reason.Context.SourceAbility.Name;
                            }
                        }

                        // Repli de secours sur le buff rattaché au jet de sauvegarde si la raison est restée muette
                        if (string.IsNullOrEmpty(sourceName) && evt.Buff != null)
                        {
                            sourceName = evt.Buff.Name;
                            if (string.IsNullOrEmpty(sourceName))
                            {
                                sourceName = evt.Buff.name; // Repli ultime vers le nom de blueprint technique interne
                            }
                        }

                        // Attribution finale si tous les diagnostics ont échoué
                        if (string.IsNullOrEmpty(sourceName))
                        {
                            sourceName = Localization.GetStringById("ui.dmg.other") ?? "Effet inconnu";
                        }
                    }

                    AddSavingThrowStats(statsSec, secIsSummon, evt, sourceName);
                }
                
                var victim = evt.Initiator; 
                var attacker = evt.Reason?.Context?.MaybeCaster; 
                if (attacker == null || attacker == victim || attacker.IsPlayerFaction == victim.IsPlayerFaction) return;

                var ability = evt.Reason?.Context?.SourceAbility;
                if (ability != null && !string.IsNullOrEmpty(ability.name))
                {
                    // 1. Zéro Boxing sur l'Enum (cast en long pour opération binaire brute)
                    // 2. Zéro allocation de string (utilisation d'IndexOf)
                    bool isDeathSpell = ((long)ability.SpellDescriptor & (long)SpellDescriptor.Death) != 0 || 
                                        ability.name.IndexOf("phantasmalkiller", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                        ability.name.IndexOf("weird", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                        ability.name.IndexOf("absolutedeath", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        ability.name.IndexOf("wailof", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isDeathSpell && !evt.IsPassed)
                    {
                        var stats = GetOrAddStats(attacker, out _);
                        if (stats != null)
                        {
                            // SUPPRESSION DE "stats.IconicSpellsCast++;" (Fin du double-comptage)
                            doomedTargets[victim.UniqueId] = attacker;
                        }
                    }
                }
				
                // --- CAPTURE DES SORTS ET EFFETS ÉVITÉS PAR LES ENNEMIS (JETS DE SAUVEGARDE RÉUSSIS) ---
                var victimUnit = evt.Initiator;
                var casterUnit = evt.Reason?.Context?.MaybeCaster;

                if (casterUnit != null && casterUnit.IsPlayerFaction && !victimUnit.IsPlayerFaction)
                {
                    if (evt.IsPassed)
                    {
                        var casterStats = GetOrAddStats(casterUnit, out _);
                        if (casterStats != null)
                        {
                            var sourceAbility = evt.Reason?.Context?.SourceAbility;
                            string spellName = null;
                            if (sourceAbility != null)
                            {
                                spellName = sourceAbility.Name ?? sourceAbility.name;
                            }
                            if (string.IsNullOrEmpty(spellName) && evt.Reason != null)
                            {
                                spellName = evt.Reason.Name;
                            }
                            if (string.IsNullOrEmpty(spellName))
                            {
                                spellName = Localization.GetStringById("ui.dmg.other") ?? "Effet inconnu";
                            }

                            string enemyName = victimUnit.CharacterName ?? "Inconnu";

                            if (!casterStats.SpellsSavedSources.ContainsKey(spellName))
                            {
                                casterStats.SpellsSavedSources[spellName] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            }

                            if (casterStats.SpellsSavedSources[spellName].ContainsKey(enemyName))
                            {
                                casterStats.SpellsSavedSources[spellName][enemyName]++;
                            }
                            else
                            {
                                casterStats.SpellsSavedSources[spellName][enemyName] = 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleSavingThrow) : " + ex);
            }
        }

        // ====================================================================
        // PURGE PRÉVENTIVE ET LIBÉRATION MÉMOIRE (IAreaHandler)
        // ====================================================================

        
		// Mise en cache du FieldInfo pour des performances absolues (Zéro allocation à la volée)
        private static readonly FieldInfo s_ItemDisplayNameField = typeof(Kingmaker.Blueprints.Items.BlueprintItem).GetField("m_DisplayNameText", BindingFlags.NonPublic | BindingFlags.Instance);

        public static string GetDamageSourceName(RuleDealDamage evt)
        {
            try
            {
                if (evt.AttackRoll != null && evt.AttackRoll.Weapon != null)
                {
                    var weapon = evt.AttackRoll.Weapon;
                    string cleanName = null;
                    if (weapon.Blueprint != null)
                    {
                        try
                        {
                            // Utilisation du cache au lieu d'interroger la réflexion à chaque coup !
                            var locStr = s_ItemDisplayNameField?.GetValue(weapon.Blueprint) as Kingmaker.Localization.LocalizedString;
                            cleanName = locStr?.ToString();
                        }
                        catch (Exception) { }

                        // Repli 1 : Nom générique du type d'arme (ex: "Longsword" / "Épée longue") s'il n'y a pas de nom unique défini
                        if (string.IsNullOrEmpty(cleanName) && weapon.Blueprint is Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon bpWeapon)
                        {
                            cleanName = bpWeapon.Type?.DefaultName?.ToString();
                        }
                    }

                    // Repli 2 : Nom standard d'instance (enchanté) si tout le reste a échoué
                    if (string.IsNullOrEmpty(cleanName))
                    {
                        cleanName = weapon.Name;
                    }

                    if (!string.IsNullOrEmpty(cleanName)) return cleanName;
                }

                if (evt.Reason != null)
                {
                    string reasonName = evt.Reason.Name;
                    if (!string.IsNullOrEmpty(reasonName)) return reasonName;
                }

                if (evt.Reason?.Context?.SourceAbility != null)
                {
                    string spellName = evt.Reason.Context.SourceAbility.Name;
                    if (!string.IsNullOrEmpty(spellName)) return spellName;
                }

                if (evt.SourceAbility != null)
                {
                    string spellName = evt.SourceAbility.Name;
                    if (!string.IsNullOrEmpty(spellName)) return spellName;
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur lors du calcul du nom de la source : " + ex.Message);
            }

            return Localization.GetStringById("ui.dmg.other") ?? "Autres sources";
        }

        // Traduction unifiée de l'énumération des types de dégâts physiques et d'énergies
        public static string GetDamageTypeLocalizedName(BaseDamage source)
        {
            if (source == null) return Localization.GetStringById("ui.dmg.other") ?? "Autres sources";

            if (source is PhysicalDamage physical)
            {
                // Opérations binaires pures = Zéro Garbage Collection, Zéro Boxing.
                bool s = (physical.Form & PhysicalDamageForm.Slashing) != 0;
                bool p = (physical.Form & PhysicalDamageForm.Piercing) != 0;
                bool b = (physical.Form & PhysicalDamageForm.Bludgeoning) != 0;

                if (s && p && b) return Localization.GetStringById("ui.dmg.all_phys") ?? "Tranchant/Perçant/Contondant";
                if (s && p) return Localization.GetStringById("ui.dmg.slash_pierce") ?? "Tranchant/Perçant";
                if (s && b) return Localization.GetStringById("ui.dmg.slash_bludgeon") ?? "Tranchant/Contondant";
                if (p && b) return Localization.GetStringById("ui.dmg.pierce_bludgeon") ?? "Perçant/Contondant";
                if (s) return Localization.GetStringById("ui.dmg.slashing") ?? "Tranchant";
                if (p) return Localization.GetStringById("ui.dmg.piercing") ?? "Perçant";
                if (b) return Localization.GetStringById("ui.dmg.bludgeoning") ?? "Contondant";
            }
            else if (source is EnergyDamage energy)
            {
                switch (energy.EnergyType)
                {
                    case DamageEnergyType.Fire: return Localization.GetStringById("ui.dmg.fire") ?? "Feu";
                    case DamageEnergyType.Cold: return Localization.GetStringById("ui.dmg.cold") ?? "Froid";
                    case DamageEnergyType.Acid: return Localization.GetStringById("ui.dmg.acid") ?? "Acide";
                    case DamageEnergyType.Electricity: return Localization.GetStringById("ui.dmg.electricity") ?? "Électricité";
                    case DamageEnergyType.Sonic: return Localization.GetStringById("ui.dmg.sonic") ?? "Son";
                    case DamageEnergyType.NegativeEnergy: return Localization.GetStringById("ui.dmg.negative") ?? "Négatif";
                    case DamageEnergyType.Holy: 
                    case DamageEnergyType.Divine: return Localization.GetStringById("ui.dmg.holy") ?? "Sacré";
                    case DamageEnergyType.Unholy: return Localization.GetStringById("ui.dmg.unholy") ?? "Profane";
                }
            }
            else if (source.Type == DamageType.Force)
            {
                return "Force";
            }
            else if (source.Type == DamageType.Direct)
            {
                return "Direct";
            }

            return Localization.GetStringById("ui.dmg.other") ?? "Autres sources";
        }
		
		// Résolution dynamique du nom du modificateur (Force, Châtiment, Attaque en puissance, etc.)
        // Résolution dynamique du nom du modificateur (Force, Châtiment, Attaque en puissance, etc.)
        public static string GetModifierName(Modifier mod)
        {
            try
            {
                // 1. Priorité absolue : le vrai nom localisé du don, buff ou arme (ex: "Attaque en puissance")
                if (mod.Fact != null && !string.IsNullOrEmpty(mod.Fact.Name))
                {
                    return mod.Fact.Name;
                }

                // 2. Traductions spécifiques pré-enregistrées
                switch (mod.Descriptor)
                {
                    case ModifierDescriptor.None: return Localization.GetStringById("ui.dmg.audit_other") ?? "Base / Autre";
                    case ModifierDescriptor.Difficulty: return Localization.GetStringById("ui.dmg.audit_difficulty") ?? "Difficulty";
                    case ModifierDescriptor.Rage: return Localization.GetStringById("ui.dmg.audit_rage") ?? "Rage";
                    case ModifierDescriptor.Enhancement: return Localization.GetStringById("ui.dmg.audit_enhancement") ?? "Enhancement";
                    case ModifierDescriptor.Focus: return Localization.GetStringById("ui.dmg.audit_focus") ?? "Focus";
                    case ModifierDescriptor.Morale: return Localization.GetStringById("ui.dmg.audit_morale") ?? "Morale";
                    case ModifierDescriptor.Luck: return Localization.GetStringById("ui.dmg.audit_luck") ?? "Luck";
                    case ModifierDescriptor.Sacred: return Localization.GetStringById("ui.dmg.audit_sacred") ?? "Sacred";
                    case ModifierDescriptor.Profane: return Localization.GetStringById("ui.dmg.audit_profane") ?? "Profane";
                    case ModifierDescriptor.Size: return Localization.GetStringById("ui.dmg.audit_size") ?? "Size";
                    case ModifierDescriptor.Dodge: return Localization.GetStringById("ui.dmg.audit_dodge") ?? "Dodge";
                    case ModifierDescriptor.WeaponTraining: return Localization.GetStringById("ui.dmg.audit_weapon_training") ?? "Weapon Training";
                    case ModifierDescriptor.UntypedStackable: return Localization.GetStringById("ui.dmg.audit_untyped_stackable") ?? "Stackable Modifier";
                }

                // 3. SOLUTION UNIVERSELLE : Pour les ~47 autres descripteurs (Feat, Alchemical, Circumstance, etc.)
                // On sépare automatiquement les mots attachés (ex: "ArmorEnhancement" devient "Armor Enhancement")
                string enumName = mod.Descriptor.ToString();
                return System.Text.RegularExpressions.Regex.Replace(enumName, "([a-z])([A-Z])", "$1 $2");
            }
            catch (Exception) { }

            return mod.Descriptor.ToString();
        }
		
		public void OnAreaDidLoad()
        {
            try
            {
                Localization.Init(Main.ModPath);
                if (combatStats != null)
                {
                    combatStats.Clear();
                }
                if (sessionHistory != null)
                {
                    sessionHistory.Clear();
                    sessionCombatCounter = 0;
                }
                if (deadUnitsThisCombat != null)
                {
                    deadUnitsThisCombat.Clear();
                }
                if (doomedTargets != null)
                {
                    doomedTargets.Clear();
                }
                if (CombatMVP_UI.Instance != null)
                {
                    CombatMVP_UI.Instance.ResetUI();
                }
                Main.Logger.Log("[CombatMVP] La trame du monde s'est reconfigurée. Données de combat purgées, VRAM et cache libérés.");
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"[CombatMVP] Erreur lors du nettoyage de la zone : {ex}");
            }
        }

        public void OnAreaBeginUnloading()
        {
            try
            {
                if (CombatMVP_UI.Instance != null)
                {
                    CombatMVP_UI.Instance.ResetUI();
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"[CombatMVP] Erreur lors du début de déchargement de zone : {ex}");
            }
        }
		
		public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt) { }

        public void OnEventDidTrigger(RuleSpellResistanceCheck evt)
        {
            try
            {
                if (!isCombatActive || evt.Initiator == null || evt.Target == null) return;

                // Si un membre du groupe lance un sort et qu'un ennemi y résiste via sa SR
                if (evt.Initiator.IsPlayerFaction && !evt.Target.IsPlayerFaction)
                {
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null)
                    {
                        if (evt.IsSpellResisted)
                        {
                            stats.SpellsResistedCount++;

                            // Extraction du nom du sort et de la cible pour l'audit détaillé
                            string spellName = evt.Ability?.Name ?? evt.Context?.Name ?? "Sort inconnu";
                            string targetName = evt.Target?.CharacterName ?? "Inconnu";

                            if (!stats.SpellsResistedSources.ContainsKey(spellName))
                                stats.SpellsResistedSources[spellName] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                
                            if (stats.SpellsResistedSources[spellName].ContainsKey(targetName))
                                stats.SpellsResistedSources[spellName][targetName]++;
                            else
                                stats.SpellsResistedSources[spellName][targetName] = 1;
                        }
                        else
                        {
                            // NOUVEAU : Le sort vient de passer la Résistance Magique avec succès !
                            stats.SpellsPenetratedCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleSpellResistanceCheck) : " + ex);
            }
        }
		
		public void HandleUnitSpawned(UnitEntityData unit)
        {
            try
            {
                if (!isCombatActive || unit == null) return;

                // Fige l'allégeance de référence des renforts apparus en cours de combat.
                if (!string.IsNullOrEmpty(unit.UniqueId) && !baselineAllegiance.ContainsKey(unit.UniqueId))
                {
                    baselineAllegiance[unit.UniqueId] = IsCurrentlyAlly(unit);
                }

                var summonPart = unit.Get<Kingmaker.UnitLogic.Parts.UnitPartSummonedMonster>();
                if (summonPart != null && summonPart.Summoner != null)
                {
                    var summonerStats = GetOrAddStats(summonPart.Summoner, out _);
                    if (summonerStats != null)
                    {
                        summonerStats.SummonsCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans HandleUnitSpawned : " + ex.Message);
            }
        }
		
		public void OnEventAboutToTrigger(RuleDispelMagic evt) { }

        public void OnEventDidTrigger(RuleDispelMagic evt)
        {
            try
            {
                if (!isCombatActive || evt.Initiator == null || !evt.Success) return;
                // On ne compte pas les auto-dissipations : retirer une de ses propres conditions via un don
                // (ex : Délivrance des Contraintes) passe par RuleDispelMagic mais n'est pas une vraie dissipation.
                if (evt.Target != null && evt.Target == evt.Initiator) return;
                var stats = GetOrAddStats(evt.Initiator, out _);
                if (stats != null)
                {
                    stats.DispelledCount++;

                    string dispelledName = null;
                    if (evt.Buff != null)
                    {
                        dispelledName = evt.Buff.Name;
                        if (string.IsNullOrEmpty(dispelledName) && evt.Buff.Blueprint != null) dispelledName = evt.Buff.Blueprint.name;
                    }
                    if (string.IsNullOrEmpty(dispelledName) && evt.AreaEffect != null && evt.AreaEffect.Blueprint != null)
                    {
                        dispelledName = evt.AreaEffect.Blueprint.name;
                    }
                    if (string.IsNullOrEmpty(dispelledName))
                    {
                        dispelledName = Localization.GetStringById("ui.dmg.other") ?? "Effet inconnu";
                    }
                    IncrementSourceCount(stats.DispelledSpells, dispelledName);
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleDispelMagic) : " + ex.Message);
            }
        }

        public void OnEventAboutToTrigger(RuleCombatManeuver evt) { }

        public void OnEventDidTrigger(RuleCombatManeuver evt)
        {
            try
            {
                if (!isCombatActive || evt.Initiator == null || !evt.Success) return;
                if (evt.Type == Kingmaker.RuleSystem.Rules.CombatManeuver.Trip)
                {
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null)
                    {
                        stats.TrippedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleCombatManeuver) : " + ex.Message);
            }
        }

        public void HandleUnitResurrected(UnitEntityData unit)
        {
            // Le crédit des résurrections est attribué de manière fiable au véritable lanceur dans
            // OnEventDidTrigger(RuleCastSpell). Ce gestionnaire ne ré-incrémente donc plus le compteur,
            // ce qui évitait à la fois un double comptage et une attribution à la mauvaise unité
            // (l'unité sélectionnée / le personnage principal au lieu du soigneur réel).
        }

        public void HandleUnitDestroyed(UnitEntityData unit)
        {
            // Pas de traitement requis pour la destruction d'entité physique
        }

        public void HandleUnitDeath(UnitEntityData unit)
        {
            try
            {
                if (!isCombatActive || unit == null) return;
                if (deadUnitsThisCombat.Contains(unit.UniqueId)) return;

                deadUnitsThisCombat.Add(unit.UniqueId);

                // Déterminer qui obtient le crédit du kill de manière robuste
                UnitEntityData killer = null;
                bool isInstakill = false;

                if (doomedTargets.TryGetValue(unit.UniqueId, out var doomedCaster))
                {
                    killer = doomedCaster;
                    isInstakill = true;
                    doomedTargets.Remove(unit.UniqueId);
                }
                else if (unit.LastHandledDamage != null)
                {
                    killer = unit.LastHandledDamage.Initiator;
                }

                if (killer != null)
                {
                    var killerStats = GetOrAddStats(killer, out bool isSummon);
                    var victimStats = GetOrAddStats(unit, out _);

                    if (killerStats != null && victimStats != null)
                    {
                        // S'assurer qu'on n'attribue des kills qu'entre factions opposées
                        if (killerStats.IsAlly != victimStats.IsAlly)
                        {
                            string victimName = unit.CharacterName ?? "Inconnu";
                            bool isHighDanger = unit.Blueprint != null && unit.Blueprint.CR >= currentPartyLevel;

                            if (isHighDanger)
                            {
                                killerStats.HighDangerKills++;
                            }

                            if (isInstakill)
                            {
                                if (isSummon)
                                {
                                    killerStats.SummonKills++;
                                    killerStats.Summons.Kills++;
                                    killerStats.Summons.InstaKills++;
                                }
                                else
                                {
                                    killerStats.InstaKills++;
                                    killerStats.Kills++;
                                }

                                if (isHighDanger)
                                {
                                    killerStats.HighDangerInstaKills++;
                                }

                                float instakillMultiplier = GetTargetMultiplier(unit);
                                if (isHighDanger)
                                {
                                    instakillMultiplier *= 1.5f;
                                }
                                killerStats.WeightedKills += 1f * instakillMultiplier;
                            }
                            else
                            {
                                if (isSummon)
                                {
                                    killerStats.SummonKills++;
                                    killerStats.Summons.Kills++;
                                }
                                else killerStats.Kills++;

                                killerStats.WeightedKills += 1f * GetTargetMultiplier(unit);
                            }

                            // Enregistrer la victime de façon groupée
                            if (killerStats.KilledUnits.ContainsKey(victimName))
                                killerStats.KilledUnits[victimName]++;
                            else
                                killerStats.KilledUnits[victimName] = 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans HandleUnitDeath : " + ex);
            }
        }
		
    } // FIN DU COMBAT TRACKER
	
	// =========================================================================
    // PARTIE 3 : L'ARCHIVE DES HAUTS FAITS (ACHIEVEMENT DATABASE) - VERSION IMPRESSIVE v1.4.1
    // =========================================================================
    public static class AchievementDatabase
    {
        public static void GrantAchievements(UnitCombatStats stat, float combatWeight, bool isAbsoluteMVP, string category)
        {
            if (stat == null) return;
            stat.Achievements.Clear();

            if (category == "Enemy" || !stat.IsAlly || stat.IsReanimated)
            {
                return;
            }

            int partyLevel = Main.Tracker != null ? Main.Tracker.currentPartyLevel : 1;
            if (partyLevel <= 0) partyLevel = 1;

            // Pré-calcul global des CC (Contrôles de foule) pour usage répété
            int totalCC = stat.CC_Prone + stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + 
                          stat.CC_Shaken + stat.CC_Nauseated + stat.CC_Sickened + stat.CC_Blinded + 
                          stat.CC_Entangled + stat.CC_Confused + stat.CC_Exhausted + stat.CC_Fatigued + 
                          stat.CC_Slowed + stat.CC_Staggered + stat.CC_Petrified + stat.CC_Asleep + 
                          stat.CC_Dazed + stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;

            // 1. Tank sans armure (Vérifié: Bracers of Armor = ProficencyGroup.None)
            var armorItem = stat.UnitData != null ? stat.UnitData.Body.Armor.MaybeItem as Kingmaker.Items.ItemEntityArmor : null;
            bool hasNoArmor = armorItem == null || armorItem.ArmorType() == Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.None;
            if (hasNoArmor && stat.DamageTaken > (partyLevel * 20) && stat.AttacksDodged >= 10)
            {
                stat.Achievements.Add(new MVPAchievement("B", Localization.GetStringById("ACH_TITLE_1"), Localization.GetFormatted("ACH_DESC_1", stat), Color.white, 30));
            }

            // 2. Maître des coups critiques
            if (stat.Crits >= 12)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_2"), Localization.GetFormatted("ACH_DESC_2", stat), Color.white, 70));
            }

            // 3. Destructeur divin (Ange)
            bool isAngel = stat.MythicPathName.ToLower().Contains("angel") || stat.MythicPathInternalName.ToLower().Contains("angel");
            if (isAngel && stat.MaxSingleHit >= (partyLevel * 15))
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_3"), Localization.GetFormatted("ACH_DESC_3", stat), Color.white, 90));
            }

            // 4. Maître des AoO (Attaques d'opportunité)
            if (stat.AoOs >= 15)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_4"), Localization.GetFormatted("ACH_DESC_4", stat), Color.white, 72));
            }

            // 5. Maître des sorts fusionnés (Deceiver - Axé sur le Contrôle de Zone)
            bool isDeceiver = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.ToLower().Contains("magicdeceiver"));
            if (isDeceiver && totalCC >= 12)
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_5"), Localization.GetFormatted("ACH_DESC_5", stat), Color.white, 88));
            }

            // 6. Cavalier de Bismuth (Vérification par Blueprint stricte)
            bool isBismuth = stat.UnitData != null && stat.UnitData.IsPet && (stat.UnitData.CharacterName.ToLower().Contains("bismuth") || stat.UnitData.Blueprint.AssetGuid.ToString() == "7c2de930e441ea249be955610f84c748");
            if (isBismuth && (stat.TotalDamage + stat.SummonDamage) > (partyLevel * 25))
            {
                stat.Achievements.Add(new MVPAchievement("B", Localization.GetStringById("ACH_TITLE_6"), Localization.GetFormatted("ACH_DESC_6", stat), Color.white, 35));
            }

            // 7. Spécialiste de la méga frappe sournoise (Sécurisé pour réclamer de vrais lourds dégâts sournois)
            if (stat.MaxSingleHit >= (partyLevel * 25) && stat.SneakAttackDmg >= (partyLevel * 15))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_7"), Localization.GetFormatted("ACH_DESC_7", stat), Color.white, 75));
            }

            // 8. Pounce Provider (Skald)
            // La capacité de bond (Pounce) partagée par le Scalde n'a de valeur que si un allié
            // s'en est réellement servi pour charger : on exige donc qu'au moins une charge ait
            // infligé des dégâts dans le groupe (signal officiel IsCharge d'Owlcat).
            bool isSkald = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.ToLower().Contains("skald"));
            bool partyLandedCharge = Main.Tracker != null && Main.Tracker.combatStats.Values.Any(s => s.IsAlly && s.ChargeDamage > 0);
            if (isSkald && stat.SupportBuffsCast >= 15 && partyLandedCharge)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_8"), Localization.GetFormatted("ACH_DESC_8", stat), Color.white, 78));
            }

            // 9. L'exécuteur fantomatique (Insta-kills)
            if (stat.InstaKills >= 5)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_9"), Localization.GetFormatted("ACH_DESC_9", stat), Color.white, 80));
            }

            // 10. Le Démon Cinétiste (Corrigé : Ne récompense plus les AFK)
            bool isKineticist = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.ToLower().Contains("kineticist"));
            bool isDemon = stat.MythicPathName.ToLower().Contains("demon") || stat.MythicPathInternalName.ToLower().Contains("demon");
            if (isKineticist && isDemon && stat.TotalDamage >= (partyLevel * 30))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_10"), Localization.GetFormatted("ACH_DESC_10", stat), Color.white, 82));
            }

            // 11. Maître vampirique
            if (stat.VampiricHealing >= (partyLevel * 10) && stat.NegativeDmg > 0)
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_11"), Localization.GetFormatted("ACH_DESC_11", stat), Color.white, 50));
            }

            // 12. Briseur d'armures (Corrigé : Exige des dégâts PHYSIQUES massifs, pas juste génériques)
            long totalPhysical = stat.SlashingDmg + stat.PiercingDmg + stat.BludgeoningDmg + stat.AllPhysDmg + stat.SlashPierceDmg + stat.SlashBludgeonDmg + stat.PierceBludgeonDmg;
            if (totalPhysical >= (partyLevel * 35) && stat.MaxSingleHit >= (partyLevel * 15))
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_12"), Localization.GetFormatted("ACH_DESC_12", stat), Color.white, 52));
            }

            // 13. Spécialiste du bouclier (Corrigé : Exige d'avoir TANKÉ des coups)
            bool hasShieldAndWeapon = stat.UnitData != null && stat.UnitData.Body.PrimaryHand.HasWeapon && stat.UnitData.Body.SecondaryHand.HasShield;
            if (hasShieldAndWeapon && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 25) && stat.AttacksDodged >= 6)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_13"), Localization.GetFormatted("ACH_DESC_13", stat), Color.white, 74));
            }

            // 14. Destructeur chaotique (Friendly Fire)
            float totalDmgInclSummons = stat.TotalDamage + stat.SummonDamage;
            if (totalDmgInclSummons > 0f && stat.FriendlyFireDmg >= 0.35f * totalDmgInclSummons && stat.FriendlyFireDmg >= 100 && totalDmgInclSummons >= (partyLevel * 45))
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_14"), Localization.GetFormatted("ACH_DESC_14", stat), Color.white, 54));
            }

            // 15. Magic Deceiver Master (Axé sur les dégâts purs)
            if (isDeceiver && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 50))
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_15"), Localization.GetFormatted("ACH_DESC_15", stat), Color.white, 92));
            }

            // 16. L'archer du déluge de flèches
            bool hasBow = stat.UnitData != null && stat.UnitData.Body.PrimaryHand.MaybeWeapon != null && 
                          (stat.UnitData.Body.PrimaryHand.MaybeWeapon.Blueprint.Type.Category == Kingmaker.Enums.WeaponCategory.Longbow || 
                           stat.UnitData.Body.PrimaryHand.MaybeWeapon.Blueprint.Type.Category == Kingmaker.Enums.WeaponCategory.Shortbow);
            if (hasBow && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 50))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_16"), Localization.GetFormatted("ACH_DESC_16", stat), Color.white, 84));
            }

            // 17. Le dévoreur de caractéristiques
            if (stat.StatDamage >= 10)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_17"), Localization.GetFormatted("ACH_DESC_17", stat), Color.white, 76));
            }

            // 18. Le briseur de barrières (CORRIGÉ : Repose désormais sur une vraie mécanique de pénétration)
            if (stat.SpellsPenetratedCount >= 4)
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_18"), Localization.GetFormatted("ACH_DESC_18", stat), Color.white, 56));
            }

            // 19. Le rempart blindé
            var armorItemHeavy = stat.UnitData != null ? stat.UnitData.Body.Armor.MaybeItem as Kingmaker.Items.ItemEntityArmor : null;
            bool isHeavyArmor = armorItemHeavy != null && armorItemHeavy.ArmorType() == Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.Heavy;
            if (isHeavyArmor && stat.UnitData.Stats.AC.ModifiedValue >= (partyLevel * 3) && stat.AttacksDodged >= 10)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_19"), Localization.GetFormatted("ACH_DESC_19", stat), Color.white, 86));
            }

            // 20. Maître des nuées (Swarm)
            if (stat.SummonDamage >= 50 && stat.CC_Nauseated >= 5 && stat.StatDamage >= 10)
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_20"), Localization.GetFormatted("ACH_DESC_20", stat), Color.white, 94));
            }

            // 21. Maître du coup de grâce (Corrigé : Exige d'avoir rendu les cibles Helpless ET de les avoir tuées)
            if (stat.CC_Helpless >= 2 && stat.Kills >= 2)
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_21"), Localization.GetFormatted("ACH_DESC_21", stat), Color.white, 58));
            }

            // 22. Volonté de fer
            if (stat.SavesWillSucceeded >= 5)
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_22"), Localization.GetFormatted("ACH_DESC_22", stat), Color.white, 60));
            }

            // 23. Buveur d'âmes
            if (stat.NegativeLevels >= 40)
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_23"), Localization.GetFormatted("ACH_DESC_23", stat), Color.white, 96));
            }

            // 24. Terreur de Baphomet
            bool isBaphometFight = Main.Tracker != null && Main.Tracker.combatStats.Values.Any(s => !s.IsAlly && s.CR > 30 && (s.Name.ToLower().Contains("baphomet") || (s.UnitData != null && s.UnitData.Blueprint.AssetGuid.ToString() == "0a53ea30e441ea249be955610f84c748")));
            if (isBaphometFight)
            {
                float totalGroupEffort = Main.Tracker.combatStats.Values.Where(s => s.IsAlly).Sum(s => s.TotalDamage + s.SummonDamage + s.HealingDone + s.VampiricHealing);
                float myContribution = stat.TotalDamage + stat.SummonDamage + stat.HealingDone + stat.VampiricHealing;
                if (totalGroupEffort > 0f && (myContribution / totalGroupEffort) >= 0.50f)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS", Localization.GetStringById("ACH_TITLE_24"), Localization.GetFormatted("ACH_DESC_24", stat), Color.white, 100));
                }
            }

            // 25. Le prêtre dévoué
            bool isCleric = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.ToLower().Contains("cleric"));
            if (isCleric && stat.SupportBuffsCast >= 40)
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_25"), Localization.GetFormatted("ACH_DESC_25", stat), Color.white, 98));
            }

            // 26. Maître de la dissipation magique (Corrigé : Suppression de l'obligation de réussir ses propres jets de sauvegarde)
            if (stat.DispelledCount >= 5)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_26"), Localization.GetFormatted("ACH_DESC_26", stat), Color.white, 70));
            }

            // 27. Le pourfendeur de géants (Corrigé : Vérifie que CE personnage a tué la créature géante)
            bool killedGiant = false;
            if (stat.KilledUnits.Count > 0 && Main.Tracker != null)
            {
                foreach (var killedName in stat.KilledUnits.Keys)
                {
                    var victimStat = Main.Tracker.combatStats.Values.FirstOrDefault(s => !s.IsAlly && s.Name == killedName);
                    if (victimStat != null && victimStat.UnitData != null && victimStat.UnitData.Descriptor.OriginalSize >= Kingmaker.Enums.Size.Large)
                    {
                        killedGiant = true;
                        break;
                    }
                }
            }
            if (killedGiant && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 20))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_27"), Localization.GetFormatted("ACH_DESC_27", stat), Color.white, 71));
            }

            // 28. Le spécialiste du croc-en-jambe
            if (stat.TrippedCount >= 6)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_28"), Localization.GetFormatted("ACH_DESC_28", stat), Color.white, 73));
            }

            // 29. Le gardien intouchable (Corrigé : Exige d'être activement pris pour cible)
            int totalAttacksDirectedAtMe = stat.HitsPhysicalTaken + stat.AttacksDodged;
            if (totalAttacksDirectedAtMe >= 15 && stat.DamageTaken == 0)
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_29"), Localization.GetFormatted("ACH_DESC_29", stat), Color.white, 85));
            }

            // 30. Le maître des invocations
            if (stat.SummonsCount >= 4 && stat.SummonDamage >= (partyLevel * 25))
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_30"), Localization.GetFormatted("ACH_DESC_30", stat), Color.white, 91));
            }

            // 31. Spécialiste du Pavois (Corrigé : Vérification stricte via l'Enum ProficiencyGroup)
            var shieldItem = stat.UnitData?.Body?.SecondaryHand?.MaybeShield;
            bool hasTowerShield = shieldItem != null && shieldItem.Blueprint.Type.ProficiencyGroup == Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.TowerShield;
            if (hasTowerShield && stat.AttacksDodged >= 10)
            {
                stat.Achievements.Add(new MVPAchievement("B", Localization.GetStringById("ACH_TITLE_31"), Localization.GetFormatted("ACH_DESC_31", stat), Color.white, 31));
            }

            // 32. Faiseur de miracles
            if (stat.ResurrectedCount >= 2)
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_32"), Localization.GetFormatted("ACH_DESC_32", stat), Color.white, 93));
            }

            // 33. Ange gardien
            if (stat.GuardianAngelHealing >= (partyLevel * 20))
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_33"), Localization.GetFormatted("ACH_DESC_33", stat), Color.white, 51));
            }

            // 34. Le destructeur de démons
            bool isDemonSlayer = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.IndexOf("ranger", StringComparison.OrdinalIgnoreCase) >= 0 && c.Archetypes.Any(a => a.name.IndexOf("demonslayer", StringComparison.OrdinalIgnoreCase) >= 0));
            if (isDemonSlayer && stat.Kills >= 4)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_34"), Localization.GetFormatted("ACH_DESC_34", stat), Color.white, 77));
            }

            // 35. Le maître du feu d'enfer
            if (stat.TotalDamage >= (partyLevel * 50) && stat.MaxSingleHit >= (partyLevel * 30) && stat.FireDmg > 0)
            {
                stat.Achievements.Add(new MVPAchievement("SS", Localization.GetStringById("ACH_TITLE_35"), Localization.GetFormatted("ACH_DESC_35", stat), Color.white, 95));
            }

            // 36. L'œil du faucon
            bool hasHawkEyeBow = stat.UnitData != null && stat.UnitData.Body.PrimaryHand.MaybeWeapon != null && 
                          (stat.UnitData.Body.PrimaryHand.MaybeWeapon.Blueprint.Type.Category == Kingmaker.Enums.WeaponCategory.Longbow || 
                           stat.UnitData.Body.PrimaryHand.MaybeWeapon.Blueprint.Type.Category == Kingmaker.Enums.WeaponCategory.Shortbow);
            if (hasHawkEyeBow && stat.Crits >= 5 && stat.TotalDamage >= (partyLevel * 30))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_36"), Localization.GetFormatted("ACH_DESC_36", stat), Color.white, 81));
            }

            // 37. Le cavalier de charge
            bool isMountedCharge = stat.UnitData != null && stat.UnitData.RiderPart != null && stat.UnitData.RiderPart.SaddledUnit != null;
            if (isMountedCharge && stat.MaxSingleHit >= (partyLevel * 15))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_37"), Localization.GetFormatted("ACH_DESC_37", stat), Color.white, 83));
            }

            // 38. Le maître de la lame cinétique
            bool isKineticBladeSource = stat.DamageBySource.Keys.Any(k => k.IndexOf("kinetic blade", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("lame cinétique", StringComparison.OrdinalIgnoreCase) >= 0);
            if (isKineticBladeSource && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 25))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_38"), Localization.GetFormatted("ACH_DESC_38", stat), Color.white, 85));
            }

            // 39. Le chirurgien de l'escouade (Corrigé : Ajout d'un seuil minimal de dégâts pour éviter les faux positifs sur les combats vides)
            float totalEncountersGroupDamage = Main.Tracker != null ? Main.Tracker.combatStats.Values.Where(s => s.IsAlly).Sum(s => s.DamageTaken) : 0f;
            if (totalEncountersGroupDamage >= (partyLevel * 20) && stat.HealingDone >= (totalEncountersGroupDamage * 0.40f))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_39"), Localization.GetFormatted("ACH_DESC_39", stat), Color.white, 87));
            }

            // 40. Le pourfendeur du mal (CORRIGÉ : Vérification absolue via l'audit des modificateurs de dégâts d'Owlcat)
            // Le Châtiment du Mal (Smite Evil) est une capacité de classe du Paladin : on exige donc
            // à la fois la classe Paladin ET la trace réelle du modificateur dans l'audit des dégâts.
            bool isPaladin = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.ToLower().Contains("paladin"));
            bool actuallyUsedSmiteEvil = stat.DamageModifiersAudit.Values.Any(modDict =>
                modDict.Keys.Any(k => k.IndexOf("smite evil", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      k.IndexOf("châtiment du mal", StringComparison.OrdinalIgnoreCase) >= 0));

            if (isPaladin && actuallyUsedSmiteEvil && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 25))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_40"), Localization.GetFormatted("ACH_DESC_40", stat), Color.white, 89));
            }

            // 41. Le combattant à mains nues
            bool hasUnarmedDealt = stat.DamageBySource.Keys.Any(k => k.IndexOf("unarmed strike", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("mains nues", StringComparison.OrdinalIgnoreCase) >= 0);
            if (hasUnarmedDealt && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 20))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_41"), Localization.GetFormatted("ACH_DESC_41", stat), Color.white, 79));
            }

            // 42. Le maître du parchemin
            bool isScrollSavant = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.IndexOf("wizard", StringComparison.OrdinalIgnoreCase) >= 0 && c.Archetypes.Any(a => a.name.IndexOf("scrollsavant", StringComparison.OrdinalIgnoreCase) >= 0));
            if (isScrollSavant && stat.ScrollsCastCount >= 3)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_42"), Localization.GetFormatted("ACH_DESC_42", stat), Color.white, 91));
            }

            // 43. Terreur de Deskari
            bool isDeskariFight = Main.Tracker != null && Main.Tracker.combatStats.Values.Any(s => !s.IsAlly && s.CR > 30 && (s.Name.IndexOf("deskari", StringComparison.OrdinalIgnoreCase) >= 0 || (s.UnitData != null && s.UnitData.Blueprint.AssetGuid.ToString() == "5e4d2bfd4e92a54419d8a1a6fcda90e1")));
            if (isDeskariFight)
            {
                float totalGroupEffortDeskari = Main.Tracker.combatStats.Values.Where(s => s.IsAlly).Sum(s => s.TotalDamage + s.SummonDamage + s.HealingDone + s.VampiricHealing);
                float myContributionDeskari = stat.TotalDamage + stat.SummonDamage + stat.HealingDone + stat.VampiricHealing;
                if (totalGroupEffortDeskari > 0f && (myContributionDeskari / totalGroupEffortDeskari) >= 0.50f)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS", Localization.GetStringById("ACH_TITLE_43"), Localization.GetFormatted("ACH_DESC_43", stat), Color.white, 100));
                }
            }

            // 44. Le briseur de sorts suprême
            if (stat.DispelledCount >= 8)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_44"), Localization.GetFormatted("ACH_DESC_44", stat), Color.white, 93));
            }

            // 45. Performance physique exceptionnelle
            if (totalPhysical >= (partyLevel * 55))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_45"), Localization.GetFormatted("ACH_DESC_45", stat), Color.white, 61));
            }

            // 46. Dégâts d'énergie exceptionnels
            float energyDmg = stat.FireDmg + stat.ColdDmg + stat.AcidDmg + stat.ElectricDmg + stat.SonicDmg + stat.HolyDmg + stat.UnholyDmg + stat.NegativeDmg;
            if (energyDmg >= (partyLevel * 55))
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_46"), Localization.GetFormatted("ACH_DESC_46", stat), Color.white, 63));
            }

            // 47. Le maître des entraves
            if (totalCC >= 20)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_47"), Localization.GetFormatted("ACH_DESC_47", stat), Color.white, 65));
            }

            // 48. Le maître des bénédictions
            if (stat.SupportBuffsCast >= 40)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_48"), Localization.GetFormatted("ACH_DESC_48", stat), Color.white, 67));
            }

            // 49. Le soigneur d'élite
            if (stat.HealingDone >= (partyLevel * 30))
            {
                stat.Achievements.Add(new MVPAchievement("A", Localization.GetStringById("ACH_TITLE_49"), Localization.GetFormatted("ACH_DESC_49", stat), Color.white, 49));
            }

            // 50. Le coordinateur tactique
            if (stat.SupportBuffsCast >= 20 && totalCC >= 10)
            {
                stat.Achievements.Add(new MVPAchievement("S", Localization.GetStringById("ACH_TITLE_50"), Localization.GetFormatted("ACH_DESC_50", stat), Color.white, 99));
            }

            // === LOT DÉDIÉ AU STYLE À UNE MAIN (uniquement si le personnage manie réellement une arme
            // de mêlée à une main : aucune fausse attribution possible sur un archer, un lanceur, etc.) ===
            bool ohmFreeHand;
            string ohmCategory;
            if (WieldsOneHandedMelee(stat.UnitData, out ohmFreeHand, out ohmCategory))
            {
                GrantOneHandedAchievements(stat, partyLevel, ohmFreeHand, ohmCategory, totalPhysical);
            }
        }

        // Détection stricte du style "arme de mêlée à une main" : main directrice tenant une arme
        // ni à distance, ni à deux mains, ni naturelle, et non maintenue à deux mains.
        private static bool WieldsOneHandedMelee(UnitEntityData unit, out bool freeOffHand, out string catName)
        {
            freeOffHand = false;
            catName = "";
            try
            {
                var w = unit?.Body?.PrimaryHand?.MaybeWeapon;
                if (w == null || w.Blueprint == null) return false;
                var t = w.Blueprint.Type;
                if (t == null) return false;
                if (t.IsRanged || t.IsTwoHanded || t.IsNatural || t.IsUnarmed) return false;
                if (!t.IsOneHanded && !t.IsLight) return false;
                if (w.HoldInTwoHands) return false;

                catName = t.Category.ToString();
                var sec = unit.Body.SecondaryHand;
                bool hasShield = sec != null && sec.HasShield;
                bool hasSecondWeapon = sec != null && sec.HasWeapon;
                freeOffHand = !hasShield && !hasSecondWeapon;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Les 50 hauts faits du bretteur à une main (A+ jusqu'à SSS+). Le cap d'affichage (top 3 par poids)
        // garantit qu'on ne voit que les plus prestigieux réellement mérités : jamais d'overdose.
        private static void GrantOneHandedAchievements(UnitCombatStats stat, int pL, bool freeOffHand, string catName, long totalPhysical)
        {
            int td = stat.TotalDamage;
            int acc = stat.AttacksAttempted > 0 ? (int)((long)stat.AttacksLanded * 100 / stat.AttacksAttempted) : 0;
            int directed = stat.HitsPhysicalTaken + stat.AttacksDodged;
            bool isAldori = !string.IsNullOrEmpty(catName) && catName.IndexOf("dueling", StringComparison.OrdinalIgnoreCase) >= 0;
            string[] finesseCats = { "rapier", "estoc", "dagger", "shortsword", "kukri", "starknife", "sickle", "dueling", "scimitar" };
            bool isFinesse = false;
            if (!string.IsNullOrEmpty(catName))
            {
                foreach (var fc in finesseCats)
                {
                    if (catName.IndexOf(fc, StringComparison.OrdinalIgnoreCase) >= 0) { isFinesse = true; break; }
                }
            }

            Action<string, int, int> A = (tier, id, w) => stat.Achievements.Add(new MVPAchievement(tier, Localization.GetStringById("ACH_TITLE_" + id), Localization.GetFormatted("ACH_DESC_" + id, stat), Color.white, w));

            if (freeOffHand && td >= pL * 90 && stat.Crits >= 15 && stat.Kills >= 6 && stat.DamageTaken <= pL * 15) A("SSS+", 51, 100);
            if (stat.HighDangerKills >= 4 && stat.MaxSingleHit >= pL * 40) A("SSS+", 52, 99);
            if (stat.AttacksLanded >= 30 && stat.Crits >= 12 && acc >= 80) A("SSS", 53, 98);
            if (stat.AttacksDodged >= 20 && stat.Kills >= 5 && stat.DamageTaken <= pL * 20) A("SSS", 54, 97);
            if (td >= pL * 80 && stat.MaxBurstDamage >= pL * 45) A("SSS", 55, 96);
            if (acc >= 90 && stat.AttacksAttempted >= 20) A("SSS-", 56, 95);
            if (stat.Kills >= 6 && stat.MaxSingleHit >= pL * 30) A("SSS-", 57, 94);
            if (isAldori && stat.Crits >= 10 && stat.AttacksDodged >= 12) A("SSS-", 58, 93);
            if (freeOffHand && directed >= 25 && stat.DamageTaken <= pL * 18) A("SSS-", 59, 92);
            if (isFinesse && stat.Crits >= 10 && td >= pL * 50) A("SS+", 60, 91);
            if (stat.AoOs >= 8 && stat.Kills >= 3) A("SS+", 61, 90);
            if (stat.MaxSingleHit >= pL * 35) A("SS+", 62, 89);
            if (stat.AttacksLanded >= 25 && acc >= 75) A("SS", 63, 88);
            if (stat.AttacksDodged >= 15 && stat.DamageTaken <= pL * 25) A("SS", 64, 87);
            if (stat.HighDangerKills >= 2 && freeOffHand) A("SS", 65, 86);
            if (stat.Crits >= 10) A("SS-", 66, 85);
            if (isFinesse && stat.Crits >= 6 && acc >= 80) A("SS-", 67, 84);
            if (isAldori && stat.AttacksDodged >= 10) A("SS-", 68, 83);
            if (freeOffHand && td >= pL * 45) A("S+", 69, 82);
            if (stat.Kills >= 5) A("S+", 70, 81);
            if (stat.AttacksLanded >= 20) A("S+", 71, 80);
            if (stat.MaxSingleHit >= pL * 25) A("S", 72, 79);
            if (stat.AttacksDodged >= 12) A("S", 73, 78);
            if (isFinesse && td >= pL * 35) A("S", 74, 77);
            if (td >= pL * 45 && stat.Crits >= 6) A("S", 75, 76);
            if (stat.AoOs >= 6) A("S-", 76, 75);
            if (acc >= 80 && stat.AttacksAttempted >= 15) A("S-", 77, 74);
            if (isAldori && stat.Crits >= 5) A("S-", 78, 73);
            if (stat.Kills >= 4) A("S-", 79, 72);
            if (td >= pL * 30) A("A+", 80, 70);
            if (stat.Crits >= 5) A("A+", 81, 69);
            if (stat.AttacksDodged >= 8) A("A+", 82, 68);
            if (stat.MaxSingleHit >= pL * 20) A("A+", 83, 67);
            if (stat.Kills >= 3) A("A+", 84, 66);
            if (directed >= 15 && stat.DamageTaken <= pL * 20) A("A+", 85, 65);
            if (freeOffHand && stat.Crits >= 3) A("A+", 86, 64);
            if (stat.AttacksLanded >= 15) A("A+", 87, 63);
            if (isFinesse && stat.Crits >= 3) A("A+", 88, 62);
            if (totalPhysical >= pL * 50) A("S", 89, 78);
            if (stat.AttacksDodged >= 18) A("SS-", 90, 84);
            if (stat.Kills >= 4 && stat.MaxSingleHit >= pL * 25) A("S+", 91, 81);
            if (td >= pL * 70) A("SSS-", 92, 93);
            if (stat.HighDangerCrits >= 3) A("SS+", 93, 90);
            if (stat.DamageTaken == 0 && directed >= 15 && stat.Kills >= 3) A("SSS", 94, 97);
            if (stat.MaxBurstDamage >= pL * 35) A("SS", 95, 87);
            if (acc >= 70 && stat.AttacksAttempted >= 12) A("A+", 96, 61);
            if (stat.MaxSingleHit >= pL * 22) A("S-", 97, 73);
            if (td >= pL * 25 && stat.Crits >= 4) A("A+", 98, 60);
            if (stat.Kills >= 2 && freeOffHand) A("A+", 99, 58);
            if (isAldori && td >= pL * 30) A("A+", 100, 64);
        }
    }
}
