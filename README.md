## 0.19.7: Gemeinsamer Knoten mit seitlich passenden Fahrbahnen

Autobahnanschlüsse unterscheiden jetzt zwischen dem gemeinsamen logischen Verbindungsknoten und den physischen Mittellinien der einzelnen Straßen. Bei eindeutigen 3+1/4-Anschlüssen bleiben die drei durchgehenden Fahrspuren bündig; die Rampenmittellinie trifft die zusätzliche äußere Fahrspur. Auch reine Breitenwechsel behalten die Spurflucht. Die native Spielpipeline unterstützt abweichende horizontale Kurvenendpunkte für Straßen ohne StrictNodes; die Mod nutzt diese Möglichkeit statt alle Straßenmittellinien durch die Knotenmitte zu führen.

Die Fähigkeit wird im Straßenkatalog aus NetGeometryData gelesen und beim Profil-Refresh übernommen. Ohne passende Spuranzahl, bekannte Spurdaten, eindeutige Richtung und gemeinsame Höhenebene wird kein solcher Versatz geraten. Topologische Endpunkte bleiben identisch; Straßenring- und Kreisverkehrsarme sind ausgenommen. Vorschau, Hover und Auswahl zeichnen die physischen Kurven. Bestehende Straßen werden nicht verändert.

Validierung: Mod-Build und vollständige Tests bestanden. Neue Tests decken Links-/Rechtsverkehr, Ein-/Ausfahrt, drei Maßstäbe, drei Rotationen, Teilvorschau, gemeinsame Knoten, bündige Fahrspuren und ungeeignete Prefabs ab. Die gespeicherten 43 Baukurse wurden mit der 1/3/4-Spur-Zuordnung als Regression geprüft; alle vier Rampen erhalten den äußeren Anschluss. Das ist keine Ingame-Bestätigung der nativen Fahrspurverbindungen. Das erweiterte last-connections.json enthält nun Prefab-IDs, physische Kurvenendpunkte, erzeugte Kurven und native Fahrspurverbindungen. Nach Neustart frisch bauen und das Ergebnis prüfen.

## 0.19.6: Gemeinsame Anschlussrichtung der Autobahn

Die beiden weiterführenden Autobahnkurven werden an eindeutigen Anschlüssen gemeinsam ausgerichtet, auch über Straßentyp-/Untergruppengrenzen hinweg. Die Rampenberechnung verwendet anschließend diese gemeinsame Richtung. Knotenpositionen und Fahrbahnversatz bleiben erhalten. Nur bekannte gerichtete Hauptfahrbahnen auf gleicher Höhenebene werden berücksichtigt; mehrdeutige Abzweige oder Richtungsknicke über 25 Grad werden nicht geglättet. Kontrollpunktverschiebungen sind auf 4 m begrenzt. Dies ist eine lokale gemeinsame Kurvenplanung, kein neuer nativer Spielknotentyp und noch keine explizite Zuweisung von Fahrspurverbindungen.

Validierung: vollständige Geometrie-/Datentests bestanden. Neues Regressionstestmaterial aus dem Bauprotokoll vom 04.10.2026 mit 43 Kursen: alle vier Rampenanschlüsse haben durchgehende Hauptfahrbahntangenten, unveränderte Endpunkte und begrenzte Kurvenabweichung. Native Darstellung und Fahrspurverbindungen sind noch im Spiel zu prüfen. Spiel neu starten und Anschluss frisch bauen; bestehende Straßen werden nicht verändert.

## 0.19.5: Tatsächliche Fahrspurpositionen

Die Straßeninitialisierung wird für das Auslesen der Fahrspurpositionen nachvollzogen: Standard-Komposition aus NetGeometrySection über die nativen NetCompositionHelpers. Der zuvor verwendete optionale DefaultNetLane-Puffer fehlt bei normalen Straßen-Prefabs; das Laufzeitprotokoll enthielt deshalb leere Spurpositionen. Die bestehenden Berechnungen für Autobahnverbreiterungen und lokale Rampentangenten erhalten nun die tatsächliche Spuraufteilung, sofern eindeutig verfügbar. Original-Kartendaten und vorhandene Straßen bleiben unverändert.

Validierung: Mod-Build und vollständige bestehende Geometrie-/Datentests bestanden. Die native Kompositionsauswertung ist gegen die installierte Spiel-API kompiliert und mit deren Initialisierungscode abgeglichen; tatsächliche Spurwerte und Anschlüsse müssen nach Spielneustart an einem frischen Stempel geprüft werden. Kein Nachweis einer vollständig behobenen Rampenverbindung durch diese Tests. Diagnostics/highway-lanes.json protokolliert die ausgelesenen Werte.

## Version 0.19.4: Rampen seitlich an die Fahrbahn heranführen

Einbahn-Autobahnrampen erhalten eigene Baugruppen. An bekannten gemeinsamen Knoten mit motorway/trunk wird bei sehr flachem Anschluss ausschließlich der anschlussnahe Bézier-Kontrollpunkt der Rampe angepasst. Der Zielwinkel berücksichtigt äußere Fahrspurposition bzw. Fahrbahnbreite und Rampenbreite. Autobahnkurven, gemeinsame Endpunkte und entfernter Rampenkontrollpunkt bleiben erhalten. Kurze notwendige Anschlüsse unter 16 Weltmetern werden nicht zwanghaft gebogen. Kreisverkehr-Anschlussgruppen und verschiedene Höhenregime sind ausgenommen. Die native Lane-Zuordnung entscheidet weiterhin das Spiel; keine direkte Lane-Verknüpfung wird erzwungen.

## Version 0.19.3: Fahrspuren statt Gesamtbreite ausrichten

Der Katalog liest DefaultNetLane-Positionen eindeutig gerichteter Spielstraßen aus. Normale Fahrspuren werden von virtuellen, Master-, Park-, Fahrrad- und sekundären Spuren getrennt. Die Autobahnausrichtung gleicht gemeinsame Fahrspurmitten von der Mittelstreifenseite ab und legt zusätzliche Spuren außen an. Das funktioniert auch bei gleicher Gesamtbreite. Bei bekannten inkompatiblen Spurabständen wird kein exakter Versatz erfunden; bei fehlenden Spurdaten bleibt der bisherige Breiten-Rückfall. Spurpositionen werden in gespeicherten Profilen erhalten, bei Katalogänderung aktualisiert und Bauplan-Caches verworfen. Diagnostics/highway-lanes.json enthält die tatsächlich ausgelesenen Autobahndaten.

Bestehende Straßen werden nicht verändert. Die native Anschlussgeometrie ist weiterhin ingame zu bestätigen; die mathematischen Tests ersetzen keine Verkehrsprüfung.

## Version 0.19.2: Außenseite anhand benachbarter Fahrbahn

Breitere Autobahnstücke werden von einer eindeutig nahen parallelen Hauptfahrbahn weg versetzt, unabhängig von der Quelllinienrichtung. Suche beschränkt auf gleiche Höhenregime und 4–80 Weltmeter Abstand, ohne Rampen. Bei mehrdeutigen Nachbarn auf beiden Seiten kein Versatz; ohne Nachbar bleibt die Verkehrsseitenregel. Der Rampenversatz läuft über vorhandene Zwischenpunkte bis maximal 64 Weltmeter aus, endet an Verzweigungen und lässt den entfernten Anschluss unverändert. Bestehende Straßen werden nicht verändert.

## Version 0.19.1: Kurze isolierte Reststücke

Vor dem Stempeln werden zusammenhängende, vollständig isolierte Straßenstücke unter insgesamt 16 Weltmetern ausgelassen. Betrachtet werden alle Kategorien nach Schnittpunktaufteilung, nicht einzelne Quellsegmente. Angeschlossene kurze Zufahrten, Ringe, Brücken und Tunnel bleiben erhalten. Erkennt das native Andocken einen Bestandsanschluss, bleibt die zusammenhängende Baugruppe erhalten. Die Vorlage bleibt editierbar und unverändert; ausgelassene Bauabschnitte werden im Ergebnis gezählt. Diagnose enthält jetzt Kurvenkontrollpunkte.

