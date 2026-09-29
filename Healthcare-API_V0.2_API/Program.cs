using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Healthcare_API_V0._2_API.Middleware;
using Healthcare_API_V0._2_Infrastructure;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Clear default .NET logging providers
builder.Logging.ClearProviders();

builder.Services.AddAllElasticApm();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(
        new[] { new Uri("https://localhost:9200") },
        options =>
        {
            // Data stream:
            // logs-healthcare-api-default
            options.DataStream = new DataStreamName(
                "logs",
                "healthcare-api",
                "default");

            // Let the sink create the required ECS templates/data stream
            options.BootstrapMethod = BootstrapMethod.Failure;
        },
        transport =>
        {
            // Elasticsearch authentication
            transport.Authentication(
                new BasicAuthentication(
                    "elastic",
                    builder.Configuration["Elasticsearch:Password"]!));

            // Development only:
            // Elasticsearch Docker uses a self-signed certificate.
            transport.ServerCertificateValidationCallback(
                (sender, certificate, chain, sslPolicyErrors) => true);
        })
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Global exception middleware
builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

// CORS
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

// Seed Identity roles
using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await IdentitySeeder.SeedRolesAsync(roleManager);
}

// Serilog HTTP request logging
app.UseSerilogRequestLogging();

// Global exception handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Swagger
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

app.Run();

