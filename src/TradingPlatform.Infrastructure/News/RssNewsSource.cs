using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingPlatform.Domain.Ingestion;
using TradingPlatform.Domain.News;

namespace TradingPlatform.Infrastructure.News
{
    /// <summary>
    /// Reads RSS/Atom feeds via HttpClient + SyndicationFeed and maps to RawNews.
    /// Hash = SHA256("Ticker|Headline|Url|Source|PublishedAtUtcTicks")
    /// </summary>
    public sealed class RssNewsSource : IRssNewsSource
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RssNewsSource> _logger;
        private readonly IReadOnlyList<string> _feedUrls;

        public RssNewsSource(
            IHttpClientFactory httpClientFactory,
            IOptions<IngestionOptions> options,
            ILogger<RssNewsSource> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _feedUrls = (options?.Value?.Feeds ?? new()).AsReadOnly();

            _logger.LogInformation("RSS: loaded {Count} feed URLs from configuration.", _feedUrls.Count);
        }

        public async IAsyncEnumerable<RawNews> GetItemsAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            if (_feedUrls.Count == 0)
                yield break;

            var client = _httpClientFactory.CreateClient(nameof(RssNewsSource));
            _logger.LogInformation("RSS: starting fetch for {Count} feeds.", _feedUrls.Count);

            foreach (var feedUrl in _feedUrls)
            {
                if (string.IsNullOrWhiteSpace(feedUrl)) continue;

                List<RawNews> items;
                try
                {
                    items = await ReadFeedAsync(client, feedUrl, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RSS: failed to read {FeedUrl}", feedUrl);
                    continue;
                }

                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return item;
                }
            }
        }

        // Watchdog-guarded fetch + parse
        private async Task<List<RawNews>> ReadFeedAsync(HttpClient client, string feedUrl, CancellationToken ct)
        {
            var list = new List<RawNews>();
            var sw = Stopwatch.StartNew();

            // timeout כולל פר בקשה (גם אם שכבות נמוכות לא מכבדות ביטול)
            var overallTimeout = TimeSpan.FromSeconds(20);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(overallTimeout);

            try
            {
                _logger.LogInformation("RSS: fetching {Url} (timeout {Seconds}s)", feedUrl, overallTimeout.TotalSeconds);

                using var req = new HttpRequestMessage(HttpMethod.Get, feedUrl);

                // נריץ עם ResponseHeadersRead כדי לא לחכות לכל הגוף
                var sendTask = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);

                // Watchdog: אם עבר overallTimeout, נבטל ונזרוק TimeoutException ברורה
                var completed = await Task.WhenAny(sendTask, Task.Delay(overallTimeout, ct)).ConfigureAwait(false);
                if (completed != sendTask)
                {
                    try { linkedCts.Cancel(); } catch { /* ignore */ }
                    throw new TimeoutException($"RSS: timeout after {overallTimeout.TotalSeconds:n0}s for {feedUrl}");
                }

                using var resp = await sendTask.ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();

                var mediaType = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? string.Empty;
                if (!(mediaType.Contains("xml") || mediaType.Contains("rss") || mediaType.Contains("atom")))
                {
                    _logger.LogWarning("RSS: unexpected content-type {MediaType} from {Url}", mediaType, feedUrl);
                    return list;
                }

                await using var stream = await resp.Content.ReadAsStreamAsync(linkedCts.Token).ConfigureAwait(false);
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

                var feed = SyndicationFeed.Load(reader);
                var sourceName = feed?.Title?.Text?.Trim() ?? new Uri(feedUrl).Host;

                if (feed?.Items == null)
                {
                    _logger.LogInformation("RSS: parsed 0 items from {Source} in {Ms} ms", sourceName, sw.ElapsedMilliseconds);
                    return list;
                }

                foreach (var item in feed.Items)
                {
                    var headline = item.Title?.Text?.Trim() ?? "(no title)";
                    var link = item.Links.Count > 0 ? item.Links[0].Uri?.ToString() ?? string.Empty : string.Empty;
                    var published = item.PublishDate != DateTimeOffset.MinValue
                        ? item.PublishDate
                        : (item.LastUpdatedTime != DateTimeOffset.MinValue ? item.LastUpdatedTime : DateTimeOffset.UtcNow);

                    var ticker = ExtractTickerFromHeadline(headline);
                    var hash = ComputeHash(ticker, headline, link, sourceName, published);

                    list.Add(new RawNews(
                        Id: Guid.NewGuid(),
                        Source: sourceName,
                        Title: headline,
                        Url: link,
                        PublishedAt: published,
                        IngestedAt: DateTimeOffset.UtcNow,
                        Hash: hash
                    ));
                }

                _logger.LogInformation("RSS: parsed {Count} items from {Source} in {Ms} ms", list.Count, sourceName, sw.ElapsedMilliseconds);
                return list;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // בוטל בגלל הטיימר הפנימי (timeout)
                throw new TimeoutException($"RSS: timeout after {overallTimeout.TotalSeconds:n0}s for {feedUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RSS: failed to fetch {Url} after {Ms} ms", feedUrl, sw.ElapsedMilliseconds);
                return list; // נכשל “רך” — ממשיכים לפיד הבא
            }
        }

        private static string ExtractTickerFromHeadline(string headline)
        {
            // מחלץ סימבול: (AAPL), [MSFT], $NVDA
            if (string.IsNullOrWhiteSpace(headline))
                return "-";

            var m = Regex.Match(headline, @"(?:\(|\[|\$)\s*([A-Z]{1,5})\s*(?:\)|\])?");
            return m.Success ? m.Groups[1].Value : "-";
        }

        private static string ComputeHash(string ticker, string headline, string url, string source, DateTimeOffset publishedAt)
        {
            using var sha = SHA256.Create();
            var input = $"{ticker}|{headline}|{url}|{source}|{publishedAt.UtcTicks}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
