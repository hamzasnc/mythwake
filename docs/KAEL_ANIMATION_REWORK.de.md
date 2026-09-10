# Kael – Überarbeitung der Körperanimation

**Gestalterisch verworfener Stand.** Der Nutzer hat die hier beschriebene Überarbeitung anhand ihrer tatsächlichen Bewegung als weiterhin fehlerhaft zurückgewiesen. Die technischen Resultate unten sind keine Freigabe von Anatomie, Bewegung oder Wirkung. Die anschließende Arbeit beginnt deshalb mit einer separaten [Einzelhieb-Probe](KAEL_SINGLE_STRIKE_STUDY.de.md); weitere Clips werden erst nach deren Beurteilung aufgebaut.

Stand: 10. September 2026. Auftrag: Haltung, Ganzkörperbewegung und Übergänge im bestehenden Spiel überarbeiten, bei unverändertem Design. Die ursprünglichen Videos `Kael-Koerperkorrektur-Gameplay.mp4` und `Kael-Koerperkorrektur-ohne-Effekte.mp4` bleiben als Vergleich lokal unter `artifacts/kael`.

## Festgestellte Ursachen

- Die Bereitschaft stand nahe an gestreckten Knieketten. Viele Angriffe übernahmen fast dieselben Fußziele, während große Handbewegungen den Eindruck bestimmten.
- Alte Handziele waren in früheren Körperproportionen authoriert und konnten die reale Armlänge überschreiten. Die IK begrenzte unterschiedliche Ziele auf ähnlich gestreckte Armposen.
- Die perspektivabhängige Registrierung spiegelte zusätzlich zu den Zeichnungen die Fußziele. Ein als tragend gezeichneter Fuß sprang beim Wechsel zur Rückenansicht um 0,71 Welteinheiten, entsprechend etwa 30 Canvas-Pixeln bei 132 Pixeln Charakterhöhe.
- Der zweite Mixer verwarf bei einem unterbrochenen Übergang den tatsächlich sichtbaren Mischzustand. Ein Null-delta-Zustandswechsel konnte trotzdem sofort zur neuen Pose springen.
- Im echten Vorher-Kampf lag zwischen Angriff und Idle genau ein Laufbild (Video etwa 65,745–65,814 s). Der Bewegungsstatus wurde vor dem letzten Ankunftsschritt berechnet.
- Die Todesanimation verlangte fast ausschließlich eine 82°-Drehung der gesamten Figur. Ein vorhandener Validator schrieb genau diese Umsetzung fest, statt den körperlichen Zusammenbruch zu prüfen.

Der lokale Vorher-Bericht liegt unter `artifacts/kael/motion-rework/before-audit/BEWEGUNGSANALYSE.de.md`. Die Analyse verwendet konkrete Bildfolgen und deren Zeitstempel; sie behauptet keine kontinuierlich angesehene vollständige Echtzeitwiedergabe beider Videos.

## Umsetzung

`KaelAssetBuilder.cs` erzeugt die zehn aktiven beziehungsweise kompatiblen Clips neu. Der Idle besitzt eine tiefere asymmetrische Bereitschaft, feste Fußanker, zurückhaltende Atmung und gegeneinander verzögerte Kopf-/Handbewegung. Die drei Basics und Sichelsturm führen Vorbereitung, Kniekompression, Beckenverlagerung, Rumpfbewegung, Schnitt und Abfangen zusammen. Die Schwertziele beziehen sich auf die tatsächliche Schulter. Ein überflüssiger vollständiger Ansichtswechsel im Kreuzhieb wurde entfernt.

Der Drehschlag besitzt eine angehobene Schrittphase für das entlastete Bein und einen gehaltenen Stützfuß. Dessen horizontales Ziel wird beim Wechsel der Zeichnung nicht mehr gespiegelt. Die Hüfte bleibt bis zum tatsächlichen Ansichtswechsel niedrig genug, damit das rückwärts gezeichnete Bein sein Ziel innerhalb seiner gemessenen Länge erreicht. Sprung und Ulti enthalten kompaktere Flugposen und ein deutliches Abfedern beim Landen.

