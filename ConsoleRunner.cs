using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SteamAchievementGenerator.Generation;
using SteamAchievementGenerator.Model;
using SteamAchievementGenerator.Parsing;

namespace SteamAchievementGenerator
{
    /// <summary>
    /// Headless mode. Useful for batch converting a folder full of saved SteamDB pages,
    /// and it is what the automated checks drive.
    /// </summary>
    internal static class ConsoleRunner
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private const int AttachParentProcess = -1;

        /// <summary>
        /// A bare file path means "open this in the window" (drag &amp; drop onto the exe,
        /// or an Explorer file association). Only an explicit switch starts headless mode.
        /// </summary>
        public static bool WantsConsole(string[] args)
        {
            if (args == null) return false;

            foreach (string arg in args)
                if (!string.IsNullOrEmpty(arg) && arg[0] == '-') return true;

            return false;
        }

        public static int Run(string[] args)
        {
            if (!AttachConsole(AttachParentProcess)) AllocConsole();

            try
            {
                return RunAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static async Task<int> RunAsync(string[] args)
        {
            string input = null;
            string output = null;
            string translationFile = null;
            string language = null;
            var options = new GeneratorOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg.ToLowerInvariant())
                {
                    case "-i":
                    case "--input":
                        input = Next(args, ref i);
                        break;
                    case "-o":
                    case "--output":
                        output = Next(args, ref i);
                        break;
                    case "-t":
                    case "--translation":
                        translationFile = Next(args, ref i);
                        break;
                    case "-l":
                    case "--language":
                        language = Next(args, ref i);
                        break;
                    case "--no-stats":
                        options.WriteStats = false;
                        break;
                    case "--no-achievements":
                        options.WriteAchievements = false;
                        break;
                    case "--no-download":
                        options.DownloadMissingIcons = false;
                        break;
                    case "--plain-text":
                        options.LocalizedTextObjects = false;
                        break;
                    case "--clean":
                        options.CleanImagesFolder = true;
                        break;
                    case "--icon-names":
                        options.IconNaming = string.Equals(Next(args, ref i), "steam", StringComparison.OrdinalIgnoreCase)
                            ? IconNaming.SteamFileName
                            : IconNaming.ApiName;
                        break;
                    case "-h":
                    case "--help":
                        PrintUsage();
                        return 0;
                    default:
                        if (input == null && !arg.StartsWith("-", StringComparison.Ordinal)) input = arg;
                        else
                        {
                            Console.Error.WriteLine("Unknown argument: " + arg);
                            PrintUsage();
                            return 2;
                        }
                        break;
                }
            }

            if (string.IsNullOrEmpty(input))
            {
                PrintUsage();
                return 2;
            }

            input = Path.GetFullPath(input);
            if (!File.Exists(input))
            {
                Console.Error.WriteLine("Input file not found: " + input);
                return 1;
            }

            Console.WriteLine("Reading " + input);
            var document = HtmlLoader.Load(input);
            var parsed = SteamDbParser.Parse(document);

            Console.WriteLine("  Game:         " + Or(parsed.Game.Name, "(unknown)"));
            Console.WriteLine("  App ID:       " + Or(parsed.Game.AppId, "(unknown)"));
            Console.WriteLine("  Developer:    " + Or(parsed.Game.Developer, "(unknown)"));
            Console.WriteLine("  Release date: " + Or(parsed.Game.ReleaseDate, "(unknown)"));
            Console.WriteLine("  Achievements: " + parsed.Achievements.Count);
            Console.WriteLine("  Stats:        " + parsed.Stats.Count);

            foreach (string warning in parsed.Warnings)
                Console.WriteLine("  ! " + warning);

            if (!string.IsNullOrEmpty(translationFile))
            {
                int failure = ApplyTranslation(parsed, translationFile, language);
                if (failure != 0) return failure;
            }

            if (parsed.Achievements.Count == 0 && parsed.Stats.Count == 0)
            {
                Console.Error.WriteLine("Nothing to generate.");
                return 1;
            }

            options.OutputDirectory = string.IsNullOrEmpty(output)
                ? Path.Combine(Path.GetDirectoryName(input), "steam_settings")
                : Path.GetFullPath(output);

            Console.WriteLine("Writing " + options.OutputDirectory);

            using (var http = HttpClientFactory.Create())
            {
                var writer = new SteamSettingsWriter(options);
                var progress = new Progress<GenerationProgress>(p =>
                {
                    if (p.Total > 0 && p.Completed % 10 == 0)
                        Console.WriteLine("  [" + p.Completed + "/" + p.Total + "] " + p.Message);
                });

                var report = await writer.GenerateAsync(parsed, input, http, progress, CancellationToken.None)
                                         .ConfigureAwait(false);

                Console.WriteLine();
                Console.WriteLine("Achievements written: " + report.AchievementsWritten);
                Console.WriteLine("Stats written:        " + report.StatsWritten);
                Console.WriteLine("Icons written:        " + report.IconsWritten);
                Console.WriteLine("Icons missing:        " + report.IconsMissing);

                PrintWarnings(report.Warnings);
                return 0;
            }
        }

