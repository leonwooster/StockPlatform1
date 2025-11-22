using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.Core.Enums;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

public class ProviderMetricsTrackerTests
{
    private readonly Mock<ILogger<ProviderMetricsTracker>> _mockLogger;
    private readonly ProviderMetricsTracker _tracker;

    public ProviderMetricsTrackerTests()
    {
        _mockLogger = new Mock<ILogger<ProviderMetricsTracker>>();
        _tracker = new ProviderMetricsTracker(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProviderMetricsTracker(null!));
    }

    [Fact]
    public void RecordSuccess_IncrementsSuccessAndTotalCounts()
    {
        // Act
        _tracker.RecordSuccess(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(1, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(1, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(0, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
    }

    [Fact]
    public void RecordFailure_IncrementsFailureAndTotalCounts()
    {
        // Act
        _tracker.RecordFailure(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(1, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(0, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(1, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
    }

    [Fact]
    public void RecordSuccess_TracksMultipleProvidersSeparately()
    {
        // Act
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(2, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(2, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(1, _tracker.GetTotalRequests(DataProviderType.AlphaVantage));
        Assert.Equal(1, _tracker.GetSuccessfulRequests(DataProviderType.AlphaVantage));
    }

    [Fact]
    public void RecordFailure_TracksMultipleProvidersSeparately()
    {
        // Act
        _tracker.RecordFailure(DataProviderType.YahooFinance);
        _tracker.RecordFailure(DataProviderType.AlphaVantage);
        _tracker.RecordFailure(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(1, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
        Assert.Equal(2, _tracker.GetFailedRequests(DataProviderType.AlphaVantage));
    }

    [Fact]
    public void RecordMixed_TracksSuccessAndFailureSeparately()
    {
        // Act
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordFailure(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(4, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(3, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(1, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
    }

    [Fact]
    public void GetTotalRequests_ReturnsZero_ForUnknownProvider()
    {
        // Act
        var total = _tracker.GetTotalRequests(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void GetSuccessfulRequests_ReturnsZero_ForUnknownProvider()
    {
        // Act
        var success = _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0, success);
    }

    [Fact]
    public void GetFailedRequests_ReturnsZero_ForUnknownProvider()
    {
        // Act
        var failed = _tracker.GetFailedRequests(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0, failed);
    }

    [Fact]
    public void GetAllMetrics_ReturnsEmptyDictionary_WhenNoMetricsRecorded()
    {
        // Act
        var metrics = _tracker.GetAllMetrics();

        // Assert
        Assert.Empty(metrics);
    }

    [Fact]
    public void GetAllMetrics_ReturnsAllProviderMetrics()
    {
        // Arrange
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordFailure(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.AlphaVantage);
        _tracker.RecordFailure(DataProviderType.AlphaVantage);
        _tracker.RecordFailure(DataProviderType.AlphaVantage);

        // Act
        var metrics = _tracker.GetAllMetrics();

        // Assert
        Assert.Equal(2, metrics.Count);
        
        Assert.True(metrics.ContainsKey(DataProviderType.YahooFinance));
        Assert.Equal(3, metrics[DataProviderType.YahooFinance].Total);
        Assert.Equal(2, metrics[DataProviderType.YahooFinance].Success);
        Assert.Equal(1, metrics[DataProviderType.YahooFinance].Failed);

        Assert.True(metrics.ContainsKey(DataProviderType.AlphaVantage));
        Assert.Equal(3, metrics[DataProviderType.AlphaVantage].Total);
        Assert.Equal(1, metrics[DataProviderType.AlphaVantage].Success);
        Assert.Equal(2, metrics[DataProviderType.AlphaVantage].Failed);
    }

    [Fact]
    public void ResetMetrics_ClearsMetricsForSpecificProvider()
    {
        // Arrange
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.AlphaVantage);

        // Act
        _tracker.ResetMetrics(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(1, _tracker.GetTotalRequests(DataProviderType.AlphaVantage));
    }

    [Fact]
    public void ResetMetrics_DoesNotThrow_ForUnknownProvider()
    {
        // Act & Assert - Should not throw
        _tracker.ResetMetrics(DataProviderType.YahooFinance);
    }

    [Fact]
    public void ResetAllMetrics_ClearsAllProviderMetrics()
    {
        // Arrange
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        _tracker.RecordSuccess(DataProviderType.AlphaVantage);
        _tracker.RecordFailure(DataProviderType.Mock);

        // Act
        _tracker.ResetAllMetrics();

        // Assert
        Assert.Equal(0, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(0, _tracker.GetTotalRequests(DataProviderType.AlphaVantage));
        Assert.Equal(0, _tracker.GetTotalRequests(DataProviderType.Mock));
        Assert.Empty(_tracker.GetAllMetrics());
    }

    [Fact]
    public void RecordSuccess_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentCalls = 1000;

        // Act - Record successes concurrently
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordSuccess(DataProviderType.YahooFinance)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(concurrentCalls, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(concurrentCalls, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(0, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
    }

    [Fact]
    public void RecordFailure_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentCalls = 1000;

        // Act - Record failures concurrently
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordFailure(DataProviderType.YahooFinance)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(concurrentCalls, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(0, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(concurrentCalls, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
    }

    [Fact]
    public void RecordMixed_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int successCalls = 500;
        const int failureCalls = 300;

        // Act - Record successes and failures concurrently
        for (int i = 0; i < successCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordSuccess(DataProviderType.YahooFinance)));
        }
        for (int i = 0; i < failureCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordFailure(DataProviderType.YahooFinance)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(successCalls + failureCalls, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(successCalls, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(failureCalls, _tracker.GetFailedRequests(DataProviderType.YahooFinance));
    }

    [Fact]
    public void MultipleProviders_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int callsPerProvider = 100;

        // Act - Record metrics for multiple providers concurrently
        for (int i = 0; i < callsPerProvider; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordSuccess(DataProviderType.YahooFinance)));
            tasks.Add(Task.Run(() => _tracker.RecordFailure(DataProviderType.YahooFinance)));
            tasks.Add(Task.Run(() => _tracker.RecordSuccess(DataProviderType.AlphaVantage)));
            tasks.Add(Task.Run(() => _tracker.RecordFailure(DataProviderType.Mock)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(callsPerProvider * 2, _tracker.GetTotalRequests(DataProviderType.YahooFinance));
        Assert.Equal(callsPerProvider, _tracker.GetSuccessfulRequests(DataProviderType.YahooFinance));
        Assert.Equal(callsPerProvider, _tracker.GetFailedRequests(DataProviderType.YahooFinance));

        Assert.Equal(callsPerProvider, _tracker.GetTotalRequests(DataProviderType.AlphaVantage));
        Assert.Equal(callsPerProvider, _tracker.GetSuccessfulRequests(DataProviderType.AlphaVantage));

        Assert.Equal(callsPerProvider, _tracker.GetTotalRequests(DataProviderType.Mock));
        Assert.Equal(callsPerProvider, _tracker.GetFailedRequests(DataProviderType.Mock));
    }

    [Fact]
    public void GetAllMetrics_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int recordCalls = 100;
        const int readCalls = 50;

        // Act - Record and read metrics concurrently
        for (int i = 0; i < recordCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordSuccess(DataProviderType.YahooFinance)));
        }
        for (int i = 0; i < readCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.GetAllMetrics()));
        }
        
        // Assert - Should not throw
        Task.WaitAll(tasks.ToArray());
        
        var finalMetrics = _tracker.GetAllMetrics();
        Assert.Equal(recordCalls, finalMetrics[DataProviderType.YahooFinance].Total);
    }

    [Fact]
    public void ResetMetrics_IsThreadSafe()
    {
        // Arrange
        _tracker.RecordSuccess(DataProviderType.YahooFinance);
        var tasks = new List<Task>();

        // Act - Reset and record concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _tracker.ResetMetrics(DataProviderType.YahooFinance)));
            tasks.Add(Task.Run(() => _tracker.RecordSuccess(DataProviderType.YahooFinance)));
        }

        // Assert - Should not throw
        Task.WaitAll(tasks.ToArray());
    }

    [Fact]
    public void RecordSuccess_LogsDebugMessage()
    {
        // Act
        _tracker.RecordSuccess(DataProviderType.YahooFinance);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded success")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordFailure_LogsDebugMessage()
    {
        // Act
        _tracker.RecordFailure(DataProviderType.YahooFinance);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded failure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ResetMetrics_LogsInformationMessage()
    {
        // Arrange
        _tracker.RecordSuccess(DataProviderType.YahooFinance);

        // Act
        _tracker.ResetMetrics(DataProviderType.YahooFinance);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reset metrics for provider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ResetAllMetrics_LogsInformationMessage()
    {
        // Arrange
        _tracker.RecordSuccess(DataProviderType.YahooFinance);

        // Act
        _tracker.ResetAllMetrics();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reset metrics for all providers")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
