using System;
using System.Collections.Generic;
using UnityEngine;

namespace WotR_CombatMVP
{
    public class TacticalAdvice
    {
        public string Tier { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Color Color { get; set; }

        public TacticalAdvice(string tier, string title, string description, Color color)
        {
            Tier = tier;
            Title = title;
            Description = description;
            Color = color;
        }
    }

    public static class TacticalDebriefing
    {
        public static List<TacticalAdvice> GenerateAdvice(UnitCombatStats stat, int partyLevel)
        {
            var list = new List<TacticalAdvice>();
            if (stat == null) return list;

            int totalDamage = stat.TotalDamage + stat.SummonDamage;

            if (stat.IsAlly)
            {
                // ====================================================================
                // ─── SECTION A : LES 16 CONSEILS ADAPTATIFS ET CHIFFRÉS AUX ALLIÉS ───
                // ====================================================================

                // 1. Alerte : Classe d'Armure Insuffisante (Dégâts physiques vs Évitement)
                int attacksDirected = stat.HitsPhysicalTaken + stat.AttacksDodged;
                if (attacksDirected >= 5)
                {
                    float dodgeRatio = (float)stat.AttacksDodged / attacksDirected;
                    // Déclenché si le personnage esquive moins de 30% des coups et a subi des dégâts physiques notables pour son niveau
                    if (dodgeRatio < 0.30f && stat.PhysicalDmgTaken > (partyLevel * 20))
                    {
                        string title = Localization.GetStringById("debrief.ac.title") ?? "Alerte : Classe d'Armure Insuffisante";
                        string rawDesc = Localization.GetStringById("debrief.ac.desc") ?? "{name} subit d'importants dégâts physiques ({physicalDmgTaken} dégâts sur {hitsPhysicalTaken} coups reçus, taux d'esquive de {attacksDodged}/{attacksDirected}). Pensez à améliorer la Classe d'Armure (CA) de {name} via l'équipement, des dons défensifs ou des sorts d'illusion protecteurs (Flou, Déplacement, Image Miroir).";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{physicalDmgTaken}", stat.PhysicalDmgTaken.ToString())
                            .Replace("{hitsPhysicalTaken}", stat.HitsPhysicalTaken.ToString())
                            .Replace("{attacksDodged}", stat.AttacksDodged.ToString())
                            .Replace("{attacksDirected}", attacksDirected.ToString());

                        list.Add(new TacticalAdvice("⚠️", title, desc, new Color(0.9f, 0.4f, 0.2f)));
                    }
                }

                // 2. Alerte : Faiblesse en Vigueur
                int totalFortSaves = stat.SavesFortFailed + stat.SavesFortSucceeded;
                if (totalFortSaves >= 3)
                {
                    float failureRatio = (float)stat.SavesFortFailed / totalFortSaves;
                    if (failureRatio > 0.50f && stat.SavesFortFailed >= 2)
                    {
                        string title = Localization.GetStringById("debrief.fort.title") ?? "Alerte : Faiblesse en Vigueur";
                        string rawDesc = Localization.GetStringById("debrief.fort.desc") ?? "{name} a échoué à la majorité de ses jets de Vigueur ({savesFortFailed} échecs sur {totalFortSaves} jets). Augmentez la Constitution de {name} (ceintures), prenez le don Volonté de fer ou utilisez des sorts de résistance (Résistance, Héroïsme).";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesFortFailed}", stat.SavesFortFailed.ToString())
                            .Replace("{totalFortSaves}", totalFortSaves.ToString());

                        list.Add(new TacticalAdvice("🩸", title, desc, new Color(0.8f, 0.2f, 0.2f)));
                    }
                }

                // 3. Alerte : Faiblesse en Réflexes
                int totalRefSaves = stat.SavesRefFailed + stat.SavesRefSucceeded;
                if (totalRefSaves >= 3)
                {
                    float failureRatio = (float)stat.SavesRefFailed / totalRefSaves;
                    if (failureRatio > 0.50f && stat.SavesRefFailed >= 2)
                    {
                        string title = Localization.GetStringById("debrief.ref.title") ?? "Alerte : Faiblesse en Réflexes";
                        string rawDesc = Localization.GetStringById("debrief.ref.desc") ?? "{name} a échoué à la majorité de ses jets de Réflexes ({savesRefFailed} échecs sur {totalRefSaves} jets). Améliorez les Réflexes de {name} avec le sort Hâte, Grâce féline, ou des équipements de Dextérité.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesRefFailed}", stat.SavesRefFailed.ToString())
                            .Replace("{totalRefSaves}", totalRefSaves.ToString());

                        list.Add(new TacticalAdvice("🍃", title, desc, new Color(0.2f, 0.8f, 0.6f)));
                    }
                }

                // 4. Alerte : Faiblesse Mentale (Volonté)
                int totalWillSaves = stat.SavesWillFailed + stat.SavesWillSucceeded;
                if (totalWillSaves >= 3)
                {
                    float failureRatio = (float)stat.SavesWillFailed / totalWillSaves;
                    if (failureRatio > 0.50f && stat.SavesWillFailed >= 2)
                    {
                        string title = Localization.GetStringById("debrief.will.title") ?? "Alerte : Faiblesse Mentale (Volonté)";
                        string rawDesc = Localization.GetStringById("debrief.will.desc") ?? "{name} a échoué à la majorité de ses jets de Volonté ({savesWillFailed} échecs sur {totalWillSaves} jets). Renforcez la Volonté de {name} via des objets de Sagesse, le sort Héroïsme ou Protection contre le mal.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesWillFailed}", stat.SavesWillFailed.ToString())
                            .Replace("{totalWillSaves}", totalWillSaves.ToString());

                        list.Add(new TacticalAdvice("🔮", title, desc, new Color(0.6f, 0.2f, 0.8f)));
                    }
                }

