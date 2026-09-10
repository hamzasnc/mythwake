# Kael: erster einzelner Schwerthieb

Stand: 10.09.2026, nachgebesserte Aufnahme `artifacts/kael/strike-study/take14/`.

Diese Arbeit liefert die vereinbarte erste Bewegungsprobe: ein durchgehender Hieb in einer Körperansicht mit Bereitschaft, Ausholen, Gewichtsverlagerung, Schnitt, Abfangen und Rückkehr. Sie ist separat von den bestehenden Spielanimationen. Ulti, weitere Basics, Lauf-/Todesanimationen und Kampfumbau gehören zur anschließenden Arbeit nach Beurteilung dieser Probe.

## Umsetzung

- Bearbeitbare Quelle: `ArtSource/Kael/MotionStudy/strike-study.json`. Die Körperkanäle besitzen einzeln gesetzte Schlüsselzeiten und Geschwindigkeiten. Ein vollständiger Umlauf der Hand um die Schulter mit entsprechender Handgelenkverdrehung wurde verworfen.
- Der fertige Clip liegt in `Assets/_Mythwake/ArtStudies/KaelStrike/strike_study_baked.anim`, das zugehörige Prefab daneben. Die gelösten Knochenbewegungen sind im Clip gespeichert. Der Studienplayer besitzt keine IK, Socketkorrektur oder Kampfauflösung.
- Die Bereitschaft ist tiefer. Waffenhand und Ellenbogen durchlaufen Hin- und Rückweg unterhalb des Kinns; die Hand wird nicht mehr hinter den Kopf geführt. Der Arm bleibt auf einer konstanten IK-Lösungsseite, ohne vollständigen Umlauf oder Wechsel während des Clips.
- Der Gegenarm hat eine eigene, niedrigere Ausgleichsbewegung. Seine Hand liegt weder auf dem Oberschenkel noch mit den Fingern unmittelbar unter der Schneide. Handrotation und Reichweite wurden über den gesamten Ablauf geprüft.
- Die Klinge wird für diese vorn geführte Aktion durchgehend vor dem Kopf gezeichnet; die greifende Hand liegt darüber. Dadurch verschwindet kein mittlerer Klingenabschnitt hinter dem Gesicht. Während des Vorbeischwingens verdeckt die Klinge kurz einen Teil des Gesichts. Gesicht und Hals behalten ihre bestehende geometrische Aufteilung und dieselben Atlaspixel.
- Füße bleiben auf ihren Bodenpunkten; Becken, Brust und Kopf folgen unterschiedlichen Zeitkurven. Haare und Kleidung besitzen versetzte Bewegungen. Einziger Kontaktmarker: `impact:0` bei 0,42 s, Cliplänge 0,84 s. Keine VFX in dieser Probe.
- Der Schnitt beginnt nach dem Ausholen bei etwa 0,18 s. Seine Zwischenbewegung ist über diesen Abschnitt verteilt; die Klinge durchläuft ihre Bahn nicht erst unmittelbar vor dem Kontakt in wenigen Bildern.

## Überprüfte Ergebnisse

| Prüfung | Ergebnis |
|---|---|
| Unity 6000.4.5f1, Kompilieren und Hintergrundexport | Exit 0; keine Import-/Renderfehler während des Studienlaufs |
| Erneut geladenes Studien-Prefab | 67 tatsächliche Bilder mit 60 Hz, einschließlich Endpose |
| Quelle gegen gespeicherten Clip, einschließlich Zwischenzeiten und Spiegelung | 666 Vergleiche; maximale Weltpositionsabweichung 0,000929 WU; keine Sprite-/Sortierungsabweichung; gemessene Grifflücke 0 |
| Geometrie von Gesicht/Hals | Je 20 Vertices und 30 Indices; Positionen, UVs und Indices vor und nach Speichern/Laden geprüft |
| Beginn gegen gehaltenes Ende | Gerenderte Bilder pixelidentisch |
| Füße in den gerenderten Frames | Maximale gemessene Koordinatenänderung rund 0,000001 WU |
| Schwertspitze in den gerenderten Frames | Niedrigster Punkt Y = 0,885178 WU |
| Beide Arme, separate Prüfung der Quellkurven bei 1 kHz | Kleinste Streckreserve: Waffenarm 0,0329 WU, Gegenarm 0,0175 WU; keine unerreichbaren Ziele oder vollständigen Handumläufe |
| Sichtbarer Pixelbeitrag der Waffenhand | In jedem Frame vorhanden; kleinster Verhältniswert 0,949952 gegenüber der isolierten Handfläche |
| Sichtbarer Pixelbeitrag der freien Hand | In jedem Frame vorhanden; kleinster Verhältniswert 0,957692 gegenüber der isolierten Handfläche |
| Ausschnitt | Alle 67 Bilder vollständig im Renderbereich; kein Bildrandkontakt |
| Körperatlas | SHA-256 unverändert: `aa861577e90838e23b2a8092860a9eb2712d9efa48f966b7c2fadd3a7e9e7594` |
| Videodatei | Fünf vollständige Wiederholungen; 395 Frames bei 60 fps; vollständig fehlerfrei dekodiert |