Haar- und Mantelnachbewegung wird aus den authorierten Bewegungsimpulsen mit unterschiedlichen gedämpften Antworten in die Clips gebacken. Im Kampf entsteht dafür kein zusätzlicher Physiksolver. Dekorative zeitgesteuerte Sinusbewegungen wurden aus den Aktionsposen entfernt. Beim Tod legen die beiden Mantelenden gezielt am Boden ab.

Der Tod verliert zuerst den Stand, knickt in Beinen und Becken ein, versucht sich abzustützen und fällt mit getrennten Gliederbewegungen in die gehaltene Endlage. Die Root-Drehung kippt nicht mehr die gesamte Standfigur. Die Waffe sinkt bereits zu Beginn ab, statt zunächst erneut die Bereitschaft einzunehmen.

`KaelRig.cs` behält bei unterbrochenen Übergängen den sichtbaren Mischzustand. Ausgehende Bewegungen laufen bei normalen Wechseln während der kurzen Übergabe weiter; abgeschlossene Teilgraphen werden entfernt. Der Tod übernimmt dagegen die eingefrorene tatsächliche Ausgangspose und geht über 200 ms in den bereits laufenden Kollaps. Nur der aktuelle Aktionsclip besitzt Markerautorität.

Die Waffenbahn beim Todesübergang verwendet die reale Ausgangsrichtung und die aus dem exportierten Death-Clip übernommenen Handkurven. Eine feste Vorschau wählt einen bodenfreien Bogen; dadurch hängt die Wahl nicht vom ersten Render-Zeitschritt ab. Nur das Handgelenk wird entlang dieser Bahn geführt und bei verbleibendem Kontakt minimal bis zur Bodentangente gedreht. Griff und Waffenanker bleiben verbunden; der Körper wird dafür nicht angehoben oder skaliert.

`KaelAnimationView.cs` beginnt Idle-/Laufphasen beim tatsächlichen Eintritt und koppelt Laufzeit an die gemessene Fortbewegung. `IdlePrototypeController.KaelCombat.cs` bestimmt den Laufstatus nach dem Ankunftsschritt. Zulässige Trefferreaktionen starten bei Alter null und dauern 200 ms; neue autoritative Aktionen übernehmen weiterhin sofort.

## Erhaltene Verträge

| Clip | Dauer | Kontakte |
|---|---:|---|
| idle | 2,40 s | keine |
| run | 0,64 s | keine |
| attack_cross | 0,84 s | 0,24 / 0,52 s |
| attack_spin | 0,84 s | 0,42 s |
| attack_jump | 0,84 s | 0,46 s |
| skill | 1,32 s | 0,40 / 0,92 s |
| hit | 0,20 s | keine zusätzliche Aktion |
| death | 0,90 s | Endlage halten |
| attack / skill_legacy | 0,60 / 1,00 s | 0,20 / 0,40 s |

Schadensbudgets, Angriffstakt, Mana, Cooldown, Ergebnisautorität und Kontakt-IDs bleiben erhalten. Es wurde keine Backend-, Balance- oder Speichermigration für diese Animationsüberarbeitung hinzugefügt. Der Körperatlas ist bytegleich zum vorherigen Stand: SHA256 `aa861577e90838e23b2a8092860a9eb2712d9efa48f966b7c2fadd3a7e9e7594`. Nur das abgeleitete Runtime-Icon wird aus der neuen tatsächlichen Idle-Pose neu gerendert.

Handbearbeitete Overrides unter `Animations/Authored` bleiben geschützt. Die generierten Clips und ihr SHA256-Manifest werden zusammen neu exportiert. Ein Bild-Crossfade, neue Effekte oder globale Skalierung ersetzen keine Körperbewegung.

## Geänderte Dateien und Assets

