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

        // --- COMPTEURS COMPLÉMENTAIRES v1.4.1 ---
        public int SummonsCount = 0;
        public int DispelledCount = 0;
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
		private Vector2 leftScrollPosition; // --- NOUVEAU : Position de défilement de la colonne de gauche ---

        // --- NOUVEAU : Le Mur de Verre ---
        private GameObject invisibleGlassWall;
		// --- NOUVEAU : Barre de recherche et filtrage dynamique ---
        private string searchQuery = "";
        private List<UnitCombatStats> filteredCombatants = new List<UnitCombatStats>();

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
                // CORRECTION UX : On ne bloque plus l'ouverture si combatStats.Count == 0. 
                // On laisse l'UI s'ouvrir pour montrer au joueur que le mod fonctionne bien !
                if (Main.Tracker == null) return; 

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
            if (Game.Instance?.Player == null) return; // Garde-fou d'initialisation tardive
            Localization.Init(Main.ModPath);
            
            var activeParty = Game.Instance.Player.PartyAndPets;
            
            // --- SYNCHRONISATION EN TEMPS RÉEL (TOYBOX & RESPEC PROOF) ---
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
                var fallbackEnemies = Main.Tracker.combatStats.Values
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

            DrawProceduralFrame(safeArea, wotrDarkSlate, 2f);

            Rect innerGoldRect = new Rect(safeArea.x + 5f, safeArea.y + 5f, safeArea.width - 10f, safeArea.height - 10f);
            DrawProceduralFrame(innerGoldRect, wotrGold, 1f);

            DrawWotRCornerBrackets(innerGoldRect, wotrGold, 2f, 15f);

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
            GUILayout.Space(15);

            // Début du conteneur de défilement gauche (Scrollbar automatique si débordement d'échelle)
            leftScrollPosition = GUILayout.BeginScrollView(leftScrollPosition, GUILayout.Width(leftZoneWidth), GUILayout.Height(height - 80));

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

            // Masqué si l'analyse pure est active
            if (!SettingsManager.Current.ShowDebriefView)
            {
                GUILayout.Label(Localization.GetStringById("ui.operational_rank") ?? "RANG OPÉRATIONNEL", new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.3490f, 0.4980f, 0.5882f) } });
                GUILayout.Label($"{currentStat.Grade}", new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, normal = { textColor = GetGradeColor(currentStat.Grade) } });
            }
            
            GUILayout.EndScrollView(); // --- NOUVEAU : Fin du conteneur de défilement gauche ---
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
            GUILayout.Label(string.Format(Localization.GetStringById("ui.damage_done") ?? " <b>Dégâts Infligés :</b> <color={0}>{1}</color>", colText, currentStat.TotalDamage), statStyle);
            
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

                if (currentStat.SummonDamage > 0 || currentStat.SummonKills > 0)
                {
                    string summonsLabel = Localization.GetStringById("ui.telemetry.summons_label") ?? "Summons:";
                    string dmgShortLabel = Localization.GetStringById("ui.telemetry.damage_short_label") ?? "damage";
                    string killsShortLabel = Localization.GetStringById("ui.telemetry.kills_short_label") ?? "kills";
                    GUILayout.Label($"  • <color={colText}>{summonsLabel}</color> <color={colSub}>{currentStat.SummonDamage} {dmgShortLabel} | {currentStat.SummonKills} {killsShortLabel}</color>", detailStyle);
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
                var topAchievements = currentStat.Achievements.OrderByDescending(a => a.Weight).Take(15).ToList();
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

        Color GetGradeColor(string grade)
        {
            if (grade.Contains("S")) return HexToColor("#C4A265"); // Or Royal
            if (grade.Contains("A")) return HexToColor("#597F96"); // Ardoise Neutre
            if (grade.Contains("B")) return HexToColor("#6D8467"); // Vert Sauge
            if (grade.Contains("C")) return HexToColor("#6D8467"); // Vert Sauge
            if (grade.Contains("F")) return HexToColor("#9E1B1B"); // Rouge brique / Sang séché
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

            // Unification de la logique d'alliance sous la variable existante "currentlyAlly"
            var mainChar = Game.Instance.Player.MainCharacter.Value;
            bool currentlyAlly = trueInitiator.IsPlayerFaction || (mainChar != null && (trueInitiator.IsAlly(mainChar) || !trueInitiator.IsEnemy(mainChar)));

            string name = trueInitiator.CharacterName ?? "Inconnu";
            
            // On vérifie si la fiche existe déjà pour comparer le changement d'attitude en temps réel
            bool isSwapped = false;
            if (combatStats.ContainsKey(name))
            {
                isSwapped = combatStats[name].OriginallyAlly != currentlyAlly;
            }

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
                    OriginallyAlly = currentlyAlly, // Enregistré à l'instanciation de la fiche
                    IsDominatedSheet = isSwapped,
                    Level = trueInitiator.Progression?.CharacterLevel ?? 1,
                    CR = trueInitiator.Blueprint?.CR ?? 1, 
                    MythicPathName = mythic,
                    MythicPathInternalName = mythicInternal, 
                    IsEvil = isEvil,
                    Gender = trueInitiator.Gender
                };
            }
            else
            {
                // Mise à jour de l'attitude courante si la fiche existe déjà
                var existingStats = combatStats[statsKey];
                existingStats.IsAlly = currentlyAlly;
                // Préserve l'état de domination s'il a déjà été acté, ou si une nouvelle bascule survient
                existingStats.IsDominatedSheet = existingStats.IsDominatedSheet || isSwapped;
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
                    
                    // UTILISATION DE LA PROPRIÉTÉ GLOBALE HasRealContribution POUR UNE VÉRIFICATION ÉTANCHE
                    if (stat.IsAlly && isMinorSkirmish && !stat.HasRealContribution)
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
                var targetStats = GetOrAddStats(evt.Target, out _);
                if (targetStats != null) 
                {
                    targetStats.DamageTaken += damage; // Accumulation globale (Allié et Ennemi) !
                    
                    // Capture ciblée des dégâts physiques reçus (Pour tout le monde : Alliés et Ennemis) !
                    if (evt.DamageBundle != null)
                    {
                        foreach (var dv in evt.ResultList ?? new List<Kingmaker.RuleSystem.Rules.Damage.DamageValue>())
                        {
                            int finalForChunk = dv.FinalValue;
                            if (finalForChunk <= 0) continue;
                            if (dv.Source is Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage)
                            {
                                targetStats.PhysicalDmgTaken += finalForChunk;
                            }
                        }
                    }

                    if (targetStats.IsAlly)
                    {
                        if (evt.Initiator.IsPlayerFaction && evt.Initiator != evt.Target)
                        {
                            var initStats = GetOrAddStats(evt.Initiator, out _);
                            if (initStats != null) initStats.FriendlyFireDmg += damage;
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

                // --- DÉTECTION DU MOMENT EXAC DE LA CHUTE DE L'ALLIÉ ---
                if (targetStats != null && targetStats.IsAlly && evt.Target.HPLeft <= 0 && (evt.Target.HPLeft + evt.Result) > 0)
                {
                    targetStats.TimesDowned++;
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
                    var stats = GetOrAddStats(evt.Initiator, out _);
                    if (stats != null)
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
                    var targetStats = GetOrAddStats(evt.Target, out _);
                    if (targetStats != null)
                    {
                        if (evt.IsHit)
                        {
                            targetStats.HitsPhysicalTaken++;
                        }
                        else
                        {
                            targetStats.AttacksDodged++;
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
                if (target == null) return;

                // --- CAPTURE DES DEBUFFS (EFFETS NÉFASTES) SUBIS PAR UN MEMBRE DU GROUPE ---
                if (target.IsPlayerFaction && buff.Blueprint.Harmful)
                {
                    var targetStats = GetOrAddStats(target, out _);
                    if (targetStats != null)
                    {
                        string debuffName = buff.Blueprint.Name;
                        if (!string.IsNullOrEmpty(debuffName))
                        {
                            targetStats.SufferedDebuffs.Add(debuffName);
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

                        // Évaluation Ange Gardien : PV de la cible inférieurs à 30% lors du déclenchement
                        if (evt.Target != null && evt.Target.HPLeft > 0 && evt.Target.HPLeft < (evt.Target.MaxHP * 0.3f))
                        {
                            stats.GuardianAngelHealing += evt.Value;
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
                if (!isCombatActive || evt.Initiator == null) return;
                
                // --- CAPTURE DES SÉCURITÉS POUR LE DÉBRIEFING (ALLIÉS ET ENNEMIS) ---
                var statsSec = GetOrAddStats(evt.Initiator, out _);
                if (statsSec != null)
                {
                    if (!evt.IsPassed)
                    {
                        statsSec.SavesFailed++;
                        
                        // Extraction sécurisée du nom de l'effet d'origine avec cascade de replis pour contrer les chaînes vides ""
                        string sourceName = null;
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

                        if (evt.Type == SavingThrowType.Fortitude)
                        {
                            statsSec.SavesFortFailed++;
                            IncrementSourceCount(statsSec.SavesFortFailedSources, sourceName);
                        }
                        else if (evt.Type == SavingThrowType.Reflex)
                        {
                            statsSec.SavesRefFailed++;
                            IncrementSourceCount(statsSec.SavesRefFailedSources, sourceName);
                        }
                        else if (evt.Type == SavingThrowType.Will)
                        {
                            statsSec.SavesWillFailed++;
                            IncrementSourceCount(statsSec.SavesWillFailedSources, sourceName);
                        }
                    }
                    else
                    {
                        statsSec.SavesSucceeded++;
                        if (evt.Type == SavingThrowType.Fortitude) statsSec.SavesFortSucceeded++;
                        else if (evt.Type == SavingThrowType.Reflex) statsSec.SavesRefSucceeded++;
                        else if (evt.Type == SavingThrowType.Will) statsSec.SavesWillSucceeded++;
                    }
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
                var stats = GetOrAddStats(evt.Initiator, out _);
                if (stats != null)
                {
                    stats.DispelledCount++;
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
            try
            {
                if (!isCombatActive || unit == null) return;
                var activeUnit = Game.Instance.TurnBasedCombatController?.CurrentTurn?.SelectedUnit;
                if (activeUnit == null)
                {
                    activeUnit = Game.Instance.Player.MainCharacter.Value;
                }
                if (activeUnit != null)
                {
                    var stats = GetOrAddStats(activeUnit, out _);
                    if (stats != null)
                    {
                        stats.ResurrectedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error("[CombatMVP] Erreur dans HandleUnitResurrected : " + ex.Message);
            }
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
                                killerStats.InstaKills++;
                                if (isSummon) killerStats.SummonKills++;
                                else killerStats.Kills++;

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
                                if (isSummon) killerStats.SummonKills++;
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
            bool isSkald = stat.UnitData != null && stat.UnitData.Progression.Classes.Any(c => c.CharacterClass.name.ToLower().Contains("skald"));
            if (isSkald && stat.SupportBuffsCast >= 15)
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
            bool actuallyUsedSmiteEvil = stat.DamageModifiersAudit.Values.Any(modDict => 
                modDict.Keys.Any(k => k.IndexOf("smite evil", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                      k.IndexOf("châtiment du mal", StringComparison.OrdinalIgnoreCase) >= 0));

            if (actuallyUsedSmiteEvil && (stat.TotalDamage + stat.SummonDamage) >= (partyLevel * 25))
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
        }
    }
}