using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ThreeMFExplorer
{
    public sealed class UpdateService
    {
        private static readonly HttpClient Client = new();

        public UpdateService()
        {
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "3MF-Explorer-Updater");
        }

        public async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            string url =
                $"https://api.github.com/repos/{GitHubInfo.Owner}/{GitHubInfo.Repository}/releases/latest";

            using HttpResponseMessage response =
                await Client.GetAsync(url);

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

            if (Version.TryParse(current, out Version? currentParsed) &&
                Version.TryParse(latest, out Version? latestParsed))
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
                release.Assets.FirstOrDefault(a =>
                    string.Equals(
                        a.Name,
                        GitHubInfo.ReleaseAssetName,
                        StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                throw new InvalidOperationException(
                    $"Das GitHub-Release enthält nicht die erwartete Datei " +
                    $"'{GitHubInfo.ReleaseAssetName}'.");
            }

            string tempZip =
                Path.Combine(
                    Path.GetTempPath(),
                    $"3MF-Explorer-{Guid.NewGuid():N}.zip");

            string tempExtract =
                Path.Combine(
                    Path.GetTempPath(),
                    $"3MF-Explorer-{Guid.NewGuid():N}");

            string applicationDirectory =
                AppContext.BaseDirectory.TrimEnd(
                    Path.DirectorySeparatorChar);

            string executablePath =
                Environment.ProcessPath ??
                Path.Combine(
                    applicationDirectory,
                    "3MF-Explorer.exe");

            await using (Stream source =
                await Client.GetStreamAsync(
                    asset.BrowserDownloadUrl))

            await using (FileStream target =
                File.Create(tempZip))
            {
                await source.CopyToAsync(target);
            }

            Directory.CreateDirectory(tempExtract);

            ZipFile.ExtractToDirectory(
                tempZip,
                tempExtract);

            string scriptPath =
                Path.Combine(
                    Path.GetTempPath(),
                    $"3MF-Explorer-update-{Guid.NewGuid():N}.ps1");

            string script = $"""
param()

Start-Sleep -Seconds 2

Copy-Item -Path '{tempExtract.Replace("'", "''")}\\*' `
    -Destination '{applicationDirectory.Replace("'", "''")}' `
    -Recurse -Force

Start-Process -FilePath '{executablePath.Replace("'", "''")}'

Remove-Item -LiteralPath '{tempZip.Replace("'", "''")}' `
    -Force -ErrorAction SilentlyContinue

Remove-Item -LiteralPath '{tempExtract.Replace("'", "''")}' `
    -Recurse -Force -ErrorAction SilentlyContinue

Remove-Item -LiteralPath $PSCommandPath `
    -Force -ErrorAction SilentlyContinue
""";

            await File.WriteAllTextAsync(
                scriptPath,
                script);

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "powershell.exe",

                    Arguments =
                        $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",

                    UseShellExecute = false,

                    CreateNoWindow = true
                });

            Application.Current.Shutdown();
        }
    }

    public sealed class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName(
            "tag_name")]

        public string TagName { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName(
            "name")]

        public string Name { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName(
            "assets")]

        public System.Collections.Generic.List<GitHubAsset> Assets { get; set; } = new();
    }

    public sealed class GitHubAsset
    {
        [System.Text.Json.Serialization.JsonPropertyName(
            "name")]

        public string Name { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName(
            "browser_download_url")]

        public string BrowserDownloadUrl { get; set; } = "";
    }
}