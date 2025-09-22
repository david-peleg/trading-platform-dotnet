using TradingPlatform.Application.Signals;
using TradingPlatform.Domain;

namespace TradingPlatform.Infrastructure.Prices;

public sealed class InMemoryPriceRepository : IPriceRepository
{
    public Task<IReadOnlyList<PricePoint>> GetRecentAsync(string symbol, int minutes, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var list = Enumerable.Range(0, minutes)
            .Select(i =>
            {
                var t = now.AddMinutes(-minutes + i + 1);
                var baseVal = 100m + (decimal)Math.Sin(i / 6.0) * 0.6m;
                var noise = (decimal)(Random.Shared.NextDouble() - 0.5) * 0.15m;
                return new PricePoint(t, baseVal + noise);
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<PricePoint>>(list);
    }
}