## Version 0.19.0: Autobahnverbreiterung nach außen

Breitere Untergruppen auf eindeutig gerichteten motorway/trunk-Hauptfahrbahnen werden gegenüber der Autobahn-Kategorienbreite um die halbe zusätzliche Spielstraßenbreite nach außen versetzt. Rechts-/Linksverkehr und Maßstab werden berücksichtigt. Angeschlossene Rampen und Nachbarabschnitte übernehmen denselben versetzten Knoten; entfernte Rampenpunkte bleiben erhalten. Vorschau, Bauplanung und Mausauswahl verwenden die korrigierte Geometrie. Quellkoordinaten und gespeicherte Zuordnungsschlüssel bleiben unverändert.

Die Ausrichtung setzt voraus, dass die Kategorienbreite die Bezugsfahrbahn beschreibt. Keine automatische Verschiebung von Zweirichtungsstraßen, Rampen als Hauptfahrbahn oder Knoten mit unterschiedlichen Höhenregimen. Bereits gebaute Straßen werden nicht verändert. Native Fahrbahnübergänge ingame prüfen.

## Version 0.18.9: Keine ungeprüften Erkennungszuordnungen

Straßenerkennung legt keine Untergruppe mehr mit einer ungeprüften Kategorienstraße an, wenn kein passender Kandidat existiert. Fahrtrichtung, Spurzahl und Autobahnregeln müssen passen. Mehrdeutige Treffer werden nur durch eine bereits passende Kategorienauswahl aufgelöst. Nicht erkannte Abschnitte behalten ihre bisherige Kategorienzuordnung; diese bleibt manuell zu prüfen. Bestehende Untergruppen werden nicht rückwirkend geändert. Diese Korrektur ist kein Nachweis einer Behebung der nativen Anschlussverformungen.

## Version 0.18.8: Deutscher Autobahn-Standard

Die zulässigen Standardbezeichnungen umfassen jetzt Schnellstraße und Einbahnschnellstraße, insbesondere Dreispurige Einbahnschnellstraße. Zuvor konnte die deutsche Bezeichnung von der automatischen Standardauswahl ausgeschlossen werden. Native Autobahnregeln, Fahrtrichtung und Spurzahl bleiben zusätzliche Bedingungen. Gespeicherte Zuordnungen und manuelle Breiten bleiben erhalten.

## Version 0.18.7: Kreisverkehr-Anschlussstraßen haben Vorrang

Erkannte getrennte Ein-/Ausfahrten verwenden die eigene Kreisverkehr-Anschlussstraßenauswahl auch bei Untergruppen/Erkennungsvorschlägen. Zuvor konnten Untergruppen die schmale Einbahnstraße durch eine zweispurige Kategorienstraße ersetzen. Gilt bis zur nächsten Straßenverzweigung/Zusammenführung; normale Zweirichtungszufahrten bleiben unverändert. Bestehende manuelle Anschlussstraßenauswahl bleibt erhalten. Bereits gebaute Straßen werden nicht verändert.

## Version 0.18.6: Straßenauswahl direkt in Unterkategorien

Die aufgeklappte Unterkategorie enthält eine Straßenauswahl mit aktuellem Namen, Suche, Kategorienfilter und Alle Straßen und Wege. Auswahl übernimmt sofort ohne zusätzlichen Bestätigungsbutton; Hauptkategorie bleibt unverändert. Der seitliche Editor wird für Straßenänderungen nicht mehr benötigt und damit nicht durch den schmalen Dock abgeschnitten.

## Version 0.18.5: Standards für Straßenkategorien

Nicht zugewiesene Kategorien erhalten passende verfügbare Standardstraßen: dreispurige Einbahn-Autobahn, zweispurige Hauptstraße nahe 16 m, schmalste geeignete zweispurige Wohnstraße und Asphaltweg. Bestehende Auswahl, Untergruppen und ausdrücklich manuelle Breiten bleiben erhalten. Fehlt ein geeigneter Kandidat, bleibt die Kategorie unverändert. Kreisverkehr und Zufahrten behalten ihre separate Auswahl.

## Version 0.18.4: Overlay-Klick öffnet Unterkategorie

Bei geöffnetem Mod-Fenster und sichtbarer Vorlage wählt ein kurzer Linksklick im normalen Standard-Spielwerkzeug die nächstliegende sichtbare Overlay-Mittellinie (12 Pixel Trefferabstand). Die Detailansicht öffnet die passende Untergruppe und hebt ihre Straßen hervor; ohne Untergruppe wird die Hauptkategorie fokussiert. Klick auf freie Karte hebt den Fokus auf. Mausziehen, UI-/Texteingaben, andere aktive Spielwerkzeuge und laufende Bauaufträge lösen keine normale Gruppenauswahl aus. Bereiche markieren bleibt beim bisherigen Markierungsverhalten. Der Gruppenfokus verändert keine Auswahl zum Entfernen und keinen Bauauftrag.

UI-Smoke mit simuliertem Overlay-Fokus prüft automatisches Aufklappen und verfügbare Bearbeitung; bestehende Bedienung bestanden. Native Maus-Hit-Erkennung und Gameface-Scrollen ingame noch zu prüfen.
## Version 0.18.3: Deutsche Namen und Hover-Hervorhebung

Technische Straßenklassen werden in Vorschlägen und bestehenden Untergruppen auf Deutsch angezeigt. Maus über Untergruppe oder Vorschlag hebt deren sichtbare Overlay-Linien weiß hervor, unabhängig von der roten Auswahl. Zusammengefasste Kurven bleiben erhalten. Verlassen, Einklappen und Schließen entfernen die Hervorhebung. UI-Smoke inklusive Hover-Befehl bestanden; ingame Darstellung noch prüfen.

## Version 0.18.2: Konservative Erkennung und gemeinsame Baugruppen

Automatische Kandidaten nur aus eingebauten Straßen mit konservativ zugelassenen deutschen/englischen Standardbezeichnungen. Straßenklasse, Fahrtrichtung und bekannte Spurzahl müssen passen. Keine Auswahl von Brücken-/Schotter-/Sondervarianten durch geringste Breite. Bei mehreren Kandidaten gilt die vorhandene Kategorienstraße oder der Rückfall darauf; Wege und Straßen ohne sichere Spurangabe bleiben ebenfalls bei der Kategorienauswahl. Dies ist weiterhin keine vollständige Analyse der nativen Fahrspuren.

Untergruppen mit identischem Straßenprofil und gleicher Kategorie/Höhenlage teilen nun einen Bauplan; redaktionelle Gruppengrenzen erzwingen keine extra Knoten. Tests mit X-Kreuzung aus unterschiedlichen 24-/8-m-Profilen bestätigen vier Arme mit identischem Zentrum. Native Darstellung/Befahrbarkeit muss ingame noch geprüft werden.

Bestehende Zuordnungen werden nicht umgeschrieben. Bereits übernommene falsche Vorschläge müssen über Zuordnung aufheben entfernt oder manuell geändert werden, bevor neue Vorschläge sie ersetzen können. Tests für Sondervarianten, mehrdeutige Kandidaten, gruppenübergreifende X-Knoten und bestehende Geometriesuite bestanden.
## Version 0.18.1: Kompakter Unterkategorienbaum

Untergruppen sind je Kategorienkarte standardmäßig hinter Unterkategorien (Anzahl) eingeklappt. Geöffnet zeigt jede Gruppe eine einzelne Zeile mit Namen und Abschnittszahl. Je Baum kann eine Detailzeile für zugewiesene Straße, Ändern und Aufheben geöffnet werden. UI-Smoke für Ein-/Ausklappen und bestehende Bearbeitung bestanden. Straßen-Erkennung und Bau unverändert.

## Version 0.18.0: Prüffähige Straßen-Erkennung

