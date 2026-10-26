"""Writes the Codex jobs for the UI kit: icon sheets (12 icons per picture in a fixed grid, so one style per sheet),
frames and buttons for 9-slicing, and painted screen backgrounds. Each job folder gets run_prompt.txt,
style_reference.png and cells.json (grid + names) for import_ui.py. Same runner as the town jobs.

usage: py -3 make_ui_jobs.py <output dir> <style reference png>
"""
import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "town"))
from make_town_jobs import HEADER, RUNNER  # noqa: E402

STYLE = """UI-STIL (für alle Teile gleich, sehr wichtig): wie die UI von 7DS Idle Adventure / AFK Arena – handgemalte Spiel-Icons, kräftige, klare Formen, dunkelbraune bis fast schwarze Outline (gleichmäßig, ca. 3–4 px bei 1024 px), Cel-Shading mit 2–3 Tonstufen plus weichem Verlauf, ein kleiner weißer Glanzpunkt, warme satte Farben, goldene Metallkanten wo passend, leicht schräg präsentiert (3/4). Gut lesbar auch bei 64 px. NICHT: Plastik-Glanz, Neon, wilde Partikel, Rauschen, Fotorealismus, generischer KI-Look, Schrift."""

GRID = """FORMAT: Bild {w}x{h}, reiner weißer Hintergrund (#FFFFFF), Raster mit {cols} Spalten × {rows} Zeilen gleich großer Zellen (die Zellen selbst NICHT zeichnen – keine Linien, keine Kästen). Jedes Motiv steht zentriert in seiner Zelle, füllt ca. 70 % der Zelle, berührt keine Zellengrenze, wirft keinen Schatten auf den Hintergrund. Reihenfolge zeilenweise von links oben nach rechts unten:
{items}"""

FRAME = """FORMAT: Bild {w}x{h}, reiner weißer Hintergrund (#FFFFFF) außen herum. {what}
Die Teile werden im Spiel per 9-Slice gestreckt: Ecken dürfen verziert sein, die geraden Kanten dazwischen müssen auf ganzer Länge GLEICHFÖRMIG sein (kein Ornament in der Kantenmitte), die Innenfläche ruhig und gleichmäßig (sanfter Verlauf ok, keine Muster)."""

