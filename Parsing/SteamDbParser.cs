using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SteamAchievementGenerator.Model;

namespace SteamAchievementGenerator.Parsing
{
    /// <summary>
    /// Reads a saved copy of https://steamdb.info/app/&lt;id&gt;/stats/ .
    ///
    /// Handles all three ways such a page is usually stored:
    ///   * WebScrapBook single file - every image is an inline data: URI
    ///   * "Webpage, complete" - images live in a sibling *_files folder
    ///   * raw HTML - images are still absolute CDN URLs
    ///
    /// Nothing here touches the file system or the network; the parser only records
    /// where an image could come from, and <see cref="IconResolver"/> fetches it later.
    /// </summary>
    public static class SteamDbParser
    {
        private static readonly Regex AppIdFromUrl =
            new Regex(@"steamdb\.info/app/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AppIdFromSteamProtocol =
            new Regex(@"steam://\w+/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PercentValue =
            new Regex(@"(\d+(?:[.,]\d+)?)\s*%", RegexOptions.Compiled);

        public static ParseResult Parse(HtmlDocument doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            var result = new ParseResult();
            result.Game = ParseGameInfo(doc, result.Warnings);
            result.Achievements = ParseAchievements(doc, result.Warnings);
            result.Stats = ParseStats(doc, result.Warnings);
            return result;
        }

        // ------------------------------------------------------------------ game info

        private static GameInfo ParseGameInfo(HtmlDocument doc, List<string> warnings)
        {
            var root = doc.DocumentNode;
            var game = new GameInfo();

            game.SourceUrl = FirstNonEmpty(
                GetAttr(root.SelectSingleNode("//html"), "data-scrapbook-source"),
                GetAttr(root.SelectSingleNode("//link[@rel='canonical']"), "href"),
                GetAttr(root.SelectSingleNode("//meta[@property='og:url']"), "content"));

            game.AppId = FindAppId(root, game.SourceUrl);
            if (string.IsNullOrEmpty(game.AppId))
                warnings.Add("No App ID found in the HTML - steam_appid.txt cannot be written and icons cannot be downloaded.");

            var nameNode = root.SelectSingleNode("//div[contains(@class,'pagehead-title')]//h1")
                        ?? root.SelectSingleNode("//h1[@itemprop='name']")
                        ?? root.SelectSingleNode("//div[contains(@class,'pagehead')]//h1");
            if (nameNode != null)
            {
                game.Name = Text(nameNode);
            }
            else
            {
                var title = root.SelectSingleNode("//title");
                if (title != null)
                {
                    string t = Text(title);
                    t = Regex.Replace(t, "[\\s]*[\\u00b7|][\\s]*SteamDB[\\s]*$", "", RegexOptions.IgnoreCase);
                    t = Regex.Replace(t, @"\s+Achievements\s*$", "", RegexOptions.IgnoreCase);
                    game.Name = t.Trim();
                }
            }

            game.Developer = InfoTableValue(root, "Developer");
            game.Publisher = InfoTableValue(root, "Publisher");
            game.ReleaseDate = ParseReleaseDate(root);

            var logo = root.SelectSingleNode("//img[contains(@class,'app-logo')]")
                    ?? root.SelectSingleNode("//div[contains(@class,'js-open-screenshot-viewer')]//img");
            if (logo != null) game.HeaderImage = BuildImageRef(logo);

            return game;
        }

        private static string FindAppId(HtmlNode root, string sourceUrl)
        {
            // The page opens with a "popular today" search dropdown full of other apps,
            // so a plain //*[@data-appid] would happily return the wrong game. Ask the
            // element that scopes the page itself first.
            string scoped = AttrIfAppId(root.SelectSingleNode("//div[contains(@class,'scope-app')][@data-appid]"), "data-appid");
            if (scoped != null) return scoped;

            scoped = AttrIfAppId(root.SelectSingleNode("//*[@itemtype='http://schema.org/SoftwareApplication'][@data-appid]"), "data-appid");
            if (scoped != null) return scoped;

            if (!string.IsNullOrEmpty(sourceUrl))
            {
                var m = AppIdFromUrl.Match(sourceUrl);
                if (m.Success) return m.Groups[1].Value;
            }

            // "App ID" row of the info table.
            string fromTable = InfoTableValue(root, "App ID");
            if (IsAppId(fromTable)) return fromTable.Trim();

            var launch = root.SelectSingleNode("//a[starts-with(@href,'steam://')]");
            if (launch != null)
            {
                var m = AppIdFromSteamProtocol.Match(GetAttr(launch, "href"));
                if (m.Success) return m.Groups[1].Value;
            }

            return AttrIfAppId(root.SelectSingleNode("//*[@data-appid]"), "data-appid");
        }

        private static string AttrIfAppId(HtmlNode node, string attribute)
        {
            string value = GetAttr(node, attribute);
            return IsAppId(value) ? value.Trim() : null;
        }

        private static bool IsAppId(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            value = value.Trim();
            if (value.Length == 0 || value.Length > 12) return false;
            foreach (char c in value) if (c < '0' || c > '9') return false;
            return true;
        }

        /// <summary>Reads a value from the two column app info table by its label.</summary>
        private static string InfoTableValue(HtmlNode root, string label)
        {
            var rows = root.SelectNodes("//table[contains(@class,'table-responsive-flex')]//tr");
            if (rows == null) rows = root.SelectNodes("//table//tr");
            if (rows == null) return null;

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 2) continue;

                string key = Text(cells[0]);
                // The label cell often carries a trailing help icon.
                if (!key.StartsWith(label, StringComparison.OrdinalIgnoreCase)) continue;
                if (key.Length > label.Length && char.IsLetterOrDigit(key[label.Length])) continue;

                var link = cells[1].SelectSingleNode("./a");
                return link != null ? Text(link) : Text(cells[1]);
            }

            return null;
        }

