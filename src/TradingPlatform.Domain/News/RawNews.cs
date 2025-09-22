using System;

namespace TradingPlatform.Domain.News
{
    /// <summary>Raw, unprocessed news item.</summary>
    public sealed record RawNews(
        Guid Id,
        string Source,
        string Title,
        string Url,
        DateTimeOffset PublishedAt,
        DateTimeOffset IngestedAt,
        string Hash
    );
}
