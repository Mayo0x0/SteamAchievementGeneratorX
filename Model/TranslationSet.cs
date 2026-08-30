using System.Collections.Generic;

namespace SteamAchievementGenerator.Model
{
    /// <summary>One achievement as it appears on a Steam Community achievement page.</summary>
    public sealed class LocalizedAchievement
    {
        /// <summary>
        /// The Steam icon file name, e.g. "b7068c06....jpg". The community page and SteamDB
        /// serve the very same file, which makes this the join key between the two pages -
        /// the community page never shows the API name.
        /// </summary>
        public string IconFileName { get; set; }

        public string DisplayName { get; set; }
        public string Description { get; set; }

        /// <summary>Global unlock rate, used as a secondary match when the icon differs.</summary>
        public double? UnlockPercentage { get; set; }

        /// <summary>Position on the page, for diagnostics.</summary>
        public int Index { get; set; }
    }

    /// <summary>The result of reading one Steam Community achievement page.</summary>
    public sealed class TranslationSet
    {
        /// <summary>Steam language name, e.g. "german". Null when it could not be detected.</summary>
        public string Language { get; set; }

        /// <summary>The raw html lang attribute, for the log.</summary>
        public string DetectedTag { get; set; }

        public string AppId { get; set; }
        public string GameName { get; set; }

        public List<LocalizedAchievement> Achievements { get; set; }
        public List<string> Warnings { get; set; }

        public TranslationSet()
        {
            Achievements = new List<LocalizedAchievement>();
            Warnings = new List<string>();
        }
    }
}
