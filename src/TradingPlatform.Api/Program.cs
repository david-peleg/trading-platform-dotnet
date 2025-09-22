using Carter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Net;
using TradingPlatform.Application.Ingestion;
using TradingPlatform.Application.Signals;
using TradingPlatform.Domain.Ingestion;
using TradingPlatform.Infrastructure.News;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// MediatR (השאר רק אם יש לך את הסטאב/Handlers)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<GenerateShortTermCommand>());

// Carter
builder.Services.AddCarter();

// Options binding (IngestionOptions בדומיין)
builder.Services.Configure<IngestionOptions>(
    builder.Configuration.GetSection("Ingestion"));

// HttpClient ל-RSS עם טיימאאוטים, דה־קומפרסיה ו-User-Agent
builder.Services.AddHttpClient(nameof(RssNewsSource), c =>
{
    c.Timeout = TimeSpan.FromSeconds(25); // טיימאאוט כולל לבקשה
    c.DefaultRequestHeaders.UserAgent.ParseAdd("TradingPlatformNET8/1.0 (+https://localhost)");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml, text/xml;q=0.9, */*;q=0.8");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    ConnectTimeout = TimeSpan.FromSeconds(7), // DNS/TCP/TLS
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    AllowAutoRedirect = true,
    UseProxy = true,
    Proxy = WebRequest.DefaultWebProxy,
    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = 8
});

// Repositories + Sources + UseCases
builder.Services.AddSingleton<IRawNewsRepository>(sp =>
    new SqlRawNewsRepository(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("TradingPlatformNet8")!,
        sp.GetRequiredService<ILogger<SqlRawNewsRepository>>()));

builder.Services.AddSingleton<IRssNewsSource, RssNewsSource>();
builder.Services.AddSingleton<INewsIngestionUseCase, NewsIngestionUseCase>();

// Worker יומי
builder.Services.AddHostedService<DailyIngestionWorker>();

// OpenTelemetry Tracing (ודא התקנת OpenTelemetry.Instrumentation.AspNetCore)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("TradingPlatform.Api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation());

var app = builder.Build();

app.MapCarter();

app.Run();
