using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StockSensePro.Core.Configuration;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

public class AlphaVantageRateLimiterTests : IDisposable
{
    private readonly Mock<ILogger<AlphaVantageRateLimiter>> _mockLogger;
    private readonly AlphaVantageSettings _settings;
    private readonly Mock<IOptions<AlphaVantageSettings>> _mockOptions;

    public AlphaVantageRateLimiterTests()
    {
        _mockLogger = new Mock<ILogger<AlphaVantageRateLimiter>>();
        _settings = new AlphaVantageSettings
        {
            RateLimit = new RateLimitSettings
            {
                RequestsPerMinute = 5,
                RequestsPerDay = 25
            }
        };
        _mockOptions = new Mock<IOptions<AlphaVantageSettings>>();
        _mockOptions.Setup(o => o.Value).Returns(_settings);
    }

    public void Dispose()
    {
        // Cleanup is handled by individual test disposal
    }

    [Fact]
    public void Constructor_InitializesWithCorrectLimits()
    {
        // Arrange & Act
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);
        var status = rateLimiter.GetStatus();

        // Assert
        Assert.Equal(5, status.MinuteRequestsLimit);
        Assert.Equal(25, status.DayRequestsLimit);
        Assert.Equal(5, status.MinuteRequestsRemaining);
        Assert.Equal(25, status.DayRequestsRemaining);
    }

    [Fact]
    public async Task TryAcquireAsync_SucceedsWhenTokensAvailable()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act
        var result = await rateLimiter.TryAcquireAsync();

        // Assert
        Assert.True(result);
        var status = rateLimiter.GetStatus();
        Assert.Equal(4, status.MinuteRequestsRemaining);
        Assert.Equal(24, status.DayRequestsRemaining);
    }

    [Fact]
    public async Task TryAcquireAsync_FailsWhenMinuteTokensExhausted()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act - Exhaust minute tokens
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync();
        }
        var result = await rateLimiter.TryAcquireAsync();

        // Assert
        Assert.False(result);
        var status = rateLimiter.GetStatus();
        Assert.Equal(0, status.MinuteRequestsRemaining);
    }

    [Fact]
    public async Task TryAcquireAsync_FailsWhenDayTokensExhausted()
    {
        // Arrange - Use settings where minute limit is higher than day limit
        var settings = new AlphaVantageSettings
        {
            RateLimit = new RateLimitSettings
            {
                RequestsPerMinute = 100,
                RequestsPerDay = 5
            }
        };
        var options = new Mock<IOptions<AlphaVantageSettings>>();
        options.Setup(o => o.Value).Returns(settings);
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, options.Object);

        // Act - Exhaust day tokens
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync();
        }
        var result = await rateLimiter.TryAcquireAsync();

        // Assert
        Assert.False(result);
        var status = rateLimiter.GetStatus();
        Assert.Equal(0, status.DayRequestsRemaining);
    }

    [Fact]
    public async Task TryAcquireAsync_DecrementsMinuteAndDayTokens()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();

        // Assert
        var status = rateLimiter.GetStatus();
        Assert.Equal(2, status.MinuteRequestsRemaining);
        Assert.Equal(22, status.DayRequestsRemaining);
    }

    [Fact]
    public async Task GetStatus_ReturnsCorrectRateLimitStatus()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);
        await rateLimiter.TryAcquireAsync();

        // Act
        var status = rateLimiter.GetStatus();

        // Assert
        Assert.NotNull(status);
        Assert.Equal(4, status.MinuteRequestsRemaining);
        Assert.Equal(24, status.DayRequestsRemaining);
        Assert.Equal(5, status.MinuteRequestsLimit);
        Assert.Equal(25, status.DayRequestsLimit);
        Assert.False(status.IsRateLimited);
    }

    [Fact]
    public async Task GetStatus_ShowsRateLimitedWhenMinuteTokensExhausted()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act - Exhaust minute tokens
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync();
        }
        var status = rateLimiter.GetStatus();

        // Assert
        Assert.True(status.IsRateLimited);
        Assert.Equal(0, status.MinuteRequestsRemaining);
    }

    [Fact]
    public async Task GetStatus_ShowsRateLimitedWhenDayTokensExhausted()
    {
        // Arrange - Use settings where minute limit is higher than day limit
        var settings = new AlphaVantageSettings
        {
            RateLimit = new RateLimitSettings
            {
                RequestsPerMinute = 100,
                RequestsPerDay = 5
            }
        };
        var options = new Mock<IOptions<AlphaVantageSettings>>();
        options.Setup(o => o.Value).Returns(settings);
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, options.Object);

        // Act - Exhaust day tokens
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync();
        }
        var status = rateLimiter.GetStatus();

        // Assert
        Assert.True(status.IsRateLimited);
        Assert.Equal(0, status.DayRequestsRemaining);
    }

    [Fact]
    public void GetStatus_ReturnsResetTimeInformation()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act
        var status = rateLimiter.GetStatus();

        // Assert
        Assert.True(status.MinuteWindowResetIn.TotalSeconds > 0);
        Assert.True(status.MinuteWindowResetIn.TotalSeconds <= 60);
        Assert.True(status.DayWindowResetIn.TotalHours > 0);
        Assert.True(status.DayWindowResetIn.TotalHours <= 24);
    }

    [Fact]
    public async Task TryAcquireAsync_IsThreadSafe()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);
        var tasks = new List<Task<bool>>();

        // Act - Try to acquire 10 tokens concurrently (should only succeed 5 times for minute limit)
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () => await rateLimiter.TryAcquireAsync()));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r);
        Assert.Equal(5, successCount); // Only 5 should succeed due to minute limit
        var status = rateLimiter.GetStatus();
        Assert.Equal(0, status.MinuteRequestsRemaining);
    }

    [Fact]
    public async Task WaitForAvailabilityAsync_WaitsForTokens()
    {
        // Arrange
        var settings = new AlphaVantageSettings
        {
            RateLimit = new RateLimitSettings
            {
                RequestsPerMinute = 2,
                RequestsPerDay = 10
            }
        };
        var options = new Mock<IOptions<AlphaVantageSettings>>();
        options.Setup(o => o.Value).Returns(settings);
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, options.Object);

        // Exhaust tokens
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();

        // Act & Assert - This should wait for the minute window to reset
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await rateLimiter.WaitForAvailabilityAsync(cts.Token));
    }

    [Fact]
    public async Task WaitForAvailabilityAsync_SucceedsWhenTokensAvailable()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act
        await rateLimiter.WaitForAvailabilityAsync();

        // Assert
        var status = rateLimiter.GetStatus();
        Assert.Equal(4, status.MinuteRequestsRemaining);
        Assert.Equal(24, status.DayRequestsRemaining);
    }

    [Fact]
    public async Task WaitForAvailabilityAsync_RespectsCancellationToken()
    {
        // Arrange
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Exhaust tokens
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync();
        }

        // Act & Assert
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await rateLimiter.WaitForAvailabilityAsync(cts.Token));
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        // Arrange
        var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, _mockOptions.Object);

        // Act
        rateLimiter.Dispose();

        // Assert - Should not throw
        rateLimiter.Dispose(); // Double dispose should be safe
    }

    [Fact]
    public async Task TryAcquireAsync_ReturnsMinuteTokenWhenDayLimitReached()
    {
        // Arrange - Use settings where minute limit is higher than day limit
        var settings = new AlphaVantageSettings
        {
            RateLimit = new RateLimitSettings
            {
                RequestsPerMinute = 100,
                RequestsPerDay = 5
            }
        };
        var options = new Mock<IOptions<AlphaVantageSettings>>();
        options.Setup(o => o.Value).Returns(settings);
        using var rateLimiter = new AlphaVantageRateLimiter(_mockLogger.Object, options.Object);

        // Exhaust day tokens
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync();
        }

        var statusBefore = rateLimiter.GetStatus();
        var minuteTokensBefore = statusBefore.MinuteRequestsRemaining;

        // Act - Try to acquire when day limit is reached
        var result = await rateLimiter.TryAcquireAsync();

        // Assert - Should fail and minute token should be returned
        Assert.False(result);
        var statusAfter = rateLimiter.GetStatus();
        Assert.Equal(minuteTokensBefore, statusAfter.MinuteRequestsRemaining);
    }
}
