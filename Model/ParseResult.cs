using System.Collections.Generic;

namespace SteamAchievementGenerator.Model
{
    public sealed class ParseResult
    {
        public GameInfo Game { get; set; }
        public List<AchievementEntry> Achievements { get; set; }
        public List<StatEntry> Stats { get; set; }

        /// <summary>Non fatal problems worth showing to the user.</summary>
        public List<string> Warnings { get; set; }

        public ParseResult()
        {
            Game = new GameInfo();
            Achievements = new List<AchievementEntry>();
            Stats = new List<StatEntry>();
            Warnings = new List<string>();
        }
    }
}
