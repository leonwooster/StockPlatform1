using Microsoft.Extensions.DependencyInjection;
using StockSensePro.Core.Interfaces;
using Xunit;

namespace StockSensePro.IntegrationTests
{
    [Collection("Integration")]
    public class CacheIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly ICacheService _cacheService;

        public CacheIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            
            var scope = _factory.Services.CreateScope();
            _cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        }

        [Fact]
        public async Task SetAndGet_WithSimpleValue_WorksCorrectly()
        {
            // Arrange
            var key = $"test:integration:{Guid.NewGuid()}";
            var value = new TestData { Id = 1, Name = "Integration Test" };

            try
            {
                // Act - Set
                await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(1));

                // Act - Get
                var result = await _cacheService.GetAsync<TestData>(key);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(value.Id, result.Id);
                Assert.Equal(value.Name, result.Name);
            }
            finally
            {
                // Cleanup
                await _cacheService.RemoveAsync(key);
            }
        }

        [Fact]
        public async Task Exists_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var key = $"test:exists:{Guid.NewGuid()}";
            var value = "test value";

            try
            {
                await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(1));

                // Act
                var exists = await _cacheService.ExistsAsync(key);

                // Assert
                Assert.True(exists);
            }
            finally
            {
                await _cacheService.RemoveAsync(key);
            }
        }

        [Fact]
        public async Task Exists_WithNonExistentKey_ReturnsFalse()
        {
            // Arrange
            var key = $"test:nonexistent:{Guid.NewGuid()}";

            // Act
            var exists = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task Remove_DeletesKey()
        {
            // Arrange
            var key = $"test:remove:{Guid.NewGuid()}";
            var value = "test value";
            await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(1));

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            var exists = await _cacheService.ExistsAsync(key);
            Assert.False(exists);
        }

        [Fact(Skip = "Integration test - requires Redis and time")]
        public async Task TTL_ExpiresAfterSpecifiedTime()
        {
            // Arrange
            var key = $"test:ttl:{Guid.NewGuid()}";
            var value = "test value";
            var ttl = TimeSpan.FromSeconds(2);

            try
            {
                // Act
                await _cacheService.SetAsync(key, value, ttl);
                
                // Verify it exists immediately
                var existsBefore = await _cacheService.ExistsAsync(key);
                Assert.True(existsBefore);

                // Wait for expiration
                await Task.Delay(TimeSpan.FromSeconds(3));

                // Verify it's gone
                var existsAfter = await _cacheService.ExistsAsync(key);
                Assert.False(existsAfter);
            }
            finally
            {
                await _cacheService.RemoveAsync(key);
            }
        }

        [Fact]
        public async Task CacheWorkflow_SetExistsGetRemove_WorksEndToEnd()
        {
            // Arrange
            var key = $"test:workflow:{Guid.NewGuid()}";
            var value = new TestData { Id = 42, Name = "Workflow Test" };

            try
            {
                // Act & Assert - Set
                await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

                // Act & Assert - Exists
                var exists = await _cacheService.ExistsAsync(key);
                Assert.True(exists);

                // Act & Assert - Get
                var retrieved = await _cacheService.GetAsync<TestData>(key);
                Assert.NotNull(retrieved);
                Assert.Equal(value.Id, retrieved.Id);
                Assert.Equal(value.Name, retrieved.Name);

                // Act & Assert - Remove
                await _cacheService.RemoveAsync(key);
                var existsAfterRemove = await _cacheService.ExistsAsync(key);
                Assert.False(existsAfterRemove);
            }
            finally
            {
                await _cacheService.RemoveAsync(key);
            }
        }

        [Fact]
        public async Task ConcurrentAccess_HandlesMultipleOperations()
        {
            // Arrange
            var keys = Enumerable.Range(1, 10)
                .Select(i => $"test:concurrent:{Guid.NewGuid()}")
                .ToList();

            try
            {
                // Act - Set multiple keys concurrently
                var setTasks = keys.Select(key => 
                    _cacheService.SetAsync(key, $"value-{key}", TimeSpan.FromMinutes(1)));
                await Task.WhenAll(setTasks);

                // Act - Get multiple keys concurrently
                var getTasks = keys.Select(key => 
                    _cacheService.GetAsync<string>(key));
                var results = await Task.WhenAll(getTasks);

                // Assert
                Assert.All(results, result => Assert.NotNull(result));
                Assert.Equal(10, results.Length);
            }
            finally
            {
                // Cleanup
                var removeTasks = keys.Select(key => _cacheService.RemoveAsync(key));
                await Task.WhenAll(removeTasks);
            }
        }

        // Test helper class
        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
