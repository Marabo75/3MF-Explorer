using System;
using System.Collections.Generic;

namespace ThreeMFExplorer
{
    public static class Localization
    {
        private static string _currentLanguage = "de";

        public static string CurrentLanguage => _currentLanguage;

        public static event EventHandler? LanguageChanged;

        public static void SetLanguage(string? language)
        {
            string normalized =
                string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
                    ? "en"
                    : "de";

            if (_currentLanguage == normalized)
                return;

            _currentLanguage = normalized;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string Get(string key)
        {
            if (_currentLanguage == "en")
            {
                if (English.TryGetValue(key, out string? value))
                    return value;
            }
            else
            {
                if (German.TryGetValue(key, out string? value))
                    return value;
            }

            if (German.TryGetValue(key, out string? fallback))
                return fallback;

            return $"[{key}]";
        }

        public static bool IsGerman => _currentLanguage == "de";
        public static bool IsEnglish => _currentLanguage == "en";

        private static readonly Dictionary<string, string> German =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Allgemein
                ["AppName"] = "3MF-Explorer",
                ["Subtitle"] = "3MF-Dateien durchsuchen, verwalten und in 3D anzeigen",
                ["Language"] = "Sprache",
                ["German"] = "Deutsch",
                ["English"] = "English",
                ["Yes"] = "Ja",
                ["No"] = "Nein",
                ["Cancel"] = "Abbrechen",
                ["Close"] = "Schließen",
                ["CloseEsc"] = "Schließen (Esc)",
                ["OK"] = "OK",
                ["Minimize"] = "Minimieren",
                ["Maximize"] = "Maximieren",
                ["Restore"] = "Wiederherstellen",

                // Hauptfenster / Navigation
                ["FoldersHeading"] = "ORDNER",
                ["Models"] = "MODELLE",
                ["SelectFolder"] = "Ordner auswählen",
                ["Search"] = "Suchen",
                ["SearchPlaceholder"] = "3MF-Dateien suchen...",
                ["SearchFiles"] = "3MF-Dateien suchen...",
                ["Sort"] = "Sortierung",
                ["SortLabel"] = "SORTIERUNG",
                ["View"] = "Ansicht",
                ["ViewLabel"] = "ANSICHT",
                ["Help"] = "? Hilfe",

                // Galerie
                ["Small"] = "Klein",
                ["Medium"] = "Mittel",
                ["Large"] = "Groß",
                ["NameAZ"] = "Name A–Z",
                ["NameZA"] = "Name Z–A",
                ["NewestFirst"] = "Neueste zuerst",
                ["OldestFirst"] = "Älteste zuerst",
                ["LargestFirst"] = "Größte zuerst",
                ["SmallestFirst"] = "Kleinste zuerst",
                ["OnlyFavorites"] = "☆ Nur Favoriten",
                ["OnlyFavoritesActive"] = "★ Nur Favoriten",
                ["OnlyFavoritesToolTip"] = "Nur Favoriten anzeigen",

                // Favoriten
                ["AddFavorite"] = "Als Favorit markieren",
                ["RemoveFavorite"] = "Favorit entfernen",
                ["Favorite"] = "Favorit",
                ["Favorites"] = "Favoriten",
                ["FavoritesSaveError"] = "Die Favoriten konnten nicht gespeichert werden.",

                // Dateien / Kontextmenü
                ["Open"] = "Öffnen",
                ["OpenFileTitle"] = "Datei öffnen",
                ["ShowInExplorer"] = "Im Explorer anzeigen",
                ["CopyPath"] = "Dateipfad kopieren",
                ["Rename"] = "Umbenennen",
                ["MoveToRecycleBin"] = "In Papierkorb verschieben",
                ["MoveFilesToRecycleBin"] = "Dateien in Papierkorb verschieben",
                ["RecycleBin"] = "Papierkorb",

                // Vorschau
                ["Preview"] = "Vorschau",
                ["PreviewHeading"] = "VORSCHAU",
                ["PreviewWindowTitle"] = "3MF-Explorer – Vorschau",
                ["EnlargePreview"] = "Große Vorschau öffnen",
                ["FileInformation"] = "Dateiinformationen",
                ["FileName"] = "Dateiname",
                ["FileSize"] = "Dateigröße",
                ["Modified"] = "Geändert",
                ["SizeValue"] = "{0:N0} KB",
                ["ModifiedValue"] = "{0:g}",

                // Druckinformationen
                ["PrintInformation"] = "Druckinformationen",
                ["PrintInformationHeading"] = "DRUCKINFORMATIONEN",
                ["PrintTime"] = "Druckzeit",
                ["Material"] = "Material",
                ["Filament"] = "Filament",
                ["FilamentWeight"] = "Filamentgewicht",
                ["FilamentLength"] = "Filamentlänge",
                ["Nozzle"] = "Düse",
                ["NozzleSize"] = "Düsengröße",
                ["PrintProfile"] = "Druckprofil",
                ["BuildPlate"] = "Druckplatte",
                ["Printer"] = "Drucker",
                ["PrinterAndBuildPlate"] = "Drucker & Bauraum",
                ["BambuStudioVersion"] = "Bambu-Studio-Version",
                ["SupportInformation"] = "Support-Informationen",

                // 3D-Ansicht
                ["3DHint"] = "Linke Maustaste: Drehen · Mausrad: Zoomen",
                ["3DHintLarge"] = "Linke Maustaste: Drehen · Mausrad: Zoomen · Esc: Schließen",
                ["Select3DView"] = "3D-Ansicht wählen",
                ["ResetView"] = "Ansicht zurücksetzen",
                ["ViewIsometric"] = "Isometrisch",
                ["ViewFront"] = "Vorne",
                ["ViewBack"] = "Hinten",
                ["ViewLeft"] = "Links",
                ["ViewRight"] = "Rechts",
                ["ViewTop"] = "Oben",
                ["ShowBuildPlate"] = "Druckplatte anzeigen",
                ["ShowOriginalBuildPlate"] = "Originale Projektplatte anzeigen",

                // 3D-Objekte
                ["Object"] = "Objekt",
                ["Objects"] = "Objekte",
                ["ObjectsMenu"] = "Objekte",
                ["OneObject"] = "1 Objekt",
                ["ObjectCount"] = "{0} Objekte",
                ["SelectObject"] = "Objekt auswählen",
                ["ShowObject"] = "Objekt anzeigen",
                ["HideObject"] = "Objekt ausblenden",
                ["Hidden"] = "Ausgeblendet",
                ["ShowAllObjects"] = "Alle Objekte anzeigen",
                ["IsolateObject"] = "Nur dieses Objekt anzeigen",
                ["FocusObject"] = "Objekt fokussieren",
                ["ObjectSize"] = "Größe: {0}",
                ["ObjectPosition"] = "Position: {0}",
                ["ObjectTriangles"] = "Dreiecke: {0:N0}",
                ["ObjectFits"] = "✓ Passt in den Bauraum",
                ["ObjectExceeds"] = "⚠ Bauraum überschritten: {0}",

                // Status
                ["Ready"] = "Bereit",
                ["NoFiles"] = "Keine 3MF-Dateien gefunden",
                ["OneFile"] = "1 Datei",
                ["Files"] = "{0} Dateien",
                ["FileCount"] = "{0} Dateien",
                ["OneFileSelected"] = "1 Datei ausgewählt",
                ["FilesSelected"] = "{0} Dateien ausgewählt",
                ["OneFileSelectedHints"] =
                    "1 Datei ausgewählt   ·   Enter: Öffnen   F2: Umbenennen   Entf: Papierkorb   Strg+C: Pfad kopieren   Strg+D: Favorit",
                ["FilesSelectedHints"] =
                    "{0} Dateien ausgewählt   ·   Entf: Papierkorb   Strg+C: Pfade kopieren   Strg+D: Favorit",
                ["KeyboardHints"] =
                    "Enter: Öffnen   F2: Umbenennen   Entf: Papierkorb   Strg+C: Pfad kopieren   Strg+D: Favorit",

                // Umbenennen
                ["RenameFile"] = "Datei umbenennen",
                ["NewFileName"] = "Neuer Dateiname",
                ["EnterFileName"] = "Neuen Dateinamen eingeben",
                ["RenameButton"] = "Umbenennen",
                ["InvalidFileName"] = "Der Dateiname enthält ungültige Zeichen.",
                ["FileAlreadyExists"] = "Eine Datei mit diesem Namen ist bereits vorhanden.",

                // Löschen
                ["DeleteTitle"] = "Datei in Papierkorb verschieben",
                ["DeleteMultipleTitle"] = "Dateien in Papierkorb verschieben",
                ["DeleteQuestion"] =
                    "Soll die ausgewählte Datei wirklich in den Windows-Papierkorb verschoben werden?",
                ["DeleteMultipleQuestion"] =
                    "Sollen die {0} ausgewählten Dateien wirklich in den Windows-Papierkorb verschoben werden?",
                ["DeleteSingleQuestionDetailed"] =
                    "Soll die Datei \"{0}\" wirklich in den Windows-Papierkorb verschoben werden?",
                ["DeleteMultipleQuestionDetailed"] =
                    "Sollen die {0} ausgewählten Dateien wirklich in den Windows-Papierkorb verschoben werden?",
                ["SelectedFilesNoLongerExist"] =
                    "Die ausgewählten Dateien sind nicht mehr vorhanden.",
                ["FileNoLongerExists"] =
                    "Die ausgewählte Datei ist nicht mehr vorhanden.",
                ["SomeDeleteErrors"] =
                    "Einige Dateien konnten nicht in den Papierkorb verschoben werden.",

                // Zwischenablage / Explorer / Fehler
                ["Error"] = "Fehler",
                ["OpenError"] = "Die Datei konnte nicht geöffnet werden.",
                ["ExplorerError"] = "Die Datei konnte nicht im Explorer angezeigt werden.",
                ["ExplorerTitle"] = "Windows Explorer",
                ["RenameError"] = "Die Datei konnte nicht umbenannt werden.",
                ["DeleteError"] = "Die Datei konnte nicht in den Papierkorb verschoben werden.",
                ["ClipboardError"] =
                    "Der Dateipfad konnte nicht in die Zwischenablage kopiert werden.",
                ["ClipboardPathsError"] =
                    "Die Dateipfade konnten nicht in die Zwischenablage kopiert werden.",
                ["ClipboardTitle"] = "Zwischenablage",

                // Update
                ["UpdateAvailable"] = "Neue Version {0} verfügbar",
                ["UpdateAvailableClick"] =
                    "Neue Version {0} verfügbar – zum Installieren klicken",
                ["UpdateAvailableTitle"] = "Update verfügbar",
                ["UpdateDownload"] = "Update herunterladen",
                ["DownloadingUpdate"] = "Update wird heruntergeladen...",
                ["UpdateInstallQuestion"] =
                    "Version {0} ist verfügbar. Soll das Update jetzt installiert werden?",
                ["UpdateInstallError"] = "Das Update konnte nicht installiert werden.",
                ["UpdateError"] = "Das Update konnte nicht durchgeführt werden.",
                ["UpdateErrorTitle"] = "Updatefehler",
                ["UpdateCheckError"] =
                    "Die Updateprüfung konnte nicht durchgeführt werden.",

                // Hilfe
                ["HelpTitle"] = "3MF-Explorer – Hilfe",
                ["UserGuide"] = "Benutzerhandbuch",
                ["OpenUserGuide"] = "Benutzerhandbuch öffnen",
                ["GettingStarted"] = "Erste Schritte",
                ["FoldersNavigation"] = "Ordner & Navigation",
                ["Gallery"] = "Galerie",
                ["PreviewPrintData"] = "Vorschau & Druckdaten",
                ["ManageFiles"] = "Dateien verwalten",
                ["KeyboardShortcuts"] = "Tastenkürzel",
                ["Updates"] = "Updates",
                ["About"] = "Über 3MF-Explorer"
            };

