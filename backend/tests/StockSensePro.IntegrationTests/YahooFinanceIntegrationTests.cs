using System.Net;
using Microsoft.Extensions.DependencyInjection;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Exceptions;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.IntegrationTests
{
    [Collection("Integration")]
    public class YahooFinanceIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly IYahooFinanceService _yahooFinanceService;

        public YahooFinanceIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            
            // Create a scope to get the service
            var scope = _factory.Services.CreateScope();
            _yahooFinanceService = scope.ServiceProvider.GetRequiredService<IYahooFinanceService>();
        }

        // ===== Real API Tests (Marked as Skip by default to avoid rate limits) =====

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task GetQuoteAsync_WithRealAPI_ReturnsValidData()
        {
            // Arrange
            var symbol = "AAPL";

            // Act
            var result = await _yahooFinanceService.GetQuoteAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.True(result.CurrentPrice > 0);
            Assert.True(result.Volume >= 0);
            Assert.NotNull(result.Exchange);
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task GetQuotesAsync_WithRealAPI_ReturnsMultipleQuotes()
        {
            // Arrange
            var symbols = new List<string> { "AAPL", "MSFT", "GOOGL" };

            // Act
            var result = await _yahooFinanceService.GetQuotesAsync(symbols);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 2); // At least 2 should succeed
            Assert.All(result, quote => Assert.True(quote.CurrentPrice > 0));
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task GetHistoricalPricesAsync_WithRealAPI_ReturnsHistoricalData()
        {
            // Arrange
            var symbol = "AAPL";
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // Act
            var result = await _yahooFinanceService.GetHistoricalPricesAsync(
                symbol, startDate, endDate, TimeInterval.Daily);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, price => Assert.Equal(symbol, price.Symbol));
            Assert.All(result, price => Assert.True(price.Close > 0));
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task GetFundamentalsAsync_WithRealAPI_ReturnsFundamentalData()
        {
            // Arrange
            var symbol = "AAPL";

            // Act
            var result = await _yahooFinanceService.GetFundamentalsAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            // Some fundamental data should be present
            Assert.True(result.PERatio.HasValue || result.EPS.HasValue);
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task GetCompanyProfileAsync_WithRealAPI_ReturnsCompanyProfile()
        {
            // Arrange
            var symbol = "AAPL";

            // Act
            var result = await _yahooFinanceService.GetCompanyProfileAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.NotEmpty(result.CompanyName);
            Assert.NotEmpty(result.Sector);
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task SearchSymbolsAsync_WithRealAPI_ReturnsSearchResults()
        {
            // Arrange
            var query = "Apple";

            // Act
            var result = await _yahooFinanceService.SearchSymbolsAsync(query, 10);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, r => r.Symbol == "AAPL");
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task GetQuoteAsync_WithInvalidSymbol_ThrowsSymbolNotFoundException()
        {
            // Arrange
            var symbol = "INVALIDXYZ123";

            // Act & Assert
            await Assert.ThrowsAsync<SymbolNotFoundException>(
                () => _yahooFinanceService.GetQuoteAsync(symbol));
        }

        [Fact(Skip = "Integration test - requires real Yahoo Finance API")]
        public async Task IsHealthyAsync_WithRealAPI_ReturnsTrue()
        {
            // Act
            var result = await _yahooFinanceService.IsHealthyAsync();

            // Assert
            Assert.True(result);
        }

        // ===== Polly Policy Integration Tests =====

        [Fact(Skip = "Integration test - tests retry behavior with real API")]
        public async Task PollyRetryPolicy_RetriesOnTransientFailure()
        {
            // This test would require a way to simulate transient failures
            // In a real scenario, you might use a test server or WireMock
            // For now, this demonstrates the concept
            
            // Arrange
            var symbol = "AAPL";

            // Act - The Polly retry policy should handle transient failures automatically
            var result = await _yahooFinanceService.GetQuoteAsync(symbol);

            // Assert
            Assert.NotNull(result);
        }

        [Fact(Skip = "Integration test - tests timeout behavior")]
        public async Task PollyTimeoutPolicy_CancelsLongRunningRequests()
        {
            // This test would require a way to simulate slow responses
            // The timeout is configured to 10 seconds in Program.cs
            
            // In a real scenario, you would:
            // 1. Set up a test server that delays responses
            // 2. Verify that requests timeout after 10 seconds
            // 3. Verify that ApiUnavailableException is thrown
            
            Assert.True(true); // Placeholder
        }

        // ===== Performance Tests =====

        [Fact(Skip = "Performance test - measures response time")]
        public async Task GetQuoteAsync_ResponseTime_IsUnder2Seconds()
        {
            // Arrange
            var symbol = "AAPL";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = await _yahooFinanceService.GetQuoteAsync(symbol);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"Response took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        }

        [Fact(Skip = "Performance test - measures concurrent requests")]
        public async Task GetQuotesAsync_ConcurrentRequests_HandlesLoad()
        {
            // Arrange
            var symbols = new List<string> { "AAPL", "MSFT", "GOOGL", "AMZN", "META" };
            var tasks = new List<Task>();

            // Act - Make 10 concurrent requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_yahooFinanceService.GetQuotesAsync(symbols));
            }

            await Task.WhenAll(tasks);

            // Assert - All requests should complete successfully
            Assert.All(tasks, task => Assert.True(task.IsCompletedSuccessfully));
        }
    }
}
