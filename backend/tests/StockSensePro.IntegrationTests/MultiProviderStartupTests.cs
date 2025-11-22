using Microsoft.Extensions.DependencyInjection;
using StockSensePro.Application.Strategies;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.IntegrationTests
{
    /// <summary>
    /// Integration tests for multi-provider startup configuration in Program.cs
    /// </summary>
    public class MultiProviderStartupTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public MultiProviderStartupTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Startup_RegistersProviderFactory()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var factory = scope.ServiceProvider.GetService<IStockDataProviderFactory>();

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void Startup_RegistersHealthMonitor()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var healthMonitor = scope.ServiceProvider.GetService<IProviderHealthMonitor>();

            // Assert
            Assert.NotNull(healthMonitor);
        }

        [Fact]
        public void Startup_RegistersMetricsTracker()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var metricsTracker = scope.ServiceProvider.GetService<IProviderMetricsTracker>();

            // Assert
            Assert.NotNull(metricsTracker);
        }

        [Fact]
        public void Startup_RegistersRateLimiter()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var rateLimiter = scope.ServiceProvider.GetService<IAlphaVantageRateLimiter>();

            // Assert
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public void Startup_RegistersAlphaVantageService()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var alphaVantageService = scope.ServiceProvider.GetService<AlphaVantageService>();

            // Assert
            Assert.NotNull(alphaVantageService);
        }

        [Fact]
        public void Startup_RegistersAllProviderStrategies()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            
            var primaryStrategy = scope.ServiceProvider.GetService<PrimaryProviderStrategy>();
            var fallbackStrategy = scope.ServiceProvider.GetService<FallbackProviderStrategy>();
            var roundRobinStrategy = scope.ServiceProvider.GetService<RoundRobinProviderStrategy>();
            var costOptimizedStrategy = scope.ServiceProvider.GetService<CostOptimizedProviderStrategy>();

            // Assert
            Assert.NotNull(primaryStrategy);
            Assert.NotNull(fallbackStrategy);
            Assert.NotNull(roundRobinStrategy);
            Assert.NotNull(costOptimizedStrategy);
        }

        [Fact]
        public void Startup_RegistersDataProviderStrategy()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var strategy = scope.ServiceProvider.GetService<IDataProviderStrategy>();

            // Assert
            Assert.NotNull(strategy);
        }

        [Fact]
        public void Startup_ConfiguresDataProviderSettings()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var settings = scope.ServiceProvider.GetService<DataProviderSettings>();

            // Assert
            Assert.NotNull(settings);
            // Verify that a valid provider is configured (not the default enum value of 0)
            Assert.True(Enum.IsDefined(typeof(DataProviderType), settings.PrimaryProvider));
        }

        [Fact]
        public void Startup_HealthMonitorIsSingleton()
        {
            // Arrange & Act
            using var scope1 = _factory.Services.CreateScope();
            using var scope2 = _factory.Services.CreateScope();
            
            var healthMonitor1 = scope1.ServiceProvider.GetService<IProviderHealthMonitor>();
            var healthMonitor2 = scope2.ServiceProvider.GetService<IProviderHealthMonitor>();

            // Assert
            Assert.Same(healthMonitor1, healthMonitor2);
        }

        [Fact]
        public void Startup_MetricsTrackerIsSingleton()
        {
            // Arrange & Act
            using var scope1 = _factory.Services.CreateScope();
            using var scope2 = _factory.Services.CreateScope();
            
            var metricsTracker1 = scope1.ServiceProvider.GetService<IProviderMetricsTracker>();
            var metricsTracker2 = scope2.ServiceProvider.GetService<IProviderMetricsTracker>();

            // Assert
            Assert.Same(metricsTracker1, metricsTracker2);
        }

        [Fact]
        public void Startup_RateLimiterIsSingleton()
        {
            // Arrange & Act
            using var scope1 = _factory.Services.CreateScope();
            using var scope2 = _factory.Services.CreateScope();
            
            var rateLimiter1 = scope1.ServiceProvider.GetService<IAlphaVantageRateLimiter>();
            var rateLimiter2 = scope2.ServiceProvider.GetService<IAlphaVantageRateLimiter>();

            // Assert
            Assert.Same(rateLimiter1, rateLimiter2);
        }

        [Fact]
        public void Startup_ProviderFactoryIsScoped()
        {
            // Arrange & Act
            using var scope1 = _factory.Services.CreateScope();
            using var scope2 = _factory.Services.CreateScope();
            
            var factory1 = scope1.ServiceProvider.GetService<IStockDataProviderFactory>();
            var factory2 = scope2.ServiceProvider.GetService<IStockDataProviderFactory>();

            // Assert - Different instances for different scopes
            Assert.NotSame(factory1, factory2);
        }
    }
}
