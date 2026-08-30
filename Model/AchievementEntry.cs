namespace SteamAchievementGenerator.Model
{
    public sealed class AchievementEntry
    {
        /// <summary>The Steam API name, e.g. "ACHIEVEMENT_Upload_Map".</summary>
        public string ApiName { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public bool Hidden { get; set; }

        /// <summary>Global unlock rate in percent as shown by SteamDB, or null when unknown.</summary>
        public double? UnlockPercentage { get; set; }

        /// <summary>Icon shown once the achievement is unlocked.</summary>
        public ImageRef Icon { get; set; }

        /// <summary>Greyed out icon shown while the achievement is locked.</summary>
        public ImageRef IconGray { get; set; }

        // Filled in by the generator once the files have been written.
        public string IconRelativePath { get; set; }
        public string IconGrayRelativePath { get; set; }
    }
}