        private static string ParseReleaseDate(HtmlNode root)
        {
            var rows = root.SelectNodes("//table[contains(@class,'table-responsive-flex')]//tr");
            if (rows == null) return null;

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 2) continue;
                if (!Text(cells[0]).StartsWith("Release Date", StringComparison.OrdinalIgnoreCase)) continue;

                var time = cells[1].SelectSingleNode(".//relative-time")
                        ?? cells[1].SelectSingleNode(".//time");
                if (time != null)
                {
                    string iso = FirstNonEmpty(GetAttr(time, "datetime"), GetAttr(time, "content"));
                    DateTime parsed;
                    if (!string.IsNullOrEmpty(iso) &&
                        DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                                          DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
                    {
                        return parsed.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
                    }
                }

                // Fall back to the plain text, dropping the "(x months ago)" suffix.
                string text = Text(cells[1]);
                int paren = text.IndexOf('(');
                if (paren > 0) text = text.Substring(0, paren);
                return text.Trim();
            }

            return null;
        }

        // --------------------------------------------------------------- achievements

        private static List<AchievementEntry> ParseAchievements(HtmlDocument doc, List<string> warnings)
        {
            var list = new List<AchievementEntry>();
            var nodes = SelectAchievementNodes(doc.DocumentNode);
            if (nodes.Count == 0)
            {
                warnings.Add("No achievement entries found. Make sure the saved page is the '/stats/' tab of a SteamDB app.");
                return list;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                var entry = new AchievementEntry();

                // The dedicated column holds the exact API name; the element id is only a fallback
                // because SteamDB sanitises it for the DOM.
                var apiNode = FindByClass(node, "achievement_api");
                entry.ApiName = apiNode != null ? Text(apiNode) : null;

                if (string.IsNullOrEmpty(entry.ApiName))
                {
                    string id = GetAttr(node, "id");
                    if (!string.IsNullOrEmpty(id) && id.StartsWith("achievement-", StringComparison.OrdinalIgnoreCase))
                        entry.ApiName = id.Substring("achievement-".Length);
                }

                if (string.IsNullOrEmpty(entry.ApiName)) continue;
                if (!seen.Add(entry.ApiName))
                {
                    warnings.Add("Duplicate achievement '" + entry.ApiName + "' in the HTML - the later one was skipped.");
                    continue;
                }

                var nameNode = FindByClass(node, "achievement_name");
                entry.DisplayName = nameNode != null ? Text(nameNode) : entry.ApiName;

                ReadDescription(node, entry);

                var unlockNode = FindByClass(node, "achievement_unlock");
                if (unlockNode != null)
                {
                    var m = PercentValue.Match(Text(unlockNode));
                    double pct;
                    if (m.Success && double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out pct))
                    {
                        entry.UnlockPercentage = pct;
                    }
                }

                entry.Icon = BuildImageRef(FindImage(node, false));
                entry.IconGray = BuildImageRef(FindImage(node, true));

                list.Add(entry);
            }

