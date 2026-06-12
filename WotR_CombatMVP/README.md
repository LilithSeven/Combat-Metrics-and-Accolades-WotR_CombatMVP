# Combat Metrics & Accolades

An immersive combat scoreboard, detailed metrics tracker, and battle log for Pathfinder: Wrath of the Righteous.

This mod tracks and organizes every mechanical action your party members, pets, and enemies make in combat.

---

## Key Features

### 1. Advanced Combat Metrics & Telemetry
* **Comprehensive Damage Tracking:** Splits damage into categories like slashing, piercing, bludgeoning, energy types, alignment damage (holy, unholy), negative energy, and sneak attacks.
* **Overkill & Max Hit Detection:** Records exactly how much extra damage you deal to targets already at 0 HP, and points out the hardest hit landed in the fight.
* **Detailed Healing Records:** Separates regular magical healing from health stolen using vampiric abilities.

### 2. Crowd Control (CC) & Status Tracking
* **21 Conditions Monitored:** Follows crowd control statuses such as prone, paralyzed, frightened, sickened, blinded, and asleep, making sure credit goes to the right caster.
* **Aggregated CC Metrics:** Keeps count of all successful status effects your party slaps on enemies.

### 3. Advanced Spell, Maneuver & Death Registry
* **Instadeath & Attribute Drains:** Logs instant kills from spells like Weird or Phantasmal Killer, tracks negative levels, and notes attribute damage.
* **Maneuver and Dispel Tracking:** Follows successful maneuvers (like trips) and checks who dispels magic against tough opponents.
* **Scroll & Resurrection Tracking:** Watches for high-level scroll casts (level 7+) and resurrection spells used mid-battle.

### 4. Dynamic Display Modes: Accolades vs. Tactical Analysis
You get two views—switch between them with a button at the top:
* **Battle Accolades View:** Highlights special combat achievements. Over 50 unique accolades can pop up based on how your group fights.
* **Tactical Analysis View:** Offers a plain, numbers-only dashboard for optimization. No grades, no fancy titles, just raw statistics:
  * **Offensive Accuracy:** How many physical attacks you attempted vs. landed.
  * **Defensive Evasion:** How many attacks came your way vs. how many you dodged.
  * **Failed Saving Throws Registry:** Lists Fortitude, Reflex, and Will saves that failed, showing exactly which spell or effect did it.
  * **Spell Resistance & Enemy Saves:** Shows which of your spells got shut down by enemy Spell Resistance or successful saves.
  * **Debuffs Suffered:** Tracks every negative effect your party gets hit with during the fight.

### 5. Smart Ally, Pet & Summon Management
* **Consolidated Summon Performance:** Damage and kills from temporary summons get rolled into their summoner’s statline, so the screen doesn’t get crowded.
* **Independent Pet Records:** Permanent animal companions and mythic pets each keep their own folder.

### 6. Domination & Reanimation Isolation
* **Faction-Swapping Protection:** If someone on your side gets dominated, or you bring an enemy back as a minion (like with the Lich’s Repurpose), their stats go into a separate folder so things don’t get mixed up.
* **Allied NPC Recognition:** Friendlies or neutral story NPCs (the ones with green circles) get grouped under “Allied Company,” not as enemies.

---

## UI & User Controls

* **Toggle Interface:** Hit Alt + M (or Alt + Space). Alt + M works best; it avoids getting snagged by Windows shortcuts.
* **Dismiss Interface:** Tap Escape (ESC) or click the red X up top.
* **Folder Navigation:** Flip through characters using the < and > buttons.
* **Search Filter:** Use the search bar above the left column to quickly find folders by name or mythic path.
* **Scroll Support:** Both left and right columns scroll on their own, so stats don’t get cut off on lower-res screens (like Steam Deck) or with bigger fonts.
* **UI Scale:** Change font and layout size right in the slider (from 0.75x to 1.35x, default is 0.90x).

---

## Optional Asset Pack (Mythic Backgrounds)

To keep the main mod quick and small, mythic backgrounds are a separate download:
* **How to Install:** Download "Combat Metrics & Accolades - Mythic Backgrounds (Assets).zip" from GitHub or grab Backgrounds from the Nexus Optional Files tab, then drop the files into the main mod folder (next to WotR_CombatMVP.dll and Info.json).
* **Customization:** Pop any 16:9 PNG image in the main mod folder and name it after a mythic path (like angel.png, lich.png, none.png) for a custom background. Darker, translucent images work best for reading stats.

---

## Installation

1. Grab the main .zip archive from Nexus Mods or GitHub.
2. Unzip it into your game’s Mods folder:
   Pathfinder Wrath Of The Righteous/Mods/
   (Make sure the folder’s called exactly Combat Metrics & Accolades)
3. Or, just drag the .zip right into Unity Mod Manager (UMM).

## Permissions & License

This mod uses a custom Closed Permissions, Open Source license.
* You can read, study, or decompile the code for your own learning.
* You’re allowed to make compatibility patches or addons that talk to this mod.
* Redistributing isn’t allowed. Don’t upload, mirror, or share the mod (or changed versions) anywhere else.
* Don’t copy or rip off the core scoring logic or the custom UI layout.
* Check LICENSE.txt for all the legal fine print.