Kleine Ansicht > Weitere Einstellungen > Straßen erkennen erstellt Vorschläge aus vorhandenen highway-, lanes-, width- und Einbahnangaben. Vorschläge zeigen Gruppe, Spielstraße, Breite und Anzahl der Quellabschnitte. Übernehmen legt normale bearbeitbare Untergruppen an; erst danach ändert sich die Overlay-Breite. Bereits zugeordnete oder entfernte Straßen bleiben unberührt. Zu viele Gruppen werden nicht teilweise übernommen. Karten-/Zuordnungswechsel verhindern die Übernahme veralteter Vorschläge.

Neu geladene Vorschauen speichern highway, numerische lanes und width (Meter, optional m). Alte Offline-Vorlagen werden nicht aktualisiert; fehlende Details werden nicht erfunden. Rampen ohne lanes erhalten einen ausdrücklich geschätzten Einspur-Vorschlag. Bei fehlendem passenden Kandidaten bleibt die Kategorienauswahl. Kreisverkehrmarkierungen werden ausgelassen. Linienpositionen und Maßstab ändern sich nicht.

Spielstraßen: Fahrtrichtung aus RoadData-Flags, Spurzahl derzeit als Hinweis aus deutscher/englischer Straßenbezeichnung. Keine vollständige Lane-Layout-Analyse; unbekannte Bezeichnungen können keinen spurbasierten Treffer liefern. OSM-Breite und Spiel-Gesamtbreite sind nur näherungsweise vergleichbar. Nicht erkannte Straßen ohne nutzbare Angaben behalten ihre Kategorie. Vorschläge deshalb vor dem Bau prüfen und Untergruppen gegebenenfalls ändern.

Prüfung: Geometrie-/Datensuite, Rampenvorschlag mit Schätzkennzeichnung, unveränderte Untergruppen, Spurzahlhinweis und bestehende UI-Smoke-Suite bestanden. Ingame-Katalogtreffer und Autobahnbau noch zu prüfen.
## Version 0.17.1: Volle Kartenbreite

Deckkraftregler und Bauen-Button stehen jeweils auf voller Breite untereinander. Kompakte Kartenabstände und getrennte Footer-Fläche bleiben bestehen. UI-Smoke bestanden; Darstellung ingame noch zu prüfen.

## Version 0.17.0: Kompakte Kategorienansicht

Kartenabstände reduziert, Deckkraft und kurzer Bauen-Button nebeneinander. Die Kategorienliste verwendet den verbleibenden Flex-Platz statt 100% Fensterhöhe; der Footer reserviert eigenen Platz und überlagert die Karten nicht. Markierungsaktionen sind unter Anzahl markiert / Aktionen aufklappbar. Kreisverkehr, Wiederherstellen und Zusatzinformationen liegen unter Weitere Einstellungen. Bei vielen geöffneten Einstellungen ist auch deren Bereich in der Höhe begrenzt und scrollbar, damit die Bedienung erreichbar bleibt.

UI-Smoke inklusive geometrischer Prüfung auf Überlagerung, Auswahlaktionen, Untergruppen, Wiederherstellen und bestehender Bedienung bestanden. Darstellung im Spiel noch zu bestätigen. Straßenbau und Autobahngeometrie unverändert.
## Version 0.16.2: Anschlussarme bis zur Zusammenführung

Die Erkennung schmaler Einbahn-Anschlussarme läuft jetzt über seitliche Fuß-/Radweganschlüsse hinweg bis zur echten Straßenverzweigung oder Zusammenführung. Dort endet das Anschlussprofil; die weiterführende Zweirichtungsstraße behält ihr Profil. Richtungs- und Höhenwechsel bleiben Grenzen. Es werden keine künstlichen Verkehrsinseln erfunden: Die Form folgt weiterhin den kartierten getrennten Armen. Für das gewünschte Bild unter Kreisverkehr > Ein-/Ausfahrten eine schmale Einbahnstraße wählen.

Regressionstest: mehrteiliger Arm mit seitlichem Fußweg wird durchgehend erfasst; Fußweg, andere Merge-Zufahrt und anschließende Zweirichtungsstraße bleiben unverändert. Gesamte Geometriesuite bestanden. Darstellung und Befahrbarkeit im Spiel noch zu bestätigen.
## Version 0.16.1: Separate Ein-/Ausfahrtstraßen

Im Kreisverkehr-Auswahlbereich gibt es zusätzlich Ein-/Ausfahrten mit eigener Straßenauswahl. Erkannte, richtungsbekannte Einbahn-Anschlussarme werden vom geschlossenen Ring bis zur nächsten Verzweigung, Richtungs- oder Höhenänderung verfolgt. Die einspurige Einbahnstraße wird als Standard verwendet, sofern im Katalog erkannt. Ringprofil und Armprofil sind unabhängig und werden in Offline-Vorlagen gespeichert. Echte Zweirichtungszufahrten, Fußwege, Brücken und Tunnel erhalten keine automatische Einbahn-Übersteuerung. Explizite Untergruppen-Zuordnungen haben für Arme Vorrang. Quellrichtungen der Ein-/Ausfahrten bleiben erhalten; rückwärts definierte Prefabs werden berücksichtigt.

Tests: eigene Armprofile, getrennte Ein-/Ausfahrten, Erhalt von Zweirichtungszufahrten, gesamte Geometriesuite und UI-Auswahl einschließlich Platzprüfung bestanden. Der konkrete Kreisverkehr aus dem Screenshot und seine Befahrbarkeit sind noch ingame zu prüfen. Die Erkennung benötigt vorhandene Einbahn-Metadaten; unmarkierte geometrische Doppelzufahrten werden nicht geraten. Vorhandene gebaute Straßen bleiben unverändert.
## Version 0.16.0: Eigene Straße für Kreisverkehrsringe

Großes Fenster unter Straßentypen & Breiten sowie kleine Ansicht im unteren Bereich: Kreisverkehr-Straße frei aus verfügbaren Straßen auswählen. Die bekannte einspurige Einbahnstraße wird automatisch gewählt, wenn im Katalog erkannt; andernfalls ausdrücklich eine Straße auswählen. Gewählte Straße und Breite werden offline gespeichert. Freie Wahl umfasst Zweirichtungsstraßen: Diese behalten Gegenverkehr und sind daher nicht als normaler Einbahn-Kreisverkehr empfohlen.

Vollständige erkannte Ringe verwenden ein eigenes Straßenprofil und werden als geschlossene native Kurvenstraßen gebaut, nicht zusätzlich als zentrales Spiel-Kreisverkehrsobjekt. Ringrichtung: Rechtsverkehr gegen, Linksverkehr im Uhrzeigersinn; gelesen aus CityConfigurationSystem.leftHandTraffic. Rückwärts ausgerichtete Einbahn-Prefabs berücksichtigen DefaultIsBackward. Zufahrten behalten ihre Kategorie und Straße. Ringe mit verschieden kategorisierten Zufahrten sind unterstützt; gebaut wird weiterhin pro Kategorie, bei gemischten Ringkategorien gehört der ganze Ring zur häufigsten Ringkategorie. Unvollständige Ringe erhalten keine automatische Umorientierung.

Prüfung: Geometrietests für beide Verkehrsseiten, Ringprofil und unveränderte Zufahrten, gemischte Kategorien/umgedrehte Quellstücke und Offline-Roundtrip; UI-Test für Auswahl und Verkehrsrichtungsanzeige; Release-Build ohne Warnungen/Fehler. Tatsächliche Fahrtrichtung und Einfädeln müssen ingame bestätigt werden. Automatische Spurzahl-Anpassung der Zufahrten ist noch nicht enthalten. Vorhandene Straßen werden nicht automatisch ersetzt.
## Version 0.15.3: Ungebaute Fußwege unterteilen keine Fahrbahn

