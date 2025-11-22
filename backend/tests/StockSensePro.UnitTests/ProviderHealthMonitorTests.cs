using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

public class ProviderHealthMonitorTests : IDisposable
{
    private readonly Mock<IStockDataProviderFactory> _mockFactory;
    private readonly Mock<ILogger<ProviderHealthMonitor>> _mockLogger;
    private readonly Mock<IOptions<DataProviderSettings>> _mockOptions;
    private readonly DataProviderSettings _settings;
    private readonly Mock<IStockDataProvider> _mockYahooProvider;
    private readonly Mock<IStockDataProvider> _mockAlphaVantageProvider;
    private ProviderHealthMonitor? _monitor;

    public ProviderHealthMonitorTests()
    {
        _mockFactory = new Mock<IStockDataProviderFactory>();
        _mockLogger = new Mock<ILogger<ProviderHealthMonitor>>();
        _mockOptions = new Mock<IOptions<DataProviderSettings>>();
        _mockYahooProvider = new Mock<IStockDataProvider>();
        _mockAlphaVantageProvider = new Mock<IStockDataProvider>();

        _settings = new DataProviderSettings
        {
            PrimaryProvider = DataProviderType.YahooFinance,
            FallbackProvider = DataProviderType.AlphaVantage,
            HealthCheckIntervalSeconds = 60
        };

        _mockOptions.Setup(o => o.Value).Returns(_settings);

        // Setup factory to return available providers
        _mockFactory.Setup(f => f.GetAvailableProviders())
            .Returns(new[] { DataProviderType.YahooFinance, DataProviderType.AlphaVantage, DataProviderType.Mock });

        _mockFactory.Setup(f => f.CreateProvider(DataProviderType.YahooFinance))
            .Returns(_mockYahooProvider.Object);

        _mockFactory.Setup(f => f.CreateProvider(DataProviderType.AlphaVantage))
            .Returns(_mockAlphaVantageProvider.Object);
    }

    public void Dispose()
    {
        _monitor?.Dispose();
    }

    [Fact]
    public void Constructor_InitializesHealthStatusForAllProviders()
    {
        // Act
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        var allStatuses = _monitor.GetAllHealthStatuses();

        // Assert
        Assert.Equal(3, allStatuses.Count);
        Assert.True(allStatuses.ContainsKey(DataProviderType.YahooFinance));
        Assert.True(allStatuses.ContainsKey(DataProviderType.AlphaVantage));
        Assert.True(allStatuses.ContainsKey(DataProviderType.Mock));

        // All should start as healthy
        Assert.All(allStatuses.Values, health => Assert.True(health.IsHealthy));
        Assert.All(allStatuses.Values, health => Assert.Equal(0, health.ConsecutiveFailures));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFactoryIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderHealthMonitor(null!, _mockLogger.Object, _mockOptions.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderHealthMonitor(_mockFactory.Object, null!, _mockOptions.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, null!));
    }

    [Fact]
    public async Task CheckHealthAsync_UpdatesHealthStatusToHealthy_WhenProviderIsHealthy()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _mockYahooProvider.Setup(p => p.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.True(health.IsHealthy);
        Assert.Equal(0, health.ConsecutiveFailures);
        Assert.True(health.AverageResponseTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckHealthAsync_UpdatesHealthStatusToUnhealthy_WhenProviderReportsUnhealthy()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _mockYahooProvider.Setup(p => p.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act - Need 3 failures to mark as unhealthy
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.False(health.IsHealthy);
        Assert.Equal(3, health.ConsecutiveFailures);
    }

    [Fact]
    public async Task CheckHealthAsync_HandlesException_AndMarksAsUnhealthy()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _mockYahooProvider.Setup(p => p.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        // Act - Need 3 failures to mark as unhealthy
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.False(health.IsHealthy);
        Assert.Equal(3, health.ConsecutiveFailures);
    }

    [Fact]
    public async Task CheckHealthAsync_UpdatesLastCheckedTimestamp()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _mockYahooProvider.Setup(p => p.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var beforeCheck = DateTime.UtcNow;

        // Act
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance);

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.True(health.LastChecked >= beforeCheck);
        Assert.True(health.LastChecked <= DateTime.UtcNow);
    }

    [Fact]
    public void GetHealthStatus_ReturnsNull_ForUnknownProvider()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        var health = _monitor.GetHealthStatus((DataProviderType)999);

        // Assert
        Assert.Null(health);
    }

    [Fact]
    public void GetAllHealthStatuses_ReturnsAllProviderStatuses()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        var allStatuses = _monitor.GetAllHealthStatuses();

