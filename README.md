# 3MF-Explorer

Repository: https://github.com/Marabo75/3MF-Explorer

3MF-Explorer ist ein Windows-Viewer und eine Modellbibliothek für Bambu-Studio-3MF-Dateien.

Das Programm zeigt 3MF-Modelle übersichtlich als Galerie mit den in den Dateien enthaltenen Vorschaubildern an und stellt zusätzliche Druck- und Dateiinformationen bereit.

## Funktionen

### 3MF-Galerie

- Ordner auswählen und 3MF-Dateien als Thumbnail-Galerie anzeigen
- Echte Vorschaubilder direkt aus 3MF-Dateien extrahieren
- Große Vorschau des ausgewählten Modells
- Datei per Doppelklick oder `Enter` öffnen
- Galerie nach Dateinamen durchsuchen
- Dateien nach Name, Änderungsdatum oder Dateigröße sortieren
- Thumbnail-Größe zwischen Klein, Mittel und Groß umschalten
- Gewählte Thumbnail-Größe automatisch speichern
- Thumbnail-Cache für eine schnellere Anzeige

### 2D- und 3D-Vorschau

3MF-Explorer kann ein ausgewähltes 3MF-Modell nicht nur als gespeichertes Vorschaubild, sondern auch interaktiv in 3D darstellen.

- Zwischen 2D- und 3D-Ansicht umschalten
- Große 2D-/3D-Vorschau in einem separaten Fenster öffnen
- 3D-Modell mit der linken Maustaste drehen
- Mit dem Mausrad zoomen
- Mit der rechten Maustaste die Ansicht verschieben
- 3D-Ansicht per Doppelklick zurücksetzen
- Feste Kameraansichten: Isometrisch, Oben, Vorne, Hinten, Links und Rechts
- Mehrteilige 3MF-Modelle an ihren in der Datei gespeicherten Positionen darstellen
- Einzelne Objekte direkt in der 3D-Ansicht auswählen und hervorheben
- Objekte ein-/ausblenden, isolieren und fokussieren
- Dreiecksanzahl sowie Größe und Position einzelner Objekte anzeigen
- Bambu-Studio-Projekte mit mehreren Druckplatten erkennen
- Zwischen einzelnen Projektplatten wechseln
- Objektlisten automatisch auf die aktive Projektplatte filtern
- Gespeicherte Multi-Plate-Koordinaten auf lokale Plattenkoordinaten normalisieren

### Drucker, Druckplatte und Bauraum

Für die 3D-Prüfung kann der Benutzer seinen eigenen Drucker auswählen. Die Auswahl wird gespeichert und nicht automatisch aus der geöffneten 3MF-Datei übernommen.

- Druckerprofil über das Druckersymbol auswählen
- Druckerauswahl bleibt nach einem Neustart erhalten
- Maßstäbliche Druckplatte des gewählten Druckers anzeigen
- Druckplatte mit Raster und strukturierter Oberfläche
- Druckplatte ein- oder ausblenden
- Modellgröße und tatsächliche Position gegen den Bauraum prüfen
- Prüfung der X-, Y- und Z-Grenzen
- Visuelle Warnung, wenn das Modell den Bauraum überschreitet
- Bauraumprüfung zusätzlich pro Objekt und pro aktiver Projektplatte
- Z-Grenze passend zur Bauhöhe des ausgewählten Druckers prüfen
- Quell-Drucker und ursprüngliche Projektplattengröße aus kompatiblen Bambu-3MF-Dateien anzeigen
- Zwischen Ziel-Druckplatte und ursprünglicher Projektplatte umschalten
- Quelllayout auf größeren Ziel-Druckplatten zentriert darstellen, ohne einzelne Objekte neu anzuordnen

### Favoriten

Ab Version 1.10.0 können Modelle als Favoriten markiert werden.

- `☆` kennzeichnet ein normales Modell
- `★` kennzeichnet ein favorisiertes Modell
- Favoriten können direkt über den Stern am Thumbnail gesetzt oder entfernt werden
- Favoriten bleiben nach einem Neustart von 3MF-Explorer erhalten
- Die ursprünglichen 3MF-Dateien werden durch die Favoritenfunktion nicht verändert
- Favoriten können auch über das Kontextmenü verwaltet werden
- `Strg+D` ändert den Favoritenstatus der aktuellen Auswahl
- Die Favoritenfunktion unterstützt auch die Mehrfachauswahl

### Favoritenfilter

Über **„Nur Favoriten“** kann die Galerie auf favorisierte Modelle beschränkt werden.

Der Favoritenfilter kann mit den anderen Galeriefunktionen kombiniert werden:

- Nur Favoriten anzeigen
- Favoriten nach Dateinamen durchsuchen
- Favoriten nach Name, Datum oder Dateigröße sortieren
- Gefilterte Favoriten mit `Strg+A` auswählen

Dadurch können auch größere 3MF-Sammlungen schnell auf häufig verwendete oder besonders interessante Modelle reduziert werden.

### Mehrfachauswahl

Mehrere Modelle können gleichzeitig ausgewählt werden.

- Normaler Klick wählt ein einzelnes Modell aus
- `Strg + Klick` fügt einzelne Modelle zur Auswahl hinzu oder entfernt sie
- `Shift + Klick` wählt einen zusammenhängenden Bereich aus
- `Strg+A` wählt alle momentan in der Galerie angezeigten Modelle aus
- Alle ausgewählten Modelle werden gleichzeitig hervorgehoben
- Die Statusleiste zeigt die Anzahl der ausgewählten Dateien
- Das zuletzt angeklickte Modell bleibt rechts in der Vorschau sichtbar

