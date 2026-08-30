using System;
using System.Collections.Generic;

namespace SteamAchievementGenerator.Parsing
{
    /// <summary>
    /// The language names Steam (and therefore gbe_fork's achievements.json) uses.
    /// They are not ISO codes - "koreana", "schinese" and "brazilian" are Steam's own spelling.
    /// </summary>
    public static class SteamLanguages
    {
        /// <summary>All API language names, in the order Steam lists them.</summary>
        public static readonly string[] All =
        {
            "english",
            "german",
            "french",
            "italian",
            "spanish",
            "latam",
            "portuguese",
            "brazilian",
            "dutch",
            "danish",
            "finnish",
            "norwegian",
            "swedish",
            "polish",
            "czech",
            "hungarian",
            "romanian",
            "bulgarian",
            "greek",
            "turkish",
            "russian",
            "ukrainian",
            "japanese",
            "koreana",
            "schinese",
            "tchinese",
            "thai",
            "vietnamese",
            "indonesian",
            "arabic"
        };

        // Longest prefix wins, so "pt-br" resolves before "pt".
        private static readonly KeyValuePair<string, string>[] FromHtmlLang =
        {
            new KeyValuePair<string, string>("pt-br", "brazilian"),
            new KeyValuePair<string, string>("zh-cn", "schinese"),
            new KeyValuePair<string, string>("zh-hans", "schinese"),
            new KeyValuePair<string, string>("zh-sg", "schinese"),
            new KeyValuePair<string, string>("zh-tw", "tchinese"),
            new KeyValuePair<string, string>("zh-hant", "tchinese"),
            new KeyValuePair<string, string>("zh-hk", "tchinese"),
            new KeyValuePair<string, string>("es-419", "latam"),
            new KeyValuePair<string, string>("es-mx", "latam"),
            new KeyValuePair<string, string>("es-ar", "latam"),
            new KeyValuePair<string, string>("ar", "arabic"),
            new KeyValuePair<string, string>("bg", "bulgarian"),
            new KeyValuePair<string, string>("cs", "czech"),
            new KeyValuePair<string, string>("da", "danish"),
            new KeyValuePair<string, string>("de", "german"),
            new KeyValuePair<string, string>("el", "greek"),
            new KeyValuePair<string, string>("en", "english"),
            new KeyValuePair<string, string>("es", "spanish"),
            new KeyValuePair<string, string>("fi", "finnish"),
            new KeyValuePair<string, string>("fr", "french"),
            new KeyValuePair<string, string>("hu", "hungarian"),
            new KeyValuePair<string, string>("id", "indonesian"),
            new KeyValuePair<string, string>("it", "italian"),
            new KeyValuePair<string, string>("ja", "japanese"),
            new KeyValuePair<string, string>("ko", "koreana"),
            new KeyValuePair<string, string>("nb", "norwegian"),
            new KeyValuePair<string, string>("nl", "dutch"),
            new KeyValuePair<string, string>("nn", "norwegian"),
            new KeyValuePair<string, string>("no", "norwegian"),
            new KeyValuePair<string, string>("pl", "polish"),
            new KeyValuePair<string, string>("pt", "portuguese"),
            new KeyValuePair<string, string>("ro", "romanian"),
            new KeyValuePair<string, string>("ru", "russian"),
            new KeyValuePair<string, string>("sv", "swedish"),
            new KeyValuePair<string, string>("th", "thai"),
            new KeyValuePair<string, string>("tr", "turkish"),
            new KeyValuePair<string, string>("uk", "ukrainian"),
            new KeyValuePair<string, string>("vi", "vietnamese"),
            new KeyValuePair<string, string>("zh", "schinese")
        };

        /// <summary>
        /// Maps the value of an html lang attribute (or a Steam l= query parameter)
        /// to a Steam language name. Returns null when nothing matches.
        /// </summary>
        public static string FromTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;

            string value = tag.Trim().ToLowerInvariant().Replace('_', '-');

            // Steam URLs carry the API name directly (?l=german).
            foreach (string language in All)
                if (string.Equals(value, language, StringComparison.Ordinal)) return language;

            string best = null;
            int bestLength = 0;

            foreach (var pair in FromHtmlLang)
            {
                if (value.Length < pair.Key.Length) continue;
                if (!value.StartsWith(pair.Key, StringComparison.Ordinal)) continue;

                // Only match on a full subtag boundary: "de" must not match "delta".
                if (value.Length > pair.Key.Length && value[pair.Key.Length] != '-') continue;

                if (pair.Key.Length > bestLength)
                {
                    best = pair.Value;
                    bestLength = pair.Key.Length;
                }
            }

            return best;
        }

        public static bool IsKnown(string language)
        {
            if (string.IsNullOrEmpty(language)) return false;

            foreach (string known in All)
                if (string.Equals(known, language, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}
