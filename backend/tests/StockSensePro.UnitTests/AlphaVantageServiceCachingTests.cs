using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

/// <summary>
/// Unit tests for provider-specific caching functionality in AlphaVantageService
/// Tests Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6
/// </summary>
public class AlphaVantageServiceCachingTests
{
    private readonly Mock<ILogger<AlphaVantageService>> _mockLogger;
    private readonly Mock<IOptions<AlphaVantageSettings>> _mockAlphaVantageOptions;
    private readonly Mock<IOptions<CacheSettings>> _mockCacheOptions;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly AlphaVantageSettings _alphaVantageSettings;
    private readonly CacheSettings _cacheSettings;

    public AlphaVantageServiceCachingTests()
    {
        _mockLogger = new Mock<ILogger<AlphaVantageService>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        _alphaVantageSettings = new AlphaVantageSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://www.alphavantage.co/query",
            Timeout = 10,
            Enabled = true,
            DataEnrichment = new DataEnrichmentSettings
            {
                EnableBidAskEnrichment = false,
                EnableCalculated52WeekRange = false,
                EnableCalculatedAverageVolume = false
            }
        };

        _cacheSettings = new CacheSettings
        {
            AlphaVantage = new ProviderCacheSettings
            {
                QuoteTTL = 900,           // 15 minutes
                HistoricalTTL = 86400,    // 24 hours
                FundamentalsTTL = 21600,  // 6 hours
                ProfileTTL = 604800,      // 7 days
                SearchTTL = 3600,         // 1 hour
                CalculatedFieldsTTL = 86400 // 24 hours
            }
        };

        _mockAlphaVantageOptions = new Mock<IOptions<AlphaVantageSettings>>();
        _mockAlphaVantageOptions.Setup(o => o.Value).Returns(_alphaVantageSettings);

