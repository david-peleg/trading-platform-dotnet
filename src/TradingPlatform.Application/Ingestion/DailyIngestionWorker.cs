using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradingPlatform.Application.Ingestion
{
    /// <summary>
    /// Minimal BackgroundService that runs INewsIngestionUseCase once a day at the configured UTC hour.
    /// appsettings: "Ingestion": { "DailyRunUtcHour": 4 }
    /// </summary>
    public sealed class DailyIngestionWorker : BackgroundService
    {
        private readonly INewsIngestionUseCase _useCase;
        private readonly ILogger<DailyIngestionWorker> _logger;
        private readonly int _dailyUtcHour;

        public DailyIngestionWorker(
            INewsIngestionUseCase useCase,
            ILogger<DailyIngestionWorker> logger,
            IConfiguration configuration)
        {
            _useCase = useCase ?? throw new ArgumentNullException(nameof(useCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var hour = configuration.GetValue<int?>("Ingestion:DailyRunUtcHour") ?? 4;
            _dailyUtcHour = (hour is >= 0 and <= 23) ? hour : 4;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyIngestionWorker started. Scheduled UTC hour: {Hour}", _dailyUtcHour);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                var next = new DateTimeOffset(now.Year, now.Month, now.Day, _dailyUtcHour, 0, 0, TimeSpan.Zero);
                if (next <= now) next = next.AddDays(1);

                var delay = next - now;
                _logger.LogInformation("Next run at {Next:u}. Sleeping for {Delay}.", next, delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // shutdown
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    _logger.LogInformation("Running daily news ingestion at {Now:u}.", DateTimeOffset.UtcNow);
                    await _useCase.RunOnceAsync(stoppingToken);
                    _logger.LogInformation("Daily news ingestion completed at {Now:u}.", DateTimeOffset.UtcNow);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Daily news ingestion failed.");
                    // Continue loop to schedule next day
                }
            }

            _logger.LogInformation("DailyIngestionWorker stopped.");
        }
    }
}
