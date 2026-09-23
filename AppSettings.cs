using System;
using System.Collections.Generic;
using System.IO;

namespace ThreeMFExplorer
{
    public static class AppSettings
    {
        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "3MF-Explorer");

        private static readonly string LegacySettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "3MF-Explorer");

        private static readonly string SettingsFile =
            Path.Combine(
                SettingsDirectory,
                "settings.ini");

        private static readonly string LegacySettingsFile =
            Path.Combine(
                LegacySettingsDirectory,
                "settings.ini");

        private const string LastFolderKey = "LastFolder";
        private const string ThumbnailViewKey = "ThumbnailView";
        private const string LanguageKey = "Language";

        public static void SaveLastFolder(string folder)
        {
            SaveSetting(
                LastFolderKey,
                folder ?? string.Empty);
        }

        public static string LoadLastFolder()
        {
            return LoadSetting(
                LastFolderKey,
                string.Empty);
        }

        public static void SaveThumbnailView(string view)
        {
            if (string.IsNullOrWhiteSpace(view))
                view = "Medium";

            SaveSetting(
                ThumbnailViewKey,
                view);
        }

        public static string LoadThumbnailView()
        {
            string view = LoadSetting(
                ThumbnailViewKey,
                "Medium");

            return view switch
            {
                "Small" => "Small",
                "Large" => "Large",
                _ => "Medium"
            };
        }

        public static void SaveLanguage(string language)
        {
            string normalized =
                string.Equals(
                    language,
                    "en",
                    StringComparison.OrdinalIgnoreCase)
                    ? "en"
                    : "de";

            SaveSetting(
                LanguageKey,
                normalized);
        }

        public static string LoadLanguage()
        {
            string language = LoadSetting(
                LanguageKey,
                "de");

            return string.Equals(
                language,
                "en",
                StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "de";
        }

        private static string LoadSetting(
            string key,
            string defaultValue)
        {
            try
            {
                string sourceFile =
                    File.Exists(SettingsFile)
                        ? SettingsFile
                        : LegacySettingsFile;

                if (!File.Exists(sourceFile))
                    return defaultValue;

                string[] lines =
                    File.ReadAllLines(sourceFile);

                string prefix = key + "=";

                foreach (string line in lines)
                {
                    if (line.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring(prefix.Length);
                    }
                }
            }
            catch
            {
                // Bei beschädigten oder nicht lesbaren Einstellungen
                // verwenden wir den Standardwert.
            }

            return defaultValue;
        }

        private static void SaveSetting(
            string key,
            string value)
        {
            try
            {
                Directory.CreateDirectory(
                    SettingsDirectory);

                Dictionary<string, string> settings =
                    LoadAllSettings();

                settings[key] = value;

                List<string> lines = new();

                // Feste Reihenfolge für eine übersichtliche Datei.
                AddIfPresent(
                    lines,
                    settings,
                    LastFolderKey);

                AddIfPresent(
                    lines,
                    settings,
                    ThumbnailViewKey);

                AddIfPresent(
                    lines,
                    settings,
                    LanguageKey);

                // Zukünftige Einstellungen ebenfalls erhalten.
                foreach (KeyValuePair<string, string> item in settings)
                {
                    if (item.Key.Equals(
                            LastFolderKey,
                            StringComparison.OrdinalIgnoreCase) ||
                        item.Key.Equals(
                            ThumbnailViewKey,
                            StringComparison.OrdinalIgnoreCase) ||
                        item.Key.Equals(
                            LanguageKey,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    lines.Add(
                        $"{item.Key}={item.Value}");
                }

                File.WriteAllLines(
                    SettingsFile,
                    lines);
            }
            catch
            {
                // Einstellungen dürfen den Programmablauf
                // nicht unterbrechen.
            }
        }

        private static Dictionary<string, string> LoadAllSettings()
        {
            Dictionary<string, string> settings =
                new(StringComparer.OrdinalIgnoreCase);

            try
            {
                string sourceFile =
                    File.Exists(SettingsFile)
                        ? SettingsFile
                        : LegacySettingsFile;

                if (!File.Exists(sourceFile))
                    return settings;

                string[] lines =
                    File.ReadAllLines(sourceFile);

                foreach (string line in lines)
                {
                    int separator =
                        line.IndexOf('=');

                    if (separator <= 0)
                        continue;

                    string key =
                        line.Substring(0, separator).Trim();

                    string value =
                        line.Substring(separator + 1);

                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    settings[key] = value;
                }
            }
            catch
            {
            }

            return settings;
        }

        private static void AddIfPresent(
            List<string> lines,
            Dictionary<string, string> settings,
            string key)
        {
            if (settings.TryGetValue(
                key,
                out string? value))
            {
                lines.Add(
                    $"{key}={value}");
            }
        }
    }
}