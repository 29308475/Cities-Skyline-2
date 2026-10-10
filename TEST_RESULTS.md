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

## Anschlussprüfung und neue Priorität (04.10.2026)

Nutzervorgabe: Korrekte Straßenverbindungen haben Vorrang vor exakter Overlay-Geometrie. Lokale Anpassungen an Anschlusswinkel, Abstand und Übergangslänge sind zulässig; grundsätzliche Straßenführung, Fahrtrichtung und Netzverbindungen müssen erhalten bleiben. Getrennte Richtungsfahrbahnen dürfen nicht durch Nähe unbeabsichtigt verbunden werden.

Letztes Bauprotokoll: 04.10.2026 11:56 UTC, 43 geplante Abschnitte. Alle 86 geplanten Endpunkte fanden einen temporären Spielknoten innerhalb 4 m; keine unterschiedlichen auf Zentimeter gerundeten Planpositionen wurden dabei demselben nächsten Knoten zugeordnet. Dies ist ein räumlicher Plausibilitätstest, kein Nachweis korrekter Fahrspuren. Die native NodeKey-Logik verknüpft über gleiche ursprüngliche Entity oder exakte Position; DisableMerge überspringt dagegen die räumliche Überlappungssuche gegen vorhandene Straßen. Ohne nachgewiesenen Fehlmerge wurde diese Sperre nicht aktiviert. Keine Änderung des Bauverhaltens in dieser Prüfung.

## 0.19.4

Gesamte .NET-Tests bestanden. Neue Tests: beide Seiten, Ein-/Ausfahrt, drei Maßstäbe, unveränderte Endpunkte/Quellkurven, nur anschlussnaher Kontrollpunkt, unveränderte Autobahn, kurze Anschlüsse. Release-Build ohne Fehler/Warnungen. Sichtbarer nativer Übergang und Fahrspurverbindungen zur äußeren Spur noch nicht ingame verifiziert.

## 0.19.3

.NET-Tests vollständig bestanden: drei gemeinsame Fahrspurmitten bei 3-auf-4-Übergang für beide Außenseiten, unterschiedliche/gleiche Gesamtbreite, Ablehnung inkompatibler Spurabstände, Kopieren/Speichern/Aktualisieren der Spurdaten. Native Datengrundlage anhand des installierten NetInitializeSystem geprüft. Release-Build ohne Warnungen/Fehler. Aktuelle Runtime-Prefabdaten und sichtbare Rampenübergänge erst nach Spielneustart prüfbar.

## 0.19.2

Gesamte .NET-Tests bestanden, darunter Nachbarfahrbahn trotz umgekehrter Linienrichtung, beide Verkehrsseiten, Rampenversatz mit unverändertem entfernten Anschluss. Release-Build ohne Fehler/Warnungen. Native Übergangsflächen und Spuranschlüsse ingame noch zu prüfen. Keine Behauptung einer vollständigen Behebung der Screenshot-Geometrie.

## 0.19.1

.NET-Tests bestanden: Komponenten statt Segmente, Kategorienanschlüsse, T-Schnittpunkt, Maßstab, geschlossene Ringe und 16-m-Grenze. Release-Build bestanden. Letztes lokales temporäres Bauprotokoll vom 04.10.2026 11:01 UTC enthielt keine mehrfachen Knoten-IDs an auf 2 Dezimalstellen identischen XYZ-Positionen nach Ausschluss gelöschter Kanten. Das bestätigt weder Fahrspurverbindungen noch nahe getrennte Knoten. Verbliebene Anschlussprobleme nicht als behoben erklärt; native Filterwirkung ingame noch zu prüfen.

## 0.19.0

Gesamte .NET-Tests bestanden: halbe Mehrbreite nach außen, Mittelstreifenkante bei gerader Fahrbahn, beide Verkehrsrichtungen, drei Maßstäbe, gemeinsame Rampen-/Übergangsknoten, isolierte Hervorhebung mit vollständigem Kontext und unveränderte Quellschlüssel. Vorhandene X-/Kreisverkehrtests bestanden. Release-Build ohne Warnungen/Fehler. Native Übergangsflächen und Verkehr ingame noch nicht geprüft.

## 0.18.9

Gesamte .NET-Tests bestanden, einschließlich Ausschluss zweispuriger Zweirichtungs-Fallbacks für Einbahnrampen und mehrdeutiger Kandidaten. Passende Kategorienzuordnung kann weiterhin Mehrdeutigkeit auflösen. Release-Build bestanden. Native Anschlussflächen und Verkehrsfluss nicht ingame verifiziert.

