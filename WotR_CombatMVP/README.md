# Combat Metrics & Accolades

An immersive combat scoreboard, detailed metrics tracker, and narrative battle log for **Pathfinder: Wrath of the Righteous**.

This mod captures every mechanical contribution of your companions, pets, and enemies during combat, translating them into tactical grades and unique, context-aware accolades at the end of each encounter.

---

## New in v1.4.0

* **Real-time DPS meter overlay**: a small, movable, resizable window that tracks whoever you choose (allies, everyone, or a pinned character). Toggle with `Alt + O`.
* **Campaign Ledger**: persistent statistics for your whole run, a campaign nemesis, each character's top 3 grades, and a text export (`Alt + L`). Stored safely in the mod folder, never in your save.
* **Combat comparison table**: every fighter side by side in one view (`Alt + K`).
* **Session history**: browse your previous fights, not just the last one.
* **Reanimated thralls block**: a Lich's risen undead get their own dedicated section, kept apart from the master's own stats.
* **Named dispel tracking**: lists the exact spells you dispelled.
* **Customizable shortcuts and numeric UI scale**: all configurable in the UMM settings page (`Ctrl + F10`).
* **Cleaner stat isolation**: summon, reanimated, and domination stats no longer leak into the master or the party totals.

---

## Key Features

* **Real-Time Combat Metrics**: Tracks physical damage types (slashing, piercing, bludgeoning), elemental energies, holy/unholy, precision sneak attacks, healing, and overkills (damage dealt below 0 HP).
* **Detailed Crowd Control (CC)**: Monitors 21 distinct status conditions (prone, paralyzed, frightened, sickened, blinded, etc.) and attributes them to the correct caster.
* **Special Death Tracking**: Tracks magical death spells (Weird, Phantasmalkiller), energy drains, and attribute drains, registering them as unique tactical executions.
* **Adaptive Pet Accolades**: Tailors animal companion accolades (e.g. *The Bow of Cataclysm*, *The Claw of Cataclysm*) dynamically based on their equipped weapon body slots.
* **Domination & Faction-Swapping Splits**: Automatically splits statistics into separate sheets (e.g., "Hostile" and "Dominated") when a unit changes sides during combat, preventing stat contamination.
* **Unified Deity Lore**: Correctly resolves and displays religious alignments and deity names in combat accolades without duplicate entries.
* **Pondrated Pet Scoring**: Evaluates animal companions and summons with a lighter weight (0.25) so they do not drag down your team's global grade.
* **CR-Relative Threat Scaling**: Adjusts combat scores based on enemy Challenge Rating relative to your active party level, ensuring minor skirmishes do not yield legendary grades.
* **Dynamic Display Toggle (Tactical Analysis)**: An interactive button has been added to the top of each character's detailed sheet. With a single click, you can switch from the traditional display (fun and immersive accolades) to a rigorous and educational combat analysis.
* **Contextualized Strategic Advice (50+ Scenarios Handled)**: If a character spent the fight paralyzed, took too much physical damage due to low Armor Class (AC), wasted healing spells, or had their spells blocked by enemy Spell Resistance (SR), the mod provides a precise diagnosis and suggests actionable in-game improvements (feats, gear, or specific protective spells).
* **Enemy "Threat Report" (Combat Autopsy)**: When viewing the record of a monster or major boss, the Tactical Analysis view transforms into a combat autopsy. It explains exactly why the enemy was so formidable and provides the tactical key to counter them in similar future encounters.
* **Streamlined UI for Pure Stat Enthusiasts**: Activating the Tactical Analysis view instantly hides all grades, overall team scores, and subjective titles, transforming the interface into a purely statistical, neutral dashboard focused entirely on optimizing party performance.
* **Persistent Preferences**: The mod automatically saves your latest display choice. If you prefer to play solely with neutral statistical reports, the scoreboard will open directly in this mode for all future combats (safely stored via the settings.json file).

---

## Optional Asset Separation (Mythic Backgrounds)

To keep download sizes minimal for future code updates, I have decoupled the mythic background images into a separate optional archive:
How to Install: Extract the contents of "Combat Metrics & Accolades - Mythic Backgrounds (Assets).zip" directly into your main mod directory (next to WotR_CombatMVP.dll and Info.json).
Customization & Custom Backgrounds: If you wish, you can completely customize the visual experience by using your own images!
Simply place any 16:9 aspect ratio PNG image in the mod directory and rename it to match the corresponding mythic path (e.g., angel.png, demon.png, lich.png, none.png).
To ensure that the text remain perfectly readable, I highly recommend using images with a dark, low-opacity background.

## How to Use

* Open / Close the dashboard: **`Alt + M`** (or `Escape`, or the red **`X`**).
* Real-time overlay: **`Alt + O`**.
* Campaign Ledger: **`Alt + L`**.
* Combat comparison table: **`Alt + K`**.
* Mod settings (shortcuts, overlay, UI scale, ledger): **`Ctrl + F10`** in the Unity Mod Manager window.
* Pagination: the **`<`** and **`>`** side buttons, and the history arrows in the top left to browse past fights.
* Every shortcut is fully customizable in the settings page, and the UI scale accepts a direct numeric value from 0.50 to 2.00.

---

## Installation

1. Download the latest release `.zip` file from Nexus Mods or from the GitHub Releases tab.
2. Extract the archive into your game's `Mods` folder:
   `Pathfinder Wrath Of The Righteous/Mods/`
   *(Ensure the extracted folder is named `Combat Metrics & Accolades`)*
3. Alternatively, install it directly by dragging and dropping the `.zip` file into the **Unity Mod Manager (UMM)** installer interface.

---

## Permissions & License

This mod is released under a custom **Closed Permissions, Open Source** license. 
* You are welcome to inspect, study, or decompile the source code for personal learning.
* You may create compatibility patches or addons that interact with this mod.
* **Redistribution is strictly prohibited**. You may not upload, mirror, or distribute this mod (or modified versions of it) on other platforms.
* No plagiarism of the core scoring algorithms or custom UI layout is allowed.
* See `LICENSE.txt` for the full license terms.