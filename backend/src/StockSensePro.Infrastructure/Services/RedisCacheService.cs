using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var value = await _database.StringGetAsync(key);
                stopwatch.Stop();

                if (value.IsNullOrEmpty)
                {
                    _logger.LogInformation(
                        "Cache MISS: Key={Key}, ResponseTime={ResponseTimeMs}ms",
                        key,
                        stopwatch.ElapsedMilliseconds);
                    return default(T);
                }

                var result = JsonSerializer.Deserialize<T>(value!);

                _logger.LogInformation(
                    "Cache HIT: Key={Key}, DataSize={DataSizeBytes}bytes, ResponseTime={ResponseTimeMs}ms",
                    key,
                    value.Length(),
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Cache Error: Operation=Get, Key={Key}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    key,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var json = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, json, expiry);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Cache SET: Key={Key}, DataSize={DataSizeBytes}bytes, TTL={TTLSeconds}s, ResponseTime={ResponseTimeMs}ms",
                    key,
                    json.Length,
                    expiry?.TotalSeconds ?? -1,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Cache Error: Operation=Set, Key={Key}, TTL={TTLSeconds}s, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    key,
                    expiry?.TotalSeconds ?? -1,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var deleted = await _database.KeyDeleteAsync(key);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Cache REMOVE: Key={Key}, Deleted={Deleted}, ResponseTime={ResponseTimeMs}ms",
                    key,
                    deleted,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Cache Error: Operation=Remove, Key={Key}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    key,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var exists = await _database.KeyExistsAsync(key);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Cache EXISTS: Key={Key}, Exists={Exists}, ResponseTime={ResponseTimeMs}ms",
                    key,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Cache Error: Operation=Exists, Key={Key}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    key,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                return false;
            }
        }
    }
}