## 0.18.8

Gesamte .NET-Tests bestanden. Neuer Regressionstest führt Dreispurige Einbahnschnellstraße durch Namens-/Spurerkennung bis zur Standardzuordnung; Sondervarianten mit Namenszusätzen bleiben ausgeschlossen. Release-Build ohne Fehler/Warnungen. Ingame-Katalogauswahl noch zu bestätigen.

## 0.18.7

Gesamte .NET-Tests bestanden: Anschlussprofil hat Vorrang vor Untergruppen, gemeinsame Ring-Anschlüsse bei getrennten Kategorien und beiden Verkehrsrichtungen über drei Maßstäbe. Bestehende Grenzen an Zusammenführungen und Zweirichtungszufahrten unverändert geprüft. Release-Build bestanden; ingame noch zu prüfen.

## Zusätzliche Verbindungsprüfung zu 0.18.6

Gesamte .NET-Tests bestanden. Neue Regressionstests prüfen X-Kreuzungen über getrennte Kategorien bei Wechsel der Untergruppenbreite (8/32 m), Kreisringe mit vier getrennten Ein-/Ausfahrten, gemeinschaftliche exakte Anschlusskoordinaten, geschlossene Ringe, native Kurven, Untergruppen-Overrides und Links-/Rechtsverkehr bei Maßstab 0,5/1/2. Kurze nahezu gerade Ringstücke dürfen innerhalb der bestehenden Geometrietoleranz gerade bleiben. Bestehende Tests für T-Anschlüsse, Brücken-/Layer-Trennung, unvollständige Kreisringe und kurze notwendige Anschlüsse ebenfalls bestanden.

Letztes lokales Diagnoseprotokoll: 2026-10-03 18:50 UTC, Apply=true, 105 geplante Abschnitte, 125 temporäre Kanten einschließlich betroffener Bestandskanten. Das ist kein Nachweis für den endgültigen Verkehrsfluss. Kein neuer Laufzeitfehler durch diese Prüfung reproduziert, daher keine Änderung am installierten Baucode. Native Spielgeometrie und Verkehr weiterhin ingame prüfen.

## 0.18.6

UI-Smoke bestanden: direkte Untergruppenwahl sendet subgroupRoad mit Gruppen-ID, schließt Liste, zeigt aktualisierten Namen aus simuliertem Backend und verändert die Hauptkategorie nicht. Overlay-Fokus öffnet weiterhin die passende Untergruppe. Release-Build erfolgreich. Native Gameface-/Bauprüfung ingame noch ausstehend.

## 0.18.5

Release-Build ohne Warnungen/Fehler. Gesamte .NET-Tests bestanden, einschließlich Kategorieauswahl, Deckkrafterhalt, Schutz vorhandener/manueller Auswahl und fehlender geeigneter Kandidaten. Ingame noch nicht geprüft.

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

Automatisiert geprüft: Metadaten/Offline-Roundtrip, Zufahrtsfilter, Brückenübergänge, vollständige und teilweise ausgewählte Kreise, Quellenerhalt, Baugrenze und Teilmengenauswahl; Löschen isolierter Testdateien, Pfadvalidierung und Erhalt anderer Versionen. Browserprüfungen mit simulierten Spielbindungen: Löschbestätigung/Abbruch, Auswahlrahmen, normalisierte Koordinaten, Alt-Klick und Bedienbarkeit der Detailfenster über der Auswahlfläche. Nativer Bau von Brücken/Kreisverkehren und Kamera-Projektion im laufenden Spiel sind noch nicht bestätigt.

## Version 0.9.2: kurze Abschnitte und Kreisverkehre

