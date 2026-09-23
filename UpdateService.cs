using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace ThreeMFExplorer
{
    public sealed class UpdateService
    {
        private static readonly HttpClient Client =
            CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new();

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "3MF-Explorer-Updater");

            return client;
        }

        public async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            string url =
                $"https://api.github.com/repos/{GitHubInfo.Owner}/{GitHubInfo.Repository}/releases/latest";

            using HttpResponseMessage response =
                await Client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            await using Stream stream =
                await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<GitHubRelease>(
                stream);
        }

        public bool IsNewerVersion(
            string currentVersion,
            string latestVersion)
        {
            string current =
                currentVersion.TrimStart('v', 'V');

            string latest =
                latestVersion.TrimStart('v', 'V');

            if (Version.TryParse(
                    current,
                    out Version? currentParsed) &&
                Version.TryParse(
                    latest,
                    out Version? latestParsed))
            {
                return latestParsed > currentParsed;
            }

            return !string.Equals(
                current,
                latest,
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task InstallAsync(
            GitHubRelease release)
        {
            GitHubAsset? asset =
                release.Assets.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Name,
                            GitHubInfo.ReleaseAssetName,
                            StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                throw new InvalidOperationException(
                    $"Das GitHub-Release enthält nicht die erwartete Datei " +
                    $"'{GitHubInfo.ReleaseAssetName}'.");
            }

            if (string.IsNullOrWhiteSpace(
                    asset.BrowserDownloadUrl))
            {
                throw new InvalidOperationException(
                    "Die Download-Adresse des Update-Assets ist leer.");
            }

            string tempZip =
                Path.Combine(
                    Path.GetTempPath(),
                    $"3MF-Explorer-{Guid.NewGuid():N}.zip");

            string tempExtract =
                Path.Combine(
                    Path.GetTempPath(),
                    $"3MF-Explorer-{Guid.NewGuid():N}");

            string scriptPath =
                Path.Combine(
                    Path.GetTempPath(),
                    $"3MF-Explorer-update-{Guid.NewGuid():N}.ps1");

            bool updaterStarted = false;

            try
            {
                string applicationDirectory =
                    AppContext.BaseDirectory.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                string executablePath =
                    Environment.ProcessPath ??
                    Path.Combine(
                        applicationDirectory,
                        "3MF-Explorer.exe");

                int currentProcessId =
                    Environment.ProcessId;

                using HttpResponseMessage downloadResponse =
                    await Client.GetAsync(
                        asset.BrowserDownloadUrl,
                        HttpCompletionOption.ResponseHeadersRead);

                downloadResponse.EnsureSuccessStatusCode();

                await using (Stream source =
                    await downloadResponse.Content.ReadAsStreamAsync())

                await using (FileStream target =
                    new FileStream(
                        tempZip,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        FileOptions.Asynchronous |
                        FileOptions.SequentialScan))
                {
                    await source.CopyToAsync(target);
                }

                ValidateUpdateArchive(
                    tempZip);

                Directory.CreateDirectory(
                    tempExtract);

                ZipFile.ExtractToDirectory(
                    tempZip,
                    tempExtract);

                string extractedExecutable =
                    Path.Combine(
                        tempExtract,
                        "3MF-Explorer.exe");

                if (!File.Exists(
                        extractedExecutable))
                {
                    throw new InvalidDataException(
                        "Die Update-ZIP enthält keine '3MF-Explorer.exe' " +
                        "im Stammverzeichnis.");
                }

                string script =
                    "param(\r\n" +
                    "    [Parameter(Mandatory = $true)]\r\n" +
                    "    [int]$ProcessId\r\n" +
                    ")\r\n" +
                    "\r\n" +
                    "$ErrorActionPreference = 'Stop'\r\n" +
                    "\r\n" +
                    "try\r\n" +
                    "{\r\n" +
                    "    Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue\r\n" +
                    "    Start-Sleep -Milliseconds 500\r\n" +
                    "\r\n" +
                    "    Copy-Item -Path '" +
                    EscapePowerShellLiteral(tempExtract) +
                    "\\*' -Destination '" +
                    EscapePowerShellLiteral(applicationDirectory) +
                    "' -Recurse -Force\r\n" +
                    "\r\n" +
                    "    Start-Process -FilePath '" +
                    EscapePowerShellLiteral(executablePath) +
                    "'\r\n" +
                    "}\r\n" +
                    "finally\r\n" +
                    "{\r\n" +
                    "    Remove-Item -LiteralPath '" +
                    EscapePowerShellLiteral(tempZip) +
                    "' -Force -ErrorAction SilentlyContinue\r\n" +
                    "\r\n" +
                    "    Remove-Item -LiteralPath '" +
                    EscapePowerShellLiteral(tempExtract) +
                    "' -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                    "\r\n" +
                    "    Remove-Item -LiteralPath $PSCommandPath " +
                    "-Force -ErrorAction SilentlyContinue\r\n" +
                    "}\r\n";

                await File.WriteAllTextAsync(
                    scriptPath,
                    script);

                Process? updaterProcess =
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName =
                                "powershell.exe",

                            Arguments =
                                $"-NoProfile -ExecutionPolicy Bypass " +
                                $"-File \"{scriptPath}\" " +
                                $"-ProcessId {currentProcessId}",

                            UseShellExecute =
                                false,

                            CreateNoWindow =
                                true
                        });

                if (updaterProcess is null)
                {
                    throw new InvalidOperationException(
                        "Der Update-Prozess konnte nicht gestartet werden.");
                }

                updaterStarted = true;

                Application.Current.Shutdown();
            }
            catch
            {
                if (!updaterStarted)
                {
                    TryDeleteFile(
                        tempZip);

                    TryDeleteDirectory(
                        tempExtract);

                    TryDeleteFile(
                        scriptPath);
                }

                throw;
            }
        }

        private static void ValidateUpdateArchive(
            string zipPath)
        {
            using ZipArchive archive =
                ZipFile.OpenRead(
                    zipPath);

            ZipArchiveEntry? executableEntry =
                archive.Entries.FirstOrDefault(
                    entry =>
                        string.Equals(
                            entry.FullName.Replace('\\', '/'),
                            "3MF-Explorer.exe",
                            StringComparison.OrdinalIgnoreCase));

            if (executableEntry is null)
            {
                throw new InvalidDataException(
                    "Die Update-ZIP ist ungültig. " +
                    "'3MF-Explorer.exe' muss direkt im Stammverzeichnis liegen.");
            }
        }

        private static string EscapePowerShellLiteral(
            string value)
        {
            return value.Replace(
                "'",
                "''");
        }

        private static void TryDeleteFile(
            string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Cleanup must never hide the actual update error.
            }
        }

        private static void TryDeleteDirectory(
            string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(
                        path,
                        recursive: true);
                }
            }
            catch
            {
                // Cleanup must never hide the actual update error.
            }
        }
    }

    public sealed class GitHubRelease
    {
        [JsonPropertyName(
            "tag_name")]
        public string TagName { get; set; } =
            string.Empty;

        [JsonPropertyName(
            "name")]
        public string Name { get; set; } =
            string.Empty;

        [JsonPropertyName(
            "assets")]
        public List<GitHubAsset> Assets { get; set; } =
            new();
    }

    public sealed class GitHubAsset
    {
        [JsonPropertyName(
            "name")]
        public string Name { get; set; } =
            string.Empty;

        [JsonPropertyName(
            "browser_download_url")]
        public string BrowserDownloadUrl { get; set; } =
            string.Empty;
    }
}
