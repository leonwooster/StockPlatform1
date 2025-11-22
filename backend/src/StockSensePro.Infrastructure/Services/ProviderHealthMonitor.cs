using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StockSensePro.Infrastructure.Services
{
    /// <summary>
    /// Monitors the health status of data providers.
    /// Tracks health metrics, response times, and failure rates for each provider.
    /// Implements periodic health checks and provides real-time health status.
    /// </summary>
    public class ProviderHealthMonitor : IProviderHealthMonitor, IDisposable
    {
        private readonly IStockDataProviderFactory _factory;
        private readonly ILogger<ProviderHealthMonitor> _logger;
        private readonly DataProviderSettings _settings;
        private readonly ConcurrentDictionary<DataProviderType, ProviderHealth> _healthStatus;
        private readonly ConcurrentDictionary<DataProviderType, List<TimeSpan>> _recentResponseTimes;
        private readonly object _lock = new();
        private Timer? _healthCheckTimer;
        private bool _disposed;

        private const int MaxResponseTimeSamples = 100; // Keep last 100 response times for averaging

        /// <summary>
        /// Initializes a new instance of the ProviderHealthMonitor
        /// </summary>
        public ProviderHealthMonitor(
            IStockDataProviderFactory factory,
            ILogger<ProviderHealthMonitor> logger,
            IOptions<DataProviderSettings> settings)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            
            _healthStatus = new ConcurrentDictionary<DataProviderType, ProviderHealth>();
            _recentResponseTimes = new ConcurrentDictionary<DataProviderType, List<TimeSpan>>();

            // Initialize health status for all available providers
            InitializeHealthStatuses();
        }

        /// <summary>
        /// Initializes health status for all available providers
        /// </summary>
        private void InitializeHealthStatuses()
        {
            foreach (var providerType in _factory.GetAvailableProviders())
            {
                _healthStatus.TryAdd(providerType, new ProviderHealth
                {
                    IsHealthy = true,
                    LastChecked = DateTime.UtcNow,
                    ConsecutiveFailures = 0,
                    AverageResponseTime = TimeSpan.Zero
                });
                _recentResponseTimes.TryAdd(providerType, new List<TimeSpan>());
            }

            _logger.LogInformation("Initialized health monitoring for {Count} providers", _healthStatus.Count);
        }

        /// <summary>
        /// Performs a health check on the specified provider
        /// </summary>
        public async Task CheckHealthAsync(DataProviderType provider, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Performing health check for provider: {Provider}", provider);

            var stopwatch = Stopwatch.StartNew();
            bool isHealthy = false;

            try
            {
                var providerInstance = _factory.CreateProvider(provider);
                isHealthy = await providerInstance.IsHealthyAsync(cancellationToken);
                stopwatch.Stop();

                if (isHealthy)
                {
                    RecordSuccess(provider, stopwatch.Elapsed);
                    _logger.LogInformation(
                        "Health check passed for {Provider} in {Duration}ms",
                        provider,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    RecordFailure(provider);
                    _logger.LogWarning("Health check failed for {Provider} - provider reported unhealthy", provider);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordFailure(provider);
                _logger.LogError(
                    ex,
                    "Health check failed for {Provider} with exception after {Duration}ms",
                    provider,
                    stopwatch.ElapsedMilliseconds);
            }

            // Update last checked timestamp
            if (_healthStatus.TryGetValue(provider, out var health))
            {
                health.LastChecked = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets the current health status for a specific provider
        /// </summary>
        public ProviderHealth? GetHealthStatus(DataProviderType provider)
        {
            return _healthStatus.TryGetValue(provider, out var health) ? health : null;
        }

        /// <summary>
        /// Gets the health status for all providers
        /// </summary>
        public Dictionary<DataProviderType, ProviderHealth> GetAllHealthStatuses()
        {
            return new Dictionary<DataProviderType, ProviderHealth>(_healthStatus);
        }

        /// <summary>
        /// Records a successful request to a provider
        /// </summary>
        public void RecordSuccess(DataProviderType provider, TimeSpan responseTime)
        {
            if (!_healthStatus.TryGetValue(provider, out var health))
            {
                _logger.LogWarning("Attempted to record success for unknown provider: {Provider}", provider);
                return;
            }

            lock (_lock)
            {
                // Reset consecutive failures on success
                health.ConsecutiveFailures = 0;
                health.IsHealthy = true;

                // Add response time to recent samples
                if (_recentResponseTimes.TryGetValue(provider, out var responseTimes))
                {
                    responseTimes.Add(responseTime);

                    // Keep only the most recent samples
                    if (responseTimes.Count > MaxResponseTimeSamples)
                    {
                        responseTimes.RemoveAt(0);
                    }

                    // Calculate average response time
                    health.AverageResponseTime = TimeSpan.FromMilliseconds(
                        responseTimes.Average(t => t.TotalMilliseconds));
                }
            }

            _logger.LogDebug(
                "Recorded success for {Provider} - Response time: {Duration}ms, Avg: {AvgDuration}ms",
                provider,
                responseTime.TotalMilliseconds,
                health.AverageResponseTime.TotalMilliseconds);
        }

        /// <summary>
        /// Records a failed request to a provider
        /// </summary>
        public void RecordFailure(DataProviderType provider)
        {
            if (!_healthStatus.TryGetValue(provider, out var health))
            {
                _logger.LogWarning("Attempted to record failure for unknown provider: {Provider}", provider);
                return;
            }

            lock (_lock)
            {
                health.ConsecutiveFailures++;

                // Mark as unhealthy after 3 consecutive failures
                if (health.ConsecutiveFailures >= 3)
                {
                    health.IsHealthy = false;
                    _logger.LogWarning(
                        "Provider {Provider} marked as unhealthy after {Failures} consecutive failures",
                        provider,
                        health.ConsecutiveFailures);
                }
                else
                {
                    _logger.LogDebug(
                        "Recorded failure for {Provider} - Consecutive failures: {Failures}",
                        provider,
                        health.ConsecutiveFailures);
                }
            }
        }

        /// <summary>
        /// Starts periodic health checks for all configured providers
        /// </summary>
        public void StartPeriodicHealthChecks()
        {
            if (_healthCheckTimer != null)
            {
                _logger.LogWarning("Periodic health checks are already running");
                return;
            }

            var intervalMs = _settings.HealthCheckIntervalSeconds * 1000;
            _healthCheckTimer = new Timer(
                PerformPeriodicHealthChecks,
                null,
                TimeSpan.Zero, // Start immediately
                TimeSpan.FromMilliseconds(intervalMs));

            _logger.LogInformation(
                "Started periodic health checks with interval of {Interval} seconds",
                _settings.HealthCheckIntervalSeconds);
        }

        /// <summary>
        /// Stops periodic health checks
        /// </summary>
        public void StopPeriodicHealthChecks()
        {
            if (_healthCheckTimer != null)
            {
                _healthCheckTimer.Dispose();
                _healthCheckTimer = null;
                _logger.LogInformation("Stopped periodic health checks");
            }
        }

        /// <summary>
        /// Callback for periodic health checks
        /// </summary>
        private async void PerformPeriodicHealthChecks(object? state)
        {
            _logger.LogDebug("Performing periodic health checks for all providers");

            var tasks = _factory.GetAvailableProviders()
                .Select(provider => CheckHealthAsync(provider, CancellationToken.None))
                .ToList();

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogDebug("Completed periodic health checks for all providers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic health checks");
            }
        }

        /// <summary>
        /// Disposes resources used by the health monitor
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopPeriodicHealthChecks();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
