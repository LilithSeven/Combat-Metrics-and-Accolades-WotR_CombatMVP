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
                // ─── SECTION A : ALERTES ALLIÉS (13 ALERTES RETENUES) ───────────────
                // ====================================================================

                // 
                int attacksDirected = stat.HitsPhysicalTaken + stat.AttacksDodged;
                if (attacksDirected >= 5)
                {
                    float dodgeRatio = (float)stat.AttacksDodged / attacksDirected;
                    if (dodgeRatio < 0.30f && stat.PhysicalDmgTaken > (partyLevel * 20))
                    {
                        string title = Localization.GetStringById("debrief.ac.title") ?? "Alerte : Classe d'Armure Insuffisante";
                        string rawDesc = Localization.GetStringById("debrief.ac.desc") ?? "{name} a subi {physicalDmgTaken} dégâts physiques sur {hitsPhysicalTaken} coups reçus (taux d'esquive : {attacksDodged}/{attacksDirected}).";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{physicalDmgTaken}", stat.PhysicalDmgTaken.ToString())
                            .Replace("{hitsPhysicalTaken}", stat.HitsPhysicalTaken.ToString())
                            .Replace("{attacksDodged}", stat.AttacksDodged.ToString())
                            .Replace("{attacksDirected}", attacksDirected.ToString());

                        list.Add(new TacticalAdvice("warning", title, desc, new Color(0.9f, 0.4f, 0.2f)));
                    }
                }

                // 
                int totalFortSaves = stat.SavesFortFailed + stat.SavesFortSucceeded;
                if (totalFortSaves >= 3)
                {
                    float failureRatio = (float)stat.SavesFortFailed / totalFortSaves;
                    if (failureRatio > 0.50f && stat.SavesFortFailed >= 2)
                    {
                        string title = Localization.GetStringById("debrief.fort.title") ?? "Alerte : Jets de Vigueur";
                        string rawDesc = Localization.GetStringById("debrief.fort.desc") ?? "{name} a échoué à {savesFortFailed} jets de Vigueur sur un total de {totalFortSaves}.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesFortFailed}", stat.SavesFortFailed.ToString())
                            .Replace("{totalFortSaves}", totalFortSaves.ToString());

                        list.Add(new TacticalAdvice("fortitude", title, desc, new Color(0.8f, 0.2f, 0.2f)));
                    }
                }

                // 
                int totalRefSaves = stat.SavesRefFailed + stat.SavesRefSucceeded;
                if (totalRefSaves >= 3)
                {
                    float failureRatio = (float)stat.SavesRefFailed / totalRefSaves;
                    if (failureRatio > 0.50f && stat.SavesRefFailed >= 2)
                    {
                        string title = Localization.GetStringById("debrief.ref.title") ?? "Alerte : Jets de Réflexes";
                        string rawDesc = Localization.GetStringById("debrief.ref.desc") ?? "{name} a échoué à {savesRefFailed} jets de Réflexes sur un total de {totalRefSaves}.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesRefFailed}", stat.SavesRefFailed.ToString())
                            .Replace("{totalRefSaves}", totalRefSaves.ToString());

                        list.Add(new TacticalAdvice("reflex", title, desc, new Color(0.2f, 0.8f, 0.6f)));
                    }
                }

                // 
                int totalWillSaves = stat.SavesWillFailed + stat.SavesWillSucceeded;
                if (totalWillSaves >= 3)
                {
                    float failureRatio = (float)stat.SavesWillFailed / totalWillSaves;
                    if (failureRatio > 0.50f && stat.SavesWillFailed >= 2)
                    {
                        string title = Localization.GetStringById("debrief.will.title") ?? "Alerte : Jets de Volonté";
                        string rawDesc = Localization.GetStringById("debrief.will.desc") ?? "{name} a échoué à {savesWillFailed} jets de Volonté sur un total de {totalWillSaves}.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesWillFailed}", stat.SavesWillFailed.ToString())
                            .Replace("{totalWillSaves}", totalWillSaves.ToString());

                        list.Add(new TacticalAdvice("will", title, desc, new Color(0.6f, 0.2f, 0.8f)));
                    }
                }

                // 
                if (stat.SpellsResistedCount >= 2)
                {
                    string title = Localization.GetStringById("debrief.sr.title") ?? "Alerte : Résistance Magique";
                    string rawDesc = Localization.GetStringById("debrief.sr.desc") ?? "{spellsResistedCount} sorts lancés par {name} ont été bloqués par la résistance magique (SR) adverse.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{spellsResistedCount}", stat.SpellsResistedCount.ToString());

                    list.Add(new TacticalAdvice("spell_resistance", title, desc, new Color(1.0f, 0.85f, 0.2f)));
                }

                // 
                if (stat.TimesDowned >= 1)
                {
                    string title = Localization.GetStringById("debrief.survival.title") ?? "Alerte : Nombre de Chutes";
                    string rawDesc = Localization.GetStringById("debrief.survival.desc") ?? "{name} est tombé{accord} inconscient{accord} à {timesDowned} reprises durant ce combat.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{accord}", stat.Gender == Kingmaker.Blueprints.Gender.Female ? "e" : "")
                        .Replace("{timesDowned}", stat.TimesDowned.ToString());

                    list.Add(new TacticalAdvice("unconscious", title, desc, new Color(0.9f, 0.1f, 0.1f)));
                }

                // 
                if (stat.FriendlyFireDmg > (partyLevel * 15) && stat.FriendlyFireDmg > totalDamage * 0.15f)
                {
                    string title = Localization.GetStringById("debrief.ally.friendly_fire.title") ?? "Alerte : Dégâts Alliés";
                    string rawDesc = Localization.GetStringById("debrief.ally.friendly_fire.desc") ?? "Les sorts de {name} ont infligé {friendlyFireDmg} dégâts aux membres du groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{friendlyFireDmg}", stat.FriendlyFireDmg.ToString());

                    list.Add(new TacticalAdvice("friendly_fire", title, desc, new Color(1.0f, 0.2f, 0.2f)));
                }

                // 
                int totalHardCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Confused + stat.CC_Asleep;
                if (totalHardCC >= 2)
                {
                    string title = Localization.GetStringById("debrief.ally.cc_locked.title") ?? "Alerte : Effets de Contrôle Subis";
                    string rawDesc = Localization.GetStringById("debrief.ally.cc_locked.desc") ?? "{name} a subi un total de {ccLocked} effets de contrôle (paralysie, étourdissement ou sommeil).";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{ccLocked}", totalHardCC.ToString());

                    list.Add(new TacticalAdvice("cc", title, desc, new Color(0.5f, 0.5f, 0.8f)));
                }

                // 
                if (stat.CC_DeathsDoor >= 1)
                {
                    string title = Localization.GetStringById("debrief.ally.deaths_door.title") ?? "Alerte : Seuil de la Mort";
                    string rawDesc = Localization.GetStringById("debrief.ally.deaths_door.desc") ?? "{name} a atteint le Seuil de la Mort pendant le combat.";
                    
                    string desc = rawDesc.Replace("{name}", stat.Name);

                    list.Add(new TacticalAdvice("deaths_door", title, desc, new Color(0.9f, 0.2f, 0.1f)));
                }

                // 
                if (totalDamage > (partyLevel * 10) && stat.AllPhysDmg > 0 && stat.MaxSingleHit > 0 && stat.MaxSingleHit < (5 + partyLevel))
                {
                    string title = Localization.GetStringById("debrief.ally.physical_res.title") ?? "Alerte : Réduction de Dégâts (DR)";
                    string rawDesc = Localization.GetStringById("debrief.ally.physical_res.desc") ?? "Les attaques physiques de {name} ont été atténuées par la réduction de dégâts (DR) adverse (le coup le plus élevé a infligé {maxSingleHit} dégâts).";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{maxSingleHit}", stat.MaxSingleHit.ToString());

                    list.Add(new TacticalAdvice("damage_reduction", title, desc, new Color(0.5f, 0.5f, 0.5f)));
                }

                // 
                if (stat.CC_Fatigued + stat.CC_Exhausted >= 1)
                {
                    string title = Localization.GetStringById("debrief.ally.fatigued_exhausted_dampener.title") ?? "Allié : Fatigue et Épuisement";
                    string rawDesc = Localization.GetStringById("debrief.ally.fatigued_exhausted_dampener.desc") ?? "{name} a combattu avec l'effet de fatigue ou d'épuisement.";
                    
                    string desc = rawDesc.Replace("{name}", stat.Name);

                    list.Add(new TacticalAdvice("fatigue", title, desc, new Color(0.6f, 0.6f, 0.6f)));
                }

                // 
                if (stat.ElectricDmg > (partyLevel * 10) && stat.CC_Stunned == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.electric_stun_missed.title") ?? "Allié : Dégâts d'Électricité";
                    string rawDesc = Localization.GetStringById("debrief.ally.electric_stun_missed.desc") ?? "Les dégâts d'électricité de {name} ({electricDmg} dégâts) ont été bloqués par une immunité ou une résistance adverse.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{electricDmg}", stat.ElectricDmg.ToString());

                    list.Add(new TacticalAdvice("electricity", title, desc, new Color(0.9f, 0.9f, 0.2f)));
                }

                // 
                if (stat.FireDmg > (partyLevel * 10) && stat.MaxSingleHit < 15 && stat.Kills == 0)
                {
                    string title = Localization.GetStringById("debrief.ally.fire_immunity_blocked.title") ?? "Allié : Dégâts de Feu";
                    string rawDesc = Localization.GetStringById("debrief.ally.fire_immunity_blocked.desc") ?? "Les dégâts de feu de {name} ({fireDmg} dégâts) ont été bloqués par une immunité ou une résistance au feu adverse.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{fireDmg}", stat.FireDmg.ToString());

                    list.Add(new TacticalAdvice("fire", title, desc, new Color(1.0f, 0.3f, 0.0f)));
                }
            }
            else
            {
                // ====================================================================
                // ─── SECTION B : ALERTES ENNEMIS (12 ALERTES RETENUES) ──────────────
                // ====================================================================

                // 
                int totalAttacksOnEnemy = stat.AttacksDodged + stat.HitsPhysicalTaken;
                if (totalAttacksOnEnemy >= 10)
                {
                    float enemyDodgeRatio = (float)stat.AttacksDodged / totalAttacksOnEnemy;
                    if (enemyDodgeRatio > 0.40f)
                    {
                        string title = Localization.GetStringById("debrief.enemy.ac.title") ?? "Adversaire : Esquive et CA";
                        string rawDesc = Localization.GetStringById("debrief.enemy.ac.desc") ?? "L'ennemi {name} a esquivé {attacksDodged} attaques sur les {totalAttacksOnEnemy} tentatives physiques dirigées contre lui.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{attacksDodged}", stat.AttacksDodged.ToString())
                            .Replace("{totalAttacksOnEnemy}", totalAttacksOnEnemy.ToString());

                        list.Add(new TacticalAdvice("shield", title, desc, new Color(0.4f, 0.7f, 0.9f)));
                    }
                }

                // 
                if (stat.TotalDamage > (partyLevel * 25))
                {
                    string title = Localization.GetStringById("debrief.enemy.damage.title") ?? "Adversaire : Dégâts Infligés";
                    string rawDesc = Localization.GetStringById("debrief.enemy.damage.desc") ?? "L'ennemi {name} a infligé un total de {totalDamage} dégâts au groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{totalDamage}", stat.TotalDamage.ToString());

                    list.Add(new TacticalAdvice("fire", title, desc, new Color(0.9f, 0.2f, 0.2f)));
                }

                // 
                int totalEnemySaves = stat.SavesSucceeded + stat.SavesFailed;
                if (totalEnemySaves >= 3)
                {
                    float successRatio = (float)stat.SavesSucceeded / totalEnemySaves;
                    if (successRatio > 0.50f && stat.SavesSucceeded >= 2)
                    {
                        string title = Localization.GetStringById("debrief.enemy.saves.title") ?? "Adversaire : Jets de Sauvegarde Réussis";
                        string rawDesc = Localization.GetStringById("debrief.enemy.saves.desc") ?? "L'ennemi {name} a réussi {savesSucceeded} jets de sauvegarde sur un total de {totalEnemySaves}.";
                        
                        string desc = rawDesc
                            .Replace("{name}", stat.Name)
                            .Replace("{savesSucceeded}", stat.SavesSucceeded.ToString())
                            .Replace("{totalEnemySaves}", totalEnemySaves.ToString());

                        list.Add(new TacticalAdvice("enemy_saves", title, desc, new Color(0.6f, 0.3f, 0.8f)));
                    }
                }

                // 
                if (stat.Crits >= 3)
                {
                    string title = Localization.GetStringById("debrief.enemy.crit_spammer.title") ?? "Adversaire : Coups Critiques";
                    string rawDesc = Localization.GetStringById("debrief.enemy.crit_spammer.desc") ?? "L'ennemi {name} a infligé {crits} coups critiques confirmés au groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{crits}", stat.Crits.ToString());

                    list.Add(new TacticalAdvice("crits", title, desc, new Color(1.0f, 0.1f, 0.1f)));
                }

                // 
                if (stat.UnholyDmg > (partyLevel * 10) || stat.NegativeDmg > (partyLevel * 10))
                {
                    string title = Localization.GetStringById("debrief.enemy.unholy_dev.title") ?? "Adversaire : Dégâts Impies et Négatifs";
                    string rawDesc = Localization.GetStringById("debrief.enemy.unholy_dev.desc") ?? "L'ennemi {name} a infligé {unholyDmg} dégâts impies et {negativeDmg} dégâts d'énergie négative.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{unholyDmg}", stat.UnholyDmg.ToString())
                        .Replace("{negativeDmg}", stat.NegativeDmg.ToString());

                    list.Add(new TacticalAdvice("unholy", title, desc, new Color(0.4f, 0.1f, 0.5f)));
                }

                // 
                int enemyCC = stat.CC_Paralyzed + stat.CC_Stunned + stat.CC_Frightened;
                if (enemyCC >= 2)
                {
                    string title = Localization.GetStringById("debrief.enemy.cc_oppressor.title") ?? "Adversaire : Effets de Contrôle Appliqués";
                    string rawDesc = Localization.GetStringById("debrief.enemy.cc_oppressor.desc") ?? "L'ennemi {name} a appliqué {ccCount} effets de contrôle sur le groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{ccCount}", enemyCC.ToString());

                    list.Add(new TacticalAdvice("cc", title, desc, new Color(0.5f, 0.3f, 0.7f)));
                }

                // 
                if (stat.StatDamage >= 5 || stat.NegativeLevels >= 2)
                {
                    string title = Localization.GetStringById("debrief.enemy.stat_destroyer.title") ?? "Adversaire : Drain de Niveaux et Caractéristiques";
                    string rawDesc = Localization.GetStringById("debrief.enemy.stat_destroyer.desc") ?? "L'ennemi {name} a infligé {statDamage} dégâts de caractéristiques et a drainé {negativeLevels} niveaux.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{statDamage}", stat.StatDamage.ToString())
                        .Replace("{negativeLevels}", stat.NegativeLevels.ToString());

                    list.Add(new TacticalAdvice("blood", title, desc, new Color(0.6f, 0.1f, 0.2f)));
                }

                // 
                if (stat.AoOs >= 3)
                {
                    string title = Localization.GetStringById("debrief.enemy.aoo_executioner.title") ?? "Adversaire : Attaques d'Opportunité";
                    string rawDesc = Localization.GetStringById("debrief.enemy.aoo_executioner.desc") ?? "L'ennemi {name} a effectué {aoos} attaques d'opportunité contre le groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{aoos}", stat.AoOs.ToString());

                    list.Add(new TacticalAdvice("aoos", title, desc, new Color(0.8f, 0.3f, 0.3f)));
                }

                // 
                if (stat.CC_Blinded >= 1)
                {
                    string title = Localization.GetStringById("debrief.enemy.blindness_oppressor.title") ?? "Adversaire : Effet Aveuglé Appliqué";
                    string rawDesc = Localization.GetStringById("debrief.enemy.blindness_oppressor.desc") ?? "L'ennemi {name} a appliqué l'effet Aveuglé à {blindedCount} membres du groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{blindedCount}", stat.CC_Blinded.ToString());

                    list.Add(new TacticalAdvice("blindness", title, desc, new Color(0.4f, 0.4f, 0.4f)));
                }

                // 
                int physicalCombatDmg = stat.SlashingDmg + stat.PiercingDmg + stat.BludgeoningDmg;
                if (stat.CC_Prone >= 1 && physicalCombatDmg > (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.enemy.prone_melee_death_trap.title") ?? "Adversaire : Dégâts sur Cibles à Terre";
                    string rawDesc = Localization.GetStringById("debrief.enemy.prone_melee_death_trap.desc") ?? "L'ennemi {name} a infligé {physicalDmg} dégâts physiques à des membres du groupe au sol.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{physicalDmg}", physicalCombatDmg.ToString());

                    list.Add(new TacticalAdvice("prone_damage", title, desc, new Color(0.8f, 0.1f, 0.2f)));
                }

                // 
                if (stat.VampiricHealing > (partyLevel * 5))
                {
                    string title = Localization.GetStringById("debrief.enemy.vampiric_siphon.title") ?? "Adversaire : Soins Vampiriques";
                    string rawDesc = Localization.GetStringById("debrief.enemy.vampiric_siphon.desc") ?? "L'ennemi {name} a récupéré {vampiricHealing} PV par des effets vampiriques.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{vampiricHealing}", stat.VampiricHealing.ToString());

                    list.Add(new TacticalAdvice("vampiric", title, desc, new Color(0.8f, 0.2f, 0.1f)));
                }

                // 
                if (stat.SneakAttackDmg > (partyLevel * 15))
                {
                    string title = Localization.GetStringById("debrief.enemy.sneaky_assassin.title") ?? "Adversaire : Attaques Sournoises Invisibles";
                    string rawDesc = Localization.GetStringById("debrief.enemy.sneaky_assassin.desc") ?? "L'ennemi invisible {name} a infligé {sneakAttackDmg} dégâts d'attaque sournoise au groupe.";
                    
                    string desc = rawDesc
                        .Replace("{name}", stat.Name)
                        .Replace("{sneakAttackDmg}", stat.SneakAttackDmg.ToString());

                    list.Add(new TacticalAdvice("sneak", title, desc, new Color(0.2f, 0.2f, 0.3f)));
                }
            }

            return list;
        }
    }
}