Ursachenabgleich: Das 8,05-m-Stück am untersuchten X-Knoten entspricht OSM-Weg 1214473352 (Lukkaer Straße). Zwei Fuß-/Radwege am nächsten Punkt erzwangen einen Planstopp trotz fehlender Wege im Straßenbauauftrag. Ungewählte Fuß-/Radwege reservieren jetzt keine Fahrbahnknoten. Reale Straßen anderer Kategorien bleiben Stopppunkte. Die Wege bleiben erhalten und werden bei späterem Bau über die vorhandene native Anschlusslogik angebunden; diese spätere Anbindung ist ingame noch zu prüfen.

Read-only-Prüfung der gespeicherten Odelzhausen-Vorlage: Anschluss nun 17,22 m als Kurve statt 8,05-m-Stück; vier Arme am selben Kreuzungspunkt erhalten. Alle Geometrie-/Datentests inklusive neuer Tests für ungewählte Fußwege und beibehaltene Straßenabzweigungen bestanden. Die tatsächliche Kreuzungsform und Befahrbarkeit sind weiterhin ingame zu prüfen. Vorhandene Stadtstraßen werden nicht automatisch umgebaut.
## Version 0.15.2: Anschlusskoordinaten und Diagnose

Schnittpunkte werden auf denselben kanonischen Zentimeterkoordinaten wie der Straßengraph berechnet. Bereits gemeinsame Endpunkte werden nicht erneut geschnitten. Negative Null wird bei Knotenschlüsseln vereinheitlicht; ein neuer Test für nahezu identische Endpunkte einer Geraden deckte diesen Fall auf. Alle Geometrietests bestanden. Dies bestätigt noch keine Behebung der gemeldeten nativen Verbindungsfehler.

Jeder abgeschlossene Bauversuch schreibt ModsData/CityStreetOverlay/Diagnostics/last-connections.json mit geplanten Anschlüssen und dem nativen temporären Netz vor Apply. Enthält gegebenenfalls betroffene bestehende Kanten; kein Nachweis des endgültigen Stadtzustands. Fehler beim Protokollieren unterbrechen den Bau nicht. Beim nächsten Versuch wird die Datei ersetzt. Keine Änderung vorhandener gebauter Straßen.
Versions-ZIPs liegen gesammelt im Ordner `Versionen/`. Neue Versionsarchive ebenfalls dort ablegen.

## Version 0.15.1: X-Kreuzungen

Die Bauplanung teilt sich geometrisch kreuzende Quelllinien an gemeinsamen, auf Zentimeter normalisierten Punkten, bevor Kategorien ausgewählt und Geraden/Kurven zusammengefasst werden. Dadurch bleiben vier Arme desselben X-Knotens erhalten, auch bei verschiedenen Kategorien. Auswahl und Untergruppen behalten ihre ursprünglichen Quellschlüssel; nur die temporäre Baugeometrie wird geteilt. Brücken, Tunnel und verschiedene bekannte Ebenen werden nicht verbunden. Alte Vorlagen ohne Höhenmerkmale können höhengetrennte Kreuzungen nicht zuverlässig unterscheiden.

Tests: X ohne Quellknoten, vier identische Anschlusspositionen, getrennte Kategorien, Auswahlübernahme, Brücken und Ebenentrennung sowie alle bisherigen Geometrietests bestanden. Release-Build geprüft. Native Verbindung im Spiel noch nicht bestätigt. Bereits gebaute Straßen werden nicht nachträglich repariert.

Kreisverkehr-Folgeschritt vereinbart: einspurige Einbahnstraße als Standard, freie Straßenauswahl, Fahrtrichtung nach Links-/Rechtsverkehr der Karte; Spurübergänge der Zufahrten prüfen. In dieser Version noch nicht implementiert.
## Version 0.15.0: Zusammenhängende Markierung und geometrische Ringerkennung

Klick und linker Rahmen schalten zusammenhängende Straßenverläufe zwischen Kreuzungen oder Profil-/Kategorie-/Höhenwechseln um. Bereits vollständig markierte Verläufe werden abgewählt, andere vollständig markiert. Jeder Verlauf wird pro Rahmen nur einmal geschaltet. Alt und rechter Rahmen bleiben reine Abwahl. Die rote Anzeige verwendet ebenfalls geplante Geraden/Kurven statt einzelner Quelldatenstücke; entfernte Bereiche und Untergruppen speichern weiterhin stabile Quellschlüssel.

Geschlossene einfache Ringe werden zusätzlich geometrisch anhand ihrer Form, Größe (4–30 m Radius) und mindestens zwei Zufahrten erkannt. Dies funktioniert ohne Roundabout-Metadaten und ohne gespeicherte Vorlagen zu verändern. Iterative Graphzerlegung verhindert Rekursionsprobleme großer Karten. Native Umwandlung erlaubt nun zwei statt mindestens drei Zufahrten. Unvollständige Ringe und Anschlüsse außerhalb der Bauauswahl bleiben zur Wahrung der Verbindungen Straßenringe; kategorienübergreifende native Kreisverkehre sind weiterhin nicht gelöst.

Geometrie-/Datentests inklusive Legacy-Ring, zwei Zufahrten, wiederholtem Rahmen und zusammenhängender Markierung bestanden. Native Bedienung und Kreisverkehrbau sind noch ingame zu prüfen.
## Version 0.14.1: Linienübergänge und Abwahlrahmen

Normale Overlay-Geraden und -Kurven verwenden runde statt flacher Enden, um keilförmige Lücken an Übergängen zu schließen. Transparente Überlagerungen können weiterhin sichtbar sein; dies ersetzt keinen zusammenhängenden Flächenrenderer und ändert nicht die nativen Bauabschnitte. Kreisverkehrerkennung ist farbunabhängig; die bisherigen Metadaten- und Anschlussbedingungen bleiben bestehen.

Rechte Maustaste ziehen wählt im Markierungsmodus Abschnitte ab; rechter Klick wählt ein getroffenes Teil ab. Linker Rahmen ergänzt, Alt bleibt ebenfalls zur Abwahl verfügbar. Beenden erfolgt über die vorhandene Schaltfläche. Die Welt bleibt mausdurchlässig. Release-Build ohne Warnungen/Fehler und UI-Smoke bestanden. Native Mausbedienung und Linienübergänge noch ingame zu prüfen.
## Version 0.14.0: Kurven aus groben Kartenpunkten

Künstliche kollineare Zwischenpunkte werden vor der Kurvenanpassung entfernt. Drei Quellpunkte können bereits eine native Kurve ergeben. Die Prüfung berücksichtigt die durch benachbarte Richtungen beschriebene Bogenform, statt jede Kurve an den geraden Quellsehnen zurückzuweisen. Quellstützpunkte bleiben innerhalb von 25 cm; scharfe Ecken und Graph-Anschlussgrenzen bleiben erhalten. Der Maßstab bleibt unverändert, kurze unvermeidbare Anschlüsse bleiben erlaubt.

Regression: Ein grob erfasster Viertelkreis mit künstlichen Zwischenpunkten ergab zuvor vier Geraden, jetzt eine kubische Kurve. Der Radius bleibt im Test zwischen allen Stützpunkten innerhalb von 0,5 m. Die bestehende 2-km-Gerade bleibt ein Bauauftrag. Sämtliche Geometrie-/Datentests bestanden.

Read-only-Planvergleich der gespeicherten Odelzhausen-Vorlage: Autobahnen zuvor 44 Geraden + 19 Kurven, jetzt 7 Geraden + 26 Kurven; Abschnitte unter 16 m von 13 auf 1 reduziert. Dies sind Planwerte, keine Messung tatsächlich gebauter Straßen. Das Spiel kann Bauaufträge nach eigenen Geometriegrenzen unterteilen. Native Anschlüsse breiter Straßen und das tatsächliche Bauergebnis sind noch ingame zu prüfen; hierfür wird keine Behebung behauptet.
## Version 0.13.2: Mausrad über der Markierungsfläche

