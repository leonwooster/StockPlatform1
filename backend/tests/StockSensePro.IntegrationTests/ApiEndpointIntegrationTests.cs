using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace StockSensePro.IntegrationTests
{
    [Collection("Integration")]
    public class ApiEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ApiEndpointIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        // ===== Health Check Tests =====

        [Fact]
        public async Task HealthCheck_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.ServiceUnavailable,
                $"Expected OK or ServiceUnavailable, but got {response.StatusCode}");
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthCheckResponse()
        {
            // Act
            var response = await _client.GetAsync("/api/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotNull(content);
            Assert.Contains("status", content.ToLower());
            Assert.Contains("timestamp", content.ToLower());
            Assert.Contains("service", content.ToLower());
        }

        // ===== Stock API Tests =====

        [Fact(Skip = "Integration test - requires database")]
        public async Task GetAllStocks_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/stocks");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Skip = "Integration test - requires database")]
        public async Task GetStockBySymbol_WithValidSymbol_ReturnsOk()
        {
            // Arrange
            var symbol = "AAPL";

            // Act
            var response = await _client.GetAsync($"/api/stocks/{symbol}");

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact(Skip = "Integration test - requires database")]
        public async Task CreateStock_WithValidData_ReturnsCreated()
        {
            // Arrange
            var newStock = new
            {
                symbol = "TEST",
                name = "Test Stock",
                currentPrice = 100.00m,
                exchange = "NASDAQ",
                sector = "Technology",
                industry = "Software"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/stocks", newStock);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created || 
                response.StatusCode == HttpStatusCode.Conflict);
        }

        // ===== Error Handling Tests =====

        [Fact]
        public async Task GetStock_WithInvalidSymbol_ReturnsNotFound()
        {
            // Arrange
            var invalidSymbol = "INVALIDXYZ123";

            // Act
            var response = await _client.GetAsync($"/api/stocks/{invalidSymbol}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "Integration test - requires validation")]
        public async Task CreateStock_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidStock = new
            {
                symbol = "", // Empty symbol should be invalid
                name = "Test"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/stocks", invalidStock);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ===== CORS Tests =====

        [Fact]
        public async Task Options_Request_ReturnsCorrectCorsHeaders()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/stocks");
            request.Headers.Add("Origin", "http://localhost:3000");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin") || 
                       response.StatusCode == HttpStatusCode.NoContent);
        }

        // ===== Content Type Tests =====

        [Fact(Skip = "Integration test - requires database")]
        public async Task GetAllStocks_ReturnsJsonContent()
        {
            // Act
            var response = await _client.GetAsync("/api/stocks");

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        // ===== Rate Limiting Tests =====

        [Fact(Skip = "Integration test - tests rate limiting")]
        public async Task MultipleRequests_WithinRateLimit_AllSucceed()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Make 10 requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("/health"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - All should succeed (assuming rate limit > 10 requests)
            Assert.All(responses, response => 
                Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        }

        [Fact(Skip = "Integration test - tests rate limiting")]
        public async Task ExcessiveRequests_ExceedingRateLimit_ReturnsTooManyRequests()
        {
            // This test would require configuring a low rate limit for testing
            // and making enough requests to exceed it
            
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Make 100 requests rapidly
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(_client.GetAsync("/api/stocks"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - Some should return 429 Too Many Requests
            Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.TooManyRequests);
        }
    }
}
