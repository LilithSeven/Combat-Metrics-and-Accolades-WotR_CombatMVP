using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

namespace WotR_CombatMVP
{
    public static class Localization
    {
        private static Dictionary<string, string> s_Localizations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Classe de stockage miroir pour la désérialisation Newtonsoft.Json
        private class LocData
        {
            [JsonProperty("strings")]
            public Dictionary<string, string> strings { get; set; }
        }

        public static void Init(string modPath)
        {
            if (string.IsNullOrEmpty(modPath)) return;

            s_Localizations.Clear();

            var locDir = Path.Combine(modPath, "Localization");
            
            // Le fichier anglais (enGB.json) sert de base de repli absolue
            var fallbackFile = Path.Combine(locDir, "enGB.json");
            if (File.Exists(fallbackFile))
            {
                LoadFileIntoDictionary(fallbackFile);
            }

            try
            {
                if (Kingmaker.Game.Instance == null) return;
                Locale currentLocale = LocalizationManager.CurrentLocale;
                string localeFileName = GetFileNameForLocale(currentLocale);

                // Si la langue du jeu n'est pas l'anglais, on fusionne avec la langue locale (ex: frFR.json)
                if (localeFileName != "enGB.json")
                {
                    var localeFile = Path.Combine(locDir, localeFileName);
                    if (File.Exists(localeFile))
                    {
                        LoadFileIntoDictionary(localeFile);
                    }
                }
            }
            catch (Exception)
            {
                // Silencieusement ignore durant l'initialisation precoce d'UMM
            }
        }

        private static void LoadFileIntoDictionary(string filePath)
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<LocData>(jsonText);
                if (data != null && data.strings != null)
                {
                    foreach (var kvp in data.strings)
                    {
                        s_Localizations[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null)
                {
                    Main.Logger.Error("[CombatMVP] Erreur d'analyse JSON pour le fichier : " + filePath + " | " + ex.Message);
                }
            }
        }

        private static string GetFileNameForLocale(Locale locale)
        {
            // Retourne directement "enGB.json", "frFR.json", "deDE.json", etc.
            return locale.ToString() + ".json";
        }

        public static string GetStringById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            s_Localizations.TryGetValue(id, out string value);
            return value;
        }

