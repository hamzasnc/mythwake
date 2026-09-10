# Kael: einzelne Bewegungsprobe

Diese Probe bearbeitet zuerst einen einfachen Schwerthieb in derselben Körperansicht. Sie ersetzt noch keine Spielanimation. Die bisherigen technischen Prüfungen der breiteren Überarbeitung waren keine gestalterische Freigabe; der Nutzer hat deren sichtbare Bewegung verworfen.

`strike-study.json` enthält die bewusst gesetzten Zeiten, Positionen und Winkel sowie die Geschwindigkeit an jedem Schlüssel. Die Kanäle von Hüfte, Brust, Kopf, Armen und Kleidung sind zeitlich versetzt. `velocity` verwendet die jeweilige Einheit pro Sekunde, bei Winkeln Grad pro Sekunde. Füße behalten ihre Bodenpunkte; es gibt keine Perspektivwechsel und keine Effekte.

Der Editor wertet die Körperanschlüsse und IK ausschließlich beim Erstellen aus. Er speichert die resultierenden lokalen Knochen- und Zeichnungsbewegungen als `strike_study_baked.anim` unter `Assets/_Mythwake/ArtStudies/KaelStrike/`. Das zugehörige Studien-Prefab spielt diesen Clip mit `KaelBakedClipPlayer` ab; es enthält keine `KaelRig`- oder IK-Komponenten. Interpolation zwischen den gespeicherten Animationsschlüsseln bleibt der normale Bestandteil der Clipwiedergabe.

Gesicht und Hals verwenden zwei komplementäre Meshteile derselben bestehenden Kopfzeichnung. Die nachgebesserte Handbahn verläuft durchgehend unterhalb des Kinns. Klinge und Waffenhand liegen für diese Aktion vor dem Gesicht; die Reihenfolge bleibt während des gesamten Clips fest. `KaelStudyHeadLayers` enthält die Schnittkontur in Bildpixeln und prüft Geometrie und UVs nach erneutem Laden der gespeicherten Assets. Der Atlas wird nicht verändert.

Aus dem Projektverzeichnis im Hintergrund erzeugen:

```powershell
& .\scripts\run-kael-background.ps1 -Method KaelStrikeStudyAuthoring.Run -LogName strike-study-take15.log -MotionReviewFolder take15 -StudyPixelAudit
```

Für jede weitere Prüfung einen neuen `take`-Namen verwenden. Die Ausgabe unter `artifacts/kael/strike-study/` enthält die tatsächlich abgespielten Bildfolgen, Messdaten und die verwendete Quelle. Fingerprints schützen nachträgliche Handbearbeitung der erzeugten Assets vor Überschreiben.

Die aktuelle überprüfte Ausgabe liegt unter `artifacts/kael/strike-study/take14/`. `scripts/review-kael-strike-study.py` erzeugt daraus `Kael-Einzelhieb.mp4`: fünf vollständige Wiederholungen in Normalgeschwindigkeit, daneben derselbe Ablauf mit den Canvas-Höhenparametern 108, 118 und 132. Die Umrechnung verwendet wie `KaelAnimationView` eine Referenzhöhe von 3,1 Welteinheiten; die Zahlen bedeuten nicht exakt so viele bemalte Körperpixel. Die Videokomposition verändert keine Bewegungsframes.

`-StudyPixelAudit` aktiviert zusätzliche Rendervergleiche für beide Hände. `pixel-audit.csv` und `pixel-audit-free-hand.csv` messen deren sichtbaren RGB-Beitrag gegenüber der isolierten Zeichnung. `scripts/audit-kael-strike-frames.py artifacts/kael/strike-study/take14` erstellt daraus native Detailseiten und `pixel-review/pixel-audit.json`. Diese Diagnose verändert keine Standardframes, Körperpositionen oder Spielstände.

Zuerst sind Stand, Ausholen, Kontakt und Abfangen als Körperhaltungen zu prüfen, dann der vollständige Ablauf in Normalgeschwindigkeit und tatsächlicher Spielgröße. Ein kleiner Bake-Fehler beweist nur die Übereinstimmung zwischen Quelle und gespeichertem Clip. Ob der Schlag gut aussieht, muss unabhängig davon anhand seiner sichtbaren Bewegung bewertet werden. Erst nach diesem ersten Qualitätsmeilenstein folgen weitere Animationen und die Integration in den Kampf.

Ergebnisse und Grenzen der aktuellen Probe: `docs/KAEL_SINGLE_STRIKE_STUDY.de.md`.
