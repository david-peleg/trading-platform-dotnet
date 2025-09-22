using Carter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TradingPlatform.Application.Signals;
using TradingPlatform.Infrastructure.Prices;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// DI
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GenerateShortTermCommand>());
builder.Services.AddCarter();
builder.Services.AddSingleton<IPriceRepository, InMemoryPriceRepository>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("TradingPlatform.Api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation());

var app = builder.Build();
app.MapCarter();
app.Run();