# (folder id, kind, size, cols, rows, [(name, description)], extra)
ICON_SHEETS = [
    ("icons_currency_1", [
        ("gold", "Gold: kleiner Stapel goldener Münzen"),
        ("heroExp", "Hero EXP: grünes, leuchtendes Elixier in bauchiger Glasflasche mit Korken"),
        ("gems", "Gems: großer facettierter blauer Edelstein"),
        ("summonScroll", "Summon Scrolls: gerollte Schriftrolle mit violettem Siegel und Goldenden"),
        ("fragmentDust", "Fragment Dust: kleiner Lederbeutel, aus dem rosa-violett glitzernder Kristallstaub rieselt"),
        ("wood", "Wood: zwei gekreuzte Holzscheite mit Schnittflächen"),
        ("stone", "Stone: zwei behauene graue Steinblöcke"),
        ("guardianSigil", "Guardian Sigils: rundes himmelblau-goldenes Siegelamulett mit Schild-Prägung"),
        ("gearMaterials", "Gear materials: Barren aus Eisen mit Schraube und Zahnrad"),
        ("relicMaterials", "Relic materials: kleiner antiker Bronzesplitter mit türkiser Rune"),
        ("upgradeStones", "Upgrade stones: grüner Wetzstein mit leuchtendem Pfeil-nach-oben-Kerbmuster"),
        ("huntTokens", "Hunt Tokens: Holzmarke mit eingebrannter Kralle, an Lederband"),
    ]),
    ("icons_currency_2", [
        ("arenaCoins", "Arena Coins: silberne Münze mit gekreuzten Schwertern"),
        ("guildCoins", "Guild Coins: bronzene Münze mit Banner-Prägung"),
        ("realmKey", "Shattered Realm Key: verzierter Schlüssel mit violettem Splitter im Griff"),
        ("heroShards", "Hero Shards: drei goldene Seelensplitter, die zusammen einen Stern bilden"),
        ("chest", "geschlossene Holz-Schatztruhe mit Goldbeschlägen"),
        ("chestOpen", "geöffnete Schatztruhe, aus der Gold strahlt und Münzen quellen"),
        ("mail", "versiegelter Briefumschlag mit rotem Wachssiegel"),
        ("lock", "goldenes Vorhängeschloss"),
        ("star", "goldener Stern (Bewertung/Sterne eines Helden)"),
        ("starEmpty", "derselbe Stern, aber leer: nur dunkler, matter Steinstern"),
        ("plus", "dickes goldenes Plus-Zeichen in einem runden grünen Edelstein-Knopf"),
        ("check", "dicker weißer Haken in einem runden grünen Knopf"),
    ]),
    ("icons_nav", [
        ("navTown", "Dorf Dawnhollow: kleines Fachwerkhaus mit Strohdach und Fahne"),
        ("navAdventure", "Abenteuer: aufgerollte Landkarte mit Kompass"),
        ("navCampaign", "Kampagne: zwei gekreuzte Schwerter vor einem Rundschild"),
        ("navHeroes", "Helden: Ritterhelm mit Federbusch"),
        ("navSummon", "Beschwörung: leuchtendes rundes Portal mit goldenem Steinring"),
        ("quests", "Quests: Schriftrolle mit Feder"),
        ("shop", "Shop: prall gefüllter Händlerbeutel mit Münzen"),
        ("guild", "Gilde: Gildenbanner an einer Stange mit Schild-Wappen ohne Schrift"),
        ("arena", "Arena: goldener Siegerpokal mit gekreuzten Schwertern"),
        ("dungeons", "Dungeons: steinernes Kerkertor mit Fackeln"),
        ("huntBoard", "Hunt Board: Holzbrett mit angehefteten Zetteln und Kralle"),
        ("trials", "Trial of Fragments: hoher Turm mit Kristall an der Spitze"),
    ]),
    ("icons_menu", [
        ("realms", "Shattered Realms: zerbrochenes violettes Portal mit schwebenden Felsbrocken"),
        ("raid", "Guardian Raid: riesige Klaue eines Guardians mit goldenen Ringen"),
        ("watchtower", "Watchtower: hölzerner Wachturm mit Glocke"),
        ("bossFragments", "Boss Fragments: dunkler Kristall mit Hörnern, rot glimmend"),
        ("relics", "Relics: antikes goldenes Artefakt (Kelch) mit Edelstein"),
        ("fragments", "Fragments: drei schwebende, kantige Kristallsplitter in Gold und Violett"),
        ("awakening", "Awakening: goldene Flügel um einen leuchtenden Stern"),
        ("formation", "Formation: fünf kleine Schilde in zwei Reihen angeordnet"),
        ("settings", "Einstellungen: Zahnrad aus Bronze"),
        ("ranking", "Rangliste: Podest mit Lorbeerkranz"),
        ("events", "Event Portal: strahlendes Portal mit Stern und Konfetti-Funken"),
        ("forge", "Schmiede: Amboss mit Hammer"),
    ]),
    ("icons_controls", [
        ("back", "Zurück: dicker Pfeil nach links in einem runden Knopf"),
        ("close", "Schließen: dickes X in einem runden roten Knopf"),
        ("info", "Info: weißes i in einem runden blauen Knopf"),
        ("auto", "Auto-Kampf: Kreispfeil um ein Schwert"),
        ("speed", "Doppelte Geschwindigkeit: zwei Vorspul-Pfeile"),
        ("upgrade", "Verbessern: dicker grüner Pfeil nach oben"),
        ("levelUp", "Level up: goldener Doppelpfeil nach oben mit Funken"),
        ("filter", "Filter: Trichter"),
        ("sort", "Sortieren: zwei Pfeile auf und ab"),
        ("swap", "Tauschen: zwei Pfeile im Kreis"),
        ("power", "Kampfkraft: Faust mit goldenen Strahlen"),
        ("time", "Zeit: Sanduhr"),
    ]),
    ("icons_aspect_role", [
        ("aspectRavak", "Fragment Aspect Ravak (Feuer, Wut): rundes Emblem, orange-rote Flamme in einem Kristall"),
        ("aspectSomnara", "Fragment Aspect Somnara (Schlaf, Verlangsamung): rundes Emblem, blauer Mond mit Sternen"),
        ("aspectEnvyra", "Fragment Aspect Envyra (Stehlen, Kopieren): rundes Emblem, türkises Auge in einem Spiegel"),
        ("aspectGorath", "Fragment Aspect Gorath (Regeneration, Wachstum): rundes Emblem, grüner Sprössling aus einem Stein"),
        ("aspectVeloria", "Fragment Aspect Veloria (Bezauberung, Verwirrung): rundes Emblem, rosa Herz mit Spirale"),
        ("roleDps", "Rolle Schadensdealer: gekreuzte Klinge, rotes Abzeichen"),
        ("roleTank", "Rolle Tank: Turmschild, blaues Abzeichen"),
        ("roleSupport", "Rolle Unterstützer: Heilkreuz mit Blatt, grünes Abzeichen"),
        ("roleDebuffer", "Rolle Schwächer: Totenkopf-Trank, violettes Abzeichen"),
        ("roleHeroLeader", "Rolle Hero Leader: Krone mit Banner, goldenes Abzeichen"),
        ("boss", "Boss-Markierung: gehörnter Schädel, rot"),
        ("elite", "Elite-Markierung: goldene Krone mit Flügeln"),
    ]),
    ("icons_stats", [
        ("statAttack", "Angriff: Schwert"),
        ("statHp", "Lebenspunkte: rotes Herz"),
        ("statDefense", "Verteidigung: Schild"),
        ("statSpeed", "Angriffstempo: geflügelter Stiefel"),
        ("statCrit", "Kritische Trefferchance: Zielscheibe mit Blitz"),
        ("statCritDamage", "Kritischer Schaden: zerbrochenes Schwert mit Funken"),
        ("statAccuracy", "Genauigkeit: Auge mit Fadenkreuz"),
        ("statEvasion", "Ausweichen: Feder im Wind"),
        ("statEnergy", "Energie: gelber Blitz in einer Kugel"),
        ("statHealing", "Heilung: grüner Kristalltropfen"),
        ("statShield", "Schild-Barriere: blaue Kuppel"),
        ("statLevel", "Level: Abzeichen mit Lorbeer"),
    ]),
    ("icons_status_1", [
        ("statusStun", "Betäubung: kreisende Sterne"), ("statusSleep", "Schlaf: Zzz-Wolke (nur Formen, keine echten Buchstaben)"),
        ("statusTaunt", "Spott: Schild mit Ausrufezeichen-Form"), ("statusProvoke", "Provokation: wütendes Hornsymbol"),
        ("statusCharm", "Bezauberung: rosa Herz mit Pfeil"), ("statusConfuse", "Verwirrung: Spiralen"),
        ("statusBurn", "Brand: Flamme"), ("statusPoison", "Gift: grüner Tropfen mit Blasen"),
        ("statusAttackUp", "Angriff hoch: Schwert mit grünem Pfeil nach oben"), ("statusAttackDown", "Angriff runter: Schwert mit rotem Pfeil nach unten"),
        ("statusDefenseUp", "Verteidigung hoch: Schild mit grünem Pfeil nach oben"), ("statusDefenseDown", "Verteidigung runter: Schild mit rotem Pfeil nach unten"),
    ], "ALLE als kleine runde Status-Abzeichen gleicher Größe: farbiger Kreis-Hintergrund (Buffs grün/blau, Debuffs rot/violett) mit dem Symbol darin."),
    ("icons_status_2", [
        ("statusDamageTakenUp", "erhöhter erlittener Schaden: zerbrochenes Schild, roter Pfeil"), ("statusDamageTakenDown", "verringerter erlittener Schaden: festes Schild, grüner Pfeil"),
        ("statusAttackSpeedUp", "Tempo hoch: Flügel mit grünem Pfeil"), ("statusSlow", "Verlangsamt: Schnecke/Sanduhr blau"),
        ("statusAccuracyDown", "Genauigkeit runter: verschwommenes Auge, roter Pfeil"), ("statusCritRateUp", "Krit hoch: Zielscheibe, grüner Pfeil"),
        ("statusRegen", "Regeneration: grünes Blatt mit Plus"), ("statusImmune", "Immun: goldene Kuppel"),
        ("statusEvade", "Ausweichen: Schatten-Silhouette mit Bewegungslinien"), ("statusShield", "Schild: blaue Barriere"),
        ("statusEnergyUp", "Energie: gelber Blitz"), ("statusCleanse", "Reinigung: weißer Funkenstern"),
    ], "ALLE als kleine runde Status-Abzeichen gleicher Größe: farbiger Kreis-Hintergrund (Buffs grün/blau, Debuffs rot/violett) mit dem Symbol darin."),
    ("icons_faction", [
        ("factionOrderOfTheVeil", "Order of the Veil: Emblem mit verschleiertem Auge und Buch, silber-blau"),
        ("factionTribeOfTheCrimsonFang", "Tribe of the Crimson Fang: Emblem mit rotem Reißzahn und Krallenspuren"),
        ("factionEmpireOfTheShatteredSky", "Empire of the Shattered Sky: Emblem mit gesprungenem Himmelskreis und Krone, gold-blau"),
        ("factionGuildsOfTheHunt", "Guilds of the Hunt: Emblem mit Bogen und Pfeil über Bestienkralle, grün-braun"),
        ("factionCircleOfAether", "Circle of Aether: Emblem mit Portalring und Stern, violett-türkis"),
        ("factionTheHollowed", "The Hollowed: Emblem mit rissiger Maske und violettem Riss, dunkel"),
    ], "Alle als gleich große Wappen-Embleme (Schildform mit Goldrand)."),
    ("icons_race", [
        ("raceHuman", "Humans: Emblem mit Schwert und Ähre"),
        ("raceVeyr", "Veyr: Emblem mit Mondsichel und Feder (Erinnerung, Mondlicht)"),
        ("raceDrakari", "Drakari: Emblem mit Drachenauge und Flamme"),
        ("raceThalassari", "Thalassari: Emblem mit Welle und Muschel"),
        ("raceStoneborn", "Stoneborn: Emblem mit Berg und Steinfaust"),
        ("raceHollowed", "Hollowed: Emblem mit verdrehter, rissiger Hand"),
        ("raceAetherial", "Aetherials: Emblem mit Portalspirale"),
        ("enemy", "Gegner allgemein: Emblem mit roter Klaue"),
    ], "Alle als gleich große runde Medaillons mit Bronzerand."),
]

