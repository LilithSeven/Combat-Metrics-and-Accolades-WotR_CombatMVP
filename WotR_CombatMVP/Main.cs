using Kingmaker.Items;
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

            try
            {
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

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

public class UnitCombatStats
    {
        public string Name;
        public UnitEntityData UnitData;
        public bool IsAlly;
        public int Level = 1;
        public int CR = 0;
        public string MythicPathName = "";
        public string MythicPathInternalName = ""; // Ajout du nom de classe interne	
        public bool IsEvil = false; 
        public Gender Gender = Gender.Male; 
        public int TotalDamage = 0;
        public int HealingDone = 0;
        public int StatDamage = 0;
        public int NegativeLevels = 0;
        public int Kills = 0;
		
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
            SummonKills > 0;
        
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
        public int SummonDamage = 0;
        public int SummonKills = 0;

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

        // --- NOUVEAU (PHASE DE DOMINATION) ---
        public bool IsDominatedSheet = false;

        // --- NOUVEAU : Cache de portrait ---
        public Texture2D CachedPortrait = null;
    }

    public class CombatMVP_UI : MonoBehaviour
    {
        public static CombatMVP_UI Instance;
        public bool showWindow = false;
        private Rect fullScreenRect;
        private Texture2D darkBackground;
        private List<UnitCombatStats> allCombatants = new List<UnitCombatStats>();
        private int currentPageIndex = 0;
        private Vector2 scrollPosition;

        // --- NOUVEAU : Le Mur de Verre ---
        private GameObject invisibleGlassWall;

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
            // AJOUT DU DOUBLE RACCOURCI IMMUNISÉ CONTRE LES REGLAGES OS : Alt + Space OU Alt + M
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.M)))
            {
                if (Main.Tracker == null || Main.Tracker.combatStats.Count == 0)
                {
                    Main.Logger.Log("[CombatMVP] Impossible d'ouvrir l'interface : aucun combat n'a encore ete enregistre dans cette zone.");
                    showWindow = false;
                    if (invisibleGlassWall != null) invisibleGlassWall.SetActive(false);
                    return; 
                }
                showWindow = !showWindow;
                EnsureUIInitialized();
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(showWindow);
                if (showWindow) RefreshPagination();
            }
            else if (showWindow && Input.GetKeyDown(KeyCode.Escape))
            {
                showWindow = false;
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(false);
            }
        }

        void RefreshPagination()
        {
            if (Main.Tracker == null) return;
            Localization.Init(Main.ModPath);
            
            var activeParty = Game.Instance.Player.PartyAndPets;
            
            // On filtre les alliés pour n'afficher que ceux qui ont réellement contribué au combat
            var allies = Main.Tracker.combatStats.Values
                .Where(s => s.IsAlly && s.TotalScore > 0 && s.HasRealContribution && s.UnitData != null && (s.IsDominatedSheet || activeParty.Contains(s.UnitData)))
                .OrderByDescending(s => s.TotalScore)
                .ToList();
                
            var enemies = Main.Tracker.combatStats.Values
                .Where(s => !s.IsAlly && s.TotalScore > 0 && s.HasRealContribution)
                .OrderByDescending(s => s.TotalScore)
                .Take(5)
                .ToList(); 

            if (enemies.Count < 3)
            {
                // Application du filtre sur le fallback des ennemis
                var fallbackEnemies = Main.Tracker.combatStats.Values
                    .Where(s => !s.IsAlly && !enemies.Contains(s) && s.HasRealContribution)
                    .OrderByDescending(s => s.DamageTaken)
                    .Take(3 - enemies.Count);
                enemies.AddRange(fallbackEnemies);
            }
            
            allCombatants = allies.Concat(enemies).ToList();
            currentPageIndex = 0;
            scrollPosition = Vector2.zero;
            
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
            var portrait = stat.UnitData.Portrait;
            Type type = portrait.GetType();
            string[] possibleNames = { "HalfLengthPortrait", "HalfLengthImage", "SmallPortrait", "FullLengthPortrait", "HalfPortrait" };
            foreach (var name in possibleNames)
            {
                var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    var val = prop.GetValue(portrait);
                    if (val is Sprite s && s.texture != null) return s.texture;
                    if (val is Texture2D t) return t;
                }
            }
            return null;
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
            if (!showWindow || allCombatants.Count == 0) return;
            EnsureUIInitialized();
            UnitCombatStats currentStat = allCombatants[currentPageIndex];
            
            fullScreenRect = new Rect(0, 0, Screen.width, Screen.height);
            Texture2D activeBg = GetDynamicBackground(currentStat);
            GUI.DrawTexture(fullScreenRect, activeBg);

            if (overlayTexture == null)
            {
                overlayTexture = new Texture2D(1, 1);
                overlayTexture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.03f, 0.75f)); 
                overlayTexture.Apply();
            }

            float marginX = Screen.width * 0.12f; 
            float marginY = Screen.height * 0.10f; 
            float safeWidth = Screen.width - (marginX * 2);
            float safeHeight = Screen.height - (marginY * 2);
            Rect safeArea = new Rect(marginX, marginY, safeWidth, safeHeight);

            GUI.DrawTexture(safeArea, overlayTexture);
            GUILayout.BeginArea(safeArea);
            DrawBookLayout(safeWidth, safeHeight);
            GUILayout.EndArea();
            DrawNavigationButtons();
        }

        private void DrawNavigationButtons()
        {
            GUIStyle navStyle = new GUIStyle(GUI.skin.button) { fontSize = 36, fontStyle = FontStyle.Bold };
            navStyle.normal.textColor = new Color(0.8f, 0.7f, 0.5f); 
            if (currentPageIndex > 0)
                if (GUI.Button(new Rect(30, Screen.height / 2 - 40, 50, 80), "<", navStyle)) { currentPageIndex--; scrollPosition = Vector2.zero; }
            if (currentPageIndex < allCombatants.Count - 1)
                if (GUI.Button(new Rect(Screen.width - 80, Screen.height / 2 - 40, 50, 80), ">", navStyle)) { currentPageIndex++; scrollPosition = Vector2.zero; }

            GUIStyle closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
            closeStyle.normal.textColor = new Color(0.9f, 0.2f, 0.2f);
            if (GUI.Button(new Rect(Screen.width - 70, 20, 50, 50), "X", closeStyle)) 
            {
                showWindow = false;
                if (invisibleGlassWall != null) invisibleGlassWall.SetActive(false);
            }
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

        void DrawBookLayout(float width, float height)
        {
            UnitCombatStats currentStat = allCombatants[currentPageIndex];
            string colTitle = "#C4A265"; 
            string colSub = "#8C8C8C"; 
            string colText = "#E2D5B5"; 
            string colDanger = "#D94C4C"; 
            
            GUIStyle sectionTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, richText = true };
            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true, margin = new RectOffset(0,0,4,4) };
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, wordWrap = true, margin = new RectOffset(15,0,0,8) };

            GUILayout.BeginHorizontal(); 

            // ================= ZONE GAUCHE (Identité & Portrait) =================
            float leftZoneWidth = width * 0.40f; 
            GUILayout.BeginVertical(GUILayout.Width(leftZoneWidth));
            GUILayout.Space(20);
            
            string allegiance = "";
            if (currentStat.IsAlly)
            {
                allegiance = currentPageIndex == 0 ? $"<color={colTitle}>{Localization.GetStringById("ui.mvp")}</color>" : $"<color=#65A2C4>{Localization.GetStringById("ui.ally_squad")}</color>";
                if (Main.Tracker != null)
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
                    // 1. Détection de la présence d'une Liche active au sein du groupe (alliés + familiers)
                    bool hasLichInParty = false;
                    try
                    {
                        foreach (var u in Kingmaker.Game.Instance.Player.PartyAndPets)
                        {
                            if (u.Progression == null) continue;
                            foreach (var cls in u.Progression.Classes)
                            {
                                if (cls.CharacterClass != null && cls.CharacterClass.IsMythic && (cls.CharacterClass.name ?? "").ToLower().Contains("lich") && cls.Level > 0)
                                {
                                    hasLichInParty = true;
                                    break;
                                }
                            }
                            if (hasLichInParty) break;
                        }
                    }
                    catch (Exception) { }

                    // 2. Détection du buff technique de "Repurpose" sur la créature
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

                    // 3. Détection de la nature de Mort-Vivant (Undead) via le nom technique du blueprint de type
                    bool isUndeadType = false;
                    try
                    {
                        string typeName = (currentStat.UnitData.Blueprint?.Type?.name ?? "").ToLower();
                        if (typeName.Contains("undead"))
                        {
                            isUndeadType = true;
                        }
                        else if (currentStat.UnitData.Descriptor?.Progression != null)
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

                    // La signature d'un relevage de la Liche :
                    // La créature est un mort-vivant, elle porte le buff de Repurpose et une Liche est dans l'équipe.
                    if (hasRepurposeBuff && hasLichInParty && isUndeadType)
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
            GUILayout.Label($"<b><color={colText}>{displayName}</color></b>", new GUIStyle(GUI.skin.label) { fontSize = 42, richText = true });

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

            int totalCC_Spec = currentStat.CC_Paralyzed + currentStat.CC_Stunned + currentStat.CC_Frightened + currentStat.CC_Nauseated + currentStat.CC_Confused + currentStat.CC_Blinded + currentStat.CC_Prone + currentStat.CC_Entangled + currentStat.CC_Exhausted + currentStat.CC_Fatigued + currentStat.CC_Shaken + currentStat.CC_Sickened + currentStat.CC_Asleep + currentStat.CC_Petrified + currentStat.CC_Slowed + currentStat.CC_Staggered + currentStat.CC_Dazed + currentStat.CC_Dazzled + currentStat.CC_Helpless + currentStat.CC_Cowering + currentStat.CC_DeathsDoor;
            string specialtyKey = "SPEC_POLYVALENT";
            if (currentStat.FriendlyFireDmg > currentStat.TotalDamage && currentStat.FriendlyFireDmg > 0) specialtyKey = "SPEC_DANGER_PUBLIC";
            else if (currentStat.TotalDamage == 0 && currentStat.HealingDone == 0 && currentStat.DamageTaken == 0 && currentStat.SummonDamage == 0 && currentStat.AttacksDodged == 0 && totalCC_Spec == 0 && currentStat.Kills == 0 && currentStat.InstaKills == 0) specialtyKey = "SPEC_SPECTATEUR_VIP";
            else if (currentStat.DamageTaken > currentStat.TotalDamage * 5f && currentStat.TotalDamage < 20 && totalCC_Spec < 5 && currentStat.InstaKills == 0 && currentStat.Kills == 0) specialtyKey = "SPEC_PUNCHING_BALL";
            else if (currentStat.FireDmg > 0 && currentStat.ColdDmg > 0 && currentStat.ElectricDmg > 0 && currentStat.AcidDmg > 0) specialtyKey = "SPEC_AVATAR_ELEMENTS";
            else if (currentStat.NegativeDmg > currentStat.TotalDamage * 0.4f && currentStat.NegativeLevels >= 3 && currentStat.DamageTaken > 0) specialtyKey = "SPEC_NECROMANCIEN_FRONT";
            else if (currentStat.HolyDmg > currentStat.TotalDamage * 0.3f && currentStat.HealingDone > 0 && currentStat.DamageTaken > currentStat.TotalDamage * 0.5f) specialtyKey = "SPEC_PALADIN_MARTYR";
            else if (currentStat.Crits >= 5 && currentStat.AoOs >= 5) specialtyKey = "SPEC_CHIRURGIEN_MARTIAL";
            else if (currentStat.CC_Frightened + currentStat.CC_Cowering + currentStat.CC_Shaken >= 6) specialtyKey = "SPEC_SEIGNEUR_CAUCHEMAR";
            else if (currentStat.CC_Confused >= 4) specialtyKey = "SPEC_MAITRE_PANDEMONIUM";
            else if (currentStat.CC_Paralyzed + currentStat.CC_Stunned + currentStat.CC_Petrified >= 4) specialtyKey = "SPEC_REGARD_MEDUSEEN";
            else if (currentStat.CC_Prone >= 5) specialtyKey = "SPEC_CASSEUR_GENOUX";
            else if (currentStat.CC_Nauseated + currentStat.CC_Sickened >= 4) specialtyKey = "SPEC_VECTEUR_PESTE";
            else if (currentStat.CC_Blinded >= 3) specialtyKey = "SPEC_VOLEUR_LUMIERE";
            else if (currentStat.CC_Asleep >= 3) specialtyKey = "SPEC_MARCHAND_SABLE";
            else if (currentStat.CC_Slowed + currentStat.CC_Staggered >= 4) specialtyKey = "SPEC_DICTATEUR_TEMPOREL";
            else if (currentStat.StatDamage >= 15) specialtyKey = "SPEC_SIPHONNEUR_ESSENCE";
            else if (currentStat.NegativeLevels >= 5) specialtyKey = "SPEC_DEVOREUR_AMES";
            else if (currentStat.CC_Frightened + currentStat.CC_Shaken + currentStat.CC_Cowering >= 3) specialtyKey = "SPEC_FLEAU_PSYCHOLOGIQUE";
            else if (totalCC_Spec >= 10) specialtyKey = "SPEC_MAITRE_ENTRAVE";
            else if (currentStat.SummonDamage > currentStat.TotalDamage * 2f) specialtyKey = "SPEC_GENERAL_OUTRE_PLAN";
            else if (currentStat.SummonDamage > currentStat.TotalDamage) specialtyKey = "SPEC_MAITRE_INVOCATEUR";
            else if (currentStat.HealingDone > currentStat.TotalDamage * 2f) specialtyKey = "SPEC_SAUVEUR_MIRACULEUX";
            else if (currentStat.SupportBuffsCast >= 12 && currentStat.TotalDamage < 50) specialtyKey = "SPEC_EGIDE_BIENVEILLANTE";
            else if (currentStat.HealingDone > currentStat.TotalDamage) specialtyKey = "SPEC_MEDECIN_TERRAIN";
            else if (currentStat.VampiricHealing > currentStat.TotalDamage * 0.5f) specialtyKey = "SPEC_SEIGNEUR_SANGSUE";
            else if (currentStat.AttacksDodged >= 15 && currentStat.DamageTaken == 0) specialtyKey = "SPEC_FANTOME_INTOUCHABLE";
            else if (currentStat.AttacksDodged >= 8) specialtyKey = "SPEC_DANSEUR_OMBRE";
            else if (currentStat.DamageTaken >= currentStat.TotalDamage * 2f && currentStat.DamageTaken > 20) specialtyKey = "SPEC_EGIDE_TANKING";
            else if (currentStat.DamageTaken >= currentStat.TotalDamage && currentStat.DamageTaken > 10) specialtyKey = "SPEC_MUR_CHAIR";
            else if (currentStat.TotalDamage > 0 && currentStat.SlashingDmg == 0 && currentStat.PiercingDmg == 0 && currentStat.BludgeoningDmg == 0) specialtyKey = "SPEC_ANOMALIE_ARCANIQUE"; 
            else if (currentStat.FireDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_PYROMANCIEN";
            else if (currentStat.ColdDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_CRYOMANCIEN";
            else if (currentStat.ElectricDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_SEIGNEUR_FOUDRE";
            else if (currentStat.AcidDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_ALCHIMISTE_CORROSIF";
            else if (currentStat.SonicDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_MAITRE_ONDES";
            else if (currentStat.HolyDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_INQUISITEUR_DIVIN";
            else if (currentStat.UnholyDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_APOTRE_SOMBRE";
            else if (currentStat.NegativeDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_CANALISATEUR_ENTROPIQUE";
            else if (currentStat.Crits >= 8) specialtyKey = "SPEC_EXECUTEUR_PRECIS";
            else if (currentStat.AoOs >= 8) specialtyKey = "SPEC_HERISSON_LETAL";
            else if (currentStat.SlashingDmg > 0 && currentStat.PiercingDmg > 0 && currentStat.BludgeoningDmg > 0) specialtyKey = "SPEC_MAITRE_ARMES";
            else if (currentStat.PiercingDmg > currentStat.TotalDamage * 0.6f && currentStat.DamageTaken == 0) specialtyKey = "SPEC_SNIPER_INTOUCHABLE";
            else if (currentStat.PiercingDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_ARTILLEUR_LOURD";
            else if (currentStat.SlashingDmg > currentStat.TotalDamage * 0.6f && currentStat.Kills >= 4) specialtyKey = "SPEC_DECAPITEUR";
            else if (currentStat.SlashingDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_MAITRE_LAME";
            else if (currentStat.BludgeoningDmg > currentStat.TotalDamage * 0.6f && currentStat.CC_Prone >= 1) specialtyKey = "SPEC_BROYEUR_OS";
            else if (currentStat.BludgeoningDmg > currentStat.TotalDamage * 0.6f) specialtyKey = "SPEC_MASSE_HUMAINE";
            else if (currentStat.OverkillDmg > currentStat.TotalDamage * 0.5f) specialtyKey = "SPEC_BOUCHER_SANGUINAIRE";
            else if (currentStat.MaxSingleHit > currentStat.TotalDamage * 0.8f && currentStat.TotalDamage > 0) specialtyKey = "SPEC_ONE_PUNCH";
            else if (currentStat.Kills >= 6) specialtyKey = "SPEC_ANGE_MORT";
            else if (currentStat.AoOs >= 4) specialtyKey = "SPEC_OPPORTUNISTE";

            string specialtyName = Localization.GetStringById(specialtyKey) ?? "Combattant Polyvalent";
            if (specialtyKey == "SPEC_DANGER_PUBLIC") specialtyName = $"<color={colDanger}>{specialtyName}</color>";
            else if (specialtyKey == "SPEC_SPECTATEUR_VIP") specialtyName = $"<color={colSub}>{specialtyName}</color>";
            else if (specialtyKey == "SPEC_PUNCHING_BALL") specialtyName = $"<color=#8b4513>{specialtyName}</color>";
            GUILayout.Label($"<size=20><color={colText}>{Localization.GetStringById("ui.specialty_title")}</color><color={colTitle}>{specialtyName}</color></size>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(20);

            Texture2D portraitTex = currentStat.CachedPortrait;
            if (portraitTex != null)
            {
                float ratio = (float)portraitTex.width / portraitTex.height;
                GUILayout.Box(portraitTex, GUILayout.Width(300), GUILayout.Height(300 / ratio));
            }
            else 
            {
                GUILayout.Box(Localization.GetStringById("ui.unknown_face") ?? "Visage Inconnu", GUILayout.Width(300), GUILayout.Height(400));
            }
            GUILayout.Space(20);

            GUILayout.Label(Localization.GetStringById("ui.operational_rank") ?? "RANG OPÉRATIONNEL", new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });
            GUILayout.Label($"{currentStat.Grade}", new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, normal = { textColor = GetGradeColor(currentStat.Grade) } });
            GUILayout.EndVertical();

            // ================= ZONE DROITE (Stats Détaillées & Succès) =================
            float rightZoneWidth = width * 0.58f; 
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
            GUILayout.Label(string.Format(Localization.GetStringById("ui.damage_done") ?? "⚔️ <b>Dégâts Infligés :</b> <color={0}>{1}</color>", colText, currentStat.TotalDamage), statStyle);
            
            string overkillText = currentStat.OverkillDmg > 0 ? string.Format(Localization.GetStringById("ui.overkill_text") ?? " <color={0}><i>(+{1} Overkill)</i></color>", colDanger, currentStat.OverkillDmg) : "";
            string instaKillText = currentStat.InstaKills > 0 ? string.Format(Localization.GetStringById("ui.instakills_text") ?? " <color=#B959FF><i>(dont {0} Insta-Kills 🔮)</i></color>", currentStat.InstaKills) : "";
            GUILayout.Label(string.Format(Localization.GetStringById("ui.kills_title") ?? "💀 <b>Éliminations :</b> <color={0}>{1}</color>{2}{3}", colText, currentStat.Kills, overkillText, instaKillText), statStyle);
            GUILayout.Label(string.Format(Localization.GetStringById("ui.crits_aoo") ?? "🎯 <b>Critiques :</b> <color={0}>{1}</color> | <b>AoO :</b> <color={0}>{2}</color>", colText, currentStat.Crits, currentStat.AoOs), statStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(rightZoneWidth * 0.48f));
            GUILayout.Label(string.Format(Localization.GetStringById("ui.damage_taken") ?? "🛡️ <b>Dégâts Subis :</b> <color={0}>{1}</color>", colText, currentStat.DamageTaken), statStyle);
            GUILayout.Label(string.Format(Localization.GetStringById("ui.attacks_dodged") ?? "🍃 <b>Esquives :</b> <color={0}>{1}</color>", colText, currentStat.AttacksDodged), statStyle);
            GUILayout.Label(string.Format(Localization.GetStringById("ui.healing_done") ?? "👼 <b>Soins :</b> <color={0}>{1}</color>", colText, currentStat.HealingDone + currentStat.VampiricHealing), statStyle);
            if (currentStat.SupportBuffsCast > 0)
            {
                GUILayout.Label(string.Format(Localization.GetStringById("ui.support_cast") ?? "✨ <b>Soutiens lancés :</b> <color={0}>{1}</color>", colText, currentStat.SupportBuffsCast), statStyle);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            if (currentStat.FriendlyFireDmg > 0)
            {
                GUILayout.Label(string.Format(Localization.GetStringById("ui.friendly_fire_warn") ?? "⚠️ <b><color={0}>Tir Allié (Dégâts infligés au groupe) : {1}</color></b>", colDanger, currentStat.FriendlyFireDmg), statStyle);
            }

            string dmgDetails = "";
            if (currentStat.SlashingDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.slashing")} ({currentStat.SlashingDmg}) | ";
            if (currentStat.PiercingDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.piercing")} ({currentStat.PiercingDmg}) | ";
            if (currentStat.BludgeoningDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.bludgeoning")} ({currentStat.BludgeoningDmg}) | ";
            if (currentStat.SlashPierceDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.slash_pierce")} ({currentStat.SlashPierceDmg}) | ";
            if (currentStat.SlashBludgeonDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.slash_bludgeon")} ({currentStat.SlashBludgeonDmg}) | ";
            if (currentStat.PierceBludgeonDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.pierce_bludgeon")} ({currentStat.PierceBludgeonDmg}) | ";
            if (currentStat.AllPhysDmg > 0) dmgDetails += $"{Localization.GetStringById("ui.dmg.all_phys")} ({currentStat.AllPhysDmg}) | ";
            if (currentStat.FireDmg > 0) dmgDetails += $"<color=#ff6600>{Localization.GetStringById("ui.dmg.fire")} ({currentStat.FireDmg})</color> | ";
            if (currentStat.ColdDmg > 0) dmgDetails += $"<color=#66ccff>{Localization.GetStringById("ui.dmg.cold")} ({currentStat.ColdDmg})</color> | ";
            if (currentStat.AcidDmg > 0) dmgDetails += $"<color=#33cc33>{Localization.GetStringById("ui.dmg.acid")} ({currentStat.AcidDmg})</color> | ";
            if (currentStat.ElectricDmg > 0) dmgDetails += $"<color=#ffff00>{Localization.GetStringById("ui.dmg.electricity")} ({currentStat.ElectricDmg})</color> | ";
            if (currentStat.SonicDmg > 0) dmgDetails += $"<color=#cc99ff>{Localization.GetStringById("ui.dmg.sonic")} ({currentStat.SonicDmg})</color> | ";
            if (currentStat.NegativeDmg > 0) dmgDetails += $"<color=#800080>{Localization.GetStringById("ui.dmg.negative")} ({currentStat.NegativeDmg})</color> | ";
            if (currentStat.HolyDmg > 0) dmgDetails += $"<color=#ffff66>{Localization.GetStringById("ui.dmg.holy")} ({currentStat.HolyDmg})</color> | ";
            if (currentStat.UnholyDmg > 0) dmgDetails += $"<color=#8b0000>{Localization.GetStringById("ui.dmg.unholy")} ({currentStat.UnholyDmg})</color> | ";

            int classifiedDamage = currentStat.SlashingDmg + currentStat.PiercingDmg + currentStat.BludgeoningDmg + 
                                   currentStat.SlashPierceDmg + currentStat.SlashBludgeonDmg + currentStat.PierceBludgeonDmg + currentStat.AllPhysDmg +
                                   currentStat.FireDmg + currentStat.ColdDmg + currentStat.AcidDmg + 
                                   currentStat.ElectricDmg + currentStat.SonicDmg + currentStat.NegativeDmg + 
                                   currentStat.HolyDmg + currentStat.UnholyDmg;

            int untypedDamage = currentStat.TotalDamage - classifiedDamage;
            if (untypedDamage > 0) dmgDetails += $"<color=#cccccc>{Localization.GetStringById("ui.dmg.other")} ({untypedDamage})</color> | ";
            if (dmgDetails.Length > 0) dmgDetails = dmgDetails.Substring(0, dmgDetails.Length - 3); 

            if (currentStat.TotalDamage > 0)
            {
                if (currentStat.SneakAttackDmg > 0) 
                {
                    GUILayout.Label(string.Format(Localization.GetStringById("ui.dmg.sneak_bonus") ?? "<color=#ffcc00><i>(dont {0} en Attaque Sournoise)</i></color>", currentStat.SneakAttackDmg), detailStyle);
                }
                if (dmgDetails.Length > 0) 
                {
                    GUILayout.Label(string.Format(Localization.GetStringById("ui.dmg.details") ?? "<color={0}><i>Détails : {1}</i></color>", colSub, dmgDetails), detailStyle);
                }
                GUILayout.Label(string.Format(Localization.GetStringById("ui.dmg.biggest_hit") ?? "<color={0}><i>Plus gros coup : {1}</i></color>", colSub, currentStat.MaxSingleHit), detailStyle);
                GUILayout.Space(10);
            }

            if (currentStat.HealingDone > 0 || currentStat.VampiricHealing > 0)
            {
                string healDetails = "";
                if (currentStat.HealingDone > 0) healDetails += $"{Localization.GetStringById("ui.heal.spells")} ({currentStat.HealingDone}) | ";
                if (currentStat.VampiricHealing > 0) healDetails += $"<color=#cc0000>{Localization.GetStringById("ui.heal.vampiric")} ({currentStat.VampiricHealing})</color> | ";
                if (healDetails.Length > 0) healDetails = healDetails.Substring(0, healDetails.Length - 3);
                GUILayout.Label(string.Format(Localization.GetStringById("ui.heal.details") ?? "<color={0}><i>Détails soins : {1}</i></color>", colSub, healDetails), detailStyle);
                GUILayout.Space(10);
            }

            int totalCC = currentStat.CC_Prone + currentStat.CC_Paralyzed + currentStat.CC_Stunned + currentStat.CC_Frightened + currentStat.CC_Shaken + currentStat.CC_Cowering + currentStat.CC_Nauseated + currentStat.CC_Sickened + currentStat.CC_Blinded + currentStat.CC_Entangled + currentStat.CC_Confused + currentStat.CC_Exhausted + currentStat.CC_Fatigued + currentStat.CC_Slowed + currentStat.CC_Staggered + currentStat.CC_Petrified + currentStat.CC_Asleep + currentStat.CC_Dazed + currentStat.CC_Dazzled + currentStat.CC_Helpless + currentStat.CC_DeathsDoor;
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
                GUILayout.Label(string.Format(Localization.GetStringById("ui.cc.total_applied") ?? "⛓️ <b>Entraves (CC) Appliquées : {0}</b>", totalCC), statStyle);
                GUILayout.Label(string.Format(Localization.GetStringById("ui.cc.details_label") ?? "<color={0}><i>Détails : {1}</i></color>", colSub, ccDetails), detailStyle);
                GUILayout.Space(10);
            }

            if (currentStat.StatDamage > 0 || currentStat.NegativeLevels > 0)
            {
                GUILayout.Label(string.Format(Localization.GetStringById("ui.misc.stat_dmg") ?? "🩸 <b>Dégâts Carac. :</b> <color={0}>{1}</color> | <b>Niveaux Drainés :</b> <color={0}>{2}</color>", colText, currentStat.StatDamage, currentStat.NegativeLevels), statStyle);
            }
            if (currentStat.IconicSpellsCast > 0)
            {
                GUILayout.Label(string.Format(Localization.GetStringById("ui.misc.iconic_spells") ?? "🔮 <b>Sorts de Mort/Terreur lancés :</b> <color={0}>{1}</color>", colText, currentStat.IconicSpellsCast), statStyle);
            }
            if (currentStat.SummonDamage > 0 || currentStat.SummonKills > 0)
            {
                GUILayout.Label(string.Format(Localization.GetStringById("ui.misc.summons_report") ?? "🦴 <b>Apport des Invocations :</b> <color={0}>{1} dégâts | {2} exécutions</color>", colText, currentStat.SummonDamage, currentStat.SummonKills), statStyle);
            }
            GUILayout.Space(25);

            string achTitleKey = currentStat.MythicPathName == (Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique") || string.IsNullOrEmpty(currentStat.MythicPathName) ? "ui.misc.heroic_ach" : "ui.misc.mythic_ach";
            GUILayout.Label($"<color={colTitle}>{Localization.GetStringById(achTitleKey)}</color>", sectionTitleStyle);
            GUILayout.Space(15);

            var topAchievements = currentStat.Achievements.OrderByDescending(a => a.Weight).Take(15).ToList();
            if (topAchievements.Count == 0)
            {
                GUILayout.Label($"<color={colSub}><i>{Localization.GetStringById("ui.misc.no_feats") ?? "Aucun fait d'armes marquant durant cet engagement."}</i></color>", detailStyle);
            }
            else
            {
                foreach (var ach in topAchievements)
                {
                    GUILayout.Label($"<size=24><b><color={ColorToHex(ach.TitleColor)}>[{ach.Tier}] {ach.Title}</color></b></size>", new GUIStyle(GUI.skin.label) { richText = true });
                    GUILayout.Label($"<size=18><color={colText}><i>\"{ach.FlavorText}\"</i></color></size>", detailStyle);
                    GUILayout.Space(15);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal(); 
        }

        string ColorToHex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

        Color GetGradeColor(string grade)
        {
            if (grade.Contains("S")) return new Color(1f, 0.8f, 0f); 
            if (grade.Contains("A")) return new Color(0.2f, 1f, 0.2f);
            if (grade.Contains("B")) return Color.yellow;
            if (grade.Contains("C")) return new Color(0.8f, 0.5f, 0.2f);
            if (grade.Contains("F")) return new Color(1f, 0.2f, 0.2f);
            if (grade.Contains("R")) return new Color(0.58f, 0.65f, 0.75f); 
            return Color.white;
        }
    }
	public class CombatTracker : 
        IPartyCombatHandler, IGlobalSubscriber, 
        IGlobalRulebookHandler<RuleDealDamage>, IGlobalRulebookHandler<RuleHealDamage>, 
        IGlobalRulebookHandler<RuleDealStatDamage>, IGlobalRulebookHandler<RuleAttackRoll>, 
        IGlobalRulebookHandler<RuleDrainEnergy>, IGlobalRulebookHandler<RuleCastSpell>,
		IGlobalRulebookHandler<RuleSavingThrow>, 
        IUnitBuffHandler,
        IAreaHandler // Écoute active des chargements et transitions de cartes
    {
        private bool isCombatActive = false;
        public Dictionary<string, UnitCombatStats> combatStats = new Dictionary<string, UnitCombatStats>();
        private HashSet<string> deadUnitsThisCombat = new HashSet<string>(); 
        public HashSet<string> factionSwappedUnitsThisCombat = new HashSet<string>();
        private int totalEnemyHPAtStart = 1; 
        private float totalEnemyCombatWeight = 1f;
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

		private static string ExtractMythicPath(UnitEntityData unit, out string internalName)
        {
            internalName = "MythicHero";
            if (unit?.Progression == null || unit.Progression.MythicLevel == 0) 
                return Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique";

            var mythicClasses = unit.Progression.Classes
                .Where(cls => cls.CharacterClass.IsMythic && cls.Level > 0)
                .ToList();

            if (mythicClasses.Count == 0)
                return Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique";

            // On cherche en priorité une classe mythique qui n'est pas l'une des deux classes génériques de départ
            var specializedClass = mythicClasses.FirstOrDefault(cls => 
                !cls.CharacterClass.name.Equals("MythicHeroClass", StringComparison.OrdinalIgnoreCase) &&
                !cls.CharacterClass.name.Equals("MythicCompanionClass", StringComparison.OrdinalIgnoreCase));

            if (specializedClass != null)
            {
                internalName = specializedClass.CharacterClass.name;
                return specializedClass.CharacterClass.LocalizedName.ToString();
            }

            internalName = mythicClasses[0].CharacterClass.name;
            return mythicClasses[0].CharacterClass.LocalizedName.ToString();
        }

        private UnitCombatStats GetOrAddStats(UnitEntityData unit, out bool isSummonAction)
        {
            isSummonAction = false;
            if (unit == null) return null;
            UnitEntityData trueInitiator = GetTrueInitiator(unit, out isSummonAction);
            if (trueInitiator == null) return null;

            bool originallyAlly = Game.Instance.Player.PartyAndPets.Contains(trueInitiator);
            bool currentlyAlly = trueInitiator.IsPlayerFaction;
            bool isSwapped = originallyAlly != currentlyAlly;

            if (isSwapped)
            {
                factionSwappedUnitsThisCombat.Add(trueInitiator.UniqueId);
            }

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
                bool currentIsAlly = trueInitiator.IsPlayerFaction;

                combatStats[statsKey] = new UnitCombatStats { 
                    Name = (trueInitiator.IsPet) ? $"{name}{Localization.GetStringById("ui.pet_suffix") ?? " (Familier)"}" : name,
                    UnitData = trueInitiator,
                    IsAlly = currentIsAlly,
                    IsDominatedSheet = isSwapped,
                    Level = trueInitiator.Progression?.CharacterLevel ?? 1,
                    CR = trueInitiator.Blueprint?.CR ?? 1, 
                    MythicPathName = mythic,
                    MythicPathInternalName = mythicInternal, // Enregistrement du nom de classe interne
                    IsEvil = isEvil,
                    Gender = trueInitiator.Gender
                };
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
                    combatStats.Clear();
                    deadUnitsThisCombat.Clear();
                    doomedTargets.Clear();
                    factionSwappedUnitsThisCombat.Clear();
                    if (CombatMVP_UI.Instance != null) CombatMVP_UI.Instance.showWindow = false;
                    totalEnemyHPAtStart = 0;
                    totalEnemyCombatWeight = 0f; 
                    var mainChar = Game.Instance.Player.MainCharacter.Value;
                    if (mainChar != null && mainChar.Progression != null)
                    {
                        currentPartyLevel = mainChar.Progression.CharacterLevel;
                    }
                    if (currentPartyLevel <= 0) currentPartyLevel = 1; 

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
                        if (unit.IsEnemy(Game.Instance.Player.MainCharacter.Value) && !unit.Descriptor.State.IsDead)
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

                foreach (var stat in combatStats.Values.Where(s => s.IsAlly))
                {
                    totalTeamDamage += stat.TotalDamage + stat.SummonDamage;
                    totalTeamHealing += stat.HealingDone + stat.VampiricHealing;
                    totalTeamCC += stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Nauseated + stat.CC_Confused + stat.CC_Blinded + stat.CC_Prone + stat.CC_Entangled + stat.CC_Exhausted + stat.CC_Fatigued + stat.CC_Shaken + stat.CC_Sickened + stat.CC_Asleep + stat.CC_Petrified + stat.CC_Slowed + stat.CC_Staggered + stat.CC_Dazed + stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;
                }

                if (totalTeamDamage <= 0) totalTeamDamage = 1f;
                if (totalTeamHealing <= 0) totalTeamHealing = 1f;
                if (totalTeamCC <= 0) totalTeamCC = 1;

                int activeAllies = combatStats.Values.Count(s => s.IsAlly);
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

                        float staticScore = (stat.WeightedKills * 18f) + 
                                            (stat.StatDamage * 8f) + 
                                            (stat.NegativeLevels * 15f) + 
                                            (stat.Crits * 5f) + 
                                            (stat.WeightedCCs * 4f) + 
                                            supportContribution; 

                        stat.TotalScore = damageRatioScore + healingRatioScore + ccRatioScore + staticScore;
                    }
                    else
                    {
                        // SCORING ENNEMI : Ratio de menace de l'ennemi par rapport à la puissance du groupe (CR / Niveau)
                        float enemyCR = Math.Max(1f, stat.CR);
                        float partyLv = Math.Max(1f, currentPartyLevel);
                        float threatMultiplier = Mathf.Clamp(enemyCR / partyLv, 0.3f, 3.0f);

                        float painScore = myTotalDmg * 0.15f; 
                        float killScore = (stat.Kills + stat.SummonKills) * 150f; 
                        float ccScore = myTotalCC * 15f; 
                        float statDrainScore = (stat.StatDamage * 10f) + (stat.NegativeLevels * 25f); 

                        stat.TotalScore = (painScore + killScore + ccScore + statDrainScore) * threatMultiplier;
                    }

                    if (stat.IsAlly && stat.TotalScore > highestScore) 
                    {
                        highestScore = stat.TotalScore;
                        absoluteMVP = stat; 
                    }

                    // 3. ATTRIBUTION DES PALIERS
                    bool isMinorSkirmish = totalEnemyHPAtStart < (currentPartyLevel * 40);
                    if (stat.IsAlly && isMinorSkirmish && myTotalDmg == 0 && myTotalHeal == 0 && stat.DamageTaken == 0 && stat.AttacksDodged == 0 && stat.Kills == 0 && stat.InstaKills == 0)
                    {
                        stat.Grade = "R"; // Réserve Tactique (Aucune pénalité)
                    }
                    else
                    {
                        float tierScale = 1f + (currentPartyLevel * 0.02f);
                        if (stat.TotalScore >= 350f * tierScale) stat.Grade = "SSS+";
                        else if (stat.TotalScore >= 300f * tierScale) stat.Grade = "SSS";
                        else if (stat.TotalScore >= 260f * tierScale) stat.Grade = "SSS-";
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

                // CALCUL DE LA NOTE GLOBALE DE L'ÉQUIPE (AVEC PONDÉRATION DES PETS ET ÉCHELLE SSS+)
                var gradedAllies = combatStats.Values.Where(s => s.IsAlly && s.Grade != "R").ToList();
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
                Main.Logger.Log($"  -> Dégâts Totaux: {stat.TotalDamage} (Dégâts Pondérés pour le Score: {stat.WeightedDamageDone:F1})");
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
                Main.Logger.Log($"  -> Phys: {physLog}");
                Main.Logger.Log($"  -> Élem/Magie: {stat.FireDmg} Feu / {stat.ColdDmg} Froid / {stat.AcidDmg} Acide / {stat.ElectricDmg} Foudre / {stat.SonicDmg} Son | Sacré/Impie/Nég: {stat.HolyDmg}/{stat.UnholyDmg}/{stat.NegativeDmg}");
                Main.Logger.Log($"  -> Soins: {stat.HealingDone + stat.VampiricHealing} | Dégâts Subis: {stat.DamageTaken} | Esquives: {stat.AttacksDodged}");
                Main.Logger.Log($"  -> Kills: {stat.Kills + stat.SummonKills} (Kills Pondérés: {stat.WeightedKills:F1})");
                Main.Logger.Log($"  -> CC Pondéré: {stat.WeightedCCs:F1} | Soutien Pondéré: {stat.WeightedSupportBuffs:F1} | Tir Allié: {stat.FriendlyFireDmg} | Overkill: {stat.OverkillDmg}");
                if (stat.Achievements.Count > 0)
                {
                    Main.Logger.Log("  -> SUCCÈS DÉBLOQUÉS :");
                    foreach (var ach in stat.Achievements.OrderByDescending(a => a.Weight))
                    {
                        Main.Logger.Log($"      * [{ach.Tier}] {ach.Title}");
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
                if (!isCombatActive || evt.Initiator == null || evt.Target == null) return;
                int damage = evt.Result; 
                if (damage <= 0) return;

                // 1. TRACKING DU DÉFENSEUR (Tanking & Tir Allié)
                var targetStats = GetOrAddStats(evt.Target, out _);
                if (targetStats != null && targetStats.IsAlly) 
                {
                    targetStats.DamageTaken += damage;
                    if (evt.Initiator.IsPlayerFaction && evt.Initiator != evt.Target)
                    {
                        var initStats = GetOrAddStats(evt.Initiator, out _);
                        if (initStats != null) initStats.FriendlyFireDmg += damage;
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

                if (isSummon) 
                {
                    stats.SummonDamage += damage;
                    stats.WeightedDamageDone += (damage * multiplier) * 0.7f;
                }
                else 
                {
                    stats.TotalDamage += damage;
                    stats.WeightedDamageDone += damage * multiplier; 
                    if (damage > stats.MaxSingleHit) stats.MaxSingleHit = damage; 

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

                // VALIDATION DES KILLS (Et exécution du Couloir de la Mort)
                if (evt.Target.HPLeft <= 0 && !deadUnitsThisCombat.Contains(evt.Target.UniqueId))
                {
                    deadUnitsThisCombat.Add(evt.Target.UniqueId);
                    bool isHighDanger = evt.Target.Blueprint != null && evt.Target.Blueprint.CR >= currentPartyLevel;
                    if (isHighDanger)
                    {
                        stats.HighDangerKills++;
                    }

                    // Vérification du Couloir de la Mort (Insta-Kill)
                    if (doomedTargets.TryGetValue(evt.Target.UniqueId, out UnitEntityData executioner))
                    {
                        var execStats = GetOrAddStats(executioner, out _);
                        if (execStats != null)
                        {
                            execStats.InstaKills++; // LE KILL MAGIQUE EST VALIDÉ !
                            execStats.Kills++;
                            if (isHighDanger)
                            {
                                execStats.HighDangerInstaKills++;
                            }

                            // Boost de score : 50% de valeur en plus pour un instakill réussi sur une cible majeure
                            float instakillMultiplier = multiplier;
                            if (isHighDanger)
                            {
                                instakillMultiplier *= 1.5f;
                            }
                            execStats.WeightedKills += 1f * instakillMultiplier;
                        }
                        doomedTargets.Remove(evt.Target.UniqueId);
                    }
                    else // Kill Classique
                    {
                        if (isSummon) stats.SummonKills++; else stats.Kills++;
                        stats.WeightedKills += 1f * multiplier; 
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans OnEventDidTrigger(RuleDealDamage) : " + ex);
            }
        }
		public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            try
            {
                if (!isCombatActive) return;

                if (evt.Initiator != null)
                {
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null)
                    {
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

                if (evt.Target != null && !evt.IsHit)
                {
                    var targetStats = GetOrAddStats(evt.Target, out _);
                    if (targetStats != null && targetStats.IsAlly)
                    {
                        targetStats.AttacksDodged++;
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
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null)
                    {
                        stats.NegativeLevels += appliedLevels;

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
                                stats.Kills++;
                                float multiplier = GetTargetMultiplier(evt.Target);
                                stats.WeightedKills += 1.5f * multiplier; 
                                stats.InstaKills++;
                                stats.EnergyDrainKills++;

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
                    var statsAlly = GetOrAddStats(caster, out _);

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
                if (target == null || caster == null) return;

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
                var stats = GetOrAddStats(caster, out _);
                if (stats == null) return;
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
                if (!isCombatActive || evt.Initiator == null || evt.Spell?.Blueprint == null) return;
                string spellName = evt.Spell.Blueprint.name != null ? evt.Spell.Blueprint.name.ToLower() : "";
                if (string.IsNullOrEmpty(spellName)) return;

                if (spellName.Contains("weird") || spellName.Contains("phantasmalkiller") || spellName.Contains("absolutedeath"))
                {
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null) stats.IconicSpellsCast++;
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
                if (isCombatActive && evt.Initiator != null && evt.Value > 0) 
                {
                    var stats = GetOrAddStats(evt.Initiator, out _);
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
                        if (isVampiric) stats.VampiricHealing += evt.Value;
                        else stats.HealingDone += evt.Value;
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
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null)
                    {
                        stats.StatDamage += damageApplied;
                        if (evt.Stat.ModifiedValueRaw < 1 && (evt.Stat.ModifiedValueRaw + damageApplied) >= 1)
                        {
                            if (!deadUnitsThisCombat.Contains(evt.Target.UniqueId))
                            {
                                deadUnitsThisCombat.Add(evt.Target.UniqueId);
                                stats.Kills++;
                                float multiplier = GetTargetMultiplier(evt.Target);
                                stats.WeightedKills += 1.5f * multiplier; 
                                stats.InstaKills++; 
                                stats.StatDamageKills++;

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
                if (!isCombatActive || evt.Initiator == null || evt.Reason?.Context == null) return;
                var victim = evt.Initiator; 
                var attacker = evt.Reason.Context.MaybeCaster; 
                if (attacker == null || attacker == victim || attacker.IsPlayerFaction == victim.IsPlayerFaction) return;

                var ability = evt.Reason.Context.SourceAbility;
                if (ability == null || ability.name == null) return;

                bool isDeathSpell = ability.SpellDescriptor.HasFlag(SpellDescriptor.Death) || 
                                    ability.name.ToLower().Contains("phantasmalkiller") || 
                                    ability.name.ToLower().Contains("weird") || 
                                    ability.name.ToLower().Contains("absolutedeath") ||
                                    ability.name.ToLower().Contains("wailof");

                if (isDeathSpell && !evt.IsPassed)
                {
                    var stats = GetOrAddStats(attacker, out _);
                    if (stats != null)
                    {
                        stats.IconicSpellsCast++; 
                        doomedTargets[victim.UniqueId] = attacker;
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

        public void OnAreaDidLoad()
        {
            try
            {
                Localization.Init(Main.ModPath);
                if (combatStats != null)
                {
                    combatStats.Clear();
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
    } // FIN DU COMBAT TRACKER
	
	// =========================================================================
    // PARTIE 3 : L'ARCHIVE DES HAUTS FAITS (ACHIEVEMENT DATABASE) - V11 Lore-Friendly
    // =========================================================================
    public static class AchievementDatabase
    {
        public static void GrantAchievements(UnitCombatStats stat, float combatWeight, bool isAbsoluteMVP, string category)
        {
            // ROUTAGE VERS LES SUCCESS DÉDIÉS
            if (category == "Pet")
            {
                GrantPetAchievements(stat, combatWeight, isAbsoluteMVP);
                return;
            }
            if (category == "Enemy")
            {
                GrantEnemyAchievements(stat, combatWeight, isAbsoluteMVP);
                return;
            }

            string mythic = stat.MythicPathName.ToLower();
            int totalKills = stat.Kills + stat.SummonKills;

            // Gestion dynamique du genre et des accords grammaticaux
            string pronoun = stat.Gender == Gender.Female ? "Elle" : "Il";
            string pronounObj = stat.Gender == Gender.Female ? "elle" : "lui";
            string accord = stat.Gender == Gender.Female ? "e" : "";
            string titlePrefix = stat.Gender == Gender.Female ? "La " : "Le ";

            // Baseline d'effort pour l'équilibrage
            float baseline = Math.Max(5f, combatWeight / 8f); 

            // =========================================================
            // LECTURE DYNAMIQUE DE LA DIVINITÉ (Système Anti-RP Break)
            // =========================================================
            string defaultLore = Localization.GetStringById("ui.lore.own_will") ?? "sa propre volonté absolue";
            if (Kingmaker.Localization.LocalizationManager.CurrentLocale == Kingmaker.Localization.Shared.Locale.enGB && defaultLore.Contains("{pronounObj}"))
            {
                defaultLore = defaultLore.Replace("{pronounObj}", stat.Gender == Gender.Female ? "her" : "his");
            }
            string divineLore = defaultLore;
            string divineLoreSubject = Localization.GetStringById("ui.lore.void") ?? "Le néant";
            bool isAtheist = stat.IsEvil; 
            string patronName = Localization.GetStringById("ui.lore.cosmic_forces") ?? "les forces cosmiques";

            if (stat.UnitData?.Progression?.Features != null)
            {
                foreach (var feature in stat.UnitData.Progression.Features)
                {
                    if (feature.Blueprint == null) continue;
                    string bpName = feature.Blueprint.name.ToLower();

                    // 1. Détection de l'athéisme
                    if (bpName.Contains("atheism") || bpName.Contains("atheiste") || bpName.Contains("athee"))
                    {
                        isAtheist = true;
                        continue;
                    }

                    // 2. Détection de la divinité
                    bool isDeity = false;
                    // Étape A : Vérification par le groupe de fonctionnalités natif du jeu
                    if (feature.Blueprint is Kingmaker.Blueprints.Classes.BlueprintFeature bpFeature && bpFeature.Groups != null)
                    {
                        if (bpFeature.Groups.Contains(Kingmaker.Blueprints.Classes.FeatureGroup.Deities))
                        {
                            isDeity = true;
                        }
                    }

                    // Étape B : Détection de secours par mot-clé (si le respec a altéré le groupe)
                    if (!isDeity && bpName.Contains("deity") && !bpName.Contains("selection") && !bpName.Contains("domain") && !bpName.Contains("pool"))
                    {
                        isDeity = true;
                    }

                    // Étape C : Filtre anti-sélection et anti-placeholder
                    if (isDeity)
                    {
                        string displayName = feature.Name ?? "";
                        string displayNameLower = displayName.ToLower();
                        if (bpName.Contains("selection") || bpName.Contains("deityselection") || bpName.Contains("pool") || bpName.Contains("placeholder") || bpName.Contains("category")
                            || displayNameLower.Contains("choix") || displayNameLower.Contains("sélection") || displayNameLower.Contains("selection") || displayNameLower.Contains("divinité") || displayNameLower.Contains("divinite") || displayNameLower.Contains("deity") || bpName.Contains("warpriest") || bpName.Contains("sacredweapon"))
                        {
                            isDeity = false;
                        }
                    }

                    // 3. Extraction et nettoyage du nom de la divinité détectée
                    if (isDeity)
                    {
                        string displayName = feature.Name;
                        if (!string.IsNullOrEmpty(displayName) && displayName.Trim() != "?")
                        {
                            patronName = displayName;
                        }
                        else
                        {
                            string rawName = feature.Blueprint.name;
                            if (!string.IsNullOrEmpty(rawName))
                            {
                                if (rawName.EndsWith("Feature", StringComparison.OrdinalIgnoreCase))
                                {
                                    rawName = rawName.Substring(0, rawName.Length - 7);
                                }
                                if (rawName.EndsWith("Deity", StringComparison.OrdinalIgnoreCase))
                                {
                                    rawName = rawName.Substring(0, rawName.Length - 5);
                                }
                                if (rawName.StartsWith("Deity", StringComparison.OrdinalIgnoreCase))
                                {
                                    rawName = rawName.Substring(5);
                                }
                                patronName = rawName;
                            }
                        }
                    }
                }
            }

            // Textes adaptatifs selon la foi du personnage (avec priorité à l'athéisme)
            divineLore = isAtheist ? defaultLore : patronName;
            divineLoreSubject = isAtheist ? (Localization.GetStringById("ui.lore.void") ?? "Le néant") : patronName;

            // =========================================================
            // APPELS DES SOUS-MÉTHODES DE SUCCÈS (CS0841 Résolu)
            // =========================================================
            // APPEL DES 50 SUCCÈS DE DIFFICULTÉ
            GrantDifficultyAchievements(stat, baseline, isAbsoluteMVP);

            // APPEL DES 100 SUCCÈS MYTHIQUES DÉDIÉS
            GrantMythicPathAchievements(stat, baseline, isAbsoluteMVP, divineLore, divineLoreSubject);

            // APPEL DES 100 SUCCÈS DE CLASSES DÉDIÉS
            GrantClassAchievements(stat, baseline, isAbsoluteMVP, divineLore, divineLoreSubject);

            // APPEL DES SUCCÈS DE DANGEROSITÉ ÉLEVÉE (NÉMÉSIS)
            GrantHighDangerAchievements(stat, baseline, isAbsoluteMVP, divineLore, divineLoreSubject);

            // =========================================================
            // LE VERROU DE COHÉRENCE (GRADE LOCK)
            // =========================================================
            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;
            bool canGetB   = stat.Grade.StartsWith("B") || canGetA;
            bool canGetC   = stat.Grade.StartsWith("C") || canGetB; 

            // =========================================================
            // DÉTECTION DU STYLE DE COMBAT
            // =========================================================
            bool isPureRanged = stat.PiercingDmg > 0 && stat.SlashingDmg == 0 && stat.BludgeoningDmg == 0 && stat.DamageTaken == 0;
            bool isPureMelee = (stat.SlashingDmg > 0 || stat.BludgeoningDmg > 0) && stat.DamageTaken > 0;
            bool isPureCaster = stat.TotalDamage > 0 && stat.AllPhysDmg == 0 && stat.SlashingDmg == 0 && stat.PiercingDmg == 0 && stat.BludgeoningDmg == 0;

            // Variables de verrouillage pour empêcher les doublons
            bool gotDamageTitle = false;
            bool gotKillTitle = false;
            bool gotStyleTitle = false;
            bool gotCCTitle = false;

            // =========================================================
            // 1. LE MVP ABSOLU (AVEC SEUIL DE SÉCURITÉ SS+)
            // =========================================================
            if (isAbsoluteMVP)
            {
                bool isEligibleForChampion = stat.Grade == "SS+" || stat.Grade.StartsWith("SSS");
                if (isEligibleForChampion)
                {
                    string titleKey = stat.IsEvil ? "ACH_GEN_CHAMPION_EVIL_TITLE" : "ACH_GEN_CHAMPION_GOOD_TITLE";
                    string descKey = stat.IsEvil ? "ACH_GEN_CHAMPION_EVIL_DESC" : "ACH_GEN_CHAMPION_GOOD_DESC";
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById(titleKey), 
                        Localization.GetFormatted(descKey, stat, divineLore, divineLoreSubject), 
                        new Color(1f, 0.8f, 0f), 1000));
                }
                else
                {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_GEN_PIVOT_TITLE"), 
                        Localization.GetFormatted("ACH_GEN_PIVOT_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.7f, 0.9f, 0.9f), 350));
                }
            }

            // =========================================================
            // 2. LORE MYTHIQUE : ADAPTATION STRICTE (Hauts Faits Conditionnels)
            // =========================================================
            if (mythic.Contains("lich") || mythic.Contains("liche"))
            {
                if (stat.TotalDamage >= baseline * 2f && canGetSS) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_MYTH_LICH_TA_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_TA_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.4f, 0.8f, 0.6f), 400));
                    gotDamageTitle = true;
                }
            }
            else if (mythic.Contains("demon") || mythic.Contains("démon"))
            {
                if (stat.TotalDamage >= baseline * 2f && canGetSS) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_MYTH_DEMON_FURY_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_FURY_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.8f, 0.1f, 0.1f), 400));
                    gotDamageTitle = true;
                }
            }
            else if (mythic.Contains("angel") || mythic.Contains("ange"))
            {
                if (stat.HealingDone >= baseline && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_MVP_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_MVP_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 1f, 0.8f), 350));
                }
            }
            else if (mythic.Contains("aeon") || mythic.Contains("éon"))
            {
                if (stat.TotalDamage >= baseline * 2f && canGetSS) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_MYTH_AEON_CORRECT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_CORRECT_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.2f, 0.5f, 1f), 400));
                    gotDamageTitle = true;
                }
            }
            else if (mythic.Contains("trickster") || mythic.Contains("mystificateur"))
            {
                if (stat.Crits >= 5 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_MYTH_TRICK_PLANAR_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_PLANAR_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(1f, 0.4f, 0.8f), 450));
                }
            }
            else if (mythic.Contains("legend") || mythic.Contains("légende"))
            {
                if (stat.TotalDamage >= baseline * 2f && canGetSS) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_TRIUMPH_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_TRIUMPH_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.8f, 0.8f, 0.8f), 450));
                    gotDamageTitle = true;
                }
            }

            // =========================================================
            // 3. DÉGÂTS & PUISSANCE BRUTE (Exclusif)
            // =========================================================
            if (!gotDamageTitle)
            {
                if (stat.TotalDamage >= baseline * 4f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_GEN_DESTRUCTION_TITLE"), 
                        Localization.GetFormatted("ACH_GEN_DESTRUCTION_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.8f, 0f, 0.2f), 500));
                }
                else if (stat.TotalDamage >= baseline * 2f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_GEN_POWER_AVATAR_TITLE"), 
                        Localization.GetFormatted("ACH_GEN_POWER_AVATAR_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(1f, 0.4f, 0f), 300));
                }
            }

            if (stat.MaxSingleHit >= baseline * 1.5f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_ANVIL_HIT_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_ANVIL_HIT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                    Color.magenta, 250));
            }

            // =========================================================
            // 4. EXÉCUTIONS (Exclusif)
            // =========================================================
            if (totalKills >= 10 && canGetSS) {
                string titleKey = stat.IsEvil ? "ACH_GEN_SHADOW_HARVEST_TITLE" : "ACH_GEN_DESTINY_SCYTHE_TITLE";
                stat.Achievements.Add(new MVPAchievement("SSS", 
                    Localization.GetStringById(titleKey), 
                    Localization.GetFormatted("ACH_GEN_SCYTHE_DESC", stat, divineLore, divineLoreSubject, null, totalKills.ToString()), 
                    Color.red, 600));
                gotKillTitle = true;
            }
            else if (totalKills >= 5 && canGetS) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_GEN_SOUL_HARVESTER_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_SOUL_HARVESTER_DESC", stat, divineLore, divineLoreSubject, null, totalKills.ToString()), 
                    new Color(0.8f, 0.2f, 0.2f), 400));
                gotKillTitle = true;
            }
            else if (totalKills >= 3 && !gotKillTitle) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_GEN_ELITE_EXECUTIONER_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_ELITE_EXECUTIONER_DESC", stat, divineLore, divineLoreSubject, null, totalKills.ToString()), 
                    Color.gray, 150));
            }

            // =========================================================
            // 5. EXPERTISE MARTIALE & ARMES (Exclusif)
            // =========================================================
            if (isPureRanged && stat.PiercingDmg >= baseline && canGetS && !gotStyleTitle) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_SNIPER_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_SNIPER_DESC", stat, divineLore, divineLoreSubject, stat.PiercingDmg.ToString()), 
                    new Color(0.4f, 0.8f, 0.4f), 350));
                gotStyleTitle = true;
            }
            else if (isPureMelee && stat.SlashingDmg >= baseline && stat.DamageTaken >= baseline && canGetS && !gotStyleTitle) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_BLADE_DANCE_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_BLADE_DANCE_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString(), null, stat.DamageTaken.ToString()), 
                    new Color(0.8f, 0.3f, 0.3f), 350));
                gotStyleTitle = true;
            }
            else if (isPureCaster && stat.TotalDamage >= baseline * 1.5f && canGetS && !gotStyleTitle) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_ESOTERIC_KNOWLEDGE_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_ESOTERIC_KNOWLEDGE_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                    new Color(0.6f, 0.2f, 0.8f), 350));
                gotStyleTitle = true;
            }

            if (stat.Crits >= 5 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_PRECISION_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_PRECISION_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                    Color.yellow, 280));
            }

            if (stat.AoOs >= 6 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_HEDGEHOG_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_HEDGEHOG_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                    new Color(0.7f, 0.7f, 0.6f), 310));
            }

            // =========================================================
            // 6. ENTRAVES, CONTRÔLE ET TORTURE (Exclusif)
            // =========================================================
            if (stat.CC_Paralyzed + stat.CC_Stunned >= 4 && canGetS) {
                int totalCCVal = stat.CC_Paralyzed + stat.CC_Stunned;
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_IMMOBILITY_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_IMMOBILITY_DESC", stat, divineLore, divineLoreSubject, null, null, totalCCVal.ToString()), 
                    new Color(0.2f, 0.8f, 0.8f), 300));
                gotCCTitle = true;
            }
            else if (stat.NegativeLevels >= 4 && canGetSS && !gotCCTitle) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_GEN_ESSENCE_DEVOURER_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_ESSENCE_DEVOURER_DESC", stat, divineLore, divineLoreSubject, null, null, stat.NegativeLevels.ToString()), 
                    new Color(0.1f, 0.1f, 0.4f), 450));
                gotCCTitle = true;
            }
            else if (stat.CC_Confused + stat.CC_Dazed >= 4 && !gotCCTitle) {
                int totalCCVal = stat.CC_Confused + stat.CC_Dazed;
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_MADNESS_SOWER_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_MADNESS_SOWER_DESC", stat, divineLore, divineLoreSubject, null, null, totalCCVal.ToString()), 
                    new Color(0.9f, 0.4f, 0.8f), 330));
                gotCCTitle = true;
            }

            if (stat.StatDamage >= 15 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_WITHERING_FLEAU_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_WITHERING_FLEAU_DESC", stat, divineLore, divineLoreSubject, stat.StatDamage.ToString()), 
                    new Color(0.5f, 0.1f, 0.6f), 350));
            }

            // =========================================================
            // 7. ÉLÉMENTS ET MAGIE
            // =========================================================
            if (stat.HolyDmg > 0 && stat.UnholyDmg > 0 && canGetA) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_GEN_BLASPHEMOUS_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_BLASPHEMOUS_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString(), null, stat.UnholyDmg.ToString()), 
                    new Color(0.5f, 0.5f, 0.5f), 200));
            } else {
                if (stat.HolyDmg >= baseline * 0.8f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_GEN_HOLY_LIGHT_TITLE"), 
                        Localization.GetFormatted("ACH_GEN_HOLY_LIGHT_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.9f, 0.4f), 180));
                }
                else if (stat.UnholyDmg >= baseline * 0.8f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_GEN_UNHOLY_FLEAU_TITLE"), 
                        Localization.GetFormatted("ACH_GEN_UNHOLY_FLEAU_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.4f, 0f, 0.6f), 180));
                }
            }

            if (stat.FireDmg >= baseline * 0.8f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_GEN_FIRE_STORM_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_FIRE_STORM_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                    new Color(1f, 0.5f, 0f), 180));
            }
            else if (stat.ColdDmg >= baseline * 0.8f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_GEN_DEADLY_WINTER_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_DEADLY_WINTER_DESC", stat, divineLore, divineLoreSubject, stat.ColdDmg.ToString()), 
                    new Color(0.4f, 0.8f, 1f), 180));
            }

            // =========================================================
            // 8. DÉFENSE & ANOMALIES
            // =========================================================
            if (stat.DamageTaken >= baseline * 3f && stat.AttacksDodged >= 5 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_GEN_LIVING_AEGIS_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_LIVING_AEGIS_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString(), null, stat.AttacksDodged.ToString()), 
                    new Color(0.3f, 0.5f, 0.8f), 350));
            }

            if (stat.SupportBuffsCast >= 10 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_DAWN_BOND_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_DAWN_BOND_DESC", stat, divineLore, divineLoreSubject, null, null, stat.SupportBuffsCast.ToString()), 
                    new Color(0.5f, 0.9f, 0.6f), 300));
            }

            if (stat.FriendlyFireDmg >= baseline * 0.2f)
            {
                stat.Achievements.Add(new MVPAchievement("F", 
                    Localization.GetStringById("ACH_GEN_ACCIDENTAL_TREASON_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_ACCIDENTAL_TREASON_DESC", stat, divineLore, divineLoreSubject, stat.FriendlyFireDmg.ToString()), 
                    new Color(1f, 0.2f, 0.6f), 50));
            }

            // APPEL DES SUCCÈS COMPLÉMENTAIRES D'EXPERTISE
            GrantComplementaryAchievements(stat, baseline, canGetSSS, canGetSS, canGetS, canGetA, canGetB, canGetC, divineLore, divineLoreSubject);
        }
		
		
	private static void GrantPetAchievements(UnitCombatStats stat, float combatWeight, bool isAbsoluteMVP)
        {
            float baseline = Math.Max(5f, combatWeight / 8f);
            int totalKills = stat.Kills + stat.SummonKills;
            int totalCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Prone + stat.CC_Entangled + stat.CC_Shaken;

            bool isNatural = false;
            bool isRangedSet = false;

            if (stat.UnitData?.Descriptor?.Body != null)
            {
                var body = stat.UnitData.Descriptor.Body;
                var primary = body.PrimaryHand?.MaybeItem as ItemEntityWeapon;
                if (primary != null)
                {
                    isNatural = primary.Blueprint.IsNatural || primary.Blueprint.Category == WeaponCategory.UnarmedStrike;
                    if (primary.Blueprint.Type != null)
                    {
                        isRangedSet = primary.Blueprint.Type.IsRanged;
                    }
                }
                else
                {
                    isNatural = true;
                }
            }

            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;
            bool canGetB   = stat.Grade.StartsWith("B") || canGetA;
            bool canGetC   = stat.Grade.StartsWith("C") || canGetB;

            // 1. LE TITRE SUPRÊME
            if (isAbsoluteMVP)
            {
                stat.Achievements.Add(new MVPAchievement("SSS", 
                    Localization.GetStringById("ACH_PET_SARKORIS_SPIRIT_TITLE"), 
                    Localization.GetFormatted("ACH_PET_SARKORIS_SPIRIT_DESC", stat, null, null), 
                    new Color(0.2f, 0.8f, 0.4f), 1000));
            }

            // 2. PUISSANCE PHYSIQUE & MASSACRE (Dynamique selon l'équipement)
            if (stat.TotalDamage >= baseline * 3f && canGetSS)
            {
                if (isNatural)
                {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_PET_CATACLYSM_CLAW_TITLE"), 
                        Localization.GetFormatted("ACH_PET_CATACLYSM_CLAW_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(1f, 0.4f, 0f), 450));
                }
                else if (isRangedSet)
                {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_PET_CATACLYSM_BOW_TITLE"), 
                        Localization.GetFormatted("ACH_PET_CATACLYSM_BOW_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(0.2f, 0.6f, 1f), 450));
                }
                else
                {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_PET_CATACLYSM_BLADE_TITLE"), 
                        Localization.GetFormatted("ACH_PET_CATACLYSM_BLADE_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(0.9f, 0.2f, 0.2f), 450));
                }
            }

            if (totalKills >= 5 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_PET_WOLF_PACK_TITLE"), 
                    Localization.GetFormatted("ACH_PET_WOLF_PACK_DESC", stat, null, null, null, totalKills.ToString()), 
                    new Color(0.8f, 0.1f, 0.1f), 400));
            }
            else if (totalKills >= 3 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_PET_HUNTER_INSTINCT_TITLE"), 
                    Localization.GetFormatted("ACH_PET_HUNTER_INSTINCT_DESC", stat, null, null, null, totalKills.ToString()), 
                    new Color(0.6f, 0.5f, 0.4f), 200));
            }

            // 3. DEFENSE, AGILITÉ & FIDÉLITÉ
            if (stat.DamageTaken >= baseline * 2f && stat.AttacksDodged >= 5 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_PET_FUR_BLOOD_AEGIS_TITLE"), 
                    Localization.GetFormatted("ACH_PET_FUR_BLOOD_AEGIS_DESC", stat, null, null, stat.DamageTaken.ToString(), null, stat.AttacksDodged.ToString()), 
                    new Color(0.3f, 0.6f, 0.9f), 380));
            }
            else if (stat.DamageTaken >= baseline * 1.5f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_PET_SACRIFICED_FLESH_TITLE"), 
                    Localization.GetFormatted("ACH_PET_SACRIFICED_FLESH_DESC", stat, null, null, stat.DamageTaken.ToString()), 
                    new Color(0.8f, 0.4f, 0.3f), 150));
            }

            if (stat.AttacksDodged >= 10 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_PET_FOREST_SHADOW_TITLE"), 
                    Localization.GetFormatted("ACH_PET_FOREST_SHADOW_DESC", stat, null, null, null, null, stat.AttacksDodged.ToString()), 
                    new Color(0.4f, 0.8f, 0.6f), 300));
            }

            // 4. CHIRURGIE MARTIALE / PRÉCISION
            if (stat.Crits >= 4 && canGetS)
            {
                if (isNatural)
                {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_PET_JUGULAR_BITE_TITLE"), 
                        Localization.GetFormatted("ACH_PET_JUGULAR_BITE_DESC", stat, null, null, null, null, stat.Crits.ToString()), 
                        Color.yellow, 290));
                }
                else
                {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_PET_JUGULAR_STRIKE_TITLE"), 
                        Localization.GetFormatted("ACH_PET_JUGULAR_STRIKE_DESC", stat, null, null, null, null, stat.Crits.ToString()), 
                        Color.yellow, 290));
                }
            }

            if (stat.AoOs >= 4 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_PET_JAW_TRAP_TITLE"), 
                    Localization.GetFormatted("ACH_PET_JAW_TRAP_DESC", stat, null, null, null, null, stat.AoOs.ToString()), 
                    new Color(0.7f, 0.5f, 0.3f), 220));
            }

            // 5. CONTRÔLE PRÉDATEUR
            if (totalCC >= 4 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_PET_BEAST_DOMINATION_TITLE"), 
                    Localization.GetFormatted("ACH_PET_BEAST_DOMINATION_DESC", stat, null, null, null, null, totalCC.ToString()), 
                    new Color(0.5f, 0.5f, 0.5f), 310));
            }
            else if (stat.CC_Prone >= 3 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_PET_LINE_BREAKER_TITLE"), 
                    Localization.GetFormatted("ACH_PET_LINE_BREAKER_DESC", stat, null, null, null, null, stat.CC_Prone.ToString()), 
                    new Color(0.5f, 0.4f, 0.3f), 180));
            }

            if (stat.TotalDamage == 0 && stat.DamageTaken == 0 && stat.AttacksDodged >= 5)
            {
                stat.Achievements.Add(new MVPAchievement("C", 
                    Localization.GetStringById("ACH_PET_TUNDRA_MIRAGE_TITLE"), 
                    Localization.GetFormatted("ACH_PET_TUNDRA_MIRAGE_DESC", stat, null, null), 
                    Color.gray, 80));
            }
        }

        private static void GrantEnemyAchievements(UnitCombatStats stat, float combatWeight, bool isAbsoluteMVP)
        {
            float baseline = Math.Max(5f, combatWeight / 8f);
            int totalKills = stat.Kills + stat.SummonKills;
            int totalCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Confused + stat.CC_Prone + stat.CC_Blinded + stat.CC_Sickened;

            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;
            bool canGetB   = stat.Grade.StartsWith("B") || canGetA;

            // 1. LE TITRE SUPRÊME (NÉMÉSIS)
            if (isAbsoluteMVP && (stat.Grade == "SS+" || stat.Grade.StartsWith("SSS")))
            {
                stat.Achievements.Add(new MVPAchievement("SSS+", 
                    Localization.GetStringById("ACH_ENEMY_WORLDBANE_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_WORLDBANE_DESC", stat, null, null), 
                    new Color(0.8f, 0.1f, 0.1f), 1000));
            }

            // 2. RAVAGE & DESTRUCTION DES ALLIÉS
            if (stat.TotalDamage >= baseline * 3f && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_ENEMY_ABYSSAL_FURY_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_ABYSSAL_FURY_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                    new Color(0.7f, 0.1f, 0.3f), 450));
            }

            if (totalKills >= 3 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_ENEMY_PHARASMA_HARVEST_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_PHARASMA_HARVEST_DESC", stat, null, null, null, totalKills.ToString()), 
                    new Color(0.5f, 0.1f, 0.1f), 400));
            }
            else if (totalKills >= 1 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_ENEMY_RIGHTEOUS_BLOOD_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_RIGHTEOUS_BLOOD_DESC", stat, null, null), 
                    new Color(0.6f, 0.2f, 0.2f), 150));
            }

            // 3. ENTRAVES, TORTURE & ENVOÛTEMENTS
            if (totalCC >= 5 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_ENEMY_SOUL_TORMENTOR_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_SOUL_TORMENTOR_DESC", stat, null, null, null, null, totalCC.ToString()), 
                    new Color(0.5f, 0f, 0.5f), 380));
            }
            else if (stat.CC_Frightened + stat.CC_Shaken >= 3 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_ENEMY_DREAD_GLANCE_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_DREAD_GLANCE_DESC", stat, null, null), 
                    new Color(0.3f, 0.1f, 0.4f), 280));
            }

            if (stat.NegativeLevels >= 4 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_ENEMY_UNHOLY_SIPHON_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_UNHOLY_SIPHON_DESC", stat, null, null, null, null, stat.NegativeLevels.ToString()), 
                    new Color(0.1f, 0.1f, 0.3f), 400));
            }

            // 4. ENCAISSEMENT & MONOLITHE IMPIE
            if (stat.DamageTaken >= baseline * 4f && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_ENEMY_SARCOPHAGE_HATRED_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_SARCOPHAGE_HATRED_DESC", stat, null, null, stat.DamageTaken.ToString()), 
                    new Color(0.4f, 0.4f, 0.5f), 350));
            }

            if (stat.AttacksDodged >= 8 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_ENEMY_BLASPHEMOUS_SILHOUETTE_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_BLASPHEMOUS_SILHOUETTE_DESC", stat, null, null, null, null, stat.AttacksDodged.ToString()), 
                    new Color(0.2f, 0.5f, 0.5f), 280));
            }

            // 5. ATTAQUES SOURNOISES & CRITIQUES SADISTES
            if (stat.SneakAttackDmg >= baseline * 0.5f && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_ENEMY_TRAITOR_CLAW_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_TRAITOR_CLAW_DESC", stat, null, null, stat.SneakAttackDmg.ToString()), 
                    new Color(0.3f, 0.3f, 0.3f), 300));
            }

            if (stat.Crits >= 3 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_ENEMY_EXECUTIONER_HIT_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_EXECUTIONER_HIT_DESC", stat, null, null, null, null, stat.Crits.ToString()), 
                    Color.red, 200));
            }

            // 6. ANOMALIES & DÉMENCE
            if (stat.FriendlyFireDmg >= baseline * 1.5f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_ENEMY_BUTCHER_DEMENTIA_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_BUTCHER_DEMENTIA_DESC", stat, null, null, stat.FriendlyFireDmg.ToString()), 
                    new Color(0.8f, 0.3f, 0.1f), 180));
            }

            if (stat.OverkillDmg >= baseline * 1.5f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_ENEMY_DESECRATION_DEAD_TITLE"), 
                    Localization.GetFormatted("ACH_ENEMY_DESECRATION_DEAD_DESC", stat, null, null, stat.OverkillDmg.ToString()), 
                    new Color(0.5f, 0f, 0f), 150));
            }
        }
		
		private static void GrantComplementaryAchievements(UnitCombatStats stat, float baseline, bool canGetSSS, bool canGetSS, bool canGetS, bool canGetA, bool canGetB, bool canGetC, string divineLore, string divineLoreSubject)
        {
            if (stat.StatDamageKills >= 1 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_STAT_KILL_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_STAT_KILL_DESC", stat, divineLore, divineLoreSubject), 
                    new Color(0.5f, 0.1f, 0.6f), 320));
            }

            if (stat.EnergyDrainKills >= 1 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_GEN_DRAIN_KILL_TITLE"), 
                    Localization.GetFormatted("ACH_GEN_DRAIN_KILL_DESC", stat, divineLore, divineLoreSubject), 
                    new Color(0.4f, 0.1f, 0.5f), 320));
            }

            // =========================================================
            // 9. MAÎTRISE DES ÉLÉMENTS (Magie & Énergies)
            // =========================================================
            if (stat.FireDmg >= baseline * 0.8f && canGetA) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_CLASS_WIZ_FIRE_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_WIZ_FIRE_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                    new Color(1f, 0.5f, 0f), 180));
            } else if (stat.FireDmg >= baseline * 0.4f) {
                stat.Achievements.Add(new MVPAchievement("B", 
                    Localization.GetStringById("ACH_CLASS_BRD_HOLY_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_BRD_HOLY_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                    new Color(1f, 0.6f, 0.2f), 80));
            }

            if (stat.ColdDmg >= baseline * 0.8f && canGetA) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_MYTH_LICH_COLD_TITLE"), 
                    Localization.GetFormatted("ACH_MYTH_LICH_COLD_DESC", stat, divineLore, divineLoreSubject, stat.ColdDmg.ToString()), 
                    new Color(0.4f, 0.8f, 1f), 180));
            }

            if (stat.NegativeDmg >= baseline * 0.8f && canGetA) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_CLASS_CLE_NEG_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_CLE_NEG_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                    new Color(0.4f, 0f, 0.6f), 180));
            }

            if (stat.HolyDmg >= baseline * 0.8f && canGetA) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_CLASS_CLE_HOLY_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_CLE_HOLY_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                    new Color(1f, 0.9f, 0.4f), 180));
            }

            // =========================================================
            // 10 & 17. EXPERTISE MARTIALE
            // =========================================================
            if (stat.Crits >= 10 && canGetSSS) {
                stat.Achievements.Add(new MVPAchievement("SSS", 
                    Localization.GetStringById("ACH_MYTH_LEGEND_MVP_CRIT_TITLE"), 
                    Localization.GetFormatted("ACH_MYTH_LEGEND_MVP_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                    new Color(1f, 0.8f, 0f), 480));
            } else if (stat.Crits >= 8 && canGetSS) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_DIFF_UNFAIR_KILLS_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_UNFAIR_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.Crits.ToString()), 
                    Color.red, 380));
            } else if (stat.Crits >= 5 && canGetS) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_CLASS_FIG_CRIT_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_FIG_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                    Color.yellow, 280));
            }

            if (stat.AoOs >= 10 && canGetSS) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_CLASS_FIG_AOO_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_FIG_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                    new Color(0.8f, 0.6f, 0.4f), 410));
            } else if (stat.AoOs >= 8 && canGetSS) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_CLASS_FIG_MVP_AOO_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_FIG_MVP_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                    new Color(0.2f, 1f, 0.2f), 380));
            }

            if (stat.AttacksDodged >= 15 && canGetSS) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_CLASS_BRD_DODGE_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_BRD_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                    new Color(0.4f, 0.4f, 0.6f), 420));
            } else if (stat.AttacksDodged >= 10 && canGetS) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_MYTH_TRICK_DODGE_TITLE"), 
                    Localization.GetFormatted("ACH_MYTH_TRICK_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                    new Color(0.6f, 1f, 0.8f), 250));
            }

            // =========================================================
            // 11. DÉFENSE, OVERKILL & TIR ALLIÉ
            // =========================================================
            if (stat.DamageTaken >= baseline * 3f && stat.AttacksDodged >= 5 && canGetSS) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_CLASS_FIG_TANK_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_FIG_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                    new Color(0.3f, 0.5f, 0.8f), 350));
            } else if (stat.DamageTaken >= baseline * 1.5f && canGetA) {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_DIFF_CORE_MARTYR_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_CORE_MARTYR_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                    new Color(0.4f, 0.6f, 0.8f), 150));
            }

            if (stat.OverkillDmg >= baseline * 1.5f && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_CLASS_BAR_KILLS_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_BAR_KILLS_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString(), (stat.Kills + stat.SummonKills).ToString()), 
                    new Color(0.6f, 0f, 0f), 350));
            }
            else if (stat.OverkillDmg >= baseline * 0.5f && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_CLASS_BAR_OVERKILL_TITLE"), 
                    Localization.GetFormatted("ACH_CLASS_BAR_OVERKILL_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString()), 
                    new Color(0.8f, 0.2f, 0.2f), 170));
            }
        }

        private static void GrantHighDangerAchievements(UnitCombatStats stat, float baseline, bool isAbsoluteMVP, string divineLore, string divineLoreSubject)
        {
            string pronoun = stat.Gender == Gender.Female ? "Elle" : "Il";
            string pronounObj = stat.Gender == Gender.Female ? "elle" : "lui";
            string accord = stat.Gender == Gender.Female ? "e" : "";

            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;

            // --- SUCCÈS D'INSTAKILL (ÉCHELLE DE DANGER) ---
            if (stat.HighDangerInstaKills >= 1 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_HD_INSTAKILL_1_TITLE"), 
                    Localization.GetFormatted("ACH_HD_INSTAKILL_1_DESC", stat, divineLore, divineLoreSubject), 
                    new Color(0.6f, 0.2f, 0.6f), 150));
            }

            if (stat.HighDangerInstaKills >= 2 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_HD_INSTAKILL_2_TITLE"), 
                    Localization.GetFormatted("ACH_HD_INSTAKILL_2_DESC", stat, divineLore, divineLoreSubject), 
                    new Color(0.7f, 0.1f, 0.7f), 300));
            }

            if (stat.HighDangerInstaKills >= 3 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_HD_INSTAKILL_3_TITLE"), 
                    Localization.GetFormatted("ACH_HD_INSTAKILL_3_DESC", stat, divineLore, divineLoreSubject), 
                    new Color(0.8f, 0f, 0.8f), 550));
            }

            if (stat.HighDangerInstaKills >= 1 && stat.UnitData != null && canGetSSS)
            {
                if (stat.CR >= stat.Level + 4)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS", 
                        Localization.GetStringById("ACH_HD_BOSS_INSTAKILL_TITLE"), 
                        Localization.GetFormatted("ACH_HD_BOSS_INSTAKILL_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(1f, 0.4f, 0f), 800));
                }
            }

            // --- SUCCÈS MARTIAUX ---
            if (stat.HighDangerCrits >= 3 && stat.TotalDamage >= baseline * 1.5f && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_HD_CRIT_S_TITLE"), 
                    Localization.GetFormatted("ACH_HD_CRIT_S_DESC", stat, divineLore, divineLoreSubject, null, null, stat.HighDangerCrits.ToString()), 
                    new Color(0.9f, 0.4f, 0.1f), 300));
            }

            if (stat.HighDangerCrits >= 5 && stat.AoOs >= 3 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_HD_CRIT_SS_TITLE"), 
                    Localization.GetFormatted("ACH_HD_CRIT_SS_DESC", stat, divineLore, divineLoreSubject, null, null, stat.HighDangerCrits.ToString()), 
                    new Color(0.8f, 0.1f, 0.1f), 550));
            }

            if (stat.MaxSingleHit >= baseline && stat.HighDangerKills >= 1 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_HD_ONEHIT_TITLE"), 
                    Localization.GetFormatted("ACH_HD_ONEHIT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                    new Color(0.7f, 0.5f, 0.3f), 150));
            }

            // --- SUCCÈS MAGIQUES ---
            if (stat.FireDmg + stat.ElectricDmg >= baseline * 2f && stat.HighDangerKills >= 2 && canGetS)
            {
                int totalDmgVal = stat.FireDmg + stat.ElectricDmg;
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_HD_ELEMENT_TITLE"), 
                    Localization.GetFormatted("ACH_HD_ELEMENT_DESC", stat, divineLore, divineLoreSubject, totalDmgVal.ToString()), 
                    new Color(0.2f, 0.8f, 0.9f), 300));
            }

            if (stat.NegativeDmg >= baseline * 2f && stat.NegativeLevels >= 4 && stat.HighDangerKills >= 2 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_HD_DRAIN_TITLE"), 
                    Localization.GetFormatted("ACH_HD_DRAIN_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                    new Color(0.4f, 0f, 0.5f), 550));
            }

            // --- SUCCÈS CONTRÔLE DE FOULE (CC) ---
            if (stat.HighDangerCCs >= 4 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_HD_CC_S_TITLE"), 
                    Localization.GetFormatted("ACH_HD_CC_S_DESC", stat, divineLore, divineLoreSubject, null, null, stat.HighDangerCCs.ToString()), 
                    new Color(0.2f, 0.8f, 0.5f), 300));
            }

            if (stat.CC_Frightened + stat.CC_Confused >= 3 && stat.HighDangerCCs >= 3 && canGetSS)
            {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_HD_CC_SS_TITLE"), 
                    Localization.GetFormatted("ACH_HD_CC_SS_DESC", stat, divineLore, divineLoreSubject), 
                    new Color(0.5f, 0.1f, 0.6f), 550));
            }

            // --- SUCCÈS DE DÉFENSE / AUTRES ---
            if (stat.DamageTaken >= baseline * 3f && stat.HighDangerKills >= 1 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_HD_TANK_TITLE"), 
                    Localization.GetFormatted("ACH_HD_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                    new Color(0.3f, 0.5f, 0.8f), 300));
            }

            if (stat.AttacksDodged >= 8 && stat.HighDangerKills >= 1 && canGetS)
            {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_HD_DODGE_TITLE"), 
                    Localization.GetFormatted("ACH_HD_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                    new Color(0.2f, 0.8f, 0.6f), 300));
            }

            if (stat.HighDangerKills >= 3 && canGetA)
            {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_HD_KILLS_TITLE"), 
                    Localization.GetFormatted("ACH_HD_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.HighDangerKills.ToString()), 
                    new Color(0.5f, 0.5f, 0.5f), 150));
            }
        }
		
		private static void GrantClassAchievements(UnitCombatStats stat, float baseline, bool isAbsoluteMVP, string divineLore, string divineLoreSubject)
        {
            if (stat.UnitData?.Progression?.Classes == null) return;

            bool isRanger = false;
            bool isWizard = false;
            bool isKineticist = false;
            bool isShaman = false;
            bool isPaladin = false;
            bool isFighter = false;
            bool isCleric = false;
            bool isRogue = false;
            bool isBarbarian = false;
            bool isBard = false;

            foreach (var cls in stat.UnitData.Progression.Classes)
            {
                if (cls.CharacterClass == null) continue;
                string clsName = cls.CharacterClass.name.ToLower();
                if (clsName.Contains("ranger") || clsName.Contains("rodeur") || clsName.Contains("rôdeur")) isRanger = true;
                else if (clsName.Contains("wizard") || clsName.Contains("magicien")) isWizard = true;
                else if (clsName.Contains("kineticist") || clsName.Contains("cinétiste")) isKineticist = true;
                else if (clsName.Contains("shaman") || clsName.Contains("chaman")) isShaman = true;
                else if (clsName.Contains("paladin")) isPaladin = true;
                else if (clsName.Contains("fighter") || clsName.Contains("guerrier")) isFighter = true;
                else if (clsName.Contains("cleric") || clsName.Contains("pretre") || clsName.Contains("prêtre")) isCleric = true;
                else if (clsName.Contains("rogue") || clsName.Contains("voleur")) isRogue = true;
                else if (clsName.Contains("barbarian") || clsName.Contains("barbare") || clsName.Contains("bloodrager")) isBarbarian = true;
                else if (clsName.Contains("bard") || clsName.Contains("barde") || clsName.Contains("skald")) isBard = true;
            }

            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;
            bool canGetB   = stat.Grade.StartsWith("B") || canGetA;
            bool canGetC   = stat.Grade.StartsWith("C") || canGetB; 

            // Détection religieuse centralisée et passée en paramètre

            int totalKills = stat.Kills + stat.SummonKills;
            int totalCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Confused + stat.CC_Prone;

            // ---------------------------------------------------------
            // 1. LE RÔDEUR
            // ---------------------------------------------------------
            if (isRanger)
            {
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_MVP_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.2f, 0.7f, 0.3f), 1100));
                }
                if (isAbsoluteMVP && stat.SummonDamage >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_SUMM_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_SUMM_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.3f, 0.8f, 0.4f), 1100));
                }
                if (stat.PiercingDmg >= baseline * 1.5f && stat.DamageTaken == 0 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_RAIN_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_RAIN_DESC", stat, divineLore, divineLoreSubject, stat.PiercingDmg.ToString()), 
                        new Color(0.4f, 0.9f, 0.5f), 1100));
                }
                if (stat.SlashingDmg >= baseline * 1.5f && stat.Crits >= 4 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_DOUBLE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_DOUBLE_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString(), null, stat.Crits.ToString()), 
                        new Color(0.8f, 0.4f, 0.2f), 550));
                }
                if (totalKills >= 5 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_CLEANER_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_CLEANER_DESC", stat, divineLore, divineLoreSubject, null, totalKills.ToString()), 
                        new Color(0.7f, 0.1f, 0.1f), 550));
                }
                if (stat.CC_Entangled >= 2 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_TRAP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_TRAP_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.6f, 0.3f), 350));
                }
                if (stat.AttacksDodged >= 8 && stat.DamageTaken >= baseline && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_SURVIVAL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_SURVIVAL_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.8f, 0.6f), 350));
                }
                if (stat.AoOs >= 4 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_SENTINEL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_SENTINEL_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.6f, 0.5f, 0.4f), 200));
                }
                if (stat.SummonKills >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_RANGER_BEAST_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_RANGER_BEAST_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.8f, 0.4f), 200));
                }
            }

            // ---------------------------------------------------------
            // 2. LE MAGICIEN
            // ---------------------------------------------------------
            if (isWizard)
            {
                if (isAbsoluteMVP && stat.FireDmg + stat.ColdDmg + stat.ElectricDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_MVP_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.3f, 0.9f), 1100));
                }
                if (isAbsoluteMVP && stat.SummonDamage >= baseline * 1.5f && stat.SummonKills >= 4 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_SUMM_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_SUMM_DESC", stat, divineLore, divineLoreSubject, null, stat.SummonKills.ToString()), 
                        new Color(0.6f, 0.4f, 0.8f), 1100));
                }
                if (stat.IconicSpellsCast >= 3 && totalCC >= 5 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_LABY_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_LABY_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.7f, 0.2f, 0.7f), 1100));
                }
                if (stat.FireDmg >= baseline * 2f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_FIRE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_FIRE_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                        new Color(1f, 0.4f, 0.1f), 550));
                }
                if (stat.AcidDmg >= baseline * 1.5f && stat.StatDamage >= 10 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_ACID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_ACID_DESC", stat, divineLore, divineLoreSubject, stat.AcidDmg.ToString()), 
                        new Color(0.3f, 0.8f, 0.2f), 550));
                }
                if (stat.NegativeDmg >= baseline * 1.5f && stat.NegativeLevels >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_DRAIN_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_DRAIN_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                        new Color(0.4f, 0.1f, 0.5f), 350));
                }
                if (stat.AttacksDodged >= 8 && stat.DamageTaken == 0 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_SHIELD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_SHIELD_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.7f, 1f), 350));
                }
                if (stat.ColdDmg >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_COLD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_COLD_DESC", stat, divineLore, divineLoreSubject, stat.ColdDmg.ToString()), 
                        new Color(0.4f, 0.8f, 1f), 200));
                }
                if (stat.ElectricDmg >= baseline && stat.CC_Stunned >= 1 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_WIZ_ELEC_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_WIZ_ELEC_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.9f, 0.9f, 0.2f), 200));
                }
            }

            // ---------------------------------------------------------
            // 3. LE CINÉTISTE
            // ---------------------------------------------------------
            if (isKineticist)
            {
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_KIN_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_MVP_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.2f, 0.8f, 1f), 1100));
                }
                if (isAbsoluteMVP && stat.OverkillDmg >= baseline * 1.5f && stat.DamageTaken >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_KIN_OVER_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_OVER_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(1f, 0.3f, 0f), 1100));
                }
                if (stat.BludgeoningDmg >= baseline * 2f && stat.CC_Prone >= 4 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_KIN_BLUDG_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_BLUDG_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Prone.ToString()), 
                        new Color(0.6f, 0.4f, 0.2f), 1100));
                }
                if (stat.FireDmg >= baseline * 2f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_KIN_FIRE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_FIRE_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                        new Color(1f, 0.4f, 0.1f), 550));
                }
                if (stat.ColdDmg >= baseline * 1.5f && stat.CC_Slowed >= 2 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_KIN_COLD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_COLD_DESC", stat, divineLore, divineLoreSubject, stat.ColdDmg.ToString()), 
                        new Color(0.4f, 0.8f, 1f), 550));
                }
                if (stat.ElectricDmg >= baseline * 1.5f && stat.CC_Stunned >= 2 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_KIN_ELEC_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_ELEC_DESC", stat, divineLore, divineLoreSubject, stat.ElectricDmg.ToString()), 
                        new Color(0.9f, 0.9f, 0.3f), 350));
                }
                if (stat.SlashingDmg >= baseline * 1.5f && stat.Crits >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_KIN_SLASH_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_SLASH_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.5f, 0.5f, 1f), 350));
                }
                if (stat.AcidDmg >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_KIN_ACID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_ACID_DESC", stat, divineLore, divineLoreSubject, stat.AcidDmg.ToString()), 
                        new Color(0.3f, 0.8f, 0.1f), 200));
                }
                if (stat.AttacksDodged >= 8 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_KIN_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_KIN_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.9f, 0.8f), 200));
                }
            }

            // ---------------------------------------------------------
            // 4. LE CHAMAN
            // ---------------------------------------------------------
            if (isShaman)
            {
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 2f && totalCC >= 4 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_SHA_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_MVP_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.6f, 0.7f, 0.5f), 1100));
                }
                if (isAbsoluteMVP && stat.SummonDamage >= baseline && stat.NegativeDmg >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_SHA_SUMM_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_SUMM_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.3f, 0.5f), 1100));
                }
                if (stat.StatDamage >= 20 && totalCC >= 5 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_SHA_CURSE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_CURSE_DESC", stat, divineLore, divineLoreSubject, stat.StatDamage.ToString()), 
                        new Color(0.5f, 0.2f, 0.6f), 1100));
                }
                if (stat.HealingDone >= baseline * 1.5f && stat.DamageTaken >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_SHA_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString(), null, stat.DamageTaken.ToString()), 
                        new Color(0.3f, 0.8f, 0.5f), 550));
                }
                if (stat.ElectricDmg + stat.SonicDmg >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_SHA_THUNDER_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_THUNDER_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.8f, 0.3f), 550));
                }
                if (stat.ColdDmg >= baseline * 1.5f && stat.CC_Paralyzed >= 2 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_SHA_COLD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_COLD_DESC", stat, divineLore, divineLoreSubject, stat.ColdDmg.ToString()), 
                        new Color(0.4f, 0.8f, 1f), 350));
                }
                if (stat.AttacksDodged >= 10 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_SHA_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.5f, 0.7f), 350));
                }
                if (stat.CC_Frightened + stat.CC_Shaken >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_SHA_FEAR_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_FEAR_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.4f, 0.5f), 200));
                }
                if (stat.SummonKills >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_SHA_BEAST_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_SHA_BEAST_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.7f, 0.4f), 200));
                }
            }

            // ---------------------------------------------------------
            // 5. LE PALADIN
            // ---------------------------------------------------------
            if (isPaladin)
            {
                bool isEvil = stat.IsEvil;
                if (isAbsoluteMVP && !isEvil && stat.HolyDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_MVP_GOOD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_MVP_GOOD_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.9f, 0.4f), 1100));
                }
                else if (isAbsoluteMVP && isEvil && stat.UnholyDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_MVP_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_MVP_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.4f, 0f, 0.5f), 1100));
                }
                if (isAbsoluteMVP && !isEvil && stat.DamageTaken >= baseline * 3f && stat.HealingDone >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_TANK_GOOD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_TANK_GOOD_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(1f, 0.8f, 0.5f), 1100));
                }
                else if (isAbsoluteMVP && isEvil && stat.DamageTaken >= baseline * 3f && stat.VampiricHealing >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_TANK_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_TANK_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.VampiricHealing.ToString()), 
                        new Color(0.5f, 0.1f, 0.1f), 1100));
                }
                if (!isEvil && stat.Kills >= 5 && stat.Crits >= 4 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_KILLS_GOOD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_KILLS_GOOD_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString(), stat.Crits.ToString()), 
                        new Color(1f, 1f, 0.7f), 1100));
                }
                else if (isEvil && stat.Kills >= 5 && stat.OverkillDmg >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_KILLS_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_KILLS_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString(), stat.Kills.ToString()), 
                        new Color(0.7f, 0f, 0f), 1100));
                }
                if (!isEvil && stat.HealingDone >= baseline * 2f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_HEAL_GOOD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_HEAL_GOOD_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 0.9f, 0.6f), 550));
                }
                else if (isEvil && stat.NegativeDmg >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_HEAL_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_HEAL_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                        new Color(0.3f, 0f, 0.4f), 550));
                }
                if (!isEvil && stat.MaxSingleHit >= baseline * 2f && stat.HolyDmg > 0 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_SMITE_GOOD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_SMITE_GOOD_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(1f, 0.8f, 0.3f), 550));
                }
                else if (isEvil && stat.MaxSingleHit >= baseline * 2f && stat.UnholyDmg > 0 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_PAL_SMITE_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_SMITE_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.8f, 0.1f, 0.1f), 550));
                }
                if (!isEvil && stat.DamageTaken >= baseline * 2f && stat.HealingDone >= baseline * 0.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_PAL_MARTYR_GOOD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_MARTYR_GOOD_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(1f, 0.9f, 0.5f), 350));
                }
                else if (isEvil && stat.DamageTaken >= baseline * 2f && stat.VampiricHealing >= baseline * 0.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_PAL_MARTYR_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_PAL_MARTYR_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.VampiricHealing.ToString()), 
                        new Color(0.8f, 0.1f, 0.2f), 350));
                }
                if (stat.AoOs >= 5 && canGetS)
                {
                    string descKey = isEvil ? "ACH_CLASS_PAL_AOO_DESC_EVIL" : "ACH_CLASS_PAL_AOO_DESC_GOOD";
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_PAL_AOO_TITLE"), 
                        Localization.GetFormatted(descKey, stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        isEvil ? new Color(0.4f, 0.1f, 0.4f) : new Color(0.9f, 0.9f, 0.6f), 350));
                }
                if (stat.Crits >= 3 && canGetA)
                {
                    string descKey = isEvil ? "ACH_CLASS_PAL_CRIT_DESC_EVIL" : "ACH_CLASS_PAL_CRIT_DESC_GOOD";
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_PAL_CRIT_TITLE"), 
                        Localization.GetFormatted(descKey, stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        isEvil ? Color.red : Color.yellow, 200));
                }
                if (stat.AttacksDodged >= 6 && canGetA)
                {
                    string descKey = isEvil ? "ACH_CLASS_PAL_DODGE_DESC_EVIL" : "ACH_CLASS_PAL_DODGE_DESC_GOOD";
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_PAL_DODGE_TITLE"), 
                        Localization.GetFormatted(descKey, stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        isEvil ? Color.black : Color.white, 200));
                }
            }
			
			// ---------------------------------------------------------
            // 6. LE GUERRIER
            // ---------------------------------------------------------
            if (isFighter)
            {
                if (isAbsoluteMVP && stat.DamageTaken >= baseline * 4f && stat.AttacksDodged >= 10 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_MVP_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_MVP_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.8f, 0.7f, 0.5f), 1100));
                }
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 3.5f && stat.Crits >= 5 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_MVP_DMG_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_MVP_DMG_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.9f, 0.5f, 0.1f), 1100));
                }
                if (isAbsoluteMVP && stat.AoOs >= 6 && stat.Kills >= 5 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_MVP_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_MVP_AOO_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.7f, 0.8f, 0.9f), 1100));
                }
                if (stat.DamageTaken >= baseline * 3f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.5f, 0.6f, 0.8f), 550));
                }
                if (stat.TotalDamage >= baseline * 2f && stat.SlashingDmg >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_SLASH_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_SLASH_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.8f, 0.2f, 0.2f), 550));
                }
                if (stat.Kills >= 5 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.7f, 0.1f, 0.1f), 550));
                }
                if (stat.AoOs >= 5 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_FIG_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        new Color(0.9f, 0.6f, 0.2f), 550));
                }
                if (stat.AttacksDodged >= 8 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_FIG_SHIELD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_SHIELD_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.6f, 0.6f, 0.6f), 350));
                }
                if (stat.Crits >= 4 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_FIG_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(0.8f, 0.8f, 0.2f), 350));
                }
                if (stat.BludgeoningDmg >= baseline * 1.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_FIG_BLUDG_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_BLUDG_DESC", stat, divineLore, divineLoreSubject, stat.BludgeoningDmg.ToString()), 
                        new Color(0.6f, 0.4f, 0.2f), 350));
                }
                if (stat.DamageTaken >= baseline * 2f && stat.Kills >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_FIG_BERSERK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_BERSERK_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString(), stat.DamageTaken.ToString()), 
                        new Color(0.8f, 0.1f, 0.1f), 350));
                }
                if (stat.SlashingDmg >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_FIG_SLASH_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_SLASH_LOW_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.9f, 0.4f, 0.1f), 200));
                }
                if (stat.AoOs >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_FIG_AOO_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_AOO_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.7f, 0.7f, 0.7f), 200));
                }
                if (stat.AttacksDodged >= 5 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_FIG_DODGE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_DODGE_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.3f, 0.8f, 0.7f), 200));
                }
                if (stat.MaxSingleHit >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_FIG_HIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_HIT_LOW_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.6f, 0.2f, 0.8f), 200));
                }
                if (stat.CC_Prone >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_FIG_PRONE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_PRONE_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.4f, 0.3f), 200));
                }
                if (stat.DamageTaken >= baseline && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_FIG_TANK_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_TANK_LOW_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.5f, 0.5f, 0.5f), 150));
                }
                if (stat.Crits >= 2 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_FIG_CRIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_CRIT_LOW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(0.9f, 0.9f, 0.5f), 150));
                }
                if (stat.TotalDamage >= baseline && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_FIG_DMG_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_DMG_LOW_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.7f, 0.7f, 0.8f), 150));
                }
                if (stat.AttacksDodged >= 3 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_FIG_SHIELD_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_FIG_SHIELD_LOW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.8f, 0.8f, 0.8f), 150));
                }
            }

            // ---------------------------------------------------------
            // 7. LE PRÊTRE
            // ---------------------------------------------------------
            if (isCleric)
            {
                bool isEvil = stat.IsEvil;
                if (isAbsoluteMVP && !isEvil && stat.HealingDone >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_MVP_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_MVP_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 0.9f, 0.6f), 1100));
                }
                if (isAbsoluteMVP && !isEvil && stat.HolyDmg >= baseline * 2.5f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_MVP_HOLY_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_MVP_HOLY_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.85f, 0.2f), 1100));
                }
                if (isAbsoluteMVP && isEvil && stat.UnholyDmg + stat.NegativeDmg >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_MVP_EVIL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_MVP_EVIL_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.4f, 0.1f, 0.5f), 1100));
                }
                if (stat.HealingDone >= baseline * 2f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 0.95f, 0.7f), 550));
                }
                if (stat.HolyDmg >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_HOLY_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_HOLY_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.8f, 0.1f), 550));
                }
                if (stat.NegativeDmg >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_NEG_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_NEG_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                        new Color(0.5f, 0.1f, 0.6f), 550));
                }
                if (stat.HealingDone >= baseline && stat.DamageTaken >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_CLE_GARD_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_GARD_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString(), null, stat.DamageTaken.ToString()), 
                        new Color(0.3f, 0.6f, 0.9f), 550));
                }
                if (stat.CC_Paralyzed + stat.CC_Stunned >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_CLE_CC_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_CC_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.2f, 0.8f, 0.8f), 350));
                }
                if (stat.VampiricHealing >= baseline * 0.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_CLE_VAMP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_VAMP_DESC", stat, divineLore, divineLoreSubject, stat.VampiricHealing.ToString()), 
                        new Color(0.8f, 0.1f, 0.2f), 350));
                }
                if (stat.Kills >= 3 && stat.HolyDmg >= baseline * 0.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_CLE_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.9f, 0.7f, 0.2f), 350));
                }
                if (stat.UnholyDmg >= baseline && isEvil && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_CLE_UNHOLY_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_UNHOLY_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.5f, 0f, 0.2f), 350));
                }
                if (stat.HealingDone >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_CLE_HEAL_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_HEAL_LOW_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(0.6f, 0.8f, 1f), 200));
                }
                if (stat.HolyDmg >= baseline * 0.5f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_CLE_HOLY_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_HOLY_LOW_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.9f, 0.4f), 200));
                }
                if (stat.NegativeDmg >= baseline * 0.5f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_CLE_NEG_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_NEG_LOW_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                        new Color(0.4f, 0.2f, 0.5f), 200));
                }
                if (stat.AttacksDodged >= 5 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_CLE_DODGE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_DODGE_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.7f, 0.9f), 200));
                }
                if (stat.MaxSingleHit >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_CLE_SMITE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_SMITE_LOW_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.7f, 0.3f, 0.8f), 200));
                }
                if (stat.HealingDone > 0 && stat.TotalDamage > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_CLE_HYBRID_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_HYBRID_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.6f, 0.6f, 0.6f), 150));
                }
                if (stat.HolyDmg > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_CLE_HOLY_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_HOLY_MID_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(1f, 0.95f, 0.6f), 150));
                }
                if (stat.NegativeDmg > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_CLE_NEG_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_NEG_MID_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.3f, 0.2f, 0.4f), 150));
                }
                if (stat.AttacksDodged >= 3 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_CLE_REFUGE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_CLE_REFUGE_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.5f, 0.7f), 150));
                }
            }

            // ---------------------------------------------------------
            // 8. LE VOLEUR
            // ---------------------------------------------------------
            if (isRogue)
            {
                if (isAbsoluteMVP && stat.SneakAttackDmg >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_MVP_SNEAK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_MVP_SNEAK_DESC", stat, divineLore, divineLoreSubject, stat.SneakAttackDmg.ToString()), 
                        new Color(0.1f, 0.6f, 0.3f), 1100));
                }
                if (isAbsoluteMVP && stat.Crits >= 6 && stat.Kills >= 5 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_MVP_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_MVP_CRIT_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString(), stat.Crits.ToString()), 
                        new Color(0.1f, 0.1f, 0.2f), 1100));
                }
                if (isAbsoluteMVP && stat.AttacksDodged >= 20 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_MVP_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_MVP_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.2f, 0.8f, 0.5f), 1100));
                }
                if (stat.SneakAttackDmg >= baseline * 2f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_SNEAK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_SNEAK_DESC", stat, divineLore, divineLoreSubject, stat.SneakAttackDmg.ToString()), 
                        new Color(0.7f, 0.1f, 0.1f), 550));
                }
                if (stat.SlashingDmg + stat.PiercingDmg >= baseline * 2f && stat.Crits >= 4 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_TWIN_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_TWIN_DESC", stat, divineLore, divineLoreSubject, (stat.SlashingDmg + stat.PiercingDmg).ToString()), 
                        new Color(0.8f, 0.2f, 0.2f), 550));
                }
                if (stat.AttacksDodged >= 12 && stat.DamageTaken == 0 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_FLAWLESS_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_FLAWLESS_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.2f, 0.7f, 0.7f), 550));
                }
                if (stat.Kills >= 4 && stat.SneakAttackDmg >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_ROG_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.5f, 0.1f, 0.5f), 550));
                }
                if (stat.PiercingDmg >= baseline * 1.5f && stat.Crits >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_ROG_PIERCE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_PIERCE_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.9f, 0.8f, 0.1f), 350));
                }
                if (stat.AttacksDodged >= 8 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_ROG_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_DODGE_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.5f, 0.6f), 350));
                }
                if (stat.SneakAttackDmg >= baseline && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_ROG_SNEAK_S_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_SNEAK_S_DESC", stat, divineLore, divineLoreSubject, stat.SneakAttackDmg.ToString()), 
                        new Color(0.1f, 0.5f, 0.3f), 350));
                }
                if (stat.AoOs >= 4 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_ROG_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        new Color(0.6f, 0.4f, 0.2f), 350));
                }
                if (stat.AttacksDodged >= 5 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_ROG_DODGE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_DODGE_LOW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.4f, 0.8f, 0.4f), 200));
                }
                if (stat.SneakAttackDmg >= baseline * 0.5f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_ROG_SNEAK_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_SNEAK_LOW_DESC", stat, divineLore, divineLoreSubject, stat.SneakAttackDmg.ToString()), 
                        new Color(0.4f, 0.4f, 0.5f), 200));
                }
                if (stat.Crits >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_ROG_CRIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_CRIT_LOW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(0.7f, 0.1f, 0.1f), 200));
                }
                if (stat.SlashingDmg >= baseline * 0.5f && stat.PiercingDmg >= baseline * 0.5f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_ROG_HYBRID_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_HYBRID_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.7f, 0.5f, 0.3f), 200));
                }
                if (stat.MaxSingleHit >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_ROG_HIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_HIT_LOW_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.8f, 0.6f, 0.2f), 200));
                }
                if (stat.TotalDamage >= baseline && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_ROG_DMG_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_DMG_LOW_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.5f, 0.5f, 0.5f), 150));
                }
                if (stat.AttacksDodged >= 3 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_ROG_DODGE_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_DODGE_MID_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.8f, 0.8f), 150));
                }
                if (stat.SneakAttackDmg > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_ROG_SNEAK_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_SNEAK_MID_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.7f, 0.5f), 150));
                }
                if (stat.CC_Prone >= 1 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_ROG_PRONE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_ROG_PRONE_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.4f, 0.3f), 150));
                }
            }
			
			// ---------------------------------------------------------
            // 9. LE BARBARE
            // ---------------------------------------------------------
            if (isBarbarian)
            {
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 4f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_MVP_DMG_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_MVP_DMG_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.9f, 0.1f, 0.1f), 1100));
                }
                if (isAbsoluteMVP && stat.OverkillDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_MVP_OVER_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_MVP_OVER_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString()), 
                        new Color(0.6f, 0.5f, 0f), 1100));
                }
                if (isAbsoluteMVP && stat.DamageTaken >= baseline * 4f && stat.Kills >= 5 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_MVP_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_MVP_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString(), stat.Kills.ToString()), 
                        new Color(0.5f, 0f, 0.1f), 1100));
                }
                if (stat.MaxSingleHit >= baseline * 2.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_ONESHOT_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_ONESHOT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(1.0f, 0.4f, 0.1f), 550));
                }
                if (stat.TotalDamage >= baseline * 2.5f && stat.SlashingDmg >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_SLASH_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_SLASH_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.8f, 0.2f, 0.2f), 550));
                }
                if (stat.Kills >= 5 && stat.OverkillDmg >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_KILLS_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString(), (stat.Kills + stat.SummonKills).ToString()), 
                        new Color(0.7f, 0f, 0f), 550));
                }
                if (stat.DamageTaken >= baseline * 3f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BAR_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.3f, 0.3f, 0.3f), 550));
                }
                if (stat.BludgeoningDmg >= baseline * 1.5f && stat.CC_Prone >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BAR_BLUDG_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_BLUDG_DESC", stat, divineLore, divineLoreSubject, stat.BludgeoningDmg.ToString(), null, stat.CC_Prone.ToString()), 
                        new Color(0.9f, 0.6f, 0.1f), 350));
                }
                if (stat.Kills >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BAR_KILLS_S_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_KILLS_S_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.9f, 0.1f, 0.2f), 350));
                }
                if (stat.OverkillDmg >= baseline * 0.8f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BAR_OVERKILL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_OVERKILL_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString()), 
                        new Color(0.7f, 0.1f, 0.1f), 350));
                }
                if (stat.DamageTaken >= baseline * 2f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BAR_TANK_S_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_TANK_S_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.6f, 0.3f, 0.2f), 350));
                }
                if (stat.SlashingDmg >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BAR_SLASH_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_SLASH_LOW_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.9f, 0.4f, 0.1f), 200));
                }
                if (stat.BludgeoningDmg >= baseline * 0.5f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BAR_BLUDG_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_BLUDG_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.4f, 0.3f), 200));
                }
                if (stat.MaxSingleHit >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BAR_HIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_HIT_LOW_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.8f, 0.2f, 0.6f), 200));
                }
                if (stat.AoOs >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BAR_AOO_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_AOO_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.3f, 0.3f), 200));
                }
                if (stat.CC_Prone >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BAR_PRONE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_PRONE_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.6f, 0.3f), 200));
                }
                if (stat.TotalDamage >= baseline && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BAR_DMG_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_DMG_LOW_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.5f, 0.5f, 0.5f), 150));
                }
                if (stat.DamageTaken >= baseline && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BAR_TANK_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_TANK_LOW_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.4f, 0.4f, 0.4f), 150));
                }
                if (stat.Crits >= 2 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BAR_CRIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_CRIT_LOW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(0.8f, 0.4f, 0.4f), 150));
                }
                if (stat.OverkillDmg > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BAR_OVER_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BAR_OVER_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.5f, 0.5f), 150));
                }
            }

            // ---------------------------------------------------------
            // 10. LE BARDE
            // ---------------------------------------------------------
            if (isBard)
            {
                if (isAbsoluteMVP && stat.TotalDamage + stat.HealingDone >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_MVP_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString(), null, stat.HealingDone.ToString()), 
                        new Color(1f, 0.85f, 0.4f), 1100));
                }
                if (isAbsoluteMVP && stat.SonicDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_MVP_SONIC_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_MVP_SONIC_DESC", stat, divineLore, divineLoreSubject, stat.SonicDmg.ToString()), 
                        new Color(0.8f, 0.3f, 0.9f), 1100));
                }
                if (isAbsoluteMVP && stat.CC_Confused + stat.CC_Dazed >= 5 && canGetSSS)
                {
                    int totalCCVal = stat.CC_Confused + stat.CC_Dazed;
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_MVP_CC_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_MVP_CC_DESC", stat, divineLore, divineLoreSubject, null, null, totalCCVal.ToString()), 
                        new Color(0.9f, 0.4f, 0.7f), 1100));
                }
                if (stat.SonicDmg >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_SONIC_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_SONIC_DESC", stat, divineLore, divineLoreSubject, stat.SonicDmg.ToString()), 
                        new Color(0.6f, 0.2f, 0.8f), 550));
                }
                if (stat.HealingDone >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(0.2f, 0.6f, 0.9f), 550));
                }
                if (stat.AttacksDodged >= 12 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_DODGE_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.2f, 0.8f, 0.8f), 550));
                }
                if (stat.CC_Confused >= 3 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_CLASS_BRD_CONFUSE_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_CONFUSE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Confused.ToString()), 
                        new Color(0.8f, 0.3f, 0.6f), 550));
                }
                if (stat.CC_Slowed + stat.CC_Staggered >= 3 && canGetS)
                {
                    int totalCCVal = stat.CC_Slowed + stat.CC_Staggered;
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BRD_SLOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_SLOW_DESC", stat, divineLore, divineLoreSubject, null, null, totalCCVal.ToString()), 
                        new Color(0.5f, 0.5f, 0.7f), 350));
                }
                if (stat.HolyDmg >= baseline * 0.5f && stat.SonicDmg >= baseline * 0.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BRD_HOLY_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_HOLY_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.9f, 0.8f, 0.3f), 350));
                }
                if (stat.HealingDone >= baseline && stat.AttacksDodged >= 6 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BRD_SURVIVAL_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_SURVIVAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString(), null, stat.AttacksDodged.ToString()), 
                        new Color(0.4f, 0.8f, 0.7f), 350));
                }
                if (stat.Crits >= 4 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_CLASS_BRD_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(0.9f, 0.4f, 0.6f), 350));
                }
                if (stat.HealingDone >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BRD_HEAL_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_HEAL_LOW_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(0.5f, 0.7f, 1f), 200));
                }
                if (stat.SonicDmg >= baseline * 0.5f && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BRD_SONIC_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_SONIC_LOW_DESC", stat, divineLore, divineLoreSubject, stat.SonicDmg.ToString()), 
                        new Color(0.7f, 0.5f, 0.8f), 200));
                }
                if (stat.AttacksDodged >= 5 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BRD_DODGE_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_DODGE_LOW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.3f, 0.8f, 0.6f), 200));
                }
                if (stat.MaxSingleHit >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BRD_HIT_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_HIT_LOW_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.8f, 0.6f, 0.2f), 200));
                }
                if (stat.CC_Dazed >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_CLASS_BRD_CC_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_CC_LOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.5f, 0.7f), 200));
                }
                if (stat.TotalDamage >= baseline && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BRD_DMG_LOW_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_DMG_LOW_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.5f, 0.5f, 0.5f), 150));
                }
                if (stat.HealingDone > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BRD_HEAL_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_HEAL_MID_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.6f, 0.8f, 0.6f), 150));
                }
                if (stat.SonicDmg > 0 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BRD_SONIC_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_SONIC_MID_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.7f, 0.5f, 0.7f), 150));
                }
                if (stat.AttacksDodged >= 3 && canGetB)
                {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_CLASS_BRD_DODGE_MID_TITLE"), 
                        Localization.GetFormatted("ACH_CLASS_BRD_DODGE_MID_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.8f, 0.5f, 0.6f), 150));
                }
            }
        } // Fin de GrantClassAchievements

        // =========================================================================
        // LES 100 SUCCÈS MYTHIQUES (10 VOIES * 10 SUCCÈS - LORE-FRIENDLY & IMMERSIFS)
        // =========================================================================
        private static void GrantMythicPathAchievements(UnitCombatStats stat, float baseline, bool isAbsoluteMVP, string divineLore, string divineLoreSubject)
        {
            if (string.IsNullOrEmpty(stat.MythicPathName)) return;
            string path = stat.MythicPathName.ToLower();
            string pronoun = stat.Gender == Gender.Female ? "Elle" : "Il";
            string pronounObj = stat.Gender == Gender.Female ? "elle" : "lui";
            string accord = stat.Gender == Gender.Female ? "e" : "";
            string titAccord = stat.Gender == Gender.Female ? "La" : "Le";
            int totalKills = stat.Kills + stat.SummonKills;
            int totalCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Confused + stat.CC_Prone;

            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;
            bool canGetB   = stat.Grade.StartsWith("B") || canGetA;

            // Détection religieuse centralisée et passée en paramètre

            // ---------------------------------------------------------
            // 1. LA VOIE DE L'ANGE (LUMIÈRE & CHÂTIMENT)
            // ---------------------------------------------------------
            if (path.Contains("ange") || path.Contains("angel"))
            {
                if (isAbsoluteMVP && stat.HolyDmg >= baseline * 2f)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_MVP_DMG_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_MVP_DMG_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.95f, 0.7f), 1100));
                }
                if (isAbsoluteMVP && stat.HealingDone >= baseline * 3f)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_MVP_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_MVP_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 0.9f, 0.6f), 1100));
                }
                if (stat.MaxSingleHit >= baseline * 3f && stat.HolyDmg > 0)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_ONESHOT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_ONESHOT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(1f, 0.85f, 0.5f), 1100));
                }
                if (stat.DamageTaken >= baseline * 3f && stat.HealingDone >= baseline)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_MARTYR_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_MARTYR_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.9f, 0.8f, 0.4f), 550));
                }
                if (stat.Kills >= 5 && stat.HolyDmg >= baseline)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(1f, 0.75f, 0.3f), 550));
                }
                if (stat.FireDmg >= baseline * 1.5f)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_FIRE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_FIRE_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                        new Color(1f, 0.5f, 0.1f), 350));
                }
                if (stat.AttacksDodged >= 10 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.6f, 0.9f, 0.9f), 350));
                }
                if (stat.SummonKills >= 3)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_SUMMON_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_SUMMON_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.8f, 0.5f), 200));
                }
                if (stat.Crits >= 4 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        Color.yellow, 200));
                }
                if (stat.CC_Frightened > 0 || stat.CC_Shaken > 0)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_ANGEL_FEAR_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_ANGEL_FEAR_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.9f, 0.9f, 0.4f), 200));
                }
            }

            // ---------------------------------------------------------
            // 2. LA VOIE DU DÉMON (RAGE & COLÈRE INEXTINGUIBLE)
            // ---------------------------------------------------------
            if (path.Contains("démon") || path.Contains("demon"))
            {
                if (isAbsoluteMVP && stat.OverkillDmg >= baseline * 2f)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_MVP_OVER_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_MVP_OVER_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString()), 
                        new Color(0.9f, 0.1f, 0.1f), 1100));
                }
                if (isAbsoluteMVP && totalKills >= 8)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_MVP_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_MVP_KILLS_DESC", stat, divineLore, divineLoreSubject, null, totalKills.ToString()), 
                        new Color(0.8f, 0f, 0), 1100));
                }
                if (stat.MaxSingleHit >= baseline * 3f && (stat.SlashingDmg > 0 || stat.BludgeoningDmg > 0))
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_ONESHOT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_ONESHOT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.7f, 0f, 0.2f), 1100));
                }
                if (stat.DamageTaken >= baseline * 3f && stat.Kills >= 4)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_BERSERK_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_BERSERK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString(), stat.Kills.ToString()), 
                        new Color(0.6f, 0.1f, 0.1f), 550));
                }
                if (stat.UnholyDmg >= baseline * 2f)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_UNHOLY_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_UNHOLY_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.4f, 0f, 0.5f), 550));
                }
                if (stat.AoOs >= 6)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        new Color(0.5f, 0.2f, 0.2f), 350));
                }
                if (stat.FireDmg >= baseline * 1.5f)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_FIRE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_FIRE_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                        new Color(1f, 0.3f, 0.1f), 350));
                }
                if (stat.SummonKills >= 3)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_SUMMON_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_SUMMON_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.6f, 0.4f, 0.4f), 200));
                }
                if (stat.Crits >= 4 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        Color.red, 200));
                }
                if (stat.CC_Frightened >= 2)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DEMON_FEAR_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEMON_FEAR_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.3f, 0.1f, 0.1f), 200));
                }
            }

            // ---------------------------------------------------------
            // 3. LA VOIE DE LA LICHE (SANS-MORT & TOMBEAU)
            // ---------------------------------------------------------
            if (path.Contains("liche") || path.Contains("lich"))
            {
                if (isAbsoluteMVP && stat.NegativeDmg >= baseline * 2f)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_LICH_MVP_NEG_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_MVP_NEG_DESC", stat, divineLore, divineLoreSubject, stat.NegativeDmg.ToString()), 
                        new Color(0.4f, 0.8f, 0.4f), 1100));
                }
                if (isAbsoluteMVP && stat.NegativeLevels >= 6)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_LICH_MVP_DRAIN_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_MVP_DRAIN_DESC", stat, divineLore, divineLoreSubject, null, null, stat.NegativeLevels.ToString()), 
                        new Color(0.5f, 0.1f, 0.6f), 1100));
                }
                if (stat.InstaKills >= 3)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_LICH_INSTAKILL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_INSTAKILL_DESC", stat, divineLore, divineLoreSubject, null, null, stat.InstaKills.ToString()), 
                        new Color(0.1f, 0.5f, 0.2f), 1100));
                }
                if (stat.SummonDamage >= baseline * 2f && stat.SummonKills >= 4)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_LICH_SUMMON_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_SUMMON_DESC", stat, divineLore, divineLoreSubject, stat.SummonDamage.ToString(), stat.SummonKills.ToString()), 
                        new Color(0.8f, 0.8f, 0.8f), 550));
                }
                if (stat.VampiricHealing >= baseline * 1.5f)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_LICH_VAMP_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_VAMP_DESC", stat, divineLore, divineLoreSubject, stat.VampiricHealing.ToString()), 
                        new Color(0.7f, 0.1f, 0.2f), 550));
                }
                if (stat.ColdDmg >= baseline * 1.5f)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_LICH_COLD_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_COLD_DESC", stat, divineLore, divineLoreSubject, stat.ColdDmg.ToString()), 
                        new Color(0.4f, 0.8f, 1f), 350));
                }
                if (stat.StatDamage >= 15)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_LICH_STAT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_STAT_DESC", stat, divineLore, divineLoreSubject, stat.StatDamage.ToString()), 
                        new Color(0.5f, 0.1f, 0.6f), 350));
                }
                if (stat.CC_Paralyzed >= 2)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_LICH_PARALYZE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_PARALYZE_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.3f, 0.7f, 0.5f), 200));
                }
                if (stat.IconicSpellsCast >= 2)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_LICH_DEATH_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_DEATH_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.2f, 0.6f), 200));
                }
                if (stat.SlashingDmg >= baseline && stat.Kills >= 3)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_LICH_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LICH_KILLS_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.6f, 0.6f, 0.6f), 200));
                }
            }
			
			// ---------------------------------------------------------
            // 4. LA VOIE DU MYSTIFICATEUR (CHAOS & CRITIQUES)
            // ---------------------------------------------------------
            if (path.Contains("grivois") || path.Contains("trickster") || path.Contains("mystificateur"))
            {
                if (isAbsoluteMVP && stat.Crits >= 8 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_MVP_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_MVP_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(1f, 0.4f, 0.8f), 1100));
                }
                if (isAbsoluteMVP && stat.SneakAttackDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_MVP_SNEAK_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_MVP_SNEAK_DESC", stat, divineLore, divineLoreSubject, stat.SneakAttackDmg.ToString()), 
                        new Color(0.8f, 0.2f, 0.8f), 1100));
                }
                if (stat.MaxSingleHit >= baseline * 3f && stat.Crits >= 4 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_ONESHOT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_ONESHOT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(1f, 0.5f, 0.9f), 1100));
                }
                if (stat.AttacksDodged >= 15 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.4f, 1f, 0.8f), 550));
                }
                if (stat.CC_Confused >= 4 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_CONFUSE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_CONFUSE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Confused.ToString()), 
                        new Color(0.9f, 0.4f, 0.8f), 550));
                }
                if (stat.AcidDmg >= baseline * 1.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_ACID_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_ACID_DESC", stat, divineLore, divineLoreSubject, stat.AcidDmg.ToString()), 
                        new Color(0.2f, 0.9f, 0.2f), 350));
                }
                if (stat.AoOs >= 5 && stat.Crits >= 2 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        new Color(0.8f, 0.8f, 0.2f), 350));
                }
                if (stat.CC_Prone >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_PRONE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_PRONE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Prone.ToString()), 
                        new Color(0.6f, 0.5f, 0.3f), 200));
                }
                if (stat.PiercingDmg >= baseline && stat.DamageTaken == 0 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_SHADOW_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_SHADOW_DESC", stat, divineLore, divineLoreSubject, stat.PiercingDmg.ToString()), 
                        new Color(0.5f, 0.8f, 0.5f), 200));
                }
                if (stat.SummonKills >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_TRICK_SUMMON_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_TRICK_SUMMON_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.4f, 0.6f), 200));
                }
            }

            // ---------------------------------------------------------
            // 5. LA VOIE DE L'ÉON (JUGE & ÉQUILIBRE)
            // ---------------------------------------------------------
            if (path.Contains("éon") || path.Contains("aeon"))
            {
                if (isAbsoluteMVP && stat.FriendlyFireDmg == 0 && stat.TotalDamage >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_AEON_MVP_ORDER_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_MVP_ORDER_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.2f, 0.6f, 1f), 1100));
                }
                if (isAbsoluteMVP && stat.InstaKills >= 2 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_AEON_MVP_ERASE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_MVP_ERASE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.InstaKills.ToString()), 
                        new Color(0.3f, 0.8f, 1f), 1100));
                }
                if (stat.Kills >= 5 && stat.OverkillDmg == 0 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_AEON_MVP_MATH_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_MVP_MATH_DESC", stat, divineLore, divineLoreSubject, null, stat.Kills.ToString()), 
                        new Color(0.5f, 0.9f, 1f), 1100));
                }
                if (stat.CC_Staggered + stat.CC_Slowed >= 5 && canGetSS)
                {
                    int totalCCVal = stat.CC_Staggered + stat.CC_Slowed;
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_AEON_STASIS_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_STASIS_DESC", stat, divineLore, divineLoreSubject, null, null, totalCCVal.ToString()), 
                        new Color(0.4f, 0.5f, 0.9f), 550));
                }
                if (stat.TotalDamage >= baseline && stat.DamageTaken >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_AEON_BALANCE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_BALANCE_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.6f, 0.6f, 0.8f), 550));
                }
                if (stat.AttacksDodged >= 10 && stat.DamageTaken == 0 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_AEON_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.7f, 1f), 350));
                }
                if (stat.StatDamage >= 15 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_AEON_STAT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_STAT_DESC", stat, divineLore, divineLoreSubject, stat.StatDamage.ToString()), 
                        new Color(0.5f, 0.4f, 0.9f), 350));
                }
                if (stat.Crits >= 4 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_AEON_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        Color.cyan, 200));
                }
                if (stat.SlashingDmg > 0 && stat.PiercingDmg > 0 && stat.BludgeoningDmg > 0 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_AEON_HYBRID_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_HYBRID_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.7f, 0.7f, 0.8f), 200));
                }
                if (stat.CC_Prone >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_AEON_PRONE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AEON_PRONE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Prone.ToString()), 
                        new Color(0.5f, 0.5f, 0.6f), 200));
                }
            }

            // ---------------------------------------------------------
            // 6. LA VOIE DE L'AZATA (LIBERTÉ & CHANSON)
            // ---------------------------------------------------------
            if (path.Contains("azata"))
            {
                if (isAbsoluteMVP && stat.UnitData != null && stat.UnitData.IsPet && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_SONG_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_SONG_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.2f, 0.9f, 0.5f), 1100));
                }
                if (isAbsoluteMVP && stat.HealingDone >= baseline * 2f && stat.TotalDamage >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_MVP_LIFE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_MVP_LIFE_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(0.3f, 1f, 0.6f), 1100));
                }
                if (stat.ElectricDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_MVP_LIGHT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_MVP_LIGHT_DESC", stat, divineLore, divineLoreSubject, stat.ElectricDmg.ToString()), 
                        new Color(0.9f, 0.4f, 1f), 1100));
                }
                if (stat.SonicDmg >= baseline * 1.5f && stat.CC_Stunned >= 2 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_SONIC_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_SONIC_DESC", stat, divineLore, divineLoreSubject, stat.SonicDmg.ToString()), 
                        new Color(1f, 0.5f, 0.8f), 550));
                }
                if (stat.AttacksDodged >= 12 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.4f, 0.9f, 0.9f), 550));
                }
                if (stat.SummonKills >= 3 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_SUMMON_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_SUMMON_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.8f, 0.5f), 350));
                }
                if (stat.SlashingDmg >= baseline * 1.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_SLASH_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_SLASH_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.9f, 0.3f, 0.3f), 350));
                }
                if (stat.CC_Entangled >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_CC_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_CC_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.7f, 0.3f), 200));
                }
                if (stat.Crits >= 4 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        Color.yellow, 200));
                }
                if (stat.CC_Slowed >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_AZATA_SLOW_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_AZATA_SLOW_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.6f, 0.6f, 0.8f), 200));
                }
            }

            // ---------------------------------------------------------
            // 7. LA VOIE DU DRAGON D'OR (COMPASSION & SAGESSE)
            // ---------------------------------------------------------
            if (path.Contains("dragon") || path.Contains("gold"))
            {
                if (isAbsoluteMVP && stat.FireDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_SOUP_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_SOUP_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                        new Color(1f, 0.85f, 0.2f), 1100));
                }
                if (isAbsoluteMVP && stat.DamageTaken >= baseline * 4f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_MVP_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_MVP_TANK_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(1f, 0.8f, 0.3f), 1100));
                }
                if (isAbsoluteMVP && stat.HealingDone >= baseline * 2f && stat.TotalDamage >= baseline && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_MVP_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_MVP_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 0.9f, 0.5f), 1100));
                }
                if (stat.BludgeoningDmg >= baseline * 2f && stat.CC_Prone >= 3 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_BLUDG_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_BLUDG_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.7f, 0.5f), 550));
                }
                if (stat.HolyDmg >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_HOLY_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_HOLY_DESC", stat, divineLore, divineLoreSubject, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.9f, 0.6f), 550));
                }
                if (stat.AttacksDodged >= 10 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.9f, 0.8f, 0.5f), 350));
                }
                if (stat.MaxSingleHit >= baseline * 2f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_ONESHOT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_ONESHOT_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.8f, 0.6f, 0.3f), 350));
                }
                if (stat.CC_Prone >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_PRONE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_PRONE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Prone.ToString()), 
                        new Color(0.8f, 0.7f, 0.4f), 200));
                }
                if (stat.HealingDone >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_HEAL_DESC", stat, divineLore, divineLoreSubject, stat.HealingDone.ToString()), 
                        new Color(1f, 1f, 0.7f), 200));
                }
                if (stat.Crits >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DRAGON_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DRAGON_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        Color.yellow, 200));
                }
            }

            // ---------------------------------------------------------
            // 8. LA VOIE DE LA LÉGENDE (VOLONTÉ MORTELLE & FORGE)
            // ---------------------------------------------------------
            if (path.Contains("légende") || path.Contains("legend"))
            {
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 4f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_MVP_WILL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_MVP_WILL_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.85f, 0.85f, 0.85f), 1100));
                }
                if (isAbsoluteMVP && totalKills >= 10 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_MVP_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_MVP_KILLS_DESC", stat, divineLore, divineLoreSubject, null, totalKills.ToString()), 
                        new Color(0.75f, 0.75f, 0.75f), 1100));
                }
                if (isAbsoluteMVP && stat.Crits >= 10 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_MVP_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_MVP_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        new Color(0.95f, 0.95f, 0.95f), 1100));
                }
                if (stat.DamageTaken >= baseline * 3f && stat.AttacksDodged >= 8 && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_RESIL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_RESIL_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.65f, 0.65f, 0.65f), 550));
                }
                if (stat.MaxSingleHit >= baseline * 3f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_ANVIL_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_ANVIL_DESC", stat, divineLore, divineLoreSubject, stat.MaxSingleHit.ToString()), 
                        new Color(0.7f, 0.7f, 0.7f), 550));
                }
                if (stat.SlashingDmg >= baseline * 2f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_SLASH_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_SLASH_DESC", stat, divineLore, divineLoreSubject, stat.SlashingDmg.ToString()), 
                        new Color(0.8f, 0.8f, 0.8f), 350));
                }
                if (stat.AoOs >= 6 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        new Color(0.5f, 0.5f, 0.5f), 350));
                }
                if (stat.PiercingDmg >= baseline && stat.DamageTaken == 0 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_SNIPER_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_SNIPER_DESC", stat, divineLore, divineLoreSubject, stat.PiercingDmg.ToString()), 
                        new Color(0.6f, 0.8f, 0.6f), 200));
                }
                if (stat.CC_Prone >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_PRONE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_PRONE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.CC_Prone.ToString()), 
                        new Color(0.5f, 0.4f, 0.3f), 200));
                }
                if (stat.CC_DeathsDoor > 0 && stat.Kills >= 2 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_LEGEND_DEATH_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_LEGEND_DEATH_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.8f, 0.1f, 0.1f), 200));
                }
            }

            // ---------------------------------------------------------
            // 9. L'ESSAIM-QUI-MARCHE (DÉVORATION & NUÉE)
            // ---------------------------------------------------------
            if (path.Contains("essaim") || path.Contains("swarm"))
            {
                if (isAbsoluteMVP && stat.TotalDamage >= baseline * 3f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_HUNGER_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_HUNGER_DESC", stat, divineLore, divineLoreSubject, stat.TotalDamage.ToString()), 
                        new Color(0.7f, 0.6f, 0.1f), 1100));
                }
                if (isAbsoluteMVP && stat.OverkillDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_DEVOUR_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_DEVOUR_DESC", stat, divineLore, divineLoreSubject, stat.OverkillDmg.ToString()), 
                        new Color(0.6f, 0.5f, 0f), 1100));
                }
                if (isAbsoluteMVP && stat.StatDamage >= 30 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_FLEAU_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_FLEAU_DESC", stat, divineLore, divineLoreSubject, null, null, stat.StatDamage.ToString()), 
                        new Color(0.5f, 0.5f, 0.1f), 1100));
                }
                if (stat.SummonDamage >= baseline * 2f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_SUMMON_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_SUMMON_DESC", stat, divineLore, divineLoreSubject, stat.SummonDamage.ToString()), 
                        new Color(0.5f, 0.4f, 0.1f), 550));
                }
                if (stat.VampiricHealing >= baseline * 1.5f && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_VAMP_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_VAMP_DESC", stat, divineLore, divineLoreSubject, stat.VampiricHealing.ToString()), 
                        new Color(0.8f, 0.1f, 0.1f), 550));
                }
                if (stat.AttacksDodged >= 12 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.6f, 0.5f, 0.2f), 350));
                }
                if (stat.AcidDmg >= baseline * 1.5f && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_ACID_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_ACID_DESC", stat, divineLore, divineLoreSubject, stat.AcidDmg.ToString()), 
                        new Color(0.3f, 0.8f, 0.1f), 350));
                }
                if (stat.CC_Sickened + stat.CC_Nauseated >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_SICK_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_SICK_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.4f, 0.6f, 0.1f), 200));
                }
                if (stat.PiercingDmg >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_PIERCE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_PIERCE_DESC", stat, divineLore, divineLoreSubject, stat.PiercingDmg.ToString()), 
                        new Color(0.7f, 0.6f, 0.3f), 200));
                }
                if (stat.StatDamage >= 10 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_SWARM_STAT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_SWARM_STAT_DESC", stat, divineLore, divineLoreSubject, stat.StatDamage.ToString()), 
                        new Color(0.5f, 0.5f, 0.2f), 200));
                }
            }

            // ---------------------------------------------------------
            // 10. LA VOIE DU DIABLE (CONTRAT & ENFERS)
            // ---------------------------------------------------------
            if (path.Contains("diable") || path.Contains("devil"))
            {
                if (isAbsoluteMVP && stat.UnholyDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_CONTRACT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_CONTRACT_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.8f, 0.3f, 0.1f), 1100));
                }
                if (isAbsoluteMVP && stat.InstaKills >= 2 && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_LAW_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_LAW_DESC", stat, divineLore, divineLoreSubject, null, null, stat.InstaKills.ToString()), 
                        new Color(0.7f, 0.1f, 0.1f), 1100));
                }
                if (isAbsoluteMVP && stat.FireDmg >= baseline * 2f && canGetSSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_FIRE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_FIRE_DESC", stat, divineLore, divineLoreSubject, stat.FireDmg.ToString()), 
                        new Color(0.9f, 0.2f, 0f), 1100));
                }
                if ((stat.StatDamage >= 15 || stat.NegativeLevels >= 4) && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_CLAUSE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_CLAUSE_DESC", stat, divineLore, divineLoreSubject), 
                        new Color(0.5f, 0.1f, 0.4f), 550));
                }
                if (stat.DamageTaken >= baseline * 2f && stat.HealingDone >= baseline && canGetSS)
                {
                    stat.Achievements.Add(new MVPAchievement("SS+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_LAWYER_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_LAWYER_DESC", stat, divineLore, divineLoreSubject, stat.DamageTaken.ToString()), 
                        new Color(0.6f, 0.5f, 0.5f), 550));
                }
                if (stat.CC_Confused + stat.CC_Stunned >= 4 && canGetS)
                {
                    int totalCCVal = stat.CC_Confused + stat.CC_Stunned;
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_COURT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_COURT_DESC", stat, divineLore, divineLoreSubject, null, null, totalCCVal.ToString()), 
                        new Color(0.7f, 0.4f, 0.7f), 350));
                }
                if (stat.AoOs >= 5 && canGetS)
                {
                    stat.Achievements.Add(new MVPAchievement("S+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_AOO_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AoOs.ToString()), 
                        new Color(0.6f, 0.4f, 0.4f), 350));
                }
                if (stat.AttacksDodged >= 8 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_DODGE_DESC", stat, divineLore, divineLoreSubject, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.5f, 0.6f), 200));
                }
                if (stat.UnholyDmg >= baseline && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_UNHOLY_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_UNHOLY_DESC", stat, divineLore, divineLoreSubject, stat.UnholyDmg.ToString()), 
                        new Color(0.5f, 0f, 0.2f), 200));
                }
                if (stat.Crits >= 3 && canGetA)
                {
                    stat.Achievements.Add(new MVPAchievement("A+", 
                        Localization.GetStringById("ACH_MYTH_DEVIL_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_MYTH_DEVIL_CRIT_DESC", stat, divineLore, divineLoreSubject, null, null, stat.Crits.ToString()), 
                        Color.red, 200));
                }
            }
			
		}
			
			// =========================================================================
        // LES 50 SUCCÈS DE DIFFICULTÉ (CORRIGÉS MAJEURS - 100% SÉCURISÉ)
        // =========================================================================
        private static void GrantDifficultyAchievements(UnitCombatStats stat, float baseline, bool isAbsoluteMVP) {
            var difficultySettings = Kingmaker.Settings.SettingsRoot.Difficulty;
            float dmgToParty = difficultySettings != null ? difficultySettings.DamageToParty.GetValue() : 1.0f;
            bool isIronman = difficultySettings != null ? difficultySettings.OnlyOneSave.GetValue() : false;

            string diffName = "Normal";
            if (dmgToParty >= 1.9f) diffName = "Unfair";
            else if (dmgToParty >= 1.4f) diffName = "Hard";
            else if (dmgToParty >= 0.95f) diffName = "Core";
            else if (dmgToParty >= 0.85f) diffName = "Daring";

            bool isVirtualUnfair = dmgToParty >= 1.9f;
            bool isVirtualHard = dmgToParty >= 1.4f && dmgToParty < 1.9f;
            bool isVirtualCore = dmgToParty >= 0.95f && dmgToParty < 1.4f;
            bool isVirtualDaring = dmgToParty >= 0.85f && dmgToParty < 0.95f;
            bool isVirtualNormal = dmgToParty >= 0.65f && dmgToParty < 0.85f;

            string pronoun = stat.Gender == Gender.Female ? "Elle" : "Il";
            string pronounObj = stat.Gender == Gender.Female ? "elle" : "lui";
            string accord = stat.Gender == Gender.Female ? "e" : "";

            int totalKills = stat.Kills + stat.SummonKills;
            int totalCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + stat.CC_Confused + stat.CC_Prone;

            bool canGetSSS = stat.Grade.Contains("SSS");
            bool canGetSS  = stat.Grade.Contains("SS") || canGetSSS;
            bool canGetS   = stat.Grade.StartsWith("S") || stat.Grade == "A+"; 
            bool canGetA   = stat.Grade.StartsWith("A") || canGetS;

            // ---------------------------------------------------------
            // BLOC A : DIFFICULTÉ "INJUSTE" (UNFAIR)
            // ---------------------------------------------------------
            if (isVirtualUnfair) {
                if (isAbsoluteMVP && stat.Grade.Contains("SSS")) {
                    stat.Achievements.Add(new MVPAchievement("SSS+", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_MVP_DESC", stat, null, null, null, null, stat.Grade), 
                        new Color(1f, 0.3f, 0.3f), 1200));
                }
                if (stat.TotalDamage >= baseline * 3f && canGetSS) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_DMG_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_DMG_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(0.9f, 0.2f, 0.2f), 500));
                }
                if (stat.DamageTaken >= baseline * 3f) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_TANK_DESC", stat, null, null, stat.DamageTaken.ToString()), 
                        new Color(0.3f, 0.5f, 0.8f), 500));
                }
                if (stat.SneakAttackDmg >= baseline && stat.DamageTaken == 0) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_SNEAK_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_SNEAK_DESC", stat, null, null, stat.SneakAttackDmg.ToString()), 
                        new Color(0.4f, 0.4f, 0.5f), 400));
                }
                if (totalCC >= 4 && canGetS) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_CC_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_CC_DESC", stat, null, null, null, null, totalCC.ToString()), 
                        new Color(0.6f, 0.3f, 0.7f), 410));
                }
                if (stat.SlashingDmg >= baseline * 2f && totalKills >= 3) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_KILLS_DESC", stat, null, null, null, totalKills.ToString()), 
                        new Color(0.8f, 0.1f, 0.2f), 400));
                }
                if (stat.HealingDone >= baseline * 2f) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_HEAL_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_HEAL_DESC", stat, null, null, stat.HealingDone.ToString()), 
                        new Color(1f, 0.9f, 0.5f), 420));
                }
                if (stat.AttacksDodged >= 12) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_DODGE_DESC", stat, null, null, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.2f, 0.8f, 0.6f), 390));
                }
                if (stat.Crits >= 5) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_CRIT_DESC", stat, null, null, null, null, stat.Crits.ToString()), 
                        Color.yellow, 380));
                }
                if (stat.UnitData != null && stat.UnitData.IsPet && stat.TotalDamage >= baseline) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_UNFAIR_PET_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_UNFAIR_PET_DESC", stat, null, null), 
                        new Color(0.4f, 0.8f, 0.4f), 400));
                }
            }

            // ---------------------------------------------------------
            // BLOC B : DIFFICULTÉ "DIFFICILE" (HARD)
            // ---------------------------------------------------------
            if (isVirtualHard) {
                if (isAbsoluteMVP && stat.Grade.Contains("SS")) {
                    stat.Achievements.Add(new MVPAchievement("SS", 
                        Localization.GetStringById("ACH_DIFF_HARD_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_MVP_DESC", stat, null, null), 
                        new Color(1f, 0.7f, 0.2f), 600));
                }
                if (stat.TotalDamage >= baseline * 2f && stat.AllPhysDmg == 0) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_HARD_MAG_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_MAG_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(0.5f, 0.3f, 0.9f), 350));
                }
                if (stat.BludgeoningDmg >= baseline * 1.5f && stat.CC_Prone >= 3) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_HARD_BLUDG_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_BLUDG_DESC", stat, null, null, stat.BludgeoningDmg.ToString(), null, stat.CC_Prone.ToString()), 
                        new Color(0.7f, 0.5f, 0.3f), 320));
                }
                if (stat.AoOs >= 5) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_HARD_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_AOO_DESC", stat, null, null, null, null, stat.AoOs.ToString()), 
                        new Color(0.6f, 0.6f, 0.6f), 250));
                }
                if (isAbsoluteMVP && stat.DamageTaken == 0) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_HARD_FLAWLESS_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_FLAWLESS_DESC", stat, null, null), 
                        new Color(0.8f, 0.8f, 0.9f), 340));
                }
                if (stat.OverkillDmg >= baseline) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_HARD_OVERKILL_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_OVERKILL_DESC", stat, null, null, stat.OverkillDmg.ToString()), 
                        new Color(0.8f, 0.2f, 0.2f), 220));
                }
                if (stat.DamageTaken >= baseline * 2f && stat.AttacksDodged >= 4) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_HARD_TANK_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_TANK_DESC", stat, null, null, stat.DamageTaken.ToString()), 
                        new Color(0.4f, 0.6f, 0.8f), 300));
                }
                if (stat.VampiricHealing >= baseline * 0.5f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_HARD_VAMP_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_VAMP_DESC", stat, null, null, stat.VampiricHealing.ToString()), 
                        new Color(0.7f, 0f, 0.2f), 240));
                }
                if (stat.PiercingDmg >= baseline * 1.5f && stat.DamageTaken == 0) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_HARD_SNIPER_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_SNIPER_DESC", stat, null, null, stat.PiercingDmg.ToString()), 
                        new Color(0.4f, 0.8f, 0.4f), 260));
                }
                if (stat.StatDamage >= 15 || stat.NegativeLevels >= 4) {
                    int totalDmgVal = stat.StatDamage + stat.NegativeLevels;
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_HARD_DRAIN_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_HARD_DRAIN_DESC", stat, null, null, totalDmgVal.ToString()), 
                        new Color(0.5f, 0.1f, 0.6f), 310));
                }
            }

            // ---------------------------------------------------------
            // BLOC C : DIFFICULTÉ "CORE" (RÈGLES DE BASE)
            // ---------------------------------------------------------
            if (isVirtualCore) {
                if (isAbsoluteMVP && stat.Grade.Contains("S")) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_CORE_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_MVP_DESC", stat, null, null), 
                        new Color(0.2f, 0.8f, 0.2f), 400));
                }
                if (stat.FireDmg + stat.ColdDmg + stat.ElectricDmg >= baseline * 1.5f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_ELEMENT_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_ELEMENT_DESC", stat, null, null), 
                        new Color(0.2f, 0.8f, 1f), 200));
                }
                if (stat.TotalDamage >= baseline * 2f && stat.MythicPathName == (Localization.GetStringById("ui.mythic_hero") ?? "Héros Mythique")) {
                    stat.Achievements.Add(new MVPAchievement("S", 
                        Localization.GetStringById("ACH_DIFF_CORE_LEGEND_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_LEGEND_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(0.7f, 0.7f, 0.7f), 310));
                }
                if (stat.DamageTaken >= baseline * 1.5f && stat.HealingDone >= baseline * 0.5f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_MARTYR_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_MARTYR_DESC", stat, null, null, stat.DamageTaken.ToString()), 
                        new Color(1f, 0.9f, 0.6f), 240));
                }
                if (stat.Crits >= 4 && canGetS) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_CRIT_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_CRIT_DESC", stat, null, null, null, null, stat.Crits.ToString()), 
                        Color.yellow, 210));
                }
                if (totalKills >= 4 && stat.DamageTaken == 0) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_BLITZ_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_BLITZ_DESC", stat, null, null, null, totalKills.ToString()), 
                        new Color(0.4f, 0.7f, 0.4f), 230));
                }
                if (stat.BludgeoningDmg >= baseline && stat.CC_Prone >= 2) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_SHIELD_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_SHIELD_DESC", stat, null, null), 
                        new Color(0.6f, 0.5f, 0.4f), 200));
                }
                if (stat.SneakAttackDmg >= baseline * 0.8f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_SNEAK_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_SNEAK_DESC", stat, null, null, stat.SneakAttackDmg.ToString()), 
                        new Color(0.3f, 0.3f, 0.3f), 210));
                }
                if (stat.SlashPierceDmg > 0 || stat.SlashBludgeonDmg > 0 || stat.PierceBludgeonDmg > 0) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_HYBRID_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_HYBRID_DESC", stat, null, null), 
                        new Color(0.7f, 0.7f, 0.8f), 200));
                }
                if (stat.IconicSpellsCast >= 1) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_CORE_DEATH_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_CORE_DEATH_DESC", stat, null, null), 
                        new Color(0.5f, 0.1f, 0.5f), 220));
                }
            }

            // ---------------------------------------------------------
            // BLOC D : DIFFICULTÉ "DARING" (AUDACIEUX)
            // ---------------------------------------------------------
            if (isVirtualDaring) {
                if (isAbsoluteMVP) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_MVP_DESC", stat, null, null), 
                        new Color(0.4f, 0.8f, 0.8f), 200));
                }
                if (stat.MaxSingleHit >= baseline * 1.5f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_BOOM_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_BOOM_DESC", stat, null, null, stat.MaxSingleHit.ToString()), 
                        Color.magenta, 180));
                }
                if (stat.AoOs >= 4 && stat.AttacksDodged >= 4) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_AOO_DESC", stat, null, null), 
                        new Color(0.6f, 0.8f, 0.6f), 190));
                }
                if (stat.HolyDmg >= baseline * 0.5f && stat.FireDmg >= baseline * 0.5f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_HOLYFIRE_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_HOLYFIRE_DESC", stat, null, null), 
                        new Color(1f, 0.7f, 0.2f), 190));
                }
                if (stat.UnitData != null && stat.UnitData.IsPet && totalKills >= 3) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_PET_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_PET_DESC", stat, null, null), 
                        new Color(0.4f, 0.8f, 0.4f), 170));
                }
                if (stat.AttacksDodged >= 8) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_DODGE_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_DODGE_DESC", stat, null, null, null, null, stat.AttacksDodged.ToString()), 
                        new Color(0.5f, 0.9f, 0.7f), 160));
                }
                if (stat.AcidDmg >= baseline * 0.8f) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_ACID_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_ACID_DESC", stat, null, null, stat.AcidDmg.ToString()), 
                        new Color(0.3f, 0.8f, 0.1f), 170));
                }
                if (stat.HolyDmg >= baseline) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_HOLY_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_HOLY_DESC", stat, null, null, stat.HolyDmg.ToString()), 
                        new Color(1f, 0.9f, 0.4f), 170));
                }
                if (stat.CC_Sickened + stat.CC_Nauseated >= 3) {
                    stat.Achievements.Add(new MVPAchievement("B", 
                        Localization.GetStringById("ACH_DIFF_DARING_SICK_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_SICK_DESC", stat, null, null), 
                        new Color(0.4f, 0.6f, 0.1f), 120));
                }
                if (stat.DamageTaken >= baseline && stat.CC_DeathsDoor > 0) {
                    stat.Achievements.Add(new MVPAchievement("A", 
                        Localization.GetStringById("ACH_DIFF_DARING_DEATHSDOOR_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_DARING_DEATHSDOOR_DESC", stat, null, null), 
                        new Color(0.6f, 0.1f, 0.1f), 190));
                }
            }

            // ---------------------------------------------------------
            // BLOC E : DIFFICULTÉ "NORMAL"
            // ---------------------------------------------------------
            if (isVirtualNormal) {
                if (isAbsoluteMVP) {
                    stat.Achievements.Add(new MVPAchievement("B", 
                        Localization.GetStringById("ACH_DIFF_NORMAL_MVP_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_NORMAL_MVP_DESC", stat, null, null), 
                        new Color(0.7f, 0.9f, 1f), 100));
                }
                if (stat.TotalDamage >= baseline && stat.DamageTaken == 0) {
                    stat.Achievements.Add(new MVPAchievement("B", 
                        Localization.GetStringById("ACH_DIFF_NORMAL_FLAWLESS_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_NORMAL_FLAWLESS_DESC", stat, null, null), 
                        new Color(0.6f, 0.8f, 0.9f), 120));
                }
                if (stat.TotalDamage >= baseline * 2f) {
                    stat.Achievements.Add(new MVPAchievement("B", 
                        Localization.GetStringById("ACH_DIFF_NORMAL_DMG_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_NORMAL_DMG_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                        new Color(0.8f, 0.5f, 0.4f), 110));
                }
                if (totalKills >= 4) {
                    stat.Achievements.Add(new MVPAchievement("B", 
                        Localization.GetStringById("ACH_DIFF_NORMAL_KILLS_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_NORMAL_KILLS_DESC", stat, null, null, null, totalKills.ToString()), 
                        new Color(0.8f, 0.3f, 0.3f), 110));
                }
                if (stat.AoOs >= 3) {
                    stat.Achievements.Add(new MVPAchievement("C", 
                        Localization.GetStringById("ACH_DIFF_NORMAL_AOO_TITLE"), 
                        Localization.GetFormatted("ACH_DIFF_NORMAL_AOO_DESC", stat, null, null, null, null, stat.AoOs.ToString()), 
                        Color.gray, 80));
                }
            }

            // ---------------------------------------------------------
            // BLOC F : CRITÈRES SPÉCIAUX & CUSTOM
            // ---------------------------------------------------------
            if (isIronman && stat.TotalDamage >= baseline) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_DIFF_IRONMAN_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_IRONMAN_DESC", stat, null, null, stat.TotalDamage.ToString()), 
                    new Color(1f, 0.5f, 0f), 550));
            }
            if (diffName.Equals("Custom", StringComparison.OrdinalIgnoreCase) && dmgToParty >= 1.5f) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_DIFF_CUSTOM_DMG_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_CUSTOM_DMG_DESC", stat, null, null, null, null, dmgToParty.ToString()), 
                    new Color(0.9f, 0.4f, 0.2f), 350));
            }
            if (diffName.Equals("Custom", StringComparison.OrdinalIgnoreCase) && dmgToParty >= 1.0f) {
                stat.Achievements.Add(new MVPAchievement("S", 
                    Localization.GetStringById("ACH_DIFF_CUSTOM_BUFF_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_CUSTOM_BUFF_DESC", stat, null, null), 
                    new Color(0.5f, 0.5f, 0.8f), 350));
            }
            if (isIronman && stat.Grade == "R") {
                stat.Achievements.Add(new MVPAchievement("A", 
                    Localization.GetStringById("ACH_DIFF_IRONMAN_RESERVE_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_IRONMAN_RESERVE_DESC", stat, null, null), 
                    new Color(0.6f, 0.7f, 0.8f), 150));
            }
            if ((isVirtualUnfair || isVirtualHard) && stat.CC_DeathsDoor > 0 && totalKills >= 1) {
                stat.Achievements.Add(new MVPAchievement("SS", 
                    Localization.GetStringById("ACH_DIFF_HARDCORE_RECOVERY_TITLE"), 
                    Localization.GetFormatted("ACH_DIFF_HARDCORE_RECOVERY_DESC", stat, null, null), 
                    new Color(0.8f, 0.1f, 0.1f), 480));
            }
        }
		
    }
}