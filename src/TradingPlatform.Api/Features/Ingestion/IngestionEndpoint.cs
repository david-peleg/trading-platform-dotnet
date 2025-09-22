using System.Threading.Tasks;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TradingPlatform.Application.Ingestion;

namespace TradingPlatform.Api.Features.Ingestion
{
    public sealed class IngestionEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/ingestion/news/run", async (INewsIngestionUseCase useCase, HttpContext ctx) =>
            {
                await useCase.RunOnceAsync(ctx.RequestAborted);
                return Results.Accepted();
            })
            .WithName("RunNewsIngestionOnce")
            .Produces(StatusCodes.Status202Accepted)
            .WithTags("Ingestion", "News");
        }
    }
}
