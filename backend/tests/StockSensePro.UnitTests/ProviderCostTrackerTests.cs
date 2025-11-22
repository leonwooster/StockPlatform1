using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

public class ProviderCostTrackerTests
{
    private readonly Mock<IProviderCostCalculator> _mockCalculator;
    private readonly Mock<ILogger<ProviderCostTracker>> _mockLogger;
    private readonly ProviderCostSettings _settings;
    private readonly ProviderCostTracker _tracker;

    public ProviderCostTrackerTests()
    {
        _mockCalculator = new Mock<IProviderCostCalculator>();
        _mockLogger = new Mock<ILogger<ProviderCostTracker>>();
        _settings = new ProviderCostSettings
        {
            Enabled = true,
            EnforceLimits = false,
            WarningThresholdPercentage = 80.0,
            Providers = new Dictionary<string, ProviderCostConfig>
            {
                { "YahooFinance", new ProviderCostConfig { CostPerCall = 0.0m, MonthlySubscription = 0.0m, CostThreshold = 0.0m } },
                { "AlphaVantage", new ProviderCostConfig { CostPerCall = 0.002m, MonthlySubscription = 0.0m, CostThreshold = 100.0m } },
                { "Mock", new ProviderCostConfig { CostPerCall = 0.0m, MonthlySubscription = 0.0m, CostThreshold = 0.0m } }
            }
        };

        // Setup default mock behavior
        _mockCalculator.Setup(x => x.GetCostPerCall(It.IsAny<DataProviderType>())).Returns(0.002m);
        _mockCalculator.Setup(x => x.CalculateCost(It.IsAny<DataProviderType>(), It.IsAny<long>()))
            .Returns<DataProviderType, long>((provider, calls) => calls * 0.002m);
        _mockCalculator.Setup(x => x.GetMonthlySubscriptionCost(It.IsAny<DataProviderType>())).Returns(0.0m);
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(It.IsAny<DataProviderType>(), It.IsAny<long>()))
            .Returns<DataProviderType, long>((provider, calls) => calls * 0.002m);

