# Gemalte Unterzeichnung fuer Kaels vordere Schulter

Die bisherige Front-Zeichnung hatte nach dem Abtrennen des Schulterpanzers keinerlei Stoffpixel am Schultergelenk. Die naechsten sichtbaren Pixel lagen 32,53 Atlas-Texel entfernt. Diese Ergaenzung liefert die bisher vom Panzer verdeckte, fertig gemalte Stoffoberflaeche. Sie ersetzt weder den bestehenden Panzer noch die sichtbare Ellbogenhaut.

- `front-cloth-source.png`: unbearbeitetes Original aus dem eingebauten Bildwerkzeug, 1028 x 1530 RGBA mit echtem Alphakanal.
- `PROMPT-front-cloth.txt`: vollstaendiger ausgefuehrter Prompt. Referenzen: `../kael-design.png` und `../parts/UpperArmNear.png`, beide vorher visuell geprueft.
- `front-cloth-proximal-underlay.png`: technisch auf das vorhandene 127 x 189 Arm-Canvas registrierte Schulterunterzeichnung. Unter das nach der Panzertrennung verbleibende Original zeichnen.
- `front-underlay-registration.json`: source/target-Landmarks, gleichmaessige Skalierung mit Rotation, Hash und gemessener Deckungsradius.
- `prepare_reference_underlay.py`: reproduziert die technische Registrierung und die Reviewbilder; malt keine neuen Farben und veraendert keine Spieldaten.
- `front-arm-layered-preview.png`: Referenzkomposition mit unveraendertem sichtbarem Originalaermel davor.
- `front-underlay-review.png`: 4-fache Vergroesserung fuer den Vergleich, Magenta markiert das Schultergelenk.

Der registrierte Schulterpunkt liegt bei (73.025, 72.85475) in Bildkoordinaten von oben links. Ein Kreis mit Radius 22,73 Texeln darum ist vollstaendig mit gemaltem Stoff bedeckt (Alpha >= 240). Der vorhandene Schulterpanzer muss unveraendert separat davor bleiben. Beim Einbau zuerst den alten Panzer abtrennen, anschliessend diese Unterlage hinter die verbliebene Armzeichnung setzen.

Es wurden nur diese neuen Quelldateien geschrieben. Atlas, Laufzeitcode und Animationen wurden hier nicht veraendert. Seiten- und Rueckenzeichnungen besitzen bereits eine gemalte Schulterflaeche und wurden nicht neu gestaltet. Visuelle Freigabe erfordert weiter Unity-Pruefungen mit angehobenem, gedrehtem und gesenktem Arm; die einzelne Quelldatei allein belegt keine fehlerfreie Koerpermontage.

Werkzeug: ausschliesslich eingebautes `image_gen`, kein API-Fallback und kein kostenpflichtiger Kauf. Ein erster Versuch mit expliziter Ellbogenhaut wurde vom Ausgabefilter verworfen; die ausgewaehlte Zeichnung ist daher eine reine Stoffunterlage, waehrend die vorhandene originale Ellbogenzeichnung erhalten bleibt.
