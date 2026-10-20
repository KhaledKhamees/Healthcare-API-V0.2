using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Healthcare_API_V0._2_API.Middleware;
using Healthcare_API_V0._2_Infrastructure;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;
// GG
var builder = WebApplication.CreateBuilder(args);

SelfLog.Enable(message => Console.Error.WriteLine($"[Serilog SelfLog] {message}"));

builder.Logging.ClearProviders();
builder.Services.AddAllElasticApm();
try
{
    Log.Logger = BuildLogger(builder.Configuration);
}
catch (Exception ex)
{
    Console.Error.WriteLine(
        $"Serilog Elasticsearch initialization failed ({ex.GetType().Name}): {ex.Message}. Continuing with console-only logging.");

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .CreateLogger();
}
builder.Host.UseSerilog();
try
{
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        await DatabaseInitializer.MigrateAsync(scope.ServiceProvider, logger);
    }

    using (var scope = app.Services.CreateScope())
    {
        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await IdentitySeeder.SeedRolesAsync(roleManager);
    }

    app.UseSerilogRequestLogging();

    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Healthcare API starting in {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Healthcare API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static Serilog.Core.Logger BuildLogger(IConfiguration configuration)
{
    var loggerConfig = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration);

    var elasticsearchUri = configuration["Elasticsearch:Uri"]
        ?? throw new InvalidOperationException("Elasticsearch:Uri is not configured");

    loggerConfig.WriteTo.Elasticsearch(
        new[] { new Uri(elasticsearchUri) },
        options =>
        {

            options.DataStream = new DataStreamName(
                "logs",
                "healthcare-api",
                "default");
            options.BootstrapMethod = BootstrapMethod.Silent;
        },
        transport =>
        {
            transport.ServerCertificateValidationCallback(
                (sender, certificate, chain, sslPolicyErrors) => true);
        });

    return loggerConfig.CreateLogger();
}

