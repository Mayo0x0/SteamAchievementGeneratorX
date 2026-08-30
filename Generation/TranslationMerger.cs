using System;
using System.Collections.Generic;
using System.Globalization;
using SteamAchievementGenerator.Model;

namespace SteamAchievementGenerator.Generation
{
    public sealed class TranslationReport
    {
        public string Language { get; set; }
        public int Matched { get; set; }

        /// <summary>Achievements from SteamDB that the localized page did not cover.</summary>
        public List<AchievementEntry> Untranslated { get; private set; }

        /// <summary>Rows of the localized page that could not be assigned to an achievement.</summary>
        public List<LocalizedAchievement> Unassigned { get; private set; }

        public List<string> Notes { get; private set; }

        public TranslationReport()
        {
            Untranslated = new List<AchievementEntry>();
            Unassigned = new List<LocalizedAchievement>();
            Notes = new List<string>();
        }
    }

    /// <summary>
    /// Joins a Steam Community achievement page onto the achievements read from SteamDB.
    ///
    /// The community page has no API names, so the join runs on the Steam icon file name,
    /// which both pages reference. Anything left over afterwards is matched by its global
    /// unlock rate, but only where that rate is unique on both sides - a wrong translation
    /// is worse than a missing one, and missing ones are reported for manual fixing.
    /// </summary>
    public static class TranslationMerger
    {
        public static TranslationReport Apply(
            IList<AchievementEntry> achievements,
            TranslationSet translations,
            string language)
        {
            var report = new TranslationReport();
            report.Language = language;

            if (achievements == null || translations == null || string.IsNullOrEmpty(language))
                return report;

            var remainingAchievements = new List<AchievementEntry>(achievements);
            var remainingRows = new List<LocalizedAchievement>(translations.Achievements);

            MatchByIcon(remainingAchievements, remainingRows, language, report);
            MatchByUniqueUnlockRate(remainingAchievements, remainingRows, language, report);

            report.Untranslated.AddRange(remainingAchievements);
            report.Unassigned.AddRange(remainingRows);

            return report;
        }

        private static void MatchByIcon(
            List<AchievementEntry> achievements,
            List<LocalizedAchievement> rows,
            string language,
            TranslationReport report)
        {
            var byIcon = new Dictionary<string, AchievementEntry>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var achievement in achievements)
            {
                string icon = achievement.IconFileName;
                if (string.IsNullOrEmpty(icon)) continue;

                if (byIcon.ContainsKey(icon)) ambiguous.Add(icon);
                else byIcon[icon] = achievement;
            }

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                var row = rows[i];
                if (string.IsNullOrEmpty(row.IconFileName)) continue;
                if (ambiguous.Contains(row.IconFileName)) continue;

                AchievementEntry target;
                if (!byIcon.TryGetValue(row.IconFileName, out target)) continue;

                target.SetLocalized(language, row.DisplayName, row.Description);
                report.Matched++;

                achievements.Remove(target);
                byIcon.Remove(row.IconFileName);
                rows.RemoveAt(i);
            }

            if (ambiguous.Count > 0)
            {
                report.Notes.Add(ambiguous.Count +
                    " icon file names are shared by several achievements; those were matched by unlock rate instead.");
            }
        }

        private static void MatchByUniqueUnlockRate(
            List<AchievementEntry> achievements,
            List<LocalizedAchievement> rows,
            string language,
            TranslationReport report)
        {
            if (achievements.Count == 0 || rows.Count == 0) return;

            var achievementsByRate = GroupAchievements(achievements);
            var rowsByRate = GroupRows(rows);

            foreach (var pair in achievementsByRate)
            {
                if (pair.Value.Count != 1) continue;

                List<LocalizedAchievement> candidates;
                if (!rowsByRate.TryGetValue(pair.Key, out candidates)) continue;
                if (candidates.Count != 1) continue;

                var achievement = pair.Value[0];
                var row = candidates[0];

                achievement.SetLocalized(language, row.DisplayName, row.Description);
                report.Matched++;
                report.Notes.Add("'" + achievement.ApiName + "' was matched by its unlock rate (" +
                                 pair.Key + "%), not by its icon - please double check it.");

                achievements.Remove(achievement);
                rows.Remove(row);
            }
        }

        private static Dictionary<string, List<AchievementEntry>> GroupAchievements(List<AchievementEntry> achievements)
        {
            var map = new Dictionary<string, List<AchievementEntry>>(StringComparer.Ordinal);

            foreach (var achievement in achievements)
            {
                if (!achievement.UnlockPercentage.HasValue) continue;

                string key = Key(achievement.UnlockPercentage.Value);
                if (!map.ContainsKey(key)) map[key] = new List<AchievementEntry>();
                map[key].Add(achievement);
            }

            return map;
        }

        private static Dictionary<string, List<LocalizedAchievement>> GroupRows(List<LocalizedAchievement> rows)
        {
            var map = new Dictionary<string, List<LocalizedAchievement>>(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                if (!row.UnlockPercentage.HasValue) continue;

                string key = Key(row.UnlockPercentage.Value);
                if (!map.ContainsKey(key)) map[key] = new List<LocalizedAchievement>();
                map[key].Add(row);
            }

            return map;
        }

        private static string Key(double percentage)
        {
            return percentage.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
