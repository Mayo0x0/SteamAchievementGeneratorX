namespace SteamAchievementGenerator.Model
{
    public sealed class GameInfo
    {
        public string Name { get; set; }
        public string AppId { get; set; }
        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string ReleaseDate { get; set; }

        /// <summary>The steamdb.info URL the page was saved from, when the saver recorded it.</summary>
        public string SourceUrl { get; set; }

        /// <summary>Header artwork, only used for the preview in the UI.</summary>
        public ImageRef HeaderImage { get; set; }
    }
}