## Prüfung der tatsächlichen Pixel und Bewegung

Ausgangspunkt war Take09. Ein erneuter Export desselben Ablaufs mit eingeschalteter Pixelprüfung (Take10) ergab in allen 67 Bildern dieselben Pixel. Die neue Messung bestätigte dabei zwei vollständig verdeckte Waffenhand-Bilder, 41 und 42. Die tiefere Bahn beseitigt diesen konkreten Fehler.

Der optionale Renderer-Audit vergleicht für jede Hand jedes vollständige Bild mit demselben Bild ohne diese Hand. Zusätzlich wird die Hand allein gerendert. Gezählt werden RGB-Unterschiede über 3/255. Die Verhältniswerte sind Diagnosewerte: Ähnliche dunkle Farben unter der Hand können sie ebenfalls reduzieren; sie sind keine exakte Messung einer geometrischen Verdeckungsfläche. Die Originalbilder werden vor den Diagnosepasses gespeichert, und kein zusätzlicher Animationsschritt wird eingefügt.

Für alle 52 Bewegungsbilder einschließlich der ersten Endpose liegen unskalierte Ober-/Unterkörperseiten vor. Weitere 15 Bilder halten dieselbe Endpose. Hals, Schultern, Ellenbogen, Hände, Hüfte, Knie, Knöchel, Füße und Stoffkanten wurden anhand der Bildfolge geprüft; fragliche Hand-/Klingenbereiche zusätzlich mit ganzzahliger Pixelvergrößerung. Die finale untere Bildregion ab Y=795 ist in allen 67 Frames pixelidentisch mit der zuvor vollständig geprüften Unterkörperfolge. Es wurden keine offenen Gelenkanschlüsse oder sprunghaften Teilwechsel in den geprüften Folgen gefunden.

Der größte Weg der Schwertspitze zwischen zwei 60-Hz-Bildern beträgt über den gesamten Clip im 132er-Canvas-Maßstab 17,8 Pixel; beim Ausgangspunkt Take09 waren es 28,1 Pixel. Das belegt die gleichmäßigere zeitliche Verteilung, ist jedoch kein allgemeiner Grenzwert für gute Animation.

Die Videoansichten verwenden die Maßstäbe der Canvas-Höhenparameter 108, 118 und 132. Es handelt sich um eine Studienprojektion, nicht um eine Aufnahme aus einem echten Kampf. Die Sichtprüfung verwendet die tatsächlichen Frames und ihre Reihenfolge; sie wird nicht als durchgehende Echtzeit-Videobeobachtung ausgegeben. Die gestalterische Beurteilung durch den Nutzer bleibt eigenständig. Die Probe verwendet weiterhin eine Körperansicht und einen kompakten einzelnen Hieb.

## Dateien und Reproduktion

- `ArtSource/Kael/MotionStudy/README.de.md`: Quelle und Hintergrundaufruf.
- `Assets/_Mythwake/Editor/KaelStrikeStudyAuthoring.cs`: zeitliche Steuerkurven einlesen und Studienexport starten.
- `Assets/_Mythwake/Editor/KaelStudyHeadLayers.cs`: getrennte Kopfgeometrie erstellen und erneut laden.
- `Assets/_Mythwake/Editor/KaelBakedMotionStudy.cs`: finale Knochenkurven speichern und unabhängig rendern.
- `Assets/_Mythwake/Scripts/KaelBakedClipPlayer.cs`: reines manuelles Abspielen des fertigen Clips.
- `scripts/review-kael-strike-study.py`: Bildfolge unverändert in die Videovorschau übernehmen; benötigt Python/Pillow und `--ffmpeg`.
- `scripts/audit-kael-strike-frames.py`: alle Bildpixel auf Ausschnitt und Endpose vergleichen, native Detailseiten erstellen und Hand-Pixelmessungen zusammenfassen.
- `artifacts/kael/strike-study/take14/`: Video, Bildfolge, Bone-CSV, Quellenkopie, Vergleich, beide Hand-Pixelmessungen und Detailseiten. Große Aufnahmebilder bleiben außerhalb der Git-Quellen.

Für diese isolierte Probe wurden keine Go-, Gameplay-, Android-, Emulator- oder Gerätetests als neue Abnahme durchgeführt. Vorhandene Veränderungen aus der vorherigen Überarbeitung bleiben erhalten; diese Probe ersetzt oder veröffentlicht sie nicht.