        _mockCacheOptions = new Mock<IOptions<CacheSettings>>();
        _mockCacheOptions.Setup(o => o.Value).Returns(_cacheSettings);
    }

    private AlphaVantageService CreateService()
    {
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_alphaVantageSettings.BaseUrl)
        };
        return new AlphaVantageService(
            httpClient,
            _mockLogger.Object,
            _mockAlphaVantageOptions.Object,
            _mockCacheOptions.Object,
            _mockCacheService.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    // ===== Cache Key Format Tests (Requirement 8.1) =====

    [Fact]
    public async Task GetQuoteAsync_UsesCacheKeyFormat_ProviderDataTypeSymbol()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedCacheKey = $"alphavantage:quote:{symbol}";
        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%"",
                ""06. volume"": ""50000000"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<MarketData>(expectedCacheKey))
            .ReturnsAsync((MarketData?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetQuoteAsync(symbol);

        // Assert - Verify cache key format
        _mockCacheService.Verify(c => c.GetAsync<MarketData>(expectedCacheKey), Times.Once);
        _mockCacheService.Verify(c => c.SetAsync(
            expectedCacheKey,
            It.IsAny<MarketData>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_UsesCacheKeyFormat_WithDateRangeAndInterval()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var expectedCacheKey = $"alphavantage:historical:{symbol}:2024-01-10:2024-01-15:Daily";
        var responseJson = @"{
            ""Time Series (Daily)"": {
                ""2024-01-15"": {
                    ""1. open"": ""175.00"",
                    ""2. high"": ""176.50"",
                    ""3. low"": ""174.00"",
                    ""4. close"": ""175.50"",
                    ""5. volume"": ""50000000""
                }
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<List<StockPrice>>(expectedCacheKey))
            .ReturnsAsync((List<StockPrice>?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert - Verify cache key format
        _mockCacheService.Verify(c => c.GetAsync<List<StockPrice>>(expectedCacheKey), Times.Once);
    }

    // ===== Provider-Specific TTL Tests (Requirements 8.1, 8.2, 8.3, 8.4, 8.5) =====

    [Fact]
    public async Task GetQuoteAsync_CachesWithProviderSpecificQuoteTTL()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedTTL = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.QuoteTTL);
        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%"",
                ""06. volume"": ""50000000"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
            .ReturnsAsync((MarketData?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetQuoteAsync(symbol);

        // Assert - Verify TTL is 900 seconds (15 minutes)
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<MarketData>(),
            expectedTTL), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_CachesWithProviderSpecificHistoricalTTL()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var expectedTTL = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.HistoricalTTL);
        var responseJson = @"{
            ""Time Series (Daily)"": {
                ""2024-01-15"": {
                    ""1. open"": ""175.00"",
                    ""2. high"": ""176.50"",
                    ""3. low"": ""174.00"",
                    ""4. close"": ""175.50"",
                    ""5. volume"": ""50000000""
                }
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<List<StockPrice>>(It.IsAny<string>()))
            .ReturnsAsync((List<StockPrice>?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert - Verify TTL is 86400 seconds (24 hours)
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<StockPrice>>(),
            expectedTTL), Times.Once);
    }

    [Fact]
    public async Task GetFundamentalsAsync_CachesWithProviderSpecificFundamentalsTTL()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedTTL = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.FundamentalsTTL);
        var responseJson = @"{
            ""Symbol"": ""AAPL"",
            ""Name"": ""Apple Inc"",
            ""PERatio"": ""28.5"",
            ""EPS"": ""6.15""
        }";

        _mockCacheService.Setup(c => c.GetAsync<FundamentalData>(It.IsAny<string>()))
            .ReturnsAsync((FundamentalData?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetFundamentalsAsync(symbol);

        // Assert - Verify TTL is 21600 seconds (6 hours)
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<FundamentalData>(),
            expectedTTL), Times.Once);
    }

    [Fact]
    public async Task GetCompanyProfileAsync_CachesWithProviderSpecificProfileTTL()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedTTL = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.ProfileTTL);
        var responseJson = @"{
            ""Symbol"": ""AAPL"",
            ""Name"": ""Apple Inc"",
            ""Sector"": ""TECHNOLOGY"",
            ""Industry"": ""ELECTRONIC COMPUTERS""
        }";

        _mockCacheService.Setup(c => c.GetAsync<CompanyProfile>(It.IsAny<string>()))
            .ReturnsAsync((CompanyProfile?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetCompanyProfileAsync(symbol);

        // Assert - Verify TTL is 604800 seconds (7 days)
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<CompanyProfile>(),
            expectedTTL), Times.Once);
    }

    [Fact]
    public async Task SearchSymbolsAsync_CachesWithProviderSpecificSearchTTL()
    {
        // Arrange
        var query = "Apple";
        var expectedTTL = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.SearchTTL);
        var responseJson = @"{
            ""bestMatches"": [
                {
                    ""1. symbol"": ""AAPL"",
                    ""2. name"": ""Apple Inc"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United States"",
                    ""9. matchScore"": ""1.0000""
                }
            ]
        }";

        _mockCacheService.Setup(c => c.GetAsync<List<StockSearchResult>>(It.IsAny<string>()))
            .ReturnsAsync((List<StockSearchResult>?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.SearchSymbolsAsync(query);

        // Assert - Verify TTL is 3600 seconds (1 hour)
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<StockSearchResult>>(),
            expectedTTL), Times.Once);
    }

    // ===== Cache-Aside Pattern Tests (Requirement 8.6) =====

    [Fact]
    public async Task GetQuoteAsync_OnSuccessfulFetch_CachesData()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%"",
                ""06. volume"": ""50000000"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
            .ReturnsAsync((MarketData?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetQuoteAsync(symbol);

        // Assert - Should fetch from API and cache the result
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(175.50m, result.CurrentPrice);

        // Verify data was cached
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<MarketData>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenCacheMiss_FetchesFromApiAndCaches()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%"",
                ""06. volume"": ""50000000"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
            .ReturnsAsync((MarketData?)null); // Cache miss

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetQuoteAsync(symbol);

        // Assert - Should fetch from API and cache
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(175.50m, result.CurrentPrice);

        // Verify HTTP call was made
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        // Verify data was cached
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<MarketData>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    // ===== Custom TTL Configuration Tests =====

    [Fact]
    public async Task GetQuoteAsync_WithCustomTTL_UsesConfiguredValue()
    {
        // Arrange
        var symbol = "AAPL";
        var customQuoteTTL = 300; // 5 minutes instead of default 15
        _cacheSettings.AlphaVantage.QuoteTTL = customQuoteTTL;
        var expectedTTL = TimeSpan.FromSeconds(customQuoteTTL);

        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%"",
                ""06. volume"": ""50000000"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
            .ReturnsAsync((MarketData?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetQuoteAsync(symbol);

        // Assert - Verify custom TTL is used
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<MarketData>(),
            expectedTTL), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithCustomTTL_UsesConfiguredValue()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var customHistoricalTTL = 43200; // 12 hours instead of default 24
        _cacheSettings.AlphaVantage.HistoricalTTL = customHistoricalTTL;
        var expectedTTL = TimeSpan.FromSeconds(customHistoricalTTL);

        var responseJson = @"{
            ""Time Series (Daily)"": {
                ""2024-01-15"": {
                    ""1. open"": ""175.00"",
                    ""2. high"": ""176.50"",
                    ""3. low"": ""174.00"",
                    ""4. close"": ""175.50"",
                    ""5. volume"": ""50000000""
                }
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<List<StockPrice>>(It.IsAny<string>()))
            .ReturnsAsync((List<StockPrice>?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert - Verify custom TTL is used
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<StockPrice>>(),
            expectedTTL), Times.Once);
    }

    // ===== Cache Logging Tests =====

    [Fact]
    public async Task GetQuoteAsync_WhenCaching_LogsCacheOperation()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%"",
                ""06. volume"": ""50000000"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        _mockCacheService.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
            .ReturnsAsync((MarketData?)null);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        await service.GetQuoteAsync(symbol);

        // Assert - Verify cache logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cached quote")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
