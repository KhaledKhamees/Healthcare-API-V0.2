using System.Net.Http.Json;
using Healthcare.LoadTests.Models;

namespace Healthcare.LoadTests.Helpers;

/// <summary>
/// Authenticates ONCE at startup and reuses the JWT for the whole run.
///
/// WHY (learning note):
/// - Real clients log in once per session, then call the API many times with
///   the bearer token. Logging in before EVERY request would load-test
///   password hashing (BCrypt/PBKDF2 by Identity) instead of your API, and
///   would risk account lockout.
/// - The API issues tokens valid for 60 minutes (Jwt:ExpirationMinutes), far
///   longer than any default profile (longest ~7 min), so one token is enough.
///   If you extend runs beyond ~50 minutes, restart the load test to log in
///   again (documented in README).
/// </summary>
public static class AuthHelper
{
    /// <summary>
    /// POST api/auth/login. Returns the raw access token.
    /// Throws InvalidOperationException with a human-readable message on failure.
    /// </summary>
    public static async Task<string> LoginAsync(
        HttpClient client,
        string email,
        string password,
        CancellationToken ct = default)
    {
        using var response = await client.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequestPayload(email, password),
            JsonContext.Options,
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Login failed with HTTP {(int)response.StatusCode}. " +
                $"Check LOADTEST_EMAIL / LOADTEST_PASSWORD. Body: {Truncate(body, 300)}");
        }

        var login = System.Text.Json.JsonSerializer.Deserialize<LoginResponseDto>(body, JsonContext.Options);

        if (string.IsNullOrWhiteSpace(login?.AccessToken))
        {
            throw new InvalidOperationException(
                "Login returned 200 but no accessToken was found in the response. " +
                "The API contract may have changed; inspect POST api/auth/login.");
        }

        return login.AccessToken;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";
}
