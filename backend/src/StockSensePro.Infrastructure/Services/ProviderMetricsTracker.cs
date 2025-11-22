using Microsoft.Extensions.Logging;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using System.Collections.Concurrent;

namespace StockSensePro.Infrastructure.Services
{
    /// <summary>
    /// Tracks API call metrics per provider
    /// Thread-safe implementation using concurrent collections
    /// </summary>
    public class ProviderMetricsTracker : IProviderMetricsTracker
    {
        private readonly ILogger<ProviderMetricsTracker> _logger;
        private readonly IProviderCostTracker? _costTracker;
        private readonly ConcurrentDictionary<DataProviderType, ProviderCallMetrics> _metrics;

        /// <summary>
        /// Initializes a new instance of the ProviderMetricsTracker
        /// </summary>
        public ProviderMetricsTracker(
            ILogger<ProviderMetricsTracker> logger,
            IProviderCostTracker? costTracker = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _costTracker = costTracker;
            _metrics = new ConcurrentDictionary<DataProviderType, ProviderCallMetrics>();
        }

        /// <summary>
        /// Records a successful API call to a provider
        /// </summary>
        public void RecordSuccess(DataProviderType provider)
        {
            var metrics = _metrics.GetOrAdd(provider, _ => new ProviderCallMetrics());
            metrics.IncrementSuccess();
            
            // Also record in cost tracker
            _costTracker?.RecordApiCall(provider);
            
            _logger.LogDebug(
                "Recorded success for {Provider} - Total: {Total}, Success: {Success}",
                provider,
                metrics.TotalRequests,
                metrics.SuccessfulRequests);
        }

        /// <summary>
        /// Records a failed API call to a provider
        /// </summary>
        public void RecordFailure(DataProviderType provider)
        {
            var metrics = _metrics.GetOrAdd(provider, _ => new ProviderCallMetrics());
            metrics.IncrementFailure();
            
            // Also record in cost tracker (failed calls still count towards cost)
            _costTracker?.RecordApiCall(provider);
            
            _logger.LogDebug(
                "Recorded failure for {Provider} - Total: {Total}, Failed: {Failed}",
                provider,
                metrics.TotalRequests,
                metrics.FailedRequests);
        }

        /// <summary>
        /// Gets the total number of requests for a provider
        /// </summary>
        public long GetTotalRequests(DataProviderType provider)
        {
            return _metrics.TryGetValue(provider, out var metrics) ? metrics.TotalRequests : 0;
        }

        /// <summary>
        /// Gets the number of successful requests for a provider
        /// </summary>
        public long GetSuccessfulRequests(DataProviderType provider)
        {
            return _metrics.TryGetValue(provider, out var metrics) ? metrics.SuccessfulRequests : 0;
        }

        /// <summary>
        /// Gets the number of failed requests for a provider
        /// </summary>
        public long GetFailedRequests(DataProviderType provider)
        {
            return _metrics.TryGetValue(provider, out var metrics) ? metrics.FailedRequests : 0;
        }

        /// <summary>
        /// Gets metrics for all providers
        /// </summary>
        public Dictionary<DataProviderType, (long Total, long Success, long Failed)> GetAllMetrics()
        {
            return _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.TotalRequests, kvp.Value.SuccessfulRequests, kvp.Value.FailedRequests));
        }

        /// <summary>
        /// Resets metrics for a specific provider
        /// </summary>
        public void ResetMetrics(DataProviderType provider)
        {
            if (_metrics.TryRemove(provider, out _))
            {
                _logger.LogInformation("Reset metrics for provider: {Provider}", provider);
            }
        }

        /// <summary>
        /// Resets metrics for all providers
        /// </summary>
        public void ResetAllMetrics()
        {
            _metrics.Clear();
            _logger.LogInformation("Reset metrics for all providers");
        }

        /// <summary>
        /// Internal class to track metrics for a single provider
        /// </summary>
        private class ProviderCallMetrics
        {
            private long _totalRequests;
            private long _successfulRequests;
            private long _failedRequests;

            public long TotalRequests => Interlocked.Read(ref _totalRequests);
            public long SuccessfulRequests => Interlocked.Read(ref _successfulRequests);
            public long FailedRequests => Interlocked.Read(ref _failedRequests);

            public void IncrementSuccess()
            {
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _successfulRequests);
            }

            public void IncrementFailure()
            {
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _failedRequests);
            }
        }
    }
}
