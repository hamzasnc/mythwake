"""Writes the Codex jobs for the AFK-style UI kit: every shared part (top bar, dock, buttons, popup, rows, slots, nav
icons) painted once, in exactly the style of the approved style frames (docs/UI_STYLE.md), so all screens match.
The sheets use the same file names as the old kit, so import_ui.py replaces them in place.

usage: py -3 make_afk_kit.py <output dir> <approved frames png>
"""
import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "town"))
from make_town_jobs import HEADER, RUNNER  # noqa: E402

STYLE = """STIL: GENAU der Stil der angehängten drei Spiel-Screens (style_reference.png: Kampagne, Heldenansicht, Quest-Popup meines Spiels). Gleiche Materialien (dunkles warmes Holz, helles Pergament, Türkis nur für Haupt-Buttons), gleiche Holzmaserung, gleiche dünnen Goldkanten, gleiche Linienstärke, gleiche Eckenrundung, gleiche Lichtrichtung (von oben), gleiche Farbtöne. Alle Teile dieses Bildes müssen zusammenpassen wie aus einem einzigen Spiel. Keine dicken Goldornamente, kein Plastik-Glanz."""

GRID = """FORMAT: Bild {w}x{h}, reiner weißer Hintergrund (#FFFFFF), Raster mit {cols} Spalten × {rows} Zeilen gleich großer Zellen (Zellen NICHT zeichnen). Jedes Teil zentriert in seiner Zelle mit weißem Abstand ringsum, berührt keine Zellengrenze, kein Schatten auf den Hintergrund. Reihenfolge zeilenweise von links oben:
{items}
{extra}
Kein Text, keine Buchstaben, keine Zahlen im ganzen Bild."""

NINE = "Teile, die im Spiel gestreckt werden (Leisten, Buttons, Panels, Reiter, Zeilen), haben verzierte Ecken nur an den Enden und auf ganzer Länge GLEICHFÖRMIGE gerade Kanten; ihre Innenfläche ist ruhig und gleichmäßig."

