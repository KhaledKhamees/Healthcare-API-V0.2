using System.Text.Json;
using FluentAssertions;

namespace Healthcare_API_V0._2.Tests.API.Middleware;

public sealed class SerilogConfigurationTests
{
    private static JsonDocument LoadApiSettings()
    {
        // Tests run from Healthcare-API_V0.2.Tests/bin/<cfg>/net8.0.
        var testBinDir = AppContext.BaseDirectory;
        var apiSettingsPath = Path.GetFullPath(
            Path.Combine(testBinDir, "..", "..", "..", "..", "Healthcare-API_V0.2_API", "appsettings.json"));

        File.Exists(apiSettingsPath).Should().BeTrue("API appsettings.json must be discoverable");
        return JsonDocument.Parse(File.ReadAllText(apiSettingsPath));
    }

    [Fact]
    public void SerilogSection_WritesStructuredJsonToConsoleOnly()
    {
        using var settings = LoadApiSettings();
        var serilog = settings.RootElement.GetProperty("Serilog");

        var sinks = serilog.GetProperty("WriteTo").EnumerateArray().ToList();
        sinks.Should().HaveCount(1, "only the Console sink is allowed (no File, no Elasticsearch)");
        sinks[0].GetProperty("Name").GetString().Should().Be("Console");
        sinks[0].GetProperty("Args").GetProperty("formatter").GetString()
            .Should().Contain("Compact")
            .And.Contain("JsonFormatter");
    }

    [Fact]
    public void SerilogSection_EnrichesWithContextAndTraceCorrelation()
    {
        using var settings = LoadApiSettings();
        var enrich = settings.RootElement.GetProperty("Serilog").GetProperty("Enrich")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToList();

        enrich.Should().Contain("FromLogContext");
        enrich.Should().Contain("WithSpan", "Activity-based TraceId/SpanId must flow into every log event");
    }
}