| Pfad | Zweck |
|---|---|
| `Assets/_Mythwake/Editor/KaelAssetBuilder.cs` | Posen, Handziele, Fußanker, Bewegungskurven und gebackene Nachbewegung |
| `Assets/_Mythwake/Scripts/KaelRig.cs` | Unterbrechbare Übergänge, pausierte Wechsel und Waffenführung beim Tod |
| `Assets/_Mythwake/Scripts/KaelAnimationView.cs` | Tatsächliche Idle-/Laufphasen und geschwindigkeitsabhängiger Lauf |
| `Assets/_Mythwake/Scripts/IdlePrototypeController.KaelCombat.cs` | Ankunftsstatus und Beginn erlaubter Trefferreaktionen im echten Kampf |
| `Assets/_Mythwake/Resources/Characters/Kael/Animations/GeneratedV2/*` | Zehn exportierte Clips samt Authoring-Fingerprints |
| `Assets/_Mythwake/Resources/Characters/Kael/Kael.controller` und `Kael.prefab` | Editierbare Vorschau und generierte Laufzeitdaten |
| `Assets/_Mythwake/Resources/Mythwake/Art/Runtime/hero_kael.png` | Abgeleitetes Icon aus der neuen Idle-Pose |
| `Assets/_Mythwake/Editor/KaelMotionContinuityReview.cs` und `.meta` | Fortlaufende Runtime- und Übergangsprüfung mit Messdaten und echten Renderbildern |
| `Assets/_Mythwake/Editor/KaelAnimationValidation.cs`, `KaelGameplayAcceptance.cs`, `KaelRenderedPoseProbe.cs` | Zusammenbruch anhand Körperlage prüfen; alte starre 82°-Vorgabe entfernen |
| `Assets/_Mythwake/Editor/KaelAssetReview.cs`, `scripts/run-kael-background.ps1` | Reproduzierbare, getrennte Hintergrundaufnahmen |
| `scripts/compare-kael-motion.py` | Zeitgleicher Vorher-/Nachher-Vergleich mit dokumentierter Zeitlupe |
| `ArtSource/Kael/V2/README.de.md` und dieses Dokument | Quellenverweis, Befunde, Reproduktion und Prüfergebnisse |

## Prüfstand

Die folgenden Prüfungen wurden mit Unity **6000.4.5f1** verborgen im Hintergrund ausgeführt. Zwischenstände mit gefundenen Fehlern bleiben lokal nachvollziehbar; die finalen neutralen Clips sind **Iteration9**, die finale Übergangsmatrix ist **final9**.

| Prüfung | Tatsächliches Ergebnis |
|---|---|
| Rebuild und neutraler Export | Exit 0; 551 echte Renderbilder bei 1024² und 60 Hz; Clip-/Roster-/Animationsprüfung bestanden. Log `artifacts/kael/motion-rework-iteration9.log`. |
| Fortlaufende Übergänge | 22 Szenarien × 1×/2× × beide Blickrichtungen = **88 Abläufe**, **8.254 echte PNG-/CSV-Samples**, **104/104 Kontakte**, keine gemeldeten technischen Fehler. Daten: `artifacts/kael/motion-continuity/final9/manifest.json`. |
| Zeitschrittunabhängigkeit beim Tod | Acht Paarprüfungen aus identischen Skill-/Spin-Ausgangsposen mit 1/60- gegen 1/30-s-Schritten; je 28 gemeinsame Alterswerte. Größte Spitzenabweichung 0,00000228 WU, Richtungsabweichung 0,000089°. |
| Anschlüsse und Boden | In der Übergangsmatrix keine gemessene Gelenk-/Grifföffnung, keine Death-Spitze unter Y=0. Größte horizontale Abweichung des NearFoot während der geprüften Stützphasen 0,000303 WU, etwa 0,013 Canvas-Pixel bei 132 Pixeln Höhe. Das ist eine Ankerprüfung; sichtbare Silhouetten wurden zusätzlich geprüft. |
| Pause, Spiegelung und gehaltene Endlage | Keine gemessene Abweichung in den vorgesehenen Paarprüfungen. 100 pausierte Zustandswechsel verändern die sichtbare Pose nicht. |
| Graph-Lebenszyklus | 200 schnelle Wechsel: höchstens elf Playables, nach abgeschlossenen Übergängen einer. Während 100 pausierter Wechsel höchstens drei. |
| Bestehende Unity-Regression | `KaelValidationSuite.RunAll`: Exit 0. Isolation, Sammlung/Save-Migration, Animation, zehn Kampfautoritätsgruppen, sechs Ulti-Fokusgruppen, Retry-Grenzen und die 13 bestehenden UI-/Progressionsprüfungen bestanden. Log `artifacts/kael/motion-rework-final9-regression.log`. |

