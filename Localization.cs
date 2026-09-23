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

            // Falls ein Text in einer Sprache versehentlich fehlt,
            // versuchen wir zuerst Deutsch als Fallback.
            if (German.TryGetValue(key, out string? fallback))
                return fallback;

            // Damit fehlende Schlüssel beim Entwickeln sofort auffallen.
            return $"[{key}]";
        }

        public static bool IsGerman =>
            _currentLanguage == "de";

        public static bool IsEnglish =>
            _currentLanguage == "en";

        private static readonly Dictionary<string, string> German =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Allgemein
                ["AppName"] = "3MF-Explorer",
                ["Language"] = "Sprache",
                ["German"] = "Deutsch",
                ["English"] = "English",
                ["Yes"] = "Ja",
                ["No"] = "Nein",
                ["Cancel"] = "Abbrechen",
                ["Close"] = "Schließen",
                ["OK"] = "OK",

                // Hauptfenster
                ["SelectFolder"] = "Ordner auswählen",
                ["Search"] = "Suchen",
                ["SearchPlaceholder"] = "3MF-Dateien suchen...",
                ["Sort"] = "Sortierung",
                ["View"] = "Ansicht",
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

                // Favoriten
                ["AddFavorite"] = "Als Favorit markieren",
                ["RemoveFavorite"] = "Favorit entfernen",
                ["Favorite"] = "Favorit",
                ["Favorites"] = "Favoriten",

                // Kontextmenü / Dateien
                ["Open"] = "Öffnen",
                ["ShowInExplorer"] = "Im Explorer anzeigen",
                ["CopyPath"] = "Dateipfad kopieren",
                ["Rename"] = "Umbenennen",
                ["MoveToRecycleBin"] = "In Papierkorb verschieben",

                // Vorschau
                ["Preview"] = "Vorschau",
                ["FileInformation"] = "Dateiinformationen",
                ["FileName"] = "Dateiname",
                ["FileSize"] = "Dateigröße",
                ["Modified"] = "Geändert",

                // Druckinformationen
                ["PrintInformation"] = "Druckinformationen",
                ["PrintTime"] = "Druckzeit",
                ["Material"] = "Material",
                ["FilamentWeight"] = "Filamentgewicht",
                ["FilamentLength"] = "Filamentlänge",
                ["NozzleSize"] = "Düsengröße",
                ["PrintProfile"] = "Druckprofil",
                ["BuildPlate"] = "Druckplatte",
                ["BambuStudioVersion"] = "Bambu-Studio-Version",
                ["SupportInformation"] = "Support-Informationen",

                // Status
                ["Ready"] = "Bereit",
                ["NoFiles"] = "Keine 3MF-Dateien gefunden",
                ["OneFile"] = "1 Datei",
                ["Files"] = "{0} Dateien",
                ["OneFileSelected"] = "1 Datei ausgewählt",
                ["FilesSelected"] = "{0} Dateien ausgewählt",

                // Tastaturhinweise
                ["KeyboardHints"] =
                    "Enter: Öffnen   F2: Umbenennen   Entf: Papierkorb   Strg+C: Pfad kopieren   Strg+D: Favorit",

                // Umbenennen
                ["RenameFile"] = "Datei umbenennen",
                ["NewFileName"] = "Neuer Dateiname",
                ["RenameButton"] = "Umbenennen",

                // Löschen
                ["DeleteTitle"] = "Datei in Papierkorb verschieben",
                ["DeleteMultipleTitle"] = "Dateien in Papierkorb verschieben",

                ["DeleteQuestion"] =
                    "Soll die ausgewählte Datei wirklich in den Windows-Papierkorb verschoben werden?",

                ["DeleteMultipleQuestion"] =
                    "Sollen die {0} ausgewählten Dateien wirklich in den Windows-Papierkorb verschoben werden?",

                // Fehler
                ["Error"] = "Fehler",
                ["OpenError"] = "Die Datei konnte nicht geöffnet werden.",
                ["ExplorerError"] = "Die Datei konnte nicht im Explorer angezeigt werden.",
                ["RenameError"] = "Die Datei konnte nicht umbenannt werden.",
                ["DeleteError"] = "Die Datei konnte nicht in den Papierkorb verschoben werden.",
                ["ClipboardError"] = "Der Dateipfad konnte nicht in die Zwischenablage kopiert werden.",

                // Update
                ["UpdateAvailable"] = "Neue Version {0} verfügbar",
                ["UpdateDownload"] = "Update herunterladen",
                ["UpdateError"] = "Das Update konnte nicht durchgeführt werden.",
                ["UpdateCheckError"] = "Die Updateprüfung konnte nicht durchgeführt werden.",

                // Hilfe
                ["HelpTitle"] = "3MF-Explorer – Hilfe",
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
                ["Language"] = "Language",
                ["German"] = "Deutsch",
                ["English"] = "English",
                ["Yes"] = "Yes",
                ["No"] = "No",
                ["Cancel"] = "Cancel",
                ["Close"] = "Close",
                ["OK"] = "OK",

                // Main window
                ["SelectFolder"] = "Select folder",
                ["Search"] = "Search",
                ["SearchPlaceholder"] = "Search 3MF files...",
                ["Sort"] = "Sort",
                ["View"] = "View",
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

                // Favorites
                ["AddFavorite"] = "Add to favorites",
                ["RemoveFavorite"] = "Remove from favorites",
                ["Favorite"] = "Favorite",
                ["Favorites"] = "Favorites",

                // Context menu / files
                ["Open"] = "Open",
                ["ShowInExplorer"] = "Show in Explorer",
                ["CopyPath"] = "Copy file path",
                ["Rename"] = "Rename",
                ["MoveToRecycleBin"] = "Move to Recycle Bin",

                // Preview
                ["Preview"] = "Preview",
                ["FileInformation"] = "File information",
                ["FileName"] = "File name",
                ["FileSize"] = "File size",
                ["Modified"] = "Modified",

                // Print information
                ["PrintInformation"] = "Print information",
                ["PrintTime"] = "Print time",
                ["Material"] = "Material",
                ["FilamentWeight"] = "Filament weight",
                ["FilamentLength"] = "Filament length",
                ["NozzleSize"] = "Nozzle size",
                ["PrintProfile"] = "Print profile",
                ["BuildPlate"] = "Build plate",
                ["BambuStudioVersion"] = "Bambu Studio version",
                ["SupportInformation"] = "Support information",

                // Status
                ["Ready"] = "Ready",
                ["NoFiles"] = "No 3MF files found",
                ["OneFile"] = "1 file",
                ["Files"] = "{0} files",
                ["OneFileSelected"] = "1 file selected",
                ["FilesSelected"] = "{0} files selected",

                // Keyboard hints
                ["KeyboardHints"] =
                    "Enter: Open   F2: Rename   Del: Recycle Bin   Ctrl+C: Copy path   Ctrl+D: Favorite",

                // Rename
                ["RenameFile"] = "Rename file",
                ["NewFileName"] = "New file name",
                ["RenameButton"] = "Rename",

                // Delete
                ["DeleteTitle"] = "Move file to Recycle Bin",
                ["DeleteMultipleTitle"] = "Move files to Recycle Bin",

                ["DeleteQuestion"] =
                    "Do you really want to move the selected file to the Windows Recycle Bin?",

                ["DeleteMultipleQuestion"] =
                    "Do you really want to move the {0} selected files to the Windows Recycle Bin?",

                // Errors
                ["Error"] = "Error",
                ["OpenError"] = "The file could not be opened.",
                ["ExplorerError"] = "The file could not be shown in Explorer.",
                ["RenameError"] = "The file could not be renamed.",
                ["DeleteError"] = "The file could not be moved to the Recycle Bin.",
                ["ClipboardError"] = "The file path could not be copied to the clipboard.",

                // Update
                ["UpdateAvailable"] = "New version {0} available",
                ["UpdateDownload"] = "Download update",
                ["UpdateError"] = "The update could not be completed.",
                ["UpdateCheckError"] = "The update check could not be completed.",

                // Help
                ["HelpTitle"] = "3MF-Explorer – Help",
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