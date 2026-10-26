"""Writes the Codex jobs for the battle backgrounds (one per campaign chapter and one per game mode) or, with "story",
for the five intro pictures (Content/Story/story_<n>.png).

Every job gets its own folder <NN>_<id>/ with run_prompt.txt and style_reference.png; run_jobs.sh generates them
with Codex, 5 in parallel. Then import_backgrounds.py copies the masters to Content/Backgrounds/<id>.png.
Only names from docs/GAME_DESIGN.md are used; chapters without a doc name are described by their boss's lair.

usage: py -3 make_background_jobs.py <output dir> <style reference png> [story]
"""
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "town"))
from make_town_jobs import HEADER, RUNNER  # noqa: E402

RULES = """BILD: Kampf-Hintergrund (wie die Kampf-Hintergründe in AFK Arena / 7DS Idle Adventure).
- Hochformat 1024x1536, randlos bemalt (kein weißer Hintergrund), KEINE Figuren, keine Tiere, keine Monster, kein Text.
- Kamera: leicht erhöhte Seitenansicht (ca. 20° von oben), wie eine Bühne. Der Horizont / die hintere Kante des Kampfplatzes liegt bei ca. 42 % der Bildhöhe von oben.
- Unten (die unteren 58 %): ein großer, ebener, offener Kampfplatz, auf dem links das Team des Spielers und rechts die Gegner stehen. In der Mitte ruhig und gut lesbar (mittlere Helligkeit, wenig kleinteilige Details, keine hohen Objekte in der Mitte), damit die Figuren davor klar zu sehen sind. Details nur an den Rändern (links/rechts/ganz unten): Steine, Gras, Trümmer, passende Requisiten.
- Oben (die oberen 42 %): die Kulisse – Himmel, ferne Landschaft oder Architektur, die den Ort erkennbar macht. Die obersten 8 % ruhig (dort liegt UI).
- Beide Bildhälften ähnlich gewichtet, keine harte Symmetrie. Weiche atmosphärische Tiefe zum Horizont.
"""

