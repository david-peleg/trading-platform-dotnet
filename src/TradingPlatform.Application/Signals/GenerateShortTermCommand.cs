using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace TradingPlatform.Application.Signals
{
    // Command placeholder (no payload for now)
    public sealed record GenerateShortTermCommand() : IRequest;

    // No-op handler to satisfy DI/mediator wiring
    public sealed class GenerateShortTermHandler : IRequestHandler<GenerateShortTermCommand>
    {
        public Task<Unit> Handle(GenerateShortTermCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Unit.Value);

        Task IRequestHandler<GenerateShortTermCommand>.Handle(GenerateShortTermCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