- Gerichtete Ketten werden unabhängig von der Eingabereihenfolge zwischen Anschlussknoten gesammelt; geschlossene Ringe bleiben geschlossen.
- Vereinfachung bevorzugt Abschnitte ab 2 m, mit maximal 25 cm Abweichung im Spielmaßstab, maximal 120 m zusammengefasster Bogenlänge und ohne Rückwärtsbewegung. Knoten aller Kategorien bleiben erhalten.
- Nicht vereinfachbare kurze Verbindungen bleiben im Plan und durchlaufen die native Spielprüfung. Die pauschale Ablehnung unter 2 m entfällt. Keine stillschweigend ausgelassenen Straßen.
- Verhindert, dass beide Enden einer kurzen Verbindung an denselben vorhandenen Knoten geschnappt werden.
- Zusätzliche Tests: 120-teilige Kreisringe mit gemischter Reihenfolge, mit/ohne vier Zufahrten, drei Maßstäbe, Ringschluss, Richtung, Abweichung, Quellenerhalt und kurze Reststücke. Alle bestanden, ebenso bestehende Datenprüfungen.
- Release-Build ohne Warnungen/Fehler. UI/Overlaydarstellung unverändert; dies erzeugt keine nativen Kreisverkehr-Objekte. Laufzeitbau und Akzeptanz der gewählten Straßenbreiten im Spiel weiterhin zu prüfen.
## Version 0.9.1: Nullreferenz beim Start des Stempels

- Lokale Game.dll geprüft: TryGetPrefab<T> liefert bei gültigem Prefab-Index auch dann true, wenn die Typumwandlung null ergibt. Der Stempel dereferenzierte das Ergebnis ungeprüft.
- Stempelabfrage auf NetGeometryData begrenzt und Nullprüfung ergänzt; dieselbe Absicherung im Straßenkatalog.
- Fehler beim Stempelstart und bei der Bauprüfung werden mit Stacktrace im CityStreetOverlay-Protokoll erfasst.
- Sicherungskopien vom Kompilieren ausgeschlossen.
- Release-Build: keine Warnungen/Fehler. Daten-, Bibliotheks-, Mosaik- und Stempelplanprüfungen bestanden.
- UI unverändert. Die konkrete Laufzeitursache aus dem Screenshot ist mangels altem Stacktrace nicht abschließend bewiesen; erneuter Bauversuch im Spiel erforderlich.
# Prüfergebnis – zuletzt 2026-10-02

## Version 0.9: Straßenstempel

- Release-Build gegen lokale Spielassemblies bestanden. Native ToolBaseSystem/NetCourse/CreationDefinition-Schnittstellen lokal geprüft; native Validierung und Apply-Pipeline verwendet.
- Reine Planprüfungen für Kategorieauswahl, Richtungserhalt, gemeinsame Kreuzungspunkte, gerades Zusammenfassen und maximale Segmentlänge bestanden. Bestehende 97 Datenprüfungen, Bibliothek und Mosaikprüfungen bestanden.
- Browserprüfung für Bau-Button, Auswahl der Kategorie, Sperre bei ausgeblendeter Vorlage, parallele Auftragssperre, Abbruch und Ausrichtung beider Kopfbuttons bestanden.
- Keine Ingame-Baubestätigung: tatsächliche Kostenabbuchung, Netzgenerierung, Knotenanschlüsse, Kollisionen, Abbruch und Werkzeugwechsel müssen in separatem Spielstand geprüft werden. Der UI-Status meldet nach Apply bewusst nur die Übergabe an das Spiel, keinen verifizierten Bauerfolg.
- Keine automatische Brücken-/Tunnelplanung oder Aufteilung bestehender Straßen an deren inneren Kreuzungspunkten. Bis 2.000 Bauabschnitte; Abschnitte unter 2 m werden nicht stillschweigend übersprungen, sondern stoppen den Auftrag.

## Version 0.8: Kleine Kategorienfenster

- Minimieren ersetzt das Hauptfenster durch vier Karten am linken Rand. Gemeinsame Bindings für Straßenauswahl und Sichtbarkeit; eigene Deckkraft pro Kategorie wird mit der Gesamtdeckkraft multipliziert.
- Kategorie-Deckkraft liegt im gespeicherten Breitenprofil. Fehlendes Feld alter Vorlagen ergibt 100 Prozent; ungültige Werte werden zurückgewiesen. Straßenauswahl erhält den Wert.
- Browserprüfungen für vier Karten, linken Rand, Minimieren/Wiederherstellen, Filter, Deckkraft, Straßenauswahl und Schließen/Freigabe bestanden. JSON- und Validierungsprüfungen sowie bestehende Tests bestanden. Darstellung im echten Spiel noch zu bestätigen.

## Version 0.7: Interaktive geografische Erweiterung