Die transparente Markierungsfläche ist jetzt vollständig mausdurchlässig (pointer-events:none). Das Spiel kann damit die Maus wieder der Welt statt einer bildschirmfüllenden UI-Fläche zuordnen. Linksklick/Rahmen, Alt-Abwahl und Rechtsklick-Abbruch werden im Spielcode über das vorhandene Input-System erfasst; die UI zeigt nur noch den Rahmen über eine eigene Bindung. Eingaben über Spiel- oder Mod-UI, bei Texteingabefokus, Fokusverlust oder geschlossenem Mod-Fenster werden nicht als Auswahl verarbeitet. Die Kamera bleibt auf der Karte frei, Werkzeugaktionen bleiben während der Auswahl gesperrt.

Browserprüfung: Welt bleibt Hit-Test-Ziel auch unter dem sichtbaren Rahmen; echte Wheel-Ereignisse erreichen die Welt; Rahmenkoordinaten und Abbruch geprüft. Release-Build erfolgreich. Tatsächlicher Zoom und native Mauserfassung müssen noch ingame überprüft werden.

## Version 0.13.1: Navigation beim Markieren

Der Markierungsmodus sperrt auf der Karte nur Werkzeugaktionen, nicht mehr die Kamera. Mausrad und WASD werden von der Markierungsfläche nicht abgefangen. Über Mod-Fenstern bleiben Kamera- und Werkzeugsperre bestehen. Browserprüfung für Freigabe beim Verlassen des Fensters und unverhinderte Wheel-/W-Tastenereignisse bestanden; Release-Build ohne Warnungen/Fehler. Tatsächliche Navigation im Spiel noch zu prüfen.

## Version 0.13.0: gemeinsame Straßenverläufe und Untergruppen

- Das normale Overlay zeichnet geplante Geraden und native Bézier-Kurven statt aller kurzen Terrain-Quellstücke. Die Vorschau verwendet dieselben Verläufe. Feine Quellstücke bleiben intern für die funktionierende Mausmarkierung verfügbar; nur markierte Stücke werden separat rot hervorgehoben.
- Gemeinsame Endpunkte werden auf dieselben Zentimeterkoordinaten normalisiert und beim nativen Bau gemeinsam behandelt. Neue OSM-Vorschauen speichern Einbahnrichtung, Brücken-, Tunnel- und Ebenenmerkmale. Zweirichtungsstraßen dürfen trotz umgekehrter OSM-Digitalisierungsrichtung zusammengeführt werden. Richtungs-, Ebenen-, Brücken-, Kategorie- und Untergruppenwechsel bleiben Grenzen. Alte Vorlagen ohne Richtungsmerkmale behalten konservativ ihre gespeicherte Linienrichtung.
- Native GenerateNodesSystem in der lokalen Game.dll prüft identische float3-Positionen. Diese Gleichheit wird nun für gemeinsame Anschlüsse hergestellt. Beim Andocken darf derselbe bestehende Knoten nicht zwei verschiedene Vorlagenpunkte verschlucken. Liegt ein Ende nahe einer kompatiblen vorhandenen Straße, wird deren CoursePos-Teilungspunkt verwendet; ein Höhenunterschied über 1 m verhindert dieses Andocken mitten auf der Straße. Brückenquerungen bleiben in getrennten Höhenplänen; Anschlussrampen treffen Brückenenden.
- Kleine Ansicht → Bereiche markieren → Als Untergruppe zuordnen → Name, Zielkategorie und Spielstraße wählen. Beispiel: Autobahnen / Auffahrt / einspurige Rampe. Untergruppen erscheinen unter ihrer Kategorie. Spielstraße ändern aktualisiert nur diese Untergruppe; Zuordnung aufheben gibt ihre Abschnitte an die ursprüngliche Kategorie zurück. Erneutes Zuordnen verschiebt die markierten Abschnitte aus bisherigen Untergruppen.
- Verbleibende Straßen bauen verarbeitet die Kategorie einschließlich ihrer Untergruppen mit dem jeweils zugeordneten Prefab. Die gemeinsame Bauprüfung bleibt bestehen. Nicht verfügbare benötigte Straßen verhindern einen unvollständigen Bau. Breiten, Vorschau und gespeicherte Offline-Versionen berücksichtigen Untergruppen. Ausschließen/Wiederherstellen bleibt verfügbar.
- 16 m bleibt eine Zielgröße; kurze Anschlüsse sind erlaubt. Das Spiel kann eigene Unterteilungen und Baugrenzen anwenden. Keine automatische Tunnelplanung. Ein Brückenstandard von 6,25 m ersetzt keine vollständige Mehrstockwerkplanung.

Prüfung: Release-Build ohne Warnungen/Fehler; bestehende Geometrie- und Datentests sowie neue Tests für Gegenrichtung, Einbahnrichtung, exakt identische T-Knoten, eigene Untergruppen-Prefabs, Kategorienwechsel, Offline-Roundtrip und Brückenquerung bestanden. Browserprüfung mit simulierten Spielbindungen prüft Anlegen, Ändern und Aufheben von Untergruppen. Der native Anschlussbau und die Darstellung mit Gameface sind im laufenden Spiel noch nicht bestätigt.

## Version 0.12.0: Bereiche zum Entfernen markieren

Die Mausauswahl markiert jetzt Straßen, die nicht gebaut werden sollen. In der kleinen Ansicht: Bereiche zum Entfernen markieren → Klick oder Rahmen → rote Markierung → Markierte entfernen. Gebaut werden alle verbleibenden Straßen der jeweiligen Kategorie. Solange eine Markierung offen ist, muss sie entfernt oder aufgehoben werden. Alt entfernt nur die Markierung. Entfernte wiederherstellen stellt sämtliche ausgeschlossenen Vorlagenabschnitte wieder her. Bereits gebaute Spielstraßen werden nicht verändert.

Ausschlüsse gelten für Spielkarten-Overlay, Vorschau und Bauplan und werden mit Offline-Versionen gespeichert. Neue Sammlungen starten ohne Ausschlüsse; zusätzliche Ausschnitte behalten bestehende Ausschlüsse. Die Quelldaten bleiben für die Wiederherstellung erhalten. Daten- und Browserprüfungen bestanden: verbleibende statt markierter Straßen bauen, leere Restvorlage, Wiederherstellung, Offline-Roundtrip, UI-Kommandos und Sperre bei offener Markierung. Laufzeitbedienung im Spiel noch zu prüfen.

## Version 0.11.1: kurze Anschlüsse zulassen

16 m ist eine Zielgröße beim Zusammenfassen, keine harte Baugrenze. Unvermeidbare kurze Anschlüsse bleiben im Bauplan und stoppen den Auftrag nicht mehr; auch nach dem Anschluss an vorhandene Knoten entfällt die 16-m-Sperre. Native Spielprüfungen gelten weiterhin. Lange Geraden, native Kurven und Maßstab bleiben unverändert. Tests für 10-m-Verbindungen zwischen Kreuzungen, 15-m-Reststücke sowie unvollständige Kreisringe bestanden.

## Version 0.11.0: längere Geraden und native Kurven

Der Stempel fasst gerade Ketten bis zum nächsten Anschluss zusammen, ohne die bisherige 120-m-Unterteilung. Kurven werden als kubische Bézier-Geometrie im nativen NetCourse übergeben, statt ausschließlich kurze gerade Straßenstücke zu bauen. Die Anpassung prüft Quellpunkte und Zwischenpunkte mit höchstens 25 cm Abweichung. Scharfe Ecken und Anschlussknoten bleiben erhalten. Der Maßstab wird nicht geändert.

Jeder übergebene Bauabschnitt muss mindestens 16 m lang sein, bei Kurven gemessen entlang des Bogens. Nach Anschluss an vorhandene Knoten wird die Länge erneut geprüft. Ein unvermeidbarer kürzerer Anschluss oder eine zu kleine Mausauswahl stoppt den Auftrag mit einem Hinweis; es werden weder Kreuzungen entfernt noch Straßen still ausgelassen. Die feinen Quellsegmente für Overlaydarstellung und Mausauswahl bleiben bestehen. Native Spielprüfungen für Steigung, Kollision und Baugröße gelten weiterhin.

