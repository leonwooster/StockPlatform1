using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Exceptions;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class YahooFinanceServiceTests
    {
        private readonly Mock<ILogger<YahooFinanceService>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

        public YahooFinanceServiceTests()
        {
            _mockLogger = new Mock<ILogger<YahooFinanceService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        }

        private YahooFinanceService CreateService()
        {
            return new YahooFinanceService(_mockHttpClientFactory.Object, _mockLogger.Object);
        }

        private void SetupHttpClient(string clientName, HttpStatusCode statusCode, string content)
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

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.com/")
            };

            _mockHttpClientFactory
                .Setup(f => f.CreateClient(clientName))
                .Returns(httpClient);
        }

        // ===== GetQuoteAsync Tests =====

        [Fact]
        public async Task GetQuoteAsync_WithValidSymbol_ReturnsMarketData()
        {
            // Arrange
            var symbol = "AAPL";
            var responseJson = @"{
                ""quoteResponse"": {
                    ""result"": [{
                        ""symbol"": ""AAPL"",
                        ""regularMarketPrice"": 175.50,
                        ""regularMarketPreviousClose"": 173.00,
                        ""regularMarketVolume"": 50000000,
                        ""bid"": 175.45,
                        ""ask"": 175.55,
                        ""regularMarketDayHigh"": 176.00,
                        ""regularMarketDayLow"": 174.50,
                        ""fiftyTwoWeekHigh"": 198.00,
                        ""fiftyTwoWeekLow"": 164.00,
                        ""averageDailyVolume3Month"": 55000000,
                        ""marketCap"": 2800000000000,
                        ""fullExchangeName"": ""NASDAQ"",
                        ""marketState"": ""REGULAR""
                    }]
                }
            }";

            SetupHttpClient("YahooFinanceQuote", HttpStatusCode.OK, responseJson);
            var service = CreateService();

            // Act
            var result = await service.GetQuoteAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(175.50m, result.CurrentPrice);
            Assert.Equal(2.50m, result.Change);
            Assert.Equal(MarketState.Open, result.MarketState);
        }

        [Fact]
        public async Task GetQuoteAsync_WithInvalidSymbol_ThrowsSymbolNotFoundException()
        {
            // Arrange
            var symbol = "INVALID";
            SetupHttpClient("YahooFinanceQuote", HttpStatusCode.NotFound, "{}");
            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<SymbolNotFoundException>(() => service.GetQuoteAsync(symbol));
        }

        [Fact]
        public async Task GetQuoteAsync_WithRateLimitExceeded_ThrowsRateLimitExceededException()
        {
            // Arrange
            var symbol = "AAPL";
            SetupHttpClient("YahooFinanceQuote", HttpStatusCode.TooManyRequests, "{}");
            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<RateLimitExceededException>(() => service.GetQuoteAsync(symbol));
        }

        [Fact]
        public async Task GetQuoteAsync_WithServerError_ThrowsApiUnavailableException()
        {
            // Arrange
            var symbol = "AAPL";
            SetupHttpClient("YahooFinanceQuote", HttpStatusCode.InternalServerError, "{}");
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
            var responseJson = @"{
                ""quoteResponse"": {
                    ""result"": [
                        {
                            ""symbol"": ""AAPL"",
                            ""regularMarketPrice"": 175.50,
                            ""regularMarketPreviousClose"": 173.00,
                            ""regularMarketVolume"": 50000000,
                            ""marketState"": ""REGULAR""
                        },
                        {
                            ""symbol"": ""MSFT"",
                            ""regularMarketPrice"": 380.00,
                            ""regularMarketPreviousClose"": 378.00,
                            ""regularMarketVolume"": 30000000,
                            ""marketState"": ""REGULAR""
                        }
                    ]
                }
            }";

            SetupHttpClient("YahooFinanceQuote", HttpStatusCode.OK, responseJson);
            var service = CreateService();

            // Act
            var result = await service.GetQuotesAsync(symbols);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("AAPL", result[0].Symbol);
            Assert.Equal("MSFT", result[1].Symbol);
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

        // ===== SearchSymbolsAsync Tests =====

        [Fact]
        public async Task SearchSymbolsAsync_WithValidQuery_ReturnsSearchResults()
        {
            // Arrange
            var query = "apple";
            var responseJson = @"{
                ""quotes"": [
                    {
                        ""symbol"": ""AAPL"",
                        ""longname"": ""Apple Inc."",
                        ""shortname"": ""Apple"",
                        ""exchange"": ""NASDAQ"",
                        ""quoteType"": ""EQUITY""
                    }
                ]
            }";

            SetupHttpClient("YahooFinanceSearch", HttpStatusCode.OK, responseJson);
            var service = CreateService();

            // Act
            var result = await service.SearchSymbolsAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("AAPL", result[0].Symbol);
            Assert.Equal("Apple Inc.", result[0].Name);
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

        // ===== IsHealthyAsync Tests =====

        [Fact]
        public async Task IsHealthyAsync_WhenApiResponds_ReturnsTrue()
        {
            // Arrange
            SetupHttpClient("YahooFinanceChart", HttpStatusCode.OK, "{}");
            var service = CreateService();

            // Act
            var result = await service.IsHealthyAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsHealthyAsync_WhenApiFails_ReturnsFalse()
        {
            // Arrange
            SetupHttpClient("YahooFinanceChart", HttpStatusCode.InternalServerError, "{}");
            var service = CreateService();

            // Act
            var result = await service.IsHealthyAsync();

            // Assert
            Assert.False(result);
        }
    }
}
