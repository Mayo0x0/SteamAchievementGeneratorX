namespace SteamAchievementGenerator.Generation
{
    public enum IconNaming
    {
        /// <summary>images/&lt;API name&gt;.jpg - readable, matches the gbe_fork example files.</summary>
        ApiName,

        /// <summary>images/&lt;steam hash&gt;.jpg - the original file name used by Steam.</summary>
        SteamFileName
    }

    public sealed class GeneratorOptions
    {
        /// <summary>Target folder; the generator creates it and writes "steam_settings" content into it.</summary>
        public string OutputDirectory { get; set; }

        public bool WriteAchievements { get; set; }
        public bool WriteStats { get; set; }
        public bool WriteAppIdFile { get; set; }

        /// <summary>Fetch icons that the saved page does not contain from the Steam CDN.</summary>
        public bool DownloadMissingIcons { get; set; }

        /// <summary>
        /// Write displayName/description as {"english": "..."} instead of a plain string.
        /// gbe_fork resolves both; the object form is what Achievement Watcher expects.
        /// </summary>
        public bool LocalizedTextObjects { get; set; }

        public IconNaming IconNaming { get; set; }

        /// <summary>Delete a previously generated images folder before writing.</summary>
        public bool CleanImagesFolder { get; set; }

        public GeneratorOptions()
        {
            WriteAchievements = true;
            WriteStats = true;
            WriteAppIdFile = true;
            DownloadMissingIcons = true;
            LocalizedTextObjects = true;
            IconNaming = IconNaming.ApiName;
            CleanImagesFolder = false;
        }
    }
}