Geprüft: 2-km-Gerade als ein Bauabschnitt, Viertelkreise in drei Maßstäben mit nativen Kontrollpunkten und Bogenlänge, scharfe Ecken, Anschlüsse anderer Kategorien, 16-m-Grenze und Auswahl. Gesamte Datentests bestanden; Release kompiliert gegen die lokalen Spielbibliotheken. Der tatsächliche Bau im laufenden Spiel wurde noch nicht überprüft.

## Version 0.10.0: gezielte Auswahl und Kartendetails

- Neu geladene Vorschauen filtern OSM-Zufahrten mit `highway=service` und `service=driveway` oder `parking_aisle`. Unmarkierte kurze Straßen bleiben erhalten; keine Längenheuristik löscht echte Sackgassen.
- In der kleinen Ansicht: „Auf Karte auswählen“, einzelne Overlay-Abschnitte anklicken oder einen Bildschirmrahmen ziehen. Klick schaltet um, Rahmen ergänzt, Alt entfernt. Weiß zeigt die Auswahl; die übrigen Linien werden gedimmt. „Auswahl bauen“ baut nur ausgewählte Abschnitte der jeweiligen Kategorie. „Alle verwenden“ stellt den bisherigen Gesamtbau wieder her. Während des Auswahlmodus ist die Kamera gesperrt; Rechtsklick oder „Auswahl beenden“ verlässt ihn.
- `bridge`-markierte Wege werden 6,25 m über dem Gelände angelegt; angrenzende Straßen teilen die erhöhten Endpunkte als Rampen. Keine automatische Tunnelplanung. Das Spiel prüft zulässige Steigungen und den gewählten Straßentyp.
- Vollständige, annähernd kreisförmige OSM-Kreisverkehre mit 4–30 m Radius und mindestens drei ausgewählten Anschlüssen werden in einen zentralen Straßenknoten mit nativem Small/Medium/Large/XL-Roundabout01-Objekt umgewandelt. Teilringe, fehlende Anschlüsse oder Verbindungen außerhalb der Bauauswahl bleiben Straßenringe. Die Spielgrößen sind diskret und entsprechen nicht immer exakt dem OSM-Radius.
- Ältere Offline-Vorlagen bleiben unverändert. Es gibt keine Aktualisierungsfunktion. Neue Kartendetails werden beim normalen Laden einer Vorschau aus den OSM-Daten (auch aus dem Cache) übernommen.
- Offline laden → Version auswählen → „Vorlage löschen“ → „Endgültig löschen“. Löscht nur diese gespeicherte Datei, einschließlich alter last-template.json. Andere Versionen und die aktuell geladene Vorlage bleiben erhalten. Keine bestehenden Nutzervorlagen wurden bei der Entwicklung gelöscht.
- Grenze: weiterhin 2.000 geplante Bauabschnitte pro Auftrag; bei Überschreitung kleinere Auswahl verwenden. Keine stille Teilplatzierung.

# City Street Overlay

Lokaler Prototyp für Cities: Skylines II: echte Städte direkt im Spiel suchen, deren Straßennetz als Vorlage anzeigen und ausgewählte Kategorien über einen expliziten Stempelknopf als echte Straßen bauen.

## Stand

- Version 0.9: „Ausgewählte Straße platzieren“ in jeder kleinen Kategorieansicht. Erstellt bodengeführte Straßenentwürfe über die native CreationDefinition/NetCourse-Pipeline und übergibt sie nur nach fehlerfreier Spielprüfung. Abbruch während der Prüfung, Statusanzeige, Schutz vor parallelen Aufträgen. Zusammenfassen gerader Vorlagensegmente erhält Knoten anderer Kategorien und die Linienrichtung. Anschluss an vorhandene kompatible Knoten bis 2 m; keine automatische Aufteilung bestehender Straßen an geometrischen Kreuzungen. Keine Brücken-/Tunnelhöhen in der bisherigen Vorlage. Maximal 2.000 Bauabschnitte pro Auftrag, ab Version 0.9.2 werden kurze Kurvenabschnitte mit maximal 25 cm Abweichung zusammengefasst. Anschlussknoten und Fahrtrichtung bleiben erhalten; unvermeidbare kurze Verbindungen prüft das Spiel. Laufzeitbau noch nicht geprüft; separate Teststadt verwenden. Minimieren und Schließen haben jetzt identische Abmessungen.

- Version 0.7: Vorschau folgt der Drehung und lässt sich mit linker Maustaste geografisch verschieben. Erst beim Loslassen wird geladen. Erfolgreich geladene Ausschnitte ergänzen die bestehende Sammlung auf der Spielkarte. Einheitliche Projektion am ersten Mittelpunkt, gemeinsame Platzierung und Überlappungsbeschnitt verhindern Versatz bzw. doppelte Linien in bereits abgedeckten Flächen. Offline-Versionen speichern alle Ausschnitte. Höchstens 24 Ausschnitte bzw. 320.000 Quellsegmente pro Sammlung; bei Fehlern bleibt der alte Stand bestehen. Der Suchbereich startet über „Neue Sammlung laden“ eine neue Sammlung. Die Vorschau zeigt jeweils den aktiven Ausschnitt, die Spielkarte die gesamte Sammlung.

- Version 0.6: Offline-Bibliothek. Jeder Speichervorgang erzeugt eine neue Version pro Stadt; Auswahl mit Datum und Vorschau über „Offline laden“. Benennung nach dem Ortsnamen der geladenen Karte, nicht einer später eingegebenen Suchanfrage. Vorhandene `last-template.json` bleibt unverändert als Version 1 verfügbar. Neue Dateien liegen unter `ModsData/CityStreetOverlay/Templates`; auch ein erneut gespeicherter älterer Stand erhält die nächste freie Versionsnummer. Ältere Vorlagen ohne gespeichertes Bild zeigen einen Hinweis und bleiben ladbar.

- Gegen die lokal vorhandenen Spielbibliotheken kompiliert.
- Stadtsuche, Straßendownload, rechteckiger Zuschnitt und metrische Projektion implementiert.
- Ingame-Fenster mit Straßenvorschau, Geländedarstellung, Position, Drehung, Maßstab, Deckkraft und Fixierung implementiert.
- Version 0.2: native Karten-Schaltfläche in der Mod-Leiste oben links; F8 und der separate Textknopf entfallen. Das Fenster startet geschlossen und lässt sich auch über × schließen.
- Version 0.3: vier farbige Straßentypen, einzeln in Vorschau und Overlay ein-/ausblendbar. Filter werden mit der Offline-Vorlage gespeichert.
- Version 0.4: pro Kategorie eine Spielstraße auswählen und deren tatsächliche Standardbreite übernehmen; geladene Mod-Straßen werden ebenfalls angeboten. Die Darstellung wird auf die Geländeoberfläche projiziert.
- Version 0.5: natives Spiel-UI statt Unity-Testfenster, zwei Spalten, größere Vorschau, aufklappbare Bereiche und veränderbare Fenstergröße. Eigene Kamera-/Werkzeug-Eingabesperren verhindern Eingaben auf der Karte, solange der Mauszeiger über dem Fenster liegt.
- Version 0.5.1: Zugriff auf TextInput/Slider korrigiert; eigene React-Fehlergrenze schützt die übrige Spieloberfläche bei Renderfehlern des Mod-Fensters.
- Version 0.5.2: gesamte Titelleiste als Verschiebefläche, feste Fenstergröße, nur ein Schließen-Knopf, eigenes Straßennetz-Icon und grafische Accordion-Pfeile ohne Schriftabhängigkeit.
- Eine Vorlage inklusive Platzierung kann offline gespeichert und geladen werden; Such- und Straßenantworten werden lokal zwischengespeichert.
- Der Nutzerscreenshot bestätigt die Straßendarstellung der ersten Version im Spiel. **Version 0.5 wurde kompiliert und im Browser mit simulierten Spielbindungen geprüft, noch nicht im laufenden Spiel.** Insbesondere Eingabesperren und das Layout in Gameface benötigen dort einen praktischen Test.