- Geometrieprüfungen für nahtlose Meterprojektion, Nord-/Ostverschiebung, Überlappungsbeschnitt, Straßentypen, unveränderte Quellausschnitte und JSON-Rundlauf einer Sammlung bestanden.
- Browserprüfung für drehungsabhängiges Ziehen, Anfrage erst nach Loslassen, Sperre bei laufender Anfrage und Freigabe des Eingabeschilds bestanden. Bestehende UI- und Datenprüfungen weiterhin bestanden.
- Downloads und Zusammenfügen laufen im Hintergrund; die bestehende Sammlung wird erst nach Erfolg ersetzt. Ein gemeinsamer Transform gilt für alle Ausschnitte. Neue Offline-Versionen enthalten die gesamte Sammlung, ältere Dateien bleiben lesbar.
- Kein Live-Downloadtest und kein Gameface-Test dieser Version. Angrenzende Kartendaten und Terrain-Anschluss müssen nach Neustart im Spiel geprüft werden.

## Version 0.6: Offline-Bibliothek

- Neue Versionen je Stadt, unabhängige Zählung für andere Städte, Neustart, unveränderte alte Speicherstände und sichere Auswahl per ID geprüft. Alte Einzelvorlage bleibt unverändert und wird als Version 1 angeboten.
- Fehlerhafte Dateien werden in der Auswahl übersprungen und gemeldet. Speichern wird bei unlesbaren Dateien mit Hinweis abgebrochen, damit keine unsichere Versionsnummer vergeben wird.
- Browserprüfung: Auswahl verschiedener Versionen, Vorschau, explizites Laden und Rückkehr bestanden; bisherige UI-Prüfungen und 97 Datenprüfungen ebenfalls bestanden. Bibliotheksansicht visuell geprüft. Laden in Gameface nach Neustart noch zu bestätigen.

## Version 0.5.8: Genaue Drehung und Fensterhöhe

- Drehung als Eingabe mit einer Nachkommastelle, Komma oder Punkt, Übernahme bei Enter/Blur; ungültige Werte werden verworfen, Werte auf ±180° begrenzt. Slider bleibt ganzzahlig und Positionssperre gilt auch für Eingabe. Backend-Standard weiterhin 0.
- Fensterhöhe orientiert sich am Bildschirm mit 130 Pixeln Platz für die untere Spielleiste; Mindesthöhe wird auf den Bildschirm begrenzt. Mehr Platz unter der Legende.
- Browserprüfungen für Dezimaleingabe, Blur, ungültigen Text, Begrenzung, ganzzahligen Slider und Sperre bestanden, ebenso bisherige UI-Prüfungen. Ingame-Bestätigung steht aus.

## Version 0.5.6: Trefferlisten und Lesbarkeit

- Stadttreffer bis vier Einträge ohne Scrollcontainer und mit exakt passender Höhe; ab fünf Einträgen begrenzte verschachtelte Scrollliste. Doppelte Anzeige des ausgewählten Treffers entfernt.
- Schrift vergrößert, Stadt- und Straßenbereiche verdichtet, Erklärung zur Straßenbreite als Tooltip erhalten. Häkchen und Untermenüpfeile als CSS-Geometrie statt Schriftzeichen.
- Browserprüfung mit einem, vier und fünf Treffern bestanden. Alle Accordion-Titel bleiben bei normaler Fenstergröße sichtbar, Stadt- und Straßenübersicht passen ohne äußeres Scrollen. Kleine Platzierungsansicht, Scrollisolation und Fehlerabfangung weiterhin geprüft. Bilder visuell geprüft; Gameface-Bestätigung steht aus.

## Version 0.5.5: Kompakte Platzierung

- Platzierungsregler in zwei Spalten mit kompakten Abständen, 1:1-Knopf neben Deckkraft und Erklärung als Tooltip.
- Accordion öffnet jeweils einen Bereich. Alle Funktionen und die Positionssperre bleiben erhalten.
- Browserprüfung bestätigt Platzierung ohne Scrollbedarf bei 1440×1000 und 800×600, dort auch mit Sperrhinweis. Gesamte UI-Regressionsprüfung bestanden. Gameface-Darstellung im Spiel noch zu bestätigen.

## Version 0.5.4: Verschachtelte Scrolllisten

- Native Wheel-Ereignisse werden nach der Verarbeitung durch die innere Spiel-Scrollable abgefangen, bevor die äußere Scrollable sie erhält. Gilt für Straßenliste und Stadtsuchergebnisse, einschließlich beider Listenenden.
- Browser-Mock an native Wheel-Listener des lokal installierten Spiels angepasst. Regressionstest prüft innere Bewegung bei unveränderter äußerer Position sowie beide Listenenden. Gesamte UI-Prüfung bestanden; Bestätigung im Spiel steht aus.

