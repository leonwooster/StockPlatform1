using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using StockSensePro.Application.Services;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class StockServiceTests
    {
        private readonly Mock<IStockRepository> _mockRepository;
        private readonly Mock<IStockDataProvider> _mockDataProvider;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<StockService>> _mockLogger;
        private readonly StockService _stockService;

        public StockServiceTests()
        {
            _mockRepository = new Mock<IStockRepository>();
            _mockDataProvider = new Mock<IStockDataProvider>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<StockService>>();
            _stockService = new StockService(
                _mockRepository.Object,
                _mockDataProvider.Object,
                _mockCacheService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetStockBySymbolAsync_WhenStockExists_ReturnsStock()
        {
            // Arrange
            var symbol = "AAPL";
            var expectedStock = new Stock 
            { 
                Symbol = symbol, 
                Name = "Apple Inc.",
                CurrentPrice = 175.00m
            };
            
            _mockRepository.Setup(repo => repo.GetBySymbolAsync(symbol))
                .ReturnsAsync(expectedStock);

            // Act
            var result = await _stockService.GetStockBySymbolAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(expectedStock.Name, result.Name);
            Assert.Equal(expectedStock.CurrentPrice, result.CurrentPrice);
        }

        [Fact]
        public async Task GetStockBySymbolAsync_WhenStockDoesNotExist_ReturnsNull()
        {
            // Arrange
            var symbol = "NONEXISTENT";
            _mockRepository.Setup(repo => repo.GetBySymbolAsync(symbol))
                .ReturnsAsync((Stock?)null);

            // Act
            var result = await _stockService.GetStockBySymbolAsync(symbol);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateStockAsync_CallsRepositoryAdd()
        {
            // Arrange
            var stock = new Stock 
            { 
                Symbol = "NEW", 
                Name = "New Stock" 
            };

            // Act
            var result = await _stockService.CreateStockAsync(stock);

            // Assert
            Assert.Equal(stock, result);
            _mockRepository.Verify(repo => repo.AddAsync(stock), Times.Once);
        }

        // ===== Caching Tests =====

        [Fact]
        public async Task GetQuoteAsync_WhenCacheHit_ReturnsCachedData()
        {
            // Arrange
            var symbol = "AAPL";
            var cachedData = new MarketData
            {
                Symbol = symbol,
                CurrentPrice = 175.50m,
                Change = 2.50m,
                ChangePercent = 1.45m,
                Volume = 50000000
            };

            _mockCacheService.Setup(cache => cache.GetAsync<MarketData>($"quote:{symbol}"))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _stockService.GetQuoteAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(cachedData.CurrentPrice, result.CurrentPrice);
            _mockCacheService.Verify(cache => cache.GetAsync<MarketData>($"quote:{symbol}"), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetQuoteAsync_WhenCacheMiss_FetchesFromApiAndCaches()
        {
            // Arrange
            var symbol = "AAPL";
            var apiData = new MarketData
            {
                Symbol = symbol,
                CurrentPrice = 175.50m,
                Change = 2.50m,
                ChangePercent = 1.45m,
                Volume = 50000000
            };

            _mockCacheService.Setup(cache => cache.GetAsync<MarketData>($"quote:{symbol}"))
                .ReturnsAsync((MarketData?)null);
            _mockDataProvider.Setup(provider => provider.GetQuoteAsync(symbol, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiData);

            // Act
            var result = await _stockService.GetQuoteAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(apiData.CurrentPrice, result.CurrentPrice);
            _mockCacheService.Verify(cache => cache.GetAsync<MarketData>($"quote:{symbol}"), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetQuoteAsync(symbol, It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(cache => cache.SetAsync($"quote:{symbol}", apiData, TimeSpan.FromSeconds(900)), Times.Once);
        }

        [Fact]
        public async Task GetHistoricalPricesAsync_WhenCacheHit_ReturnsCachedData()
        {
            // Arrange
            var symbol = "AAPL";
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var cacheKey = $"historical:{symbol}:2024-01-01:2024-01-31:Daily";
            var cachedData = new List<StockPrice>
            {
                new StockPrice { Symbol = symbol, Date = startDate, Close = 175.00m },
                new StockPrice { Symbol = symbol, Date = startDate.AddDays(1), Close = 176.00m }
            };

            _mockCacheService.Setup(cache => cache.GetAsync<List<StockPrice>>(cacheKey))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _stockService.GetHistoricalPricesAsync(symbol, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(symbol, result[0].Symbol);
            _mockCacheService.Verify(cache => cache.GetAsync<List<StockPrice>>(cacheKey), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetHistoricalPricesAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), 
                It.IsAny<Core.Enums.TimeInterval>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetHistoricalPricesAsync_WhenCacheMiss_FetchesFromApiAndCaches()
        {
            // Arrange
            var symbol = "AAPL";
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var cacheKey = $"historical:{symbol}:2024-01-01:2024-01-31:Daily";
            var apiData = new List<StockPrice>
            {
                new StockPrice { Symbol = symbol, Date = startDate, Close = 175.00m },
                new StockPrice { Symbol = symbol, Date = startDate.AddDays(1), Close = 176.00m }
            };

            _mockCacheService.Setup(cache => cache.GetAsync<List<StockPrice>>(cacheKey))
                .ReturnsAsync((List<StockPrice>?)null);
            _mockDataProvider.Setup(provider => provider.GetHistoricalPricesAsync(
                symbol, startDate, endDate, Core.Enums.TimeInterval.Daily, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiData);

            // Act
            var result = await _stockService.GetHistoricalPricesAsync(symbol, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockCacheService.Verify(cache => cache.GetAsync<List<StockPrice>>(cacheKey), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetHistoricalPricesAsync(
                symbol, startDate, endDate, Core.Enums.TimeInterval.Daily, It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(cache => cache.SetAsync(cacheKey, apiData, TimeSpan.FromSeconds(86400)), Times.Once);
        }

        [Fact]
        public async Task GetFundamentalsAsync_WhenCacheHit_ReturnsCachedData()
        {
            // Arrange
            var symbol = "AAPL";
            var cachedData = new FundamentalData
            {
                Symbol = symbol,
                PERatio = 28.5m,
                EPS = 6.15m,
                ProfitMargin = 0.25m
            };

            _mockCacheService.Setup(cache => cache.GetAsync<FundamentalData>($"fundamentals:{symbol}"))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _stockService.GetFundamentalsAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(cachedData.PERatio, result.PERatio);
            _mockCacheService.Verify(cache => cache.GetAsync<FundamentalData>($"fundamentals:{symbol}"), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetFundamentalsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetFundamentalsAsync_WhenCacheMiss_FetchesFromApiAndCaches()
        {
            // Arrange
            var symbol = "AAPL";
            var apiData = new FundamentalData
            {
                Symbol = symbol,
                PERatio = 28.5m,
                EPS = 6.15m,
                ProfitMargin = 0.25m
            };

            _mockCacheService.Setup(cache => cache.GetAsync<FundamentalData>($"fundamentals:{symbol}"))
                .ReturnsAsync((FundamentalData?)null);
            _mockDataProvider.Setup(provider => provider.GetFundamentalsAsync(symbol, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiData);

            // Act
            var result = await _stockService.GetFundamentalsAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(apiData.PERatio, result.PERatio);
            _mockCacheService.Verify(cache => cache.GetAsync<FundamentalData>($"fundamentals:{symbol}"), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetFundamentalsAsync(symbol, It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(cache => cache.SetAsync($"fundamentals:{symbol}", apiData, TimeSpan.FromSeconds(21600)), Times.Once);
        }

        [Fact]
        public async Task GetCompanyProfileAsync_WhenCacheHit_ReturnsCachedData()
        {
            // Arrange
            var symbol = "AAPL";
            var cachedData = new CompanyProfile
            {
                Symbol = symbol,
                CompanyName = "Apple Inc.",
                Sector = "Technology",
                Industry = "Consumer Electronics"
            };

            _mockCacheService.Setup(cache => cache.GetAsync<CompanyProfile>($"profile:{symbol}"))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _stockService.GetCompanyProfileAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(cachedData.CompanyName, result.CompanyName);
            _mockCacheService.Verify(cache => cache.GetAsync<CompanyProfile>($"profile:{symbol}"), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetCompanyProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetCompanyProfileAsync_WhenCacheMiss_FetchesFromApiAndCaches()
        {
            // Arrange
            var symbol = "AAPL";
            var apiData = new CompanyProfile
            {
                Symbol = symbol,
                CompanyName = "Apple Inc.",
                Sector = "Technology",
                Industry = "Consumer Electronics"
            };

            _mockCacheService.Setup(cache => cache.GetAsync<CompanyProfile>($"profile:{symbol}"))
                .ReturnsAsync((CompanyProfile?)null);
            _mockDataProvider.Setup(provider => provider.GetCompanyProfileAsync(symbol, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiData);

            // Act
            var result = await _stockService.GetCompanyProfileAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(apiData.CompanyName, result.CompanyName);
            _mockCacheService.Verify(cache => cache.GetAsync<CompanyProfile>($"profile:{symbol}"), Times.Once);
            _mockDataProvider.Verify(provider => provider.GetCompanyProfileAsync(symbol, It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(cache => cache.SetAsync($"profile:{symbol}", apiData, TimeSpan.FromSeconds(604800)), Times.Once);
        }

        [Fact]
        public async Task SearchSymbolsAsync_WhenCacheHit_ReturnsCachedData()
        {
            // Arrange
            var query = "apple";
            var cacheKey = "search:apple";
            var cachedData = new List<StockSearchResult>
            {
                new StockSearchResult { Symbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ" },
                new StockSearchResult { Symbol = "APLE", Name = "Apple Hospitality REIT", Exchange = "NYSE" }
            };

            _mockCacheService.Setup(cache => cache.GetAsync<List<StockSearchResult>>(cacheKey))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _stockService.SearchSymbolsAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("AAPL", result[0].Symbol);
            _mockCacheService.Verify(cache => cache.GetAsync<List<StockSearchResult>>(cacheKey), Times.Once);
            _mockDataProvider.Verify(provider => provider.SearchSymbolsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SearchSymbolsAsync_WhenCacheMiss_FetchesFromApiAndCaches()
        {
            // Arrange
            var query = "apple";
            var cacheKey = "search:apple";
            var apiData = new List<StockSearchResult>
            {
                new StockSearchResult { Symbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ" },
                new StockSearchResult { Symbol = "APLE", Name = "Apple Hospitality REIT", Exchange = "NYSE" }
            };

            _mockCacheService.Setup(cache => cache.GetAsync<List<StockSearchResult>>(cacheKey))
                .ReturnsAsync((List<StockSearchResult>?)null);
            _mockDataProvider.Setup(provider => provider.SearchSymbolsAsync(query, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiData);

            // Act
            var result = await _stockService.SearchSymbolsAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockCacheService.Verify(cache => cache.GetAsync<List<StockSearchResult>>(cacheKey), Times.Once);
            _mockDataProvider.Verify(provider => provider.SearchSymbolsAsync(query, 10, It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(cache => cache.SetAsync(cacheKey, apiData, TimeSpan.FromSeconds(3600)), Times.Once);
        }

        [Fact]
        public async Task SearchSymbolsAsync_NormalizesQueryToLowerCase()
        {
            // Arrange
            var query = "APPLE";
            var cacheKey = "search:apple"; // Should be lowercase
            var cachedData = new List<StockSearchResult>
            {
                new StockSearchResult { Symbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ" }
            };

            _mockCacheService.Setup(cache => cache.GetAsync<List<StockSearchResult>>(cacheKey))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _stockService.SearchSymbolsAsync(query);

            // Assert
            Assert.NotNull(result);
            _mockCacheService.Verify(cache => cache.GetAsync<List<StockSearchResult>>(cacheKey), Times.Once);
        }
    }
}
