# Kael V2 – Integration und Bedienung

Die aktive Gestaltung und Exportanleitung stehen in [V2/README.de.md](V2/README.de.md). Historische Quellen und ursprüngliche PNG-Zeichnungen bleiben lokal.

Die aktuelle Korrektur von Kopfanschluss, Körperproportionen, Armen, Schulterplatte und Beinen ist im [Integrations- und Prüfstand](../../docs/KAEL_INTEGRATION.de.md) zusammengefasst. Die ältere lokale Körperfreigabe wurde nach erneuter Beanstandung zurückgenommen; maßgeblich ist die dort beschriebene dritte Iteration der zweiten Pixelprüfung.

## Kael auswählen

1. **Helden / Heroes** öffnen und Kael auswählen. Portrait und Beschreibung zeigen die neue Gestaltung.
2. In **Kampagne oder Dungeon → Formation** einen der sieben Plätze anklicken und Kael aus der Bank zuweisen. Drag-Zuweisung ist ebenfalls vorhanden.
3. Kampf starten. **AUTO** verwendet seine aktive Ulti automatisch; manuell wird die Kael-Skillkarte bei vollem Mana anklickbar.

Es gibt acht verfügbare Helden und sieben aktive Kampfplätze. Kaels bestehende ID `hero_kael`, Fortschritt und Ausrüstung werden weiterverwendet. Die fünf Aufstellungspresets behalten sieben Plätze.

## Laufzeit

`IdlePrototypeController` führt den echten Canvas-Kampf aus. `MythwakeCombatSession` bestimmt lokale Aktionen, Schaden, Mana, Abklingzeit und Ergebnis. Serverereignisse laufen über `MythwakeCombatReplay`; manuelle Skillwahl ist dort keine lokale Autorität.

Eine Basic-Aktion wählt `attack_cross`, `attack_spin` oder `attack_jump`. Alle Kontakte teilen das Gesamtbudget; `actionId` und `contactId` verhindern doppelte Auflösung. Ein erfolgreicher Basic vergibt insgesamt zwei Mana. Sichelsturm / Crescent Tempest besitzt zwei Kontakte bei .40/.92 Sekunden mit 25/75 Prozent des gesamten 400%-Budgets. Mana26, Cooldown4.5 Sekunden, keine neue passive Fähigkeit.

`KaelRig` steuert Bones, Spritewechsel und IK mit einem manuellen Animationsgraphen. `KaelAnimationView` projiziert unabhängige Instanzen über einen gemeinsamen Renderatlas in Kampf, Home und Formation. `KaelCombatVfx` verwendet den gemalten Atlas und einen eigenen Arenamaskenbereich. `KaelUltimateBackdrop` stellt die eigene Aktionsillustration und die Abdunklung dar. Der Fokus bleibt am echten Bodenpunkt des Charakters.

Alte Serverereignisse ohne Varianteninformationen nutzen die kompatiblen Ein-Kontakt-Clips `attack` (.60/.20) und `skill_legacy` (1.0/.40). Die neuen lokalen und Serveraktionen verwenden die V2-Profile.

## Quellen und Prüfungen

- Native bearbeitbare Assets: `Assets/_Mythwake/Resources/Characters/Kael`.
- Technische Aufbereitung: `ArtSource/Kael/V2`; Originalzeichnungen bleiben lokal und sind nicht Teil eines frischen Git-Klons.
- Rig-Rebuild aus mitgelieferten Spielassets: `scripts/run-kael-background.ps1 -Method KaelAssetReview.Run -LogName rig-rebuild.log`.
- Vollständiger Quellenexport mit lokalen Originalzeichnungen: `scripts/rebuild-kael.ps1`.
- Verborgene Tests: `scripts/run-kael-background.ps1`, mit pro Lauf eigenem isolierten Testprofil.
- Rig-/Bewegungsbilder, vollständige echte Kampfaufnahme, Ereignisprotokolle und Android-Prüfbericht: lokal unter `artifacts/kael`, nicht im Git-Repository.

Eine Rig-Montage ist ausdrücklich kein Kampfnachweis. Ein gebautes APK ersetzt keinen Emulator- oder Gerätetest; diese Abnahmearten werden getrennt dokumentiert.
