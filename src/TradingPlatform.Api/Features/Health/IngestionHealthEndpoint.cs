using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using TradingPlatform.Domain.Ingestion;

namespace TradingPlatform.Api.Features.Health
{
    public sealed class IngestionHealthEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/health/ingestion", (IOptions<IngestionOptions> opt) =>
            {
                var feeds = opt.Value?.Feeds ?? new();
                return Results.Ok(new
                {
                    env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    feedCount = feeds.Count,
                    feeds
                });
            })
            .WithName("IngestionHealth")
            .Produces(StatusCodes.Status200OK)
            .WithTags("Health");
        }
    }
}
