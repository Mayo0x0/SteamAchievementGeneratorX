namespace SteamAchievementGenerator.Model
{
    /// <summary>
    /// Everything we know about one achievement icon after parsing the HTML.
    /// The bytes may already be present (WebScrapBook inlines them as a data: URI),
    /// or we may only have a hint that lets us find/download the file later.
    /// </summary>
    public sealed class ImageRef
    {
        /// <summary>Decoded bytes, when the page carried the image inline.</summary>
        public byte[] InlineData { get; set; }

        /// <summary>
        /// Original Steam file name, e.g. "b7068c06....jpg". SteamDB keeps it in
        /// <c>data-name</c>; WebScrapBook additionally puts it into the data: URI
        /// as <c>;filename=...</c>. It is the key for the CDN fallback download.
        /// </summary>
        public string SuggestedFileName { get; set; }

        /// <summary>Absolute http(s) URL, when the page was saved without inlining.</summary>
        public string RemoteUrl { get; set; }

        /// <summary>Relative src, when the page was saved as "complete webpage" with a _files folder.</summary>
        public string RelativePath { get; set; }

        public bool HasInlineData
        {
            get { return InlineData != null && InlineData.Length > 0; }
        }

        public bool IsEmpty
        {
            get
            {
                return !HasInlineData
                    && string.IsNullOrEmpty(RemoteUrl)
                    && string.IsNullOrEmpty(RelativePath)
                    && string.IsNullOrEmpty(SuggestedFileName);
            }
        }

        /// <summary>File extension to use for the generated file, derived from the source name.</summary>
        public string Extension
        {
            get
            {
                string name = SuggestedFileName;
                if (string.IsNullOrEmpty(name)) name = RemoteUrl;
                if (string.IsNullOrEmpty(name)) name = RelativePath;
                if (string.IsNullOrEmpty(name)) return ".jpg";

                int q = name.IndexOf('?');
                if (q >= 0) name = name.Substring(0, q);

                int dot = name.LastIndexOf('.');
                if (dot < 0 || dot == name.Length - 1) return ".jpg";

                string ext = name.Substring(dot).ToLowerInvariant();
                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                    case ".webp":
                    case ".bmp":
                        return ext;
                    default:
                        return ".jpg";
                }
            }
        }
    }
}
