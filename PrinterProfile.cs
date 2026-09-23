using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ThreeMFExplorer
{
    public sealed class PrinterProfile
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public double Width { get; init; }
        public double Depth { get; init; }
        public double Height { get; init; }

        public string BuildVolumeText =>
            $"{Width:0} × {Depth:0} × {Height:0} mm";

        public override string ToString() => Name;

        public static PrinterProfile BambuLabA1Mini { get; } = new()
        {
            Id = "bambu-lab-a1-mini",
            Name = "Bambu Lab A1 mini",
            Width = 180.0,
            Depth = 180.0,
            Height = 180.0
        };

        public static PrinterProfile BambuLabA1 { get; } = new()
        {
            Id = "bambu-lab-a1",
            Name = "Bambu Lab A1",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabA2L { get; } = new()
        {
            Id = "bambu-lab-a2l",
            Name = "Bambu Lab A2L",
            Width = 330.0,
            Depth = 320.0,
            Height = 325.0
        };

        public static PrinterProfile BambuLabP1P { get; } = new()
        {
            Id = "bambu-lab-p1p",
            Name = "Bambu Lab P1P",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabP1S { get; } = new()
        {
            Id = "bambu-lab-p1s",
            Name = "Bambu Lab P1S",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabP2S { get; } = new()
        {
            Id = "bambu-lab-p2s",
            Name = "Bambu Lab P2S",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabX1 { get; } = new()
        {
            Id = "bambu-lab-x1",
            Name = "Bambu Lab X1",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabX1C { get; } = new()
        {
            Id = "bambu-lab-x1c",
            Name = "Bambu Lab X1 Carbon",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabX1E { get; } = new()
        {
            Id = "bambu-lab-x1e",
            Name = "Bambu Lab X1E",
            Width = 256.0,
            Depth = 256.0,
            Height = 256.0
        };

        public static PrinterProfile BambuLabX2D { get; } = new()
        {
            Id = "bambu-lab-x2d",
            Name = "Bambu Lab X2D",
            Width = 256.0,
            Depth = 256.0,
            Height = 260.0
        };

        public static PrinterProfile BambuLabH2S { get; } = new()
        {
            Id = "bambu-lab-h2s",
            Name = "Bambu Lab H2S",
            Width = 340.0,
            Depth = 320.0,
            Height = 340.0
        };

        public static PrinterProfile BambuLabH2D { get; } = new()
        {
            Id = "bambu-lab-h2d",
            Name = "Bambu Lab H2D",
            // Official single-nozzle printable volume.
            // The total dual-nozzle envelope is wider.
            Width = 325.0,
            Depth = 320.0,
            Height = 325.0
        };

        public static PrinterProfile BambuLabH2C { get; } = new()
        {
            Id = "bambu-lab-h2c",
            Name = "Bambu Lab H2C",
            // Official single-nozzle printable volume.
            // The total multi-nozzle envelope is wider.
            Width = 305.0,
            Depth = 320.0,
            Height = 325.0
        };

        public static IReadOnlyList<PrinterProfile> All { get; } =
            new List<PrinterProfile>
            {
                BambuLabA1Mini,
                BambuLabA1,
                BambuLabA2L,
                BambuLabP1P,
                BambuLabP1S,
                BambuLabP2S,
                BambuLabX1,
                BambuLabX1C,
                BambuLabX1E,
                BambuLabX2D,
                BambuLabH2S,
                BambuLabH2D,
                BambuLabH2C
            };

        private static readonly string SelectionFile =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3MF-Explorer",
                "printer-profile.txt");

        private static readonly string LegacySelectionFile =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3MF-Explorer",
                "printer-profile.txt");

        public static PrinterProfile LoadSelected()
        {
            try
            {
                string sourceFile =
                    File.Exists(SelectionFile)
                        ? SelectionFile
                        : LegacySelectionFile;

                if (File.Exists(sourceFile))
                {
                    string id = File.ReadAllText(sourceFile).Trim();
                    PrinterProfile? profile = All.FirstOrDefault(p =>
                        string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

                    if (profile is not null)
                        return profile;
                }
            }
            catch
            {
            }

            return BambuLabX1C;
        }

        public static void SaveSelected(PrinterProfile profile)
        {
            try
            {
                string? directory = Path.GetDirectoryName(SelectionFile);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(SelectionFile, profile.Id);
            }
            catch
            {
            }
        }

        public static PrinterProfile FindById(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                PrinterProfile? profile = All.FirstOrDefault(p =>
                    string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

                if (profile is not null)
                    return profile;
            }

            return BambuLabX1C;
        }
    }
}
