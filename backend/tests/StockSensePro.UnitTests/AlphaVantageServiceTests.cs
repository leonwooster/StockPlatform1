using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Exceptions;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

public class AlphaVantageServiceTests
{
    private readonly Mock<ILogger<AlphaVantageService>> _mockLogger;
    private readonly Mock<IOptions<AlphaVantageSettings>> _mockOptions;
    private readonly Mock<IOptions<CacheSettings>> _mockCacheOptions;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly AlphaVantageSettings _settings;
    private readonly CacheSettings _cacheSettings;

    public AlphaVantageServiceTests()
    {
        _mockLogger = new Mock<ILogger<AlphaVantageService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _settings = new AlphaVantageSettings
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
        _mockOptions = new Mock<IOptions<AlphaVantageSettings>>();
        _mockOptions.Setup(o => o.Value).Returns(_settings);

        _cacheSettings = new CacheSettings
        {
            AlphaVantage = new ProviderCacheSettings
            {
                QuoteTTL = 900,
                HistoricalTTL = 86400,
                FundamentalsTTL = 21600,
                ProfileTTL = 604800,
                SearchTTL = 3600,
                CalculatedFieldsTTL = 86400
            }
        };
        _mockCacheOptions = new Mock<IOptions<CacheSettings>>();
        _mockCacheOptions.Setup(o => o.Value).Returns(_cacheSettings);
    }

