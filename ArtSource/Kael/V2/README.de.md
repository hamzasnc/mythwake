# Kael V2 – bearbeitbare Charakterquellen

Kael ist ein kompakter, aggressiver Klingentänzer mit elfenbeinweißem Haar, asymmetrischer dunkler Kleidung, geteilten Mantelschößen und einer langen Klinge mit karminroter Sichel-Parierstange. ID `hero_kael`, Besitz und Fortschritt bleiben erhalten. Der gestalterische Maßstab steht in `docs/CHARACTER_QUALITY_STANDARD.de.md`.

## Quellen und Aufbereitung

**Git-Umfang:** Original- und Zwischenzeichnungen (`*.png`) bleiben auf Wunsch des Besitzers lokal. Die vorbereiteten Spieltexturen, Atlasdaten, Rig und Clips unter `Assets` sind vollständig versioniert; ein normaler Unity-Build benötigt die Originale nicht. Die folgenden Quelldateien beschreiben den lokalen vollständigen Export, nicht den Inhalt eines frischen Klons.

Die Originalzeichnungen entstanden mit dem eingebauten Bildwerkzeug. `generation-prompts.json` enthält die verwendeten Prompts und die Herkunft der Dateien. Die erste Proportionsstudie und der fehlgeschlagene Alpha-Versuch bleiben als verworfene Zwischenstände erhalten. Die technische Freistellung per Python wurde am 10. September 2026 ausdrücklich freigegeben.

- `parts-front-source.png`: zwanzig eigenständige Vorderansicht-Teile.
- `parts-swaps-source.png`: passende Rück- und Seitenzeichnungen.
- `rear-lower`: ergänzende Rückzeichnungen von Gürtel, Oberschenkeln, Waden und Stiefeln. Sie ersetzen die irreführende Kombination aus Rückentorso und Frontbeinen.
- `shoulder-underlay`: eigenständig gezeichnete, vollständig gemalte Ärmelunterlage unter dem Front-Schulterpanzer, einschließlich Prompt und gemessener Registrierung.
- `expressions-source.png`: Aktions-/Blinzelgesichter und Handzeichnungen.
- `portrait-source.png`, `skill-icon-source.png`: Sammlungsportrait und Sichelsturm-Symbol.
- `action-portrait-source.png`: eigene dynamische Ulti-Illustration.
- `vfx-source.png`: gemalte Schnitt-, Auflade- und Trefferformen.
- `parts/*.png`: freigestellte, zugeschnittene, einzeln bearbeitbare Anhänge.
- `prepare_assets.py`: reproduzierbare Freistellung, Pivots, Weltmaße und Atlaspackung.
- `prepare_effects.py`: technische Aufbereitung der Effekttexturen.

Die neutralen Hintergründe der Körperteile werden ausschließlich über außen verbundene Pixel entfernt. Dadurch bleiben eingeschlossene helle Haare und Rüstung erhalten. Die ausgemessenen Zuschnitte, Gelenkpositionen und Pivots stehen im Skript. Nach dem Skalieren wird mit echtem Straight Alpha in einen 2048×2048-Atlas bei 256 Pixeln pro Welteinheit gepackt. Die Hell-/Dunkel-Prüfblätter dienen der Kantenkontrolle, nicht als Kampfbeweis.

Jede Kopf- und Körperansicht behält ihr eigenes Seitenverhältnis. `landmarks` enthält gemessene Anschlüsse relativ zum jeweiligen Sprite-Pivot in Welteinheiten; `joints` und `viewJoints` enthalten daraus zusammengesetzte neutrale Bone-Positionen. Die Front-Hüfte liegt bei Y=1,40. Offene Montagekappen an Arm-/Beinteilen werden nur innerhalb der vorhandenen gemalten Überlappung abgeschnitten. Willkürliche Breitenstreckung wird nicht mehr verwendet.