        private static readonly Dictionary<string, string> English =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // General
                ["AppName"] = "3MF-Explorer",
                ["Subtitle"] = "Browse, manage and view 3MF files in 3D",
                ["Language"] = "Language",
                ["German"] = "Deutsch",
                ["English"] = "English",
                ["Yes"] = "Yes",
                ["No"] = "No",
                ["Cancel"] = "Cancel",
                ["Close"] = "Close",
                ["CloseEsc"] = "Close (Esc)",
                ["OK"] = "OK",
                ["Minimize"] = "Minimize",
                ["Maximize"] = "Maximize",
                ["Restore"] = "Restore",

                // Main window / navigation
                ["FoldersHeading"] = "FOLDERS",
                ["Models"] = "MODELS",
                ["SelectFolder"] = "Select folder",
                ["Search"] = "Search",
                ["SearchPlaceholder"] = "Search 3MF files...",
                ["SearchFiles"] = "Search 3MF files...",
                ["Sort"] = "Sort",
                ["SortLabel"] = "SORT",
                ["View"] = "View",
                ["ViewLabel"] = "VIEW",
                ["Help"] = "? Help",

                // Gallery
                ["Small"] = "Small",
                ["Medium"] = "Medium",
                ["Large"] = "Large",
                ["NameAZ"] = "Name A–Z",
                ["NameZA"] = "Name Z–A",
                ["NewestFirst"] = "Newest first",
                ["OldestFirst"] = "Oldest first",
                ["LargestFirst"] = "Largest first",
                ["SmallestFirst"] = "Smallest first",
                ["OnlyFavorites"] = "☆ Favorites only",
                ["OnlyFavoritesActive"] = "★ Favorites only",
                ["OnlyFavoritesToolTip"] = "Show favorites only",