# 9-slice chrome: (id, size, cols, rows, [(name, description)])
CHROME = [
    ("chrome_buttons", (1024, 1536), 2, 4, [
        ("btnGold", "Haupt-Button: breite, abgerundete Taste in warmem Gold mit Relief, dunkler Outline und hellem Glanzstreifen oben"),
        ("btnGoldPressed", "derselbe goldene Button gedrückt: etwas dunkler, ohne Glanz"),
        ("btnBlue", "Neben-Button: dieselbe Form in dunklem Königsblau mit Goldrand"),
        ("btnBluePressed", "derselbe blaue Button gedrückt"),
        ("btnRed", "Gefahr-Button: dieselbe Form in Rot mit Goldrand"),
        ("btnGreen", "Bestätigen-Button: dieselbe Form in Grün mit Goldrand"),
        ("btnGray", "deaktivierter Button: dieselbe Form in stumpfem Grau"),
        ("btnTab", "Tab-Reiter: flacher, oben abgerundeter Reiter in dunklem Blau mit dünnem Goldrand"),
    ], "Alle Buttons gleich groß und breit (Seitenverhältnis ca. 3:1), ohne Text."),
    ("chrome_panels", (1536, 1024), 2, 2, [
        ("panel", "Standard-Panel: dunkelblaue Fläche (#1a2238) mit schmalem, gealtertem Goldrahmen und kleinen verzierten Ecken"),
        ("panelLight", "helles Panel: pergamentfarbene Fläche mit braunem Lederrand und Messingecken"),
        ("popup", "Popup-Fenster: dunkelblau mit breiterem, prächtigem Goldrahmen, verzierten Ecken"),
        ("plate", "Namensschild/Leiste: schmale dunkle Holzleiste mit Goldkanten (Seitenverhältnis 4:1)"),
    ], "Jedes Teil ein eigenes Rechteck."),
    ("chrome_cards", (1536, 1024), 3, 2, [
        ("cardCommon", "Heldenkarten-Rahmen Common: grau-silberner Metallrahmen"),
        ("cardRare", "Heldenkarten-Rahmen Rare: blauer Rahmen mit Silberkanten"),
        ("cardEpic", "Heldenkarten-Rahmen Epic: violetter Rahmen mit Goldkanten"),
        ("cardLegendary", "Heldenkarten-Rahmen Legendary: prächtiger goldener Rahmen mit Edelsteinen"),
        ("cardTranscendent", "Heldenkarten-Rahmen Transcendent: rot-goldener Rahmen mit Flammen-Ornament und Sternen"),
        ("cardEmpty", "leerer Platz: dunkler, halbtransparent wirkender Steinrahmen"),
    ], "Alle als gleich große Hochformat-Rahmen (3:4) mit LEERER, rein weißer Innenfläche (die Innenfläche bleibt weiß, dort kommt das Heldenporträt hin); nur der Rahmen ist bemalt."),
    ("chrome_bars", (1536, 1024), 2, 4, [
        ("barFrame", "Fortschrittsbalken-Rahmen: lange, flache dunkle Rinne mit Goldrand"),
        ("barFillGold", "Füllung des Balkens: goldener Streifen mit Glanz"),
        ("barFillGreen", "Füllung: grüner Streifen (Lebenspunkte)"),
        ("barFillBlue", "Füllung: blauer Streifen (Energie)"),
        ("chip", "Währungs-Kapsel: kurze, dunkle, abgerundete Pille mit dünnem Goldrand"),
        ("ribbon", "Titelband: rotes Stoffband mit goldenem Saum und eingerollten Enden"),
        ("badge", "rote Benachrichtigungs-Perle (runder, glänzender roter Punkt mit weißem Rand)"),
        ("tag", "kleines Etikett: abgerundetes dunkelrotes Schildchen"),
    ], "Balken und Bänder sehr breit (Seitenverhältnis ca. 6:1), Rest passend klein."),
    ("chrome_bars_nav", (1536, 1024), 1, 2, [
        ("navBar", "untere Navigationsleiste: breite dunkle Holz-/Steinleiste mit Goldkante oben, ruhig und gleichmäßig über die ganze Breite"),
        ("topBar", "obere Leiste: schmale dunkle Leiste mit Goldkante unten, gleichmäßig über die ganze Breite"),
    ], "Beide Leisten über die volle Bildbreite (keine Enden-Verzierung in der Mitte)."),
]