# (id, text) – chapters follow campaign.json (boss of each chapter in brackets), then the modes.
JOBS = [
    ("dawnhollow",
     "ORT: Rand des zerstörten Grenzdorfs Dawnhollow am frühen Morgen. Kampfplatz: zertrampelte Dorfwiese mit Erdweg. Hinten: verkohlte Fachwerkhäuser, umgestürzte Zäune, Rauchfahnen, dahinter der grüne Wald; ganz fern schwach violettes Licht über den Bäumen. Stimmung: warmes Morgenlicht, Hoffnung nach der Katastrophe."),
    ("mossveil_woods",
     "ORT: Der Mossveil-Wald nach dem Aufbrechen des Rifts. Kampfplatz: moosige Waldlichtung mit Wurzeln an den Rändern. Hinten: hohe alte Bäume, teils verdreht, violette Risse im Boden und violettes Licht zwischen den Stämmen; ein alter, halb zerfallener Steinschrein (Mossveil Shrine) mit einem leuchtenden Erdriss klein im Hintergrund. Stimmung: grün-violett, geheimnisvoll."),
    ("chapter_3",
     "ORT: Ein verwüstetes Schlachtfeld auf einer alten Heerstraße (Gebiet der Crimson Wraiths, Boss: Crimson Wraith Adept). Kampfplatz: gepflasterte, rissige Straße mit Gras. Hinten: Ruinen eines Wachpostens, zerrissene rote Banner ohne Symbol, glimmende rote Glut und dunkelroter Dunst am Abendhimmel."),
    ("chapter_4",
     "ORT: Vor dem Hollow Spire (Boss: Hollow Spire Warden). Kampfplatz: dunkler Steinhof mit Platten, Risse mit blassblauem Licht. Hinten: hohe, schwarze, gotische Turmspitzen einer verlassenen Festung, blassblaue Fenster, kalter Nebel, Dämmerung."),
    ("chapter_5",
     "ORT: Im Obsidian Vault (Boss: Obsidian Vault Sentinel). Kampfplatz: polierter schwarzer Obsidianboden mit goldenen Einlegearbeiten. Hinten: riesige Tresortüren und Säulen aus Obsidian und Gold, smaragdgrün leuchtende Schlösser und Kristalle, warmes Fackellicht in einer großen unterirdischen Halle."),
    ("chapter_6",
     "ORT: Ein Feld zerbrochener Fragmente (Boss: Fragment Devourer). Kampfplatz: felsige Ebene mit verstreuten Kristallsplittern an den Rändern. Hinten: riesige, schräg aus dem Boden ragende, türkis und violett leuchtende Fragment-Kristalle, schwebende Splitter, ein angefressener Krater, Dämmerhimmel mit Rissen."),
    ("chapter_7",
     "ORT: Eine überflutete Küstenruine (Boss: Corrupted Guardian of the Deep). Kampfplatz: nasser Sandstrand mit flachen Pfützen, Muscheln und Korallen an den Rändern. Hinten: versunkene Tempelruinen halb im türkisen Meer, Wellen, Korallen auf Säulen, bewölkter Himmel mit Lichtstrahlen."),
    ("chapter_8",
     "ORT: Ein Bergplateau über den Wolken (Boss: Dragon Echo of the Sky). Kampfplatz: windgepeitschtes Felsplateau mit Gras. Hinten: Wolkenmeer, ferne Berggipfel, Sturmwolken mit Blitzen, ein alter zerbrochener Steinbogen am Rand, blau-goldenes Licht."),
    ("chapter_9",
     "ORT: Vorhof eines falschen Wächter-Tempels (Boss: False Guardian Construct). Kampfplatz: weißer Marmorhof mit goldenen Linien, Risse mit rosa-violettem Licht. Hinten: prachtvoller Marmortempel mit goldenen Ringen und Statuen ohne Gesicht, Heiligenschein-artige Lichtringe am Himmel, kühl und unheimlich schön."),
    ("chapter_10",
     "ORT: Das innere Heiligtum des falschen Wächter-Tempels (Boss: False Guardian Construct). Kampfplatz: zerbrochener Marmorboden, schwebende Bodenplatten an den Rändern. Hinten: gewaltige Halle mit zerbrochener Kuppel, durch die ein violetter Riss im Himmel sichtbar ist, schwebende Trümmer, goldene Konstrukt-Teile."),
    ("arena",
     "ORT: Die Arena (PvP). Kampfplatz: Sandboden einer runden steinernen Arena. Hinten: Ränge eines alten Kolosseums (leer, keine Zuschauer), Fahnen ohne Symbol, Fackeln, Sonnenuntergang, festlich und kompetitiv."),
    ("dungeon",
     "ORT: Ein Dungeon hinter der Portal Chamber. Kampfplatz: alter Steinboden einer unterirdischen Halle. Hinten: Säulen, Kristalle, Ketten, ein Portalbogen mit blau-violettem Licht, Fackeln, kühle Höhlenatmosphäre."),
    ("realm_of_ash",
     "ORT: Shattered Realm – Realm of Ash (Feuerschaden stärker, Heilung schwächer). Kampfplatz: aschgraue, rissige Ebene mit glühender Lava in den Rissen an den Rändern. Hinten: brennende Ruinen, Aschefall, schwebende zerbrochene Landmassen, rot-oranger Himmel mit violetten Rissen."),
    ("realm_of_stillness",
     "ORT: Shattered Realm – Realm of Stillness (alles verlangsamt). Kampfplatz: gefrorene, spiegelglatte blasse Ebene. Hinten: in der Luft erstarrte Wassertropfen und Felsbrocken, gefrorene Zeit, stille blasse blau-violette Farben, schwebende zerbrochene Landmassen, ein stehengebliebener Sternenhimmel."),
    ("realm_of_reflection",
     "ORT: Shattered Realm – Realm of Reflection (Gegner kopieren und reflektieren). Kampfplatz: spiegelnder Kristallboden. Hinten: riesige zerbrochene Spiegelscherben und Kristallwände, gespiegelte schwebende Inseln, silbern-violettes Licht, Prismenfarben."),
    ("realm_of_hunger",
     "ORT: Shattered Realm – Realm of Hunger (Gegner regenerieren und wachsen). Kampfplatz: dunkler, organisch wirkender Boden mit Ranken an den Rändern. Hinten: wuchernde dunkelgrüne und violette Ranken, pulsierende Kokons, schwebende zerbrochene Landmassen, düster-grüner Himmel mit violetten Rissen."),
    ("trial",
     "ORT: Trial of Fragments – ein Turm. Kampfplatz: runde Steinplattform hoch oben im Turm. Hinten: Turmwand mit fünf großen leuchtenden Kristallen in fünf Farben (orange-rot, blau, türkis, grün, rosa) für die fünf Fragment Aspects, Sternenhimmel durch hohe Bögen."),
    ("raid",
     "ORT: Schlachtfeld einer Guardian Raid. Kampfplatz: weite, aufgerissene Ebene mit Kratern. Hinten: gewaltige Silhouette eines riesigen Gebirges unter einem dramatischen Himmel, Lichtsäulen, Banner von Gilden ohne Symbol am Rand, episch und groß."),
    ("watchtower",
     "ORT: Verteidigung von Dawnhollow am Watchtower bei Nacht. Kampfplatz: Erdweg vor der Palisade des Dorfes. Hinten: Holzpalisade, ein hoher hölzerner Wachturm mit Glocke und Fackeln, die Dorfdächer dahinter, dunkler Wald, Mondlicht und violetter Schimmer über den Bäumen."),
]

