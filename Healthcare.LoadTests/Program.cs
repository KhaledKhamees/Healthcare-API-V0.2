using Healthcare.LoadTests.Configuration;
using Healthcare.LoadTests.Helpers;
using Healthcare.LoadTests.Scenarios;
using Microsoft.Extensions.Configuration;
using NBomber.CSharp;

// ---------------------------------------------------------------------------
// Usage:
//   dotnet run -- --profile baseline   (default; 5 users, ~30 s)
//   dotnet run -- --profile normal     (50 users, 60 s)
//   dotnet run -- --profile ramp       (10 -> 25 -> 50 -> 100 users)
//   dotnet run -- --profile sustained  (50 users for 5 min)
//   dotnet run -- --profile stress     (ramp to 200 + hold; tune first!)
// ---------------------------------------------------------------------------

var profile = ParseProfile(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.GetSection("LoadTest").Get<LoadTestSettings>() ?? new LoadTestSettings();

// Secrets and host overrides come from the environment so they are never
// committed. appsettings.json holds non-secret placeholders only.
var baseUrlOverride = Environment.GetEnvironmentVariable("LOADTEST_BASEURL");
if (!string.IsNullOrWhiteSpace(baseUrlOverride))
    settings.BaseUrl = baseUrlOverride;

var email = Environment.GetEnvironmentVariable("LOADTEST_EMAIL") ?? settings.Auth.Email;
var password = Environment.GetEnvironmentVariable("LOADTEST_PASSWORD") ?? settings.Auth.Password;

Console.WriteLine("=== Healthcare.LoadTests ===");
Console.WriteLine($"API:     {settings.BaseUrl}");
Console.WriteLine($"Profile: {profile} — {LoadProfiles.Describe(profile, settings.Load)}");

// 1) Fail fast on placeholder ids: running with Guid.Empty would produce 100%
//    404s and teach you nothing. The errors tell you exactly what to fill in.
var dataErrors = TestDataProvider.Validate(settings);
if (dataErrors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Missing test data — fill these in appsettings.json (see README 'Test data'):");
    foreach (var error in dataErrors)
        Console.WriteLine($"  - {error}");
    return 2;
}

using var http = HttpClientProvider.Create(settings);
var data = new TestDataProvider(settings);

// 2) Authenticate ONCE and reuse the token (see AuthHelper for why).
//    Anonymous scenarios still run if login is skipped, but authenticated
//    scenarios will report NO_AUTH_TOKEN failures instead of real results.
string? token = null;
if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
{
    try
    {
        token = await AuthHelper.LoginAsync(http, email, password);
        Console.WriteLine($"Auth:    logged in as {email}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Auth:    LOGIN FAILED — {ex.Message}");
        Console.WriteLine("Auth:    continuing; authenticated scenarios will report NO_AUTH_TOKEN.");
    }
}
else
{
    Console.WriteLine("Auth:    no credentials (LOADTEST_EMAIL/LOADTEST_PASSWORD) — running anonymous-only; authenticated scenarios will report NO_AUTH_TOKEN.");
}

// 3) Build the representative suite (NOT one scenario per endpoint).
var scenarios = new[]
{
    ReadScenarios.SimpleReadDoctorById(http, data, token),
    ReadScenarios.ListDoctorsBySpecialization(http, data, token),
    CacheScenarios.CacheHitClinicById(http, data, token),
    CacheScenarios.DbReadAppointmentById(http, data, token),
    WriteScenarios.CreateAppointment(http, data, token),
    WriteScenarios.UpdateAppointment(http, data, token),
    ComplexQueryScenarios.DoctorClinicAppointments(http, data, token),
    MixedWorkloadScenarios.MixedRealisticUserFlow(http, data, token),
}.Select(s => LoadProfiles.Apply(s, profile, settings.Load))
 .ToArray();

Console.WriteLine($"Scenarios: {scenarios.Length} (reports in ./reports)");
Console.WriteLine("Tip: watch NBomber P95 live, then pivot to Kibana APM (README 'APM correlation').");
Console.WriteLine();

// 4) Run. NBomber prints live stats + writes txt/csv/html/md reports.
//    P50/P75/P90/P95/P99, RPS, success/fail counts are built in — no custom
//    measurement code needed (or wanted: don't re-implement the framework).
NBomberRunner
    .RegisterScenarios(scenarios)
    .WithTestSuite("healthcare-api")
    .WithTestName($"healthcare-api-{profile}")
    .WithReportFolder("reports")
    .Run();

return 0;

static string ParseProfile(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if ((args[i] is "--profile" or "-p") && i + 1 < args.Length)
            return args[i + 1].Trim().ToLowerInvariant();

        if (args[i] is "--help" or "-h" or "-?")
        {
            Console.WriteLine("Profiles: " + string.Join(", ", LoadProfiles.All));
            Console.WriteLine("Example: dotnet run -- --profile ramp");
            Environment.Exit(0);
        }
    }

    return LoadProfiles.Baseline;
}
