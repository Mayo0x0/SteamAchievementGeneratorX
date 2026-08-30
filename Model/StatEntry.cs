using System.Globalization;

namespace SteamAchievementGenerator.Model
{
    /// <summary>Stat types understood by the stats.json of gbe_fork.</summary>
    public enum StatType
    {
        Int,
        Float,
        AvgRate
    }

    public sealed class StatEntry
    {
        public string ApiName { get; set; }

        /// <summary>The "Display Name" column of SteamDB. Empty when SteamDB shows "no name".</summary>
        public string DisplayName { get; set; }

        public StatType Type { get; set; }

        /// <summary>Default value, kept as text because gbe_fork expects a JSON string.</summary>
        public string DefaultValue { get; set; }

        /// <summary>Global (aggregated) value. SteamDB does not expose it, so it stays "0".</summary>
        public string GlobalValue { get; set; }

        public StatEntry()
        {
            DefaultValue = "0";
            GlobalValue = "0";
            Type = StatType.Int;
        }

        public string TypeToken
        {
            get
            {
                switch (Type)
                {
                    case StatType.Float: return "float";
                    case StatType.AvgRate: return "avgrate";
                    default: return "int";
                }
            }
        }

        public static StatType ParseType(string token)
        {
            if (string.IsNullOrEmpty(token)) return StatType.Int;
            switch (token.Trim().ToLowerInvariant())
            {
                case "float": return StatType.Float;
                case "avgrate": return StatType.AvgRate;
                default: return StatType.Int;
            }
        }

        /// <summary>
        /// SteamDB never states whether a stat is an int or a float, so we guess from the
        /// default value. SteamDB formats numbers the English way, so a dot means "float"
        /// and a comma is a thousands separator.
        /// </summary>
        public static StatType GuessType(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue)) return StatType.Int;
            if (defaultValue.IndexOf('.') >= 0) return StatType.Float;

            long ignored;
            if (long.TryParse(Clean(defaultValue), NumberStyles.Integer, CultureInfo.InvariantCulture, out ignored))
                return StatType.Int;

            return StatType.Float;
        }

        /// <summary>Strips grouping characters SteamDB may print inside a number.</summary>
        private static string Clean(string value)
        {
            return (value ?? "").Trim()
                .Replace(" ", "")
                .Replace("\u00a0", "")
                .Replace("\u202f", "")
                .Replace(",", "");
        }

        /// <summary>Normalises the default value so std::stof / std::stol inside gbe_fork can read it.</summary>
        public string NormalizedDefaultValue
        {
            get
            {
                string raw = Clean(DefaultValue);
                if (raw.Length == 0) return Type == StatType.Int ? "0" : "0.0";

                if (Type == StatType.Int)
                {
                    long l;
                    if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out l))
                        return l.ToString(CultureInfo.InvariantCulture);

                    double d;
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                        return ((long)d).ToString(CultureInfo.InvariantCulture);

                    return "0";
                }

                double f;
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                    return f.ToString("R", CultureInfo.InvariantCulture);

                return "0.0";
            }
        }
    }
}
