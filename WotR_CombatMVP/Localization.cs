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
        /// </summary>
        public static string GetFormatted(string key, UnitCombatStats stat, string damage = null, string kills = null, string extra = null)
        {
            string template = GetStringById(key);
            if (string.IsNullOrEmpty(template)) return key;

            // Détecte si la langue actuelle du jeu est l'anglais
            bool isEnglish = LocalizationManager.CurrentLocale == Locale.enGB;

            // Configuration adaptative et croisée des pronoms et accords de genre
            string pronoun = stat.Gender == Kingmaker.Blueprints.Gender.Female 
                ? (isEnglish ? "She" : "Elle") 
                : (isEnglish ? "He" : "Il");

            string pronounObj = stat.Gender == Kingmaker.Blueprints.Gender.Female 
                ? (isEnglish ? "her" : "elle") 
                : (isEnglish ? "him" : "lui");

            string accord = stat.Gender == Kingmaker.Blueprints.Gender.Female 
                ? (isEnglish ? "" : "e") 
                : "";

            string titlePrefix = stat.Gender == Kingmaker.Blueprints.Gender.Female 
                ? (isEnglish ? "The " : "La ") 
                : (isEnglish ? "The " : "Le ");

            template = template.Replace("{pronoun}", pronoun);
            template = template.Replace("{pronounObj}", pronounObj);
            template = template.Replace("{accord}", accord);
            template = template.Replace("{titlePrefix}", titlePrefix);

            // --- INJECTIONS DYNAMIQUES NOMINATIVES ET TÉLÉMÉTRIQUES ---
            template = template.Replace("{name}", stat.Name ?? "");
            template = template.Replace("{damageTaken}", stat.DamageTaken.ToString());
            template = template.Replace("{physicalDmgTaken}", stat.PhysicalDmgTaken.ToString());
            template = template.Replace("{attacksDodged}", stat.AttacksDodged.ToString());
            template = template.Replace("{hitsPhysicalTaken}", stat.HitsPhysicalTaken.ToString());
            template = template.Replace("{savesFortFailed}", stat.SavesFortFailed.ToString());
            template = template.Replace("{savesFortSucceeded}", stat.SavesFortSucceeded.ToString());
            template = template.Replace("{savesRefFailed}", stat.SavesRefFailed.ToString());
            template = template.Replace("{savesRefSucceeded}", stat.SavesRefSucceeded.ToString());
            template = template.Replace("{savesWillFailed}", stat.SavesWillFailed.ToString());
            template = template.Replace("{savesWillSucceeded}", stat.SavesWillSucceeded.ToString());
			template = template.Replace("{attacksAttempted}", stat.AttacksAttempted.ToString());
            template = template.Replace("{attacksLanded}", stat.AttacksLanded.ToString());
            template = template.Replace("{spellsResistedCount}", stat.SpellsResistedCount.ToString());
            template = template.Replace("{friendlyFireDmg}", stat.FriendlyFireDmg.ToString());
            template = template.Replace("{overkillDmg}", stat.OverkillDmg.ToString());
            template = template.Replace("{totalDamage}", (stat.TotalDamage + stat.SummonDamage).ToString());
            template = template.Replace("{healingDone}", (stat.HealingDone + stat.VampiricHealing).ToString());
            template = template.Replace("{crits}", stat.Crits.ToString());
            template = template.Replace("{aoos}", stat.AoOs.ToString());
			// --- INJECTIONS COMPLÉMENTAIRES POUR LES IMPRESSIVE ACHIEVEMENTS ---
            template = template.Replace("{maxSingleHit}", stat.MaxSingleHit.ToString());
            template = template.Replace("{vampiricHealing}", stat.VampiricHealing.ToString());
            template = template.Replace("{statDamage}", stat.StatDamage.ToString());
            template = template.Replace("{negativeLevels}", stat.NegativeLevels.ToString());
            template = template.Replace("{summonsCount}", stat.SummonsCount.ToString());
            template = template.Replace("{summonDamage}", stat.SummonDamage.ToString());
            template = template.Replace("{dispelledCount}", stat.DispelledCount.ToString());
            template = template.Replace("{trippedCount}", stat.TrippedCount.ToString());
            template = template.Replace("{scrollsCast}", stat.ScrollsCastCount.ToString());
            template = template.Replace("{supportBuffsCast}", stat.SupportBuffsCast.ToString());

            // Calcul dynamique unifié du cumul des Crowd Control (CC) appliqués
            int totalCC = stat.CC_Prone + stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened + 
                          stat.CC_Shaken + stat.CC_Cowering + stat.CC_Nauseated + stat.CC_Sickened + 
                          stat.CC_Blinded + stat.CC_Entangled + stat.CC_Confused + stat.CC_Exhausted + 
                          stat.CC_Fatigued + stat.CC_Slowed + stat.CC_Staggered + stat.CC_Dazed + 
                          stat.CC_Dazzled + stat.CC_Helpless + stat.CC_Cowering + stat.CC_DeathsDoor;
            template = template.Replace("{totalCC}", totalCC.ToString());

            // Remplacements historiques pour rétrocompatibilité
            if (damage != null) template = template.Replace("{damage}", damage);
            if (kills != null) template = template.Replace("{kills}", kills);
            if (extra != null) template = template.Replace("{extra}", extra);

            return template;
        }
    }
}