        /// <summary>Reads a Steam Community achievement page and merges it into the parse result.</summary>
        private static int ApplyTranslation(ParseResult parsed, string path, string language)
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                Console.Error.WriteLine("Translation file not found: " + path);
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("Reading " + path);

            var translations = SteamCommunityParser.Parse(HtmlLoader.Load(path));

            foreach (string warning in translations.Warnings)
                Console.WriteLine("  ! " + warning);

            if (string.IsNullOrEmpty(language)) language = translations.Language;

            if (string.IsNullOrEmpty(language))
            {
                Console.Error.WriteLine(
                    "Could not tell which language that page is in (html lang=\"" +
                    Or(translations.DetectedTag, "") + "\"). Pass --language <name>, e.g. --language german.");
                return 1;
            }

            if (!SteamLanguages.IsKnown(language))
            {
                Console.WriteLine("  ! '" + language + "' is not a Steam language name; writing it verbatim.");
            }

            Console.WriteLine("  Language:     " + language +
                              (string.IsNullOrEmpty(translations.DetectedTag)
                                  ? ""
                                  : " (from lang=\"" + translations.DetectedTag + "\")"));
            Console.WriteLine("  Rows:         " + translations.Achievements.Count);

            if (!string.IsNullOrEmpty(translations.AppId) &&
                !string.IsNullOrEmpty(parsed.Game.AppId) &&
                translations.AppId != parsed.Game.AppId)
            {
                Console.WriteLine("  ! That page belongs to App ID " + translations.AppId +
                                  ", the SteamDB page to " + parsed.Game.AppId + ".");
            }

            var report = TranslationMerger.Apply(parsed.Achievements, translations, language);

            Console.WriteLine("  Translated:   " + report.Matched + " of " + parsed.Achievements.Count);

            foreach (string note in report.Notes)
                Console.WriteLine("  ! " + note);

            if (report.Untranslated.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Missing " + language + " text for " + report.Untranslated.Count +
                                  " achievements - add them by hand in achievements.json, or in the window:");
                foreach (var achievement in report.Untranslated)
                    Console.WriteLine("  - " + achievement.ApiName + "  (" + achievement.DisplayName + ")");
            }

            if (report.Unassigned.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine(report.Unassigned.Count + " rows of the localized page matched no achievement:");
                foreach (var row in report.Unassigned)
                    Console.WriteLine("  - " + row.DisplayName);
            }

            return 0;
        }

        private static void PrintWarnings(List<string> warnings)
        {
            if (warnings.Count == 0) return;

            Console.WriteLine();
            Console.WriteLine("Warnings (" + warnings.Count + "):");

            int shown = 0;
            foreach (string warning in warnings)
            {
                if (shown++ >= 25)
                {
                    Console.WriteLine("  ... and " + (warnings.Count - 25) + " more");
                    break;
                }
                Console.WriteLine("  - " + warning);
            }
        }

        private static string Next(string[] args, ref int index)
        {
            if (index + 1 >= args.Length) throw new ArgumentException("Missing value after " + args[index]);
            return args[++index];
        }

        private static string Or(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Steam Achievement Generator X");
            Console.WriteLine();
            Console.WriteLine("  SteamAchievementGenerator.exe --input <steamdb.html> [--output <folder>] [options]");
            Console.WriteLine();
            Console.WriteLine("  --output <folder>        target folder (default: steam_settings next to the HTML)");
            Console.WriteLine("  --translation <file>     saved steamcommunity.com achievements page with the");
            Console.WriteLine("                           official translations (repeatable is not supported yet)");
            Console.WriteLine("  --language <name>        Steam language name for --translation, e.g. german;");
            Console.WriteLine("                           detected from the page when omitted");
            Console.WriteLine("  --icon-names api|steam   file names for the icons (default: api)");
            Console.WriteLine("  --no-stats               do not write stats.json");
            Console.WriteLine("  --no-achievements        do not write achievements.json or icons");
            Console.WriteLine("  --no-download            never contact the Steam CDN for missing icons");
            Console.WriteLine("  --plain-text             write displayName/description as plain strings");
            Console.WriteLine("  --clean                  delete an existing images folder first");
            Console.WriteLine();
            Console.WriteLine("Start without arguments for the graphical interface.");
        }
    }
}