                // Favorites
                ["AddFavorite"] = "Add to favorites",
                ["RemoveFavorite"] = "Remove from favorites",
                ["Favorite"] = "Favorite",
                ["Favorites"] = "Favorites",
                ["FavoritesSaveError"] = "The favorites could not be saved.",

                // Files / context menu
                ["Open"] = "Open",
                ["OpenFileTitle"] = "Open file",
                ["ShowInExplorer"] = "Show in Explorer",
                ["CopyPath"] = "Copy file path",
                ["Rename"] = "Rename",
                ["MoveToRecycleBin"] = "Move to Recycle Bin",
                ["MoveFilesToRecycleBin"] = "Move files to Recycle Bin",
                ["RecycleBin"] = "Recycle Bin",

                // Preview
                ["Preview"] = "Preview",
                ["PreviewHeading"] = "PREVIEW",
                ["PreviewWindowTitle"] = "3MF-Explorer – Preview",
                ["EnlargePreview"] = "Open large preview",
                ["FileInformation"] = "File information",
                ["FileName"] = "File name",
                ["FileSize"] = "File size",
                ["Modified"] = "Modified",
                ["SizeValue"] = "{0:N0} KB",
                ["ModifiedValue"] = "{0:g}",

                // Print information
                ["PrintInformation"] = "Print information",
                ["PrintInformationHeading"] = "PRINT INFORMATION",
                ["PrintTime"] = "Print time",
                ["Material"] = "Material",
                ["Filament"] = "Filament",
                ["FilamentWeight"] = "Filament weight",
                ["FilamentLength"] = "Filament length",
                ["Nozzle"] = "Nozzle",
                ["NozzleSize"] = "Nozzle size",
                ["PrintProfile"] = "Print profile",
                ["BuildPlate"] = "Build plate",
                ["Printer"] = "Printer",
                ["PrinterAndBuildPlate"] = "Printer & build volume",
                ["BambuStudioVersion"] = "Bambu Studio version",
                ["SupportInformation"] = "Support information",

