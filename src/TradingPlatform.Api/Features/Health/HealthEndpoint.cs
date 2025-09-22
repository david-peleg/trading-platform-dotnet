using System;
using System.Threading.Tasks;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TradingPlatform.Api.Features.Health
{
    /// <summary>
    /// Simple health endpoint for Postman / uptime checks.
    /// GET /health  → 200 OK with basic info.
    /// </summary>
    public sealed class HealthEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/health", () =>
            {
                var now = DateTimeOffset.UtcNow;

                return Results.Ok(new
                {
                    status = "OK",
                    service = "TradingPlatform.Api",
                    utc = now.ToString("u"),
                    message_en = "Service is running",
                    message_he = "השירות פעיל"
                });
            })
            .WithName("Health")
            .Produces(StatusCodes.Status200OK)
            .WithTags("Health");
        }
    }
}
