using System.Collections.Generic;
using System.Threading;
using TradingPlatform.Domain.News;

namespace TradingPlatform.Infrastructure.News
{
    /// <summary>RSS source that yields RawNews items.</summary>
    public interface IRssNewsSource
    {
        IAsyncEnumerable<RawNews> GetItemsAsync(CancellationToken ct = default);
    }
}