SHEETS = [
    ("afk_bars", (1536, 1024), 1, 3, [
        ("topBar", "die obere Leiste des Spiels: schmale, sehr breite dunkle Holzleiste mit dünner Goldkante unten (Seitenverhältnis ca. 12:1), leer"),
        ("dockBar", "die untere Navigationsleiste als Hintergrund: breite dunkle Holzleiste mit Goldkante oben (ca. 8:1), leer"),
        ("sectionRibbon", "Abschnitts-Überschrift: schmales dunkles Holzband mit kleinen Pfeilspitzen an beiden Enden (ca. 10:1), leer"),
    ], NINE),
    ("afk_buttons", (1024, 1536), 2, 4, [
        ("btnTeal", "Haupt-Button: länglich-sechseckig, Türkis-Cyan mit sanftem Leuchten und dünnem Goldrand, wie 'Kampf'/'Level Up' in den Screens, ohne Text und ohne Icon"),
        ("btnTealPressed", "derselbe türkise Button gedrückt (dunkler, kein Leuchten)"),
        ("btnWood", "Neben-Button: dieselbe Form aus dunklem Holz mit dünnem Goldrand, wie 'Los'/'Schließen' in den Screens, ohne Text"),
        ("btnWoodPressed", "derselbe Holz-Button gedrückt"),
        ("btnTealDisabled", "derselbe Haupt-Button entsättigt grau (deaktiviert)"),
        ("pillCost", "Währungs-Pille der oberen Leiste: dunkle, halbtransparent wirkende abgerundete Pille mit dünnem Goldrand (ca. 3:1), leer"),
        ("plusButton", "kleines quadratisches Holzfeld mit Goldrand und einem goldenen Plus darin (wie neben den Edelsteinen oben)"),
        ("backButton", "Zurück-Knopf: kleines dunkles Holzquadrat mit abgerundeten Ecken und einem hellen geschwungenen Pfeil nach links (wie unten links in den Screens)"),
    ], "Die fünf Buttons gleiche Form und Größe (Seitenverhältnis ca. 3,3:1)."),
    ("afk_panels", (1536, 1024), 3, 2, [
        ("popupParchment", "Popup-Fenster: helle Pergamentfläche mit geschnitztem dunklem Holzrahmen und kleinen Eckverzierungen (wie das Aufgaben-Popup), leer"),
        ("titlePlate", "Titelschild: dunkles Holzschild mit leicht eingerollten Enden, das oben aus dem Popup ragt (ca. 4:1), leer"),
        ("rowParchment", "Listenzeile: etwas hellere Pergament-Karte mit dünnem braunem Rand und leicht abgerundeten Ecken (ca. 5:1), leer"),
        ("tabActive", "aktiver Reiter: helles Pergament, oben abgerundet, dünner Holzrand (ca. 3:1), leer"),
        ("tabInactive", "inaktiver Reiter: dunkles Holz, oben abgerundet, dünner Goldrand (ca. 3:1), leer"),
        ("statBand", "dunkles, halbtransparent wirkendes Holzband für Werte (wie unter dem Helden in der Heldenansicht, ca. 6:1), leer"),
    ], NINE),
    ("afk_slots", (1536, 1024), 3, 2, [
        ("navTile", "Kachel der unteren Navigation, inaktiv: dunkles Holz, hochkant, leer (wie die Tabs unten in den Screens)"),
        ("navTileActive", "dieselbe Kachel aktiv: helleres warmes Holz mit sanftem warmem Glimmen von unten, leer"),
        ("sideButton", "runder Holzring für die Seitenknöpfe (Quests, Post …), Innenfläche rein weiß (dort kommt ein Icon hin)"),
        ("skillRing", "runder Ring aus Holz und dünnem Gold für Fähigkeiten in der Heldenansicht, mit kleinem runden Zahlenschild unten, Innenfläche rein weiß"),
        ("slotDark", "quadratischer Item-Rahmen: dunkles Holz mit dünnem Goldrand und leicht vertiefter Innenfläche (wie die Belohnungs-Icons in der Quest-Liste), Innenfläche dunkel"),
        ("avatarFrame", "runder Avatar-Ring aus Gold für die obere Leiste mit kleinem Holzschild unten rechts für die Stufe, Innenfläche rein weiß"),
    ], "Rahmen mit weißer Innenfläche lassen innen alles weiß."),
    ("afk_misc", (1536, 1024), 3, 2, [
        ("barFrame", "Fortschrittsbalken-Rinne: lange flache dunkle Rinne mit dünnem Holz-Gold-Rand (ca. 10:1), leer"),
        ("barFillTeal", "Füllung des Balkens: gleichmäßiger Türkis-Streifen mit leichtem Glanz (ca. 10:1)"),
        ("badge", "roter Benachrichtigungs-Punkt mit dünnem weißem Rand"),
        ("star", "goldener Stern (Bewertung)"),
        ("starEmpty", "derselbe Stern leer, dunkler Stein"),
        ("chestSmall", "kleine geschlossene Holztruhe mit Goldbeschlägen (wie auf dem Aktivitätsbalken im Quest-Popup)"),
    ], NINE),
    ("afk_nav_icons", (1536, 1024), 4, 3, [
        ("reliefTown", "Dorf: Häuschen"),
        ("reliefAdventure", "Abenteuer: Kompassrose"),
        ("reliefCampaign", "Kampagne: zwei gekreuzte Schwerter"),
        ("reliefHeroes", "Helden: Helm mit Federbusch"),
        ("reliefSummon", "Beschwörung: Portal-Spirale"),
        ("reliefQuests", "Quests: Schriftrolle"),
        ("reliefEvents", "Events: Stern"),
        ("reliefShop", "Shop: Beutel mit Münzen"),
        ("reliefMail", "Post: Briefumschlag"),
        ("reliefBag", "Inventar: Lederbeutel"),
        ("reliefGuild", "Gilde: Banner"),
        ("reliefMap", "Karte: gefaltete Karte"),
    ], "ALLE als einfarbige Sepia-Relief-Icons GENAU wie die Icons in der unteren Leiste und den Seitenknöpfen der Screens (beige/elfenbein mit brauner Schattierung, dunkelbraune Outline), gleiche Größe, frontal."),
]

BACKGROUNDS = [
    ("bg_hero_stage", "BILD: Hintergrund der Heldenansicht, Hochformat 1024x1536, randlos, GENAU wie die Halle in der angehängten Heldenansicht: Torbogen mit Licht, Banner, runde verzierte Bühne mit Kreis-Siegel unten mittig, OHNE Helden, ohne UI, ohne Text; ruhig, Mitte frei für eine Figur."),
]


def main():
    out = Path(sys.argv[1])
    frames = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    n = 0
    for id, (w, h), cols, rows, items, extra in SHEETS:
        n += 1
        d = out / f"{n:02d}_{id}"
        d.mkdir(exist_ok=True)
        shutil.copy(frames, d / "style_reference.png")
        listing = "\n".join(f"{i + 1}. {desc}" for i, (_, desc) in enumerate(items))
        body = f"UI-KIT-SHEET\n{STYLE}\n" + GRID.format(w=w, h=h, cols=cols, rows=rows, items=listing, extra=extra)
        (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + body + "\n", encoding="utf-8")
        kind = "icons" if id.endswith("icons") else "chrome"
        (d / "cells.json").write_text(json.dumps({"kind": kind, "cols": cols, "rows": rows, "names": [k for k, _ in items]}), encoding="utf-8")
        jobs.append(d.name)
    for id, text in BACKGROUNDS:
        n += 1
        d = out / f"{n:02d}_{id}"
        d.mkdir(exist_ok=True)
        shutil.copy(frames, d / "style_reference.png")
        (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + STYLE + "\n" + text + "\n", encoding="utf-8")
        (d / "cells.json").write_text(json.dumps({"kind": "background", "names": [id]}), encoding="utf-8")
        jobs.append(d.name)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER.replace("xargs -P 5", "xargs -P 7"), encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
