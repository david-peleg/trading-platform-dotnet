namespace TradingPlatform.Domain;

public readonly record struct PricePoint(DateTime Timestamp, decimal Close);

public enum SignalKind { Buy, Sell, Hold }