## Version 0.5.3: Abstände und Icon

- Nutzerscreenshot bestätigt funktionierendes Fenster und Accordion-Pfeile; SVG-Icon bleibt im Spiel leer.
- SVG-Daten-URL durch CSS-Geometrie für Toolbar und Fenstertitel ersetzt. Keine Bildressource oder Schriftzeichen nötig.
- Vorschau-Spalte auf die quadratische Vorschau abgestimmt: keine breiten seitlichen Leerflächen, Hinweis und Überschrift linksbündig.
- Browserprüfung bestanden, einschließlich bündiger Kanten, Icon-Elementen, Verschieben, Eingabesperren und Fehlerabfangung. Desktop-Vorschau visuell geprüft. Darstellung im Spiel nach Neustart noch zu bestätigen.

## Version 0.5.1: Korrektur des GUI-Ausfalls

- Spielerbericht und Player.log: Beim ?ffnen unter 0.5 verschwindet die gesamte GUI; React-Fehler #130 mit Komponententyp undefined.
- Ursache: TextInput und Slider sind keine Exporte von cs2/ui. Zugriff auf die registrierten Spielmodule ?ber cs2/modding.getModule korrigiert.
- Browser-Mocks entsprechend eingeschr?nkt. Der Test pr?ft zus?tzlich die ?ffentliche Exporttabelle und die verwendeten Modulregistrierungen im lokal installierten Spiel-Bundle.
- React-Fehlergrenze begrenzt Renderfehler auf das Mod-Fenster, schlie?t es und l?st die Eingabesperre. Gezielter Fehler mit fehlendem Modul: Toolbar/Kartenmodell bleiben bedienbar; erneutes ?ffnen nach Fehlerbehebung funktioniert.
- UI-Regressionspr?fung und Release-Build bestanden. Best?tigung im laufenden Spiel nach Neustart steht aus.

## Version 0.5: Natives Fenster und Eingabesperren

- Release-Build: keine Warnungen oder Fehler. Die bisherigen 97 Geografie-/Filter-/Breitenprüfungen sind bestanden.
- Browserprüfung mit echtem MJS/CSS und simulierten Spielbindungen in Headless Edge bestanden: Darstellung, vier Filter, Straßensuche/-auswahl, Fixierung, Verschieben, Größenänderung, Bildschirmbegrenzung, Schließen/Wiederöffnen, Stadtsuche, Ladezustand.
- Maustests bestanden: Klicks im Fenster erreichen das simulierte Kartenmodell nicht; der Kontrollbereich scrollt und das Mausrad wird nicht an das Kartenmodell weitergereicht. Außerhalb bleibt das Kartenmodell klickbar. Hover aktiviert die Sperr-Anforderung; Verlassen/Schließen gibt sie frei. Ziehen endet auch beim Loslassen innerhalb des Fensters.
- Layout bei 1440 × 1000 und 800 × 600 geprüft, einschließlich sichtbarer Farblegende und Aktionsleiste. Browser-Testbilder liegen unter `dist/window-preview.png` und `dist/window-compact.png`; ihre Straßendaten sind synthetisch.
- Backend verwendet eigene `InputManager.CreateMapBarrier("Camera", ...)` und `CreateMapBarrier("Tool", ...)`, entsprechend den in der lokalen `Game.UI.InputBindings` verwendeten Spiel-Schnittstellen. Sperren werden beim Schließen und beim Mod-Entladen freigegeben; die Barrieren beim Zerstören des UI-Systems entsorgt.
- Noch nicht bestätigt: Darstellung mit den echten Gameface-Komponenten sowie effektive Kamera-/Werkzeugsperre im laufenden Spiel. Browser-Mocks ersetzen diesen Test nicht.

## Version 0.4: Spielstraßenbreiten und Maßstab