BACKGROUNDS = [
    ("bg_summon", "Beschwörungs-Halle: ein großer alter Steinbogen (Summoning Gate) mit wirbelndem gold-türkisem Portal in der Bildmitte oben, magische Runen im Boden, Kerzen, ehrfürchtige Stimmung. Unteres Drittel ruhig (dort liegen Buttons)."),
    ("bg_heroes", "Helden-Halle: Innenraum einer Ritterhalle aus Holz und Stein mit Waffenständern, Bannern ohne Schrift, Kamin, warmes Licht; ruhig und leicht unscharf, weil Heldenkarten darüber liegen."),
    ("bg_shop", "Händlerzelt: Inneres eines bunten Händlerzelts mit Regalen voller Tränke, Kisten, Stoffe, Laternen; ruhig und leicht unscharf."),
    ("bg_menu", "Neutraler Menü-Hintergrund: dunkelblauer, leicht strukturierter Stoff/Stein mit dezenten goldenen Ornament-Ecken und weichem Vignettenlicht; sehr ruhig."),
    ("bg_guild", "Gildenhalle: großer Saal mit langem Tisch, Gildenbannern ohne Schrift, Kronleuchter, Fenster mit Abendlicht; ruhig und leicht unscharf."),
    ("bg_splash", "Ladebildschirm/Key Art: Blick über das wieder aufgebaute Dawnhollow am Morgen, dahinter der Mossveil-Wald mit violettem Schimmer, ganz fern schwebende Shattered Realms am Himmel; heroisch und hoffnungsvoll. Oben ein ruhiger Himmelsbereich (dort kommt später das Logo hin), KEIN Text, kein Logo."),
]