    private AlphaVantageService CreateService()
    {
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_settings.BaseUrl)
        };
        return new AlphaVantageService(httpClient, _mockLogger.Object, _mockOptions.Object, _mockCacheOptions.Object);
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

    [Fact]
    public void Constructor_WithValidParameters_InitializesSuccessfully()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new AlphaVantageService(httpClient, _mockLogger.Object, _mockOptions.Object, _mockCacheOptions.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AlphaVantageService(null!, _mockLogger.Object, _mockOptions.Object, _mockCacheOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AlphaVantageService(httpClient, null!, _mockOptions.Object, _mockCacheOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AlphaVantageService(httpClient, _mockLogger.Object, null!, _mockCacheOptions.Object));
    }

    [Fact]
    public void Constructor_ConfiguresHttpClientWithBaseUrl()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new AlphaVantageService(httpClient, _mockLogger.Object, _mockOptions.Object, _mockCacheOptions.Object);

        // Assert
        Assert.Equal(new Uri(_settings.BaseUrl), httpClient.BaseAddress);
    }

    [Fact]
    public void Constructor_ConfiguresHttpClientWithTimeout()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new AlphaVantageService(httpClient, _mockLogger.Object, _mockOptions.Object, _mockCacheOptions.Object);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(_settings.Timeout), httpClient.Timeout);
    }

    [Fact]
    public void Constructor_LogsInitialization()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new AlphaVantageService(httpClient, _mockLogger.Object, _mockOptions.Object, _mockCacheOptions.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AlphaVantageService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ===== GetQuoteAsync Tests =====

    [Fact]
    public async Task GetQuoteAsync_WithValidSymbol_ReturnsMarketData()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""02. open"": ""174.50"",
                ""03. high"": ""176.00"",
                ""04. low"": ""174.00"",
                ""05. price"": ""175.50"",
                ""06. volume"": ""50000000"",
                ""07. latest trading day"": ""2024-01-15"",
                ""08. previous close"": ""173.00"",
                ""09. change"": ""2.50"",
                ""10. change percent"": ""1.45%""
            }
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetQuoteAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(175.50m, result.CurrentPrice);
        Assert.Equal(2.50m, result.Change);
        Assert.Equal(1.45m, result.ChangePercent);
        Assert.Equal(50000000L, result.Volume);
        Assert.Equal(176.00m, result.DayHigh);
        Assert.Equal(174.00m, result.DayLow);
    }

    [Fact]
    public async Task GetQuoteAsync_WithInvalidSymbol_ThrowsSymbolNotFoundException()
    {
        // Arrange
        var symbol = "INVALID";
        var responseJson = @"{ ""Global Quote"": {} }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<SymbolNotFoundException>(() => service.GetQuoteAsync(symbol));
    }

    [Fact]
    public async Task GetQuoteAsync_WithRateLimitError_ThrowsRateLimitExceededException()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute and 500 calls per day.""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() => service.GetQuoteAsync(symbol));
    }

    [Fact]
    public async Task GetQuoteAsync_WithInvalidApiKey_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Error Message"": ""Invalid API call. Please retry or visit the documentation for API key.""
        }";

        SetupHttpResponse(HttpStatusCode.Unauthorized, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetQuoteAsync(symbol));
    }

    [Fact]
    public async Task GetQuoteAsync_WithNetworkError_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetQuoteAsync(symbol));
    }

    [Fact]
    public async Task GetQuoteAsync_WithTimeout_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetQuoteAsync(symbol));
    }

    // ===== GetQuotesAsync Tests =====

    [Fact]
    public async Task GetQuotesAsync_WithMultipleSymbols_ReturnsMarketDataList()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "MSFT" };
        var appleResponse = @"{
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
        var msftResponse = @"{
            ""Global Quote"": {
                ""01. symbol"": ""MSFT"",
                ""05. price"": ""380.00"",
                ""08. previous close"": ""378.00"",
                ""09. change"": ""2.00"",
                ""10. change percent"": ""0.53%"",
                ""06. volume"": ""30000000"",
                ""03. high"": ""381.00"",
                ""04. low"": ""379.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        var callCount = 0;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(callCount == 1 ? appleResponse : msftResponse)
                };
            });

        var service = CreateService();

        // Act
        var result = await service.GetQuotesAsync(symbols);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal(175.50m, result[0].CurrentPrice);
        Assert.Equal("MSFT", result[1].Symbol);
        Assert.Equal(380.00m, result[1].CurrentPrice);
    }

    [Fact]
    public async Task GetQuotesAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var symbols = new List<string>();
        var service = CreateService();

        // Act
        var result = await service.GetQuotesAsync(symbols);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetQuotesAsync_WithPartialFailures_ReturnsPartialResults()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "INVALID", "MSFT" };
        var appleResponse = @"{
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
        var invalidResponse = @"{ ""Global Quote"": {} }";
        var msftResponse = @"{
            ""Global Quote"": {
                ""01. symbol"": ""MSFT"",
                ""05. price"": ""380.00"",
                ""08. previous close"": ""378.00"",
                ""09. change"": ""2.00"",
                ""10. change percent"": ""0.53%"",
                ""06. volume"": ""30000000"",
                ""03. high"": ""381.00"",
                ""04. low"": ""379.00"",
                ""07. latest trading day"": ""2024-01-15""
            }
        }";

        var callCount = 0;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                var content = callCount switch
                {
                    1 => appleResponse,
                    2 => invalidResponse,
                    _ => msftResponse
                };
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(content)
                };
            });

        var service = CreateService();

        // Act
        var result = await service.GetQuotesAsync(symbols);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Only AAPL and MSFT, INVALID is skipped
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal("MSFT", result[1].Symbol);
    }

    [Fact]
    public async Task GetQuotesAsync_WithRateLimitExceeded_StopsProcessing()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "MSFT", "GOOGL" };
        var appleResponse = @"{
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
        var rateLimitResponse = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute.""
        }";

        var callCount = 0;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(callCount == 1 ? appleResponse : rateLimitResponse)
                };
            });

        var service = CreateService();

        // Act
        var result = await service.GetQuotesAsync(symbols);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only AAPL, stopped after rate limit on MSFT
        Assert.Equal("AAPL", result[0].Symbol);
    }

    // ===== GetHistoricalPricesAsync Tests =====

    [Fact]
    public async Task GetHistoricalPricesAsync_WithValidSymbolDaily_ReturnsStockPriceList()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var responseJson = @"{
            ""Time Series (Daily)"": {
                ""2024-01-15"": {
                    ""1. open"": ""175.00"",
                    ""2. high"": ""176.50"",
                    ""3. low"": ""174.00"",
                    ""4. close"": ""175.50"",
                    ""5. volume"": ""50000000""
                },
                ""2024-01-12"": {
                    ""1. open"": ""173.00"",
                    ""2. high"": ""174.50"",
                    ""3. low"": ""172.50"",
                    ""4. close"": ""174.00"",
                    ""5. volume"": ""45000000""
                },
                ""2024-01-11"": {
                    ""1. open"": ""172.00"",
                    ""2. high"": ""173.50"",
                    ""3. low"": ""171.50"",
                    ""4. close"": ""173.00"",
                    ""5. volume"": ""40000000""
                },
                ""2024-01-09"": {
                    ""1. open"": ""170.00"",
                    ""2. high"": ""171.50"",
                    ""3. low"": ""169.50"",
                    ""4. close"": ""171.00"",
                    ""5. volume"": ""35000000""
                }
            }
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Only dates within range (1/11, 1/12, 1/15)
        Assert.Equal(symbol, result[0].Symbol);
        Assert.Equal(new DateTime(2024, 1, 11), result[0].Date);
        Assert.Equal(173.00m, result[0].Close);
        Assert.Equal(40000000L, result[0].Volume);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithWeeklyInterval_ReturnsWeeklyData()
    {
        // Arrange
        var symbol = "MSFT";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var responseJson = @"{
            ""Weekly Time Series"": {
                ""2024-01-26"": {
                    ""1. open"": ""380.00"",
                    ""2. high"": ""385.00"",
                    ""3. low"": ""378.00"",
                    ""4. close"": ""383.50"",
                    ""5. volume"": ""150000000""
                },
                ""2024-01-19"": {
                    ""1. open"": ""375.00"",
                    ""2. high"": ""381.00"",
                    ""3. low"": ""374.00"",
                    ""4. close"": ""380.00"",
                    ""5. volume"": ""140000000""
                }
            }
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetHistoricalPricesAsync(symbol, startDate, endDate, Core.Enums.TimeInterval.Weekly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(symbol, result[0].Symbol);
        Assert.Equal(380.00m, result[0].Close);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithMonthlyInterval_ReturnsMonthlyData()
    {
        // Arrange
        var symbol = "GOOGL";
        var startDate = new DateTime(2023, 12, 1);
        var endDate = new DateTime(2024, 2, 29);
        var responseJson = @"{
            ""Monthly Time Series"": {
                ""2024-02-29"": {
                    ""1. open"": ""140.00"",
                    ""2. high"": ""145.00"",
                    ""3. low"": ""138.00"",
                    ""4. close"": ""143.50"",
                    ""5. volume"": ""500000000""
                },
                ""2024-01-31"": {
                    ""1. open"": ""135.00"",
                    ""2. high"": ""141.00"",
                    ""3. low"": ""134.00"",
                    ""4. close"": ""140.00"",
                    ""5. volume"": ""480000000""
                },
                ""2023-12-31"": {
                    ""1. open"": ""130.00"",
                    ""2. high"": ""136.00"",
                    ""3. low"": ""129.00"",
                    ""4. close"": ""135.00"",
                    ""5. volume"": ""450000000""
                }
            }
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetHistoricalPricesAsync(symbol, startDate, endDate, Core.Enums.TimeInterval.Monthly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(symbol, result[0].Symbol);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithInvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 15);
        var endDate = new DateTime(2024, 1, 10);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetHistoricalPricesAsync(symbol, startDate, endDate));
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithFutureEndDate_AdjustsToToday()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.Date.AddDays(-5);
        var endDate = DateTime.UtcNow.Date.AddDays(5); // Future date
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

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert - Should not throw and should process successfully
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithInvalidSymbol_ThrowsSymbolNotFoundException()
    {
        // Arrange
        var symbol = "INVALID";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var responseJson = @"{ ""Time Series (Daily)"": {} }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<SymbolNotFoundException>(() =>
            service.GetHistoricalPricesAsync(symbol, startDate, endDate));
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithRateLimitError_ThrowsRateLimitExceededException()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var responseJson = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute.""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() =>
            service.GetHistoricalPricesAsync(symbol, startDate, endDate));
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_WithNetworkError_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() =>
            service.GetHistoricalPricesAsync(symbol, startDate, endDate));
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_FiltersDataByDateRange()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 12);
        var endDate = new DateTime(2024, 1, 14);
        var responseJson = @"{
            ""Time Series (Daily)"": {
                ""2024-01-15"": {
                    ""1. open"": ""175.00"",
                    ""2. high"": ""176.50"",
                    ""3. low"": ""174.00"",
                    ""4. close"": ""175.50"",
                    ""5. volume"": ""50000000""
                },
                ""2024-01-12"": {
                    ""1. open"": ""173.00"",
                    ""2. high"": ""174.50"",
                    ""3. low"": ""172.50"",
                    ""4. close"": ""174.00"",
                    ""5. volume"": ""45000000""
                },
                ""2024-01-11"": {
                    ""1. open"": ""172.00"",
                    ""2. high"": ""173.50"",
                    ""3. low"": ""171.50"",
                    ""4. close"": ""173.00"",
                    ""5. volume"": ""40000000""
                }
            }
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only 1/12 is within range
        Assert.Equal(new DateTime(2024, 1, 12), result[0].Date);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_ReturnsDataInChronologicalOrder()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = new DateTime(2024, 1, 10);
        var endDate = new DateTime(2024, 1, 15);
        var responseJson = @"{
            ""Time Series (Daily)"": {
                ""2024-01-15"": {
                    ""1. open"": ""175.00"",
                    ""2. high"": ""176.50"",
                    ""3. low"": ""174.00"",
                    ""4. close"": ""175.50"",
                    ""5. volume"": ""50000000""
                },
                ""2024-01-11"": {
                    ""1. open"": ""172.00"",
                    ""2. high"": ""173.50"",
                    ""3. low"": ""171.50"",
                    ""4. close"": ""173.00"",
                    ""5. volume"": ""40000000""
                },
                ""2024-01-12"": {
                    ""1. open"": ""173.00"",
                    ""2. high"": ""174.50"",
                    ""3. low"": ""172.50"",
                    ""4. close"": ""174.00"",
                    ""5. volume"": ""45000000""
                }
            }
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetHistoricalPricesAsync(symbol, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result[0].Date < result[1].Date);
        Assert.True(result[1].Date < result[2].Date);
    }

    // ===== GetFundamentalsAsync Tests =====

    [Fact]
    public async Task GetFundamentalsAsync_WithValidSymbol_ReturnsFundamentalData()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Symbol"": ""AAPL"",
            ""AssetType"": ""Common Stock"",
            ""Name"": ""Apple Inc"",
            ""Exchange"": ""NASDAQ"",
            ""Currency"": ""USD"",
            ""Country"": ""USA"",
            ""Sector"": ""TECHNOLOGY"",
            ""Industry"": ""ELECTRONIC COMPUTERS"",
            ""PERatio"": ""28.5"",
            ""PEGRatio"": ""2.1"",
            ""PriceToBookRatio"": ""45.2"",
            ""PriceToSalesRatioTTM"": ""7.8"",
            ""EVToRevenue"": ""7.5"",
            ""EVToEBITDA"": ""22.3"",
            ""ProfitMargin"": ""0.25"",
            ""OperatingMarginTTM"": ""0.30"",
            ""ReturnOnEquityTTM"": ""1.47"",
            ""ReturnOnAssetsTTM"": ""0.22"",
            ""QuarterlyRevenueGrowthYOY"": ""0.08"",
            ""QuarterlyEarningsGrowthYOY"": ""0.11"",
            ""EPS"": ""6.15"",
            ""RevenueTTM"": ""383285000000"",
            ""DividendYield"": ""0.0052"",
            ""PayoutRatio"": ""0.15"",
            ""CurrentRatio"": ""1.07"",
            ""DebtToEquity"": ""1.73"",
            ""QuickRatio"": ""0.95"",
            ""MarketCapitalization"": ""2800000000000""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetFundamentalsAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(28.5m, result.PERatio);
        Assert.Equal(2.1m, result.PEGRatio);
        Assert.Equal(45.2m, result.PriceToBook);
        Assert.Equal(7.8m, result.PriceToSales);
        Assert.Equal(22.3m, result.EVToEBITDA);
        Assert.Equal(0.25m, result.ProfitMargin);
        Assert.Equal(0.30m, result.OperatingMargin);
        Assert.Equal(1.47m, result.ReturnOnEquity);
        Assert.Equal(0.22m, result.ReturnOnAssets);
        Assert.Equal(0.08m, result.RevenueGrowth);
        Assert.Equal(0.11m, result.EarningsGrowth);
        Assert.Equal(6.15m, result.EPS);
        Assert.Equal(0.0052m, result.DividendYield);
        Assert.Equal(0.15m, result.PayoutRatio);
        Assert.Equal(1.07m, result.CurrentRatio);
        Assert.Equal(1.73m, result.DebtToEquity);
        Assert.Equal(0.95m, result.QuickRatio);
        Assert.True(result.LastUpdated <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetFundamentalsAsync_WithMissingFields_HandlesGracefully()
    {
        // Arrange
        var symbol = "TSLA";
        var responseJson = @"{
            ""Symbol"": ""TSLA"",
            ""Name"": ""Tesla Inc"",
            ""PERatio"": ""None"",
            ""PEGRatio"": ""-"",
            ""EPS"": ""3.62"",
            ""DividendYield"": ""None"",
            ""PayoutRatio"": ""None""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetFundamentalsAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Null(result.PERatio); // "None" should be parsed as null
        Assert.Null(result.PEGRatio); // "-" should be parsed as null
        Assert.Equal(3.62m, result.EPS);
        Assert.Null(result.DividendYield);
        Assert.Null(result.PayoutRatio);
    }

    [Fact]
    public async Task GetFundamentalsAsync_WithInvalidSymbol_ThrowsSymbolNotFoundException()
    {
        // Arrange
        var symbol = "INVALID";
        var responseJson = @"{}";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<SymbolNotFoundException>(() => service.GetFundamentalsAsync(symbol));
    }

    [Fact]
    public async Task GetFundamentalsAsync_WithRateLimitError_ThrowsRateLimitExceededException()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute.""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() => service.GetFundamentalsAsync(symbol));
    }

    [Fact]
    public async Task GetFundamentalsAsync_WithInvalidApiKey_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Error Message"": ""Invalid API call. Please retry or visit the documentation for API key.""
        }";

        SetupHttpResponse(HttpStatusCode.Unauthorized, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetFundamentalsAsync(symbol));
    }

    [Fact]
    public async Task GetFundamentalsAsync_WithNetworkError_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetFundamentalsAsync(symbol));
    }

    [Fact]
    public async Task GetFundamentalsAsync_WithTimeout_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetFundamentalsAsync(symbol));
    }

    // ===== GetCompanyProfileAsync Tests =====

    [Fact]
    public async Task GetCompanyProfileAsync_WithValidSymbol_ReturnsCompanyProfile()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Symbol"": ""AAPL"",
            ""Name"": ""Apple Inc"",
            ""Description"": ""Apple Inc. is an American multinational technology company."",
            ""Exchange"": ""NASDAQ"",
            ""Currency"": ""USD"",
            ""Country"": ""USA"",
            ""Sector"": ""TECHNOLOGY"",
            ""Industry"": ""ELECTRONIC COMPUTERS""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetCompanyProfileAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal("Apple Inc", result.CompanyName);
        Assert.Equal("TECHNOLOGY", result.Sector);
        Assert.Equal("ELECTRONIC COMPUTERS", result.Industry);
        Assert.Equal("Apple Inc. is an American multinational technology company.", result.Description);
        Assert.Equal("USA", result.Country);
        Assert.Equal("NASDAQ", result.Exchange);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetCompanyProfileAsync_WithMissingFields_HandlesGracefully()
    {
        // Arrange
        var symbol = "TSLA";
        var responseJson = @"{
            ""Symbol"": ""TSLA"",
            ""Name"": ""Tesla Inc"",
            ""Sector"": ""Consumer Cyclical"",
            ""Industry"": ""Auto Manufacturers""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.GetCompanyProfileAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal("Tesla Inc", result.CompanyName);
        Assert.Equal("Consumer Cyclical", result.Sector);
        Assert.Equal("Auto Manufacturers", result.Industry);
        Assert.Equal(string.Empty, result.Description);
        Assert.Equal(string.Empty, result.Country);
        Assert.Equal(string.Empty, result.Exchange);
        Assert.Equal(string.Empty, result.Currency);
    }

    [Fact]
    public async Task GetCompanyProfileAsync_WithInvalidSymbol_ThrowsSymbolNotFoundException()
    {
        // Arrange
        var symbol = "INVALID";
        var responseJson = @"{}";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<SymbolNotFoundException>(() => service.GetCompanyProfileAsync(symbol));
    }

    [Fact]
    public async Task GetCompanyProfileAsync_WithRateLimitError_ThrowsRateLimitExceededException()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute.""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() => service.GetCompanyProfileAsync(symbol));
    }

    [Fact]
    public async Task GetCompanyProfileAsync_WithInvalidApiKey_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        var responseJson = @"{
            ""Error Message"": ""Invalid API call. Please retry or visit the documentation for API key.""
        }";

        SetupHttpResponse(HttpStatusCode.Unauthorized, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetCompanyProfileAsync(symbol));
    }

    [Fact]
    public async Task GetCompanyProfileAsync_WithNetworkError_ThrowsApiUnavailableException()
    {
        // Arrange
        var symbol = "AAPL";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.GetCompanyProfileAsync(symbol));
    }

    // ===== SearchSymbolsAsync Tests =====

    [Fact]
    public async Task SearchSymbolsAsync_WithValidQuery_ReturnsSearchResults()
    {
        // Arrange
        var query = "Apple";
        var responseJson = @"{
            ""bestMatches"": [
                {
                    ""1. symbol"": ""AAPL"",
                    ""2. name"": ""Apple Inc"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United States"",
                    ""5. marketOpen"": ""09:30"",
                    ""6. marketClose"": ""16:00"",
                    ""7. timezone"": ""UTC-04"",
                    ""8. currency"": ""USD"",
                    ""9. matchScore"": ""1.0000""
                },
                {
                    ""1. symbol"": ""AAPL.LON"",
                    ""2. name"": ""Apple Inc"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United Kingdom"",
                    ""5. marketOpen"": ""08:00"",
                    ""6. marketClose"": ""16:30"",
                    ""7. timezone"": ""UTC+01"",
                    ""8. currency"": ""GBP"",
                    ""9. matchScore"": ""0.8000""
                }
            ]
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.SearchSymbolsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal("Apple Inc", result[0].Name);
        Assert.Equal("Equity", result[0].AssetType);
        Assert.Equal("United States", result[0].Region);
        Assert.Equal(1.0000m, result[0].MatchScore);
        Assert.Equal("AAPL.LON", result[1].Symbol);
        Assert.Equal(0.8000m, result[1].MatchScore);
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var query = "Tech";
        var limit = 2;
        var responseJson = @"{
            ""bestMatches"": [
                {
                    ""1. symbol"": ""AAPL"",
                    ""2. name"": ""Apple Inc"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United States"",
                    ""9. matchScore"": ""1.0000""
                },
                {
                    ""1. symbol"": ""MSFT"",
                    ""2. name"": ""Microsoft Corporation"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United States"",
                    ""9. matchScore"": ""0.9500""
                },
                {
                    ""1. symbol"": ""GOOGL"",
                    ""2. name"": ""Alphabet Inc"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United States"",
                    ""9. matchScore"": ""0.9000""
                }
            ]
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.SearchSymbolsAsync(query, limit);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Limited to 2 results
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal("MSFT", result[1].Symbol);
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Arrange
        var query = "";
        var service = CreateService();

        // Act
        var result = await service.SearchSymbolsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithWhitespaceQuery_ReturnsEmptyList()
    {
        // Arrange
        var query = "   ";
        var service = CreateService();

        // Act
        var result = await service.SearchSymbolsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var query = "NONEXISTENT123";
        var responseJson = @"{
            ""bestMatches"": []
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.SearchSymbolsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithRateLimitError_ThrowsRateLimitExceededException()
    {
        // Arrange
        var query = "Apple";
        var responseJson = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute.""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() => service.SearchSymbolsAsync(query));
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithInvalidApiKey_ThrowsApiUnavailableException()
    {
        // Arrange
        var query = "Apple";
        var responseJson = @"{
            ""Error Message"": ""Invalid API call. Please retry or visit the documentation for API key.""
        }";

        SetupHttpResponse(HttpStatusCode.Unauthorized, responseJson);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.SearchSymbolsAsync(query));
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithNetworkError_ThrowsApiUnavailableException()
    {
        // Arrange
        var query = "Apple";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.SearchSymbolsAsync(query));
    }

    [Fact]
    public async Task SearchSymbolsAsync_WithTimeout_ThrowsApiUnavailableException()
    {
        // Arrange
        var query = "Apple";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ApiUnavailableException>(() => service.SearchSymbolsAsync(query));
    }

    // ===== IsHealthyAsync Tests =====

    [Fact]
    public async Task IsHealthyAsync_WithSuccessfulResponse_ReturnsTrue()
    {
        // Arrange
        var responseJson = @"{
            ""bestMatches"": [
                {
                    ""1. symbol"": ""IBM"",
                    ""2. name"": ""International Business Machines Corporation"",
                    ""3. type"": ""Equity"",
                    ""4. region"": ""United States"",
                    ""9. matchScore"": ""1.0000""
                }
            ]
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WithInvalidApiKey_ReturnsFalse()
    {
        // Arrange
        var responseJson = @"{
            ""Error Message"": ""Invalid API call. Please retry or visit the documentation for API key.""
        }";

        SetupHttpResponse(HttpStatusCode.Unauthorized, responseJson);
        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WithRateLimitError_ReturnsFalse()
    {
        // Arrange - Rate limit response doesn't contain bestMatches
        var responseJson = @"{
            ""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute.""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.False(result); // Rate limit response doesn't contain bestMatches
    }

    [Fact]
    public async Task IsHealthyAsync_WithNetworkError_ReturnsFalse()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WithTimeout_ReturnsFalse()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WithEmptyResponse_ReturnsFalse()
    {
        // Arrange
        var responseJson = @"";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WithMalformedJson_ReturnsFalse()
    {
        // Arrange
        var responseJson = @"{ invalid json }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);
        var service = CreateService();

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.False(result);
    }
}