Die Mehrfachauswahl berücksichtigt die aktuell angezeigte Galerie. Dadurch kann beispielsweise zuerst über die Suche oder den Favoritenfilter gefiltert und anschließend mit `Strg+A` die sichtbare Auswahl markiert werden.

### Ordnernavigation

- Navigation über Laufwerke und Verzeichnisbaum
- Einfacher Wechsel zwischen verschiedenen Modellordnern
- Zuletzt verwendeten Ordner automatisch speichern
- Zuletzt verwendeten Ordner beim nächsten Programmstart wiederherstellen

### 3MF- und Druckinformationen

Bei kompatiblen Bambu-Studio-3MF-Dateien können zusätzliche Informationen angezeigt werden:

- Druckzeit
- Material
- Filamentgewicht
- Filamentlänge
- Düsengröße
- Druckprofil
- Druckplatte
- Bambu-Studio-Version
- Support-Informationen
- Dateigröße
- Änderungsdatum

Welche Informationen verfügbar sind, hängt vom Inhalt der jeweiligen 3MF-Datei ab.

Bei einer Mehrfachauswahl zeigt der Vorschaubereich weiterhin die Informationen des zuletzt angeklickten Modells.

### Dateiverwaltung

Über das Kontextmenü oder verschiedene Tastenkürzel können 3MF-Dateien direkt aus 3MF-Explorer verwaltet werden.

- Datei öffnen
- Datei im Windows-Explorer anzeigen
- Vollständigen Dateipfad kopieren
- Datei umbenennen
- Datei in den Windows-Papierkorb verschieben
- Mehrere ausgewählte Dateien gemeinsam in den Papierkorb verschieben
- Dateipfade mehrerer ausgewählter Dateien gemeinsam kopieren
- Modelle als Favoriten markieren
- Favoriten wieder entfernen

Beim Verschieben mehrerer Dateien in den Papierkorb erfolgt eine gemeinsame Sicherheitsabfrage.

Die Dateien werden dabei nicht direkt endgültig gelöscht und können normalerweise über den Windows-Papierkorb wiederhergestellt werden.

Das Umbenennen ist auf einzelne Dateien beschränkt.

### Tastenkürzel

| Taste | Funktion |
|---|---|
| `Enter` | Ausgewählte 3MF-Datei öffnen |
| `F2` | Einzelne ausgewählte Datei umbenennen |
| `Entf` | Ausgewählte Datei(en) in den Papierkorb verschieben |
| `Strg+C` | Dateipfad bzw. ausgewählte Dateipfade kopieren |
| `Strg+D` | Favoritenstatus der ausgewählten Datei(en) ändern |
| `Strg+A` | Alle momentan angezeigten Modelle auswählen |
| `Strg + Klick` | Modell zur Auswahl hinzufügen oder daraus entfernen |
| `Shift + Klick` | Zusammenhängenden Bereich auswählen |

Bei mehreren ausgewählten Dateien kopiert `Strg+C` die vollständigen Dateipfade zeilenweise in die Zwischenablage.

`Strg+D` kann auch bei einer Mehrfachauswahl verwendet werden.

### Integrierte Bedienungsanleitung

3MF-Explorer besitzt eine integrierte Bedienungsanleitung.

Über **„? Hilfe“** kann direkt im Programm ein eigenes Hilfefenster geöffnet werden.

Die Bedienungsanleitung enthält:

- Erste Schritte
- Ordner und Navigation
- Galerie
- Einzel- und Mehrfachauswahl
- Favoriten und Favoritenfilter
- Vorschau, 2D-/3D-Ansicht, Druckplatte und Bauraum
- Dateiverwaltung
- Tastenkürzel
- Updatefunktion
- Informationen über 3MF-Explorer

Die Hilfe funktioniert vollständig offline und ist optisch in das 3MF-Explorer-Dark-Mode-Design integriert.

### Updates

3MF-Explorer besitzt eine integrierte Updateprüfung über GitHub Releases.

Wenn eine neuere Version verfügbar ist, wird dies in der Statusleiste angezeigt. Das Update kann anschließend direkt aus 3MF-Explorer heruntergeladen und installiert werden.

Für die integrierte Updatefunktion muss der GitHub-Release das Asset `3MF-Explorer.zip` enthalten. Die veröffentlichten Programmdateien müssen direkt im Stammverzeichnis dieser ZIP liegen.

Nach erfolgreicher Aktualisierung wird 3MF-Explorer mit der neuen Version gestartet.

Persönliche Einstellungen und Favoriten werden unabhängig von den Programmdateien gespeichert und bleiben bei einem normalen Update erhalten.

### Benutzeroberfläche

- Modernes Dark-Mode-Design
- Eigene dunkle Windows-Titelleiste
- Eigene Schaltflächen für Minimieren, Maximieren und Schließen
- Dunkle Scrollbalken
- Dunkler Umbenennen-Dialog
- Dunkles Hilfe-Fenster
- Hervorhebung aller ausgewählten Modelle
- Favoriten-Stern direkt am Thumbnail
- Favoritenfilter in der Galerie
- Statusleiste mit Auswahl-, Versions-, Update- und Bedieninformationen
- Deutsche und englische Benutzeroberfläche
- Zweisprachige integrierte Bedienungsanleitung

## Systemanforderungen

- Windows
- .NET 10 Desktop Runtime
- WPF

## Version

**3MF-Explorer 1.12.1**

Die Änderungen der einzelnen Versionen befinden sich in `CHANGELOG.md`.