        _tracker = new ProviderCostTracker(_mockCalculator.Object, _mockLogger.Object, Options.Create(_settings));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCalculatorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderCostTracker(null!, _mockLogger.Object, Options.Create(_settings)));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderCostTracker(_mockCalculator.Object, null!, Options.Create(_settings)));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderCostTracker(_mockCalculator.Object, _mockLogger.Object, null!));
    }

    [Fact]
    public void RecordApiCall_IncrementsCallCount()
    {
        // Act
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Assert
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        Assert.Equal(1, metrics.TotalApiCalls);
    }

    [Fact]
    public void RecordApiCall_IncrementsCallCount_ForMultipleCalls()
    {
        // Act
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Assert
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        Assert.Equal(3, metrics.TotalApiCalls);
    }

    [Fact]
    public void RecordApiCall_TracksMultipleProvidersSeparately()
    {
        // Act
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.YahooFinance);

        // Assert
        var alphaMetrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        var yahooMetrics = _tracker.GetCostMetrics(DataProviderType.YahooFinance);
        
        Assert.Equal(2, alphaMetrics.TotalApiCalls);
        Assert.Equal(1, yahooMetrics.TotalApiCalls);
    }

    [Fact]
    public void RecordApiCall_DoesNothing_WhenCostTrackingDisabled()
    {
        // Arrange
        var disabledSettings = new ProviderCostSettings { Enabled = false };
        var tracker = new ProviderCostTracker(_mockCalculator.Object, _mockLogger.Object, Options.Create(disabledSettings));

        // Act
        tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Assert
        var metrics = tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        Assert.Equal(0, metrics.TotalApiCalls);
    }

    [Fact]
    public void GetCostMetrics_ReturnsCorrectMetrics()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Act
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(2, metrics.TotalApiCalls);
        Assert.Equal(0.004m, metrics.EstimatedCost);
        Assert.Equal(0.0m, metrics.MonthlySubscriptionCost);
        Assert.Equal(0.004m, metrics.TotalEstimatedCost);
        Assert.Equal(0.002m, metrics.CostPerCall);
        Assert.Equal(100.0m, metrics.CostThreshold);
    }

    [Fact]
    public void GetCostMetrics_CalculatesThresholdPercentage()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 40))
            .Returns(80.0m);

        for (int i = 0; i < 40; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Act
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(80.0, metrics.ThresholdPercentage);
        Assert.False(metrics.IsThresholdExceeded);
    }

    [Fact]
    public void GetCostMetrics_SetsThresholdExceeded_WhenCostExceedsThreshold()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 60))
            .Returns(120.0m);

        for (int i = 0; i < 60; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Act
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(120.0, metrics.ThresholdPercentage);
        Assert.True(metrics.IsThresholdExceeded);
    }

    [Fact]
    public void GetAllCostMetrics_ReturnsMetricsForAllProviders()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.YahooFinance);

        // Act
        var allMetrics = _tracker.GetAllCostMetrics();

        // Assert
        Assert.Equal(3, allMetrics.Count); // All provider types
        Assert.True(allMetrics.ContainsKey(DataProviderType.AlphaVantage));
        Assert.True(allMetrics.ContainsKey(DataProviderType.YahooFinance));
        Assert.True(allMetrics.ContainsKey(DataProviderType.Mock));
    }

    [Fact]
    public void IsCostThresholdExceeded_ReturnsFalse_WhenBelowThreshold()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 10))
            .Returns(20.0m);

        for (int i = 0; i < 10; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Act
        var exceeded = _tracker.IsCostThresholdExceeded(DataProviderType.AlphaVantage);

        // Assert
        Assert.False(exceeded);
    }

    [Fact]
    public void IsCostThresholdExceeded_ReturnsTrue_WhenThresholdExceeded()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 60))
            .Returns(120.0m);

        for (int i = 0; i < 60; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Act
        var exceeded = _tracker.IsCostThresholdExceeded(DataProviderType.AlphaVantage);

        // Assert
        Assert.True(exceeded);
    }

    [Fact]
    public void IsCostThresholdExceeded_ReturnsFalse_WhenNoThresholdConfigured()
    {
        // Arrange
        for (int i = 0; i < 1000; i++)
        {
            _tracker.RecordApiCall(DataProviderType.YahooFinance);
        }

        // Act
        var exceeded = _tracker.IsCostThresholdExceeded(DataProviderType.YahooFinance);

        // Assert
        Assert.False(exceeded); // No threshold configured (0.0)
    }

    [Fact]
    public void IsCostThresholdExceeded_ReturnsFalse_WhenCostTrackingDisabled()
    {
        // Arrange
        var disabledSettings = new ProviderCostSettings { Enabled = false };
        var tracker = new ProviderCostTracker(_mockCalculator.Object, _mockLogger.Object, Options.Create(disabledSettings));

        // Act
        var exceeded = tracker.IsCostThresholdExceeded(DataProviderType.AlphaVantage);

        // Assert
        Assert.False(exceeded);
    }

    [Fact]
    public void GetCostThresholdPercentage_ReturnsCorrectPercentage()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 25))
            .Returns(50.0m);

        for (int i = 0; i < 25; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Act
        var percentage = _tracker.GetCostThresholdPercentage(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(50.0, percentage);
    }

    [Fact]
    public void GetCostThresholdPercentage_ReturnsZero_WhenNoThresholdConfigured()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.YahooFinance);

        // Act
        var percentage = _tracker.GetCostThresholdPercentage(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0.0, percentage);
    }

    [Fact]
    public void ResetCostTracking_ClearsMetricsForProvider()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Act
        _tracker.ResetCostTracking(DataProviderType.AlphaVantage);

        // Assert
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        Assert.Equal(0, metrics.TotalApiCalls);
    }

    [Fact]
    public void ResetCostTracking_DoesNotAffectOtherProviders()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.YahooFinance);

        // Act
        _tracker.ResetCostTracking(DataProviderType.AlphaVantage);

        // Assert
        var alphaMetrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        var yahooMetrics = _tracker.GetCostMetrics(DataProviderType.YahooFinance);
        
        Assert.Equal(0, alphaMetrics.TotalApiCalls);
        Assert.Equal(1, yahooMetrics.TotalApiCalls);
    }

    [Fact]
    public void ResetAllCostTracking_ClearsAllProviderMetrics()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        _tracker.RecordApiCall(DataProviderType.YahooFinance);
        _tracker.RecordApiCall(DataProviderType.Mock);

        // Act
        _tracker.ResetAllCostTracking();

        // Assert
        var alphaMetrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        var yahooMetrics = _tracker.GetCostMetrics(DataProviderType.YahooFinance);
        var mockMetrics = _tracker.GetCostMetrics(DataProviderType.Mock);
        
        Assert.Equal(0, alphaMetrics.TotalApiCalls);
        Assert.Equal(0, yahooMetrics.TotalApiCalls);
        Assert.Equal(0, mockMetrics.TotalApiCalls);
    }

    [Fact]
    public void RecordApiCall_LogsWarning_WhenThresholdExceeded()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, It.IsAny<long>()))
            .Returns<DataProviderType, long>((provider, calls) => calls * 2.0m);

        // Act - Record enough calls to exceed threshold
        for (int i = 0; i < 51; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cost threshold exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordApiCall_LogsWarning_WhenApproachingThreshold()
    {
        // Arrange
        _mockCalculator.Setup(x => x.GetTotalEstimatedCost(DataProviderType.AlphaVantage, It.IsAny<long>()))
            .Returns<DataProviderType, long>((provider, calls) => calls * 2.0m);

        // Act - Record enough calls to reach 80% threshold
        for (int i = 0; i < 40; i++)
        {
            _tracker.RecordApiCall(DataProviderType.AlphaVantage);
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cost threshold warning")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordApiCall_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentCalls = 1000;

        // Act - Record API calls concurrently
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordApiCall(DataProviderType.AlphaVantage)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        Assert.Equal(concurrentCalls, metrics.TotalApiCalls);
    }

    [Fact]
    public void GetCostMetrics_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int recordCalls = 100;
        const int readCalls = 50;

        // Act - Record and read metrics concurrently
        for (int i = 0; i < recordCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.RecordApiCall(DataProviderType.AlphaVantage)));
        }
        for (int i = 0; i < readCalls; i++)
        {
            tasks.Add(Task.Run(() => _tracker.GetCostMetrics(DataProviderType.AlphaVantage)));
        }

        // Assert - Should not throw
        Task.WaitAll(tasks.ToArray());

        var finalMetrics = _tracker.GetCostMetrics(DataProviderType.AlphaVantage);
        Assert.Equal(recordCalls, finalMetrics.TotalApiCalls);
    }

    [Fact]
    public void ResetCostTracking_LogsInformationMessage()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Act
        _tracker.ResetCostTracking(DataProviderType.AlphaVantage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reset cost tracking for provider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ResetAllCostTracking_LogsInformationMessage()
    {
        // Arrange
        _tracker.RecordApiCall(DataProviderType.AlphaVantage);

        // Act
        _tracker.ResetAllCostTracking();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reset cost tracking for all providers")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