                // 3D view
                ["3DHint"] = "Left mouse button: Rotate · Mouse wheel: Zoom",
                ["3DHintLarge"] =
                    "Left mouse button: Rotate · Mouse wheel: Zoom · Esc: Close",
                ["Select3DView"] = "Select 3D view",
                ["ResetView"] = "Reset view",
                ["ViewIsometric"] = "Isometric",
                ["ViewFront"] = "Front",
                ["ViewBack"] = "Back",
                ["ViewLeft"] = "Left",
                ["ViewRight"] = "Right",
                ["ViewTop"] = "Top",
                ["ShowBuildPlate"] = "Show build plate",
                ["ShowOriginalBuildPlate"] = "Show original project plate",

                // 3D objects
                ["Object"] = "Object",
                ["Objects"] = "Objects",
                ["ObjectsMenu"] = "Objects",
                ["OneObject"] = "1 object",
                ["ObjectCount"] = "{0} objects",
                ["SelectObject"] = "Select object",
                ["ShowObject"] = "Show object",
                ["HideObject"] = "Hide object",
                ["Hidden"] = "Hidden",
                ["ShowAllObjects"] = "Show all objects",
                ["IsolateObject"] = "Show only this object",
                ["FocusObject"] = "Focus object",
                ["ObjectSize"] = "Size: {0}",
                ["ObjectPosition"] = "Position: {0}",
                ["ObjectTriangles"] = "Triangles: {0:N0}",
                ["ObjectFits"] = "✓ Fits within the build volume",
                ["ObjectExceeds"] = "⚠ Build volume exceeded: {0}",

