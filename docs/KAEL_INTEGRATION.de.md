# Kael – Integrations- und Prüfstand

Stand: 10. September 2026. Kael (`hero_kael`) verwendet Unity 6000.4.5f1 mit Unity 2D Animation 14.0.3, eigene Bones, SpriteSkins, IK, Austauschzeichnungen und manuell ausgewertete Animationen. Eine Spine-Lizenz ist für diesen Charakter nicht erforderlich. Der gestalterische Maßstab steht in [CHARACTER_QUALITY_STANDARD.de.md](CHARACTER_QUALITY_STANDARD.de.md).

## Spiel und Lieferung

Kael über **Helden → Kael → Formation** einem der sieben Kampfplätze zuweisen. Neue lokale Spielstände erhalten ihn im ersten Platz; bestehende Fortschritte und fünf Siebener-Presets bleiben erhalten. Acht Helden sind verfügbar, sieben nehmen am Kampf teil. Die ID und vorhandener Fortschritt bleiben beim Neuentwurf gleich.

Die drei Basic-Choreografien gehören zu einer Fähigkeit. Sichelsturm / Crescent Tempest ist die einzige aktive Ulti: 26 Mana, insgesamt 400 % Angriff, zwei Kontakte mit 25/75 % des gemeinsamen Budgets und 4,5 Sekunden Cooldown. Lokale Kampfaktionen und Serverreplays verwenden Aktions- und Kontaktkennungen. Manuelle Eingaben verändern nur die lokale Sitzung; Serverreplays geben bereits aufgelöste Ereignisse wieder.

Das Repository enthält Code, Migration 0034, Tests, Dokumentation, Quellenskripte und alle benötigten Spielassets einschließlich sechs Laufzeittexturen. Originalzeichnungen, Entwürfe, Prüfbilder, Videos, Testprotokolle und APKs bleiben lokal. Ein frischer Klon kann die mitgelieferten Unity-Assets verwenden und daraus bauen. Der optionale vollständige Bilderexport benötigt den lokalen Originalbestand; Eingänge und Schritte stehen in [der Quellenanleitung](../ArtSource/Kael/V2/README.de.md).

## Finale Körperkorrektur

Der Kopf sitzt zwischen getrenntem hinterem Kragen und vorderer Halsüberdeckung. Beide Kragenteile folgen demselben Torso-Bone. Sichtbare Zeichnungen besitzen gemessene eigene Körperanschlüsse; Ansichtswechsel wechseln Anschlüsse, Fußziele, IK und Zeichenreihenfolge gemeinsam. Die Front-Schulter besitzt eine vollständig gemalte Ärmelunterlage unter dem getrennten Panzer. Seitenhüfte und Mantelsaum sind vollständig; freie Schulter und Ellbogen überlappen geschlossen.

In drei Korrekturdurchläufen wurden zusätzlich hervorstehender Seitennacken und fremde Haut-/Matte-Pixel an Rücken- und Seitenplatten entfernt. Der finale Atlas enthält 46 Teile, Layoutversion 4, 2048×2048 Pixel, Straight Alpha, 256 Pixel pro Welteinheit. Sein SHA256 lautet `aa861577e90838e23b2a8092860a9eb2712d9efa48f966b7c2fadd3a7e9e7594`.

## Durchgeführte Prüfungen

Alle Unity-Läufe verwendeten isolierte Testprofile und liefen im Hintergrund. Die folgenden Ergebnisse stammen vom finalen Körperstand vor diesem Git-Push; die großen Nachweise sind bewusst nur lokal vorhanden.

| Prüfung | Ergebnis | Lokaler Nachweis unter `artifacts/kael/` |
|---|---|---|
| Export und Rig | Exit 0, 46 Teile, Bindungen und Perspektivkurven einschließlich `TorsoBack` geprüft | `pixel-repair-2-final-render.log` |
| Posen und Übergänge | 30 Posen und 28 Übergangsbilder bei 1024×1024 | `body-fix-review/pixel-repair-2-final*` |
| Bewegung | 551 echte Unity-Samples bei 60 fps; davon 490 aus acht aktiven Clips und 61 Legacy-Skill-Samples | `pixel-repair-2/motion-final/` |
| Finale Sichtprüfung | Alle 490 aktiven Samples, 30 Posen und 28 Übergänge gesichtet; kritische obere Anschlüsse zusätzlich bei 6× geprüft, keine weitere belegbare Ablösung | `pixel-repair-2/space-audit/iteration3/FINAL_UPPER_BODY_REVIEW.de.md` |
| Tatsächliche Canvas-Größe | Elf Originalframes aus Home, Formation und Kampf samt 8×-Ausschnitten geprüft; keine weitere sichtbare Montageablösung in dieser Stichprobe | `pixel-repair-2/real-canvas-review/REAL_CANVAS_REVIEW.de.md` |
| Unity-Regression | Exit 0, 19 übergeordnete Prüfungen einschließlich Speicherung, Formation, Kampfautorität, Fokus/Pause und bestehendem Spielumfang | `pixel-repair-2-unity-regression.log` |
| Echter Spielpfad | `passed`, 4.034 echte Game-View-Frames bei 540×960, 144,532 Sekunden; fünf Kämpfe, manuell/Auto, 1×/2×, Wiederholung, Platz 7, Bank, Speichern/Laden und tatsächlicher Tod | `gameplay-pixel-repair-2/acceptance.json` |
| Android | Erfolgreicher IL2CPP/ARM64-Build; ZIP-CRC, Manifest und AArch64-Bibliotheken geprüft | `pixel-repair-2-android-build.log`, `pixel-repair-2/android-package.json` |

Unmittelbar vor dem Push bestand zusätzlich `go test ./... -count=1` für alle Backend-Pakete. Der opt-in PostgreSQL-Migrationstest blieb dabei deaktiviert; es wurde keine Datenbank verändert. Die Hashprüfung bestätigte unveränderten finalen Atlas, Layout, Prefab, Rig, Builder und Quellenaufbereitung gegenüber dem dokumentierten Unity-Teststand.

Die vier regulären Testkämpfe enthalten jeweils 20–24 normale Kael-Aktionen und eine abgeschlossene Ulti mit zwei Kontakten. Der fünfte prüft die echte Todespose. Es wurden nicht alle 4.034 Kampfframes einzeln pixelgeprüft. Technische Anschlussmessungen und Builds ersetzen keine gestalterische Beurteilung. Ein Emulator- oder Gerätetest des finalen APK wurde nicht durchgeführt.

Die ausführliche lokale Dokumentation ist `artifacts/kael/pixel-repair-2/PRUEFUNG.de.md`. Frühere Körperfreigaben und APKs mit `Bodyfix` oder `V2-QA` im Namen betreffen beanstandete Vorstände. Der finale lokale Build liegt unter `Builds/Android/Kael-Koerperkorrektur-20260910/`.

## Prüfungen erneut ausführen

Bei geschlossenem Unity-Editor vom Projektstamm aus:

```powershell
scripts/run-kael-background.ps1 -Method KaelValidationSuite.RunAll -LogName regression.log
scripts/run-kael-background.ps1 -Method KaelAssetReview.Run -LogName rig-rebuild.log
```

Backend-Tests aus `backend`: `go test ./... -count=1`. Der gesonderte PostgreSQL-Migrationstest ist nur mit einer leeren isolierten Testdatenbank freizuschalten; die Einschränkungen stehen in [backend/README.md](../backend/README.md). Dieser Push führt keine Migration oder Bereitstellung auf einem laufenden Server aus.