## Bauen und lokal installieren

PowerShell im Projektordner:

```powershell
dotnet build .\CityStreetOverlay.csproj -c Release
.\install-local.ps1
```

Bei anderem Spielpfad: `install-local.ps1 -GameManagedPath 'E:/SteamLibrary/steamapps/common/Cities Skylines II/Cities2_Data/Managed'`.

Das Installationsskript kopiert `CityStreetOverlay.dll`, `CityStreetOverlay.mjs` und `CityStreetOverlay.css` in den lokalen Mods-Ordner unter `%LOCALAPPDATA%Low/Colossal Order/Cities Skylines II/Mods/CityStreetOverlay`. Alle drei Dateien müssen nebeneinander liegen. Spielbibliotheken werden nicht mitgeliefert. Spiel zum Installieren schließen und danach neu starten.

## Bedienung

1. Eine Stadt im Spiel laden. In der **Mod-Leiste oben links das Karten-Symbol** mit dem Hinweis „Straßen-Vorlage · City Street Overlay“ anklicken. Der Knopf ist hervorgehoben, solange das Fenster geöffnet ist.
2. Im Bereich **Stadt & Ausschnitt** den Stadtnamen eingeben und **Suchen** klicken oder Enter drücken. Passenden Treffer auswählen.
3. Quadratgröße zwischen 0,5 und 10 km und bei Bedarf dessen Mittelpunkt einstellen. Zunächst 2 km oder weniger verwenden.
4. **Straßenvorschau laden** anklicken. Die Vorschau zeigt Straßen, Norden liegt oben. Änderungen am Ausschnitt werden erst mit erneutem Laden übernommen.
5. Unten **Auf Karte anzeigen** anklicken. Die Vorlage erscheint zunächst am Weltursprung der Spielkarte.
6. Unter **Platzierung & Darstellung** die Position über Regler und 10-m-Schritte anpassen, drehen und skalieren. Maßstab 1 bedeutet näherungsweise reale Meter. „Position fixieren“ verhindert Änderungen der Platzierung.
7. Unter **Straßentypen & Breiten** pro Kategorie eine Straße auswählen. Die gelesene Breite in Metern wird direkt angezeigt. Deckkraft unter Platzierung anpassen. Zum normalen Bauen Fenster über denselben Mod-Knopf oder **×** schließen; die Vorlage bleibt sichtbar.
8. Unten **Speichern** beziehungsweise **Offline laden** verwenden. Beim Laden bleibt die Anzeige zunächst ausgeschaltet, bis sie ausdrücklich aktiviert wird.

## Fenster und Mausbedienung

- Linke Spalte: quadratische Vorschau mit Nordpfeil, Größe und Farblegende. Die Vorschaugrafik hat 512 × 512 Pixel und passt sich der Fenstergröße an.
- Rechte Spalte: **Stadt & Ausschnitt**, **Straßentypen & Breiten**, **Platzierung & Darstellung** lassen sich über ihre Überschriften unabhängig auf-/zuklappen und scrollen.
- Die gesamte Titelleiste verschiebt das Fenster, außer dem Schließen-Knopf. Die feste Größe passt sich bei kleinen Bildschirmen automatisch an. Die Position bleibt beim Schließen und Wiederöffnen innerhalb derselben Spielsitzung erhalten, wird aber nicht dauerhaft gespeichert.
- Die untere Aktionsleiste bleibt sichtbar. Sie enthält Sichtbarkeit, Fixierung, Offline-Laden und Speichern.
- Über dem Fenster sperrt die Mod die Spieleingabekarten **Camera** und **Tool** über eigene `InputBarrier`-Instanzen. Dadurch sollen Klicks nicht Straßen setzen/Objekte auswählen und das Mausrad nicht die Spielkamera zoomen. Die nativen UI-Scrollbereiche bleiben bedienbar. Beim Ziehen gilt die Sperre bis zum Loslassen; außerhalb des Fensters sowie beim Schließen oder Entladen wird sie freigegeben.
- Die lokale Browserprüfung bestätigt Ereignisabgrenzung, Scrollen und Sperr-Anforderungen. Die endgültige Prüfung gegen Kamera und Bauwerkzeug muss im laufenden Spiel erfolgen.

## Vier Straßentypen

Im Bereich **Straßentypen & Breiten** stehen vier Schalter mit Farblegende. Sie wirken auf Vorschau und Spielkarte; dazu ist kein erneuter Download nötig. Alle vier sind zunächst eingeschaltet. Werden alle ausgeschaltet, bleibt die Vorlage geladen, aber ohne sichtbare Linien.

| Gruppe | Farbe | OSM-Kategorien |
| --- | --- | --- |
| Autobahnen | Orange | `motorway`, `motorway_link` |
| Hauptstraßen | Gelb | `trunk`, `primary`, `secondary`, `tertiary` und ihre `_link`-Anschlüsse |
| Wohn-/Nebenstraßen | Türkis | `residential`, `unclassified`, `living_street`, `service` |
| Fuß-/Radwege | Grün | `pedestrian`, `footway`, `cycleway`, `path`, `steps` |

