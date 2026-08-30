using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamAchievementGenerator.Model;

namespace SteamAchievementGenerator.Generation
{
    public sealed class GenerationProgress
    {
        public int Completed { get; set; }
        public int Total { get; set; }
        public string Message { get; set; }
    }

    public sealed class GenerationReport
    {
        public string OutputDirectory { get; set; }
        public int AchievementsWritten { get; set; }
        public int StatsWritten { get; set; }
        public int IconsWritten { get; set; }
        public int IconsMissing { get; set; }
        public List<string> Warnings { get; private set; }
        public List<string> FilesWritten { get; private set; }

        public GenerationReport()
        {
            Warnings = new List<string>();
            FilesWritten = new List<string>();
        }
    }

    /// <summary>
    /// Writes a steam_settings folder for the gbe_fork Goldberg emulator
    /// (https://github.com/Detanup01/gbe_fork).
    ///
    /// Layout produced:
    ///   steam_settings/steam_appid.txt
    ///   steam_settings/achievements.json
    ///   steam_settings/stats.json
    ///   steam_settings/images/*.jpg
    /// </summary>
    public sealed class SteamSettingsWriter
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly GeneratorOptions _options;

        public SteamSettingsWriter(GeneratorOptions options)
        {
            _options = options ?? new GeneratorOptions();
        }

        public async Task<GenerationReport> GenerateAsync(
            ParseResult parsed,
            string htmlFilePath,
            HttpClient http,
            IProgress<GenerationProgress> progress,
            CancellationToken cancellationToken)
        {
            if (parsed == null) throw new ArgumentNullException("parsed");
            if (string.IsNullOrEmpty(_options.OutputDirectory))
                throw new InvalidOperationException("No output directory configured.");

            var report = new GenerationReport();
            report.OutputDirectory = _options.OutputDirectory;

            Directory.CreateDirectory(_options.OutputDirectory);

            string imagesDir = Path.Combine(_options.OutputDirectory, "images");
            bool needImages = _options.WriteAchievements && parsed.Achievements.Count > 0;

            if (needImages)
            {
                if (_options.CleanImagesFolder && Directory.Exists(imagesDir))
                    Directory.Delete(imagesDir, true);
                Directory.CreateDirectory(imagesDir);
            }

            var resolver = new IconResolver(http, parsed.Game != null ? parsed.Game.AppId : null, htmlFilePath);
            resolver.AllowDownloads = _options.DownloadMissingIcons;

            int total = needImages ? parsed.Achievements.Count : 0;
            total += 3; // achievements.json + stats.json + steam_appid.txt
            int done = 0;

            // ---------------------------------------------------------------- icons
            if (needImages)
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var achievement in parsed.Achievements)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Report(progress, ++done, total, "Icons: " + achievement.ApiName);

                    achievement.IconRelativePath = await WriteIconAsync(
                        resolver, imagesDir, usedNames, achievement, achievement.Icon, false, report, cancellationToken)
                        .ConfigureAwait(false);

                    achievement.IconGrayRelativePath = await WriteIconAsync(
                        resolver, imagesDir, usedNames, achievement, achievement.IconGray, true, report, cancellationToken)
                        .ConfigureAwait(false);