        // Assert
        Assert.Equal(3, allStatuses.Count);
        Assert.True(allStatuses.ContainsKey(DataProviderType.YahooFinance));
        Assert.True(allStatuses.ContainsKey(DataProviderType.AlphaVantage));
        Assert.True(allStatuses.ContainsKey(DataProviderType.Mock));
    }

    [Fact]
    public void RecordSuccess_ResetsConsecutiveFailures()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        
        // First record some failures
        _monitor.RecordFailure(DataProviderType.YahooFinance);
        _monitor.RecordFailure(DataProviderType.YahooFinance);

        // Act
        _monitor.RecordSuccess(DataProviderType.YahooFinance, TimeSpan.FromMilliseconds(100));

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.Equal(0, health.ConsecutiveFailures);
        Assert.True(health.IsHealthy);
    }

    [Fact]
    public void RecordSuccess_UpdatesAverageResponseTime()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.RecordSuccess(DataProviderType.YahooFinance, TimeSpan.FromMilliseconds(100));
        _monitor.RecordSuccess(DataProviderType.YahooFinance, TimeSpan.FromMilliseconds(200));
        _monitor.RecordSuccess(DataProviderType.YahooFinance, TimeSpan.FromMilliseconds(300));

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.Equal(200, health.AverageResponseTime.TotalMilliseconds, 1); // Average of 100, 200, 300
    }

    [Fact]
    public void RecordSuccess_MaintainsRollingWindowOfResponseTimes()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act - Record 105 samples (should keep only last 100)
        for (int i = 1; i <= 105; i++)
        {
            _monitor.RecordSuccess(DataProviderType.YahooFinance, TimeSpan.FromMilliseconds(i));
        }

        // Assert - Average should be based on samples 6-105 (last 100)
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        // Average of 6 to 105 is 55.5
        Assert.Equal(55.5, health.AverageResponseTime.TotalMilliseconds, 1);
    }

    [Fact]
    public void RecordFailure_IncrementsConsecutiveFailures()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.RecordFailure(DataProviderType.YahooFinance);
        _monitor.RecordFailure(DataProviderType.YahooFinance);

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.Equal(2, health.ConsecutiveFailures);
    }

    [Fact]
    public void RecordFailure_MarksUnhealthyAfterThreeConsecutiveFailures()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.RecordFailure(DataProviderType.YahooFinance);
        _monitor.RecordFailure(DataProviderType.YahooFinance);
        
        var healthBefore = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.True(healthBefore!.IsHealthy); // Still healthy after 2 failures

        _monitor.RecordFailure(DataProviderType.YahooFinance);

        // Assert
        var healthAfter = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(healthAfter);
        Assert.False(healthAfter.IsHealthy); // Unhealthy after 3 failures
        Assert.Equal(3, healthAfter.ConsecutiveFailures);
    }

    [Fact]
    public void RecordFailure_LogsWarning_ForUnknownProvider()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.RecordFailure((DataProviderType)999);

        // Assert - Should not throw, just log warning
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unknown provider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordSuccess_LogsWarning_ForUnknownProvider()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.RecordSuccess((DataProviderType)999, TimeSpan.FromMilliseconds(100));

        // Assert - Should not throw, just log warning
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unknown provider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StartPeriodicHealthChecks_StartsHealthCheckTimer()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.StartPeriodicHealthChecks();

        // Assert - Verify log message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Started periodic health checks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StartPeriodicHealthChecks_LogsWarning_WhenAlreadyRunning()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _monitor.StartPeriodicHealthChecks();

        // Act
        _monitor.StartPeriodicHealthChecks();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already running")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StopPeriodicHealthChecks_StopsHealthCheckTimer()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _monitor.StartPeriodicHealthChecks();

        // Act
        _monitor.StopPeriodicHealthChecks();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopped periodic health checks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StopPeriodicHealthChecks_DoesNothing_WhenNotRunning()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act
        _monitor.StopPeriodicHealthChecks();

        // Assert - Should not log anything
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Dispose_StopsPeriodicHealthChecks()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        _monitor.StartPeriodicHealthChecks();

        // Act
        _monitor.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopped periodic health checks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);

        // Act & Assert - Should not throw
        _monitor.Dispose();
        _monitor.Dispose();
    }

    [Fact]
    public void RecordSuccess_IsThreadSafe()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        var tasks = new List<Task>();

        // Act - Record 100 successes concurrently
        for (int i = 0; i < 100; i++)
        {
            var responseTime = TimeSpan.FromMilliseconds(i + 1);
            tasks.Add(Task.Run(() => _monitor.RecordSuccess(DataProviderType.YahooFinance, responseTime)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert - Should have recorded all successes without errors
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.Equal(0, health.ConsecutiveFailures);
        Assert.True(health.IsHealthy);
        Assert.True(health.AverageResponseTime > TimeSpan.Zero);
    }

    [Fact]
    public void RecordFailure_IsThreadSafe()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        var tasks = new List<Task>();

        // Act - Record 10 failures concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _monitor.RecordFailure(DataProviderType.YahooFinance)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.Equal(10, health.ConsecutiveFailures);
        Assert.False(health.IsHealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_RespectsCancellationToken()
    {
        // Arrange
        _monitor = new ProviderHealthMonitor(_mockFactory.Object, _mockLogger.Object, _mockOptions.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockYahooProvider.Setup(p => p.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act - Need 3 failures to mark as unhealthy
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance, cts.Token);
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance, cts.Token);
        await _monitor.CheckHealthAsync(DataProviderType.YahooFinance, cts.Token);

        // Assert - Should handle cancellation gracefully
        var health = _monitor.GetHealthStatus(DataProviderType.YahooFinance);
        Assert.NotNull(health);
        Assert.False(health.IsHealthy);
        Assert.Equal(3, health.ConsecutiveFailures);
    }
}
