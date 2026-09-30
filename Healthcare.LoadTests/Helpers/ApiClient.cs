using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Healthcare.LoadTests.Models;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Helpers;

/// <summary>
/// Thin wrapper over HttpClient that converts every HTTP outcome into an
/// NBomber <see cref="IResponse"/>.
///
/// CRITICAL RULE (learning note): never hide HTTP failures. A 500 MUST show up
/// as a failed NBomber request, otherwise your error rate looks like 0% while
/// the API is actually broken. Hence: 2xx -> Response.Ok, anything else (or a
/// network/timeout exception) -> Response.Fail with the status code + a short
/// body preview so you can diagnose from the NBomber report alone.
/// Status codes and byte counts also flow into NBomber's P50/P95/P99 stats.
/// </summary>
public static class ApiClient
{
    /// <summary>Per-scenario header so APM/Kibana can split traffic by scenario.</summary>
    public const string ScenarioHeader = "X-Load-Test-Scenario";

    public static Task<IResponse> GetAsync(
        HttpClient client, string url, string scenarioName, string? token, CancellationToken ct)
        => SendAsync(client, HttpMethod.Get, url, scenarioName, token, body: null, ct);

    public static Task<IResponse> PostJsonAsync<T>(
        HttpClient client, string url, string scenarioName, string? token, T body, CancellationToken ct)
        => SendAsync(client, HttpMethod.Post, url, scenarioName, token, (object?)body, ct);

    public static Task<IResponse> PutJsonAsync<T>(
        HttpClient client, string url, string scenarioName, string? token, T body, CancellationToken ct)
        => SendAsync(client, HttpMethod.Put, url, scenarioName, token, (object?)body, ct);

    private static async Task<IResponse> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string scenarioName,
        string? token,
        object? body,
        CancellationToken ct)
    {
        if (token is null && RequiresAuth(url, method))
        {
            return Response.Fail(
                statusCode: "NO_AUTH_TOKEN",
                message: "No access token: login was skipped or failed. Set LOADTEST_EMAIL/LOADTEST_PASSWORD.",
                sizeBytes: 0,
                customLatencyMs: 0);
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation(ScenarioHeader, scenarioName);

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonContext.Options),
                Encoding.UTF8,
                "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // HttpClient timeout (NOT NBomber stopping the run).
            return Response.Fail("TIMEOUT", "Request timed out: " + ex.Message, 0, 0);
        }
        catch (TaskCanceledException)
        {
            return Response.Fail("CANCELLED", "Request cancelled (run shutting down).", 0, 0);
        }
        catch (HttpRequestException ex)
        {
            // DNS failure, connection refused, TLS error... -> the API (or its
            // port/config) is unreachable. Surfaces as failures, not hangs.
            return Response.Fail("CONNECTION_ERROR", ex.Message, 0, 0);
        }
        catch (Exception ex)
        {
            return Response.Fail("CLIENT_ERROR", ex.GetType().Name + ": " + ex.Message, 0, 0);
        }

        using (response)
        {
            byte[] bytes;
            try
            {
                bytes = await response.Content.ReadAsByteArrayAsync(ct);
            }
            catch (Exception ex)
            {
                return Response.Fail("READ_BODY_ERROR", ex.Message, 0, 0);
            }

            var code = ((int)response.StatusCode).ToString();

            if (response.IsSuccessStatusCode)
            {
                return Response.Ok(statusCode: code, sizeBytes: bytes.LongLength, message: string.Empty, customLatencyMs: 0);
            }

            // Non-2xx: record as failure WITH a body preview for diagnosis.
            // NOTE: 409 on create (slot taken) is an *expected business outcome*
            // under concurrent load; it still counts as Fail here so the rate is
            // visible — interpret it via the README, don't hide it.
            string preview;
            try { preview = Encoding.UTF8.GetString(bytes); }
            catch { preview = "<non-text body>"; }

            return Response.Fail(code, Truncate(preview, 300), bytes.LongLength, 0);
        }
    }

    // Endpoints in this API that require Authorization: Bearer.
    private static bool RequiresAuth(string url, HttpMethod method)
        => url.StartsWith("api/appointments", StringComparison.OrdinalIgnoreCase)
           || (url.StartsWith("api/doctors", StringComparison.OrdinalIgnoreCase)
               && method != HttpMethod.Get);

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";
}
