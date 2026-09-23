using System.IO;
using System.IO.Compression;

namespace ThreeMFExplorer
{
    public static class ThumbnailExtractor
    {
        public static string? ExtractPreview(string fileName)
        {
            string tempFolder =
                Path.Combine(
                    Path.GetTempPath(),
                    "3MF-Explorer");

            Directory.CreateDirectory(tempFolder);

            using ZipArchive archive =
                ZipFile.OpenRead(fileName);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string name =
                    entry.FullName.Replace("\\", "/");

                if (name.Equals(
                    "Metadata/plate_1.png",
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    string targetFile =
                        Path.Combine(
                            tempFolder,
                            Path.GetFileNameWithoutExtension(fileName)
                            + "_preview.png");

                    entry.ExtractToFile(
                        targetFile,
                        true);

                    return targetFile;
                }
            }

            return null;
        }
    }
}