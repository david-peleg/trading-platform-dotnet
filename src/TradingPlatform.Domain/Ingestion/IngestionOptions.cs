using System.Collections.Generic;

namespace TradingPlatform.Domain.Ingestion
{
    /// <summary>
    /// הגדרות אינג'סט: שעה יומית (UTC) ורשימת פידי RSS.
    /// </summary>
    public sealed class IngestionOptions
    {
        public int DailyRunUtcHour { get; init; } = 4;
        public List<string> Feeds { get; init; } = new();
    }
}