        /// <summary>
        /// Extrait et formate une chaîne de caractères en remplaçant les jetons dynamiques de genre, d'accord et d'environnement.
        /// VERSION OPTIMISÉE (Zéro-Allocation sur les jetons absents)
        /// </summary>
        public static string GetFormatted(string key, UnitCombatStats stat, string damage = null, string kills = null, string extra = null)
        {
            string template = GetStringById(key);
            if (string.IsNullOrEmpty(template)) return key;
            if (stat == null) return template; // SÉCURITÉ ANTI-CRASH

            // Résolution des pronoms et accords de genre pour CHACUNE des 8 langues prises en charge.
            // Auparavant seul l'anglais était distingué et toutes les autres langues retombaient sur le
            // français (ex: un joueur espagnol voyait "lui/elle" au lieu de "él/ella").
            bool fem = stat.Gender == Kingmaker.Blueprints.Gender.Female;
            string pronoun, pronounObj, accord, titlePrefix;
            switch (LocalizationManager.CurrentLocale)
            {
                case Locale.frFR:
                    pronoun = fem ? "Elle" : "Il";
                    pronounObj = fem ? "elle" : "lui";
                    accord = fem ? "e" : "";
                    titlePrefix = fem ? "La " : "Le ";
                    break;
                case Locale.deDE:
                    pronoun = fem ? "Sie" : "Er";
                    pronounObj = fem ? "sie" : "ihn";
                    accord = "";
                    titlePrefix = fem ? "Die " : "Der ";
                    break;
                case Locale.esES:
                    pronoun = fem ? "Ella" : "Él";
                    pronounObj = fem ? "ella" : "él";
                    accord = fem ? "a" : "o";
                    titlePrefix = fem ? "La " : "El ";
                    break;
                case Locale.itIT:
                    pronoun = fem ? "Lei" : "Lui";
                    pronounObj = fem ? "lei" : "lui";
                    accord = fem ? "a" : "o";
                    titlePrefix = fem ? "La " : "Il ";
                    break;
                case Locale.ptBR:
                    pronoun = fem ? "Ela" : "Ele";
                    pronounObj = fem ? "ela" : "ele";
                    accord = fem ? "a" : "o";
                    titlePrefix = fem ? "A " : "O ";
                    break;
                case Locale.ruRU:
                    pronoun = fem ? "Она" : "Он";
                    pronounObj = fem ? "её" : "его";
                    accord = fem ? "а" : "";
                    titlePrefix = "";
                    break;
                case Locale.zhCN:
                    pronoun = fem ? "她" : "他";
                    pronounObj = fem ? "她" : "他";
                    accord = "";
                    titlePrefix = "";
                    break;
                case Locale.enGB:
                default:
                    pronoun = fem ? "She" : "He";
                    pronounObj = fem ? "her" : "him";
                    accord = "";
                    titlePrefix = "The ";
                    break;
            }

            if (template.Contains("{pronoun}")) template = template.Replace("{pronoun}", pronoun);
            if (template.Contains("{pronounObj}")) template = template.Replace("{pronounObj}", pronounObj);
            if (template.Contains("{accord}")) template = template.Replace("{accord}", accord);
            if (template.Contains("{titlePrefix}")) template = template.Replace("{titlePrefix}", titlePrefix);

            // --- INJECTIONS DYNAMIQUES (Le ToString() n'est appelé que si la balise est vraiment là) ---
            if (template.Contains("{name}")) template = template.Replace("{name}", stat.Name ?? "");
            if (template.Contains("{damageTaken}")) template = template.Replace("{damageTaken}", stat.DamageTaken.ToString());
            if (template.Contains("{physicalDmgTaken}")) template = template.Replace("{physicalDmgTaken}", stat.PhysicalDmgTaken.ToString());
            if (template.Contains("{attacksDodged}")) template = template.Replace("{attacksDodged}", stat.AttacksDodged.ToString());
            if (template.Contains("{hitsPhysicalTaken}")) template = template.Replace("{hitsPhysicalTaken}", stat.HitsPhysicalTaken.ToString());
            if (template.Contains("{savesFortFailed}")) template = template.Replace("{savesFortFailed}", stat.SavesFortFailed.ToString());
            if (template.Contains("{savesFortSucceeded}")) template = template.Replace("{savesFortSucceeded}", stat.SavesFortSucceeded.ToString());
            if (template.Contains("{savesRefFailed}")) template = template.Replace("{savesRefFailed}", stat.SavesRefFailed.ToString());
            if (template.Contains("{savesRefSucceeded}")) template = template.Replace("{savesRefSucceeded}", stat.SavesRefSucceeded.ToString());
            if (template.Contains("{savesWillFailed}")) template = template.Replace("{savesWillFailed}", stat.SavesWillFailed.ToString());
            if (template.Contains("{savesWillSucceeded}")) template = template.Replace("{savesWillSucceeded}", stat.SavesWillSucceeded.ToString());
            if (template.Contains("{attacksAttempted}")) template = template.Replace("{attacksAttempted}", stat.AttacksAttempted.ToString());
            if (template.Contains("{attacksLanded}")) template = template.Replace("{attacksLanded}", stat.AttacksLanded.ToString());
            if (template.Contains("{spellsResistedCount}")) template = template.Replace("{spellsResistedCount}", stat.SpellsResistedCount.ToString());
            if (template.Contains("{friendlyFireDmg}")) template = template.Replace("{friendlyFireDmg}", stat.FriendlyFireDmg.ToString());
            if (template.Contains("{overkillDmg}")) template = template.Replace("{overkillDmg}", stat.OverkillDmg.ToString());
            if (template.Contains("{totalDamage}")) template = template.Replace("{totalDamage}", (stat.TotalDamage + stat.SummonDamage).ToString());
            if (template.Contains("{healingDone}")) template = template.Replace("{healingDone}", (stat.HealingDone + stat.VampiricHealing).ToString());
            if (template.Contains("{crits}")) template = template.Replace("{crits}", stat.Crits.ToString());
            if (template.Contains("{aoos}")) template = template.Replace("{aoos}", stat.AoOs.ToString());
            if (template.Contains("{kills}")) template = template.Replace("{kills}", stat.Kills.ToString());
            if (template.Contains("{maxBurst}")) template = template.Replace("{maxBurst}", stat.MaxBurstDamage.ToString());
            if (template.Contains("{highDangerKills}")) template = template.Replace("{highDangerKills}", stat.HighDangerKills.ToString());

            // --- INJECTIONS COMPLÉMENTAIRES ---
            if (template.Contains("{maxSingleHit}")) template = template.Replace("{maxSingleHit}", stat.MaxSingleHit.ToString());
            if (template.Contains("{vampiricHealing}")) template = template.Replace("{vampiricHealing}", stat.VampiricHealing.ToString());
            if (template.Contains("{statDamage}")) template = template.Replace("{statDamage}", stat.StatDamage.ToString());
            if (template.Contains("{negativeLevels}")) template = template.Replace("{negativeLevels}", stat.NegativeLevels.ToString());
            if (template.Contains("{summonsCount}")) template = template.Replace("{summonsCount}", stat.SummonsCount.ToString());
            if (template.Contains("{summonDamage}")) template = template.Replace("{summonDamage}", stat.SummonDamage.ToString());
            if (template.Contains("{dispelledCount}")) template = template.Replace("{dispelledCount}", stat.DispelledCount.ToString());
            if (template.Contains("{trippedCount}")) template = template.Replace("{trippedCount}", stat.TrippedCount.ToString());
            if (template.Contains("{scrollsCast}")) template = template.Replace("{scrollsCast}", stat.ScrollsCastCount.ToString());
            if (template.Contains("{supportBuffsCast}")) template = template.Replace("{supportBuffsCast}", stat.SupportBuffsCast.ToString());

            // Calcul dynamique unifié du cumul des Crowd Control (CC) : Uniquement s'il est requis !
            if (template.Contains("{totalCC}"))
            {
                int totalCC = stat.CC_Prone + stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + 
                              stat.CC_Shaken + stat.CC_Cowering + stat.CC_Nauseated + stat.CC_Sickened + 
                              stat.CC_Blinded + stat.CC_Entangled + stat.CC_Confused + stat.CC_Exhausted + 
                              stat.CC_Fatigued + stat.CC_Slowed + stat.CC_Staggered + stat.CC_Dazed + 
                              stat.CC_Dazzled + stat.CC_Helpless + stat.CC_DeathsDoor;
                template = template.Replace("{totalCC}", totalCC.ToString());
            }

            // Remplacements historiques pour rétrocompatibilité
            if (damage != null && template.Contains("{damage}")) template = template.Replace("{damage}", damage);
            if (kills != null && template.Contains("{kills}")) template = template.Replace("{kills}", kills);
            if (extra != null && template.Contains("{extra}")) template = template.Replace("{extra}", extra);

            return template;
        }
    }
}