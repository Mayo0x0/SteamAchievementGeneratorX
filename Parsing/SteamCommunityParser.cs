using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SteamAchievementGenerator.Model;

namespace SteamAchievementGenerator.Parsing
{
    /// <summary>
    /// Reads a saved copy of https://steamcommunity.com/stats/&lt;appid&gt;/achievements/ .
    ///
    /// That page renders in whatever language the browser asked for, which is where the
    /// official translations come from. It does not print API names, so every row is
    /// identified by its icon file name - the same file SteamDB links to.
    /// </summary>
    public static class SteamCommunityParser
    {
        private static readonly Regex AppIdFromUrl =
            new Regex(@"steamcommunity\.com/stats/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LanguageFromUrl =
            new Regex(@"[?&]l=([A-Za-z_-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PercentValue =
            new Regex(@"(\d+(?:[.,]\d+)?)\s*%", RegexOptions.Compiled);

        public static TranslationSet Parse(HtmlDocument doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            var result = new TranslationSet();
            var root = doc.DocumentNode;

            string sourceUrl = FirstNonEmpty(
                GetAttr(root.SelectSingleNode("//html"), "data-scrapbook-source"),
                GetAttr(root.SelectSingleNode("//link[@rel='canonical']"), "href"),
                GetAttr(root.SelectSingleNode("//meta[@property='og:url']"), "content"));

            result.DetectedTag = GetAttr(root.SelectSingleNode("//html"), "lang");
            result.Language = SteamLanguages.FromTag(result.DetectedTag);

            if (result.Language == null && !string.IsNullOrEmpty(sourceUrl))
            {
                // An explicit ?l=german in the URL beats the browser language.
                var m = LanguageFromUrl.Match(sourceUrl);
                if (m.Success) result.Language = SteamLanguages.FromTag(m.Groups[1].Value);
            }

            if (!string.IsNullOrEmpty(sourceUrl))
            {
                var m = AppIdFromUrl.Match(sourceUrl);
                if (m.Success) result.AppId = m.Groups[1].Value;
            }

            var heading = root.SelectSingleNode("//div[contains(@class,'gameLogo')]//img")
                       ?? root.SelectSingleNode("//div[@class='profile_small_header_name']//a");
            if (heading != null) result.GameName = Text(heading);

            var rows = root.SelectNodes("//div[contains(@class,'achieveRow')]");
            if (rows == null)
            {
                result.Warnings.Add(
                    "No achievement rows found. Expected a saved copy of " +
                    "https://steamcommunity.com/stats/<appid>/achievements/ .");
                return result;
            }

            int index = 0;
            foreach (var row in rows)
            {
                var entry = new LocalizedAchievement();
                entry.Index = index++;

                var img = row.SelectSingleNode(".//img");
                if (img != null) entry.IconFileName = IconFileNameOf(img);

                var name = row.SelectSingleNode(".//div[contains(@class,'achieveTxt')]/h3")
                        ?? row.SelectSingleNode(".//h3");
                var description = row.SelectSingleNode(".//div[contains(@class,'achieveTxt')]/h5")
                               ?? row.SelectSingleNode(".//h5");

                entry.DisplayName = Text(name);
                entry.Description = Text(description);

                var percent = row.SelectSingleNode(".//div[contains(@class,'achievePercent')]");
                if (percent != null)
                {
                    var m = PercentValue.Match(Text(percent));
                    double value;
                    if (m.Success && double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        entry.UnlockPercentage = value;
                    }
                }

                if (string.IsNullOrEmpty(entry.DisplayName) && string.IsNullOrEmpty(entry.Description))
                    continue;

                result.Achievements.Add(entry);
            }

            if (result.Achievements.Count == 0)
                result.Warnings.Add("The page contains achievement rows, but none of them had a name.");

            int withoutIcon = 0;
            foreach (var entry in result.Achievements)
                if (string.IsNullOrEmpty(entry.IconFileName)) withoutIcon++;

            if (withoutIcon > 0)
                result.Warnings.Add(withoutIcon + " localized rows carry no icon file name and can only be matched by unlock rate.");

            return result;
        }

        /// <summary>
        /// The icon name is the join key, so dig it out of whatever the saver produced:
        /// an inlined data: URI with a filename parameter, or a plain CDN URL.
        /// </summary>
        private static string IconFileNameOf(HtmlNode img)
        {
            string fromAttribute = DataUri.SafeFileName(GetAttr(img, "data-name"));
            if (!string.IsNullOrEmpty(fromAttribute)) return fromAttribute;

            string src = FirstNonEmpty(
                GetAttr(img, "src"),
                GetAttr(img, "data-src"),
                GetAttr(img, "data-scrapbook-orig-attr-src"));
            if (string.IsNullOrEmpty(src)) return null;

            if (DataUri.IsDataUri(src))
            {
                var parsed = DataUri.TryParse(src);
                return parsed != null ? DataUri.SafeFileName(parsed.FileName) : null;
            }

            return DataUri.SafeFileName(src);
        }

        private static string GetAttr(HtmlNode node, string name)
        {
            return node == null ? null : node.GetAttributeValue(name, null);
        }

        private static string Text(HtmlNode node)
        {
            if (node == null) return "";

            string raw = HtmlEntity.DeEntitize(node.InnerText) ?? "";
            var sb = new StringBuilder(raw.Length);
            bool pendingSpace = false;

            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0) pendingSpace = true;
                    continue;
                }

                if (pendingSpace)
                {
                    sb.Append(' ');
                    pendingSpace = false;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
                if (!string.IsNullOrEmpty(value)) return value;

            return null;
        }
    }
}