Der Kragen ist in `TorsoBack` hinter dem Kopf und `Torso` vor dem Hals aufgeteilt. Beide Zeichnungen verwenden denselben Torso-Bone und identische Pivots; die geschlossene Montagekappe ist entfernt. Der separate historische `Collar`-Quellenanhang wird nicht zusätzlich gerendert. `PauldronNear` und seine Ansichtsvarianten enthalten die Schulterplatte getrennt vom Oberarm. Der bewegte Arm besitzt eine vollständige gemalte Schulterunterlage; die Überdeckung hängt dadurch nicht allein von der gegenläufig gedrehten Platte ab. Beim Ansichtswechsel wechseln Sprite, anatomische Registrierung, Fußziele, Knie-IK und Zeichenreihenfolge gemeinsam; die Animationsdauer bleibt gleich.

`KaelRig.bodySockets` speichert die gemessenen Anschlüsse pro sichtbarem Sprite im Prefab. Nach Auswertung des Animationsgraphen und vor IK werden die Kind-Bones daran gebunden. Damit mischt ein Clipübergang keine Front-/Rück-Schulterposition zwischen zwei diskreten Zeichnungen. Körperrotation, Root-/Hüftbewegung, IK-Ziele und Stoffverformung behalten ihre Animation. Deckungsgleiche Anschlüsse müssen zusätzlich visuell auf korrekte gemalte Überlappung geprüft werden.

## Reproduzierbarer Unity-Export

Für einen Rig-Rebuild aus den versionierten Spieltexturen genügt `scripts/run-kael-background.ps1 -Method KaelAssetReview.Run -LogName rig-rebuild.log`. Dafür werden weder Python noch Originalzeichnungen benötigt.

Der vollständige Quellenexport benötigt zusätzlich alle sieben oben genannten `*-source.png` im V2-Stamm, die sieben `rear-lower/*-source.png`, `shoulder-underlay/front-cloth-source.png`, `shoulder-underlay/front-cloth-proximal-underlay.png` und die versionierte `front-underlay-registration.json`. Die registrierte Schulterunterlage ist ein eigener Eingang; die beiden Hauptskripte erzeugen sie nicht automatisch. Diese Bilder müssen vor einem vollständigen Re-Export aus dem lokalen Quellenbestand wiederhergestellt werden.

Vom Projektstamm `scripts/rebuild-kael.ps1` ausführen. Voraussetzungen: aktivierte kostenlose Unity-Personal-Lizenz, Unity **6000.4.5f1**, Unity 2D Animation **14.0.3**, Python mit **Pillow und numpy**. Das Skript erlaubt alternative `-UnityPath`- und `-PythonPath`-Angaben und startet Unity im Hintergrund. Eine kostenpflichtige Spine-Lizenz ist für Kael nicht erforderlich.

Das Ergebnis liegt in `Assets/_Mythwake/Resources/Characters/Kael`. Das Prefab enthält eigene Bones, SpriteSkins, vier IK-Ketten und Waffen-/Griff-/VFX-Anker. Haare und Mantelschöße besitzen gewichtete Meshes. Austauschzeichnungen und Zeichenreihenfolge sind in den Clips hinterlegt. Keine Ravik-Datei ist Eingabe dieser Produktion.

Animationen werden unter `Animations/GeneratedV2` erzeugt. SHA256-Fingerprints verhindern das Überschreiben nachträglich veränderter Clips. Handbearbeitete Alternativen gehören nach `Animations/Authored/<clipname>.anim`; diese werden bevorzugt und niemals vom Generator überschrieben. Änderungen an Pivots und Grundproportionen gehören in `prepare_assets.py`, Änderungen an der generierten Choreografie in `KaelAssetBuilder.cs`.

## Aktionen und Kontakte

