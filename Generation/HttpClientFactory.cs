using System;
using System.Net;
using System.Net.Http;

namespace SteamAchievementGenerator.Generation
{
    public static class HttpClientFactory
    {
        public static HttpClient Create()
        {
            // The Steam CDN rejects requests without a browser like User-Agent.
            EnableModernTls();

            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");

            return client;
        }

        private static void EnableModernTls()
        {
            // .NET Framework 4.8 still defaults to whatever the machine config says.
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException)
            {
                // Nothing to do - the platform already negotiates a modern protocol.
            }
        }
    }
}