Der abschließende echte Spielpfad ist ebenfalls **bestanden**: `artifacts/kael/gameplay-motion-rework-final/acceptance.json`, Log `artifacts/kael/motion-rework-gameplay-final.log`, **4.108 unveränderte Game-View-Bilder bei 540 × 960**. Accountstart → Home → Sammlung/Details → Formation → Kampf → Ergebnis → erneuter Kampf wurde im normalen `SampleScene`-Controller mit isoliertem Profil ausgeführt. Eine zweite Kael-Instanz und der Canvas-Lebenszyklus wurden vor der Aufnahme geprüft.

| Echter Kampf | Tempo | Kaels Basic-Aktionen | Erfolgreiche Basic-Kontakte | Ultis / Ulti-Kontakte |
|---|---:|---:|---:|---:|
| Kampagne, manuell | 1× | 23 | 31 | 1 / 2 |
| Kampagne, Wiederholung automatisch | 2× | 22 | 29 | 1 / 2 |
| Kampagne, Körperprüfung ohne VFX | 1× | 23 | 31 | 1 / 2 |
| Dungeon, Kael in Aufstellungsplatz 7 | 1× | 22 | 30 | 1 / 2 |
| Tatsächliche Niederlage | 1× | 0 | 0 | 0 / 0 |

Die letzte Niederlage entsteht aus gewöhnlichen Gegnerangriffen; Kael stirbt bei 900 ms Kampfzeit und erreicht anschließend seine gehaltene Todespose. Die vier Ultis sind auf vier wiederholte Kämpfe verteilt, nicht vier Ultis in einer Runde. Pausieren während des echten Ulti-Fokus wurde im manuellen Kampf geprüft. Die Aufnahme verändert ausschließlich legale Fortschrittsfelder im isolierten Testprofil; Mana, Treffer, HP und Animationszustände werden im aufgezeichneten Kampf nicht injiziert.

Elf abschließende Gameplay-Bildtafeln prüfen die aktuellen Basics, die Ulti mit und ohne Effekte, Übergänge bei 1×/2× und den echten Tod. Der alte einzelne Run-Frame zwischen Cross und Idle kommt im gesamten finalen CSV-Verlauf nicht mehr vor; die entsprechenden Anschlüsse wurden zusätzlich visuell kontrolliert. Kein neuer blockierender Anschluss- oder Rücksetzfehler wurde in diesen Folgen gefunden. Der echte Tod wurde bei 1× aufgezeichnet; 2×-Tod ist durch die separate gerenderte Übergangsprüfung belegt. Bericht mit konkreten Frame-Nummern: `artifacts/kael/motion-rework/gameplay-final-audit/REVIEW.de.md`.

Die PNGs der Übergangsmatrix stammen aus fortlaufenden tatsächlichen Runtime-Samples, ausdrücklich getrennt vom echten Canvas-Kampf. Zusätzlich zu den ursprünglichen Fällen wurden Death-Einstiege während der Drehung und in der Luft geprüft. Das nachträglich gefundene Problem einer anderen Waffenbahn bei 2× ist durch die Paarprüfung abgesichert; die finalen 1×-/2×- und gespiegelten Death-Bilder wurden danach erneut geprüft.

Visuelle Nachprüfung: Alle **490 Bilder der acht aktiven neutralen Clips** wurden auf sichtbare Pixel unter der Bodenlinie ausgewertet. Spin, Sprung, Ulti und Tod zeigen dort nach der Korrektur keinen Durchtritt mehr. Alle **55 Death-Bilder** und die geänderten Fuß-/Waffen-/Stofffolgen wurden außerdem chronologisch vergrößert auf Anschlüsse angesehen. Beim Lauf verbleibt im 1024²-Bild 23 eine einzelne Pixelzeile am Fußrand, entsprechend etwa 0,13 Canvas-Pixeln bei 132 Pixeln Charakterhöhe; diese Restabweichung wird nicht als vollständig beseitigt ausgegeben. Details: `artifacts/kael/motion-rework/iteration9-audit/REVIEW.de.md` und `floor-pixels.json`.

## Reproduktion und Ausgaben

Bei geschlossenem Unity-Editor vom Projektstamm aus:

