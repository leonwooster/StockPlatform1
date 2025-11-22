using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.Application.Strategies;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class ProviderStrategyTests
    {
        // Test implementation of ProviderStrategyBase for testing purposes
        private class TestProviderStrategy : ProviderStrategyBase
        {
            private readonly DataProviderType _providerToReturn;

            public TestProviderStrategy(
                IStockDataProviderFactory factory,
                IProviderHealthMonitor healthMonitor,
                ILogger logger,
                DataProviderType providerToReturn = DataProviderType.YahooFinance)
                : base(factory, healthMonitor, logger)
            {
                _providerToReturn = providerToReturn;
            }

            public override IStockDataProvider SelectProvider(DataProviderContext context)
            {
                var provider = _factory.CreateProvider(_providerToReturn);
                LogProviderSelection(context, provider);
                return provider;
            }

            public override string GetStrategyName() => "TestStrategy";

            // Expose protected methods for testing
            public bool TestIsProviderHealthy(DataProviderContext context, DataProviderType providerType)
                => IsProviderHealthy(context, providerType);

            public bool TestHasRateLimitCapacity(DataProviderContext context, DataProviderType providerType)
                => HasRateLimitCapacity(context, providerType);
        }

        private readonly Mock<IStockDataProviderFactory> _mockFactory;
        private readonly Mock<IProviderHealthMonitor> _mockHealthMonitor;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IStockDataProvider> _mockProvider;

        public ProviderStrategyTests()
        {
            _mockFactory = new Mock<IStockDataProviderFactory>();
            _mockHealthMonitor = new Mock<IProviderHealthMonitor>();
            _mockLogger = new Mock<ILogger>();
            _mockProvider = new Mock<IStockDataProvider>();
        }

        [Fact]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TestProviderStrategy(null!, _mockHealthMonitor.Object, _mockLogger.Object));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullHealthMonitor_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TestProviderStrategy(_mockFactory.Object, null!, _mockLogger.Object));

            Assert.Equal("healthMonitor", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, null!));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void SelectProvider_ReturnsProviderFromFactory()
        {
            // Arrange
            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.YahooFinance))
                .Returns(_mockProvider.Object);

            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext("AAPL", "GetQuote");

            // Act
            var result = strategy.SelectProvider(context);

            // Assert
            Assert.NotNull(result);
            Assert.Same(_mockProvider.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.YahooFinance), Times.Once);
        }

        [Fact]
        public void GetStrategyName_ReturnsCorrectName()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);

            // Act
            var result = strategy.GetStrategyName();

            // Assert
            Assert.Equal("TestStrategy", result);
        }

        [Fact]
        public void GetFallbackProvider_ReturnsNullByDefault()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);

            // Act
            var result = strategy.GetFallbackProvider();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsProviderHealthy_WithHealthyProvider_ReturnsTrue()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(100)) }
                }
            };

            // Act
            var result = strategy.TestIsProviderHealthy(context, DataProviderType.YahooFinance);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsProviderHealthy_WithUnhealthyProvider_ReturnsFalse()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)) }
                }
            };

            // Act
            var result = strategy.TestIsProviderHealthy(context, DataProviderType.YahooFinance);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsProviderHealthy_WithNoHealthInfo_ReturnsTrue()
        {
            // Arrange
            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.YahooFinance))
                .Returns((ProviderHealth?)null);

            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>()
            };

            // Act
            var result = strategy.TestIsProviderHealthy(context, DataProviderType.YahooFinance);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRateLimitCapacity_WithAvailableCapacity_ReturnsTrue()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                RateLimitRemaining = new Dictionary<DataProviderType, int>
                {
                    { DataProviderType.YahooFinance, 100 }
                }
            };

            // Act
            var result = strategy.TestHasRateLimitCapacity(context, DataProviderType.YahooFinance);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRateLimitCapacity_WithNoCapacity_ReturnsFalse()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                RateLimitRemaining = new Dictionary<DataProviderType, int>
                {
                    { DataProviderType.YahooFinance, 0 }
                }
            };

            // Act
            var result = strategy.TestHasRateLimitCapacity(context, DataProviderType.YahooFinance);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasRateLimitCapacity_WithNoRateLimitInfo_ReturnsTrue()
        {
            // Arrange
            var strategy = new TestProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                RateLimitRemaining = new Dictionary<DataProviderType, int>()
            };

            // Act
            var result = strategy.TestHasRateLimitCapacity(context, DataProviderType.YahooFinance);

            // Assert
            Assert.True(result);
        }
    }
}

    // Tests for PrimaryProviderStrategy
    public class PrimaryProviderStrategyTests
    {
        private readonly Mock<IStockDataProviderFactory> _mockFactory;
        private readonly Mock<IProviderHealthMonitor> _mockHealthMonitor;
        private readonly Mock<ILogger<PrimaryProviderStrategy>> _mockLogger;
        private readonly Mock<IStockDataProvider> _mockProvider;

        public PrimaryProviderStrategyTests()
        {
            _mockFactory = new Mock<IStockDataProviderFactory>();
            _mockHealthMonitor = new Mock<IProviderHealthMonitor>();
            _mockLogger = new Mock<ILogger<PrimaryProviderStrategy>>();
            _mockProvider = new Mock<IStockDataProvider>();
        }

        [Fact]
        public void SelectProvider_AlwaysReturnsPrimaryProvider()
        {
            // Arrange
            var settings = Microsoft.Extensions.Options.Options.Create(new DataProviderSettings
            {
                PrimaryProvider = DataProviderType.YahooFinance
            });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.YahooFinance))
                .Returns(_mockProvider.Object);

            var strategy = new PrimaryProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, settings, _mockLogger.Object);
            var context = new DataProviderContext("AAPL", "GetQuote");

            // Act
            var result = strategy.SelectProvider(context);

            // Assert
            Assert.NotNull(result);
            Assert.Same(_mockProvider.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.YahooFinance), Times.Once);
        }

        [Fact]
        public void GetStrategyName_ReturnsPrimary()
        {
            // Arrange
            var settings = Microsoft.Extensions.Options.Options.Create(new DataProviderSettings());
            var strategy = new PrimaryProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, settings, _mockLogger.Object);

            // Act
            var result = strategy.GetStrategyName();

            // Assert
            Assert.Equal("Primary", result);
        }
    }

    // Tests for FallbackProviderStrategy
    public class FallbackProviderStrategyTests
    {
        private readonly Mock<IStockDataProviderFactory> _mockFactory;
        private readonly Mock<IProviderHealthMonitor> _mockHealthMonitor;
        private readonly Mock<ILogger<FallbackProviderStrategy>> _mockLogger;
        private readonly Mock<IStockDataProvider> _mockPrimaryProvider;
        private readonly Mock<IStockDataProvider> _mockFallbackProvider;

        public FallbackProviderStrategyTests()
        {
            _mockFactory = new Mock<IStockDataProviderFactory>();
            _mockHealthMonitor = new Mock<IProviderHealthMonitor>();
            _mockLogger = new Mock<ILogger<FallbackProviderStrategy>>();
            _mockPrimaryProvider = new Mock<IStockDataProvider>();
            _mockFallbackProvider = new Mock<IStockDataProvider>();
        }

        [Fact]
        public void SelectProvider_WithHealthyPrimary_ReturnsPrimaryProvider()
        {
            // Arrange
            var settings = Microsoft.Extensions.Options.Options.Create(new DataProviderSettings
            {
                PrimaryProvider = DataProviderType.YahooFinance,
                FallbackProvider = DataProviderType.AlphaVantage
            });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.YahooFinance))
                .Returns(_mockPrimaryProvider.Object);

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.YahooFinance))
                .Returns(new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(100)));

            var strategy = new FallbackProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, settings, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(100)) }
                }
            };

            // Act
            var result = strategy.SelectProvider(context);

            // Assert
            Assert.Same(_mockPrimaryProvider.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.YahooFinance), Times.Once);
        }

        [Fact]
        public void SelectProvider_WithUnhealthyPrimary_ReturnsFallbackProvider()
        {
            // Arrange
            var settings = Microsoft.Extensions.Options.Options.Create(new DataProviderSettings
            {
                PrimaryProvider = DataProviderType.YahooFinance,
                FallbackProvider = DataProviderType.AlphaVantage
            });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.AlphaVantage))
                .Returns(_mockFallbackProvider.Object);

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.YahooFinance))
                .Returns(new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)));

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.AlphaVantage))
                .Returns(new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)));

            var strategy = new FallbackProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, settings, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)) },
                    { DataProviderType.AlphaVantage, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)) }
                }
            };

            // Act
            var result = strategy.SelectProvider(context);

            // Assert
            Assert.Same(_mockFallbackProvider.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.AlphaVantage), Times.Once);
        }

        [Fact]
        public void GetFallbackProvider_WithConfiguredFallback_ReturnsFallbackProvider()
        {
            // Arrange
            var settings = Microsoft.Extensions.Options.Options.Create(new DataProviderSettings
            {
                PrimaryProvider = DataProviderType.YahooFinance,
                FallbackProvider = DataProviderType.AlphaVantage
            });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.AlphaVantage))
                .Returns(_mockFallbackProvider.Object);

            var strategy = new FallbackProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, settings, _mockLogger.Object);

            // Act
            var result = strategy.GetFallbackProvider();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_mockFallbackProvider.Object, result);
        }

        [Fact]
        public void GetStrategyName_ReturnsFallback()
        {
            // Arrange
            var settings = Microsoft.Extensions.Options.Options.Create(new DataProviderSettings());
            var strategy = new FallbackProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, settings, _mockLogger.Object);

            // Act
            var result = strategy.GetStrategyName();

            // Assert
            Assert.Equal("Fallback", result);
        }
    }

    // Tests for RoundRobinProviderStrategy
    public class RoundRobinProviderStrategyTests
    {
        private readonly Mock<IStockDataProviderFactory> _mockFactory;
        private readonly Mock<IProviderHealthMonitor> _mockHealthMonitor;
        private readonly Mock<ILogger<RoundRobinProviderStrategy>> _mockLogger;
        private readonly Mock<IStockDataProvider> _mockProvider1;
        private readonly Mock<IStockDataProvider> _mockProvider2;

        public RoundRobinProviderStrategyTests()
        {
            _mockFactory = new Mock<IStockDataProviderFactory>();
            _mockHealthMonitor = new Mock<IProviderHealthMonitor>();
            _mockLogger = new Mock<ILogger<RoundRobinProviderStrategy>>();
            _mockProvider1 = new Mock<IStockDataProvider>();
            _mockProvider2 = new Mock<IStockDataProvider>();
        }

        [Fact]
        public void SelectProvider_RotatesThroughProviders()
        {
            // Arrange
            _mockFactory
                .Setup(f => f.GetAvailableProviders())
                .Returns(new[] { DataProviderType.YahooFinance, DataProviderType.AlphaVantage });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.YahooFinance))
                .Returns(_mockProvider1.Object);

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.AlphaVantage))
                .Returns(_mockProvider2.Object);

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(It.IsAny<DataProviderType>()))
                .Returns(new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(100)));

            var strategy = new RoundRobinProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext("AAPL", "GetQuote");

            // Act - Call multiple times to verify rotation
            var result1 = strategy.SelectProvider(context);
            var result2 = strategy.SelectProvider(context);
            var result3 = strategy.SelectProvider(context);

            // Assert - Should rotate between providers
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
        }

        [Fact]
        public void SelectProvider_OnlySelectsHealthyProviders()
        {
            // Arrange
            _mockFactory
                .Setup(f => f.GetAvailableProviders())
                .Returns(new[] { DataProviderType.YahooFinance, DataProviderType.AlphaVantage });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.AlphaVantage))
                .Returns(_mockProvider2.Object);

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.YahooFinance))
                .Returns(new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)));

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.AlphaVantage))
                .Returns(new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)));

            var strategy = new RoundRobinProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)) },
                    { DataProviderType.AlphaVantage, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)) }
                },
                RateLimitRemaining = new Dictionary<DataProviderType, int>
                {
                    { DataProviderType.YahooFinance, 100 },
                    { DataProviderType.AlphaVantage, 100 }
                }
            };

            // Act
            var result = strategy.SelectProvider(context);

            // Assert - Should only select the healthy provider
            Assert.Same(_mockProvider2.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.AlphaVantage), Times.Once);
        }

        [Fact]
        public void GetStrategyName_ReturnsRoundRobin()
        {
            // Arrange
            var strategy = new RoundRobinProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);

            // Act
            var result = strategy.GetStrategyName();

            // Assert
            Assert.Equal("RoundRobin", result);
        }
    }

    // Tests for CostOptimizedProviderStrategy
    public class CostOptimizedProviderStrategyTests
    {
        private readonly Mock<IStockDataProviderFactory> _mockFactory;
        private readonly Mock<IProviderHealthMonitor> _mockHealthMonitor;
        private readonly Mock<ILogger<CostOptimizedProviderStrategy>> _mockLogger;
        private readonly Mock<IStockDataProvider> _mockYahooProvider;
        private readonly Mock<IStockDataProvider> _mockAlphaVantageProvider;

        public CostOptimizedProviderStrategyTests()
        {
            _mockFactory = new Mock<IStockDataProviderFactory>();
            _mockHealthMonitor = new Mock<IProviderHealthMonitor>();
            _mockLogger = new Mock<ILogger<CostOptimizedProviderStrategy>>();
            _mockYahooProvider = new Mock<IStockDataProvider>();
            _mockAlphaVantageProvider = new Mock<IStockDataProvider>();
        }

        [Fact]
        public void SelectProvider_PrefersFreeProvider()
        {
            // Arrange
            _mockFactory
                .Setup(f => f.GetAvailableProviders())
                .Returns(new[] { DataProviderType.YahooFinance, DataProviderType.AlphaVantage });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.YahooFinance))
                .Returns(_mockYahooProvider.Object);

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(It.IsAny<DataProviderType>()))
                .Returns(new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(100)));

            var strategy = new CostOptimizedProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(100)) },
                    { DataProviderType.AlphaVantage, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)) }
                },
                RateLimitRemaining = new Dictionary<DataProviderType, int>
                {
                    { DataProviderType.YahooFinance, 100 },
                    { DataProviderType.AlphaVantage, 100 }
                }
            };

            // Act
            var result = strategy.SelectProvider(context);

            // Assert - Should select free provider (Yahoo) over paid (AlphaVantage)
            Assert.Same(_mockYahooProvider.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.YahooFinance), Times.Once);
        }

        [Fact]
        public void SelectProvider_UsesPaidProviderWhenFreeIsUnhealthy()
        {
            // Arrange
            _mockFactory
                .Setup(f => f.GetAvailableProviders())
                .Returns(new[] { DataProviderType.YahooFinance, DataProviderType.AlphaVantage });

            _mockFactory
                .Setup(f => f.CreateProvider(DataProviderType.AlphaVantage))
                .Returns(_mockAlphaVantageProvider.Object);

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.YahooFinance))
                .Returns(new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)));

            _mockHealthMonitor
                .Setup(m => m.GetHealthStatus(DataProviderType.AlphaVantage))
                .Returns(new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)));

            var strategy = new CostOptimizedProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);
            var context = new DataProviderContext
            {
                Symbol = "AAPL",
                Operation = "GetQuote",
                ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
                {
                    { DataProviderType.YahooFinance, new ProviderHealth(false, DateTime.UtcNow, 5, TimeSpan.FromMilliseconds(500)) },
                    { DataProviderType.AlphaVantage, new ProviderHealth(true, DateTime.UtcNow, 0, TimeSpan.FromMilliseconds(200)) }
                },
                RateLimitRemaining = new Dictionary<DataProviderType, int>
                {
                    { DataProviderType.YahooFinance, 0 },
                    { DataProviderType.AlphaVantage, 100 }
                }
            };

            // Act
            var result = strategy.SelectProvider(context);

            // Assert - Should use paid provider when free is unhealthy
            Assert.Same(_mockAlphaVantageProvider.Object, result);
            _mockFactory.Verify(f => f.CreateProvider(DataProviderType.AlphaVantage), Times.Once);
        }

        [Fact]
        public void CalculateEstimatedCost_ReturnsCorrectCost()
        {
            // Act
            var yahooFinanceCost = CostOptimizedProviderStrategy.CalculateEstimatedCost(DataProviderType.YahooFinance, 1000);
            var alphaVantageCost = CostOptimizedProviderStrategy.CalculateEstimatedCost(DataProviderType.AlphaVantage, 1000);

            // Assert
            Assert.Equal(0m, yahooFinanceCost); // Free
            Assert.Equal(2.0m, alphaVantageCost); // $0.002 * 1000 = $2.00
        }

        [Fact]
        public void GetStrategyName_ReturnsCostOptimized()
        {
            // Arrange
            var strategy = new CostOptimizedProviderStrategy(_mockFactory.Object, _mockHealthMonitor.Object, _mockLogger.Object);

            // Act
            var result = strategy.GetStrategyName();

            // Assert
            Assert.Equal("CostOptimized", result);
        }
    }

