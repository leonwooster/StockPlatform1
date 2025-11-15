using Moq;
using StackExchange.Redis;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class RedisCacheServiceTests
    {
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly RedisCacheService _cacheService;

        public RedisCacheServiceTests()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_mockDatabase.Object);
            
            _cacheService = new RedisCacheService(_mockRedis.Object);
        }

        // ===== GetAsync Tests =====

        [Fact]
        public async Task GetAsync_WhenKeyExists_ReturnsDeserializedValue()
        {
            // Arrange
            var key = "test:key";
            var expectedValue = new TestData { Id = 1, Name = "Test" };
            var json = System.Text.Json.JsonSerializer.Serialize(expectedValue);
            
            _mockDatabase.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)json);

            // Act
            var result = await _cacheService.GetAsync<TestData>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedValue.Id, result.Id);
            Assert.Equal(expectedValue.Name, result.Name);
        }

        [Fact]
        public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
        {
            // Arrange
            var key = "nonexistent:key";
            
            _mockDatabase.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            // Act
            var result = await _cacheService.GetAsync<TestData>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WithEmptyValue_ReturnsDefault()
        {
            // Arrange
            var key = "empty:key";
            
            _mockDatabase.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.EmptyString);

            // Act
            var result = await _cacheService.GetAsync<TestData>(key);

            // Assert
            Assert.Null(result);
        }

        // ===== SetAsync Tests =====

        [Fact]
        public async Task SetAsync_WithValue_SerializesAndStores()
        {
            // Arrange
            var key = "test:key";
            var value = new TestData { Id = 1, Name = "Test" };
            var expiry = TimeSpan.FromMinutes(15);

            // Act
            await _cacheService.SetAsync(key, value, expiry);

            // Assert
            _mockDatabase.Verify(db => db.StringSetAsync(
                key,
                It.Is<RedisValue>(v => v.ToString().Contains("Test")),
                expiry,
                false,
                When.Always,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithoutExpiry_StoresWithoutTTL()
        {
            // Arrange
            var key = "test:key";
            var value = new TestData { Id = 1, Name = "Test" };

            // Act
            await _cacheService.SetAsync(key, value);

            // Assert
            _mockDatabase.Verify(db => db.StringSetAsync(
                key,
                It.IsAny<RedisValue>(),
                null,
                false,
                When.Always,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithNullValue_StoresNull()
        {
            // Arrange
            var key = "test:key";
            TestData? value = null;

            // Act
            await _cacheService.SetAsync(key, value);

            // Assert
            _mockDatabase.Verify(db => db.StringSetAsync(
                key,
                It.IsAny<RedisValue>(),
                null,
                false,
                When.Always,
                CommandFlags.None), Times.Once);
        }

        // ===== RemoveAsync Tests =====

        [Fact]
        public async Task RemoveAsync_CallsKeyDelete()
        {
            // Arrange
            var key = "test:key";

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            _mockDatabase.Verify(db => db.KeyDeleteAsync(
                key,
                It.IsAny<CommandFlags>()), Times.Once);
        }

        // ===== ExistsAsync Tests =====

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
        {
            // Arrange
            var key = "test:key";
            
            _mockDatabase.Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var key = "nonexistent:key";
            
            _mockDatabase.Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.False(result);
        }

        // ===== Integration Scenarios =====

        [Fact]
        public async Task CacheWorkflow_SetGetRemove_WorksCorrectly()
        {
            // Arrange
            var key = "workflow:key";
            var value = new TestData { Id = 42, Name = "Workflow Test" };
            var json = System.Text.Json.JsonSerializer.Serialize(value);

            _mockDatabase.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)json);
            _mockDatabase.Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act & Assert - Set
            await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
            _mockDatabase.Verify(db => db.StringSetAsync(
                key,
                It.IsAny<RedisValue>(),
                TimeSpan.FromMinutes(5),
                false,
                When.Always,
                CommandFlags.None), Times.Once);

            // Act & Assert - Exists
            var exists = await _cacheService.ExistsAsync(key);
            Assert.True(exists);

            // Act & Assert - Get
            var retrieved = await _cacheService.GetAsync<TestData>(key);
            Assert.NotNull(retrieved);
            Assert.Equal(value.Id, retrieved.Id);

            // Act & Assert - Remove
            await _cacheService.RemoveAsync(key);
            _mockDatabase.Verify(db => db.KeyDeleteAsync(
                key,
                It.IsAny<CommandFlags>()), Times.Once);
        }

        // Test helper class
        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
