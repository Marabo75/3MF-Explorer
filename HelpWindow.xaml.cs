using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThreeMFExplorer
{
    public partial class HelpWindow : Window
    {
        private string _currentPage = "Start";

        public HelpWindow()
        {
            InitializeComponent();

            Title = Localization.Get("HelpTitle");

            TxtHelpVersion.Text =
                $"3MF-Explorer {VersionInfo.Version}";

            ApplyLanguage();
            ShowHelpPage("Start");
        }

        private void ApplyLanguage()
        {
            Title = Localization.Get("HelpTitle");
            TxtWindowTitle.Text = Localization.Get("HelpTitle");
            TxtGuideSubtitle.Text = Localization.Get("UserGuide");
            BtnClose.ToolTip = Localization.Get("Close");

            BtnNavStart.Content = Localization.Get("GettingStarted");
            BtnNavFolders.Content = Localization.Get("FoldersNavigation");
            BtnNavGallery.Content = Localization.Get("Gallery");
            BtnNavPreview.Content = Localization.Get("PreviewPrintData");
            BtnNavFiles.Content = Localization.Get("ManageFiles");
            BtnNavKeyboard.Content = Localization.Get("KeyboardShortcuts");
            BtnNavUpdates.Content = Localization.Get("Updates");
            BtnNavAbout.Content = Localization.Get("About");
        }

        private void Navigation_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not string page)
            {
                return;
            }

            ShowHelpPage(page);
        }

        private void ShowHelpPage(string page)
        {
            _currentPage = page;

            switch (page)
            {
                case "Folders":
                    ShowFolders();
                    break;

                case "Gallery":
                    ShowGallery();
                    break;

                case "Preview":
                    ShowPreview();
                    break;

                case "Files":
                    ShowFiles();
                    break;

                case "Keyboard":
                    ShowKeyboard();
                    break;

                case "Updates":
                    ShowUpdates();
                    break;

                case "About":
                    ShowAbout();
                    break;

                default:
                    ShowStart();
                    break;
            }
        }

        private void ShowStart()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Erste Schritte";
                TxtHelpSubtitle.Text = "Willkommen bei 3MF-Explorer";
                TxtHelpContent.Text =
                    """
                    3MF-Explorer ist ein Viewer und eine Modellbibliothek für 3MF-Dateien.

                    Nach dem Start kannst du links einen Ordner auswählen, der deine 3MF-Dateien enthält. 3MF-Explorer durchsucht den ausgewählten Ordner und zeigt die gefundenen Modelle als Galerie an.

                    1. Klicke links auf „Ordner auswählen“.

                    2. Wähle den Ordner mit deinen 3MF-Dateien aus.

                    3. Die gefundenen Modelle erscheinen automatisch in der Galerie.

                    4. Klicke auf ein Modell, um rechts die Vorschau und die verfügbaren Druckinformationen anzuzeigen.

                    5. Mit 2D und 3D kannst du zwischen dem gespeicherten Vorschaubild und der interaktiven 3D-Darstellung wechseln.

                    6. Mit einem Doppelklick auf ein Thumbnail oder mit Enter kannst du die ausgewählte 3MF-Datei mit der in Windows zugeordneten Anwendung öffnen.

                    Mehrere Modelle können mit Strg + Klick oder Shift + Klick gleichzeitig ausgewählt werden.

                    Mit Strg+A werden alle momentan in der Galerie angezeigten Modelle ausgewählt.

                    Modelle können über den Stern am Thumbnail als Favoriten markiert werden. Favoriten bleiben auch nach einem Neustart von 3MF-Explorer erhalten.

                    Der zuletzt verwendete Ordner und verschiedene Programmeinstellungen werden gespeichert und beim nächsten Start wiederhergestellt.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Getting Started";
                TxtHelpSubtitle.Text = "Welcome to 3MF-Explorer";
                TxtHelpContent.Text =
                    """
                    3MF-Explorer is a viewer and model library for 3MF files.

                    After starting the program, select a folder containing your 3MF files on the left. 3MF-Explorer scans the selected folder and displays the models in a gallery.

                    1. Click “Select folder” on the left.

                    2. Select the folder containing your 3MF files.

                    3. The models found are displayed automatically in the gallery.

                    4. Click a model to display its preview and available print information on the right.

                    5. Use 2D and 3D to switch between the stored preview image and the interactive 3D view.

                    6. Double-click a thumbnail or press Enter to open the selected 3MF file with the application associated with it in Windows.

                    Multiple models can be selected with Ctrl + click or Shift + click.

                    Ctrl+A selects all models currently displayed in the gallery.

                    Models can be marked as favorites using the star on the thumbnail. Favorites remain saved after restarting 3MF-Explorer.

                    The last used folder and various application settings are saved and restored the next time 3MF-Explorer starts.
                    """;
            }
        }

        private void ShowFolders()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Ordner & Navigation";
                TxtHelpSubtitle.Text = "3MF-Sammlungen auswählen und durchsuchen";
                TxtHelpContent.Text =
                    """
                    Auf der linken Seite befindet sich die Ordnernavigation.

                    ORDNER AUSWÄHLEN

                    Über „Ordner auswählen“ kannst du direkt einen Ordner auf deinem Computer auswählen.

                    ORDNERBAUM

                    Unterhalb des ausgewählten Pfads befindet sich der Verzeichnisbaum. Dort kannst du Laufwerke und Unterordner öffnen und direkt zwischen verschiedenen Modellordnern wechseln.

                    ZULETZT VERWENDETER ORDNER

                    3MF-Explorer merkt sich den zuletzt verwendeten Ordner. Beim nächsten Programmstart wird dieser automatisch wieder geladen, sofern der Ordner weiterhin verfügbar ist.

                    Die Galerie zeigt die 3MF-Dateien des aktuell ausgewählten Ordners an.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Folders & Navigation";
                TxtHelpSubtitle.Text = "Select and browse 3MF collections";
                TxtHelpContent.Text =
                    """
                    The folder navigation is located on the left side of the main window.

                    SELECT FOLDER

                    “Select folder” lets you choose a folder on your computer directly.

                    FOLDER TREE

                    The directory tree is shown below the selected path. You can expand drives and subfolders and switch directly between different model folders.

                    LAST USED FOLDER

                    3MF-Explorer remembers the last folder you used. It is loaded automatically the next time the program starts, provided the folder is still available.

                    The gallery displays the 3MF files in the currently selected folder.
                    """;
            }
        }

        private void ShowGallery()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Galerie";
                TxtHelpSubtitle.Text = "Modelle suchen, sortieren, auswählen und favorisieren";
                TxtHelpContent.Text =
                    """
                    Die Galerie zeigt die in einem Ordner gefundenen 3MF-Dateien mit ihren Vorschaubildern.

                    SUCHE

                    Über das Suchfeld kannst du die angezeigten Modelle nach Dateinamen filtern.

                    SORTIERUNG

                    Die Galerie kann nach Name, Änderungsdatum oder Dateigröße sortiert werden.

                    ANSICHT

                    Über „Ansicht“ kannst du zwischen den Thumbnail-Größen Klein, Mittel und Groß wechseln. Die ausgewählte Größe wird gespeichert.

                    AUSWAHL

                    Ein normaler Klick wählt ein Modell aus. Strg + Klick fügt einzelne Modelle zur Auswahl hinzu oder entfernt sie. Shift + Klick wählt einen zusammenhängenden Bereich aus. Strg+A wählt alle momentan angezeigten Modelle aus.

                    Bei einer Mehrfachauswahl bleiben alle ausgewählten Modelle hervorgehoben. Rechts wird weiterhin das zuletzt angeklickte Modell angezeigt.

                    FAVORITEN

                    ☆ kennzeichnet ein normales Modell.
                    ★ kennzeichnet ein favorisiertes Modell.

                    Der Favoritenstatus kann direkt über den Stern am Thumbnail oder über das Kontextmenü geändert werden. Die ursprüngliche 3MF-Datei wird dadurch nicht verändert.

                    Über „Nur Favoriten“ kann die Galerie auf favorisierte Modelle beschränkt werden. Suche, Sortierung und Favoritenfilter können gemeinsam verwendet werden.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Gallery";
                TxtHelpSubtitle.Text = "Search, sort, select and favorite models";
                TxtHelpContent.Text =
                    """
                    The gallery displays the 3MF files found in a folder together with their preview images.

                    SEARCH

                    Use the search field to filter the displayed models by file name.

                    SORTING

                    The gallery can be sorted by name, modification date or file size.

                    VIEW

                    Use “View” to switch between Small, Medium and Large thumbnail sizes. The selected size is saved.

                    SELECTION

                    A normal click selects one model. Ctrl + click adds or removes individual models from the selection. Shift + click selects a continuous range. Ctrl+A selects all models currently displayed.

                    With multiple selection, all selected models remain highlighted. The most recently clicked model continues to be shown on the right.

                    FAVORITES

                    ☆ indicates a normal model.
                    ★ indicates a favorite model.

                    The favorite status can be changed directly using the star on the thumbnail or from the context menu. This does not modify the original 3MF file.

                    “Favorites only” restricts the gallery to favorite models. Search, sorting and the favorites filter can be used together.
                    """;
            }
        }

        private void ShowPreview()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Vorschau & 3D";
                TxtHelpSubtitle.Text = "2D-Vorschau, 3D-Modell, Druckplatte und Bauraum";
                TxtHelpContent.Text =
                    """
                    Wenn du ein Modell in der Galerie auswählst, erscheint rechts die Vorschau.

                    2D-VORSCHAU

                    2D zeigt das in der 3MF-Datei gespeicherte Vorschaubild.

                    3D-VORSCHAU

                    Über „3D“ wird das eigentliche 3D-Modell aus der 3MF-Datei geladen. Bei Dateien mit mehreren Modellteilen werden die enthaltenen Geometrien gemeinsam an ihren in der 3MF gespeicherten Positionen dargestellt.

                    3D-STEUERUNG

                    Linke Maustaste + Ziehen
                    Dreht die Ansicht frei um das Modell.

                    Mausrad
                    Zoomt hinein oder heraus.

                    Rechte Maustaste + Ziehen
                    Verschiebt die Ansicht.

                    Doppelklick
                    Setzt die 3D-Ansicht zurück.

                    FESTE KAMERAANSICHTEN

                    Über das Ansichtsmenü stehen Isometrisch, Oben, Vorne, Hinten, Links und Rechts zur Verfügung. „Ansicht zurücksetzen“ stellt die Ausgangsansicht und den passenden Zoom wieder her.

                    XYZ-ORIENTIERUNGSANZEIGE

                    Unten links in der 3D-Vorschau zeigt eine kleine X/Y/Z-Achsenanzeige jederzeit die aktuelle räumliche Orientierung. X wird rot, Y grün und Z blau dargestellt. Die Anzeige dreht sich zusammen mit dem Modell und ist auch in der großen Vorschau sichtbar.

                    OBJEKTE IN MEHRTEILIGEN 3MF-DATEIEN

                    Über das Objektmenü können die einzelnen Objekte beziehungsweise Build-Items einer 3MF-Datei angezeigt werden. Ein Objekt kann ausgewählt und farblich hervorgehoben werden.

                    Über das Kontrollkästchen neben einem Objekt lässt es sich ein- oder ausblenden. „Alle anzeigen“ blendet alle Objekte wieder ein.

                    „Objekt fokussieren“ richtet die Kamera auf das ausgewählte Objekt aus. „Ansicht zurücksetzen“ wechselt anschließend wieder zur vollständigen Modellansicht.

                    Für das ausgewählte Objekt werden Größe, Position und eine eigene Prüfung gegen den Bauraum des gewählten Druckers angezeigt. Dadurch ist bei mehrteiligen 3MF-Dateien erkennbar, welches einzelne Objekt eine X-, Y- oder Z-Grenze überschreitet.

                    DRUCKER

                    Über das Druckersymbol wählst du den Drucker aus, den du selbst besitzt bzw. für die Prüfung verwenden möchtest. Der Drucker wird nicht automatisch aus der geöffneten 3MF übernommen.

                    Die gewählte Druckereinstellung wird gespeichert.

                    DRUCKPLATTE

                    Die Druckplatte wird passend zum ausgewählten Drucker maßstäblich dargestellt. Sie besitzt ein Raster und eine strukturierte Oberfläche und kann ein- oder ausgeblendet werden.

                    BAURAUMPRÜFUNG

                    3MF-Explorer vergleicht die tatsächliche Position und Ausdehnung des geladenen Modells mit Breite, Tiefe und Höhe des ausgewählten Druckers.

                    Passt das Modell vollständig in den Bauraum, wird dies entsprechend angezeigt. Überschreitungen in X, Y oder Z werden als Warnung dargestellt.

                    GROSSE VORSCHAU

                    Über die Schaltfläche zum Vergrößern kann die aktuelle 2D- oder 3D-Vorschau in einem separaten großen Fenster geöffnet werden. Auch dort stehen Druckerauswahl, Druckplatte und 3D-Kamerasteuerung zur Verfügung.

                    DRUCKINFORMATIONEN

                    Bei kompatiblen Bambu-Studio-3MF-Dateien versucht 3MF-Explorer zusätzliche Informationen auszulesen, zum Beispiel Druckzeit, Material, Filamentgewicht, Filamentlänge, Düsengröße, Druckprofil, Druckplatte, Bambu-Studio-Version und Support-Informationen.

                    Welche Informationen verfügbar sind, hängt vom Inhalt der jeweiligen 3MF-Datei ab.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Preview & 3D";
                TxtHelpSubtitle.Text = "2D preview, 3D model, build plate and build volume";
                TxtHelpContent.Text =
                    """
                    When you select a model in the gallery, its preview is displayed on the right.

                    2D PREVIEW

                    2D shows the preview image stored in the 3MF file.

                    3D PREVIEW

                    “3D” loads the actual 3D model from the 3MF file. For files containing multiple model parts, the contained geometry is displayed together at the positions stored in the 3MF.

                    3D CONTROLS

                    Left mouse button + drag
                    Rotates the view freely around the model.

                    Mouse wheel
                    Zooms in or out.

                    Right mouse button + drag
                    Pans the view.

                    Double-click
                    Resets the 3D view.

                    FIXED CAMERA VIEWS

                    The view menu provides Isometric, Top, Front, Back, Left and Right. “Reset view” restores the initial view and suitable zoom.

                    XYZ ORIENTATION INDICATOR

                    A small X/Y/Z axis indicator in the lower-left corner of the 3D preview always shows the current spatial orientation. X is red, Y is green and Z is blue. The indicator rotates together with the model and is also visible in the large preview.

                    OBJECTS IN MULTI-PART 3MF FILES

                    The object menu lists the individual objects or build items contained in a 3MF file. An object can be selected and highlighted.

                    Use the check box next to an object to show or hide it. “Show all” makes every object visible again.

                    “Focus object” centers the camera on the selected object. “Reset view” then returns to the complete model view.

                    For the selected object 3MF-Explorer shows its size, position and an individual build-volume check for the selected printer. This makes it possible to see which object in a multi-part 3MF exceeds an X, Y or Z limit.

                    PRINTER

                    Use the printer icon to select the printer you own or want to use for the size check. The printer is not selected automatically from the opened 3MF file.

                    The selected printer setting is saved.

                    BUILD PLATE

                    The build plate is displayed to scale for the selected printer. It has a grid and textured appearance and can be shown or hidden.

                    BUILD VOLUME CHECK

                    3MF-Explorer compares the actual position and dimensions of the loaded model with the width, depth and height of the selected printer.

                    If the model fits completely within the build volume, this is indicated accordingly. Exceeding the X, Y or Z limits is shown as a warning.

                    LARGE PREVIEW

                    The enlarge button opens the current 2D or 3D preview in a separate large window. Printer selection, build plate and 3D camera controls are also available there.

                    PRINT INFORMATION

                    For compatible Bambu Studio 3MF files, 3MF-Explorer attempts to read additional information such as print time, material, filament weight, filament length, nozzle size, print profile, build plate, Bambu Studio version and support information.

                    The information available depends on the contents of the individual 3MF file.
                    """;
            }
        }

        private void ShowFiles()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Dateien verwalten";
                TxtHelpSubtitle.Text = "3MF-Dateien direkt aus 3MF-Explorer verwalten";
                TxtHelpContent.Text =
                    """
                    Mit einem Rechtsklick auf ein Modell öffnest du das Kontextmenü.

                    ÖFFNEN
                    Öffnet die ausgewählte 3MF-Datei mit der in Windows zugeordneten Anwendung.

                    IM EXPLORER ANZEIGEN
                    Öffnet den Windows-Explorer und markiert die ausgewählte Datei.

                    DATEIPFAD KOPIEREN
                    Kopiert den vollständigen Dateipfad. Bei einer Mehrfachauswahl werden die Pfade zeilenweise kopiert.

                    UMBENENNEN
                    Öffnet bei einer einzelnen ausgewählten Datei den Umbenennen-Dialog.

                    IN PAPIERKORB VERSCHIEBEN
                    Verschiebt die ausgewählte Datei nach einer Sicherheitsabfrage in den Windows-Papierkorb. Bei einer Mehrfachauswahl können alle ausgewählten Dateien gemeinsam verschoben werden.

                    Die Dateien werden nicht direkt endgültig gelöscht und können normalerweise über den Windows-Papierkorb wiederhergestellt werden.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Manage Files";
                TxtHelpSubtitle.Text = "Manage 3MF files directly from 3MF-Explorer";
                TxtHelpContent.Text =
                    """
                    Right-click a model to open its context menu.

                    OPEN
                    Opens the selected 3MF file with the application associated with it in Windows.

                    SHOW IN EXPLORER
                    Opens Windows Explorer and selects the file.

                    COPY FILE PATH
                    Copies the full file path. With multiple selection, the paths are copied one per line.

                    RENAME
                    Opens the rename dialog when a single file is selected.

                    MOVE TO RECYCLE BIN
                    Moves the selected file to the Windows Recycle Bin after confirmation. With multiple selection, all selected files can be moved together.

                    Files are not permanently deleted directly and can normally be restored from the Windows Recycle Bin.
                    """;
            }
        }

        private void ShowKeyboard()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Tastenkürzel";
                TxtHelpSubtitle.Text = "3MF-Explorer schneller bedienen";
                TxtHelpContent.Text =
                    """
                    AUSWAHL

                    STRG + KLICK
                    Fügt ein Modell zur bestehenden Auswahl hinzu oder entfernt es wieder.

                    SHIFT + KLICK
                    Wählt einen zusammenhängenden Bereich aus.

                    STRG + A
                    Wählt alle momentan in der Galerie angezeigten Modelle aus.

                    STRG + D
                    Ändert den Favoritenstatus der aktuellen Auswahl.

                    DATEIAKTIONEN

                    ENTER
                    Öffnet die aktuell ausgewählte 3MF-Datei.

                    F2
                    Öffnet bei einer einzelnen ausgewählten Datei den Dialog zum Umbenennen.

                    ENTF
                    Verschiebt die ausgewählte Datei bzw. die ausgewählten Dateien nach einer Sicherheitsabfrage in den Windows-Papierkorb.

                    STRG + C
                    Kopiert den vollständigen Dateipfad. Bei einer Mehrfachauswahl werden alle ausgewählten Dateipfade zeilenweise kopiert.

                    3D-VORSCHAU

                    ESC
                    Schließt das große Vorschaufenster.

                    Die Maussteuerung für Drehen, Zoomen und Verschieben ist im Kapitel „Vorschau & 3D“ beschrieben.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Keyboard Shortcuts";
                TxtHelpSubtitle.Text = "Use 3MF-Explorer more quickly";
                TxtHelpContent.Text =
                    """
                    SELECTION

                    CTRL + CLICK
                    Adds a model to the existing selection or removes it.

                    SHIFT + CLICK
                    Selects a continuous range.

                    CTRL + A
                    Selects all models currently displayed in the gallery.

                    CTRL + D
                    Changes the favorite status of the current selection.

                    FILE ACTIONS

                    ENTER
                    Opens the currently selected 3MF file.

                    F2
                    Opens the rename dialog when a single file is selected.

                    DELETE
                    Moves the selected file or selected files to the Windows Recycle Bin after confirmation.

                    CTRL + C
                    Copies the full file path. With multiple selection, all selected file paths are copied one per line.

                    3D PREVIEW

                    ESC
                    Closes the large preview window.

                    Mouse controls for rotating, zooming and panning are described in the “Preview & 3D” chapter.
                    """;
            }
        }

        private void ShowUpdates()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Updates";
                TxtHelpSubtitle.Text = "3MF-Explorer aktuell halten";
                TxtHelpContent.Text =
                    """
                    3MF-Explorer kann beim Programmstart prüfen, ob eine neuere Version verfügbar ist.

                    Wenn ein Update gefunden wurde, erscheint in der Statusleiste ein entsprechender Hinweis.

                    Durch Anklicken des Hinweises kannst du die Installation des Updates starten.

                    Das Update wird aus dem veröffentlichten GitHub-Release heruntergeladen. Der Release muss dafür ein passendes ZIP-Paket enthalten.

                    Nach erfolgreicher Aktualisierung wird 3MF-Explorer mit der neuen Version neu gestartet.

                    Die aktuell installierte Versionsnummer wird im Hauptfenster angezeigt.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "Updates";
                TxtHelpSubtitle.Text = "Keep 3MF-Explorer up to date";
                TxtHelpContent.Text =
                    """
                    3MF-Explorer can check for a newer version when the program starts.

                    When an update is found, a corresponding notification appears in the status bar.

                    Click the notification to start installing the update.

                    The update is downloaded from the published GitHub release. The release must contain a suitable ZIP package.

                    After a successful update, 3MF-Explorer restarts with the new version.

                    The currently installed version number is displayed in the main window.
                    """;
            }
        }

        private void ShowAbout()
        {
            if (Localization.IsGerman)
            {
                TxtHelpTitle.Text = "Über 3MF-Explorer";
                TxtHelpSubtitle.Text = $"Version {VersionInfo.Version}";
                TxtHelpContent.Text =
                    $"""
                    3MF-Explorer

                    Version {VersionInfo.Version}

                    3MF-Explorer ist ein Windows-Viewer und eine Modellbibliothek für 3MF-Dateien mit besonderem Fokus auf Bambu-Studio-Projekte.

                    Zu den Funktionen gehören:

                    • Thumbnail-Galerie für 3MF-Dateien
                    • 2D- und interaktive 3D-Vorschau
                    • Große Vorschau in separatem Fenster
                    • Druckerauswahl und maßstäbliche Druckplatte
                    • Bauraumprüfung in X, Y und Z
                    • Feste 3D-Kameraansichten
                    • Auslesen verschiedener Druckinformationen
                    • Suche, Sortierung und Favoriten
                    • Einzel- und Mehrfachauswahl
                    • Dateiverwaltung
                    • Tastatursteuerung
                    • Thumbnail-Cache
                    • Automatische Updateprüfung
                    • Zweisprachige Bedienungsanleitung

                    3MF-Explorer wird kontinuierlich weiterentwickelt.
                    """;
            }
            else
            {
                TxtHelpTitle.Text = "About 3MF-Explorer";
                TxtHelpSubtitle.Text = $"Version {VersionInfo.Version}";
                TxtHelpContent.Text =
                    $"""
                    3MF-Explorer

                    Version {VersionInfo.Version}

                    3MF-Explorer is a Windows viewer and model library for 3MF files with a particular focus on Bambu Studio projects.

                    Features include:

                    • Thumbnail gallery for 3MF files
                    • 2D and interactive 3D preview
                    • Large preview in a separate window
                    • Printer selection and build plate displayed to scale
                    • Build-volume checking on X, Y and Z
                    • Fixed 3D camera views
                    • Reading various print information
                    • Search, sorting and favorites
                    • Single and multiple selection
                    • File management
                    • Keyboard controls
                    • Thumbnail cache
                    • Automatic update checking
                    • Bilingual user guide

                    3MF-Explorer is continuously being developed.
                    """;
            }
        }

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private void BtnClose_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
