using Dapper;
using Microsoft.Data.SqlClient;
using TradingPlatform.Application.Signals;
using TradingPlatform.Domain;

namespace TradingPlatform.Infrastructure.Prices;

public sealed class SqlPriceRepository(string connectionString) : IPriceRepository
{
    public async Task<IReadOnlyList<PricePoint>> GetRecentAsync(string symbol, int minutes, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (@count)
    [TimestampUtc] AS [Timestamp],
    CAST([Close] AS decimal(18,6)) AS [Close]
FROM [dbo].[Prices]
WHERE [Symbol] = @symbol AND [TimestampUtc] >= DATEADD(minute, -@count, SYSUTCDATETIME())
ORDER BY [TimestampUtc] ASC;";

        await using var conn = new SqlConnection(connectionString);
        var rows = await conn.QueryAsync<PricePoint>(new CommandDefinition(sql, new { symbol, count = minutes }, cancellationToken: ct));
        return rows.ToList();
    }
}