# second wave, numbered after the backgrounds so earlier job folders keep their names
EXTRA_ICONS = [
    ("icons_ranks", [
        ("rankBronze", "Arena-Rang Bronze: Wappen aus Bronze mit gekreuzten Schwertern"),
        ("rankSilver", "Arena-Rang Silver: Wappen aus Silber, etwas prächtiger"),
        ("rankGold", "Arena-Rang Gold: goldenes Wappen mit Lorbeer"),
        ("rankPlatinum", "Arena-Rang Platinum: platin-türkises Wappen mit Flügeln"),
        ("rankMythic", "Arena-Rang Mythic: violett-goldenes Wappen mit Kristallen und Flügeln"),
        ("rankGuardian", "Arena-Rang Guardian: himmelblau-goldenes Wappen mit Guardian-Schild und großen Flügeln"),
        ("rankShatteredKing", "Arena-Rang Shattered King: zerbrochene, schwebende Krone über dunkel-goldenem Wappen mit violetten Rissen"),
        ("gradeCommon", "Seltenheits-Stein Common: kleiner grauer geschliffener Edelstein"),
        ("gradeRare", "Seltenheits-Stein Rare: blauer geschliffener Edelstein"),
        ("gradeEpic", "Seltenheits-Stein Epic: violetter geschliffener Edelstein"),
        ("gradeLegendary", "Seltenheits-Stein Legendary: goldener geschliffener Edelstein"),
        ("gradeTranscendent", "Seltenheits-Stein Transcendent: rot-goldener Edelstein mit Stern"),
    ], "Die sieben Ränge werden von Rang zu Rang sichtbar prächtiger; die Edelsteine alle gleich groß und gleich geschliffen."),
    ("icons_modes", [
        ("dungeonMaterial", "Material Dungeon: Kiste mit Erzen und Barren"),
        ("dungeonGear", "Gear Dungeon: Rüstung und Schwert"),
        ("dungeonFragment", "Fragment Dungeon: violetter Kristallhaufen"),
        ("dungeonRelic", "Relic Dungeon: antike Truhe mit Rune"),
        ("realmAsh", "Realm of Ash: brennende, zerbrochene Insel"),
        ("realmStillness", "Realm of Stillness: gefrorene Uhr mit erstarrten Tropfen"),
        ("realmReflection", "Realm of Reflection: zerbrochener Spiegel"),
        ("realmHunger", "Realm of Hunger: wuchernde dunkle Ranke mit Maul-Blüte"),
        ("questMain", "Hauptquest: goldenes Buch mit Lesezeichen"),
        ("questDaily", "Tagesquest: Sonne über einer Schriftrolle"),
        ("questWeekly", "Wochenquest: Kalenderblatt aus Pergament mit Siegel (ohne Zahlen)"),
        ("questVillage", "Dorfquest: Hammer vor einem kleinen Haus"),
    ]),
    ("icons_chests", [
        ("chestWood", "einfache Holztruhe (geschlossen)"),
        ("chestSilver", "Truhe mit Silberbeschlägen (geschlossen)"),
        ("chestGold", "prächtige Goldtruhe (geschlossen)"),
        ("chestEpic", "violette Kristalltruhe (geschlossen)"),
        ("chestLegendary", "legendäre Truhe mit Flügeln und Edelstein (geschlossen)"),
        ("chestOpened", "geöffnete leere Truhe (bereits abgeholt), matter"),
        ("slotWeapon", "leerer Ausrüstungsplatz Waffe: nur helle Schwert-Silhouette, grau-transparent wirkend"),
        ("slotArmor", "leerer Ausrüstungsplatz Rüstung: Brustpanzer-Silhouette, grau"),
        ("slotBoots", "leerer Ausrüstungsplatz Stiefel: Stiefel-Silhouette, grau"),
        ("slotAccessory", "leerer Ausrüstungsplatz Accessoire: Amulett-Silhouette, grau"),
        ("slotRelic", "leerer Relikt-Platz: Kelch-Silhouette, grau"),
        ("slotFragment", "leerer Fragment-Platz: Kristall-Silhouette, grau"),
    ], "Die fünf geschlossenen Truhen werden von Stufe zu Stufe wertvoller; die Platz-Silhouetten alle gleich schlicht (einfarbig hellgrau mit dunkler Outline)."),
]

