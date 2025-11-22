using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.API.Controllers;
using StockSensePro.API.Middleware;
using StockSensePro.API.Models;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class HealthControllerTests
    {
        private readonly Mock<IStockDataProvider> _mockStockDataProvider;
        private readonly Mock<ILogger<HealthController>> _mockLogger;
        private readonly Mock<IProviderHealthMonitor> _mockHealthMonitor;
        private readonly Mock<IProviderMetricsTracker> _mockMetricsTracker;
        private readonly Mock<IAlphaVantageRateLimiter> _mockRateLimiter;
        private readonly Mock<IDataProviderStrategy> _mockStrategy;
        private readonly Mock<IStockDataProviderFactory> _mockProviderFactory;

        public HealthControllerTests()
        {
            _mockStockDataProvider = new Mock<IStockDataProvider>();
            _mockLogger = new Mock<ILogger<HealthController>>();
            _mockHealthMonitor = new Mock<IProviderHealthMonitor>();
            _mockMetricsTracker = new Mock<IProviderMetricsTracker>();
            _mockRateLimiter = new Mock<IAlphaVantageRateLimiter>();
            _mockStrategy = new Mock<IDataProviderStrategy>();
            _mockProviderFactory = new Mock<IStockDataProviderFactory>();
        }

        private HealthController CreateController()
        {
            return new HealthController(
                _mockStockDataProvider.Object,
                _mockLogger.Object,
                null,
                _mockHealthMonitor.Object,
                _mockMetricsTracker.Object,
                _mockRateLimiter.Object,
                _mockStrategy.Object,
                _mockProviderFactory.Object
            );
        }

        [Fact]
        public void GetProviderHealth_WithHealthMonitor_ReturnsHealthStatus()
        {
            // Arrange
            var controller = CreateController();
            var providers = new[] { DataProviderType.AlphaVantage, DataProviderType.YahooFinance };
            
            _mockProviderFactory.Setup(f => f.GetAvailableProviders())
                .Returns(providers);

            _mockHealthMonitor.Setup(m => m.GetHealthStatus(DataProviderType.AlphaVantage))
                .Returns(new ProviderHealth
                {
                    IsHealthy = true,
                    LastChecked = DateTime.UtcNow,
                    ConsecutiveFailures = 0,
                    AverageResponseTime = TimeSpan.FromMilliseconds(150)
                });

            _mockHealthMonitor.Setup(m => m.GetHealthStatus(DataProviderType.YahooFinance))
                .Returns(new ProviderHealth
                {
                    IsHealthy = false,
                    LastChecked = DateTime.UtcNow,
                    ConsecutiveFailures = 3,
                    AverageResponseTime = TimeSpan.FromMilliseconds(500)
                });

            // Act
            var result = controller.GetProviderHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ProviderHealthResponse>(okResult.Value);
            
            Assert.Equal(2, response.Providers.Count);
            Assert.True(response.Providers["AlphaVantage"].IsHealthy);
            Assert.False(response.Providers["YahooFinance"].IsHealthy);
            Assert.True(response.OverallHealthy); // At least one provider is healthy
            Assert.Equal(0, response.Providers["AlphaVantage"].ConsecutiveFailures);
            Assert.Equal(3, response.Providers["YahooFinance"].ConsecutiveFailures);
        }

        [Fact]
        public void GetProviderHealth_WithoutHealthMonitor_ReturnsNotFound()
        {
            // Arrange
            var controller = new HealthController(
                _mockStockDataProvider.Object,
                _mockLogger.Object,
                null,
                null, // No health monitor
                _mockMetricsTracker.Object,
                _mockRateLimiter.Object,
                _mockStrategy.Object,
                _mockProviderFactory.Object
            );

            // Act
            var result = controller.GetProviderHealth();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public void GetProviderApiMetrics_WithMetricsTracker_ReturnsMetrics()
        {
            // Arrange
            var controller = CreateController();
            var providers = new[] { DataProviderType.AlphaVantage, DataProviderType.YahooFinance };
            
            _mockProviderFactory.Setup(f => f.GetAvailableProviders())
                .Returns(providers);

            _mockStrategy.Setup(s => s.GetStrategyName())
                .Returns("PrimaryWithFallback");

            _mockMetricsTracker.Setup(m => m.GetTotalRequests(DataProviderType.AlphaVantage))
                .Returns(100);
            _mockMetricsTracker.Setup(m => m.GetSuccessfulRequests(DataProviderType.AlphaVantage))
                .Returns(95);
            _mockMetricsTracker.Setup(m => m.GetFailedRequests(DataProviderType.AlphaVantage))
                .Returns(5);

            _mockMetricsTracker.Setup(m => m.GetTotalRequests(DataProviderType.YahooFinance))
                .Returns(50);
            _mockMetricsTracker.Setup(m => m.GetSuccessfulRequests(DataProviderType.YahooFinance))
                .Returns(48);
            _mockMetricsTracker.Setup(m => m.GetFailedRequests(DataProviderType.YahooFinance))
                .Returns(2);

            // Act
            var result = controller.GetProviderApiMetrics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ProviderApiMetricsResponse>(okResult.Value);
            
            Assert.Equal(2, response.Providers.Count);
            Assert.Equal("PrimaryWithFallback", response.Strategy);
            Assert.Equal(100, response.Providers["AlphaVantage"].TotalRequests);
            Assert.Equal(95, response.Providers["AlphaVantage"].SuccessfulRequests);
            Assert.Equal(5, response.Providers["AlphaVantage"].FailedRequests);
            Assert.Equal(95.0, response.Providers["AlphaVantage"].SuccessRate);
            Assert.Equal("AlphaVantage", response.CurrentProvider); // Most successful requests
        }

        [Fact]
        public void GetProviderApiMetrics_WithoutMetricsTracker_ReturnsNotFound()
        {
            // Arrange
            var controller = new HealthController(
                _mockStockDataProvider.Object,
                _mockLogger.Object,
                null,
                _mockHealthMonitor.Object,
                null, // No metrics tracker
                _mockRateLimiter.Object,
                _mockStrategy.Object,
                _mockProviderFactory.Object
            );

            // Act
            var result = controller.GetProviderApiMetrics();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public void GetProviderRateLimits_WithAlphaVantage_ReturnsRateLimitInfo()
        {
            // Arrange
            var controller = CreateController();
            var providers = new[] { DataProviderType.AlphaVantage, DataProviderType.YahooFinance };
            
            _mockProviderFactory.Setup(f => f.GetAvailableProviders())
                .Returns(providers);

            _mockRateLimiter.Setup(r => r.GetStatus())
                .Returns(new RateLimitStatus
                {
                    MinuteRequestsRemaining = 3,
                    MinuteRequestsLimit = 5,
                    MinuteWindowResetIn = TimeSpan.FromSeconds(30),
                    DayRequestsRemaining = 450,
                    DayRequestsLimit = 500,
                    DayWindowResetIn = TimeSpan.FromHours(12)
                });

            // Act
            var result = controller.GetProviderRateLimits();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ProviderRateLimitResponse>(okResult.Value);
            
            Assert.Equal(2, response.Providers.Count);
            
            // Alpha Vantage should have explicit rate limits
            Assert.Equal(3, response.Providers["AlphaVantage"].MinuteRequestsRemaining);
            Assert.Equal(5, response.Providers["AlphaVantage"].MinuteRequestsLimit);
            Assert.Equal(450, response.Providers["AlphaVantage"].DayRequestsRemaining);
            Assert.False(response.Providers["AlphaVantage"].IsRateLimited);
            
            // Yahoo Finance should have unlimited (-1)
            Assert.Equal(-1, response.Providers["YahooFinance"].MinuteRequestsRemaining);
            Assert.Equal(-1, response.Providers["YahooFinance"].MinuteRequestsLimit);
            Assert.False(response.Providers["YahooFinance"].IsRateLimited);
        }

        [Fact]
        public void GetProviderRateLimits_WhenRateLimited_ReturnsCorrectStatus()
        {
            // Arrange
            var controller = CreateController();
            var providers = new[] { DataProviderType.AlphaVantage };
            
            _mockProviderFactory.Setup(f => f.GetAvailableProviders())
                .Returns(providers);

            _mockRateLimiter.Setup(r => r.GetStatus())
                .Returns(new RateLimitStatus
                {
                    MinuteRequestsRemaining = 0,
                    MinuteRequestsLimit = 5,
                    MinuteWindowResetIn = TimeSpan.FromSeconds(45),
                    DayRequestsRemaining = 0,
                    DayRequestsLimit = 500,
                    DayWindowResetIn = TimeSpan.FromHours(8)
                });

            // Act
            var result = controller.GetProviderRateLimits();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ProviderRateLimitResponse>(okResult.Value);
            
            Assert.Single(response.Providers);
            Assert.Equal(0, response.Providers["AlphaVantage"].MinuteRequestsRemaining);
            Assert.Equal(0, response.Providers["AlphaVantage"].DayRequestsRemaining);
            Assert.True(response.Providers["AlphaVantage"].IsRateLimited);
        }

        [Fact]
        public void GetProviderApiMetrics_CalculatesSuccessRateCorrectly()
        {
            // Arrange
            var controller = CreateController();
            var providers = new[] { DataProviderType.AlphaVantage };
            
            _mockProviderFactory.Setup(f => f.GetAvailableProviders())
                .Returns(providers);

            _mockMetricsTracker.Setup(m => m.GetTotalRequests(DataProviderType.AlphaVantage))
                .Returns(200);
            _mockMetricsTracker.Setup(m => m.GetSuccessfulRequests(DataProviderType.AlphaVantage))
                .Returns(180);
            _mockMetricsTracker.Setup(m => m.GetFailedRequests(DataProviderType.AlphaVantage))
                .Returns(20);

            // Act
            var result = controller.GetProviderApiMetrics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ProviderApiMetricsResponse>(okResult.Value);
            
            Assert.Equal(90.0, response.Providers["AlphaVantage"].SuccessRate);
        }

        [Fact]
        public void GetProviderHealth_AllProvidersUnhealthy_ReturnsOverallUnhealthy()
        {
            // Arrange
            var controller = CreateController();
            var providers = new[] { DataProviderType.AlphaVantage, DataProviderType.YahooFinance };
            
            _mockProviderFactory.Setup(f => f.GetAvailableProviders())
                .Returns(providers);

            _mockHealthMonitor.Setup(m => m.GetHealthStatus(It.IsAny<DataProviderType>()))
                .Returns(new ProviderHealth
                {
                    IsHealthy = false,
                    LastChecked = DateTime.UtcNow,
                    ConsecutiveFailures = 5,
                    AverageResponseTime = TimeSpan.FromMilliseconds(1000)
                });

            // Act
            var result = controller.GetProviderHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ProviderHealthResponse>(okResult.Value);
            
            Assert.False(response.OverallHealthy);
        }
    }
}
