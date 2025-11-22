using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Configuration;
using StockSensePro.API.Middleware;
using StockSensePro.API.Models;

namespace StockSensePro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IStockDataProvider _stockDataProvider;
        private readonly ILogger<HealthController> _logger;
        private readonly RateLimitMetrics? _rateLimitMetrics;
        private readonly IProviderHealthMonitor? _providerHealthMonitor;
        private readonly IProviderMetricsTracker? _providerMetricsTracker;
        private readonly IAlphaVantageRateLimiter? _alphaVantageRateLimiter;
        private readonly IDataProviderStrategy? _dataProviderStrategy;
        private readonly IStockDataProviderFactory? _providerFactory;
        private readonly IProviderCostTracker? _providerCostTracker;
        private readonly IOptions<ProviderCostSettings>? _costSettings;

        public HealthController(
            IStockDataProvider stockDataProvider, 
            ILogger<HealthController> logger,
            RateLimitMetrics? rateLimitMetrics = null,
            IProviderHealthMonitor? providerHealthMonitor = null,
            IProviderMetricsTracker? providerMetricsTracker = null,
            IAlphaVantageRateLimiter? alphaVantageRateLimiter = null,
            IDataProviderStrategy? dataProviderStrategy = null,
            IStockDataProviderFactory? providerFactory = null,
            IProviderCostTracker? providerCostTracker = null,
            IOptions<ProviderCostSettings>? costSettings = null)
        {
            _stockDataProvider = stockDataProvider;
            _logger = logger;
            _rateLimitMetrics = rateLimitMetrics;
            _providerHealthMonitor = providerHealthMonitor;
            _providerMetricsTracker = providerMetricsTracker;
            _alphaVantageRateLimiter = alphaVantageRateLimiter;
            _dataProviderStrategy = dataProviderStrategy;
            _providerFactory = providerFactory;
            _providerCostTracker = providerCostTracker;
            _costSettings = costSettings;
        }

        /// <summary>
        /// Health check endpoint that tests Yahoo Finance API connectivity
        /// </summary>
        /// <returns>Health status of the Yahoo Finance API</returns>
        [HttpGet]
        public async Task<ActionResult<HealthCheckResponse>> GetHealth(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Health check requested at {Timestamp}", DateTime.UtcNow);

                var isHealthy = await _stockDataProvider.IsHealthyAsync(cancellationToken);

                var response = new HealthCheckResponse
                {
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Service = "Yahoo Finance API",
                    Details = isHealthy 
                        ? "Yahoo Finance API is accessible and responding" 
                        : "Yahoo Finance API is not accessible or not responding"
                };

                _logger.LogInformation(
                    "Health check completed: Status={Status}, Service={Service}, Timestamp={Timestamp}",
                    response.Status,
                    response.Service,
                    response.Timestamp);

                if (isHealthy)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(503, response); // Service Unavailable
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with exception at {Timestamp}", DateTime.UtcNow);

                var response = new HealthCheckResponse
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Service = "Yahoo Finance API",
                    Details = $"Health check failed: {ex.Message}"
                };

                return StatusCode(503, response);
            }
        }

        /// <summary>
        /// Get rate limiting metrics
        /// </summary>
        /// <returns>Rate limiting statistics and metrics</returns>
        [HttpGet("metrics")]
        public ActionResult<RateLimitMetricsSummary> GetMetrics()
        {
            try
            {
                if (_rateLimitMetrics == null)
                {
                    return NotFound(new { message = "Rate limiting metrics are not available" });
                }

                var summary = _rateLimitMetrics.GetSummary();
                
                _logger.LogInformation(
                    "Rate limit metrics requested: TotalRequests={TotalRequests}, TotalRateLimitHits={TotalRateLimitHits}",
                    summary.TotalRequests,
                    summary.TotalRateLimitHits);

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve rate limit metrics");
                return StatusCode(500, new { message = "Failed to retrieve metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get provider-specific metrics including health status, rate limits, and API usage
        /// </summary>
        /// <returns>Comprehensive provider metrics</returns>
        [HttpGet("providers")]
        public ActionResult<ProviderMetricsResponse> GetProviderMetrics()
        {
            try
            {
                _logger.LogInformation("Provider metrics requested at {Timestamp}", DateTime.UtcNow);

                var response = new ProviderMetricsResponse
                {
                    Timestamp = DateTime.UtcNow
                };

                // Get current strategy name if available
                if (_dataProviderStrategy != null)
                {
                    response.Strategy = _dataProviderStrategy.GetStrategyName();
                }

                // Get all available providers
                var availableProviders = _providerFactory?.GetAvailableProviders() ?? Enumerable.Empty<DataProviderType>();

                foreach (var providerType in availableProviders)
                {
                    var providerName = providerType.ToString();
                    var metrics = new ProviderMetrics();

                    // Get API call metrics
                    if (_providerMetricsTracker != null)
                    {
                        metrics.TotalRequests = _providerMetricsTracker.GetTotalRequests(providerType);
                        metrics.SuccessfulRequests = _providerMetricsTracker.GetSuccessfulRequests(providerType);
                        metrics.FailedRequests = _providerMetricsTracker.GetFailedRequests(providerType);
                    }

                    // Get health status
                    if (_providerHealthMonitor != null)
                    {
                        var health = _providerHealthMonitor.GetHealthStatus(providerType);
                        if (health != null)
                        {
                            metrics.IsHealthy = health.IsHealthy;
                            metrics.LastHealthCheck = health.LastChecked;
                            metrics.ConsecutiveFailures = health.ConsecutiveFailures;
                            metrics.AverageResponseTimeMs = health.AverageResponseTime.TotalMilliseconds;
                        }
                    }

                    // Get rate limit info for Alpha Vantage
                    if (providerType == DataProviderType.AlphaVantage && _alphaVantageRateLimiter != null)
                    {
                        var rateLimitStatus = _alphaVantageRateLimiter.GetStatus();
                        metrics.RateLimit = new RateLimitInfo
                        {
                            MinuteRequestsRemaining = rateLimitStatus.MinuteRequestsRemaining,
                            MinuteRequestsLimit = rateLimitStatus.MinuteRequestsLimit,
                            MinuteWindowResetIn = rateLimitStatus.MinuteWindowResetIn,
                            DayRequestsRemaining = rateLimitStatus.DayRequestsRemaining,
                            DayRequestsLimit = rateLimitStatus.DayRequestsLimit,
                            DayWindowResetIn = rateLimitStatus.DayWindowResetIn,
                            IsRateLimited = rateLimitStatus.IsRateLimited
                        };
                    }

                    response.Providers[providerName] = metrics;
                }

                // Determine current provider (the one with most recent successful request or primary)
                if (response.Providers.Any())
                {
                    var activeProvider = response.Providers
                        .Where(p => p.Value.TotalRequests > 0)
                        .OrderByDescending(p => p.Value.SuccessfulRequests)
                        .FirstOrDefault();

                    response.CurrentProvider = activeProvider.Key ?? response.Providers.Keys.FirstOrDefault();
                }

                _logger.LogInformation(
                    "Provider metrics retrieved: {ProviderCount} providers, Strategy: {Strategy}",
                    response.Providers.Count,
                    response.Strategy ?? "Unknown");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve provider metrics");
                return StatusCode(500, new { message = "Failed to retrieve provider metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get health status for all data providers
        /// </summary>
        /// <returns>Health status for each provider</returns>
        [HttpGet("providers/health")]
        public ActionResult<ProviderHealthResponse> GetProviderHealth()
        {
            try
            {
                _logger.LogInformation("Provider health status requested at {Timestamp}", DateTime.UtcNow);

                if (_providerHealthMonitor == null)
                {
                    return NotFound(new { message = "Provider health monitoring is not available" });
                }

                var response = new ProviderHealthResponse
                {
                    Timestamp = DateTime.UtcNow
                };

                // Get all available providers
                var availableProviders = _providerFactory?.GetAvailableProviders() ?? Enumerable.Empty<DataProviderType>();

                foreach (var providerType in availableProviders)
                {
                    var health = _providerHealthMonitor.GetHealthStatus(providerType);
                    if (health != null)
                    {
                        response.Providers[providerType.ToString()] = new ProviderHealthStatus
                        {
                            IsHealthy = health.IsHealthy,
                            LastChecked = health.LastChecked,
                            ConsecutiveFailures = health.ConsecutiveFailures,
                            AverageResponseTimeMs = health.AverageResponseTime.TotalMilliseconds
                        };
                    }
                }

                // Determine overall health
                response.OverallHealthy = response.Providers.Values.Any(p => p.IsHealthy);

                _logger.LogInformation(
                    "Provider health retrieved: {ProviderCount} providers, Overall healthy: {OverallHealthy}",
                    response.Providers.Count,
                    response.OverallHealthy);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve provider health status");
                return StatusCode(500, new { message = "Failed to retrieve provider health status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get API usage metrics for all data providers
        /// </summary>
        /// <returns>API usage metrics for each provider</returns>
        [HttpGet("providers/metrics")]
        public ActionResult<ProviderApiMetricsResponse> GetProviderApiMetrics()
        {
            try
            {
                _logger.LogInformation("Provider API metrics requested at {Timestamp}", DateTime.UtcNow);

                if (_providerMetricsTracker == null)
                {
                    return NotFound(new { message = "Provider metrics tracking is not available" });
                }

                var response = new ProviderApiMetricsResponse
                {
                    Timestamp = DateTime.UtcNow
                };

                // Get current strategy name if available
                if (_dataProviderStrategy != null)
                {
                    response.Strategy = _dataProviderStrategy.GetStrategyName();
                }

                // Get all available providers
                var availableProviders = _providerFactory?.GetAvailableProviders() ?? Enumerable.Empty<DataProviderType>();

                foreach (var providerType in availableProviders)
                {
                    var metrics = new ProviderApiMetrics
                    {
                        TotalRequests = _providerMetricsTracker.GetTotalRequests(providerType),
                        SuccessfulRequests = _providerMetricsTracker.GetSuccessfulRequests(providerType),
                        FailedRequests = _providerMetricsTracker.GetFailedRequests(providerType)
                    };

                    // Calculate success rate
                    if (metrics.TotalRequests > 0)
                    {
                        metrics.SuccessRate = (double)metrics.SuccessfulRequests / metrics.TotalRequests * 100;
                    }

                    response.Providers[providerType.ToString()] = metrics;
                }

                // Determine current provider
                if (response.Providers.Any())
                {
                    var activeProvider = response.Providers
                        .Where(p => p.Value.TotalRequests > 0)
                        .OrderByDescending(p => p.Value.SuccessfulRequests)
                        .FirstOrDefault();

                    response.CurrentProvider = activeProvider.Key ?? response.Providers.Keys.FirstOrDefault();
                }

                _logger.LogInformation(
                    "Provider API metrics retrieved: {ProviderCount} providers, Strategy: {Strategy}",
                    response.Providers.Count,
                    response.Strategy ?? "Unknown");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve provider API metrics");
                return StatusCode(500, new { message = "Failed to retrieve provider API metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get rate limit status for all data providers
        /// </summary>
        /// <returns>Rate limit status for each provider</returns>
        [HttpGet("providers/ratelimits")]
        public ActionResult<ProviderRateLimitResponse> GetProviderRateLimits()
        {
            try
            {
                _logger.LogInformation("Provider rate limit status requested at {Timestamp}", DateTime.UtcNow);

                var response = new ProviderRateLimitResponse
                {
                    Timestamp = DateTime.UtcNow
                };

                // Get all available providers
                var availableProviders = _providerFactory?.GetAvailableProviders() ?? Enumerable.Empty<DataProviderType>();

                foreach (var providerType in availableProviders)
                {
                    // Only Alpha Vantage has explicit rate limiting
                    if (providerType == DataProviderType.AlphaVantage && _alphaVantageRateLimiter != null)
                    {
                        var rateLimitStatus = _alphaVantageRateLimiter.GetStatus();
                        response.Providers[providerType.ToString()] = new RateLimitInfo
                        {
                            MinuteRequestsRemaining = rateLimitStatus.MinuteRequestsRemaining,
                            MinuteRequestsLimit = rateLimitStatus.MinuteRequestsLimit,
                            MinuteWindowResetIn = rateLimitStatus.MinuteWindowResetIn,
                            DayRequestsRemaining = rateLimitStatus.DayRequestsRemaining,
                            DayRequestsLimit = rateLimitStatus.DayRequestsLimit,
                            DayWindowResetIn = rateLimitStatus.DayWindowResetIn,
                            IsRateLimited = rateLimitStatus.IsRateLimited
                        };
                    }
                    else
                    {
                        // Other providers don't have explicit rate limits
                        response.Providers[providerType.ToString()] = new RateLimitInfo
                        {
                            MinuteRequestsRemaining = -1, // Unlimited
                            MinuteRequestsLimit = -1,
                            MinuteWindowResetIn = TimeSpan.Zero,
                            DayRequestsRemaining = -1,
                            DayRequestsLimit = -1,
                            DayWindowResetIn = TimeSpan.Zero,
                            IsRateLimited = false
                        };
                    }
                }

                _logger.LogInformation(
                    "Provider rate limit status retrieved: {ProviderCount} providers",
                    response.Providers.Count);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve provider rate limit status");
                return StatusCode(500, new { message = "Failed to retrieve provider rate limit status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get cost tracking metrics for all data providers
        /// </summary>
        /// <returns>Cost metrics for each provider</returns>
        [HttpGet("providers/costs")]
        public ActionResult<ProviderCostResponse> GetProviderCosts()
        {
            try
            {
                _logger.LogInformation("Provider cost metrics requested at {Timestamp}", DateTime.UtcNow);

                if (_providerCostTracker == null)
                {
                    return NotFound(new { message = "Provider cost tracking is not available" });
                }

                var response = new ProviderCostResponse
                {
                    Timestamp = DateTime.UtcNow,
                    CostTrackingEnabled = _costSettings?.Value?.Enabled ?? false,
                    CostLimitsEnforced = _costSettings?.Value?.EnforceLimits ?? false
                };

                // Get all available providers
                var availableProviders = _providerFactory?.GetAvailableProviders() ?? Enumerable.Empty<DataProviderType>();

                decimal totalCost = 0;
                long totalCalls = 0;

                foreach (var providerType in availableProviders)
                {
                    var metrics = _providerCostTracker.GetCostMetrics(providerType);
                    
                    response.Providers[providerType.ToString()] = new ProviderCostInfo
                    {
                        TotalApiCalls = metrics.TotalApiCalls,
                        EstimatedCost = metrics.EstimatedCost,
                        MonthlySubscriptionCost = metrics.MonthlySubscriptionCost,
                        TotalEstimatedCost = metrics.TotalEstimatedCost,
                        CostPerCall = metrics.CostPerCall,
                        CostThreshold = metrics.CostThreshold,
                        ThresholdPercentage = metrics.ThresholdPercentage,
                        IsThresholdExceeded = metrics.IsThresholdExceeded,
                        TrackingStarted = metrics.TrackingStarted,
                        LastUpdated = metrics.LastUpdated
                    };

                    totalCost += metrics.TotalEstimatedCost;
                    totalCalls += metrics.TotalApiCalls;
                }

                response.TotalEstimatedCost = totalCost;
                response.TotalApiCalls = totalCalls;

                _logger.LogInformation(
                    "Provider cost metrics retrieved: {ProviderCount} providers, Total cost: ${TotalCost}, Total calls: {TotalCalls}",
                    response.Providers.Count,
                    totalCost,
                    totalCalls);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve provider cost metrics");
                return StatusCode(500, new { message = "Failed to retrieve provider cost metrics", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Response model for health check endpoint
    /// </summary>
    public class HealthCheckResponse
    {
        /// <summary>
        /// Health status: "Healthy" or "Unhealthy"
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the health check
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Name of the service being checked
        /// </summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>
        /// Additional details about the health status
        /// </summary>
        public string Details { get; set; } = string.Empty;
    }
}
