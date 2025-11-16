using Microsoft.AspNetCore.Mvc;
using StockSensePro.Core.Interfaces;
using StockSensePro.API.Middleware;

namespace StockSensePro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IStockDataProvider _stockDataProvider;
        private readonly ILogger<HealthController> _logger;
        private readonly RateLimitMetrics? _rateLimitMetrics;

        public HealthController(
            IStockDataProvider stockDataProvider, 
            ILogger<HealthController> logger,
            RateLimitMetrics? rateLimitMetrics = null)
        {
            _stockDataProvider = stockDataProvider;
            _logger = logger;
            _rateLimitMetrics = rateLimitMetrics;
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