                    // A missing greyscale icon leaves the overlay with no image at all,
                    // so fall back to the unlocked one rather than an empty path.
                    if (string.IsNullOrEmpty(achievement.IconGrayRelativePath) &&
                        !string.IsNullOrEmpty(achievement.IconRelativePath))
                    {
                        achievement.IconGrayRelativePath = achievement.IconRelativePath;
                        report.Warnings.Add(
                            "Using the unlocked icon as the locked icon for '" + achievement.ApiName + "'.");
                    }
                }
            }

            foreach (string warning in resolver.Warnings) report.Warnings.Add(warning);

            // --------------------------------------------------------- achievements
            if (_options.WriteAchievements)
            {
                Report(progress, ++done, total, "Writing achievements.json");

                string path = Path.Combine(_options.OutputDirectory, "achievements.json");
                File.WriteAllText(path, BuildAchievementsJson(parsed.Achievements), Utf8NoBom);

                report.AchievementsWritten = parsed.Achievements.Count;
                report.FilesWritten.Add(path);
            }
            else
            {
                done++;
            }

            // ----------------------------------------------------------------- stats
            if (_options.WriteStats && parsed.Stats.Count > 0)
            {
                Report(progress, ++done, total, "Writing stats.json");

                string path = Path.Combine(_options.OutputDirectory, "stats.json");
                File.WriteAllText(path, BuildStatsJson(parsed.Stats), Utf8NoBom);

                report.StatsWritten = parsed.Stats.Count;
                report.FilesWritten.Add(path);
            }
            else
            {
                done++;
                if (_options.WriteStats)
                    report.Warnings.Add("The page contains no stats table, so stats.json was not written.");
            }

            // ------------------------------------------------------------- steam_appid
            if (_options.WriteAppIdFile)
            {
                Report(progress, ++done, total, "Writing steam_appid.txt");

                if (parsed.Game != null && !string.IsNullOrEmpty(parsed.Game.AppId))
                {
                    string path = Path.Combine(_options.OutputDirectory, "steam_appid.txt");
                    File.WriteAllText(path, parsed.Game.AppId, Utf8NoBom);
                    report.FilesWritten.Add(path);
                }
                else
                {
                    report.Warnings.Add("No App ID available - steam_appid.txt was not written.");
                }
            }
            else
            {
                done++;
            }

            Report(progress, total, total, "Done");
            return report;
        }

        // ------------------------------------------------------------------- icons

        private async Task<string> WriteIconAsync(
            IconResolver resolver,
            string imagesDir,
            HashSet<string> usedNames,
            AchievementEntry achievement,
            ImageRef image,
            bool gray,
            GenerationReport report,
            CancellationToken cancellationToken)
        {
            if (image == null || image.IsEmpty)
            {
                report.IconsMissing++;
                return null;
            }

            byte[] bytes = await resolver.ResolveAsync(image, cancellationToken).ConfigureAwait(false);
            if (bytes == null || bytes.Length == 0)
            {
                report.IconsMissing++;
                report.Warnings.Add(
                    "No " + (gray ? "locked" : "unlocked") + " icon for '" + achievement.ApiName + "'.");
                return null;
            }

            string fileName = BuildIconFileName(achievement, image, gray, usedNames);
            string fullPath = Path.Combine(imagesDir, fileName);

            File.WriteAllBytes(fullPath, bytes);
            report.IconsWritten++;

            return "images/" + fileName;
        }

        private string BuildIconFileName(AchievementEntry achievement, ImageRef image, bool gray, HashSet<string> usedNames)
        {
            string extension = image.Extension;
            string stem;

            if (_options.IconNaming == IconNaming.SteamFileName && !string.IsNullOrEmpty(image.SuggestedFileName))
            {
                stem = Path.GetFileNameWithoutExtension(image.SuggestedFileName);
                extension = Path.GetExtension(image.SuggestedFileName);
                if (string.IsNullOrEmpty(extension)) extension = image.Extension;
            }
            else
            {
                stem = Sanitize(achievement.ApiName);
                if (gray) stem += "_gray";
            }

            if (string.IsNullOrEmpty(stem)) stem = gray ? "achievement_gray" : "achievement";

            string candidate = stem + extension;
            int suffix = 1;
            while (!usedNames.Add(candidate))
            {
                candidate = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture) + extension;
                suffix++;
            }

            return candidate;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c == '.' || c == '-' || c == '_' || char.IsLetterOrDigit(c)) sb.Append(c);
                else sb.Append('_');
            }

            string result = sb.ToString().Trim('.', ' ');

            // Reserved DOS device names would make the file unopenable.
            string upper = result.ToUpperInvariant();
            switch (upper)
            {
                case "CON": case "PRN": case "AUX": case "NUL":
                case "COM1": case "COM2": case "COM3": case "COM4": case "COM5":
                case "COM6": case "COM7": case "COM8": case "COM9":
                case "LPT1": case "LPT2": case "LPT3": case "LPT4": case "LPT5":
                case "LPT6": case "LPT7": case "LPT8": case "LPT9":
                    result = "_" + result;
                    break;
            }

            if (result.Length > 100) result = result.Substring(0, 100);
            return result.Length == 0 ? null : result;
        }

        // -------------------------------------------------------------------- json

        private string BuildAchievementsJson(List<AchievementEntry> achievements)
        {
            var array = new JArray();

            foreach (var achievement in achievements)
            {
                var item = new JObject();
                item["name"] = achievement.ApiName;

                string displayName = achievement.DisplayName ?? "";
                string description = achievement.Description ?? "";

                if (_options.LocalizedTextObjects)
                {
                    item["displayName"] = new JObject { { "english", displayName } };
                    item["description"] = new JObject { { "english", description } };
                }
                else
                {
                    item["displayName"] = displayName;
                    item["description"] = description;
                }

                item["hidden"] = achievement.Hidden ? "1" : "0";

                string icon = achievement.IconRelativePath ?? "";
                string iconGray = achievement.IconGrayRelativePath ?? "";

                item["icon"] = icon;
                item["icon_gray"] = iconGray;   // gbe_fork, current format
                item["icongray"] = iconGray;    // Goldberg / Achievement Watcher, legacy format

                array.Add(item);
            }

            return array.ToString(Formatting.Indented) + Environment.NewLine;
        }

        private static string BuildStatsJson(List<StatEntry> stats)
        {
            var array = new JArray();

            foreach (var stat in stats)
            {
                var item = new JObject();
                item["name"] = stat.ApiName;
                item["type"] = stat.TypeToken;
                item["default"] = stat.NormalizedDefaultValue;
                item["global"] = string.IsNullOrEmpty(stat.GlobalValue) ? "0" : stat.GlobalValue;
                array.Add(item);
            }

            return array.ToString(Formatting.Indented) + Environment.NewLine;
        }

        private static void Report(IProgress<GenerationProgress> progress, int done, int total, string message)
        {
            if (progress == null) return;
            progress.Report(new GenerationProgress { Completed = done, Total = total, Message = message });
        }
    }
}
