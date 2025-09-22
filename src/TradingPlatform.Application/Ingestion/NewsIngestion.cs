using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingPlatform.Infrastructure.News; // <-- כאן הממשק IRssNewsSource
// IRawNewsRepository גם מוגדר ב-Infrastructure.News

namespace TradingPlatform.Application.Ingestion
{
    public interface INewsIngestionUseCase
    {
        Task RunOnceAsync(CancellationToken ct = default);
    }

    public sealed class NewsIngestionUseCase : INewsIngestionUseCase
    {
        private readonly IRawNewsRepository _repo;
        private readonly IRssNewsSource _rss;
        private readonly ILogger<NewsIngestionUseCase> _logger;

        public NewsIngestionUseCase(
            IRawNewsRepository repo,
            IRssNewsSource rss,
            ILogger<NewsIngestionUseCase> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _rss = rss ?? throw new ArgumentNullException(nameof(rss));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunOnceAsync(CancellationToken ct = default)
        {
            var count = 0;
            await foreach (var item in _rss.GetItemsAsync(ct))
            {
                await _repo.UpsertAsync(item, ct);
                count++;
            }

            _logger.LogInformation("News ingestion completed. Upserted {Count} items.", count);
        }
    }
}