                // 5. Alerte : Résistance Magique Ennemie Subie
                if (stat.SpellsResistedCount >= 2)
                {
                    string title = Localization.GetStringById("debrief.sr.title") ?? "Alerte : Résistance Magique Ennemie";
                    string rawDesc = Localization.GetStringById("debrief.sr.desc") ?? "Plusieurs sortilèges de {name} ({spellsResistedCount}) ont été bloqués par la résistance magique adverse. Pensez à acquérir les dons de Pénétration de sorts (Spell Penetration) ou à utiliser des sortilèges ignorant la résistance magique.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{spellsResistedCount}", stat.SpellsResistedCount.ToString());

                    list.Add(new TacticalAdvice("⚡", title, desc, new Color(1.0f, 0.85f, 0.2f)));
                }

                // 6. Alerte : Vigilance de Survie (Inconscience)
                if (stat.TimesDowned >= 1)
                {
                    string title = Localization.GetStringById("debrief.survival.title") ?? "Alerte : Vigilance de Survie";
                    string rawDesc = Localization.GetStringById("debrief.survival.desc") ?? "{name} est tombé{accord} inconscient{accord} ({timesDowned} fois) durant cet affrontement. Ajustez le placement tactique de {name}, améliorez sa Constitution ou lancez des protections préventives (Protection contre la mort, Invisibilité).";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{accord}", stat.Gender == Kingmaker.Blueprints.Gender.Female ? "e" : "")
                        .Replace("{timesDowned}", stat.TimesDowned.ToString());

                    list.Add(new TacticalAdvice("💀", title, desc, new Color(0.9f, 0.1f, 0.1f)));
                }

                // 7. Alerte : Dégâts de Tir Allié Élevés (Friendly Fire)
                if (stat.FriendlyFireDmg > (partyLevel * 15) && stat.FriendlyFireDmg > totalDamage * 0.15f)
                {
                    string title = Localization.GetStringById("debrief.ally.friendly_fire.title") ?? "Alerte : Catastrophe de Tir Allié";
                    string rawDesc = Localization.GetStringById("debrief.ally.friendly_fire.desc") ?? "Les sorts de zone de {name} ont infligé d'importants dégâts à votre propre équipe ({friendlyFireDmg} dégâts infligés au groupe). Pensez à utiliser le don de métamagie 'Sort sélectif' (Selective Spell) ou à mieux cibler vos lancers.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{friendlyFireDmg}", stat.FriendlyFireDmg.ToString());

