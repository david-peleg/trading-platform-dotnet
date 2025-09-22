using Carter;
using MediatR;
using TradingPlatform.Application.Signals;

namespace TradingPlatform.Api.Features.Signals;

public sealed class GenerateShortTermEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/signals/short-term/generate",
            async (GenerateShortTermCommand cmd, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(cmd, ct)))
           .WithName("GenerateShortTermSignal")
           .Produces<GenerateShortTermResult>(StatusCodes.Status200OK);
    }
}
