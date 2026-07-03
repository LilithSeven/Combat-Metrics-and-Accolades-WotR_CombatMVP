using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WotR_CombatMVP
{
    // Agrégat persistant d'un personnage sur l'ensemble d'un run (campagne).
    public class LedgerCharacter
    {
        public string Name = "";
        public long TotalDamage = 0;
        public long TotalHealing = 0;
        public long TotalDamageTaken = 0;
        public int TotalKills = 0;
        public int TotalCC = 0;
        public int TimesDowned = 0;
        public int Combats = 0;
        public int MaxSingleHit = 0;
        public float BestScore = 0f;
        public string BestGrade = "";
        // Nombre de fois que ce personnage a décroché chaque note (pour afficher son top 3 dans le registre).
        public Dictionary<string, int> GradeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    // Agrégat persistant d'un ennemi sur l'ensemble d'un run (pour la "némésis").
    public class LedgerEnemy
    {
        public string Name = "";
        public long DamageToParty = 0;
        public int KillsOnParty = 0;
    }

    public class RunLedger
    {
        public string RunId = "";
        public string CampaignName = "";
        public int TotalCombats = 0;
        public string LastUpdated = "";
        public Dictionary<string, LedgerCharacter> Characters = new Dictionary<string, LedgerCharacter>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, LedgerEnemy> Enemies = new Dictionary<string, LedgerEnemy>(StringComparer.OrdinalIgnoreCase);
    }

    // Persistance SÛRE POUR LES SAUVEGARDES : le registre est stocké dans un JSON dédié au mod
    // (UserSettings/history/<runId>.json). Le mod n'écrit JAMAIS dans les fichiers de sauvegarde du jeu.
    public static class LedgerManager
    {
        private static string s_Dir;
        private static string s_CurrentRunId;
        public static RunLedger Current { get; private set; } = new RunLedger();

        public static void Init(string modPath)
        {
            try
            {
                if (string.IsNullOrEmpty(modPath)) return;
                s_Dir = Path.Combine(Path.Combine(modPath, "UserSettings"), "history");
                if (!Directory.Exists(s_Dir)) Directory.CreateDirectory(s_Dir);
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[CombatMVP] Erreur d'initialisation du registre : " + ex.Message);
            }
        }

        private static string SanitizeFile(string id)
        {
            if (string.IsNullOrEmpty(id)) return "default";
            foreach (var c in Path.GetInvalidFileNameChars()) id = id.Replace(c, '_');
            if (id.Length > 80) id = id.Substring(0, 80);
            return id;
        }

        // Charge (ou crée) le registre du run courant. Idempotent tant que le runId ne change pas.
        public static void EnsureRun(string runId, string campaignName)
        {
            try
            {
                if (string.IsNullOrEmpty(runId)) runId = "default";
                if (s_CurrentRunId == runId && Current != null)
                {
                    if (!string.IsNullOrEmpty(campaignName)) Current.CampaignName = campaignName;
                    return;
                }
                s_CurrentRunId = runId;

                string path = string.IsNullOrEmpty(s_Dir) ? null : Path.Combine(s_Dir, SanitizeFile(runId) + ".json");
                if (path != null && File.Exists(path))
                {
                    var loaded = JsonConvert.DeserializeObject<RunLedger>(File.ReadAllText(path));
                    Current = loaded ?? new RunLedger();
                }
                else
                {
                    Current = new RunLedger();
                }
                if (Current.Characters == null) Current.Characters = new Dictionary<string, LedgerCharacter>(StringComparer.OrdinalIgnoreCase);
                if (Current.Enemies == null) Current.Enemies = new Dictionary<string, LedgerEnemy>(StringComparer.OrdinalIgnoreCase);
                Current.RunId = runId;
                if (!string.IsNullOrEmpty(campaignName)) Current.CampaignName = campaignName;
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[CombatMVP] Erreur EnsureRun du registre : " + ex.Message);
                Current = new RunLedger { RunId = runId };
            }
        }

        public static void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(s_Dir) || Current == null || string.IsNullOrEmpty(s_CurrentRunId)) return;
                Current.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                string path = Path.Combine(s_Dir, SanitizeFile(s_CurrentRunId) + ".json");
                File.WriteAllText(path, JsonConvert.SerializeObject(Current, Formatting.Indented));
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[CombatMVP] Erreur d'enregistrement du registre : " + ex.Message);
            }
        }

        public static void ResetCurrent()
        {
            try
            {
                if (Current == null) return;
                if (Current.Characters != null) Current.Characters.Clear();
                if (Current.Enemies != null) Current.Enemies.Clear();
                Current.TotalCombats = 0;
                Save();
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[CombatMVP] Erreur de réinitialisation du registre : " + ex.Message);
            }
        }

        public static LedgerEnemy GetNemesis()
        {
            if (Current == null || Current.Enemies == null) return null;
            LedgerEnemy best = null;
            foreach (var e in Current.Enemies.Values)
            {
                if (best == null || e.DamageToParty > best.DamageToParty) best = e;
            }
            return best;
        }
    }
}
