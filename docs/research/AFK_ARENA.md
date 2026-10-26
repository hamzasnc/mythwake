# AFK Arena (Lilith Games) – Referenzdokument für das Mythwake-Team

Stand der Recherche: 26.10.2026. Spielversion zum Zeitpunkt der Recherche: **1.205** (erschienen 20.10.2026).

Dieses Dokument beschreibt AFK Arena als Referenz für Mechaniken, Systeme, UI und Content-Struktur. Es ist
**kein** Vorlagen-Katalog: Mythwake übernimmt keine Namen, Figuren, Lore oder Art aus AFK Arena (siehe Abschnitt 11).

## Wie dieses Dokument zu lesen ist

- **Spielbegriffe bleiben Englisch** (so wie im Spiel), z. B. "Arena of Heroes", "King's Tower", "Resonating Crystal".
- **Quellen:** Hauptquelle ist das AFK Arena Fandom-Wiki (https://afk-arena.fandom.com), ergänzt durch afk.guide,
  Pocketgamer, Udonis/AppMagic und die offiziellen Patch Notes (im Wiki gespiegelt). Jede Sektion nennt ihre Quellen.
- **Aktualität:** Das Wiki ist uneinheitlich gepflegt. Viele Seiten stammen aus 2019–2022, die Patch Notes sind aber bis
  Oktober 2026 aktuell. Wo sich Quellen widersprechen, steht das ausdrücklich dabei.
- **"nicht verifiziert"** heißt: Wert oder Detail konnte in keiner Quelle bestätigt werden. Angaben, die nur aus
  allgemeiner Spielkenntnis stammen, sind als solche markiert.
- **Achtung, Verwechslungsgefahr mit AFK Journey** (Nachfolger von Lilith, 2024): "Dream Realm" und "Battle Drills" sind
  Modi aus **AFK Journey**, nicht aus AFK Arena. Ebenso wurde kein Battle Pass namens "Fabled Road" gefunden. Die
  AFK-Arena-Entsprechungen stehen in den jeweiligen Abschnitten.

## Inhalt

0. [Steckbrief](#0-steckbrief)
1. [Core Loop & Progression](#1-core-loop--progression)
2. [Kampf](#2-kampf)
3. [Helden & Helden-Progression](#3-helden--helden-progression)
4. [Summoning](#4-summoning)
5. [Spielmodi & Features](#5-spielmodi--features)
6. [Monster & Bosse](#6-monster--bosse)
7. [UI & Navigation](#7-ui--navigation)
8. [Ökonomie & Währungen](#8-ökonomie--währungen)
9. [Monetarisierung](#9-monetarisierung)
10. [Retention-Design](#10-retention-design)
11. [Was Mythwake übernehmen kann](#11-was-mythwake-übernehmen-kann)
12. [Lücken & offene Punkte](#12-lücken--offene-punkte)

---

## 0. Steckbrief

| Punkt | Wert |
|---|---|
| Entwickler/Publisher | Lilith Games (China) |
| Genre | Idle-/AFK-RPG mit Auto-Battle, Gacha-Heldensammlung |
| Global-Launch | 9. April 2019 |
| Umsatz | > 1,5 Mrd. USD Lifetime-Spending (AppMagic-Schätzung, Stand 7. Jubiläum April 2026); 12,1 Mio. USD im ersten Monat, 469,8 Mio. USD im ersten Jahr (AppMagic). Andere Quelle (data.ai via Naavik) nennt nur 147 Mio. USD für 2019 – Schätzmethoden weichen stark ab. |
| Spieler | "über 100 Mio. Spieler" (Lilith zum 5. Jubiläum, 2024) |
| Peak | ca. Januar 2020: knapp 80 Mio. USD Umsatz und knapp 5 Mio. Downloads in einem Monat (AppMagic via Udonis) |
| Setting | Esperia; Göttin des Lebens Dura vs. Gott des Todes Annih (Hypogeans) |
| Update-Takt | etwa alle 2 Wochen ein Patch, fast jeder Patch bringt einen neuen Helden (Belege: 1.198–1.205, Juli–Okt. 2026) |
| Art-Stil | 2D, handgezeichnet, an Art Nouveau angelehnt (Interview Lead Environment Artist); von Dritten als "stained-glass" Look beschrieben |

Quellen: https://www.pocketgamer.biz/afk-arena-celebrates-seventh-anniversary-and-over-15bn-in-lifetime-earnings ·
https://www.blog.udonis.co/mobile-marketing/mobile-games/afk-arena-monetization · https://naavik.co/digest/rise-fall-lilith-games/ ·
https://pocketgamer.com/afk-arena/surpasses-100m-players/ · https://creativebloq.com/art/digital-art/ai-cant-replicate-the-soul-of-a-scene-says-afk-arenas-lead-environment-artist ·
https://afk-arena.fandom.com/wiki/Patch_Notes

---

## 1. Core Loop & Progression

### 1.1 Der Kern-Loop

1. **Campaign-Stage schlagen** → erhöht die Idle-Rate (AFK Rewards) und schaltet Features frei.
2. **AFK Rewards einsammeln** (Gold, Hero EXP, Hero's Essence, Player-EXP, Gear, später Emblems, Stones, Poe Coins …).
3. **Helden leveln/ascenden/ausrüsten** mit diesen Ressourcen.
4. An der nächsten Wand hängen bleiben → Nebenmodi spielen (Tower, Labyrinth, Bounties, Arena, Guild) → wieder stärker → weiter.

Das Spiel ist bewusst so gebaut, dass man "stecken bleibt" und die Lösung Warten + Sammeln ist (Wiki-Novice-Guide:
"it's pretty normal to get stuck sometimes, just keep collecting your afk rewards").

### 1.2 Campaign

| Punkt | Details |
|---|---|
| Struktur | Kapitel mit nummerierten Stages, Schreibweise `Kapitel-Stage` (z. B. 2-28). |
| Kapitelanzahl | **85** (Kapitel 85 "Stillwater Temple" kam mit Patch 1.203, Sept. 2026). Neue Kapitel kommen laufend (Kap. 82 mit 1.190, Kap. 84 mit 1.199, Kap. 85 mit 1.203). |
| Stages pro Kapitel | Kap. 1: 12 · Kap. 2: 28 · Kap. 3–4: 36 · Kap. 5–19: 40 · ab Kap. 20: 60 (abgeleitet aus der Quest-Tabelle des Wikis). |
| Teams pro Stage | Späte Kapitel: "teams = 5" laut Kapitel-Infobox (Kap. 82). Bedeutung im Detail (mehrere Teams pro Stage?) nicht verifiziert. |
| Boss-Stages | Schlüssel-Stages haben Bosse, die meist **Helden** sind (z. B. ist Soren Boss von Stage 2-28). Eigene Boss-Stage-Regel (z. B. "jede X. Stage") nicht verifiziert. |
| Belohnung pro Stage | Gering (z. B. "12 000 Gold in Kapitel 20"); die eigentliche Belohnung ist die höhere Idle-Rate + Quest-Meilensteine. |
| Campaign-Quests | Diamanten alle 4 Stages (100–400 💎), Kapitelabschluss gibt VIP-EXP + Helden/Scrolls (Kap. 2–8 Common Scrolls, Kap. 9–21 Faction Scrolls, Kap. 26–34 Faction Choice Cards, ab Kap. 35 je 20 Stargazer Scrolls + 100 Bait + Upgrade-Steine). |
| Schwierigkeits-Glättung | Mit neuen Kapiteln wird das vorherige oft leichter gemacht ("reduced the difficulty for certain stages of Chapter 84"). Teils gibt es eine Combat-Rating-Sperre pro Kapitel (Patch 1.180: "Adjusted the Combat Rating restriction for Chapter 77"). |
| Replays | Bei Schlüssel-Stages kann man 10 erfolgreiche Replays anderer Spieler mit Formation, Gear und Statistik ansehen; im King's Tower 3 Replays pro Floor. |

Quellen: https://afk-arena.fandom.com/wiki/Campaign · https://afk-arena.fandom.com/wiki/Quests ·
https://afk-arena.fandom.com/wiki/Nightmare_Realm · https://afk-arena.fandom.com/wiki/Update_1.203_Patch_Notes ·
https://afk-arena.fandom.com/wiki/Replay_System · https://afk-arena.fandom.com/wiki/Soren

### 1.3 AFK Rewards (Idle-Truhe)

| Punkt | Details |
|---|---|
| Ort | Truhe unten in der Mitte des Campaign-Screens. |
| Inhalt | Gold, Hero's Essence, Hero EXP, Player-EXP + seltene Drops. |
| Was skaliert | Campaign-Fortschritt (höchste Stage) und VIP Rank; zusätzlich Field-of-Stars-Boni (z. B. "AFK reward hero experience drops: +50%" je Starburst) und Subscription (+10 % Base-AFK-Gold). |
| Cap | Basis **12 Stunden** (afk.guide: Belohnungen "don't accumulate after 12hr of idling"). Laut Wiki-VIP-Tabelle: VIP 3 = 14 h, VIP 4 = 16 h, VIP 5 = 18 h, VIP 6 = 24 h. Das Wiki nennt für VIP 7+ noch höhere Caps (36 h, 60 h, …) – **nicht verifiziert**, vermutlich fehlerhaft. |
| Drop-Freischaltungen | Legendary-Gear ab 8-9, Legendary+ ab 11-18, Mythic-Gear ab 16-11, Primordial Emblem ab 17-1, Amplifying Emblem ab 18-1, Faction Emblems ab 19-1, Mythic-Upgrade-Steine ab 21-1 (später T1 ab 20-60, T2 ab 25-60, T3 ab 29-60), Invigorating Essence ab Kap. 5. |
| Visuelles Feedback | Die Truhe ändert ihr Aussehen nach der Wartezeit: Stufen bei 10+, 60+, 360+ und 600+ Minuten. |

### 1.4 Fast Rewards

- Ein Klick = **120 Minuten** AFK Rewards sofort (inkl. Chance auf seltene Drops).
- **1× pro Tag gratis**, danach Diamanten mit steigendem Preis (Startpreis 50 💎 laut Wiki-Diamonds-Seite; die
  weiteren Stufen sind nicht verifiziert).
- Maximale Nutzungen pro Tag hängen am VIP Rank (VIP 1: 2 · VIP 3: 3 · VIP 6: 5 · VIP 9: 7 · VIP 12: 8 · VIP 15: 12 laut afk.guide).
- Zusätzlich gibt es **Hourglass-/Chest-Items** ("2/6/8/24 hours worth of Gold"), die X Stunden AFK-Ertrag sofort geben – teils
  vom VIP-Bonus beeinflusst, teils nicht.

### 1.5 Player Level & VIP

- **Player Level** steigt über Player-EXP aus der AFK-Truhe. Jedes Level gibt 10 💎; Meilensteine (Level 10, 20 …) geben
  VIP-EXP + Diamanten. Level-Meilensteine schalten u. a. Field-of-Stars-Starbursts frei (Level 90–180).
- **VIP Rank** steigt über VIP-EXP aus Player-Levels (gratis) **und** Echtgeld-Käufen. Ein F2P-Spieler erreicht laut Wiki
  etwa **VIP 10** (VIP 10 = 30 000 VIP-EXP, "Max Achievable by F2P").
- Details zu VIP-Perks: siehe Abschnitt 9.1.

Quellen: https://afk-arena.fandom.com/wiki/AFK_Rewards_Chest · https://afk-arena.fandom.com/wiki/Fast_Rewards ·
https://afk-arena.fandom.com/wiki/VIP_Rank · https://afk.guide/afk-arena-vip-ranks/ · https://afk.guide/afk-arena-beginner-guide/ ·
https://afk-arena.fandom.com/wiki/Items · https://afk-arena.fandom.com/wiki/Diamonds · https://afk-arena.fandom.com/wiki/Field_of_Stars ·
https://afk-arena.fandom.com/wiki/Mythic_Upgrade_Stones · https://www.appgamer.com/afk-arena/strategy-guide/afk-rewards

---

## 2. Kampf

### 2.1 Auto-Battle

- Kämpfe laufen **vollautomatisch**: Helden laufen, greifen an und nutzen Skills selbst.
- Der Spieler entscheidet **vor** dem Kampf (Helden, Formation, Gear …) und **während** des Kampfes nur, **wann Ultimates
  ausgelöst werden** – oder überlässt es dem **Auto**-Schalter.
- **Timer:** Kämpfe haben ein Zeitlimit mit Countdown. Die Wiki-Bossbeschreibungen nennen Ereignisse bei 1:16, 0:53, 0:29
  und 0:08 Restzeit, was auf ca. **90 Sekunden** hindeutet (exakte Länge nicht separat verifiziert). In Event-Modi verliert
  bei Timeout meist der Angreifer ("If a battle timer runs out, the attacking side's heroes will be wiped out").
- **Battle Speed:** 2× ab VIP 2 (afk.guide) bzw. VIP 3 (Wiki) – Quellen widersprechen sich; **4×** ab VIP 10 (Wiki).
- **Skip/Quick Battle** für die Arena ab VIP 6.
- Community-Kritik: Die Auto-KI nutzt Skills nicht optimal (z. B. Heilung bei vollem Leben); in Labyrinth/Bossen wird
  manuelles Ult-Timing empfohlen.

### 2.2 Energy & Ultimates

| Punkt | Details |
|---|---|
| Ressource | Orangefarbener **Energy**-Balken, der im Kampf steigt. |
| Quellen | Angreifen, Schaden nehmen, Skills, Buffs; **Kill = +200 Energy** für den Finisher. |
| Ultimate | Bei **1000 Energy** bereit. Manuell: auf das aufleuchtende Porträt tippen. Automatisch, wenn "Auto" an ist. |
| Haste | Mehr Angriffstempo = mehr Energiegewinn. |
| Gegenspiel | CC (Stun, Knock-up, Banish) verhindert Energiegewinn; viele Helden ziehen Gegnern Energie ab (z. B. Energieabzug bei Treffer, "Energy-Regen −60 %"). |
| Boss-Gegenmechanik | Grotesque Mage (Boss) verbietet Ultimates ganz: bei 1000 Energy wird die Energie abgezogen und der Normalangriff stattdessen stärker (bis +800 %). |

### 2.3 Formation & Targeting

- **5 Heldenplätze** pro Team. Front- und Backline-Begriffe sind im Spiel verankert (z. B. Signature Item: "When Estrilda is on
  the frontline … damage received by her backline allies is reduced"). Die Aufteilung **2 vorne / 3 hinten** entspricht der
  Spielkenntnis; in den Quellen nicht ausdrücklich als Zahl belegt.
- **Kein manuelles Targeting:** In keiner Quelle wurde eine Spieler-Einstellung für Zielprioritäten gefunden. Targeting ist
  **pro Skill fest definiert**, z. B.: "nearest enemy" (Wrizz' Molotov), "two frontline enemies" (Ice Shemira),
  "enemy with the highest Attack Rating", "ally with the highest Combat Rating", "lowest health percentage allies",
  "most densely populated area", "targets in formation order", "enemy that has dealt the most damage".
- **Positionsspiel** passiert über Platztausch ("Sometimes even just swapping your two frontline heroes can make all the difference").
- **Beast (Haustier)** als 6. Einheit ohne Platz in der Formation (siehe 3.12).

### 2.4 Factions & Faction Advantage

Acht Factions (Wiki, Stand 2026):

| Faction | Beschreibung (Lore-Kurzform) | Vorteil gegen |
|---|---|---|
| Lightbearers | Menschen/Ritter im Namen Duras | Maulers |
| Maulers | "Stärke ist alles" (Orks, Wüstenvölker) | Wilders |
| Wilders | Naturschützer (Elfen, Treants) | Graveborns |
| Graveborns | Untote, Nekromantie | Lightbearers |
| Celestials | Götter/Diener Duras | Hypogeans (und umgekehrt) |
| Hypogeans | Dämonen Annihs, Hauptschurken | Celestials (und umgekehrt) |
| Dimensionals | Crossover-Helden aus anderen IPs | **neutral** – kein Vor- oder Nachteil |
| Draconis | Drachenblut-Faction (seit Patch 1.140, April 2024, 5. Jubiläum) | laut Wiki "advantage over Draconis" (Wortlaut unklar, Details nicht verifiziert) |

- **Faction Advantage = +25 % Schaden** gegen die unterlegene Faction (Wiki). Weitere Effekte (z. B. Accuracy) nicht verifiziert.
- Der Kreis der vier Grund-Factions ist **LB → Mauler → Wilder → GB → LB**; Celestial/Hypogean bilden ein separates Paar.

**Team-Faction-Bonus** (für Spieler *und* Gegner, im Kampf über das Icon oben links/rechts neben dem Attack Rating einsehbar,
aktive Boni grün):

| Zusammensetzung | Bonus |
|---|---|
| 3 gleiche Faction | +10 % ATK, +10 % HP |
| 3 gleiche + 2 einer anderen | +15 % ATK, +15 % HP |
| 4 gleiche | +15 % ATK, +20 % HP |
| 5 gleiche | +25 % ATK, +25 % HP |

- **Celestials** zählen als Joker (als jede Faction) für den Bonus.
- **Hypogeans** haben eine eigene, stapelbare Leiter: 1 = +30 % DEF · 2 = +25 % Energy Recovery bei Treffer · 3 = +15 % Crit
  Rating · 4 = +30 % Crit Damage · 5 = +15 Haste.
- **Draconis:** Jeder eingesetzte Draconis-Held gibt allen Verbündeten +2 % ATK / +2 % HP.

Quelle: https://afk-arena.fandom.com/wiki/Factions · https://afk-arena.fandom.com/wiki/Draconis · https://www.pocketgamer.com/afk-arena/draconis-faction/

### 2.5 Klassen, Typen, Rollen

Jeder Held hat drei Achsen:

| Achse | Werte | Funktion |
|---|---|---|
| **Class** (5) | Tank, Warrior, Ranger, Mage, Support | Bestimmt Elder-Tree-Zweig ("Dura's Virtues"), Engraving-Monolith-Form, Class-Artifacts, Furniture-Klassenbonus. |
| **Type** (3) | Strength, Intelligence, Agility | Strength/Agility = physischer Schaden, Intelligence = magischer Schaden; bestimmt u. a. Gear-Gruppe und Beast-Buffs. |
| **Role** (9) | Tank, AoE, Continuous Damage, Debuffer, Control, Buffer, Burst Damage, Heal, Assassin | Beschreibt die Funktion im Team; Helden unter Mythic-Tier haben Primär- und Sekundärrolle. |

Quellen: https://afk-arena.fandom.com/wiki/Hero_Classes · https://afk-arena.fandom.com/wiki/Hero_Types · https://afk-arena.fandom.com/wiki/Hero_Roles

### 2.6 Stats (Attributes)

| Stat | Wirkung (laut Wiki) |
|---|---|
| HP | Lebenspunkte; bei 0 stirbt der Held / Death-Skill triggert. |
| ATK (Attack Rating) | Schadensbasis. |
| DEF | Reduziert physischen und magischen Schaden. |
| Crit Rating | Chance auf kritischen Treffer (Basis: doppelter Schaden). |
| Accuracy (ACC) | Trefferchance; Gegenstück zu Dodge. |
| Dodge | Ausweichchance. |
| Haste | Angriffs- und Cast-Frequenz (und damit Energy-Gewinn). |
| Recovery / Health Regeneration | Natürliche HP-Regeneration; HR: +1 % Regeneffekt pro Punkt. |
| Magic Resist (MR) / Physical Resist (PR) | Prozentuale Reduktion des jeweiligen Schadenstyps. |
| Life Leech (LL) | Heilung beim Angreifen. |
| Crit Damage Amplification (CDA) vs. Crit Damage Resistance (CDR) | Differenz bis 30 Punkte: 1 % Crit-Multiplikator pro Punkt; darüber 2 % pro Punkt; max. ±90 %. |
| Insight (IS) vs. Tenacity (TY) | Pro Punkt Tenacity über der Insight des Angreifers: +2 % Chance, Kontrolleffekten zu widerstehen. |
| Received Healing (RH) | +1 % erhaltene Heilung pro Punkt. |
| Attack Speed (AS) | +1 % Normalangriffsfrequenz pro Punkt. |
| Crit Block Rate (CBR) | −1 % Chance, kritisch getroffen zu werden, pro Punkt (aus Engravings). |
| Physical Pierce (PP) / Magic Pierce (MP) | +1 % Schaden pro Punkt und −1 Punkt gegnerische Resist (aus Engravings). |
| Skill Haste / Energy Regen | Als eigener Stat nicht in der Wiki-Liste; Energiegewinn wird über Haste, "Energy Recovery"-Buffs und Skills geregelt. |

Combat Rating (CR) = sichtbare Power-Zahl pro Held/Team (für Matchmaking, Floor-Platzierung, Gegner-Skalierung).

Quelle: https://afk-arena.fandom.com/wiki/Attributes · https://afk-arena.fandom.com/wiki/Energy

### 2.7 Status-Effekte (Auswahl, aus Skill- und Bossbeschreibungen)

| Effekt | Beispiel in Quellen |
|---|---|
| Stun | Leviathan: Geysire stunnen; Torrential Blow 2 s Stun |
| Knock-up / Knock-back | Leviathan; Blizzard mit Knock-back |
| Entangle / Root | Arden "Entangling Roots" (bis 3 s), The Unhinged |
| Sleep | Tasi "Slumber" (ganzes Gegnerteam 4 s) |
| Banish (vom Feld entfernen) | Tasi "Banishment" (bis 6 s); Fawkes' Sarg |
| Charm / Mind Control | Nemora "Beguile"; Soren "Frighten" (Helden greifen Verbündete an) |
| Terrify / Horrify | Ezizh (verhindert z. B. Ice Shemiras Blizzard) |
| Freeze, Slow | Ice Shemira "Frozen Grasp" / "Frost Nova" |
| Burn, Poison, DoT | Wrizz "Molotov" (9 s Burn), Debuffer-Rolle |
| Heilverbot | Ice Shemira: bei Schneefall keine Heilung |
| Energy Drain / Energy-Regen-Malus | Lucius' Signature Item (−30 % bis −90 % Energy Recovery) |
| Shields, Buff-Steal, Buff-Removal | Raine-Schild, Grotesque Mage "Phantom Magic", The Unhinged "Lightning Fury" |
| Weather (Feldzustand) | Leviathan "Crashing Maw", Skriath "Sandstorm", Ice Shemira "Blizzard" überschreiben sich gegenseitig |

Bosse sind fast immer **immun gegen Crowd Control** ("Wrizz is immune to crowd control abilities").

Quellen: https://afk-arena.fandom.com/wiki/Crowd_Control · https://afk-arena.fandom.com/wiki/The_Twisted_Realm ·
https://afk-arena.fandom.com/wiki/Leviathan · https://afk-arena.fandom.com/wiki/Wrizz · https://afk-arena.fandom.com/wiki/Signature_Items

---

## 3. Helden & Helden-Progression

AFK Arena hat **sehr viele übereinanderliegende Progressionsschichten**. Grob von "früh" nach "spät":
Level → Ascension → Gear → Resonating Crystal → Signature Item → Furniture → Elder Tree/Dura's Virtues → Engravings →
Stars → Collections/Artifacts → Beasts → Oath Mark Codex. Dazu globale Schichten (Library Unions, Field of Stars,
Gear Resonance).

### 3.1 Rarity-Gruppen und Ascension-Tiers

**Drei Rarity-Gruppen** bestimmen, wie weit ein Held aufsteigen kann:

| Gruppe | Max. Ascension | Max. Level | Kommt aus dem Tavern als | Nutzen |
|---|---|---|---|---|
| Common | (Common) | 100 | Common | Wird im Rickety Cart "retired" → Hero's Essence + Hero Coins |
| Legendary (+) Tier | Legendary+ | 160 | Rare (selten Elite) | Früh-/Midgame; später **Fodder** |
| Ascended Tier | Ascended + 5★ | 240, danach Resonating Crystal | Elite | Endgame-Helden |

**Ascension-Tiers:** Common → Rare → Rare+ → Elite → Elite+ → Legendary → Legendary+ → Mythic → Mythic+ → Ascended → Ascended 1★–5★
(im Spiel farbcodiert; Ascended = weißer/kristallener Rahmen).

**Ascension-Kosten im Temple of Ascension** (vier Grund-Factions + Draconis):

| Schritt | Zusätzliche Kosten (neben dem Original) |
|---|---|
| Rare → Rare+ | 2 Kopien desselben Helden (Rare) |
| Rare+ → Elite | 2 beliebige Helden gleicher Faction (Rare+) |
| Elite → Elite+ | 1 Kopie desselben Helden (Elite) |
| Elite+ → Legendary | 2 beliebige Helden gleicher Faction (Elite+) |
| Legendary → Legendary+ | 1 Kopie desselben Helden (Elite+) |
| Legendary+ → Mythic | 1 beliebiger Held gleicher Faction (Legendary+) |
| Mythic → Mythic+ | 1 beliebiger Held gleicher Faction (Legendary+) |
| Mythic+ → Ascended | 2 Kopien desselben Helden (Elite+) |
| Ascended → jeder Stern | 1 Kopie desselben Helden (Elite+) pro Stern |

- Summe (Wiki-Guide): **8 Elite-Kopien + 180 Rare-Fodder** bis Ascended; **+10 Elite-Kopien** für 5★ → 18 Kopien.
- **Celestial/Hypogean:** kein Fodder, nur eigene Kopien (je 1 Elite+ pro Stufe, 2 Elite+ für Ascended) → 14 Kopien bis Ascended, 24 bis 5★.
- **Awakened / Highborn-Helden:** eigener Pfad über Legendary-Kopien → 12 Kopien bis Ascended, 27 bis 5★.
- **Dimensionals:** "Soul Fusion" – der Dimensional übernimmt Ascension und Level des stärksten eigenen Helden; Entkoppeln kostet 100 💎;
  gemeinsam mit dem verlinkten Helden im selben Team nur mit **Dimensional Key**.
- Geopferte Helden geben **alle investierten Ressourcen und Gear zurück** (wichtig für das Gefühl "nichts geht verloren").
- Der Wiki-Guide zählte "109 Ascended Tier Heroes aus 5 Factions" für Fodder-Planung (älterer Stand).

Quellen: https://afk-arena.fandom.com/wiki/Temple_of_Ascension · https://afk-arena.fandom.com/wiki/AFK_ARENA_ascension_guide ·
https://afk-arena.fandom.com/wiki/Rarity

### 3.2 Leveln & Resonating Crystal

- Leveln kostet **Gold + Hero EXP**, an Schwellen zusätzlich **Hero's Essence** (bei Level 11, 21 und dann alle 20 Level).
  Bis Level 240 summiert sich Hero's Essence auf 118 100 pro Held.
- **Resonating Crystal** (Ranhorn, ab 4-36): Die 5 höchsten Helden bilden die **Pentagramm**-Spitze. Weitere Helden können in
  Slots gesetzt werden und erhalten **das Level des schwächsten der 5** (begrenzt durch ihr Tier-Cap).
  - Slot leeren → 24 h Cooldown (mit 💎 überspringbar).
  - Slots werden mit **Invigorating Essence** (AFK-Drop) bzw. Diamanten freigeschaltet. Seit Patch 1.202 (Sept. 2026):
    max. 200 Slots per Diamanten, 299 per Invigorating Essence.
  - Ab 5 Helden auf Level 240 wird der Crystal selbst gelevelt (je 10 "Strengthen"-Schritte pro Level, Gold + Hero EXP, letzter
    Schritt + Essence). **Cap-Erhöhung: +5 Level pro Ascended-Held** im Besitz (z. B. 5 Ascended → 265). Wiki nennt ein Max von 800
    (Stand unklar, nicht verifiziert).
- Wirkung fürs Design: **Nur 5 Helden wirklich leveln**, alle anderen werden "mitgezogen" → breiter Roster ohne Grind-Strafe.
  Der **Rickety Cart** erlaubt außerdem Reset (100 % Rückerstattung), Revert und Retire.

Quellen: https://afk-arena.fandom.com/wiki/Resonating_Crystal · https://afk-arena.fandom.com/wiki/Leveling_Requirements ·
https://afk-arena.fandom.com/wiki/Hero's_Essence · https://afk-arena.fandom.com/wiki/Rickety_Cart · https://afk-arena.fandom.com/wiki/Update_1.202_Patch_Notes

### 3.3 Skills & Skill-Unlocks

- Jeder Held hat typischerweise **4 Skills**: der erste ist die **Ultimate**, dazu zwei bis drei aktive/passive Skills.
- Skills werden **über das Heldenlevel** freigeschaltet bzw. verstärkt. Raster laut Hero-Template: **1, 11, 21, 41, 61, 81, 101,
  121, 141, 161, 181, 201, 221** (z. B. Leviathan: Ult ab 1, zweiter Skill ab 11, verstärkt ab 21, dritter ab 41, vierter ab 61 …).
- Weitere Skill-Erweiterungen kommen über **Signature Item** (+10/+20/+30/+40), **Furniture** (3/9 Mythic-Teile) und
  **Engravings** (Level 30/60/80 + Sterne).

Quelle: https://afk-arena.fandom.com/wiki/Hero_Template · https://afk-arena.fandom.com/wiki/Leviathan

### 3.4 Signature Items

| Punkt | Details |
|---|---|
| Unlock | Ab **Mythic**-Tier, eigener Slot unten links im Gear-Screen. |
| Effekt | Prozentuale Stat-Boni pro Stufe + **einzigartige Fähigkeit**, die alle 10 Stufen stärker wird. Stufen mit Endziffer 5 geben Extra-Stats (z. B. ACC, Dodge, Crit). |
| Max-Stufe | +30 für Grund-Factions, +40 für Celestial/Hypogean/Dimensional (Wiki, älterer Stand). Epic Faction Emblems werden laut Wiki für "level 31-50" genutzt – ein höheres Cap ist also wahrscheinlich, aber nicht verifiziert. |
| Kosten | **Primordial Emblem** (20 zum Freischalten, Stufe 1–10) → **Amplifying Emblem** (11–20) → **Faction Emblems** (21–40) → **Epic Faction Emblems** (31–50, ab Stage 37-60). |
| Quellen der Emblems | King's Tower, AFK Rewards (ab Kap. 17/18/19), Gift Sets, Events, Tavern-Meilensteine. |

Quellen: https://afk-arena.fandom.com/wiki/Signature_Items · https://afk-arena.fandom.com/wiki/Signature_Item_Bonuses

### 3.5 Furniture (The Oak Inn)

- **Oak Inn** (Ranhorn): ab Kap. 4 kommen Freunde mit Geschenken vorbei; **eigenes Inn ab Kap. 17** mit Ascended-Helden.
- Jeder Ascended-Held bekommt ein **Zimmer** (klein → mittel in 6 h → groß in 10 h, nur ein Upgrade gleichzeitig). Max. 8 Gebäude
  (je 300 💎) × 40 Etagen = 320 Helden.
- **Furniture** in 3 Typen (Large, Small, Hanging), Raritäten Common/Elite/Legendary/Mythic:
  - Rare/Common-Stücke boosten eine **Faction**, Legendary eine **Class** (+Faction), **Mythic** einen **bestimmten Helden**.
  - **3 Mythic-Teile** schalten die Furniture-Fähigkeit des Helden frei, **9 Teile** die starke Version.
- **Crafting** im Workshop: **300 Poe Coins** pro Stück; Pity: Legendary alle 30, Mythic alle 90 Crafts; **Furniture Wishlist** mit
  bis zu 10 Helden; "Smart Selections" verteilt Möbel automatisch.

Quellen: https://afk-arena.fandom.com/wiki/The_Oak_Inn · https://afk-arena.fandom.com/wiki/Oak_Inn_Furniture · https://afk-arena.fandom.com/wiki/Hero_Furniture_Abilities

### 3.6 Eternal Engravings

| Punkt | Details |
|---|---|
| Unlock | Ab Kapitel 21/22 (Wiki widerspricht sich) und **Ascended 1★**. Tab "Engrave" im Hero-Detail. |
| Aufbau | Jeder Held hat einen **Monolith** (Form je Class: Schild/Tank, Hammer/Warrior, Bogen/Ranger, Zauberkreis/Mage, Baum/Support) mit **3 Major- und 5 Minor-Glyphen**, die in fester Reihenfolge aufgedeckt werden. |
| Engraving Level | Summe aller Glyphen. Level **30** und **60** verstärken einen Skill (zusätzlich je nach Sternen), Level **80** schaltet "Endeavor" für PvP-Modi frei. |
| Kosten | **Elemental Shards** (Major 1–5, Minor 1–3; insgesamt 3 750 für Level 0–30) und **Elemental Cores** (Major 6–20, Minor 4–8; 4 500 für 30–60, 6 000 für 60–80, 7 700 für 80–100). |
| Neue Stats | Bringt u. a. Crit Block Rate, Physical/Magic Pierce. |
| Events | "Engraving Rewind" setzt Engravings eines Helden zurück (Event, z. B. Jan. 2026). |

Quelle: https://afk-arena.fandom.com/wiki/Eternal_Engravings · https://afk-arena.fandom.com/wiki/Events

### 3.7 Elder Tree & Dura's Virtues

- **Elder Tree** (Library, ab 8-40): globale Stat-Boni für **alle** Helden. Gelevelt mit **Flawless Droplets**, die man erhält,
  wenn ein Elite-Held **zum ersten Mal** erhalten oder aufgestiegen wird (insgesamt 28 Droplets pro Ascended-Held + 2 pro Stern).
  → Belohnt **Breite** der Sammlung.
- **Dura's Virtues** (ab 12-40 und Baum-Level 15): je ein Zweig pro **Class** (Celerity/Ranger, Fortitude/Tank, Might/Warrior,
  Sorcery/Mage, Sustenance/Support), gelevelt mit **Twisted Essence** (50 für Level 1, 2 460 kumuliert bis 15, 7 410 bis 25,
  ab 27: 800 pro Level). Reset: 300 💎 oder Reset Scroll.

Quelle: https://afk-arena.fandom.com/wiki/The_Elder_Tree

### 3.8 Gear

| Punkt | Details |
|---|---|
| Slots | Weapon/Hand, Head, Body/Chest, Foot – plus Artifact-/Collection-Slot und Signature-Item-Slot. |
| Qualitäten | Crude → Common → Rare → Rare+ → Elite → Elite+ → Legendary → Legendary+ → Mythic → **Mythic T1–T5** (T5 seit Patch 1.198, Juli 2026). |
| Gear-Gruppen | Das Wiki listet Gear nach Strength/Intelligence/Agility; die FAQ spricht von "class"-gebundenem Gear. Welche Einteilung aktuell gilt, ist nicht verifiziert. |
| Faction-Gear | Ab Rare+ kann Gear eine Faction tragen: **+30 % Stats**, wenn der Held dieselbe Faction hat. Ab Mythic T3 entfällt die Faction, der Bonus wandert in die Basiswerte und es kommt ein **heldenspezifischer Bonus** dazu. |
| Enhancement | Sterne (bis +5) durch Verfüttern anderer Gear/Enhancement Tokens + Gold (+100 Gold je Grade-Punkt). Mythic braucht 8 000/16 000/24 000/40 000/72 000 Punkte für +1…+5. |
| Mythic-Tiers | Seit 1.198: **Rune Stones** (Mythic→T1: 2, T1→T2: 3, T2→T3: 5, T3→T4: 15) und **Rune Cores** (T4→T5: 10). Alte T1–T3-Steine werden umgetauscht. |
| Quellen | AFK-Truhe, Fast Rewards, Store, Guild Store, Labyrinth (Roamer), Misty Valley, Events. |
| Gear Resonance | Library: hebt alle schwächeren getragenen Teile auf ein freigeschaltetes Tier (z. B. "Own 100 pieces of Mythic Tier 3 Gear"). Resonierendes Gear kann nicht mehr verstärkt werden. Seit 1.198 auch für T4/T5. |

Quellen: https://afk-arena.fandom.com/wiki/Gear · https://afk-arena.fandom.com/wiki/Gear_Resonance · https://afk-arena.fandom.com/wiki/Mythic_Upgrade_Stones ·
https://afk-arena.fandom.com/wiki/Update_1.198_Patch_Notes

### 3.9 Artifacts & Collections

**Dura's Artifacts** (7, für jeden Helden, aus Peaks of Time): Dura's Grace (Schild nach Ult), Dura's Eye (Haste nach Crit),
Dura's Call (Energie pro Sekunde), Dura's Drape (Heilrate), Dura's Blade (ATK wächst über Zeit), Dura's Chalice of Vitality
(Heilung in den ersten 30–45 s), Dura's Conviction (ATK ohne Gegner in der Nähe, Schockwelle).

- Upgrade bis **5 Sterne**; jeder Stern erhöht Stats **und wie viele Helden dasselbe Artifact gleichzeitig tragen dürfen** (bis 8).
- Upgrade mit **Dura's Fragments** (Stargazer, AFK Rewards, Quests).
- **Class Artifacts** (13) aus den **Trials of God** (Peaks of Time) – nur für eine Class; Upgrade über Schwierigkeitsstufen (ab 16, 3★ bei 40).

**Collections** (neuere Schicht, Ghoulish Gallery, ab Kap. 6): "a new type of artifacts", ersetzen das Artifact im Slot und gelten
auch in Cursed Realm, Nightmare Corridor und Treasure Scramble. Man erspielt sie durch **Erkundungen** (7 Versuche/Woche, Roguelite-artig:
unterwegs Collections wählen und verbessern, bis zu 3 "Bizarre Totems" pro Lauf). Anfangs 17 Collections, neue Zonen per Patch
(z. B. "South Zone" mit 1.195). Collections geben HP/ATK/DEF-Prozentboni auf Basis- und Gear-Werte.

Quellen: https://afk-arena.fandom.com/wiki/Artifacts · https://afk-arena.fandom.com/wiki/Ghoulish_Gallery · https://afk-arena.fandom.com/wiki/Update_1.195_Patch_Notes

### 3.10 Library: Unions, Field of Stars, Oath Mark Codex, Echoes of Time

| Feature | Unlock | Funktion |
|---|---|---|
| **Library Unions** | 2-16 | Vordefinierte Heldengruppen (z. B. "The Gallant Three"); vollständig = Stat-Bonus für die Mitglieder, stärker bei Elite+ und Legendary+. Fehlende Helden kann man von **Freunden/Gilde leihen** ("My Support Heroes", bis 6). Ab Höchststufe permanent aktiv (seit 1.47). |
| **Field of Stars** | 11-40 | Konstellationen (Longhorn, The Big Torch, Sands of Time, The Chalice, The Northern Helm, The Obsidian Finch, The Great Lute, The Duck) mit "Starbursts", die an Bedingungen hängen (Player-Level, Anzahl Ascended-Helden …) und **Ressourcen-Boni** geben (z. B. +50 % Hero EXP aus AFK, Dimensional Keys, monatliche Hero Choice Pack-Nutzungen). Freischalten kostet Zeit + Gold (oder 20 💎). |
| **Oath Mark Codex** | (neu, 2025/26) | Erstes Erreichen eines Tiers mit einem Elite-Helden gibt **Codex Fragments** → Codex-Level → globale Stats; heldenspezifische "Oath Marks" (Tiers: Ebonglow, Starsign, Flareglint) mit **Radiant Ink** aktivieren; "Oath Mastery" gibt Spezialstats für alle. |
| **Echoes of Time** | – | Archiv vergangener Story-Events/Videos. |

Quellen: https://afk-arena.fandom.com/wiki/Library · https://afk-arena.fandom.com/wiki/Library_Unions · https://afk-arena.fandom.com/wiki/Field_of_Stars ·
https://afk-arena.fandom.com/wiki/Oath_Mark_Codex

### 3.11 Weitere Heldenschichten

- **Awakened Heroes:** stärkere Neuinterpretationen bestehender Helden (z. B. "Talene – The Resurging Flame", "Fawkes – Undying Hunter"
  Nov. 2025, "Theowyn – Queen of Bound Souls" Okt. 2026). Kommen aus dem **Temple of Time** und bekommen eigene **Legendary
  Epic**-Abenteuer.
- **Highborn Dragons:** Draconis-Spitzenhelden (z. B. Leviathan, Skylan, Thalassa – Aug. 2026) aus dem **Dragonheart Temple**;
  Mechanik "Scale Manifest" (Drachenform im Kampf), eigener Schadenstyp "Dragonspeak Damage".
- **Skins** (rein kosmetisch) aus Wardrobe (Shop) oder Events.
- **Swap Scroll** (einmalig, ab Kap. 24): tauscht Level, Ascension, Signature Item und Mythic Furniture zweier Helden.

Quellen: https://afk-arena.fandom.com/wiki/Legendary_Epic · https://afk-arena.fandom.com/wiki/Update_1.205_Patch_Notes ·
https://afk-arena.fandom.com/wiki/Update_1.200_Patch_Notes · https://afk-arena.fandom.com/wiki/Skins · https://afk-arena.fandom.com/wiki/Swap_Scroll

### 3.12 Beasts (Battle Pets)

- **Beast Grounds** in Ranhorn (Unlock laut Wiki 10-40 bzw. Bait ab 14-40; Patch 1.201 nennt 14-40).
- Ein Beast kämpft als zusätzliche Einheit mit, **kann nicht angegriffen/kontrolliert/gebufft werden**, und buffft Helden nach Typ
  (Strength/Intelligence/Agility).
- Erwerb per **Lure** mit **Bait**: 10 Bait = 52,5 % Spice, 45 % Rare Beast, 2,5 % Elite Beast (aus der Wishlist). 300 Spice → 1 Secret Spice
  → beliebiges Elite Beast. Duplikate leveln; Skills bei Beast-Level 6/12/18.
- Seit 2026 zusätzlich **Fabled Beasts** über einen "Wishing Shrine" (z. B. Blazefang, Patch 1.201).
- Wiki listet 27 Beasts (z. B. Dreary Ball, Polar Beast, Fire Breather, Winged Lion, Nightfall Pegasus).

Quellen: https://afk-arena.fandom.com/wiki/Beasts · https://afk-arena.fandom.com/wiki/Beast_Grounds · https://afk-arena.fandom.com/wiki/Update_1.201_Patch_Notes

### 3.13 Roster-Größe & Release-Takt

| Faction | Spielbare Helden (Wiki-Zählung) |
|---|---|
| Lightbearers | 35 |
| Maulers | 35 |
| Wilders | 32 |
| Graveborns | 33 |
| Celestials | 24 (Liste zeigt 25 Namen) |
| Hypogeans | 22 |
| Dimensionals | 26 |
| Draconis | 18 laut Factions-Seite, 26 (inkl. Common-Einheiten) laut Draconis-Seite |

- Die Wiki-Klassenliste führt **226 Ascended-Tier-Einträge** (inkl. Awakened-Varianten und Crossovers). Gesamtroster also deutlich
  über 200 Helden; die Wiki-Zählungen sind nicht synchron gepflegt.
- **Crossover-Helden (Dimensionals)** u. a. aus Assassin's Creed (Ezio), Prince of Persia, Overlord, Re:Zero, That Time I Got Reincarnated
  as a Slime, Persona 5, One-Punch Man, The Witcher, Solo Leveling, Delicious in Dungeon, Kaiju No. 8; dazu Mythen-/Folklore-/Historienfiguren
  (King Arthur, Merlin, Joan of Arc, Leonardo da Vinci, Dracula, Athena, Medusa).
- **Takt 2026:** 1.198 (14.07.) Draconis-Held · 1.199 (28.07.) 2 Dimensionals · 1.200 (11.08.) Highborn Dragon · 1.201 (25.08.) Mauler ·
  1.202 (08.09.) Lightbearer · 1.203 (22.09.) 2 Dimensionals (Crossover) · 1.204 (06.10.) kein Held · 1.205 (20.10.) Awakened Graveborn.
  → **Ein neuer Held etwa alle 2 Wochen**, mit eigener Trial-, Bountiful-Trials-, Sign-In- und "Hero Parchment"-Eventkette.

Quellen: https://afk-arena.fandom.com/wiki/Factions · https://afk-arena.fandom.com/wiki/Hero_Classes · https://afk-arena.fandom.com/wiki/Dimensionals ·
https://afk-arena.fandom.com/wiki/Patch_Notes

---

## 4. Summoning

### 4.1 Die Summon-Orte im Überblick

| Ort | Unlock | Währung | Inhalt |
|---|---|---|---|
| **Noble Tavern – "Produces all Heroes"** | 1-12 | Common Scrolls oder 300 💎 (1×) / 2 700 💎 (10×) | Alle Grund-Factions; seltene Celestial/Hypogean |
| **Faction Pack** | 1-12 | Faction Scrolls | Nur die Tages-Faction (Rotation LB → Mauler → Wilder → GB; Wechsel 300 💎) |
| **Companion Pack** | 1-12 | Companion Points (10 / 100) | Wie oben, aber niedrigere Elite-Rate |
| **Hero Choice Pack** | Field of Stars "The Big Torch" | 300 / 3 000 💎 | Jede Elite-Ziehung = der gewählte Wunschheld; 1–5× pro Monat (nur Grund-Factions) |
| **Stargazing Room** | Kap. 15 oder VIP 13 | Stargazer Card oder 500 💎 | Gewählter Held (v. a. Celestial/Hypogean) + Lotterie-Preise |
| **Temple of Time** | Kap. 15/VIP 13 bzw. 16-1/VIP 6 (Quellen widersprechen sich) | Time Emblems (1/Tag im Store für 500 💎) | Gewählter **Awakened Hero** + Lotterie-Preise |
| **Dragonheart Temple** | (bei Highborn-Release ab 16-1) | Draconis Insignias | Highborn-Dragon-Helden |
| **Stores / Events** | – | Hero Coins, Labyrinth Tokens, Gladiator Coins, Event-Währung | Soulstones bestimmter Helden; Dimensionals per Kauf oder Exchange-Event |

### 4.2 Raten & "Pity"

**Tavern-Raten** (Wiki, Common/Faction/Hero Choice):

| Tab | Common | Rare | Elite |
|---|---|---|---|
| Produces all Heroes / Faction / Hero Choice | 51,69 % | 43,70 % | 4,61 % |
| Companion Pack | 52,80 % | 44,79 % | 2,41 % |

- **Garantie (Community-beobachtet, "hidden bonus"):** Erster 10er-Pull enthält einen Elite; **jeder dritte 10er-Pull garantiert einen
  Elite** – effektiv ein 30er-Pity. Einzelpulls zählen dafür nicht. Offiziell nicht bestätigt.
- **Celestial/Hypogean im Tavern:** separat gewürfelt, ca. 0,2 % Anteil an den 4,61 %, ca. 0,02 % pro Held (afk.guide).
- **Summon-Meilensteine** (Tavern-Zähler, zurückgesetzt nach 100): 20 Summons → 20 Primordial Emblems · 50 → 30 Primordial ·
  80 → 20 Primordial + 30 Amplifying (gelegentlich 20 Emblem Choice Chests) · **100 → 1 Hero Choice Card** (Elite nach Wahl einer Faction).
- **Stargazer:** 2 % gewählter Held, 0,01 % 30 000 💎, 24,5 % Artifact-Fragment, 3,2 % Elite Hero Card u. a. **Pity laut afk.guide (April 2026):**
  spätestens nach 65 Einzel-Gazes (32 500 💎) bzw. 70 Karten im 10er-Modus; derselbe Guide nennt an anderer Stelle "bis 20 000 💎" –
  widersprüchlich.
- **Temple of Time:** ebenfalls 2 % gewählter Awakened-Held, 0,01 % 30 000 💎. Launch-Perks bei neuem Awakened: 50 Time Emblems gratis ab
  16-1 und 20 % Rabatt auf die ersten sechs 10er-Ziehungen.

### 4.3 Wishlist

- Seit Patch 1.17: **5 Wunschhelden je Grund-Faction** (LB, Mauler, Wilder, GB).
- Erhöht **nicht** die Elite-Rate, sondern bestimmt, **welcher** Elite kommt. Bei voller Wishlist kommen aus Tavern-Elites praktisch
  nur Wishlist-Helden (je ca. 0,2205 %); Celestial/Hypogean werden davor separat gewürfelt.
- Spieler-Tipp im Spiel selbst: "Players are advised to put all the heroes they want on their wishlist".

### 4.4 Summon-Währungen

| Währung | Herkunft |
|---|---|
| Common Scrolls | Daily-Chest (100 Activity → 1), Weekly-Chest (3), Kapitel 2–8, Events, Temporal Rift |
| Faction Scrolls | Kapitel 9–21, Events |
| Companion Points | Freunde (max. 20 empfangen/Tag), Mercenary-Verleih (10 pro Verleih, max. 50/Woche) |
| Diamonds | siehe Abschnitt 8 |
| Stargazer Cards/Scrolls | Packs (Deals), Towers of Esperia (ab Floor 125), Kapitel ab 35 (je 20), Events |
| Time Emblems | Store (1/Tag), Towers of Esperia, Events, Monthly Card (Option seit 1.201) |
| Draconis Insignias | Events, Guild Store, Dream Store, Deals, Deluxe Monthly Card (seit 1.201) |
| Soulstones | 60 Stück = 1 Held (Rare/Elite/heldenspezifisch); aus Bounties, Stores, King's Tower, Regal Rewards |

Quellen: https://afk-arena.fandom.com/wiki/The_Noble_Tavern · https://afk-arena.fandom.com/wiki/Stargazing_Room · https://afk-arena.fandom.com/wiki/Temple_of_Time ·
https://afk-arena.fandom.com/wiki/Scrolls · https://afk-arena.fandom.com/wiki/Time_Emblem · https://afk-arena.fandom.com/wiki/Draconis_Insignia ·
https://afk-arena.fandom.com/wiki/Soulstones · https://afk.guide/?p=558 · https://afk.guide/?p=469 · https://afk.guide/?p=1598 ·
https://afk-arena.fandom.com/wiki/Update_1.180_Patch_Notes

---

## 5. Spielmodi & Features

### 5.0 Unlock-Reihenfolge (Feature-Gating über Campaign-Stages)

| Stage | Freischaltung |
|---|---|
| Tutorial | Campaign, Temple of Ascension, Noble Tavern (Tutorial-Pulls) |
| 1-12 | Ranhorn, Noble Tavern, Rickety Cart, Barracks Store |
| 2-3 | Dark Forest |
| 2-4 | Arcane Labyrinth, Labyrinth Store |
| 2-8 | Store |
| 2-12 | King's Tower |
| 2-16 | Library, Unions |
| 2-20 | Guild, Guild Store |
| 2-28 | Arena of Heroes |
| 3-12 | Bounty Board |
| 4-36 | Resonating Crystal, Wall of Legends |
| Kap. 4 / Kap. 17 | Oak Inn (Besuch / eigenes Inn) |
| 5-40 bzw. 9-20 | Legends' Challenger Tournament (Quellen widersprechen sich; Challenger Store 9-20) |
| Kap. 6 | Ghoulish Gallery (Collections) |
| 6-4 / 6-20 | Peaks of Time / Voyage of Wonders |
| 6-40 | Mercenaries |
| 7-40 | Isle of Gold (Event) |
| 8-40 | Elder Tree |
| 9-24 | Labyrinth Hard Mode |
| 10-40 / 14-40 | Beast Grounds / Bait |
| 11-40 | Field of Stars |
| 12-40 | Twisted Realm, Dura's Virtues |
| 14-40 | Towers of Esperia (4 Factions) |
| Kap. 15 (oder VIP 13) | Stargazing Room |
| 15-40 | Abyssal Expedition |
| 16-1 | Temple of Time (bzw. VIP 6), Launch-Perks |
| Kap. 17 | Misty Valley |
| 19-40 | Temporal Rift, Hunting Fields |
| Kap. 21/22 | Eternal Engravings |
| Kap. 24 | Swap Scroll |
| Kap. 26 + 30 Läufe | Dismal Labyrinth |
| 29-60 | Celestial Sanctum & Infernal Fortress (Towers) |

Quellen: Wiki-Seiten der jeweiligen Features (siehe einzelne Abschnitte) · https://afk-arena.fandom.com/wiki/Store

### 5.1 King's Tower & Towers of Esperia

| Punkt | Details |
|---|---|
| Prinzip | Endlose Floor-Leiter, jeder Floor ein Kampf gegen ein festes Team. Ein Versuchslimit für den Haupt-Tower ist in den Quellen nicht genannt (nach Spielkenntnis unbegrenzt, nicht verifiziert). |
| Floors | **3 250** (Patch 1.200, Aug. 2026); vorher 3 200 (1.194). Neue Floors kommen per Patch, oft mit gleichzeitiger Erleichterung alter Floors. |
| Belohnungen | Rare Soulstones, Elite Soulstones (alle 10 Floors), Gold, Hero's Essence, Diamanten; Quest-Meilensteine alle 10 Floors (100 → 400 💎, ab 1 700 zusätzlich alle 50 Floors 500 Elemental Cores). |
| **Towers of Esperia** | Faction-Towers, in denen **nur Helden der Faction** erlaubt sind (Gegner sind gemischt). Tower of Light (LB: Mo/Fr/So), The Brutal Citadel (Mauler: Di/Fr/So), The World Tree (Wilder: Mi/Sa/So), The Forsaken Necropolis (GB: Do/Sa/So), Celestial Sanctum (Mi/Fr/So), Infernal Fortress (Do/Sa/So). |
| Limits | Laut Wiki "60 floors per week"; Faction-Towers 1 890 Floors, Celestial/Hypogean je 1 260 (Stand unklar). |
| Belohnungen Faction-Towers | Mythic-Sets (Floor 20–80), Emblems, Poe Coins, Stargazer Scrolls (ab 125), Time Emblems (ab 100 im Celestial Tower). |
| Hilfen | Mercenaries (1 Sieg pro Woche und Modus), Replays anderer Spieler. |

Quellen: https://afk-arena.fandom.com/wiki/King's_Tower · https://afk-arena.fandom.com/wiki/Campaign · https://afk-arena.fandom.com/wiki/Update_1.200_Patch_Notes

### 5.2 Arcane Labyrinth (Normal, Hard, Dismal)

| Punkt | Details |
|---|---|
| Prinzip | Roguelite-Dungeon mit Hex-Feldern; Helden **heilen nicht zwischen Kämpfen**, Energie bleibt erhalten. Jeder Schritt zerstört benachbarte Felder (Wegwahl!). |
| Zyklus | Reset alle **48 Stunden** (alles inkl. Relics weg). |
| Floors | 3 Floors, je 9 Reihen mit 2–3 Feldern + Boss oben. Floor-Boss gibt 50/100/200 💎 (älterer Stand). |
| Feldtypen | Gegner (braunes/rotes Banner, Boss), **Abandoned Wagons** (zusätzlicher Leihheld), **Fountain of Vitality** (+50 % HP), **Mystic** (Wiederbelebung), **Roamer** (Händler gegen 💎). |
| Gegner | **Korrumpierte Arena-Verteidigungsteams echter Spieler**, skaliert auf die Stärke der eigenen Top 5. |
| **Relics** | Nach Kämpfen Auswahl aus 3 temporären Relics (Normal: 22 Relics-Pool), Stat-/Effekt-Buffs nur im Labyrinth; Raritäten Blau/Lila/Gold. |
| Hard Mode | ab 9-24; stärkere Gegner, eigene Begegnungen; korrumpierte Helden schließen sich nach Sieg an; Wrizz-Begegnung (Guild Coins). |
| **Dismal Mode** | ab Kap. 26 + 30 Normal-Läufen; nur 8 Relics, jeder Gegner droppt ein sichtbares Relic, Boss 3 Gold-Relics; **verbotene Factions** und Debuffs pro Ebene, wechseln alle 4 Zyklen (8 Tage). Überspringt man einen Zyklus, steigt die nächste Belohnung (max. 180 %). |
| Belohnungen | Gold, EXP, **Labyrinth Tokens** (Cap 200 000), Chests, Diamanten; **Dura's Tears** als Revive-Item (aus Weekly-Chest). |
| Store | Labyrinth Store (monatlicher Refresh): Soulstones, Helden (z. B. Elite-Held für 45 000 Tokens), Hero's Essence, Dimensionals. |

Quellen: https://afk-arena.fandom.com/wiki/Arcane_Labyrinth · https://afk.guide/arcane-labyrinth/ · https://afk-arena.fandom.com/wiki/Labyrinth_Tokens

### 5.3 Peaks of Time, Voyage of Wonders, Legendary Epic, Misty Valley

- **Peaks of Time** (6-4): **Puzzle-/Erkundungskarten** (Historic Ventures, feste Schwierigkeit; Wandering Balloon, auf CR skaliert, 14 Tage
  offen). Ziel: alle **Crystal-** und **Gold-Truhen** finden. Mechaniken pro Karte (Portale, Schalter, Rätsel – z. B. "The Dismal Descent"
  mit Farb-/Symbol-Rätsel). Einmalige Belohnungen; 60 % Fortschritt + Campaign-Stage schalten die nächste Karte frei. Crystal-Truhen
  enthalten **Dura's Artifacts**. Dazu **Trials of God** (Class Artifacts).
- **Voyage of Wonders** (6-20): zeitlich begrenzte Peaks-Karten (14 Tage), Gegner skaliert beim Start und bleiben dann fix
  ("you can grow stronger within the 14 days"). Oft mit Spezial-Buffs für einen Helden (z. B. "Void Rift": nur Morael macht Schaden,
  +300 % ATK). Haupttruhe oft Helden-Auswahl oder 10 Stargazer Cards.
- **Legendary Epic**: Voyage-Abenteuer pro **Awakened Hero**, schwerer, freigeschaltet durch Besitz des Helden (ab Kap. 16).
- **Misty Valley** (ab Kap. 17, wiederkehrend, 31 Tage): 20 Stages (10 sofort, 10 nach 7 Tagen); jede Stage mit **Stone/Silver/Gold-Gates**
  (Stone = besiegen, Silver = Faction-Anforderung, Gold = Spezialbedingung wie bestimmte Helden oder Ult-Limit). Am Ende wählbare
  Treasure-Trove-Belohnung. Hilfe anderer Spieler für 100 💎.

Quellen: https://afk-arena.fandom.com/wiki/Peaks_of_Time · https://afk-arena.fandom.com/wiki/The_Dismal_Descent · https://afk-arena.fandom.com/wiki/Voyage_of_Wonders ·
https://afk-arena.fandom.com/wiki/Void_Rift · https://afk-arena.fandom.com/wiki/Legendary_Epic · https://afk-arena.fandom.com/wiki/Misty_Valley

### 5.4 Bounty Board

| Punkt | Details |
|---|---|
| Prinzip | **Expeditionen**: Helden werden auf Quests geschickt (zeitbasiert), Belohnung nach Ablauf. Ein Held kann nur eine Quest gleichzeitig machen. |
| Solo Bounties | 6–10 pro Tag (VIP-abhängig), Refresh alle 24 h oder manuell für 50 💎; Anforderungen wie "1 Lightbearer + 1 Graveborn" oder Mindestlevel. Raritäten Common (1★) bis Legendary/Mythic (4★+). |
| Team Bounties | 1/Tag (2 ab VIP 6), brauchen mehrere Helden; **Leihhelden von Freunden/Gilde** ("My Support Heroes", bis 6 verleihbar); verfallen nach 7 Tagen. |
| Belohnungen | Gold, Hero's Essence, Diamanten, Rare/Elite Soulstones. |
| Komfort | Autofill/Auto-Dispatch ab VIP 6 (afk.guide: VIP 9). |
| Laufzeiten | Genaue Stunden pro Quest-Rarität nicht verifiziert. |

Quelle: https://afk-arena.fandom.com/wiki/Bounty_Board · https://afk-arena.fandom.com/wiki/VIP_Rank

### 5.5 PvP

| Modus | Mechanik | Zeitstruktur | Belohnung |
|---|---|---|---|
| **Arena of Heroes** (2-28) | Asynchron: eigenes Angriffsteam vs. gespeichertes **Defense-Team** eines anderen Spielers; Ladder Points, Matchmaking nach Punkten; Replays aller Kämpfe. | 2 Gratis-Kämpfe/Tag (+VIP), danach Arena Tickets; **Saison 2 Wochen**, danach Punkte-Reset. | Tägliche + Saisonbelohnungen (Diamanten); Quest-Meilensteine für Siege (20 → 500 Siege) und Ladder Points (1 010 → 1 800). |
| **Legends' Challenger Tournament** | 3 Teams à 5 Helden, Best-of-3, 7 Divisionen (Novice → Champion). | 2-Wochen-Saison; Challenger Points akkumulieren **stündlich** (Cap 10 000); Gladiator Coins stündlich (Cap 500 000; Champion: "400 − Rang + 1" pro Stunde). | Challenger Store: Soulstones von Celestial/Hypogean (60 Stk. für 250 000), Dimensionals, Emblems. |
| **Legends' Championship** | Top 10 der Challenger-Saison; K.-o.-Format "last man standing": überlebende Helden behalten HP, **Exhaustion**-Stapel pro weiterem Kampf. Union-Boni hier immer maximal. | Saisonal; Detailformat nicht verifiziert. | Nicht verifiziert. Zuschauer können per **Battle Wager** Gold wetten (bis 48 h AFK-Gold, 6 Wetten pro Championship). |
| **Heroes of Esperia** | Event-PvP mit Trophäen; 6 Ligen (Bronze, Silver, Gold, Diamond, Master, Legend); Master können Legend-Plätze herausfordern. | Etwa alle 2 Monate. | Avatar Frames, Medaillen. |
| **Treasure Scramble** | Region-übergreifend; Teams werden um 02:00 und 14:00 UTC automatisch "auf Schatzsuche" geschickt und kämpfen gegen andere Teams derselben Division (6 Divisionen, Prestige). Alle Helden auf Level 360, keine Artifacts. Mehr Teams in höheren Divisionen. | Saison 10 Tage, sehr häufig wiederkehrend. | Erkundungs- und Ranglistenbelohnungen. |
| **Fight of Fates** | Karten-Kampf: Deck aus 5 Hero Cards + 6 Effect Cards, Energie "Gems of Dawn" (max. 9), Vanguard-Held startet; alle Helden normiert (Level 300, Mythic T2, SI +30, 9 Furniture). | Event (z. B. Aug. 2026). | Diamanten, Scrolls; Top 1000 → Fortune Chest für die Gilde. |
| **Battle of Blood** | Draft: 10 zufällige Hero Cards, abwechselnd 2/2/1 auf das Feld legen, dann Autokampf; normierte Helden. | Event. | – |
| Freunde-Challenge | Testkampf gegen die Formation eines Freundes. | – | – |

- **Engraving "Endeavor"** (ab Engraving-Level 80) wirkt nur in PvP-Modi → eigene PvP-Balancing-Schicht.

Quellen: https://afk-arena.fandom.com/wiki/Arena_of_Heroes · https://afk.guide/arena-of-heroes · https://afk.guide/?p=16946 · https://afk-arena.fandom.com/wiki/Gladiator_Coins ·
https://afk-arena.fandom.com/wiki/Battle_Wager · https://afk-arena.fandom.com/wiki/Heroes_of_Esperia · https://afk-arena.fandom.com/wiki/Treasure_Scramble ·
https://afk-arena.fandom.com/wiki/Fight_of_Fates · https://afk-arena.fandom.com/wiki/Battle_of_Blood

### 5.6 Guild

| Punkt | Details |
|---|---|
| Unlock / Kosten | 2-20; Gründung 500 💎; **server-übergreifend**. |
| Größe | Level 1: 30 Mitglieder, +5 pro Level (Level 9: 70); bis 3 Deputies; "Exemplar"-Rolle (Mercenary-Verleih). |
| Leveln | Nur über **Guild Activity Points**: Jedes Mitglied spendet durch Daily Quests max. 100 Punkte/Tag. Keine Guild-Tech/Forschung gefunden (nicht verifiziert, dass es keine gibt). |
| Leader-Inaktivität | Nach 72 h offline wird die Leitung automatisch an den aktivsten Deputy/Spieler übertragen. |
| Austritt | 1 h Cooldown; Guild Coins bleiben beim Spieler. |

**Guild-Modi:**

| Modus | Mechanik | Rhythmus | Belohnung |
|---|---|---|---|
| **Team Hunting – Wrizz** | Boss-Damage-Race: beliebiges Team gegen einen Boss, Belohnung nach Schaden (20 Truhen-Stufen von 5 000 bis 5 Mrd. Schaden). | täglich, 2 Versuche (3 ab VIP 6) | Gold, Gear, Guild Coins (65–1 300 pro Kampf), Diamantbeutel; Tagesbestwert halb nochmal per Mail |
| **Team Hunting – Soren** | Wie Wrizz, aber muss mit **9 000 Guild Activity Points** geöffnet werden (Gilde entscheidet, wann). | unregelmäßig | wie Wrizz, besseres Gear |
| **Twisted Realm** | Boss-Leaderboard pro "Floor" (nach Crystal-Level eingeteilt); nur Crystal-Helden + 1 Mercenary; alle Helden auf gleichem Level; Boss wird mit verlorener HP stärker; nur bester Versuch zählt. Divisionen Bronze–Diamond (je 5 Unterstufen) + Legend (Top 50). | Rotation mehrerer Bosse; 5 Gratis-Versuche/Session, max. 99 | Twisted Essence, Twisted Sigils, Poe Coins; Gildenbelohnung (Diamanten) nach gesammelten Legend Points |
| **Cursed Realm** | Wie Twisted Realm, aber Boss + **5 Shadow-Guard-Teams**; Schaden aller zählt; Helden auf Level 360. | alle 7 Tage; 10 Gratis, max. 199 | Twisted Essence, Elemental Shards/Cores, Stargazer Scrolls |
| **Nightmare Corridor** | 6 Bosse nacheinander; Score = getötete Bosse + Zeit; Helden Level 480; **wöchentlich rotierender Faction-Buff** (3 Helden einer Faction-Gruppe → +100 % ATK/DEF, Boss +30 % Schaden). Boss-Aufstellung wechselt alle 28 Tage. Teilnahme ab Crystal 350 oder 30 Ascended. | wöchentlich | Dream Tokens (Dream Store), Epic Emblems (Fortune Chest für die Gilde) |
| **Guild Trials** | Teil der **Bountiful Trials**: Gildenmitglieder teilen sich einen Helden-Pool (Legendary+), schlagen 5 feste Gegnerteams auf selbstgewähltem Level 300–999. | 14 Tage pro Hero-Release | Ranking, Fortune Chests mit bis zu 280 000 Guild Coins |
| **Spire Contest** | Gilde erkundet gemeinsam, sammelt Gildenschatz, Aufteilung am Ende. | Event (z. B. Nov. 2025, Juli 2026) | Gildenschatz |
| **Golden Frontier** | **Guild-War/Territorium**: 12 Ligen kämpfen auf einer Karte um Städte; Rally-, Siege- und Calculation-Phasen täglich; Wetter, Vanguard War (Top 5 CR), Karawanen, Spirit Powers. Divisionen mit Auf-/Abstieg (Bronze–Diamond) oder "Gilded Casual". | 4 Tage Anmeldung, 28 Tage Event, 2 Tage Auswertung | Diamanten, Essence, Gems, Ranglistenbelohnungen |
| **Guild Haggle** | Gildenmitglieder "feilschen" gemeinsam den Preis einer Truhe herunter; Rabatt für alle. | Event (Sept. 2026) | Rabatt |
| **Guild Wars** | Älterer Modus: Enrollment 48 h, Matchmaking 1 h, Battles (afk.guide). Aktueller Status nicht verifiziert. | – | – |
| **Guild Store** | Gear, Mythic-Upgrade-Steine/Rune Cores, Time Emblems, Draconis Insignias; täglicher Refresh. | täglich | – |
| **Guild Camp** | Seit 1.200: "Guild Customization" (Skins/Statuen kombinierbar). | – | kosmetisch |

Quellen: https://afk-arena.fandom.com/wiki/Guild · https://afk-arena.fandom.com/wiki/Guild_Level · https://afk-arena.fandom.com/wiki/Wrizz ·
https://afk-arena.fandom.com/wiki/Soren · https://afk-arena.fandom.com/wiki/The_Twisted_Realm · https://afk-arena.fandom.com/wiki/Cursed_Realm ·
https://afk-arena.fandom.com/wiki/Nightmare_Corridor · https://afk-arena.fandom.com/wiki/Guild_Trials · https://afk-arena.fandom.com/wiki/Golden_Frontier ·
https://afk-arena.fandom.com/wiki/Update_1.202_Patch_Notes · https://afk-arena.fandom.com/wiki/Update_1.200_Patch_Notes · https://afk.guide/guild/ ·
https://afk.guide/?p=2155 · https://afk.guide/afk-arena-guild-wars/

### 5.7 Temporal Rift

- Ab 19-40, eingeführt mit Patch 1.79. **Saisonal (90 Tage)**, ca. 7 Tage Pause zwischen Saisons.
- Endlose Floors; alle Helden normiert (Level 360, alle Slots Mythic T4 ohne Heldenbonus, keine Artifacts).
- **1 Heavenly Card pro Kampf, 7 pro Woche** → stark limitiertes Wochenspiel.
- Vor jedem Kampf **Team Badges** wählen; im Kampf droppen zufällige **Divine Seals**, die in Badge-Slots gesteckt werden (Build-Crafting).
  Alle 20 Floors ein Boss (z. B. **Continuum Stalker**), danach werden die Seals gelöscht.
- Weitere Unterfeatures: Fountain of Time, Temporal Beacon (Hero Talents), Temporal Armillary, Hall of Fame, Sanctioned Realm.
- Belohnungen: Soulstones, Gold, Essence, EXP, T1/T2-Stone-Choice-Chests, Stargazer Scrolls, Scrolls, Bait.

Quelle: https://afk-arena.fandom.com/wiki/Temporal_Rift · https://afk-arena.fandom.com/wiki/Continuum_Stalker · https://afk-arena.fandom.com/wiki/Update_1.204_Patch_Notes

### 5.8 Abyssal Expedition / Vault of Time & Hunting Fields

- **Abyssal Expedition** (15-40, seit Juni 2020 mit Beta; Saisons bis mind. Okt. 2025; 2026 als "Vault of Time" mit Saisons): **Gilden-MMO-Karte**.
  Mehrere Gilden bilden eine **Militia** (bis 70 Spieler) mit eigenem Chat. Man erobert Felder und **Settlements** (Tier 1–8), stationiert
  ("garrison") Helden, sammelt **Blessed Essence** und **Blessed Relics** (Klassen-Relics mit Stufen), steigt in **Noble Titles** auf
  (mehr Heldenplätze, mehr Territorium) und kämpft gegen Gate-Bosse und einen Final Boss (z. B. "Gouldos", 2026 "Cindercrow Specter").
  Kartenzonen: Lush Plains, Burning Steppes/Rocky Slopes/Frozen Plains (je nach Startpunkt), Outlands, Forbidden Sphere.
  **Normal** (gratis) oder **Honorable Enrollment** (3 000 💎, mehr Belohnungen, eigenes Leaderboard). Während des Events ist der
  **Trading Hub** in Ranhorn aktiv (Militia-Auktion mit Würfeln, Global Dutch Auction, Exchange Store).
- **The Hunting Fields** (19-40): ähnlich, aber in **5er-Squads** (solo zufällig oder mit Freunden), Zuschauer erhalten 80 % der
  Belohnung (max. 10 pro Team), Mini-Bosse, "Hunter's Marks", Hero Runes, Saison-Endboss (z. B. "Frozen Overseer").

Quellen: https://afk-arena.fandom.com/wiki/Abyssal_Expedition · https://afk-arena.fandom.com/wiki/Trading_Hub · https://afk-arena.fandom.com/wiki/The_Hunting_Fields ·
https://afk-arena.fandom.com/wiki/Update_1.201_Patch_Notes · https://afk-arena.fandom.com/wiki/Update_1.195_Patch_Notes

### 5.9 Mercenaries & Garrisoned Mercenaries

- **Mercenaries** (6-40): bis zu **3 Leihhelden pro Woche** von Freunden/Gilden-Exemplars; einsetzbar in Campaign, King's Tower, Faction Towers,
  Twisted Realm, Labyrinth, Voyage. **Nach einem Sieg mit Mercenary ist der Modus bis zum Wochen-Reset (So 00:00 UTC) gesperrt**;
  Niederlagen sind unbegrenzt wiederholbar. Verleiher bekommt Companion Points.
- **Garrisoned Mercenaries:** Dimensionals für **30 Tage** leihen (100 Dimensional Stones; nach 6 Leihen kostenlos); kommen als Level-1-Elite
  ohne Gear und gelten dann als eigener Held.

Quellen: https://afk-arena.fandom.com/wiki/Mercenaries · https://afk-arena.fandom.com/wiki/Garrisoned_Mercenaries

### 5.10 Events (wiederkehrend und limitiert)

| Event-Typ | Mechanik (Kurz) |
|---|---|
| **Hero-Launch-Kette** | Trial (Held testen), **Bountiful Trials** (Kämpfe mit vorgegebenem Team, Truhen), Guild Trials, Sign-In (7 Tage), **Hero Parchment** (Sammel-Event), Launch-Perks im Summon-Tempel |
| **Misty Valley** | 20-Stage-Challenge mit Bonus-Gates (siehe 5.3) |
| **Isle of Gold** | Würfel-Brettspiel: 6 Würfe alle 12 h (max. 30), eigene Insel bebauen (4 Ressourcen-, bis 14 Verteidigungsgebäude), andere Inseln plündern; 14 Tage |
| **Celestial Celebrations / Frosty Isles** | "Schatzinsel"-Tile-Picking: Wunschbelohnung wählen, Tiles mit Keys (aus Daily Quests) aufdecken, bei Treffer nächste Insel |
| **Magician Hat / Circus Tour** | Magic Tickets aus Dailies (max. 275/Tag, 3 300 in 12 Tagen) → Lotterie + Circus Medals → Gift Cart; Tickets auch gegen Geld ($29,99/$49,99/$99,99) |
| **Heroes of Esperia / Treasure Scramble / Fight of Fates / Battle of Blood** | PvP-Events (siehe 5.5) |
| **Lost Spire** | 25-Floor-Roguelite-Tower mit Runen und temporären Upgrades (Spire Tokens, Adventure Torches) |
| **Illusory Journey** | "Neustart"-Modus: Helden von Null sammeln/trainieren, Tactics-Auswahl nach Knoten, Merchant Coins, Illusion Passes |
| **Faction Hunt** | Schatzjagd nur mit nicht-awakened Helden der 4 Grund-Factions, Routen- und Eigenschafts-basierte Formationen |
| **Treasure Vanguard** | Faction-Einladungen, um Belohnungen konkurrieren (Details nicht verifiziert) |
| **Pursuit of Treasures** | Treasure Crystals sammeln, mit Gilde Geschenkboxen öffnen |
| **Saison-/Feiertags-Events** | Noctis Animarum (Halloween: Claw Machine, Kürbis-Deko, Mischief Shop), Midwinter, Summer (Whimsical Farmstead), Jubiläen, Eco-Event "Tides of Renewal" (UNEP Green Game Jam) |
| **City Revamp** | Saisonale Optik für Ranhorn und Dark Forest (z. B. "Autumnal Times", 1.202) |
| **Story-Events** | Lyrical Exploration, Rowan's Adventure (Briefe → Skin), Esperian Idols (Helden-Beliebtheitswahl), Poetic Pop Quiz |

Quellen: https://afk-arena.fandom.com/wiki/Events · https://afk-arena.fandom.com/wiki/Isle_of_Gold · https://afk-arena.fandom.com/wiki/Celestial_Celebrations ·
https://afk-arena.fandom.com/wiki/The_Frosty_Isles · https://afk-arena.fandom.com/wiki/Magician_Hat · https://afk-arena.fandom.com/wiki/Bountiful_Trials ·
https://afk-arena.fandom.com/wiki/The_Lost_Spire · https://afk-arena.fandom.com/wiki/Update_1.198_Patch_Notes · https://afk-arena.fandom.com/wiki/Update_1.202_Patch_Notes ·
https://afk-arena.fandom.com/wiki/Update_1.205_Patch_Notes · https://afk-arena.fandom.com/wiki/Esperian_Idols · https://afk-arena.fandom.com/wiki/Rowan's_Adventure

### 5.11 Was es in AFK Arena **nicht** gibt (Verwechslungen)

| Begriff | Status |
|---|---|
| Dream Realm | Modus aus **AFK Journey**. In AFK Arena entspricht dem am ehesten Twisted/Cursed Realm bzw. Nightmare Corridor. |
| Battle Drills | Gildenmodus aus **AFK Journey**. AFK-Arena-Pendants: Golden Frontier, Abyssal Expedition, Spire Contest. |
| "Fabled Road" | Kein Treffer. AFK-Arena-Passes heißen Regal Rewards, Twisted Bounties, Champions of Esperia (siehe 9). |
| Guild Tech / Forschung | In keiner Quelle gefunden. |
| Manuelle Zielpriorität | In keiner Quelle gefunden (Targeting ist pro Skill fest). |

---

## 6. Monster & Bosse

### 6.1 Wer kämpft gegen wen?

- **Campaign, Towers, Labyrinth, Arena:** Gegner sind fast immer **Heldenformationen** (dieselben Helden, die Spieler sammeln können).
  Im Labyrinth sind es sogar **korrumpierte Arena-Teams echter Spieler**.
- **Minor Enemies** (wenige generische NPCs) für leichtere Kämpfe in Campaign, Peaks of Time und Hero-Trials: Hypogean Elite (orkartig,
  tanky), Hypogean Mage (Sensenmann-artig, heilt sich, AoE), Fiend (Imp, senkt ATK), Dwarven Pulverizer (Stun-Ansturm), Dwarven
  Bombardier (baut eine Kanone auf).
- **Common-Einheiten** der Factions (z. B. "Draconis Warrior", "Draconis Archer") als schwache Gegner/Fodder.
- **Dedizierte Bosse** gibt es fast nur in **Gilden-, Rift- und Event-Modi** – meist große Einzelgegner mit Skript-Mechaniken und
  Enrage, **immun gegen CC**.

→ Design-Lektion: AFK Arena spart massiv Content-Kosten, weil **ein Asset (der Held) gleichzeitig Spielfigur, Gegner und Boss-Vorlage** ist.
Viele Bosse sind "corrupted/evil"-Varianten bekannter Helden (Evil Nemora, Dark Arden, Ice Shemira).

Quelle: https://afk-arena.fandom.com/wiki/Minor_Enemies · https://afk-arena.fandom.com/wiki/Arcane_Labyrinth · https://afk-arena.fandom.com/wiki/Draconis

### 6.2 Benannte Bosse und ihre Mechaniken

| Boss | Modus | Optik/Thema (laut Lore-Text) | Mechanik |
|---|---|---|---|
| **Wrizz, the Desecrator** | Team Hunting (täglich), Labyrinth Hard, Midwinter-Event | Goblin-Leichenfledderer, gierig nach Gold | Bombs Away: 4 große AoE-Explosionen zu festen Zeiten; Molotov: Burn auf nächsten Gegner (alle ~7 s); Enrage |
| **Soren** | Team Hunting (per 9 000 Activity), Boss Stage 2-28 | Verfluchter Knappe, 200 Jahre altes Untoten-Wesen (Graveborn) | Char: 2-s-Kanal, riesiger AoE 4× pro Kampf; Frighten: 5-s-Charm auf alle vor ihm; Enrage |
| **Kane** | Twisted Realm, Cursed Realm, Labyrinth (Heroic Merits) | Höllischer Ritter, der seinen König erschlug, mit Drachen | **Hell's Judgment**: tötet bei 15/40/65 s den höchsten Schadensverursacher; Dragon Fire (130 % AoE); Hellblade (zerstört Schilde, 165 % AoE); Cursed-Version: −75 % ATK für Top-DPS |
| **Demonic Entity (Evil Nemora)** | Twisted Realm, Halloween-Event | Dunkles Spiegelbild einer Heldin | Selbst nicht angreifbar, heilt Gefolge; beschwört alle 10 s einen **Dreaf**, jeder größer und stärker (bis 5); Schild + Energie für stärksten Verbündeten |
| **The Unhinged (Dark Arden)** | Twisted/Cursed Realm | Böse Entität, die einen Treant-Helden besessen hat | Entangle aller Gegner bei 30 s und 60 s; 50 %-Schadensschild, wenn nicht kontrolliert; Lightning Fury entfernt Buffs |
| **Ice Shemira** | Twisted/Cursed Realm | Winter-Phantom | **Blizzard** (Wetter): Heilverbot, später DoT; Frostrüstung ignoriert kleine Treffer; Frost Nova friert Gegner < 50 % HP ein |
| **Grotesque Mage** | Twisted/Cursed Realm, Abyssal-Gate-Boss | Ehemaliger Zauberer Gaston nach missglückter Show | Verbietet Ultimates (Energie → stärkerer Normalangriff, bis +800 %); Kettenblitz nach Formationsreihenfolge; Phantom stiehlt Buffs |
| **The Burning Brute** | Twisted Realm (Neujahr) | Feuerbestie aus einem anderen Universum, einmal pro Jahr, Schwäche: Lärm | Blaze Barrier (−80 % Schaden von außen); Doze bei 80 %/50 % HP, aufwecken mit > 10 % HP-Burst → aggressivere Phase |
| **Continuum Stalker** | Temporal Rift | Zeit-Bestie (Hypogean) | Gears of Time (Zahnräder treffen dichteste Gruppe, −100 Energie); Temporal Distortion (stärksten Gegner einsperren, AoE durch Zeitkopie); Looming Destruction nach 5 Casts |
| **Nightmare-Corridor-Bosse** | Nightmare Corridor (Rotation alle 28 Tage) | z. B. Ossivorous Spore (fleischfressende Sumpfblume), Astral Devourer, Mirror Demon, Infernal Overlord, Netherspinner, Cursed Capricornus, Ashen Prophet, Blighted Seraph, Sky Serpent, Molten Crystalline Worm Queen, Clawlossus, Abyssal Maestro, Dune Destroyer, The Fire Daemon … | Sechs Bosse pro Durchgang; Faction-Zustände ("Order and Faith", "Feral Pulse", "Hour of Adjudication", "Fiendish Flame") rotieren wöchentlich |
| **Gouldos / Cindercrow Specter** | Abyssal Expedition / Vault of Time | Final Bosses der Militia-Karte | Mehrere "Leben", Gildenkoordination; Militia-Belohnungen pro Kill |
| **Frozen Overseer** | Hunting Fields (Saison 1) | Urzeit-Bestie in Schneegebiet | Saison-Endboss; Details nicht verifiziert |

### 6.3 Visuelle Vielfalt

- **Silhouetten:** Bosse reichen von humanoid (Kane zu Pferd/Drache, Soren, Grotesque Mage) über Bestien (Burning Brute, Clawlossus) bis zu
  Pflanzen/Insekten (Ossivorous Spore, Worm Queen) und abstrakten Wesen (Astral Devourer, Continuum Stalker). Wachsende Gegner als
  Mechanik: Demonic Entity beschwört Dreafs, "each new Dreaf … larger and stronger".
- **Farbthemen** folgen dem Element/Modus: Eis (Ice Shemira, Frozen Overseer), Feuer (Burning Brute, Fire Daemon, "Molten"-Bosse),
  Zeit/Kosmos (Continuum Stalker, Astral Devourer), Sumpf/Fäulnis (Ossivorous Spore), Licht/Engel (Blighted Seraph, The Light Martyr).
- **Größen:** Gilden- und Rift-Bosse stehen allein auf ihrer Seite und sind deutlich größer als Helden; Campaign-Gegner sind normale
  Heldengröße. Konkrete Pixel-/Skalierungswerte: nicht verifiziert.
- **Wetter-Effekte** färben das ganze Schlachtfeld um (Blizzard, Sandstorm, Crashing Maw) – günstige Methode für visuelle Abwechslung.
- Hinweis: Diese Beschreibung basiert auf Lore- und Skilltexten des Wikis, nicht auf ausgewerteten Screenshots.

Quellen: https://afk-arena.fandom.com/wiki/The_Twisted_Realm · https://afk-arena.fandom.com/wiki/Cursed_Realm · https://afk-arena.fandom.com/wiki/Nightmare_Corridor ·
https://afk-arena.fandom.com/wiki/Wrizz · https://afk-arena.fandom.com/wiki/Soren · https://afk-arena.fandom.com/wiki/Demonic_Entity · https://afk-arena.fandom.com/wiki/Continuum_Stalker ·
https://afk-arena.fandom.com/wiki/The_Hunting_Fields · https://afk-arena.fandom.com/wiki/Trading_Hub · https://afk-arena.fandom.com/wiki/Update_1.201_Patch_Notes

---

## 7. UI & Navigation

> Hinweis: Für die UI gibt es kaum textliche Primärquellen. Was die Wiki-Infoboxen belegen, ist mit Quelle markiert; der Rest ist
> allgemeine Spielkenntnis und als **"nicht verifiziert"** gekennzeichnet. Für Layout-Entscheidungen bitte eigene Screenshots machen.

### 7.1 Hauptbereiche (Tabs der unteren Leiste)

Das Wiki führt diese Bereiche als **"Menu Tab"**: **Campaign**, **Dark Forest**, **Ranhorn**, **Heroes**, dazu **Chat** als eigenes Menü.
Reihenfolge der Buttons in der Leiste: nicht verifiziert (Spielkenntnis: Ranhorn – Dark Forest – Campaign – Heroes – Chat; eine
Community-Quelle beschreibt eine andere Belegung).

| Tab | Rolle | Inhalt laut Wiki |
|---|---|---|
| **Campaign** | Start-/Haupt-Tab, hier beginnt das Tutorial | Campaign-Kapitel/Battle-Button, **AFK Rewards Chest** (unten Mitte), **Fast Rewards**, Quests, Bag, Mail, Friends, Community, **Merchants** (Shop), **Events**, **The World Map**, Bard Tales |
| **Ranhorn** (ab 1-12) | **Stadt-Hub** als Wimmelbild mit Gebäuden | Guild, Oak Inn, Library, Temple of Ascension, Resonating Crystal, Noble Tavern, Store, Wall of Legends, Rickety Cart, Trading Hub, Beast Grounds, Dragon Isle |
| **Dark Forest** (ab 2-3) | **Modus-Hub** als Karte mit Gebäuden | Peaks of Time, Voyage of Wonders, King's Tower, Arena of Heroes, Arcane Labyrinth, Bounty Board, Abyssal Expedition, Hunting Fields, Temporal Rift, Ghoulish Gallery |
| **Heroes** | Heldenverwaltung | Unter-Tabs **Formations**, **Popularity**, **Heroes**, **Portraits** (Portraits: 100 💎 pro neu erhaltenem Helden abholbar) |
| **Chat** | Kommunikation | Local, Global, Guild, Squad (Abyssal/Hunting Fields); @-Erwähnungen, Emotes, Chat Bubbles |

- **Gesperrte Gebäude** sind in Ranhorn/Dark Forest sichtbar (grau) und zeigen beim Antippen, welche Stage sie freischaltet (MuMu-Guide) →
  das Hub ist gleichzeitig **Roadmap**.
- **Saisonale Skins für die Hubs** ("City Revamp: Autumnal Times" färbt Ranhorn und Dark Forest herbstlich, 1.202).

Quellen: https://afk-arena.fandom.com/wiki/Campaign · https://afk-arena.fandom.com/wiki/Ranhorn · https://afk-arena.fandom.com/wiki/Dark_Forest ·
https://afk-arena.fandom.com/wiki/Heroes · https://afk-arena.fandom.com/wiki/Chat · https://mumuglobal.com/blog/afk-arena-tips-tricks-guide.html

### 7.2 Menü-Organisation & Badges

- **Prinzip "Gebäude = Menü":** Statt langer Listen liegt jedes Feature als Gebäude auf einer Karte (Ranhorn = Aufbau/Helden-Meta,
  Dark Forest = Kampfmodi). Unter-Features stecken als Räume in Gebäuden (Noble Tavern → Stargazing Room, Temple of Time, Dolly's Corner;
  Library → Elder Tree, Unions, Field of Stars, Gear Resonance, Oak Inn → Workshop).
- **Shop-Zugang über Ressourcenleiste:** Das "+" oben rechts im Campaign-Screen öffnet Gift Shop/VIP-Perks (Wiki-VIP-FAQ).
- **Faction-Info im Kampf** oben links/rechts neben dem Attack Rating (Wiki).
- **Notification-Badges:** Rote Punkte an Gebäuden/Tabs bei abholbaren Belohnungen sind Genre-Standard; konkrete Regeln nicht verifiziert.
- **Dolly's Corner:** Zweimonatliches In-Game-"Magazin" mit Teasern zu neuen Helden (je ca. 9 Seiten, gelegentlich Diamanten) – ein
  diegetisches News-Feature.

### 7.3 Typische Screen-Layouts

| Screen | Belegte Elemente | Nicht verifiziert |
|---|---|---|
| **Campaign** | AFK-Truhe unten Mitte (Aussehen ändert sich nach 10/60/360/600 min), Fast Rewards, Battle-Button, Quests/Bag/Mail/Friends, Events/Merchants | exakte Anordnung |
| **Hero Detail** | Level-Up-Button (tippen oder halten), Skills-Ansicht, Gear-Screen mit Signature-Slot unten links, **"Engrave"**-Tab, Strategie-Portal ("Hero Profile", "Popular Partner", seit 1.204) | Layout der Stats, Tab-Reihenfolge |
| **Formation** | 5 Plätze, Front/Back; Faction-Bonus-Anzeige; Mercenary-Auswahl; Beast-Icon oben links im Kampf | genaue Slot-Anordnung |
| **Battle HUD** | Porträts mit **HP- und Energy-Balken**, Porträt leuchtet bei voller Energie (Tippen = Ult), **Auto**-Button, Speed-Toggle, Timer, Faction-Boni-Icons; Replays: eigene HP-Balken grün, gegnerische rot; Boss-Kämpfe zeigen Damage-Counter und Enrage-Stacks | Positionen |
| **Result/Statistik** | "Battle Statistics": Schaden ausgeteilt, Heilung, Schaden genommen pro Held; Replays | Victory/Defeat-Gestaltung |
| **Summon** | Tabs pro Summon-Typ, Fortschrittsbalken bis Hero Choice Card/Emblems, Wishlist-Button | Animation |

Quellen: https://afk-arena.fandom.com/wiki/AFK_Rewards_Chest · https://afk-arena.fandom.com/wiki/Energy · https://afk-arena.fandom.com/wiki/Arena_of_Heroes ·
https://afk-arena.fandom.com/wiki/Replay_System · https://afk-arena.fandom.com/wiki/Update_1.204_Patch_Notes · https://afk-arena.fandom.com/wiki/Beast_Grounds

### 7.4 Onboarding / Tutorial

- Das Tutorial beginnt im **Campaign**-Tab; die ersten Stages sind kurz (Kapitel 1 hat nur 12 Stages) und schalten Kern-Gebäude schnell frei
  (Tavern/Ranhorn bei 1-12, Dark Forest bei 2-3, King's Tower 2-12, Guild 2-20, Arena 2-28).
- **Frühe Gratis-Ressourcen:** Kapitel 1 gibt 600 💎 + 60 VIP-EXP, Kapitel 2–8 Helden + Common Scrolls; Diamanten alle 4 Stages.
- **Erste Helden:** "The newest players will likely use their free Ulric in their first stages" (Common-Held); garantierter Elite im ersten
  10er-Pull (Community-Beobachtung).
- **Pacing-Prinzip:** Jedes neue System erscheint, wenn der Spieler ungefähr an die Grenze des vorherigen stößt (siehe Unlock-Tabelle 5.0).
  Wer stecken bleibt, bekommt Replays und Mercenaries als Hilfe.
- Detaillierter Tutorial-Ablauf (Dialoge, Erzähler, erste Pulls): nicht verifiziert.

Quellen: https://afk-arena.fandom.com/wiki/Campaign · https://afk-arena.fandom.com/wiki/Rarity · https://afk-arena.fandom.com/wiki/The_Noble_Tavern

---

## 8. Ökonomie & Währungen

### 8.1 Haupt- und Fortschrittsressourcen

| Ressource | Quellen (Faucets) | Verbrauch (Sinks) |
|---|---|---|
| **Gold** | AFK-Truhe (Hauptquelle), Fast Rewards, Bounties, Guild Hunts, Hourglasses, Quests | Leveln, Gear-Enhancement, Store, Crystal-Strengthen, Field of Stars, Battle Wager |
| **Hero EXP** | AFK-Truhe, Fast Rewards, Hourglasses, Labyrinth | Leveln, Crystal-Strengthen |
| **Hero's Essence** ("Dust") | AFK-Truhe, Bounties (wichtig!), Retire im Rickety Cart, Store, Labyrinth Store | Level-Schwellen (alle 20 Level), Crystal-Level |
| **Diamonds** | Campaign-Quests (alle 4 Stages), Player-Level (10/Level), Daily-Chest (100), Weekly-Chest (300 + 500), King's Tower, Arena, Bounties, Guild Hunts, Achievements (bis 30 000), Wall of Legends (50 pro Server-Meilenstein), Portraits (100 pro neuem Held), Wartungs-Kompensation, Redemption Codes, Monthly Cards, Käufe | Summons (300/2 700), Stargazer (500), Fast Rewards, Store-Refresh (100), Bounty-Refresh (50), Crystal-Cooldown (100), Hero Reset, Gilde gründen (500), Oak-Inn-Gebäude (300), Honorable Enrollment (3 000), Extra-Versuche Twisted/Cursed Realm |
| **Soulstones** (Rare/Elite/heldenspezifisch, 60 = Held) | Bounties, King's Tower, Barracks Store, Labyrinth Store, Regal Rewards, Challenger Store | Helden-Beschwörung |

### 8.2 Modus-Währungen (je eine pro Modus mit eigenem Shop)

| Währung | Quelle | Shop / Sink |
|---|---|---|
| **Guild Coins** | Wrizz/Soren (nach Schaden), Labyrinth-Wrizz, Guild Trials | Guild Store (Gear, Upgrade-Steine/Rune Cores, Time Emblems, Insignias) |
| **Labyrinth Tokens** | Arcane Labyrinth (Cap 200 000) | Labyrinth Store (Soulstones, Elite-Helden, Essence, Dimensionals) |
| **Gladiator Coins** (Challenger Coins) | Legends' Challenger, stündlich (Cap 500 000) | Challenger Store (Celestial/Hypogean-Soulstones à 250 000, Emblem Chests) |
| **Hero Coins** | Retire von Common-Helden (160 pro Held) | Barracks Store (60 Elite Soulstones für 18 000, 60 Rare für 2 400, Twisted Essence) |
| **Poe Coins** | AFK-Truhe, Weekly Quests, Peaks/Voyage, Store, Twisted Realm | Oak-Inn-Furniture (300 pro Craft) |
| **Twisted Essence** | Twisted Realm (Rang), Cursed Realm, Barracks Store, Twisted Bounties | Dura's Virtues |
| **Elemental Shards / Cores** | AFK-Truhe (ab Kap. 21/23), Store, Cursed Realm, Misty Valley, Monthly Cards, King's Tower | Eternal Engravings |
| **Emblems** (Primordial, Amplifying, Faction, Epic Faction) | AFK-Truhe, King's Tower, Tavern-Meilensteine, Events, Nightmare Corridor | Signature Items |
| **Rune Stones / Rune Cores** (früher T1–T3 Stones) | AFK-Truhe, Guild Store, Misty Valley, Twisted Bounties | Mythic-Gear-Tiers |
| **Flawless Droplets** | Erst-Erhalt/Aufstieg von Elite-Helden | Elder Tree |
| **Invigorating Essence** | AFK-Truhe ab Kap. 5 | Crystal-Slots |
| **Dura's Fragments** | Stargazer, AFK, Quests | Dura's Artifacts |
| **Companion Points** | Freunde, Mercenary-Verleih | Companion Summon |
| **Bait / Spice** | AFK, Labyrinth, Store, Treasure Scramble, Temporal Rift, Kapitelquests | Beasts |
| **Dimensional Stones / Keys** | Monatlicher Tausch (max. 200/Monat) / Field of Stars | Garrisoned Mercenaries / Dimensional im selben Team |
| **Dragon Crystals** (bound/unbound) | Abyssal Expedition | Trading Hub |
| **Dream Tokens** | Nightmare Corridor | Dream Store |
| **Heroic Merits / Medals of Valor / Twisted Sigils** | Labyrinth (Kane), Daily/Weekly-Chests, Twisted Realm | Pass-Fortschritt (siehe 9.3) |
| **Event-Währungen** | Events (Candies, Magic Tickets, Keys, Tide Shells …) | Event-Shops, nach Event oft in Gold umgerechnet ("recalled for 5k Gold Coins per ticket") |

### 8.3 Ökonomie-Muster

- **Stunden als Währungseinheit:** Viele Belohnungen sind "X Stunden AFK-Ertrag" → Belohnungen skalieren automatisch mit dem Fortschritt,
  ohne dass Designer Tabellen pro Spielphase pflegen müssen.
- **Jeder Modus hat seinen eigenen Shop + Währung** → der Spieler hat immer etwas zu sparen; Stores refreshen täglich (Store, Guild, Barracks
  00:00 UTC) oder monatlich (Labyrinth, Challenger).
- **Caps auf Speicher** (Labyrinth Tokens, Gladiator Coins, Challenger Points, AFK-Stunden) zwingen zum regelmäßigen Einloggen.
- **Rückerstattung** beim Reset/Ascenden reduziert Fehlentscheidungs-Frust.

Quellen: https://afk-arena.fandom.com/wiki/Diamonds · https://afk-arena.fandom.com/wiki/Gold · https://afk-arena.fandom.com/wiki/Hero's_Essence ·
https://afk-arena.fandom.com/wiki/Poe_Coins · https://afk-arena.fandom.com/wiki/Labyrinth_Tokens · https://afk-arena.fandom.com/wiki/Gladiator_Coins ·
https://afk-arena.fandom.com/wiki/Hero_Coins · https://afk-arena.fandom.com/wiki/Guild_Coins · https://afk-arena.fandom.com/wiki/Twisted_Essence ·
https://afk-arena.fandom.com/wiki/Elemental_Core · https://afk-arena.fandom.com/wiki/Elemental_Shard · https://afk-arena.fandom.com/wiki/Store ·
https://afk-arena.fandom.com/wiki/Achievements · https://afk-arena.fandom.com/wiki/Wall_of_Legends · https://afk-arena.fandom.com/wiki/Magician_Hat

---

## 9. Monetarisierung

### 9.1 VIP

- VIP-EXP aus **Player-Levels** (gratis) und **Käufen**; Perks gehen nie verloren.
- Perks: AFK-Gold/EXP-Bonus, Fast-Reward-Nutzungen, Arena-Gratiskämpfe, Bounty-Slots, Heldenplätze, Battle Speed, Skip/Quick Battle,
  Labyrinth-Boni, längere AFK-Caps (bis VIP 6 belegt), Stargazer-Unlock (VIP 13), Monthly-Card-Stufen.

| VIP | VIP-EXP | AFK Gold/EXP | Fast Rewards/Tag | Arena gratis/Tag | Auswahl weiterer Perks |
|---|---|---|---|---|---|
| 1 | 100 | +5 % | 2 | 1 | – |
| 3 | 1 000 | +20 % | 3 | 2 | AFK-Cap 14 h |
| 6 | 7 000 | +50 % | 5 | 3 | AFK-Cap 24 h, 2 Team Bounties, 3 Guild-Hunt-Versuche, Arena-Skip |
| 9 | 20 000 | +90 % | 7 | 5 | Bounty Autofill (afk.guide) |
| 10 | 30 000 | +100 % | 7 | 5 | 4× Battle Speed (Wiki), "Max Achievable by F2P" |
| 12 | 80 000 | +150 % | 8 | 6 | Labyrinth Gold +100 % |
| 13 | 150 000 | +160 % | 8 | 7 | Stargazer-Unlock |
| 15 | 500 000 | laut afk.guide +120 % (Datenfehler wahrscheinlich) | 12 | 7 | – |

- **Max-VIP:** afk.guide nennt VIP 15; die Wiki-Tabelle listet bis VIP 36 (VIP 18 = 5 Mio., VIP 36 = 50 Mio. VIP-EXP) – **nicht verifiziert**.

### 9.2 Abos & Karten

| Produkt | Inhalt |
|---|---|
| **Monthly Card** | 300 💎 + 300 VIP-EXP sofort, **100 💎/Tag**, insgesamt 3 300 💎; seit 1.201 auch Time Emblems wählbar |
| **Deluxe Monthly Card** | 980 💎 + 980 VIP-EXP sofort, **400 💎/Tag**, insgesamt 12 980 💎; seit 1.201 mit Draconis Insignias |
| **Subscription ("Noble")** | $1,39/Woche oder $3,99/Monat; +1 Team Bounty, +10 % Base-AFK-Gold, +1 Mercenary-Nutzung in King's Tower/Towers/Campaign, +10 % Guild Coins und Labyrinth Tokens; **kein VIP-EXP** |

Beide Monatskarten sind gleichzeitig aktivierbar; höhere VIP-Stufen erhöhen die tägliche "Rewards Level"-Stufe der Karten.

### 9.3 Passes ("Noble Society")

| Pass | Fortschritt durch | Free-Spur | Premium-Spur | Dauer |
|---|---|---|---|---|
| **Regal Rewards** | Medals of Valor aus Daily/Weekly-Quest-Truhen | Rare Soulstones (bis 1 650 pro Runde) | +550 Elite Soulstones, +5 500 💎 (älterer Stand); seit 1.201 auch Draconis Insignias; Premium $24,99 (älterer Stand) | früher 35 Tage, seit 1.201: 28 Tage |
| **Twisted Bounties** | Twisted Sigils aus Twisted-Realm-Rängen | Auswahl Hero EXP/Twisted Essence/Poe Coins/Gold | mehr davon + Diamanten; seit 1.201 T5 Rune Core Choice Chests; 1 630 VIP-EXP | früher 44, jetzt 28 Tage |
| **Champions of Esperia** | Heroic Merits aus dem Labyrinth (Kane) | Hero's Essence | mehr + Diamanten; seit 1.201 Highborn Dragon Chests; 1 630 VIP-EXP | früher 44, jetzt 34 Tage |

- **Perk Levels 1–5:** Jede neue Runde erhöht die Pass-Stufe → mehr Belohnungen für treue Käufer.
- Premium-Käufer können in den letzten 4 Tagen fehlenden Fortschritt mit Diamanten nachkaufen.
- Weitere Passes in Events (z. B. "Illusion Passes" in Illusory Journey).

### 9.4 Packs & Shop-Tabs (Merchants)

- **Visiting Merchant / Advancement Rewards:** Diamanten-Rückfluss nach Kapitelabschlüssen (Kauf einmalig).
- **Merchant Ship:** Daily/Weekly/Monthly Deals (enthalten "almost every item you can find in the game") + Special Monthly Bundle.
- **Heroic Ship / Hero Growth Bundles:** Bundles schalten Stufen frei, wenn ein bestimmter Held aufsteigt (Sichtbarkeit nach VIP).
- **Dimensional Deals:** Crossover-Helden direkt kaufen; erster Held einer IP-Kollaboration mit Rabatt (85 % Preis). Alternativ gratis über
  **Exchange-Events** (Währung vorher sammeln, 7 Tage Tauschfenster) bzw. später im Challenger/Labyrinth Store.
- **Wardrobe:** Skins (+980 VIP-EXP pro Kauf).
- **Event Deals:** z. B. Magic Tickets ($29,99/$49,99/$99,99), Temporal-Feedback-Bundles (geben "Continuum Points").
- **Stargazer Cards** fast nur aus Echtgeld-Paketen ("earned from spending real money on in-game packages").
- **Free-Offers:** Merchants enthalten tägliche Gratis-Angebote (afk.guide: "Check Deals daily for free rewards").

### 9.5 Free vs. Paid & F2P-Wahrnehmung

- **Alles ist grundsätzlich gratis erreichbar** ("all the items sold by the Merchants can also be obtained for free through events and in-game
  features"), Geld beschleunigt.
- Großzügige Diamanten-Quellen (Achievements allein 30 000 💎, Kompensations-Mails, Codes) und Pity-artige Systeme (Hero Choice Card alle
  100 Summons, Wishlist) gelten als F2P-freundlich für Story/Campaign.
- Community-Tenor (Reddit, 2023–2024): F2P kommt stetig voran, stößt aber in **kompetitiven Modi** (Legends' Championship, Top-Leaderboards)
  an eine Decke; "competing in the most demanding PvP mode is hopeless without heavy investment". Spätes Spiel wird als endlose
  "upkeep" empfunden.
- Kennzahlen: Lifetime Revenue per Download ca. 22,10 USD global, 53,54 USD in Tier-1-Ost (AppMagic via Udonis).

Quellen: https://afk-arena.fandom.com/wiki/VIP_Rank · https://afk.guide/afk-arena-vip-ranks/ · https://afk-arena.fandom.com/wiki/Merchants · https://afk-arena.fandom.com/wiki/Deals ·
https://afk-arena.fandom.com/wiki/Regal_Rewards · https://afk-arena.fandom.com/wiki/The_Twisted_Realm · https://afk-arena.fandom.com/wiki/Update_1.201_Patch_Notes ·
https://afk-arena.fandom.com/wiki/Dimensionals · https://afk-arena.fandom.com/wiki/Exchanging_Dimensionals · https://afk-arena.fandom.com/wiki/Stargazing_Room ·
https://www.blog.udonis.co/mobile-marketing/mobile-games/afk-arena-monetization · https://lr.in.psf.lt/r/afkarena/comments/17iouug/i_beat_the_game/k6w9lwx/?context=3

---

## 10. Retention-Design

### 10.1 Tägliche / wöchentliche Struktur

**Daily Quests** (Activity Points, Truhen bei 20/40/60/80/100):

| Quest | Punkte |
|---|---|
| Begin a Battle | 20 |
| Summon a Hero at the Tavern | 20 |
| Begin a Battle in the Arena of Heroes | 20 |
| Level Up Your Hero 1 Time | 10 |
| Collect Loot 2 Times | 10 |
| Enhance Your Gear 1 Time | 10 |
| Gift a friend Companion Points | 10 |
| Use Fast Rewards 1 Time | 10 |
| Begin a Battle in the King's Tower | 10 |
| Take Part in 1 Guild Hunt | 10 |
| Begin 3 Solo Bounty Quests | 10 |
| Claim 1 Oak Inn Friend's Gift | 10 |
| Purchase Items from the Store 1 time | 10 |

Truhen: 20 → kleine Essence · 40 → Gold-Hourglass · 60 → 5 Rare Soulstones + 2 Arena Tickets · **80 → 100 💎** · 100 → 1 Common Scroll.
Zugleich: **1 Guild-EXP pro Activity Point** und Fortschritt im Regal-Rewards-Pass. Max. Tagespunkte 100 (ab 2-12), später 140 (ab 3-12).

**Weekly Quests** (Truhen 20–150): z. B. 20 Summons, 2 Ascensions, Labyrinth-Bosse, 15 Arena-Siege, 5 Team Bounties, 40 Furniture-Draws,
3 Twisted Realms, je 1× Cursed Realm, Nightmare Corridor, Treasure Scramble, Collections, 10 Beast Lures. Truhen u. a. 300 💎 und 500 💎,
Dura's Tears, Common Scrolls, Reset Scroll, Wish Leaves.

→ Die Daily-Liste ist ein **"Rundgang durch alle Systeme"** in wenigen Minuten; die Weekly-Liste deckt die wöchentlichen Modi ab.

### 10.2 Timer, die zurückholen

| Timer | Rhythmus |
|---|---|
| AFK-Cap | 12 h (VIP länger) → 1–2 Logins pro Tag |
| Daily Reset | 00:00 UTC (Quests, Stores, Bounties, Fast Reward gratis, Arena gratis, Wrizz) |
| Bounty Board | tägliche neue Quests, Abholung nach Ablauf |
| Labyrinth | 48 h |
| Twisted Realm | Session-basiert mit Boss-Rotation; Cursed Realm & Nightmare Corridor wöchentlich |
| Mercenary-Reset | Sonntag 00:00 UTC |
| Arena / Challenger | 2-Wochen-Saisons; Challenger-Coins stündlich mit Cap |
| Treasure Scramble | Dispatch 02:00 und 14:00 UTC, 10-Tage-Saisons |
| Temporal Rift | 7 Karten pro Woche, 90-Tage-Saison |
| Isle of Gold | 6 Würfe alle 12 h (Cap 30) |
| Oak Inn | Zimmer-Upgrades 6 h / 10 h |
| Hero-Releases | ca. alle 2 Wochen mit 7-Tage-Sign-In |
| Passes | 28–34-Tage-Runden mit Perk Level 1–5 |
| Field of Stars | Starburst-Freischaltung braucht Zeit (Beschleunigung 20 💎) |

### 10.3 Login- und Comeback-Belohnungen

- **Sign-In-Events** zu fast jedem Hero-Release (7 Tage: Diamanten, Time Emblems/Insignias, Stargazer Scrolls) und saisonal ("Autumn Delights":
  7 Logins → Hero Choice Chest).
- **"New Login Event"** und "Warm Reunion" (Server-übergreifender Fortschritt, tägliche Belohnungen).
- **Kompensations-Mails** bei Wartung/Bugs (Diamanten).
- **Launch-Perks**: Gratis-Summon-Währung für neue Helden ab Stage 16-1.

### 10.4 Soziale Bindung

- **Freunde** (bis 40): Companion Points senden/empfangen (Quellen: senden 30 oder 40, empfangen 20 pro Tag), Oak-Inn-Besuche mit Geschenken,
  Mercenary-Verleih, Support Heroes für Unions/Bounties, Freundes-Challenge.
- **Gilde:** tägliche Hunts, Activity-Beitrag, geteilte Belohnungen (Twisted Realm Legend Points, Fortune Chests, Guild Haggle), Gildenkriege,
  Militia (Abyssal), Gilden-Chat mit Event-Bubbles und angepinnten Logs (1.204).
- **Server-Wettbewerb:** **Wall of Legends** (Server-Erstleistungen geben **allen** Spielern des Servers 50 💎 → positive soziale Wirkung).
- **Asynchrones Wissen teilen:** Replays von Campaign/Tower, Formation-Empfehlungen anderer Spieler mit "copy in one tap" (Guild Hunt, 1.204).
- **Community-Content:** Esperian Idols (Abstimmung über Lieblingshelden), Dolly's Corner, Developer-Feedback-Briefe, Redemption Codes.

### 10.5 Langzeit-Ziele

- Achievements (Badges/Sticker + bis 30 000 💎), Avatar Frames, Portraits/Sammlung, Faction Ladders (Punkte pro Ascension), Collections,
  Oath Mark Codex – alles belohnt **Breite** des Rosters, nicht nur die Top 5.

Quellen: https://afk-arena.fandom.com/wiki/Quests · https://afk-arena.fandom.com/wiki/Friends · https://afk-arena.fandom.com/wiki/Companion_Points ·
https://afk-arena.fandom.com/wiki/Wall_of_Legends · https://afk-arena.fandom.com/wiki/Achievements · https://afk-arena.fandom.com/wiki/Store ·
https://afk-arena.fandom.com/wiki/Update_1.203_Patch_Notes · https://afk-arena.fandom.com/wiki/Update_1.204_Patch_Notes · https://afk-arena.fandom.com/wiki/Events

---

## 11. Was Mythwake übernehmen kann

**Grundregel:** Mythwake übernimmt **Prinzipien**, keine Namen, Figuren, Lore oder Art aus AFK Arena. Alle Mythwake-Begriffe unten stammen
aus `docs/GAME_DESIGN.md` (Corex-Dokument). Wo das Dokument eine Lücke hat, steht das ausdrücklich dabei – das Team entscheidet, nicht
diese Recherche.

### 11.1 Zuordnung AFK Arena → Mythwake-Systeme

| AFK-Arena-Prinzip | Mythwake-System (laut Design-Doc) | Übernehmbares Prinzip |
|---|---|---|
| Campaign + AFK-Truhe mit Stunden-Cap, Fast Rewards | **Idle Progression System** (Stage-Push oder Farmen, Zeitlimit normal/Premium) | Belohnungen in "Stunden Ertrag" denken; Cap zwingt zu 1–2 Logins/Tag; ein Gratis-Schnellabruf pro Tag; Truhe zeigt visuell, wie voll sie ist. |
| Ranhorn (Stadt-Hub mit Gebäuden = Menüs) | **Building System** / Dorf als Main Hub (Survivor Camp, Broken Forge, Healer's Tent, Hunter's Hut, Watchtower, Fragment Shrine, Summoning Gate, Guardian Shrine, Portal Chamber) | Gebäude **sind** das Menü; gesperrte Gebäude sichtbar mit Unlock-Hinweis = Roadmap. Mythwake geht weiter: Gebäude werden **wieder aufgebaut/geupgradet** und haben **Helden-Slots** (Village Style) – das hat AFK Arena so nicht. |
| Dark Forest (Modus-Hub) | Portal Chamber (Dungeons, Events, Shattered Realm), Hunter's Hut (Hunts), Watchtower (Defense) | Kampfmodi räumlich bündeln, aber in Mythwake über die eigenen Gebäude statt über einen zweiten Hub – oder bewusst ein zweiter Hub, falls das Team das will (nicht im Doc festgelegt). |
| Faction-Wheel (+25 % Schaden) + Team-Faction-Boni | **Fragment Aspects** (Ravak, Somnara, Envyra, Gorath, Veloria) als Vorteilszyklus; **Faction System** und **Race/Species System** als Team-Synergien | Zwei getrennte Achsen wie in AFK Arena: eine **Konter-Achse** (dort Faction, bei Mythwake die Aspects) und eine **Synergie-Achse** (2/3/5-Boni). Doc-Lücke: Es nennt "seven Fragment Aspects", benennt aber nur fünf – Team muss entscheiden. |
| Celestial/Hypogean als "Joker"/eigene Bonusleiter, Dimensional neutral | Factions des Docs (z. B. The Hollowed) | Prinzip: eine Sondergruppe mit **eigener Bonuslogik** macht Teambau interessanter; welche Mythwake-Gruppe das sein könnte, entscheidet das Team. |
| Kein manuelles Targeting, Targeting pro Skill | Team Formation: Doc **wünscht** Zielprioritäten ("Target mages first …") | Hier geht Mythwake bewusst **über** AFK Arena hinaus. Empfehlung: Targeting-Regeln pro Skill wie AFK Arena als Basis, Spieler-Priorität als Zusatz. |
| 5er-Formation, Front/Back | 2 Frontline + 3 Backline, Slots durch Fortschritt freischalten | AFK Arena startet mit vollen 5; Mythwakes Slot-Freischaltung ist ein zusätzliches frühes Ziel. |
| Manuelle Ults + Auto-Toggle, 90-s-Kampf | Skill and Ultimate System | Bereits so entschieden (manuelle Ults + Auto). Bosse, die Ult-Timing bestrafen/belohnen (Kane, Grotesque Mage), machen den manuellen Modus wertvoll. |
| Ascension mit Fodder + Resonating Crystal | **Hero Upgrade System** (Level, Rank, Stars, Skills, Awakening) | **Nur wenige Helden echt leveln, Rest wird mitgezogen** (Crystal-Prinzip) und **volle Rückerstattung** bei Reset → Spieler probieren mehr Teams aus (Doc-Ziel: "not only rely on one strongest meta team"). |
| Signature Item (Held-spezifisch, ab Mythic) | **Relics** (jeder Held 1 Relic, einzigartige Effekte, Upgrade + Rarity) | Ein Held-gebundener Slot, der eine **Skill-Mechanik verändert**, nicht nur Stats; Stufen 10/20/30 schalten Effekt-Upgrades frei. |
| Furniture-Sets (3/9 Teile → Fähigkeit) | **Gear Set Bonus** ("all equipment from one set") | Teil-Schwellen (z. B. 2/4 Teile) statt nur Vollset geben frühere Erfolgserlebnisse. |
| Faction-Gear (+30 %), Gear-Tiers, Gear Resonance | **Gear System** (Weapon, Armor, Boots, Accessory, Relic; Upgrade/Enhance/Refine/Craft) | "Resonance" gegen Gear-Grind: niedrige Teile automatisch auf ein freigeschaltetes Niveau heben. |
| Engravings, Elder Tree, Dura's Virtues | **Fragment System** (Core/Aspect/Echo Fragment, Fragment Dust, Fusion, Drawbacks) | Fragments als **Build-Entscheidung** (Doc: "should not only increase stats"); globale Klassen-Boni à la Virtues könnten am **Fragment Shrine** hängen. |
| Awakened Heroes, Legendary Epic | **Guardian Awakening System** + Guardian Shrine | Awakening als **eigener Content-Moment** mit Story-Abenteuer (Doc: "unlock a new story … or a special dungeon") – AFK Arena macht genau das mit Legendary Epic. |
| Tavern (Common/Faction/Companion/Choice), Stargazer, Wishlist, 30er-Garantie, Hero Choice Card alle 100 | **Summoning System** (Normal, Faction, Fragment, Event, Guardian Summon; Pity) | Faction-Summon mit **Tagesrotation**; **Wishlist** (bestimmt *welcher* Held, nicht *ob*); sichtbarer Fortschrittsbalken zu einer **Wahl-Belohnung**; teurer Ziel-Summon für Spitzenhelden. |
| Bounty Board (Expeditionen) | **Hunt Board System** (Hunter's Hut; Daily/Weekly; Schwierigkeit steigt mit Gebäudelevel) | Dispatch-Logik (Helden mit Anforderungen losschicken), tägliche Refresh-Liste, Leihhelden von Freunden; Raritätsstufen der Aufträge. |
| King's Tower, Faction Towers (nur eine Faction, Wochentags-Rotation) | **Trial of Fragments** (Tower pro Aspect; Team muss passenden Aspect haben) | Fast 1:1 als Prinzip: **Aspect-gebundene Türme mit Wochentags-Rotation** und Floor-Meilensteinen. |
| Arcane Labyrinth (Roguelite, temporäre Relics, Dismal mit Bann-Factions) | **Dungeon System** (Endless/Wave), **Shattered Realm System** (Regeln pro Realm) | Temporäre Lauf-Buffs, keine Heilung zwischen Kämpfen, Wegwahl; Dismal-Prinzip (**gebannte Gruppen + Debuffs**) passt zu "Each Shattered Realm can have its own rules" (Realm of Ash, Stillness, Reflection, Hunger). |
| Peaks of Time / Voyage / Misty Valley | **Region Map System**, **Event Portal System** | Erkundungskarten mit Truhen und Rätseln; Bonus-Gates (Stone/Silver/Gold) als Herausforderungsstufen pro Stage. |
| Twisted/Cursed Realm, Wrizz/Soren (Damage-Race-Bosse, Enrage, CC-Immunität, Divisionen) | **Guardian Raid System** (Celestial Turtle, Dragon of the Sky, Hollowed Leviathan, Six-Legged Worldbearer, Craterborn Guardian) | **Damage-Leaderboard mit Truhenstufen**, nur bester Versuch zählt, Gratis-Versuche + Kauf-Versuche, Boss-Rotation mit **einer klaren Signatur-Mechanik pro Boss** (z. B. "tötet Top-DPS", "verbietet Ults", "Wetter verhindert Heilung"). Gilde öffnet einen Spezialboss mit gesammelten Aktivitätspunkten. |
| Guild Level über Daily-Aktivität, Golden Frontier, Abyssal Expedition | **Guild System** → **Guild Castle** (Guild Forge, Raid Hall, Guild Shrine, Guild Market, Training Grounds, War Room) | Gildenfortschritt **automatisch aus Daily Quests** speisen (keine Extra-Arbeit); Gebäude-Ausbau mit gemeinsamen Ressourcen (Doc) ist eine Erweiterung, die AFK Arena nicht hat. |
| Arena of Heroes, Legends' Challenger (3 Teams), Championship | **Arena System** (Attack/Defense, Targeting-Prioritäten, Ränge Bronze → Shattered King, Saisons) | 2-Wochen-Saisons, gratis Versuche + Tickets, Replays; mehrteamiges Format als späteres Endgame-PvP. |
| Events mit Hero-Launch-Kette, Saison-Events, Isle of Gold etc. | **Event Portal System** (eigene Story, Gegner, Boss, Währung, Shop, limitierter Held/Skin) | Jedes Portal als **Paket**: Story + Stages + Boss + Shop + Sign-In; wiederverwendbare Event-Templates (Brettspiel, Tile-Picking, Lotterie) senken Content-Kosten. |
| Library Unions (Helden-Kombos), Field of Stars | **Race/Faction-Synergien**, **Hero Leaders** | Konkrete Helden-Kombos mit Story-Text als Sammelanreiz. |
| Oak Inn (Helden wohnen, Freunde besuchen) | **Village Style** (Helden in Gebäude-Slots für Dorf-Boni, gleichzeitig kämpfend) | Mythwakes Ansatz ist mechanisch stärker (echte Boni). Prinzip übernehmen: Sammlung **sichtbar im Hub** machen + sozialer Besuch. |
| Bosse alle X Campaign-Stages | **Boss System** (Boss alle 5 oder 10 Stages; Hollowtusk Alpha, Riftborn Treant, Crimson Wraith Adept …) | AFK-Arena-Bosse in der Campaign sind meist Heldenteams; Mythwake will **echte Boss-Monster** – teurer, aber unterscheidend. |
| Boss-Gegner = Helden-Assets | **Enemy System** (Wild/Corrupted Beasts, Hollowed, Crimson Wraiths, Riftborn, Fragment Constructs, Corrupted Guardians) | Kostenhebel: "corrupted"-Varianten eigener Helden als Gegner/Bosse wiederverwenden (passt lore-seitig zu Hollowed und Corrupted Guardians). |
| VIP, Monthly Cards, Passes mit Perk Levels | **Monetization Systems** (Battle Pass = "Guild Contract", Monthly Card, Idle Reward Boosts, Starter/Village Packs) | Pass-Fortschritt aus Daily/Weekly-Quests (kein Extra-Grind); Monthly Card mit Sofort- + Tagesanteil; Abo mit Komfort-Perks statt Power. Doc-Vorgabe: Monetarisierung darf die Lore nicht brechen. |
| Daily/Weekly Quests mit Activity-Truhen | **Quest System** (Daily, Weekly, Village, Faction, Hunt, Event) | Daily-Liste als **Rundgang durch alle Systeme** (5 Minuten); Activity Points füttern gleichzeitig Gilde und Pass. |

### 11.2 Wichtigste Design-Prinzipien (Kurzfassung)

1. **Ein Fortschrittswert treibt alles:** Die Campaign-Stage bestimmt Idle-Rate und Feature-Unlocks. Jedes neue System erscheint, wenn das
   vorherige "voll" ist.
2. **Stunden als Belohnungseinheit** → Belohnungen skalieren automatisch mit dem Fortschritt.
3. **Breiter Roster ohne Grind-Strafe:** Crystal-Prinzip + volle Rückerstattung + globale Boni für Sammlungsbreite (Elder Tree, Codex, Achievements).
4. **Jeder Modus = eigener Rhythmus + eigene Währung + eigener Shop.** Unterschiedliche Timer (12 h, 24 h, 48 h, Woche, 2 Wochen, Saison) verteilen Logins.
5. **Bosse mit einer Signatur-Mechanik** (eine Regel pro Boss), CC-immun, Enrage, Damage-Truhenstufen → Teambau statt reiner Power.
6. **Normierte Modi** (alle Helden auf festem Level/Gear, z. B. Treasure Scramble, Fight of Fates) halten Endgame-Wettbewerb fair und testen Wissen.
7. **Sozial ohne Zwang:** Leihhelden, Companion Points, Server-Erstleistungen, die allen nützen, Gildenfortschritt aus normaler Aktivität.
8. **Content-Effizienz:** Helden-Assets als Gegner, Event-Templates, Wetter-Effekte als Variation, alle 2 Wochen ein Held mit fester Eventkette.

### 11.3 Was Mythwake bewusst anders machen sollte (aus AFK-Arena-Schwächen)

- **Systeminflation:** AFK Arena hat > 15 überlappende Progressionsschichten; Spieler empfinden das späte Spiel als endlose "upkeep". Mythwakes
  Doc erlaubt Vereinfachung ("We can also combine something here if we want to simplify it") – nutzen.
- **Fodder-Ascension** (180 Rare-Helden für einen Ascended) ist für neue Spieler schwer verständlich; Mythwake plant Stars über Duplikate/Shards.
- **Kein Spieler-Targeting** in AFK Arena – Mythwake-Doc will es, ein klares Unterscheidungsmerkmal.
- **Gegner sind meist Heldenteams** → wenig Monster-Identität. Mythwakes Enemy-Gruppen und benannte Bosse sind eine Chance für mehr Welt.
- **PvP-Pay-Wall** wird in der Community kritisiert → normierte Wettbewerbsmodi früh einplanen.

### 11.4 Offene Fragen an das Team (aus dem Abgleich mit dem Design-Doc)

- Fragment Aspects: Das Doc sagt "seven", benennt fünf (Ravak, Somnara, Envyra, Gorath, Veloria). Bleibt es bei fünf?
- Soll es einen zweiten Hub (Kampfmodi) neben dem Dorf geben, oder hängen alle Modi an Dorfgebäuden?
- Gibt es eine Sondergruppe mit eigener Bonuslogik (Pendant zu Celestial/Hypogean/Dimensional)? Das Doc legt das nicht fest.
- Welche Mythwake-Bosse sollen eine Signatur-Mechanik bekommen, und welche Gegner dürfen "corrupted"-Varianten von Helden sein?

---

## 12. Lücken & offene Punkte

| Thema | Status |
|---|---|
| AFK-Cap über VIP 6 hinaus | Wiki nennt 36 h+ für VIP 7+, nicht verifiziert |
| Fast-Rewards-Preisstufen nach 50 💎 | nicht verifiziert |
| Max-VIP-Level | afk.guide: 15; Wiki: bis 36 – widersprüchlich |
| Kampfdauer | ca. 90 s aus Boss-Timern abgeleitet, nicht direkt belegt |
| Formation 2 vorne / 3 hinten | Spielkenntnis, Begriffe Front/Backline belegt |
| Tavern-Pity (Elite alle 3 Zehnerpulls) | Community-Beobachtung, nicht offiziell |
| Stargazer-Pity | afk.guide: 65 bzw. 70 Pulls, widersprüchlicher Zweitwert |
| Celestial/Hypogean-Wishlist, aktuelle Wishlist-Regeln | nur alter Stand (5 pro Faction) belegt |
| Signature-Item-Max (+30/+40/+50?) | widersprüchlich |
| Gear-Gruppen (Typ vs. Class) | widersprüchlich |
| Twisted-Realm-Rhythmus | nicht verifiziert |
| Legends' Championship Format/Belohnungen | nur Grundformat belegt |
| Bounty-Laufzeiten | nicht verifiziert |
| Bottom-Bar-Reihenfolge, Badge-Regeln, Summon-Animation, Tutorial-Dialoge | nicht verifiziert (Screenshots nötig) |
| Draconis-Faction-Vorteil im Detail, Dragon Isle, Dragon Trials | nicht verifiziert |
| Guild Tech/Forschung | nicht gefunden |

**Empfehlung:** Für UI-Fragen das Spiel selbst installieren und Screenshots der Kernscreens (Campaign, Ranhorn, Dark Forest, Hero Detail,
Formation, Battle, Result, Tavern) in `docs/research/` ablegen.
