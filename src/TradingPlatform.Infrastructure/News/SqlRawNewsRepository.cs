using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TradingPlatform.Domain.News;

namespace TradingPlatform.Infrastructure.News
{
    public interface IRawNewsRepository
    {
        /// <summary>Upsert (by Hash) a single RawNews item. Returns affected row count (0/1).</summary>
        Task<int> UpsertAsync(RawNews item, CancellationToken ct = default);
    }

    public sealed class SqlRawNewsRepository : IRawNewsRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlRawNewsRepository> _logger;

        public SqlRawNewsRepository(string connectionString, ILogger<SqlRawNewsRepository> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> UpsertAsync(RawNews item, CancellationToken ct = default)
        {
            const string sql = @"
MERGE INTO [dbo].[RawNews] AS T
USING (VALUES (@Id, @Source, @Title, @Url, @PublishedAt, @IngestedAt, @Hash))
       AS S (Id, Source, Title, Url, PublishedAt, IngestedAt, Hash)
   ON T.[Hash] = S.[Hash]
WHEN MATCHED THEN
    UPDATE SET
        T.[Source]      = S.[Source],
        T.[Title]       = S.[Title],
        T.[Url]         = S.[Url],
        T.[PublishedAt] = S.[PublishedAt],
        T.[IngestedAt]  = S.[IngestedAt]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [Source], [Title], [Url], [PublishedAt], [IngestedAt], [Hash])
    VALUES (S.[Id], S.[Source], S.[Title], S.[Url], S.[PublishedAt], S.[IngestedAt], S.[Hash])
OUTPUT $action;";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            // MERGE OUTPUT returns one row ('INSERT' or 'UPDATE') when a change occurs.
            var actions = await conn.QueryAsync<string>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        item.Id,
                        item.Source,
                        item.Title,
                        item.Url,
                        item.PublishedAt,
                        item.IngestedAt,
                        item.Hash
                    },
                    cancellationToken: ct));

            var count = 0;
            foreach (var _ in actions) count++;

            _logger.LogDebug("RawNews upsert affected {Count} row(s) for Hash {Hash}.", count, item.Hash);
            return count;
        }
    }
}
