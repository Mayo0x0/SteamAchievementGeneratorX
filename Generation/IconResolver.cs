using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SteamAchievementGenerator.Model;

namespace SteamAchievementGenerator.Generation
{
    /// <summary>
    /// Turns an <see cref="ImageRef"/> into actual bytes.
    ///
    /// Order of attempts:
    ///   1. inline data from the page (WebScrapBook single file)
    ///   2. the local assets folder next to the HTML ("Webpage, complete")
    ///   3. the absolute URL still present in the markup
    ///   4. the Steam community CDN, reconstructed from the App ID and the icon file name
    ///
    /// Step 4 matters even for a fully inlined page: SteamDB only loads the greyscale
    /// icons when a row is hovered, so a freshly saved page usually stores them as the
    /// empty placeholder "data:," and the bytes have to come from the CDN.
    /// </summary>
    public sealed class IconResolver
    {
        private static readonly string[] CdnTemplates =
        {
            "https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{0}/{1}",
            "https://cdn.akamai.steamstatic.com/steamcommunity/public/images/apps/{0}/{1}",
            "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}",
            "https://shared.fastly.steamstatic.com/community_assets/{0}/{1}"
        };

        private readonly HttpClient _http;
        private readonly string _appId;
        private readonly List<string> _localSearchPaths = new List<string>();
        private readonly Dictionary<string, byte[]> _downloadCache =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public bool AllowDownloads { get; set; }

        /// <summary>Human readable notes about anything that could not be fetched.</summary>
        public List<string> Warnings { get; private set; }

        public IconResolver(HttpClient http, string appId, string htmlFilePath)
        {
            _http = http;
            _appId = appId;
            AllowDownloads = true;
            Warnings = new List<string>();

            if (!string.IsNullOrEmpty(htmlFilePath))
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(htmlFilePath));
                string stem = Path.GetFileNameWithoutExtension(htmlFilePath);

                AddSearchPath(dir);
                AddSearchPath(Path.Combine(dir, stem + "_files"));
                AddSearchPath(Path.Combine(dir, stem + ".files"));
                AddSearchPath(Path.Combine(dir, stem));
                AddSearchPath(Path.Combine(dir, "index_files"));
            }
        }

        private void AddSearchPath(string path)
        {
            if (!string.IsNullOrEmpty(path) && !_localSearchPaths.Contains(path))
                _localSearchPaths.Add(path);
        }

        public async Task<byte[]> ResolveAsync(ImageRef image, CancellationToken cancellationToken)
        {
            if (image == null || image.IsEmpty) return null;

            if (image.HasInlineData) return image.InlineData;

            byte[] local = TryReadLocal(image);
            if (local != null) return local;

            if (!AllowDownloads) return null;

            if (!string.IsNullOrEmpty(image.RemoteUrl))
            {
                byte[] remote = await DownloadAsync(image.RemoteUrl, cancellationToken).ConfigureAwait(false);
                if (remote != null) return remote;
            }

            if (!string.IsNullOrEmpty(_appId) && !string.IsNullOrEmpty(image.SuggestedFileName))
            {
                foreach (string template in CdnTemplates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string url = string.Format(template, _appId, image.SuggestedFileName);
                    byte[] bytes = await DownloadAsync(url, cancellationToken).ConfigureAwait(false);
                    if (bytes != null) return bytes;
                }
            }

            return null;
        }

        private byte[] TryReadLocal(ImageRef image)
        {
            var names = new List<string>();
            if (!string.IsNullOrEmpty(image.RelativePath)) names.Add(image.RelativePath);
            if (!string.IsNullOrEmpty(image.SuggestedFileName)) names.Add(image.SuggestedFileName);

            foreach (string raw in names)
            {
                string relative = raw.Replace('/', Path.DirectorySeparatorChar).TrimStart('.', Path.DirectorySeparatorChar);
                string fileName = Path.GetFileName(relative);

                foreach (string dir in _localSearchPaths)
                {
                    byte[] bytes = TryReadFile(CombineSafe(dir, relative));
                    if (bytes != null) return bytes;

                    if (!string.Equals(relative, fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        bytes = TryReadFile(CombineSafe(dir, fileName));
                        if (bytes != null) return bytes;
                    }
                }
            }

            return null;
        }

        /// <summary>Path.Combine throws on illegal characters; a bad src must not abort the run.</summary>
        private static string CombineSafe(string dir, string relative)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(relative)) return null;
            if (relative.Length > 200) return null;

            try
            {
                string combined = Path.Combine(dir, relative);
                return combined.Length > 240 ? null : combined;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static byte[] TryReadFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                if (!File.Exists(path)) return null;
                byte[] bytes = File.ReadAllBytes(path);
                return bytes.Length > 0 ? bytes : null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            byte[] cached;
            if (_downloadCache.TryGetValue(url, out cached)) return cached;

            byte[] result = null;
            try
            {
                using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken)
                                                .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        if (bytes.Length > 0) result = bytes;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                // HttpClient reports its own timeout as a cancellation as well.
                Warnings.Add("Timeout while downloading " + url);
            }
            catch (HttpRequestException ex)
            {
                Warnings.Add("Download failed for " + url + ": " + ex.Message);
            }
            catch (Exception ex)
            {
                Warnings.Add("Download failed for " + url + ": " + ex.Message);
            }

            _downloadCache[url] = result;
            return result;
        }
    }
}
