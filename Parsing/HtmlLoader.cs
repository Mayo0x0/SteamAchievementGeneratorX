using System;
using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace SteamAchievementGenerator.Parsing
{
    public static class HtmlLoader
    {
        /// <summary>
        /// Loads a saved SteamDB page.
        ///
        /// A WebScrapBook single file page is mostly base64 payload, so the document is
        /// read in one go and handed to HtmlAgilityPack with the encoding taken from the
        /// byte order mark or the meta charset - never from the current ANSI code page,
        /// which would mangle non ASCII achievement names.
        /// </summary>
        public static HtmlDocument Load(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (!File.Exists(path)) throw new FileNotFoundException("HTML file not found.", path);

            byte[] bytes = File.ReadAllBytes(path);
            string text = Decode(bytes);

            var doc = new HtmlDocument();
            doc.OptionFixNestedTags = true;
            doc.OptionAutoCloseOnEnd = true;

            doc.LoadHtml(text);
            return doc;
        }

        private static string Decode(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            string charset = SniffCharset(bytes);
            if (!string.IsNullOrEmpty(charset) &&
                charset.IndexOf("utf-8", StringComparison.OrdinalIgnoreCase) < 0)
            {
                try
                {
                    return Encoding.GetEncoding(charset).GetString(bytes);
                }
                catch (ArgumentException)
                {
                    // Unknown charset - fall through to UTF-8.
                }
            }

            return new UTF8Encoding(false).GetString(bytes);
        }

        /// <summary>Looks for a meta charset inside the first few KB of the document.</summary>
        private static string SniffCharset(byte[] bytes)
        {
            int length = Math.Min(bytes.Length, 4096);
            string head = Encoding.ASCII.GetString(bytes, 0, length);

            var match = System.Text.RegularExpressions.Regex.Match(
                head,
                "charset\\s*=\\s*[\"']?([A-Za-z0-9_.:-]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