            return list;
        }

        private static List<HtmlNode> SelectAchievementNodes(HtmlNode root)
        {
            var byId = root.SelectNodes("//div[starts-with(@id,'achievement-')]");
            var result = new List<HtmlNode>();

            if (byId != null)
            {
                foreach (var n in byId)
                    if (HasClass(n, "achievement")) result.Add(n);
            }

            if (result.Count > 0) return result;

            // Older/newer markup may drop the id; fall back to the list container.
            var inList = root.SelectNodes("//div[contains(@class,'achievements_list')]//div[contains(@class,'achievement')]");
            if (inList != null)
            {
                foreach (var n in inList)
                    if (HasClass(n, "achievement") && FindByClass(n, "achievement_api") != null) result.Add(n);
            }

            return result;
        }

        private static void ReadDescription(HtmlNode node, AchievementEntry entry)
        {
            var descNode = FindByClass(node, "achievement_desc");
            if (descNode == null)
            {
                entry.Description = "";
                entry.Hidden = false;
                return;
            }

            // SteamDB blurs the description of hidden achievements with .achievement_spoiler,
            // either on the description element itself or on an inner span.
            var spoiler = HasClass(descNode, "achievement_spoiler")
                ? descNode
                : FindByClass(descNode, "achievement_spoiler");

            entry.Hidden = spoiler != null;

            var source = spoiler ?? descNode;
            var clone = source.Clone();

            // Drop the legacy "Hidden achievement:" marker if it is still there.
            var markers = clone.SelectNodes("./i|./em|./span[contains(@class,'muted')]");
            if (markers != null)
            {
                foreach (var marker in markers)
                {
                    if (Text(marker).IndexOf("hidden achievement", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        entry.Hidden = true;
                        marker.Remove();
                    }
                }
            }

            entry.Description = Text(clone);
        }

        private static HtmlNode FindImage(HtmlNode achievement, bool gray)
        {
            var images = achievement.SelectNodes(".//img");
            if (images == null) return null;

            foreach (var img in images)
            {
                bool isSmall = HasClass(img, "achievement_image_small");
                if (gray && isSmall) return img;
                if (!gray && !isSmall && HasClass(img, "achievement_image")) return img;
            }

            return null;
        }

        // ----------------------------------------------------------------------- stats

        private static List<StatEntry> ParseStats(HtmlDocument doc, List<string> warnings)
        {
            var list = new List<StatEntry>();
            var root = doc.DocumentNode;

            var rows = root.SelectNodes("//tr[starts-with(@id,'stat-')]");
            if (rows == null) rows = SelectStatRowsByHeading(root);

            if (rows == null || rows.Count == 0)
            {
                // Not an error: plenty of games define achievements but no stats.
                return list;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 1) continue;

                var stat = new StatEntry();
                stat.ApiName = Text(cells[0]);

                if (string.IsNullOrEmpty(stat.ApiName))
                {
                    string id = GetAttr(row, "id");
                    if (!string.IsNullOrEmpty(id) && id.StartsWith("stat-", StringComparison.OrdinalIgnoreCase))
                        stat.ApiName = id.Substring("stat-".Length);
                }

                if (string.IsNullOrEmpty(stat.ApiName)) continue;
                if (!seen.Add(stat.ApiName))
                {
                    warnings.Add("Duplicate stat '" + stat.ApiName + "' in the HTML - the later one was skipped.");
                    continue;
                }

                if (cells.Count > 1)
                {
                    // SteamDB renders "<i>no name</i>" when the stat has no display name.
                    var italic = cells[1].SelectSingleNode("./i");
                    string display = Text(cells[1]);
                    bool placeholder = italic != null &&
                        string.Equals(Text(italic), "no name", StringComparison.OrdinalIgnoreCase);
                    stat.DisplayName = placeholder ? "" : display;
                }

                if (cells.Count > 2) stat.DefaultValue = Text(cells[2]);
                if (string.IsNullOrEmpty(stat.DefaultValue)) stat.DefaultValue = "0";

                stat.Type = StatEntry.GuessType(stat.DefaultValue);
                stat.GlobalValue = "0";

                list.Add(stat);
            }

            return list;
        }

        /// <summary>Fallback: the table that follows the "Stats" heading.</summary>
        private static HtmlNodeCollection SelectStatRowsByHeading(HtmlNode root)
        {
            var headings = root.SelectNodes("//h2|//h3");
            if (headings == null) return null;

            foreach (var heading in headings)
            {
                if (!string.Equals(Text(heading), "Stats", StringComparison.OrdinalIgnoreCase)) continue;

                for (var sibling = heading.NextSibling; sibling != null; sibling = sibling.NextSibling)
                {
                    if (sibling.NodeType != HtmlNodeType.Element) continue;

                    var table = string.Equals(sibling.Name, "table", StringComparison.OrdinalIgnoreCase)
                        ? sibling
                        : sibling.SelectSingleNode(".//table");
                    if (table == null) continue;

                    var rows = table.SelectNodes(".//tbody/tr");
                    if (rows == null) rows = table.SelectNodes(".//tr[td]");
                    if (rows != null && rows.Count > 0) return rows;
                }
            }

            return null;
        }

        // --------------------------------------------------------------------- helpers

        private static ImageRef BuildImageRef(HtmlNode img)
        {
            if (img == null) return null;

            var image = new ImageRef();
            image.SuggestedFileName = DataUri.SafeFileName(GetAttr(img, "data-name"));

            string src = FirstNonEmpty(
                GetAttr(img, "src"),
                GetAttr(img, "data-src"),
                GetAttr(img, "data-scrapbook-orig-attr-src"));

            if (string.IsNullOrEmpty(src)) return image;

            if (DataUri.IsDataUri(src))
            {
                var parsed = DataUri.TryParse(src);
                if (parsed != null)
                {
                    if (parsed.HasData) image.InlineData = parsed.Data;
                    if (string.IsNullOrEmpty(image.SuggestedFileName))
                        image.SuggestedFileName = DataUri.SafeFileName(parsed.FileName);
                }
                return image;
            }

            if (src.StartsWith("//", StringComparison.Ordinal))
            {
                image.RemoteUrl = "https:" + src;
            }
            else if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                image.RemoteUrl = src;
            }
            else
            {
                image.RelativePath = src;
                if (string.IsNullOrEmpty(image.SuggestedFileName))
                    image.SuggestedFileName = DataUri.SafeFileName(src);
            }

            if (!string.IsNullOrEmpty(image.RemoteUrl) && string.IsNullOrEmpty(image.SuggestedFileName))
                image.SuggestedFileName = DataUri.SafeFileName(image.RemoteUrl);

            return image;
        }

        private static bool HasClass(HtmlNode node, string className)
        {
            if (node == null) return false;
            string classes = GetAttr(node, "class");
            if (string.IsNullOrEmpty(classes)) return false;

            foreach (var token in classes.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                if (string.Equals(token, className, StringComparison.Ordinal)) return true;

            return false;
        }

        private static HtmlNode FindByClass(HtmlNode root, string className)
        {
            var candidates = root.SelectNodes(".//*[contains(@class,'" + className + "')]");
            if (candidates == null) return null;

            foreach (var candidate in candidates)
                if (HasClass(candidate, className)) return candidate;

            return null;
        }

        private static string GetAttr(HtmlNode node, string name)
        {
            return node == null ? null : node.GetAttributeValue(name, null);
        }

        /// <summary>Inner text with entities resolved and whitespace collapsed.</summary>
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
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }
    }
}
