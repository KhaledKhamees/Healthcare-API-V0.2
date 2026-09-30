using System.Net;
using Healthcare.LoadTests.Configuration;

namespace Healthcare.LoadTests.Helpers;

/// <summary>
/// Builds the ONE shared HttpClient used by every virtual user.
///
/// WHY a single shared client (learning note):
/// - Each HttpClient owns a socket pool. Creating one per request exhausts
///   ephemeral ports (SocketException) and the "bottleneck" you measure would
///   be your own load generator, not the API.
/// - SocketsHttpHandler with a pooled connection lifetime reuses TCP
///   connections (keep-alive), like a browser does.
/// </summary>
public static class HttpClientProvider
{
    public const string LoadTestHeader = "X-Load-Test";

    public static HttpClient Create(LoadTestSettings settings)
    {
        var handler = new SocketsHttpHandler
        {
            // Re-resolve DNS / recycle connections every 2 minutes so long
            // sustained runs do not pin a single stale connection.
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),

            // Must exceed the max concurrent users you plan (stress = 200).
            // If this were too low, requests would queue CLIENT-side and you
            // would misread the API as slow. This is the "client bottleneck"
            // the README warns about.
            MaxConnectionsPerServer = 1024,

            AutomaticDecompression = DecompressionMethods.All
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + '/'),
            Timeout = TimeSpan.FromSeconds(settings.Load.RequestTimeoutSeconds)
        };

        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Healthcare.LoadTests/nbomber");
        // Lets you filter load-test traffic in Kibana/APM (see README).
        client.DefaultRequestHeaders.Add(LoadTestHeader, "true");

        return client;
    }
}
