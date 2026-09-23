using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace ThreeMFExplorer
{
    public sealed class BambuPrintMetadata
    {
        public string PrintTime { get; set; } = "–";

        public string Material { get; set; } = "–";

        public string FilamentWeight { get; set; } = "–";

        public string FilamentLength { get; set; } = "–";

        public string NozzleDiameter { get; set; } = "–";

        public string PrintProfile { get; set; } = "–";

        public string PlateType { get; set; } = "–";

        public string BambuStudioVersion { get; set; } = "–";

        public string Support { get; set; } = "–";
    }

    public static class ThreeMfMetadataExtractor
    {
        public static BambuPrintMetadata Extract(
            string filePath)
        {
            BambuPrintMetadata result =
                new BambuPrintMetadata();

            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                return result;
            }

            try
            {
                using ZipArchive archive =
                    ZipFile.OpenRead(filePath);

                ReadSliceInfo(
                    archive,
                    result);

                ReadProjectSettings(
                    archive,
                    result);
            }
            catch
            {
                // Bei ungültigen oder nicht unterstützten
                // 3MF-Dateien bleiben die Werte auf "–".
            }

            return result;
        }

        private static void ReadSliceInfo(
            ZipArchive archive,
            BambuPrintMetadata result)
        {
            ZipArchiveEntry? entry =
                FindEntry(
                    archive,
                    "Metadata/slice_info.config");

            if (entry is null)
            {
                return;
            }

            try
            {
                using Stream stream =
                    entry.Open();

                XDocument document =
                    XDocument.Load(stream);

                XElement? root =
                    document.Root;

                if (root is null)
                {
                    return;
                }

                XElement? plate =
                    root
                        .Descendants()
                        .FirstOrDefault(
                            x =>
                                x.Name.LocalName.Equals(
                                    "plate",
                                    StringComparison.OrdinalIgnoreCase));

                if (plate is not null)
                {
                    string? prediction =
                        FindMetadataValue(
                            plate,
                            "prediction");

                    if (TryParseDouble(
                            prediction,
                            out double seconds))
                    {
                        result.PrintTime =
                            FormatPrintTime(seconds);
                    }

                    string? filamentWeight =
                        FindMetadataValue(
                            plate,
                            "weight");

                    if (TryParseDouble(
                            filamentWeight,
                            out double weight))
                    {
                        result.FilamentWeight =
                            $"{weight:0.00} g";
                    }

                    string? filamentLength =
                        FindMetadataValue(
                            plate,
                            "length");

                    if (TryParseDouble(
                            filamentLength,
                            out double length))
                    {
                        result.FilamentLength =
                            $"{length / 1000.0:0.00} m";
                    }
                }

                XElement? filament =
                    root
                        .Descendants()
                        .FirstOrDefault(
                            x =>
                                x.Name.LocalName.Equals(
                                    "filament",
                                    StringComparison.OrdinalIgnoreCase));

                if (filament is not null)
                {
                    string? material =
                        GetAttributeValue(
                            filament,
                            "type");

                    if (!string.IsNullOrWhiteSpace(material))
                    {
                        result.Material =
                            material;
                    }

                    string? weight =
                        GetAttributeValue(
                            filament,
                            "used_g");

                    if (TryParseDouble(
                            weight,
                            out double filamentWeight))
                    {
                        result.FilamentWeight =
                            $"{filamentWeight:0.00} g";
                    }

                    string? length =
                        GetAttributeValue(
                            filament,
                            "used_m");

                    if (TryParseDouble(
                            length,
                            out double filamentLength))
                    {
                        result.FilamentLength =
                            $"{filamentLength:0.00} m";
                    }
                }
            }
            catch
            {
                // Fehlerhafte Einzelwerte sollen den Viewer
                // nicht zum Absturz bringen.
            }
        }

        private static void ReadProjectSettings(
            ZipArchive archive,
            BambuPrintMetadata result)
        {
            ZipArchiveEntry? entry =
                FindEntry(
                    archive,
                    "Metadata/project_settings.config");

            if (entry is null)
            {
                return;
            }

            try
            {
                using StreamReader reader =
                    new StreamReader(
                        entry.Open());

                string content =
                    reader.ReadToEnd();

                using JsonDocument document =
                    JsonDocument.Parse(
                        content);

                JsonElement root =
                    document.RootElement;

                string? nozzleDiameter =
                    FindJsonValue(
                        root,
                        "nozzle_diameter");

                if (!string.IsNullOrWhiteSpace(nozzleDiameter))
                {
                    result.NozzleDiameter =
                        $"{nozzleDiameter} mm";
                }

                string? printProfile =
                    FindJsonValue(
                        root,
                        "print_settings_id");

                if (!string.IsNullOrWhiteSpace(printProfile))
                {
                    result.PrintProfile =
                        printProfile;
                }

                string? plateType =
                    FindJsonValue(
                        root,
                        "curr_bed_type");

                if (string.IsNullOrWhiteSpace(plateType))
                {
                    plateType =
                        FindJsonValue(
                            root,
                            "bed_type");
                }

                if (!string.IsNullOrWhiteSpace(plateType))
                {
                    result.PlateType =
                        plateType;
                }

                string? studioVersion =
                    FindJsonValue(
                        root,
                        "version");

                if (!string.IsNullOrWhiteSpace(studioVersion))
                {
                    result.BambuStudioVersion =
                        studioVersion;
                }

                string? support =
                    FindJsonValue(
                        root,
                        "enable_support");

                if (!string.IsNullOrWhiteSpace(support))
                {
                    result.Support =
                        FormatBooleanValue(
                            support);
                }

                string? material =
                    FindJsonValue(
                        root,
                        "filament_type");

                if (!string.IsNullOrWhiteSpace(material))
                {
                    result.Material =
                        material;
                }
            }
            catch
            {
                // Nicht jede 3MF-Datei enthält
                // project_settings.config im gleichen Format.
            }
        }

        private static ZipArchiveEntry? FindEntry(
            ZipArchive archive,
            string path)
        {
            return archive.Entries.FirstOrDefault(
                entry =>
                    entry.FullName.Equals(
                        path,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static string? FindMetadataValue(
            XElement parent,
            string key)
        {
            XElement? element =
                parent
                    .Descendants()
                    .FirstOrDefault(
                        x =>
                        {
                            string? keyValue =
                                GetAttributeValue(
                                    x,
                                    "key");

                            return string.Equals(
                                keyValue,
                                key,
                                StringComparison.OrdinalIgnoreCase);
                        });

            if (element is null)
            {
                return null;
            }

            string? value =
                GetAttributeValue(
                    element,
                    "value");

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return element.Value;
        }

        private static string? GetAttributeValue(
            XElement element,
            string name)
        {
            return element
                .Attributes()
                .FirstOrDefault(
                    attribute =>
                        attribute.Name.LocalName.Equals(
                            name,
                            StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }

        private static string? FindJsonValue(
            JsonElement element,
            string propertyName)
        {
            if (element.ValueKind ==
                JsonValueKind.Object)
            {
                foreach (
                    JsonProperty property
                    in element.EnumerateObject())
                {
                    if (property.Name.Equals(
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return JsonElementToString(
                            property.Value);
                    }

                    string? nested =
                        FindJsonValue(
                            property.Value,
                            propertyName);

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }
            else if (
                element.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    JsonElement item
                    in element.EnumerateArray())
                {
                    string? nested =
                        FindJsonValue(
                            item,
                            propertyName);

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return null;
        }

        private static string? JsonElementToString(
            JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    return element.GetRawText();

                case JsonValueKind.True:
                    return "true";

                case JsonValueKind.False:
                    return "false";

                case JsonValueKind.Array:
                {
                    JsonElement[] values =
                        element
                            .EnumerateArray()
                            .ToArray();

                    if (values.Length > 0)
                    {
                        return JsonElementToString(
                            values[0]);
                    }

                    break;
                }
            }

            return null;
        }

        private static bool TryParseDouble(
            string? value,
            out double result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }

        private static string FormatPrintTime(
            double totalSeconds)
        {
            TimeSpan time =
                TimeSpan.FromSeconds(
                    totalSeconds);

            if (time.TotalHours >= 1)
            {
                return
                    $"{(int)time.TotalHours} Std. " +
                    $"{time.Minutes} Min. " +
                    $"{time.Seconds} Sek.";
            }

            if (time.TotalMinutes >= 1)
            {
                return
                    $"{time.Minutes} Min. " +
                    $"{time.Seconds} Sek.";
            }

            return
                $"{time.Seconds} Sek.";
        }

        private static string FormatBooleanValue(
            string value)
        {
            string normalized =
                value
                    .Trim()
                    .ToLowerInvariant();

            if (normalized == "true" ||
                normalized == "1")
            {
                return "Ja";
            }

            if (normalized == "false" ||
                normalized == "0")
            {
                return "Nein";
            }

            return value;
        }
    }
}