| Animation | Dauer | Autoritative Kontakte |
|---|---:|---|
| idle | 2,40 s | keine; nahtlose Schleife |
| run | 0,64 s | keine; mit Fortbewegung gekoppelt |
| attack_cross | 0,84 s | 0,24 / 0,52 s, 50 / 50 % |
| attack_spin | 0,84 s | 0,42 s, 100 % |
| attack_jump | 0,84 s | 0,46 s, 100 % |
| skill – Sichelsturm | 1,32 s | 0,40 / 0,92 s, 25 / 75 % |
| hit | 0,20 s | keine zusätzliche Kampfaktion |
| death | 0,90 s | Endpose halten |

Die drei Basics sind Varianten **einer** Fähigkeit. Jede Aktion verfügt über ein gemeinsames Schadensbudget; Kontakt-IDs verbinden Auflösung, Zahl und Treffer-VFX. Mana wird einmal pro erfolgreicher Basic-Aktion vergeben. Sichelsturm kostet weiterhin 26 Mana, verursacht insgesamt 400 % effektiven Angriff und behält 4,5 Sekunden Cooldown. Es wurde keine neue passive Fähigkeit hinzugefügt.

Die Ulti hält kurz die Umgebung an und fokussiert den tatsächlichen Charakter. Die eigene Aktionsillustration liegt am Rand; Drehung, aufsteigender Schnitt und Abschluss bleiben sichtbar. Der Fokus und die Kontakte folgen der gemeinsamen Kampfuhr, einschließlich Pause und 1×/2×. Animationsmarker lösen keinen zusätzlichen Schaden aus.

## Prüfung

Die anschließende Überarbeitung von Haltung, Ganzkörperclips, Standfüßen, Nachbewegung und Runtime-Übergängen ist in [KAEL_ANIMATION_REWORK.de.md](../../../docs/KAEL_ANIMATION_REWORK.de.md) dokumentiert. Die folgende Körperkorrektur bleibt als vorheriger Atlas-/Montagestand erhalten; aktuelle Animationsnachweise liegen separat unter `artifacts/kael/motion-rework`.

`KaelAssetReview.Run` exportiert das echte Rig, prüft seine Daten und schreibt Posen. `KaelAssetReview.CaptureMotion` erzeugt eine getrennt gekennzeichnete Bewegungsprüfung ohne VFX. `KaelValidationSuite.RunAll` benötigt ein isoliertes `-mythwakeTestProfile`. `KaelGameplayAcceptance.Run` prüft und zeichnet den tatsächlichen Spielpfad auf; angeordnete Rigbilder ersetzen diese Aufnahme nicht.

`KaelAssetReview.RebuildBodyReview` kombiniert Export, Prüfbilder und Bewegungsaufnahme mit `KaelBodyAssemblyReview`: neutrale Front-/Seiten-/Rückansichten, Kopfnicken und echte Kontaktposen. Die Pose-JSONs enthalten 14 Anschlussmessungen sowohl im Rendererraum als auch nach SpriteSkin-Bindtransformation. Fehlende Messpunkte sind ausdrücklich als nicht verfügbar markiert. Die Daten prüfen Montagefehler; sie bewerten keine gezeichnete Anatomie oder Animationswirkung automatisch.

`KaelAssetReview.RebuildPixelReview` ergänzt die Körperprüfung durch 1024×1024-Bewegungsaufnahmen bei 60 fps. `capture-settings.json` hält Auflösung und Bildrate fest; `bounds.csv` bezeichnet jeden tatsächlichen Sample. Die Videoausgabe übernimmt diese Bildrate. `scripts/review-kael-pixels.py` ordnet ausschließlich unveränderte Renderbilder an.

Aktuelle Körperkorrektur, Resultate und Grenzen: [versionierter Prüfstand](../../../docs/KAEL_INTEGRATION.de.md). Detaillierte Iterationen und Aufnahmen bleiben lokal unter `artifacts/kael/pixel-repair-2/PRUEFUNG.de.md`. Die ältere Sichtfreigabe in `artifacts/kael/KOERPERFIX.de.md` wurde nach erneuter Beanstandung zurückgenommen. Ein bestandener Build ist keine automatische gestalterische Freigabe.