EXTRA_CHROME = [
    ("chrome_items", (1536, 1024), 3, 2, [
        ("itemCommon", "Gegenstands-Rahmen Common: quadratischer grau-silberner Rahmen"),
        ("itemRare", "Gegenstands-Rahmen Rare: blauer Rahmen"),
        ("itemEpic", "Gegenstands-Rahmen Epic: violetter Rahmen mit Gold"),
        ("itemLegendary", "Gegenstands-Rahmen Legendary: goldener Rahmen mit Edelsteinen"),
        ("itemTranscendent", "Gegenstands-Rahmen Transcendent: rot-goldener Rahmen mit Sternen"),
        ("itemEmpty", "leerer Gegenstands-Platz: dunkler Steinrahmen"),
    ], "Alle als gleich große QUADRATISCHE Rahmen mit LEERER, rein weißer Innenfläche (die bleibt weiß); nur der Rahmen ist bemalt, schmal genug für kleine Kacheln."),
    ("chrome_banners", (1536, 1024), 2, 2, [
        ("bannerVictory", "Sieg-Banner: breites goldenes Band mit Flügeln links und rechts und Lorbeer, die Mitte frei für den Text"),
        ("bannerDefeat", "Niederlage-Banner: dasselbe Banner in dunklem Grau-Rot, Flügel zerzaust"),
        ("summonCard", "Summon-Banner-Rahmen: hohe Karte mit Goldrahmen und Sternen-Ornament oben, Innenfläche dunkelblau"),
        ("questCard", "Quest-Zeile: breite Pergament-Leiste mit Holzenden"),
    ], "Bänder sehr breit (ca. 4:1); die Summon-Karte Hochformat (2:3)."),
]


# third wave: battle HUD, actions, more screens, logo and app icon
WAVE3_ICONS = [
    ("icons_actions", [
        ("skillNormal", "Normaler Angriff: einfaches Schwert in rundem Steinrahmen"),
        ("skillSpecial", "Spezial-Fähigkeit: Schwert mit blauem Wirbel"),
        ("skillPassive", "Passive: leuchtendes Auge in einem Dreieck (ohne Schrift)"),
        ("skillUltimate", "Ultimate: goldener Stern-Ausbruch mit Flammen"),
        ("chat", "Chat: zwei Sprechblasen"),
        ("donate", "Spenden: Hand, die eine Münze in eine Gildentruhe legt"),
        ("sweep", "Sofort-Abschluss (Sweep): Schriftrolle mit Blitz"),
        ("firstClear", "Erst-Abschluss-Belohnung: goldene Fahne mit Stern"),
        ("salvage", "Zerlegen: Hammer, der ein Schwert zerbricht"),
        ("enhance", "Verstärken: funkelnder Amboss mit Edelstein"),
        ("craft", "Schmieden: Hammer und Zange gekreuzt"),
        ("equip", "Ausrüsten: Pfeil, der in einen Brustpanzer zeigt"),
    ]),
]

WAVE3_CHROME = [
    ("chrome_hud", (1536, 1024), 3, 2, [
        ("bossBar", "Boss-Lebensbalken-Rahmen: langer, prächtiger dunkler Balkenrahmen mit Hörner-Ornament an beiden Enden (sehr breit, ca. 8:1)"),
        ("ultFrame", "Rahmen für ein Helden-Porträt im Kampf: quadratisch mit abgerundeten Ecken, Goldrand, unten eine schmale Rinne für die Energie (Innenfläche rein weiß, bleibt frei)"),
        ("ultReady", "derselbe Porträt-Rahmen leuchtend: goldene Strahlen und Funken am Rand, Ultimate bereit (Innenfläche rein weiß)"),
        ("skillFrame", "quadratischer Fähigkeiten-Rahmen aus Stein mit Goldecken (Innenfläche rein weiß)"),
        ("toggle", "kleiner runder Schalter-Knopf aus dunklem Metall mit Goldrand (ohne Symbol)"),
        ("timerPlate", "kleine dunkle Plakette mit Goldrand für einen Timer (ca. 3:1)"),
    ], "Die Rahmen mit weißer Innenfläche lassen innen alles weiß."),
]

WAVE3_BACKGROUNDS = [
    ("bg_quests", "Quest-Zelt: Inneres eines Feldzelts mit großem Holzbrett voller angehefteter Zettel (ohne Schrift), Kerzen, Karten auf dem Tisch; ruhig und leicht unscharf."),
    ("bg_formation", "Kriegsrat-Zelt: Tisch mit ausgerollter Landkarte und Figuren, Waffenständer, Fackeln; ruhig und leicht unscharf."),
    ("bg_arena", "Arena-Vorraum: Steinhalle mit Bogen zur sonnigen Arena, Siegerpokale, Banner ohne Schrift; ruhig und leicht unscharf."),
    ("bg_dungeon", "Portal Chamber: runde Steinhalle mit mehreren Portalbögen in verschiedenen Farben, Kristalle, Ketten; ruhig und leicht unscharf."),
]

LOGO = """BILD: Spiel-Logo mit dem Schriftzug "MYTHWAKE" (genau diese Buchstaben, korrekt geschrieben, in Großbuchstaben).
- Format 1536x1024, freigestellt auf reinem weißem Hintergrund (#FFFFFF).
- Wie die Logos von AFK Arena / 7DS Idle Adventure: dicke, leicht gewölbte Fantasy-Buchstaben aus Gold mit dunkler Outline und Relief, dahinter ein leuchtender kantiger Kristallsplitter (der Mythwake Core) in Gold und Violett, kleine Flügel- oder Klingen-Ornamente links und rechts.
- Gut lesbar, kompakt, kein weiterer Text, kein Untertitel."""