```powershell
scripts/run-kael-background.ps1 -Method KaelAssetReview.RebuildMotionReview -LogName motion-export.log -MotionReviewFolder review
scripts/run-kael-background.ps1 -Method KaelMotionContinuityReview.RunAndValidate -LogName motion-continuity.log -MotionReviewFolder review -MotionReviewCapture all
scripts/run-kael-background.ps1 -Method KaelValidationSuite.RunAll -LogName motion-regression.log
scripts/run-kael-background.ps1 -Method KaelGameplayAcceptance.Run -LogName motion-gameplay.log -CaptureDirectory artifacts/kael/gameplay-motion-review
```

Die Gameplay-Ausgabe benötigt ein neues leeres Verzeichnis. Jeder Lauf verwendet ein eigenes isoliertes Testprofil und startet Unity verborgen mit reduzierter Prozesspriorität. Originalspielstände werden nicht für die Prüfungen verwendet.

`scripts/compare-kael-motion.py` stellt unveränderte Vorher-/Nachher-Renderbilder bei identischen Clipaltern nebeneinander. Die Zeitlupe wiederholt vorhandene Samples, sie erfindet keine Zwischenbilder. `scripts/encode-kael-gameplay.py` verwendet die tatsächlich gemessenen Zeitabstände der Game-View-Aufnahme.

Lokale Lieferdateien:

- `artifacts/kael/motion-rework/Kael-Bewegungsvergleich.mp4`: neutraler Vorher-/Nachher-Vergleich, 1024 × 560, 60 fps, 20,133 s; alle acht aktiven Clips bei 1× sowie Drehschlag/Tod zusätzlich bei 0,25×. Die JSON-Nebendatei dokumentiert Quelle und Zeitplan.
- `artifacts/kael/motion-rework/Kael-Neue-Bewegung-Gameplay.mp4`: vollständige finale Game-View-Aufnahme, 540 × 960, 30 fps, rund 142,8 s, mit fünf Kämpfen, darunter 2× und ein echter Kampf ohne VFX.
- `artifacts/kael/Kael-Koerperkorrektur-Gameplay.mp4`: unveränderte frühere Game-View-Aufnahme für den direkten Gameplay-Vergleich.
- `artifacts/kael/motion-rework/continuity-audit/final9-review/FINAL_DEATH_CONTINUITY_REVIEW.de.md`: abschließende gezielte Bildprüfung der Übergänge, inklusive der späten Skill-Einstiege.

Beide gelieferten MP4-Dateien wurden abschließend vollständig mit FFmpeg decodiert, ohne gemeldeten Decodierfehler. Alle zehn exportierten Clips stimmen mit ihrem SHA256-Manifest überein. Die Hintergrundtests sind beendet; die von Unity nebenbei erneut serialisierte TMP-Fallback-Schrift wurde auf den ursprünglichen Stand zurückgesetzt.

## Grenzen der gestalterischen Bewertung

Die vorhandenen drei gezeichneten Ansichten werden weiter diskret gewechselt. Die Fußübersetzung wurde korrigiert; eine vollständig stufenlose räumliche Drehung entsteht daraus nicht. Für weichere langsame Perspektivwechsel wären zusätzliche passende Zwischenansichten von Kopf, Rumpf, Hüfte, Schulterpanzer und Beinen samt ihren anatomischen Anschlüssen erforderlich. Der bestehende Artstyle wurde dafür nicht durch gestreckte oder überdrehte Ersatzzeichnungen verändert.

In der kleinen nativen Kampfdarstellung bleiben feine Hand-/Kniekonturen begrenzt auflösbar. Die echte Todesendlage wird teilweise durch einen anderen Teilnehmer verdeckt; die separate neutrale Prüfung zeigt sie ohne Verdeckung. Die Sichtprüfung verwendet dokumentierte zeitliche Bildfolgen und Vergrößerungen. Eine durchgehend angesehene Echtzeitwiedergabe aller Videos wird nicht behauptet.

Technische Tests, neutrale Bildfolgen und ausgewählte echte Gameplay-Frames ergeben keine automatische Freigabe auf dem Qualitätsniveau eines veröffentlichten Netmarble-Spiels. Ein Geräte-Performanceprofil und eine künstlerische Echtzeitabnahme sind gesondert zu beurteilen. In diesem Auftrag werden keine neuen Emulator-/Geräte- oder Android-Build-Ergebnisse behauptet.
