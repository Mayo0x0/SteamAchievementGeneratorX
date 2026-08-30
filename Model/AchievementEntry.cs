using System;
using System.Collections.Generic;

namespace SteamAchievementGenerator.Model
{
    public sealed class AchievementEntry
    {
        /// <summary>The Steam API name, e.g. "ACHIEVEMENT_Upload_Map".</summary>
        public string ApiName { get; set; }

        /// <summary>English name, as printed by SteamDB.</summary>
        public string DisplayName { get; set; }

        /// <summary>English description, as printed by SteamDB.</summary>
        public string Description { get; set; }

        public bool Hidden { get; set; }

        /// <summary>Global unlock rate in percent as shown by SteamDB, or null when unknown.</summary>
        public double? UnlockPercentage { get; set; }

        /// <summary>Icon shown once the achievement is unlocked.</summary>
        public ImageRef Icon { get; set; }

        /// <summary>Greyed out icon shown while the achievement is locked.</summary>
        public ImageRef IconGray { get; set; }

        /// <summary>Steam language name -&gt; translated name, filled from a community page.</summary>
        public Dictionary<string, string> LocalizedDisplayNames { get; private set; }

        /// <summary>Steam language name -&gt; translated description.</summary>
        public Dictionary<string, string> LocalizedDescriptions { get; private set; }

        // Filled in by the generator once the files have been written.
        public string IconRelativePath { get; set; }
        public string IconGrayRelativePath { get; set; }

        public AchievementEntry()
        {
            LocalizedDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LocalizedDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The Steam icon file name, which is how a community page row is matched to this entry.</summary>
        public string IconFileName
        {
            get { return Icon != null ? Icon.SuggestedFileName : null; }
        }

        public string GetLocalizedDisplayName(string language)
        {
            string value;
            return language != null && LocalizedDisplayNames.TryGetValue(language, out value) ? value : null;
        }

        public string GetLocalizedDescription(string language)
        {
            string value;
            return language != null && LocalizedDescriptions.TryGetValue(language, out value) ? value : null;
        }

        /// <summary>Stores a translation, or removes it when the text is blank.</summary>
        public void SetLocalized(string language, string displayName, string description)
        {
            if (string.IsNullOrEmpty(language)) return;

            Store(LocalizedDisplayNames, language, displayName);
            Store(LocalizedDescriptions, language, description);
        }

        private static void Store(Dictionary<string, string> target, string language, string value)
        {
            if (value != null) value = value.Trim();

            if (string.IsNullOrEmpty(value)) target.Remove(language);
            else target[language] = value;
        }

        /// <summary>True when at least one of the two fields has text for that language.</summary>
        public bool HasLocalization(string language)
        {
            return !string.IsNullOrEmpty(GetLocalizedDisplayName(language))
                || !string.IsNullOrEmpty(GetLocalizedDescription(language));
        }
    }
}