STORY_RULES = """BILD: Illustration für die Intro-Geschichte (Vollbild auf dem Handy, wie die Story-Bilder in AFK Arena).
- Hochformat 1024x1536, randlos bemalt, filmische Komposition mit klarem Fokus, dramatisches Licht.
- Das untere Drittel ruhig und eher dunkel (dort liegt der Text der Geschichte).
- Die Spielfigur ("du") wird nie von vorne gezeigt: nur von hinten, als Silhouette oder als Hand – kein Gesicht, kein festgelegtes Aussehen.
- Kein Text, keine Schrift, keine UI.
"""

# the five pages of the intro, after the lore sneak-peek of docs/GAME_DESIGN.md (strings story.1–5)
STORY = [
    ("story_1",
     "SZENE: Atheris ist zerbrochen. Weite Fantasy-Landschaft aus großer Höhe: gefallene Burgen und Königreiche in Ruinen, riesige leuchtende Risse in der Erde, aus denen violettes Licht und schwebende zerbrochene Landmassen (die Shattered Realms) aufsteigen; am Himmel kreisen kantige Kristallsplitter (Fragmente). Episch, melancholisch, Dämmerung."),
    ("story_2",
     "SZENE: Das friedliche Grenzdorf Dawnhollow vor der Katastrophe: ein kleines, gemütliches Dorf aus Fachwerkhäusern mit Strohdächern auf einer Waldlichtung, Rauch aus Schornsteinen, Felder, ein Bach mit Holzbrücke, warmes Abendlicht; am Horizont dunkler Wald (Mossveil). Idyllisch, mit einem kaum merklichen violetten Schimmer tief im Wald."),
    ("story_3",
     "SZENE: Unter einem alten moosbewachsenen Steinschrein im Wald (Mossveil Shrine) bricht die Erde auf: ein riesiger leuchtender violetter Riss (Rift), violettes Licht flutet durch die Bäume, die Bäume verdrehen sich, Silhouetten verdrehter Wölfe und Wildschweine mit violetten Augen, Funken und Splitter in der Luft. Bedrohlich und chaotisch."),
    ("story_4",
     "SZENE: Nahaufnahme: eine ausgestreckte Hand (von hinten/seitlich, kein Gesicht) berührt einen schwebenden kantigen Kristallsplitter, der in goldenem und violettem Licht explodiert und sich in einen strahlenden, gezähmten goldenen Kern verwandelt (der Mythwake Core); um ihn herum geisterhafte, leuchtende Silhouetten von Kriegern, Königen und Magiern (Echoes). Magisch, ehrfürchtig."),
    ("story_5",
     "SZENE: Morgengrauen über dem zerstörten Dawnhollow: rauchende Ruinen, verkohlte Balken, eine kleine Gruppe Überlebender (in einfacher Kleidung, mit Werkzeugen und Bündeln) schaut zur Spielfigur, die von hinten im Vordergrund steht und ein schwaches goldenes Leuchten in der Hand hält; im Hintergrund pulsiert der violette Rift über dem Wald. Hoffnungsvoll trotz der Zerstörung, erstes warmes Sonnenlicht."),
]


def main():
    out = Path(sys.argv[1])
    style = Path(sys.argv[2])
    story = len(sys.argv) > 3 and sys.argv[3] == "story"
    out.mkdir(parents=True, exist_ok=True)
    folders = []
    for i, (id, text) in enumerate(STORY if story else JOBS, start=1):
        d = out / f"{i:02d}_{id}"
        d.mkdir(exist_ok=True)
        shutil.copy(style, d / "style_reference.png")
        rules = STORY_RULES if story else RULES
        (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + rules + "\n" + text + "\n",
                                          encoding="utf-8")
        folders.append(d.name)
    (out / "_jobs.txt").write_text("\n".join(folders) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(folders), "jobs in", out)


if __name__ == "__main__":
    main()