                // Status
                ["Ready"] = "Ready",
                ["NoFiles"] = "No 3MF files found",
                ["OneFile"] = "1 file",
                ["Files"] = "{0} files",
                ["FileCount"] = "{0} files",
                ["OneFileSelected"] = "1 file selected",
                ["FilesSelected"] = "{0} files selected",
                ["OneFileSelectedHints"] =
                    "1 file selected   ·   Enter: Open   F2: Rename   Del: Recycle Bin   Ctrl+C: Copy path   Ctrl+D: Favorite",
                ["FilesSelectedHints"] =
                    "{0} files selected   ·   Del: Recycle Bin   Ctrl+C: Copy paths   Ctrl+D: Favorite",
                ["KeyboardHints"] =
                    "Enter: Open   F2: Rename   Del: Recycle Bin   Ctrl+C: Copy path   Ctrl+D: Favorite",

                // Rename
                ["RenameFile"] = "Rename file",
                ["NewFileName"] = "New file name",
                ["EnterFileName"] = "Enter new file name",
                ["RenameButton"] = "Rename",
                ["InvalidFileName"] = "The file name contains invalid characters.",
                ["FileAlreadyExists"] = "A file with this name already exists.",

                // Delete
                ["DeleteTitle"] = "Move file to Recycle Bin",
                ["DeleteMultipleTitle"] = "Move files to Recycle Bin",
                ["DeleteQuestion"] =
                    "Do you really want to move the selected file to the Windows Recycle Bin?",
                ["DeleteMultipleQuestion"] =
                    "Do you really want to move the {0} selected files to the Windows Recycle Bin?",
                ["DeleteSingleQuestionDetailed"] =
                    "Do you really want to move \"{0}\" to the Windows Recycle Bin?",
                ["DeleteMultipleQuestionDetailed"] =
                    "Do you really want to move the {0} selected files to the Windows Recycle Bin?",
                ["SelectedFilesNoLongerExist"] =
                    "The selected files no longer exist.",
                ["FileNoLongerExists"] =
                    "The selected file no longer exists.",
                ["SomeDeleteErrors"] =
                    "Some files could not be moved to the Recycle Bin.",

                // Clipboard / Explorer / errors
                ["Error"] = "Error",
                ["OpenError"] = "The file could not be opened.",
                ["ExplorerError"] = "The file could not be shown in Explorer.",
                ["ExplorerTitle"] = "Windows Explorer",
                ["RenameError"] = "The file could not be renamed.",
                ["DeleteError"] = "The file could not be moved to the Recycle Bin.",
                ["ClipboardError"] =
                    "The file path could not be copied to the clipboard.",
                ["ClipboardPathsError"] =
                    "The file paths could not be copied to the clipboard.",
                ["ClipboardTitle"] = "Clipboard",

                // Update
                ["UpdateAvailable"] = "New version {0} available",
                ["UpdateAvailableClick"] =
                    "New version {0} available – click to install",
                ["UpdateAvailableTitle"] = "Update available",
                ["UpdateDownload"] = "Download update",
                ["DownloadingUpdate"] = "Downloading update...",
                ["UpdateInstallQuestion"] =
                    "Version {0} is available. Do you want to install the update now?",
                ["UpdateInstallError"] = "The update could not be installed.",
                ["UpdateError"] = "The update could not be completed.",
                ["UpdateErrorTitle"] = "Update error",
                ["UpdateCheckError"] = "The update check could not be completed.",

                // Help
                ["HelpTitle"] = "3MF-Explorer – Help",
                ["UserGuide"] = "User guide",
                ["OpenUserGuide"] = "Open user guide",
                ["GettingStarted"] = "Getting Started",
                ["FoldersNavigation"] = "Folders & Navigation",
                ["Gallery"] = "Gallery",
                ["PreviewPrintData"] = "Preview & Print Data",
                ["ManageFiles"] = "Manage Files",
                ["KeyboardShortcuts"] = "Keyboard Shortcuts",
                ["Updates"] = "Updates",
                ["About"] = "About 3MF-Explorer"
            };
    }
}