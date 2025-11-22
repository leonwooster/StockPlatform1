using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class StockDataProviderFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<StockDataProviderFactory>> _mockLogger;
        private readonly Mock<IYahooFinanceService> _mockYahooFinanceService;
        private readonly Mock<MockYahooFinanceService> _mockMockYahooFinanceService;
        private readonly StockDataProviderFactory _factory;

        public StockDataProviderFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<StockDataProviderFactory>>();
            _mockYahooFinanceService = new Mock<IYahooFinanceService>();
            
            // Create a mock logger for MockYahooFinanceService
            var mockYahooLogger = new Mock<ILogger<MockYahooFinanceService>>();
            _mockMockYahooFinanceService = new Mock<MockYahooFinanceService>(mockYahooLogger.Object);

            _factory = new StockDataProviderFactory(
                _mockServiceProvider.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void CreateProvider_WithYahooFinanceType_ReturnsYahooFinanceService()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IYahooFinanceService)))
                .Returns(_mockYahooFinanceService.Object);

            // Act
            var result = _factory.CreateProvider(DataProviderType.YahooFinance);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IStockDataProvider>(result);
            _mockServiceProvider.Verify(sp => sp.GetService(typeof(IYahooFinanceService)), Times.Once);
        }

        [Fact]
        public void CreateProvider_WithMockType_ReturnsMockYahooFinanceService()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(MockYahooFinanceService)))
                .Returns(_mockMockYahooFinanceService.Object);

            // Act
            var result = _factory.CreateProvider(DataProviderType.Mock);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IStockDataProvider>(result);
            _mockServiceProvider.Verify(sp => sp.GetService(typeof(MockYahooFinanceService)), Times.Once);
        }

        [Fact]
        public void CreateProvider_WithAlphaVantageType_ThrowsNotImplementedException()
        {
            // Act & Assert
            var exception = Assert.Throws<NotImplementedException>(() =>
                _factory.CreateProvider(DataProviderType.AlphaVantage));

            Assert.Contains("Alpha Vantage provider is not yet implemented", exception.Message);
        }

        [Fact]
        public void CreateProvider_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var invalidType = (DataProviderType)999;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.CreateProvider(invalidType));

            Assert.Contains("Unsupported provider type", exception.Message);
        }

        [Fact]
        public void CreateProvider_WithValidStringName_ReturnsCorrectProvider()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IYahooFinanceService)))
                .Returns(_mockYahooFinanceService.Object);

            // Act
            var result = _factory.CreateProvider("YahooFinance");

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IStockDataProvider>(result);
        }

        [Fact]
        public void CreateProvider_WithCaseInsensitiveStringName_ReturnsCorrectProvider()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IYahooFinanceService)))
                .Returns(_mockYahooFinanceService.Object);

            // Act
            var result = _factory.CreateProvider("yahoofinance");

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IStockDataProvider>(result);
        }

        [Fact]
        public void CreateProvider_WithNullStringName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.CreateProvider((string)null!));

            Assert.Contains("Provider name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void CreateProvider_WithEmptyStringName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.CreateProvider(string.Empty));

            Assert.Contains("Provider name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void CreateProvider_WithWhitespaceStringName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.CreateProvider("   "));

            Assert.Contains("Provider name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void CreateProvider_WithInvalidStringName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.CreateProvider("InvalidProvider"));

            Assert.Contains("Unknown provider name", exception.Message);
            Assert.Contains("InvalidProvider", exception.Message);
        }

        [Fact]
        public void GetAvailableProviders_ReturnsYahooFinanceAndMock()
        {
            // Act
            var result = _factory.GetAvailableProviders().ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(DataProviderType.YahooFinance, result);
            Assert.Contains(DataProviderType.Mock, result);
            Assert.DoesNotContain(DataProviderType.AlphaVantage, result);
        }

        [Fact]
        public void GetAvailableProviders_ReturnsEnumerableOfDataProviderType()
        {
            // Act
            var result = _factory.GetAvailableProviders();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<DataProviderType>>(result);
        }
    }
}