APP_ICON = """BILD: App-Icon des Spiels, quadratisch 1024x1024, randlos bemalt (füllt das ganze Quadrat, keine runden Ecken, kein weißer Rand).
- Motiv: der Mythwake Core – ein schwebender, kantiger, golden-violett leuchtender Kristallsplitter vor dunkelblauem, magischem Hintergrund mit goldenen Ornamenten; kraftvoll und gut erkennbar auch winzig.
- Kein Text."""


# fourth wave, after studying AFK Arena / 7DS Idle screens (docs/UI_STYLE.md): light parchment popups, a glowing teal
# primary button, wooden dock, one-tone relief menu icons, full-screen result art and a hero stage
WAVE4_CHROME = [
    ("chrome_parchment", (1536, 1024), 2, 2, [
        ("popupParchment", "Popup-Fenster: helle Pergament-/Cremefläche (warm, sauber, ganz leichte Papierstruktur) mit prächtigem dunkelbraunem Holzrahmen und goldenen Eckbeschlägen"),
        ("sectionRibbon", "Abschnitts-Überschrift: schmales, langes dunkelbraunes Band mit dünner Goldkante und kleinen spitzen Enden (ca. 10:1), Mitte frei für Text"),
        ("rowParchment", "Listenzeile: breite helle Pergament-Leiste mit dünnem braunem Rand und leicht abgerundeten Ecken (ca. 5:1)"),
        ("slotDark", "dunkle, leicht vertiefte Mulde mit weichem Innenschatten und dünnem Goldrand, quadratisch (Hintergrund für Items)"),
    ], "Alle Flächen ruhig und gleichmäßig für 9-Slice."),
    ("chrome_buttons2", (1024, 1536), 2, 4, [
        ("btnTeal", "Haupt-Aktions-Button wie 'Begin'/'Level Up' bei AFK Arena: breite, leicht sechseckige Taste in leuchtendem Türkis/Cyan mit hellem Glanz, Goldrand und weichem Leuchten"),
        ("btnTealPressed", "derselbe türkise Button gedrückt (etwas dunkler, kein Glanz)"),
        ("btnWood", "Neben-Button: dieselbe Form aus dunklem poliertem Holz mit Bronze-Rand"),
        ("btnWoodPressed", "derselbe Holz-Button gedrückt"),
        ("btnTealDisabled", "derselbe sechseckige Button entsättigt grau-blau (deaktiviert)"),
        ("pillCost", "kleine dunkle Pille für Kosten/Anzahl mit dünnem Goldrand (ca. 3:1)"),
        ("navTile", "Kachel der unteren Navigationsleiste: hochkant, dunkles Holz mit Bronze-Kante, ruhig (inaktiv)"),
        ("navTileActive", "dieselbe Kachel aktiv: helleres warmes Holz, goldener Rand, sanftes Leuchten von unten"),
    ], "Die fünf Buttons gleiche Form und Größe (Seitenverhältnis ca. 3:1)."),
    ("chrome_hub", (1536, 1024), 3, 2, [
        ("sideButton", "Rahmen für einen Seitenleisten-Knopf auf dem Hauptbildschirm: kleine rautenförmige/achteckige Bronze-Plakette mit dunkler Mitte (Mitte rein weiß lassen, dort kommt ein Icon hin)"),
        ("avatarFrame", "runder Spieler-Avatar-Rahmen aus Gold mit kleinem Wappen-Schild unten für die Stufe (Innenfläche rein weiß)"),
        ("stageTrack", "Stage-Fortschrittsleiste: lange dünne goldene Schiene mit kleinen runden Knotenpunkten (ca. 10:1)"),
        ("chestPile", "AFK-Beute: Haufen Goldmünzen mit einer kleinen Schatztruhe und zwei Edelsteinen davor, breit und flach (für unter das Team)"),
        ("closeRound", "runder Schließen-Knopf: rote Kugel mit goldenem Rand und weißem X"),
        ("starBig", "großer goldener Bewertungsstern mit Glanz"),
    ], "Rahmen mit weißer Innenfläche lassen innen alles weiß."),
]

WAVE4_ICONS = [
    ("icons_relief", [
        ("reliefTown", "Dorf (Dawnhollow): kleines Haus mit Fahne"),
        ("reliefAdventure", "Abenteuer: Karte mit Kompass"),
        ("reliefCampaign", "Kampagne: zwei gekreuzte Schwerter vor Schild"),
        ("reliefHeroes", "Helden: Helm mit Federbusch"),
        ("reliefSummon", "Beschwörung: rundes Portal"),
        ("reliefQuests", "Quests: Schriftrolle mit Feder"),
        ("reliefMail", "Post: Briefumschlag"),
        ("reliefBag", "Inventar: Lederbeutel"),
        ("reliefEvents", "Events: Stern im Portal"),
        ("reliefShop", "Shop: Marktstand"),
        ("reliefGuild", "Gilde: Banner"),
        ("reliefMap", "Weltkarte: gefaltete Karte"),
    ], "ALLE im selben einheitlichen Stil wie die Menü-Icons von AFK Arena: einfarbiges, warmes Sepia/Bronze-Relief (helles Elfenbein-Beige mit brauner Schattierung und dunkelbrauner Outline), KEINE bunten Farben, wie aus Holz oder Messing geschnitzt."),
]

