using FluentAssertions;
using TradingPlatform.Application.Signals;
using TradingPlatform.Infrastructure.Prices;

namespace TradingPlatform.Tests;

public class SignalTests
{
    [Fact]
    public async Task Generates_Signal_And_Confidence()
    {
        var repo = new InMemoryPriceRepository();
        var handler = new GenerateShortTermHandler(repo);

        var res = await handler.Handle(new GenerateShortTermCommand("AAPL"), CancellationToken.None);

        res.Symbol.Should().Be("AAPL");
        res.Confidence.Should().BeGreaterThanOrEqualTo(0);
        res.Confidence.Should().BeLessThanOrEqualTo(1);
        // לחלופין: res.Confidence.Should().BeInRange(0, 1);
    }
}
