using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SteamAchievementGenerator.Parsing
{
    /// <summary>
    /// Decoder for RFC 2397 data: URIs.
    ///
    /// WebScrapBook inlines every asset of a page as
    /// <c>data:image/jpeg;filename=&lt;original name&gt;;base64,&lt;payload&gt;</c>.
    /// The non standard <c>filename</c> parameter is what lets us keep the original
    /// Steam file name, and it is also present for assets the browser never
    /// downloaded - those end up as the empty URI <c>data:,</c>.
    /// </summary>
    public static class DataUri
    {
        public sealed class Parsed
        {
            public string MediaType { get; set; }
            public string FileName { get; set; }
            public byte[] Data { get; set; }

            public bool HasData
            {
                get { return Data != null && Data.Length > 0; }
            }
        }

        public static bool IsDataUri(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns null when the value is not a data: URI at all.</summary>
        public static Parsed TryParse(string value)
        {
            if (!IsDataUri(value)) return null;

            int comma = value.IndexOf(',');
            if (comma < 0) return null;

            string header = value.Substring("data:".Length, comma - "data:".Length);
            string payload = value.Substring(comma + 1);

            var result = new Parsed();
            bool isBase64 = false;

            string[] parts = header.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0) continue;

                if (i == 0 && part.IndexOf('=') < 0)
                {
                    result.MediaType = part;
                    continue;
                }

                if (string.Equals(part, "base64", StringComparison.OrdinalIgnoreCase))
                {
                    isBase64 = true;
                    continue;
                }

                int eq = part.IndexOf('=');
                if (eq > 0 && string.Equals(part.Substring(0, eq), "filename", StringComparison.OrdinalIgnoreCase))
                {
                    string name = part.Substring(eq + 1).Trim('"');
                    try { name = Uri.UnescapeDataString(name); }
                    catch (UriFormatException) { /* keep the raw value */ }
                    result.FileName = name;
                }
            }

            if (payload.Length == 0)
            {
                // "data:," - WebScrapBook records the placeholder for assets the browser
                // never fetched (SteamDB only loads the greyscale icons on hover).
                result.Data = new byte[0];
                return result;
            }

            try
            {
                result.Data = isBase64 ? DecodeBase64(payload) : DecodePercentEncoded(payload);
            }
            catch (FormatException)
            {
                result.Data = new byte[0];
            }

            return result;
        }

        private static byte[] DecodeBase64(string payload)
        {
            // Base64 inside a URI may carry whitespace or the URL safe alphabet.
            var sb = new StringBuilder(payload.Length);
            foreach (char c in payload)
            {
                if (char.IsWhiteSpace(c)) continue;
                if (c == '-') sb.Append('+');
                else if (c == '_') sb.Append('/');
                else sb.Append(c);
            }

            int pad = sb.Length % 4;
            if (pad == 2) sb.Append("==");
            else if (pad == 3) sb.Append('=');
            else if (pad == 1) return new byte[0]; // not decodable

            return Convert.FromBase64String(sb.ToString());
        }

        private static byte[] DecodePercentEncoded(string payload)
        {
            var bytes = new List<byte>(payload.Length);
            for (int i = 0; i < payload.Length; i++)
            {
                char c = payload[i];
                if (c == '%' && i + 2 < payload.Length)
                {
                    int hi = HexValue(payload[i + 1]);
                    int lo = HexValue(payload[i + 2]);
                    if (hi >= 0 && lo >= 0)
                    {
                        bytes.Add((byte)((hi << 4) | lo));
                        i += 2;
                        continue;
                    }
                }

                if (c < 0x80)
                {
                    bytes.Add((byte)c);
                }
                else
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
                }
            }

            return bytes.ToArray();
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        /// <summary>
        /// A data: URI can be several megabytes long, which makes it useless as a file
        /// name hint. This gives a short, loggable description instead.
        /// </summary>
        public static string Describe(string value)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            if (!IsDataUri(value)) return value.Length > 120 ? value.Substring(0, 120) + "..." : value;

            int comma = value.IndexOf(',');
            string header = comma > 0 ? value.Substring(0, comma) : value;
            if (header.Length > 120) header = header.Substring(0, 120) + "...";
            return header + ",<" + Math.Max(0, value.Length - comma - 1) + " chars>";
        }

        /// <summary>Best effort file name for an inline asset.</summary>
        public static string SafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            name = name.Replace('\\', '/');
            int slash = name.LastIndexOf('/');
            if (slash >= 0) name = name.Substring(slash + 1);

            int q = name.IndexOf('?');
            if (q >= 0) name = name.Substring(0, q);

            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');

            name = name.Trim();
            return name.Length == 0 ? null : name;
        }
    }
}