WAVE4_BACKGROUNDS = [
    ("bg_hero_stage", "Helden-Bühne (wie der Hintergrund der Heldenansicht in AFK Arena): eine runde, leicht erhöhte Steinbühne mit magischem goldenen Kreis-Siegel am Boden in der unteren Bildmitte, dahinter ein großer verzierter Torbogen mit weichem Licht und Vorhängen, symmetrisch, ruhig, mittig viel Platz für eine Figur."),
    ("bg_victory", "Sieg-Bildschirm-Kunst (wie bei AFK Arena): warme orange-goldene Gegenlicht-Szene, Silhouetten einer siegreichen Heldengruppe auf einem Hügel mit wehender Fahne vor einer strahlenden Sonne, dunkle Vignette; die untere Hälfte fast schwarz und ruhig (dort liegen Belohnungen)."),
    ("bg_defeat", "Niederlage-Bildschirm-Kunst: kalte blau-graue Szene, zerbrochene Fahne und Schwert im Boden, Regen, dunkle Vignette; die untere Hälfte fast schwarz und ruhig."),
]


def write(out, style, folder, body, cells):
    d = out / folder
    d.mkdir(exist_ok=True)
    shutil.copy(style, d / "style_reference.png")
    (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + body + "\n", encoding="utf-8")
    (d / "cells.json").write_text(json.dumps(cells, indent=1), encoding="utf-8")
    return folder


def icon_job(items, extra):
    cols, rows = (4, 3) if len(items) > 8 else ((3, 2) if len(items) <= 6 else (4, 2))
    listing = "\n".join(f"{i + 1}. {desc}" for i, (_, desc) in enumerate(items))
    body = (f"ICON-SHEET ({len(items)} Icons)\n{STYLE}\n{extra}\n"
            + GRID.format(w=1536, h=1024, cols=cols, rows=rows, items=listing)
            + "\nKein Text, keine Zahlen, keine Buchstaben im ganzen Bild.")
    return body, {"kind": "icons", "cols": cols, "rows": rows, "names": [name for name, _ in items]}


def chrome_job(w, h, cols, rows, items, extra):
    listing = "\n".join(f"{i + 1}. {desc}" for i, (_, desc) in enumerate(items))
    what = (f"Raster mit {cols} Spalten × {rows} Zeilen gleich großer Zellen (Zellen NICHT zeichnen), jedes Teil zentriert in "
            f"seiner Zelle mit weißem Abstand ringsum. {extra}\nReihenfolge zeilenweise:\n{listing}")
    body = f"UI-RAHMEN-SHEET\n{STYLE}\n" + FRAME.format(w=w, h=h, what=what) + "\nKein Text im ganzen Bild."
    return body, {"kind": "chrome", "cols": cols, "rows": rows, "names": [name for name, _ in items]}


def background_job(id, text):
    body = ("BILD: Hintergrund eines Menü-Bildschirms (Handy, Hochformat 1024x1536, randlos bemalt, keine Figuren, kein Text, keine UI).\n"
            "Stil wie die Menü-Hintergründe von AFK Arena / 7DS Idle Adventure: handgemalt, weiche Tiefe, nicht zu detailreich, "
            "damit Menüs darüber gut lesbar sind.\n" + text)
    return body, {"kind": "background", "names": [id]}


def main():
    out = Path(sys.argv[1])
    style = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    specs = [(s[0], *icon_job(s[1], s[2] if len(s) > 2 else "")) for s in ICON_SHEETS]
    specs += [(id, *chrome_job(w, h, cols, rows, items, extra)) for id, (w, h), cols, rows, items, extra in CHROME]
    specs += [(id, *background_job(id, text)) for id, text in BACKGROUNDS]
    specs += [(s[0], *icon_job(s[1], s[2] if len(s) > 2 else "")) for s in EXTRA_ICONS]
    specs += [(id, *chrome_job(w, h, cols, rows, items, extra)) for id, (w, h), cols, rows, items, extra in EXTRA_CHROME]
    specs += [(s[0], *icon_job(s[1], s[2] if len(s) > 2 else "")) for s in WAVE3_ICONS]
    specs += [(id, *chrome_job(w, h, cols, rows, items, extra)) for id, (w, h), cols, rows, items, extra in WAVE3_CHROME]
    specs += [(id, *background_job(id, text)) for id, text in WAVE3_BACKGROUNDS]
    specs += [("logo", f"{LOGO}\n{STYLE}", {"kind": "chrome", "cols": 1, "rows": 1, "names": ["logo"]}),
              ("app_icon", APP_ICON, {"kind": "appicon", "names": ["app_icon"]})]
    specs += [(id, *chrome_job(w, h, cols, rows, items, extra)) for id, (w, h), cols, rows, items, extra in WAVE4_CHROME]
    specs += [(s[0], *icon_job(s[1], s[2] if len(s) > 2 else "")) for s in WAVE4_ICONS]
    specs += [(id, *background_job(id, text)) for id, text in WAVE4_BACKGROUNDS]
    jobs =[write(out, style, f"{n:02d}_{id}", body, cells) for n, (id, body, cells) in enumerate(specs, start=1)]
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER.replace("xargs -P 5", "xargs -P 8"), encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()