                    list.Add(new TacticalAdvice("🔥", title, desc, new Color(1.0f, 0.2f, 0.2f)));
                }

                // 8. Alerte : Overkill Inefficace (Gaspillage de DPS)
                if (totalDamage > (partyLevel * 15) && stat.OverkillDmg > totalDamage * 0.35f && stat.Kills < 2)
                {
                    string title = Localization.GetStringById("debrief.ally.overkill_waste.title") ?? "Alerte : Overkill Tactique Inefficace";
                    string rawDesc = Localization.GetStringById("debrief.ally.overkill_waste.desc") ?? "{name} inflige un immense surplus de dégâts sur des cibles déjà condamnées ({overkillDmg} dégâts d'overkill pour seulement {kills} éliminations). Redirigez les attaques lourdes de {name} vers des ennemis pleins de vie.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{overkillDmg}", stat.OverkillDmg.ToString())
                        .Replace("{kills}", stat.Kills.ToString());

                    list.Add(new TacticalAdvice("⚔️", title, desc, new Color(0.7f, 0.7f, 0.7f)));
                }

                // 9. Alerte : Attaque Sournoise Non Déclenchée
                if (totalDamage > (partyLevel * 15) && stat.SneakAttackDmg > 0 && stat.SneakAttackDmg < totalDamage * 0.15f)
                {
                    string title = Localization.GetStringById("debrief.ally.low_sneak.title") ?? "Alerte : Précision Sournoise Manquée";
                    string rawDesc = Localization.GetStringById("debrief.ally.low_sneak.desc") ?? "Une infime partie des dégâts physiques de {name} bénéficie de ses attaques sournoises ({sneakAttackDmg} dégâts sur {totalDamage} totaux). Assurez-vous de coordonner {name} pour flanker activement ses cibles.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{sneakAttackDmg}", stat.SneakAttackDmg.ToString())
                        .Replace("{totalDamage}", totalDamage.ToString());

                    list.Add(new TacticalAdvice("🗡️", title, desc, new Color(1.0f, 0.6f, 0.2f)));
                }

                // 10. Alerte : Gaspillage de Soins (Overhealing)
                int totalHeal = stat.HealingDone + stat.VampiricHealing;
                if (totalHeal > (partyLevel * 25) && stat.TimesDowned == 0 && stat.DamageTaken < totalHeal * 0.3f)
                {
                    string title = Localization.GetStringById("debrief.ally.overhealing.title") ?? "Alerte : Gaspillage de Soins (Overhealing)";
                    string rawDesc = Localization.GetStringById("debrief.ally.overhealing.desc") ?? "{name} a généré un volume de soins élevé ({healingDone} PV soignés) alors que l'équipe n'a subi que très peu de dommages ({damageTaken} dégâts subis). Ajustez le timing des soins pour économiser vos ressources.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{healingDone}", totalHeal.ToString())
                        .Replace("{damageTaken}", stat.DamageTaken.ToString());

                    list.Add(new TacticalAdvice("👼", title, desc, new Color(0.4f, 0.8f, 0.9f)));
                }

                // 11. Alerte : Siphon Vampirique Inutile
                if (stat.VampiricHealing > (partyLevel * 5) && stat.DamageTaken == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.vampiric_waste.title") ?? "Alerte : Siphon Vampirique Inutile";
                    string rawDesc = Localization.GetStringById("debrief.ally.vampiric_waste.desc") ?? "{name} a absorbé {healingDone} PV via des effets vampiriques alors que sa santé était déjà maximale (0 dégât subi). Conservez ces compétences pour les situations de combat direct.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{healingDone}", stat.VampiricHealing.ToString());

                    list.Add(new TacticalAdvice("🧛", title, desc, new Color(0.8f, 0.1f, 0.3f)));
                }

                // 12. Alerte : Opportunités Perdues (Melee spécifique sans AoO)
                bool isMeleeCombatant = (stat.SlashingDmg > 0 || stat.BludgeoningDmg > 0);
                if (isMeleeCombatant && stat.Crits >= 4 && stat.AoOs == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.low_aoo.title") ?? "Alerte : Opportunités Perdues au Combat";
                    string rawDesc = Localization.GetStringById("debrief.ally.low_aoo.desc") ?? "{name} a infligé {crits} coups critiques au corps-à-corps mais n'a déclenché aucune attaque d'opportunité. Considérez l'acquisition du don 'Réflexes de combat' pour capitaliser sur les ouvertures ennemies.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{crits}", stat.Crits.ToString());

                    list.Add(new TacticalAdvice("👣", title, desc, new Color(0.6f, 0.6f, 0.6f)));
                }

                // 13. Alerte : Verrouillage Mental Continu (CC subis)
                int totalHardCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Confused + stat.CC_Asleep;
                if (totalHardCC >= 2)
                {
                    string title = Localization.GetStringById("debrief.ally.cc_locked.title") ?? "Alerte : Verrouillage Mental Continu";
                    string rawDesc = Localization.GetStringById("debrief.ally.cc_locked.desc") ?? "{name} a passé plusieurs tours paralysé, étourdi ou endormi (total de {ccLocked} entraves subies). Pensez à appliquer préventivement des sorts comme 'Liberté de mouvement' ou 'Cœur indomptable'.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{ccLocked}", totalHardCC.ToString());

                    list.Add(new TacticalAdvice("⛓️", title, desc, new Color(0.5f, 0.5f, 0.8f)));
                }

                // 14. Alerte : Passage au Seuil de la Mort
                if (stat.CC_DeathsDoor >= 1)
                {
                    string title = Localization.GetStringById("debrief.ally.deaths_door.title") ?? "Alerte : Chute au Seuil de la Mort";
                    string rawDesc = Localization.GetStringById("debrief.ally.deaths_door.desc") ?? "{name} a franchi le Seuil de la Mort. Assurez-vous d'appliquer le sort 'Protection contre la mort' (Death Ward) pour immuniser {name} contre la nécrose ou les drains d'énergie destructeurs.";
                    
                    string desc = rawDesc.Replace("{name}", stat.Name);

                    list.Add(new TacticalAdvice("☠️", title, desc, new Color(0.9f, 0.2f, 0.1f)));
                }

                // 15. Alerte : Blocage de la Pénétration Arcanique (Lanceur magique vs SR)
                if (totalDamage > (partyLevel * 10) && stat.AllPhysDmg == 0 && stat.SpellsResistedCount >= 2)
                {
                    string title = Localization.GetStringById("debrief.ally.magical_reliance.title") ?? "Alerte : Blocage de la Pénétration Arcanique";
                    string rawDesc = Localization.GetStringById("debrief.ally.magical_reliance.desc") ?? "La puissance offensive de {name} est freinée par la résistance magique ennemie ({spellsResistedCount} sorts résistés). Privilégiez des sorts de Conjuration ignorant la SR ou investissez dans les dons de pénétration.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{spellsResistedCount}", stat.SpellsResistedCount.ToString());

                    list.Add(new TacticalAdvice("🔮", title, desc, new Color(0.7f, 0.3f, 0.9f)));
                }

                // 16. Alerte : Blocage de la Réduction de Dégâts (Martial vs DR)
                if (totalDamage > (partyLevel * 10) && stat.AllPhysDmg > 0 && stat.MaxSingleHit > 0 && stat.MaxSingleHit < (5 + partyLevel))
                {
                    string title = Localization.GetStringById("debrief.ally.physical_res.title") ?? "Alerte : Blocage par Réduction de Dégâts (DR)";
                    string rawDesc = Localization.GetStringById("debrief.ally.physical_res.desc") ?? "Les attaques physiques de {name} butent contre la réduction de dégâts (DR) adverse (plus puissant coup à {maxSingleHit} dégâts). Utilisez des types de fer adaptés ou alignez vos armes.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{maxSingleHit}", stat.MaxSingleHit.ToString());

                    list.Add(new TacticalAdvice("🛡️", title, desc, new Color(0.5f, 0.5f, 0.5f)));
                }

                // 26. Conseil : Pénétration d'Acide Sous-Optimisée
                if (totalDamage > (partyLevel * 15) && stat.AcidDmg > 0 && stat.AcidDmg < totalDamage * 0.1f)
                {
                    string title = Localization.GetStringById("debrief.ally.acid_underused.title") ?? "Conseil : Pénétration d'Acide Sous-Optimisée";
                    string rawDesc = Localization.GetStringById("debrief.ally.acid_underused.desc") ?? "L'acide de {name} n'a représenté qu'une part infime de ses dégâts ({acidDmg} dégâts acides sur {totalDamage} totaux). C'est pourtant une énergie très efficace contre la majorité des démons.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{acidDmg}", stat.AcidDmg.ToString())
                        .Replace("{totalDamage}", totalDamage.ToString());

                    list.Add(new TacticalAdvice("🧪", title, desc, new Color(0.3f, 0.8f, 0.2f)));
                }

                // 27. Conseil : Choix Judicieux du Son (Sonic)
                if (stat.SonicDmg > (partyLevel * 5))
                {
                    string title = Localization.GetStringById("debrief.ally.sonic_supremacy.title") ?? "Optimisation : Choix Judicieux du Son (Sonic)";
                    string rawDesc = Localization.GetStringById("debrief.ally.sonic_supremacy.desc") ?? "Excellent choix pour {name} ({sonicDmg} dégâts sonores infligés). Très peu de démons de la Plaie possèdent une résistance au Son, ce qui en fait votre arme arcanique la plus fiable.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{sonicDmg}", stat.SonicDmg.ToString());

                    list.Add(new TacticalAdvice("🎵", title, desc, new Color(0.6f, 0.4f, 0.9f)));
                }

                // 28. Alerte : Synergie Secoué/Briseur de Défenses
                if (stat.CC_Shaken >= 2 && stat.CC_Frightened == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.shaken_vulnerability.title") ?? "Alerte : Synergie Secoué (Shaken) Manquée";
                    string rawDesc = Localization.GetStringById("debrief.ally.shaken_vulnerability.desc") ?? "{name} a appliqué l'état Secoué à {shakenCount} reprises mais vos guerriers n'en ont pas tiré parti. Pensez à leur faire acquérir le don 'Briseur de défenses' (Shatter Defenses) pour cibler la CA d'esquive.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{shakenCount}", stat.CC_Shaken.ToString());

                    list.Add(new TacticalAdvice("💀", title, desc, new Color(1.0f, 0.5f, 0.0f)));
                }

                // 29. Conseil : Optimisation du Placement des Invocations
                if (stat.SummonDamage > 0 && stat.SummonKills == 0 && stat.DamageTaken > (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.ally.summon_meat_shield.title") ?? "Conseil : Placement des Invocations";
                    string rawDesc = Localization.GetStringById("debrief.ally.summon_meat_shield.desc") ?? "Les créatures invoquées par {name} ont infligé des dégâts ({summonDamage} dégâts) mais n'ont pas absorbé les menaces à sa place ({damageTaken} dégâts subis par {name}). Positionnez-les plus en avant.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{summonDamage}", stat.SummonDamage.ToString())
                        .Replace("{damageTaken}", stat.DamageTaken.ToString());

                    list.Add(new TacticalAdvice("🦴", title, desc, new Color(0.5f, 0.5f, 0.5f)));
                }

                // 30. Alerte : Faiblesse du Multiplicateur de Dégâts de Critique
                if (stat.Crits >= 5 && totalDamage < (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.ally.precision_crit_miss.title") ?? "Alerte : Faiblesse des Dégâts de Critique";
                    string rawDesc = Localization.GetStringById("debrief.ally.precision_crit_miss.desc") ?? "{name} a confirmé {crits} coups critiques mais son total de dégâts reste extrêmement faible ({totalDamage} dégâts). Augmentez ses dégâts physiques de base ou optimisez ses enchantements.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{crits}", stat.Crits.ToString())
                        .Replace("{totalDamage}", totalDamage.ToString());

                    list.Add(new TacticalAdvice("🎯", title, desc, new Color(0.9f, 0.6f, 0.1f)));
                }

                // 31. Conseil : Optimisation des Dégâts Sacrés
                if (totalDamage > (partyLevel * 10) && stat.HolyDmg > 0 && stat.HolyDmg > totalDamage * 0.5f)
                {
                    string title = Localization.GetStringById("debrief.ally.holy_smite_efficiency.title") ?? "Optimisation : Puissance Sacrée Maximisée";
                    string rawDesc = Localization.GetStringById("debrief.ally.holy_smite_efficiency.desc") ?? "Excellente optimisation sur {name} ({holyDmg} dégâts sacrés). Les dégâts Sacrés ignorent totalement les résistances élémentaires et la réduction de dégâts (DR) démoniaque. Poursuivez sur cette voie.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{holyDmg}", stat.HolyDmg.ToString());

                    list.Add(new TacticalAdvice("☀️", title, desc, new Color(1.0f, 0.9f, 0.4f)));
                }

                // 32. Conseil : Exploitation de la Cécité (Blindness)
                if (stat.CC_Blinded >= 1 && stat.DamageTaken > (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.ally.blindness_miss_chance.title") ?? "Conseil : Exploitation de la Cécité";
                    string rawDesc = Localization.GetStringById("debrief.ally.blindness_miss_chance.desc") ?? "{name} a appliqué l'état Aveuglé à {blindedCount} cibles mais a continué à subir des dégâts lourds ({damageTaken} dégâts subis). Mettez de la distance pour forcer le jet de chance de rater (50%).";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{blindedCount}", stat.CC_Blinded.ToString())
                        .Replace("{damageTaken}", stat.DamageTaken.ToString());

                    list.Add(new TacticalAdvice("🕶️", title, desc, new Color(0.3f, 0.5f, 0.7f)));
                }

                // 33. Conseil : Exploitation Tactique de l'État À Terre (Prone)
                if (stat.CC_Prone >= 3 && stat.AoOs == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.prone_opportunity.title") ?? "Conseil : Exploitation de l'État À Terre";
                    string rawDesc = Localization.GetStringById("debrief.ally.prone_opportunity.desc") ?? "{name} a renversé ses cibles à {proneCount} reprises mais aucun allié n'a déclenché d'attaque d'opportunité gratuite. Positionnez vos guerriers de mêlée au contact direct des cibles à terre.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{proneCount}", stat.CC_Prone.ToString());

                    list.Add(new TacticalAdvice("🧎", title, desc, new Color(0.8f, 0.5f, 0.3f)));
                }

                // 34. Alerte : État de Fatigue ou d'Épuisement Gênant
                if (stat.CC_Fatigued + stat.CC_Exhausted >= 1)
                {
                    string title = Localization.GetStringById("debrief.ally.fatigued_exhausted_dampener.title") ?? "Alerte : Impact de la Fatigue Tactique";
                    string rawDesc = Localization.GetStringById("debrief.ally.fatigued_exhausted_dampener.desc") ?? "{name} a combattu sous l'influence de l'épuisement ou de la fatigue. Cela réduit grandement la CA et interdit les charges tactiques. Reposez-vous ou lancez 'Restauration partielle'.";
                    
                    string desc = rawDesc.Replace("{name}", stat.Name);

                    list.Add(new TacticalAdvice("💤", title, desc, new Color(0.6f, 0.6f, 0.6f)));
                }

                // 41. Conseil : Inefficacité de l'Acide Pur
                if (stat.AcidDmg > (partyLevel * 10) && stat.Kills == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.acid_dps_waste.title") ?? "Conseil : Limitation de l'Acide Pur";
                    string rawDesc = Localization.GetStringById("debrief.ally.acid_dps_waste.desc") ?? "{name} a infligé d'importants dégâts d'acide ({acidDmg} dégâts) mais n'a obtenu aucune élimination. L'acide est parfait pour user les boss mais manque de punch de finition. Associez-le à un burst physique.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{acidDmg}", stat.AcidDmg.ToString());

                    list.Add(new TacticalAdvice("🧪", title, desc, new Color(0.5f, 0.8f, 0.5f)));
                }

                // 42. Conseil : Synergie de Contrôle de Froid Manquée
                if (stat.ColdDmg > (partyLevel * 5) && stat.CC_Slowed == 0 && stat.CC_Staggered == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.cold_control_underused.title") ?? "Conseil : Synergie de Froid Manquée";
                    string rawDesc = Localization.GetStringById("debrief.ally.cold_control_underused.desc") ?? "{name} a infligé {coldDmg} dégâts de froid mais n'a appliqué aucun ralentissement. Favorisez des sorts de givre lourds combinant dégâts et entraves physiques.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{coldDmg}", stat.ColdDmg.ToString());

                    list.Add(new TacticalAdvice("❄️", title, desc, new Color(0.4f, 0.8f, 1.0f)));
                }

                // 43. Alerte : Immunité de l'Électricité des Démons
                if (stat.ElectricDmg > (partyLevel * 10) && stat.CC_Stunned == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.electric_stun_missed.title") ?? "Alerte : Immunité d'Électricité des Démons";
                    string rawDesc = Localization.GetStringById("debrief.ally.electric_stun_missed.desc") ?? "Le dps électrique de {name} ({electricDmg} dégâts) s'est heurté à l'immunité démoniaque innée (100% de résistance). Prenez le don mythique 'Élément transcendant' pour percer cette barrière.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{electricDmg}", stat.ElectricDmg.ToString());

                    list.Add(new TacticalAdvice("⚡", title, desc, new Color(0.9f, 0.9f, 0.2f)));
                }

                // 44. Alerte : Immunité de Feu des Démons
                if (stat.FireDmg > (partyLevel * 10) && stat.MaxSingleHit < 15 && stat.Kills == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.fire_immunity_blocked.title") ?? "Alerte : Immunité de Feu des Démons";
                    string rawDesc = Localization.GetStringById("debrief.ally.fire_immunity_blocked.desc") ?? "Les sorts de feu de {name} ({fireDmg} dégâts) butent contre la résistance ou l'immunité au feu naturelle des démons. Considérez le don mythique 'Élément transcendant : Feu'.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{fireDmg}", stat.FireDmg.ToString());

                    list.Add(new TacticalAdvice("🔥", title, desc, new Color(1.0f, 0.3f, 0.0f)));
                }

                // 45. Conseil : Synergie du Son et de l'Étourdissement
                if (stat.SonicDmg > (partyLevel * 5) && stat.CC_Stunned >= 1)
                {
                    string title = Localization.GetStringById("debrief.ally.sonic_stun_synergy.title") ?? "Synergie : Son et Étourdissement";
                    string rawDesc = Localization.GetStringById("debrief.ally.sonic_stun_synergy.desc") ?? "Excellente synergie sonore pour {name} ({sonicDmg} dégâts sonores et {stunCount} étourdissements). Les sorts de son ciblent la Vigueur, ignorant la CA et les réflexes élevés des démons rapides.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{sonicDmg}", stat.SonicDmg.ToString())
                        .Replace("{stunCount}", stat.CC_Stunned.ToString());

                    list.Add(new TacticalAdvice("🔊", title, desc, new Color(0.7f, 0.5f, 0.9f)));
                }

                // 46. Alerte : Conflit d'Alignement de Dégâts (Holy vs Unholy)
                if (stat.HolyDmg > (partyLevel * 5) && stat.UnholyDmg > (partyLevel * 5))
                {
                    string title = Localization.GetStringById("debrief.ally.holy_unholy_clash.title") ?? "Alerte : Conflit d'Alignement de Dégâts";
                    string rawDesc = Localization.GetStringById("debrief.ally.holy_unholy_clash.desc") ?? "{name} disperse son efficacité mythique en infligeant à la fois des dégâts sacrés et profanes ({holyDmg} sacrés vs {unholyDmg} profanes). Spécialisez-vous à 100% dans une voie d'alignement.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{holyDmg}", stat.HolyDmg.ToString())
                        .Replace("{unholyDmg}", stat.UnholyDmg.ToString());

                    list.Add(new TacticalAdvice("⚖️", title, desc, new Color(0.5f, 0.5f, 0.5f)));
                }

                // 47. Conseil : Neutralisation de Boss Majeurs
                if (stat.HighDangerCCs >= 2)
                {
                    string title = Localization.GetStringById("debrief.ally.high_danger_cc.title") ?? "Tactique : Neutralisation de Boss Majeurs";
                    string rawDesc = Localization.GetStringById("debrief.ally.high_danger_cc.desc") ?? "Superbe travail de {name} qui a réussi à neutraliser ({highDangerCCs} CC appliqués) des menaces d'un niveau supérieur au groupe. C'est la clé de la victoire en difficulté Injuste.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{highDangerCCs}", stat.HighDangerCCs.ToString());

                    list.Add(new TacticalAdvice("👑", title, desc, new Color(1.0f, 0.85f, 0.0f)));
                }
            }
            else
            {
                // =====================================================================
                // ─── SECTION B : LES 9 CONSEILS DE MENACE ET D'AUTOPSIE DE L'ENNEMI ───
                // =====================================================================

                // 17. Menace : Évitement ou CA Trop Élevée
                int totalAttacksOnEnemy = stat.AttacksDodged + stat.HitsPhysicalTaken;
                if (totalAttacksOnEnemy >= 10)
                {
                    float enemyDodgeRatio = (float)stat.AttacksDodged / totalAttacksOnEnemy;
                    if (enemyDodgeRatio > 0.40f)
                    {
                        string title = Localization.GetStringById("debrief.enemy.ac.title") ?? "Menace : Évitement ou CA Trop Élevée";
                        string rawDesc = Localization.GetStringById("debrief.enemy.ac.desc") ?? "La cible {name} a esquivé la majorité de vos attaques physiques (taux d'esquive de {attacksDodged}/{totalAttacksOnEnemy}). Contrez-la en ciblant sa CA de Contact (rayons) ou via des sorts de zone.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{attacksDodged}", stat.AttacksDodged.ToString())
                            .Replace("{totalAttacksOnEnemy}", totalAttacksOnEnemy.ToString());

                        list.Add(new TacticalAdvice("🛡️", title, desc, new Color(0.4f, 0.7f, 0.9f)));
                    }
                }

                // 18. Menace : Puissance de Feu Dévastatrice
                if (stat.TotalDamage > (partyLevel * 25))
                {
                    string title = Localization.GetStringById("debrief.enemy.damage.title") ?? "Menace : Puissance de Feu Dévastatrice";
                    string rawDesc = Localization.GetStringById("debrief.enemy.damage.desc") ?? "La cible {name} a infligé un total terrifiant de {totalDamage} dégâts au groupe. Elle représentait un danger de priorité maximale qu'il fallait neutraliser par du contrôle lourd.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{totalDamage}", stat.TotalDamage.ToString());

                    list.Add(new TacticalAdvice("🔥", title, desc, new Color(0.9f, 0.2f, 0.2f)));
                }

                // 19. Menace : Volonté et Robustesse Héroïques
                int totalEnemySaves = stat.SavesSucceeded + stat.SavesFailed;
                if (totalEnemySaves >= 3)
                {
                    float successRatio = (float)stat.SavesSucceeded / totalEnemySaves;
                    if (successRatio > 0.50f && stat.SavesSucceeded >= 2)
                    {
                        string title = Localization.GetStringById("debrief.enemy.saves.title") ?? "Menace : Volonté et Robustesse Héroïques";
                        string rawDesc = Localization.GetStringById("debrief.enemy.saves.desc") ?? "La cible {name} a réussi la majorité de ses jets de sauvegarde ({savesSucceeded} réussis sur {totalEnemySaves} tirs). Privilégiez des sortilèges sans jet de sauvegarde.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesSucceeded}", stat.SavesSucceeded.ToString())
                            .Replace("{totalEnemySaves}", totalEnemySaves.ToString());

                        list.Add(new TacticalAdvice("🧿", title, desc, new Color(0.6f, 0.3f, 0.8f)));
                    }
                }

                // 20. Menace : Rafale de Critiques Destructeurs
                if (stat.Crits >= 3)
                {
                    string title = Localization.GetStringById("debrief.enemy.crit_spammer.title") ?? "Menace : Rafale de Critiques Destructeurs";
                    string rawDesc = Localization.GetStringById("debrief.enemy.crit_spammer.desc") ?? "La cible {name} a infligé {crits} coups critiques confirmés au groupe. Utilisez l'effet 'Chance protectrice' (Protective Luck) ou imposez du camouflage pour forcer les échecs.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{crits}", stat.Crits.ToString());

                    list.Add(new TacticalAdvice("🎯", title, desc, new Color(1.0f, 0.1f, 0.1f)));
                }

                // 21. Menace : Évitement Physique Absolu
                if (totalAttacksOnEnemy >= 10)
                {
                    float enemyDodgeRatio = (float)stat.AttacksDodged / totalAttacksOnEnemy;
                    if (enemyDodgeRatio > 0.60f)
                    {
                        string title = Localization.GetStringById("debrief.enemy.dodge_god.title") ?? "Menace : Évitement Physique Absolu";
                        string rawDesc = Localization.GetStringById("debrief.enemy.dodge_god.desc") ?? "La cible {name} est virtuellement intouchable physiquement ({attacksDodged} esquives sur {totalAttacksOnEnemy} assauts). Ciblez sa CA de contact ou ses Réflexes via la magie.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{attacksDodged}", stat.AttacksDodged.ToString())
                            .Replace("{totalAttacksOnEnemy}", totalAttacksOnEnemy.ToString());

                        list.Add(new TacticalAdvice("🍃", title, desc, new Color(0.2f, 0.8f, 0.8f)));
                    }
                }

                // 22. Menace : Corruption Sacrilège Déchaînée
                if (stat.UnholyDmg > (partyLevel * 10) || stat.NegativeDmg > (partyLevel * 10))
                {
                    string title = Localization.GetStringById("debrief.enemy.unholy_dev.title") ?? "Menace : Corruption Sacrilège Déchaînée";
                    string rawDesc = Localization.GetStringById("debrief.enemy.unholy_dev.desc") ?? "La cible {name} a déchaîné d'immenses dégâts Impies ({unholyDmg}) ou Négatifs ({negativeDmg}). Protégez impérativement votre équipe avec 'Protection contre la mort' (Death Ward).";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{unholyDmg}", stat.UnholyDmg.ToString())
                        .Replace("{negativeDmg}", stat.NegativeDmg.ToString());

                    list.Add(new TacticalAdvice("👿", title, desc, new Color(0.4f, 0.1f, 0.5f)));
                }

                // 23. Menace : Domination et Entraves de Zone
                int enemyCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened;
                if (enemyCC >= 2)
                {
                    string title = Localization.GetStringById("debrief.enemy.cc_oppressor.title") ?? "Menace : Domination et Entraves de Zone";
                    string rawDesc = Localization.GetStringById("debrief.enemy.cc_oppressor.desc") ?? "La cible {name} a paralysé, étourdi ou terrifié votre ligne de front ({ccCount} entraves appliquées). Cibler sa Vigueur ou sa Volonté pour briser sa concentration.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{ccCount}", enemyCC.ToString());

                    list.Add(new TacticalAdvice("⛓️", title, desc, new Color(0.5f, 0.3f, 0.7f)));
                }

                // 24. Menace : Siphon d'Essence
                if (stat.StatDamage >= 5 || stat.NegativeLevels >= 2)
                {
                    string title = Localization.GetStringById("debrief.enemy.stat_destroyer.title") ?? "Menace : Siphon et Flétrissure d'Essence";
                    string rawDesc = Localization.GetStringById("debrief.enemy.stat_destroyer.desc") ?? "La cible {name} détruit vos combattants à petit feu ({statDamage} dégâts carac. et {negativeLevels} niveaux drainés). Utilisez 'Restauration complète' post-combat.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{statDamage}", stat.StatDamage.ToString())
                        .Replace("{negativeLevels}", stat.NegativeLevels.ToString());

                    list.Add(new TacticalAdvice("🩸", title, desc, new Color(0.6f, 0.1f, 0.2f)));
                }

                // 25. Menace : Invocations Planaires Multiples
                if (stat.SummonDamage > (partyLevel * 15) || stat.SummonKills >= 2)
                {
                    string title = Localization.GetStringById("debrief.enemy.summon_spammer.title") ?? "Menace : Invocations Secondaires Multiples";
                    string rawDesc = Localization.GetStringById("debrief.enemy.summon_spammer.desc") ?? "La cible {name} a saturé le terrain avec des invocations ({summonDamage} dégâts subis via ses sbires). Ignorez les sous-fifres et burst-dps le conjurateur d'origine.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{summonDamage}", stat.SummonDamage.ToString());

                    list.Add(new TacticalAdvice("💀", title, desc, new Color(0.5f, 0.5f, 0.5f)));
                }

                // 35. Menace : Opportuniste Sanglant
                if (stat.AoOs >= 3)
                {
                    string title = Localization.GetStringById("debrief.enemy.aoo_executioner.title") ?? "Menace : Opportuniste Sanglant";
                    string rawDesc = Localization.GetStringById("debrief.enemy.aoo_executioner.desc") ?? "L'ennemi {name} a exploité toutes vos brèches ({aoos} attaques d'opportunité gratuites). Utilisez le don 'Esquive' ou sécurisez vos incantations défensivement.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{aoos}", stat.AoOs.ToString());

                    list.Add(new TacticalAdvice("👣", title, desc, new Color(0.8f, 0.3f, 0.3f)));
                }

                // 36. Menace : Briseur de Jambes (Mise à Terre)
                if (stat.CC_Prone >= 2)
                {
                    string title = Localization.GetStringById("debrief.enemy.prone_striker.title") ?? "Menace : Briseur de Jambes (Mise à Terre)";
                    string rawDesc = Localization.GetStringById("debrief.enemy.prone_striker.desc") ?? "La cible {name} a renversé vos héros à {proneCount} reprises, provoquant des attaques d'opportunité fatales. Appliquez 'Liberté de mouvement' préventivement.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{proneCount}", stat.CC_Prone.ToString());

                    list.Add(new TacticalAdvice("🧎", title, desc, new Color(0.7f, 0.4f, 0.2f)));
                }

                // 37. Menace : Assassin des Ombres (Attaques sournoises)
                if (stat.SneakAttackDmg > (partyLevel * 10))
                {
                    string title = Localization.GetStringById("debrief.enemy.sneak_slayer.title") ?? "Menace : Assassin des Ombres";
                    string rawDesc = Localization.GetStringById("debrief.enemy.sneak_slayer.desc") ?? "L'ennemi {name} a lacéré vos flancs ({sneakAttackDmg} dégâts en attaque sournoise). Empêchez le flanquement ou utilisez le don 'Esquive instinctive'.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{sneakAttackDmg}", stat.SneakAttackDmg.ToString());

                    list.Add(new TacticalAdvice("🗡️", title, desc, new Color(0.9f, 0.5f, 0.1f)));
                }

                // 38. Menace : Voleur de Vision (Cécité)
                if (stat.CC_Blinded >= 1)
                {
                    string title = Localization.GetStringById("debrief.enemy.blindness_oppressor.title") ?? "Menace : Voleur de Vision";
                    string rawDesc = Localization.GetStringById("debrief.enemy.blindness_oppressor.desc") ?? "La cible {name} a aveuglé votre équipe ({blindedCount} cibles aveuglées), provoquant une perte de la CA de Dex et 50% d'échec. Préparez des parchemins de 'Délivrance de la cécité'.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{blindedCount}", stat.CC_Blinded.ToString());

                    list.Add(new TacticalAdvice("👓", title, desc, new Color(0.4f, 0.4f, 0.4f)));
                }

                // 40. Menace : Siphon et Flétrissure de Niveaux
                if (stat.NegativeLevels >= 3)
                {
                    string title = Localization.GetStringById("debrief.enemy.stat_drain_will.title") ?? "Menace : Siphon et Flétrissure de Niveaux";
                    string rawDesc = Localization.GetStringById("debrief.enemy.stat_drain_will.desc") ?? "L'ennemi {name} a drainé {negativeLevels} niveaux d'énergie, paralysant vos jets offensifs et de sauvegarde. Prévoyez des sorts de 'Restauration' rapide.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{negativeLevels}", stat.NegativeLevels.ToString());

                    list.Add(new TacticalAdvice("🩸", title, desc, new Color(0.5f, 0.1f, 0.2f)));
                }

                // 48. Menace : Piège du Choc de Corps-à-Corps Prone
                int physicalCombatDmg = stat.SlashingDmg + stat.PiercingDmg + stat.BludgeoningDmg;
                if (stat.CC_Prone >= 1 && physicalCombatDmg > (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.enemy.prone_melee_death_trap.title") ?? "Menace : Choc de Corps-à-Corps à Terre";
                    string rawDesc = Localization.GetStringById("debrief.enemy.prone_melee_death_trap.desc") ?? "Ce colosse {name} a renversé vos tanks puis les a écrasés au sol ({physicalDmg} dégâts physiques infligés). Maintenez vos tanks debout via 'Liberté de mouvement'.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{physicalDmg}", physicalCombatDmg.ToString());

                    list.Add(new TacticalAdvice("⚔️", title, desc, new Color(0.8f, 0.1f, 0.2f)));
                }

                // 49. Menace : Siphon de Vie Actif
                if (stat.VampiricHealing > (partyLevel * 5))
                {
                    string title = Localization.GetStringById("debrief.enemy.vampiric_siphon.title") ?? "Menace : Siphon de Vie Actif";
                    string rawDesc = Localization.GetStringById("debrief.enemy.vampiric_siphon.desc") ?? "La cible {name} s'est soignée de {vampiricHealing} PV en absorbant la santé de votre groupe. Concentrez votre burst ou appliquez des effets d'hémorragie.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{vampiricHealing}", stat.VampiricHealing.ToString());

                    list.Add(new TacticalAdvice("🩸", title, desc, new Color(0.8f, 0.2f, 0.1f)));
                }

                // 50. Menace : Assassin d'Élite Invisible
                if (stat.SneakAttackDmg > (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.enemy.sneaky_assassin.title") ?? "Menace : Assassin d'Elite Invisible";
                    string rawDesc = Localization.GetStringById("debrief.enemy.sneaky_assassin.desc") ?? "La cible {name} est un assassin invisible qui a déchiré votre groupe avec {sneakAttackDmg} dégâts de sournoise. Les sorts 'Vision véridique' ou 'Voir l'invisibilité' sont requis.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{sneakAttackDmg}", stat.SneakAttackDmg.ToString());

                    list.Add(new TacticalAdvice("🕵️", title, desc, new Color(0.2f, 0.2f, 0.3f)));
                }
            }

            return list;
        }
    }
}