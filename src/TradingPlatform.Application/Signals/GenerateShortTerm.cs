using MediatR;
using TradingPlatform.Domain;

namespace TradingPlatform.Application.Signals;

public sealed record GenerateShortTermCommand(
    string Symbol,
    int Lookback = 30,         // דק'
    int HorizonMinutes = 5     // דק'
) : IRequest<GenerateShortTermResult>;

public sealed record GenerateShortTermResult(
    string Symbol,
    SignalKind Signal,
    double Confidence,
    DateTime AsOfUtc
);

public interface IPriceRepository
{
    Task<IReadOnlyList<PricePoint>> GetRecentAsync(string symbol, int minutes, CancellationToken ct);
}

public sealed class GenerateShortTermHandler(IPriceRepository repo) : IRequestHandler<GenerateShortTermCommand, GenerateShortTermResult>
{
    public async Task<GenerateShortTermResult> Handle(GenerateShortTermCommand request, CancellationToken ct)
    {
        var minutes = Math.Max(request.Lookback + request.HorizonMinutes, 10);
        var prices = await repo.GetRecentAsync(request.Symbol, minutes, ct);
        if (prices.Count < request.Lookback)
            return new(request.Symbol, SignalKind.Hold, 0.0, DateTime.UtcNow);

        // פיצ'ר פשוט: ממוצע חציוני לעומת חצי שני (momentum)
        var last = prices.OrderBy(p => p.Timestamp).TakeLast(request.Lookback).ToArray();
        var h = request.Lookback / 2;
        var avgOld = (double)last.Take(h).Average(p => p.Close);
        var avgNew = (double)last.Skip(h).Average(p => p.Close);
        var diff = avgNew - avgOld;
        var rel = avgOld == 0 ? 0 : diff / (double)avgOld;

        var signal = Math.Abs(rel) < 0.0005 ? SignalKind.Hold : (rel > 0 ? SignalKind.Buy : SignalKind.Sell);
        var confidence = Math.Min(0.99, Math.Abs(rel) * 500); // נרמול גס

        return new(request.Symbol, signal, confidence, DateTime.UtcNow);
    }
}