- Release-Build gegen die lokalen Spielbibliotheken: keine Warnungen oder Fehler.
- 97 Prüfungen bestanden. Zusätzliche Fälle: unabhängige Breiten pro Kategorie, volle statt halbe Straßenbreite, Zuordnung per Prefab-ID, Aktualisierung durch vorhandene Prefabs, unveränderte Kataloge, fehlende/gleichnamige andere Straßen, ungültige Breiten, JSON-Speicherung und Maßstäbe 0,5 / 1 / 2.
- Lokale API geprüft: RoadPrefab/PathwayPrefab, PrefabSystem.TryGetPrefab und IsAvailable, UIObject, NetGeometryData.m_DefaultWidth, OverlayRenderSystem.Buffer.DrawLine mit StyleFlags.Projected.
- Die Straßenbreite kommt aus dem geladenen Prefab; keine fest verdrahteten Breiten nach Straßennamen. Die Projektion verwendet Weltmeter und keinen kamerazugewandten Streifen.
- Die Vorschau ist quadratisch und stellt die relative Straßenbreite dar (Minimum ein Pixel). Der Maßstab verändert die Ausdehnung, nicht die gewählte Straßenbreite.
- Noch offen: Laufzeitprüfung des Katalogs mit Vanilla- und Mod-Straßen sowie visueller Breitenvergleich auf der Spielkarte, einschließlich Kameradrehung und Gelände. Die Offline-Tests bestätigen nicht die konkrete Darstellung im laufenden Spiel.

## Version 0.3: Vier Straßentypen

- Release-Build gegen die lokalen Spielbibliotheken: keine Warnungen oder Fehler.
- 77 Prüfungen bestanden: bisherige Geografieprüfungen plus Klassifizierung aller 19 unterstützten OSM-Wegtypen, Abdeckung durch die Download-Abfrage, Erhalt der Kategorie beim Unterteilen, Ausschluss von Flächen/ungeeigneten Typen, alle 16 Filterkombinationen, JSON-Rundlauf von Kategorien/Filtern/Platzierung und Kompatibilität mit alten Vorlagen.
- Fehlerhafte Metadaten (falsche Anzahl oder ungültige Kategorie) werden beim Laden abgelehnt.
- Vorschau, Overlay und Farblegende verwenden dieselbe Palette. Beide Darstellungen nutzen dieselbe Filterlogik.
- Farben und Filter noch nicht im laufenden Spiel geprüft. Kein erneuter externer Downloadtest; der Download wurde um Fuß-/Radwege erweitert und erhält dadurch einen neuen Cache-Schlüssel.
- Kein zusätzlicher Nachlade-Knopf; neue Vorschauen enthalten die Kategorien, ältere Vorlagen bleiben einfarbig lesbar.

## Version 0.2: Mod-Leiste

- Native Registrierung über `GameTopLeft` und Button/Tooltip aus `cs2/ui`, entsprechend den lokal installierten UI-Modulen und der lokalen Spieloberfläche.
- C#-Bindings für Öffnen/Schließen und den Auswahlzustand gegen die vorhandenen Spielbibliotheken kompiliert: keine Warnungen oder Fehler.
- JavaScript-Syntaxprüfung mit `node --check`: bestanden.
- Release enthält DLL und MJS nebeneinander. Lokale Installation und neues ZIP auf Version 0.2 aktualisiert.
- Der Nutzerscreenshot bestätigt die Darstellung des Straßen-Overlays der ersten Version. Die neue Toolbar-Schaltfläche und ihr Klickverhalten wurden noch nicht im laufenden Spiel geprüft.

## Ursprünglicher Stand Version 0.1

- Release-Build gegen die lokale Cities-Skylines-II-Installation: erfolgreich, keine Warnungen oder Fehler.
- 14 Offline-Prüfungen für Projektion, Zuschnitt, Zahlenformate und Datenfehler: bestanden.
- Echte Nominatim-Stadtsuche nach Berlin: erfolgreich.
- Echter Overpass-Straßendownload eines 500-m-Ausschnitts: zweimal HTTP 504 vom Dienst. Der vollständige Online-Ablauf ist deshalb noch nicht bestätigt. Die Offline-Prüfungen verwenden eine kleine synthetische Straßenantwort, keine echten geladenen Straßendaten.
- Ingame-Laden, Fenster, Overlay-Rendering, Spielsteuerung, Ladewechsel und Bildrate: noch nicht getestet.

Die Spielsignaturen für TerrainSystem.GetHeightData, TerrainUtils.SampleHeight und OverlayRenderSystem.Buffer.DrawLine wurden in der lokalen Game.dll überprüft. Eine erfolgreiche Kompilierung ersetzt keinen Laufzeittest im Spiel.