Diese vier Gruppen sind die Darstellungsgruppen der Mod, basierend auf dem [OSM-highway-Tag](https://wiki.openstreetmap.org/wiki/Key:highway). Flächen mit `area=yes` werden nicht als Straßenlinien dargestellt. Geh- und Radwege erscheinen als eigene Linien, sofern sie als eigene Wege erfasst sind; seitliche Radstreifen auf einer Straße werden nicht separat gezeichnet.

Neue Vorschauen laden die Straßentypen automatisch mit. Ältere Offline-Vorlagen ohne Typdaten bleiben kompatibel und einfarbig türkis; bei ihnen wird keine Kategorie geraten. Es gibt keinen zusätzlichen Nachlade-Knopf. Neu geladene und gespeicherte Vorlagen behalten ihre vier Kategorien und die gewählten Filter auch offline.

## Spielstraßenbreite und Maßstab

Unter **Straßentypen & Breiten** gibt es für jede der vier Kategorien einen Auswahlknopf. Die aufklappbare Liste zeigt Namen und Breite der geladenen Straßen und Wege. Ein Suchfeld grenzt die Auswahl ein; **Alle Straßen und Wege** hebt die Vorauswahl nach Kategorie auf. So können auch besondere Straßen aus Mods zugeordnet werden.

- Ausgewählte Straße: die Mod liest ihre gesamte Standardbreite auf Bodenniveau aus `NetGeometryData.m_DefaultWidth`. Sie verwendet die tatsächlich geladene Variante, keine feste Tabelle vermeintlicher Standardbreiten. Nicht verfügbare DLC-Inhalte und Debug-Prefabs werden nicht angeboten. Die Auswahl schaltet keine Straßen im Spiel frei.
- Noch keine Auswahl: zunächst 3 m manuelle Linienbreite. Es wird nicht automatisch eine möglicherweise unpassende Straße zugeordnet. **Breite manuell einstellen** erlaubt eine eigene Breite pro Kategorie.
- **Reale Abstände 1:1**: geografische Abstände bleiben näherungsweise in echten Metern erhalten. Die lokale Projektion eignet sich für die kleinen Stadtausschnitte, ersetzt aber keine Vermessung. Eine neue Vorschau startet auf 1:1; beim Offline-Laden wird der gespeicherte Maßstab beibehalten.
- Der Maßstab verändert ausschließlich die Abstände und Ausdehnung des Netzes. Eine ausgewählte 16-m-Spielstraße bleibt auch bei Maßstab 0,5 oder 2 als 16-m-Streifen dargestellt.
- Die quadratische Vorschau berücksichtigt dieselben Breiten relativ zum Ausschnitt. Sehr schmale Linien erhalten dort mindestens einen Pixel zur Sichtbarkeit. Die Spielkarte verwendet die Breite in Metern.
- Zuordnung, Name und Breite werden mit der Vorlage gespeichert. Eine verfügbare Straße wird beim Laden/Öffnen wieder anhand ihrer Prefab-ID zugeordnet; bei geänderter Breite wird der neue Wert übernommen. Fehlt eine Mod-Straße, bleibt ihre gespeicherte Breite erhalten und ein Hinweis erscheint. Es wird nicht auf eine gleichnamige andere Straße gewechselt.
- Alte Vorlagen ohne Zuordnungen behalten ihre bisherige manuelle Breite. Bei alten einfarbigen Vorlagen gibt es entsprechend eine gemeinsame Straßenauswahl.

Breite bedeutet hier den normalen Straßenquerschnitt einschließlich der dort enthaltenen Seitenbereiche. Kreuzungsaufweitungen, Kurvengeometrie, Brücken, Tunnel und nachträgliche Straßen-Upgrades werden nicht exakt nachgebildet. Bei getrennten Autobahn-Mittellinien eine Spielstraße für **eine Fahrtrichtung** je Linie wählen. Die Vorlage baut weiterhin keine Straßen.

## Grenzen des Prototyps

- Die Vorschau enthält Straßenlinien, keine Hintergrundkarte und keine Satellitenbilder. Ausschnittwahl erfolgt über Größe und Mittelpunktregler; noch kein Ziehen eines Rechtecks auf einer interaktiven Karte.
- Natives Gameface-Fenster mit Kamera-/Werkzeugsperren; Verhalten bei Spielstandswechseln und mit weiteren UI-Mods muss im Spiel getestet werden.
- Eine aktive Vorlage, ein Speicherplatz, keine Zuordnung zu einzelnen Spielständen. Vor Stadtwechsel Vorlage ausblenden.
- Straßen werden auf die Geländeoberfläche projiziert. Brücken und Tunnel erhalten keine eigenen Höhen.
- Lokale Projektion ohne Datumsgrenzenüberquerung, bis maximal 80° Breite. Der Prototyp begrenzt Daten auf 80.000 Liniensegmente; große Ausschnitte können trotzdem die Bildrate beeinträchtigen.
- Geplante spätere Ausbaustufen: direktes Platzieren mit der Maus, mehrere gespeicherte Vorlagen, Kartenansicht für die Ausschnittwahl.

## Kartendienste und Daten

Stadtsuche verwendet Nominatim, Straßen werden über Overpass aus OpenStreetMap geladen. Daten: **© OpenStreetMap-Mitwirkende**, [ODbL / Attribution](https://www.openstreetmap.org/copyright).

**Öffentlicher Nominatim-Dienst: [Nutzungsregeln](https://operations.osmfoundation.org/policies/nominatim/).** Dieser lokale Prototyp sucht ausschließlich nach Klick, ohne Autovervollständigung, mit erkennbarem User-Agent, Cache und mindestens 1,1 Sekunden zwischen Anfragen. Das Limit von einer Anfrage pro Sekunde gilt für die gesamte Anwendung, nicht pro Nutzer. Vor einer öffentlichen Verteilung ist daher ein passender Anbieter beziehungsweise ein gemeinsamer Proxy mit globaler Begrenzung erforderlich.

Provider können ohne Neubuild in `services.json` im Datenordner geändert werden. Suchanbieter muss das Nominatim-JSONv2-Format anbieten, Straßenanbieter das Overpass-Format. Suchtexte und Ausschnittkoordinaten gehen an diese Dienste. Fehler oder Zeitlimits werden im Fenster angezeigt; keine automatischen Wiederholungen.

Datenordner: `%LOCALAPPDATA%Low/Colossal Order/Cities Skylines II/ModsData/CityStreetOverlay`. Dort liegen `services.json`, JSON-Caches und `last-template.json`. Cache-Dateien bleiben bis zur manuellen Entfernung erhalten, damit bekannte Anfragen offline nutzbar sind.

## Prüfungen

```powershell
dotnet build .\Tests\Tests.csproj
dotnet --fx-version 9.0.14 .\Tests\bin\Debug\net6.0\Tests.dll
# Optionaler Integrationstest mit zwei echten Kartendienst-Anfragen:
dotnet --fx-version 9.0.14 .\Tests\bin\Debug\net6.0\Tests.dll --online
```

Die 97 Prüfungen decken Projektion in Meter, Nordrichtung, deutsche Dezimalformate, Randzuschnitt einschließlich durchquerender Straßen, Gelände-Unterteilung, ungültige Bereiche, unvollständige Serverantworten, die vier Straßenklassen, alle 16 Filterkombinationen, neue/alte Offline-Vorlagen sowie Straßenbreiten und Zuordnungen ab. Der explizite Runtime-Aufruf nutzt die auf diesem Rechner verfügbare .NET-9-Runtime; das Spiel lädt die Mod selbst unter Unity.

Manuell im Spiel prüfen: Karten-Symbol in der Mod-Leiste finden, Fenster darüber öffnen/schließen, × testen und Hervorhebung kontrollieren. Stadt auswählen, Vorschau laden, Vorlage anzeigen und mit der Kamera aus mehreren Richtungen betrachten; verschieben/drehen/skalieren/fixieren; Sichtbarkeit und Transparenz ändern; speichern, Spiel neu starten und offline laden. Zusätzlich Hügel, Wasser, Hauptmenü/Stadtwechsel und Eingaben über dem Fenster prüfen.

Für Version 0.3 zusätzlich einen Ausschnitt mit Autobahn, Hauptstraße, Wohnstraße und Fuß-/Radweg laden. Farben in Vorschau und Overlay vergleichen, jeden Filter einzeln und alle gemeinsam ausschalten. Mit einer Teilmenge speichern und offline laden; ausgewählte Filter müssen erhalten bleiben.

Für Version 0.4 eine gerade Spielstraße als Vergleich wählen. Dieselbe Variante einer Kategorie zuordnen, den Vorlagenstreifen darüberlegen und Breite bei Draufsicht sowie schräger Kamera vergleichen. Den Maßstab ändern: Abstände müssen sich ändern, die Breite in Metern muss gleich bleiben. Auswahl speichern und wieder laden. Mit einer geladenen Mod-Straße und später ohne diese Mod die gespeicherte Breite und den Hinweis prüfen.

UI-Browserprüfung aus dem Workspace: `node CityStreetOverlay/Tests/ui-smoke.cjs`. Benötigt die lokalen Entwicklungsabhängigkeiten unter `.tools/ui-preview` (React 17, ReactDOM 17, playwright-core) und Microsoft Edge. Der Test lädt das echte MJS/CSS mit simulierten Spielkomponenten; er prüft Layout, Filter, Straßenauswahl, Fixierung, Suche, Verschieben, Größenänderung, Bildschirmgrenzen, Mausrad-Weitergabe und Freigabe der Sperre. Bilder unter `dist/window-preview.png` und `dist/window-compact.png` sind Browser-Testbilder mit synthetischen Straßendaten.

Ingame-Abnahme für Version 0.5: Bauwerkzeug aktivieren, über leeren Fensterflächen und Bedienelementen klicken; dahinter darf nichts gebaut/ausgewählt werden. Über Vorschau, Reglern und Listen scrollen; die Karte darf nicht zoomen, die Listen müssen scrollen. Danach den Mauszeiger außerhalb bewegen und Kamera/Bauwerkzeug testen. Fenster schließen, erneut öffnen, verschieben, vergrößern und am Rand loslassen; die Eingabesperren dürfen nicht hängenbleiben.

Technische Grundlagen: [offizieller Code-Modding-Überblick](https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding), [Nominatim-Suchformat](https://nominatim.org/release-docs/latest/api/Search/) und die lokal vorhandenen Spielassemblies.
