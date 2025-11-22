using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Infrastructure.Services;
using Xunit;

namespace StockSensePro.UnitTests;

public class ProviderCostCalculatorTests
{
    private readonly Mock<ILogger<ProviderCostCalculator>> _mockLogger;
    private readonly ProviderCostSettings _settings;
    private readonly ProviderCostCalculator _calculator;

    public ProviderCostCalculatorTests()
    {
        _mockLogger = new Mock<ILogger<ProviderCostCalculator>>();
        _settings = new ProviderCostSettings
        {
            Enabled = true,
            Providers = new Dictionary<string, ProviderCostConfig>
            {
                { "YahooFinance", new ProviderCostConfig { CostPerCall = 0.0m, MonthlySubscription = 0.0m } },
                { "AlphaVantage", new ProviderCostConfig { CostPerCall = 0.002m, MonthlySubscription = 49.99m } },
                { "Mock", new ProviderCostConfig { CostPerCall = 0.0m, MonthlySubscription = 0.0m } }
            }
        };
        _calculator = new ProviderCostCalculator(_mockLogger.Object, Options.Create(_settings));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ProviderCostCalculator(null!, Options.Create(_settings)));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ProviderCostCalculator(_mockLogger.Object, null!));
    }

    [Fact]
    public void GetCostPerCall_ReturnsCorrectCost_ForYahooFinance()
    {
        // Act
        var cost = _calculator.GetCostPerCall(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void GetCostPerCall_ReturnsCorrectCost_ForAlphaVantage()
    {
        // Act
        var cost = _calculator.GetCostPerCall(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(0.002m, cost);
    }

    [Fact]
    public void GetCostPerCall_ReturnsZero_ForUnconfiguredProvider()
    {
        // Arrange
        var emptySettings = new ProviderCostSettings { Providers = new Dictionary<string, ProviderCostConfig>() };
        var calculator = new ProviderCostCalculator(_mockLogger.Object, Options.Create(emptySettings));

        // Act
        var cost = calculator.GetCostPerCall(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void CalculateCost_ReturnsZero_ForZeroCalls()
    {
        // Act
        var cost = _calculator.CalculateCost(DataProviderType.AlphaVantage, 0);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void CalculateCost_CalculatesCorrectly_ForSingleCall()
    {
        // Act
        var cost = _calculator.CalculateCost(DataProviderType.AlphaVantage, 1);

        // Assert
        Assert.Equal(0.002m, cost);
    }

    [Fact]
    public void CalculateCost_CalculatesCorrectly_ForMultipleCalls()
    {
        // Act
        var cost = _calculator.CalculateCost(DataProviderType.AlphaVantage, 100);

        // Assert
        Assert.Equal(0.2m, cost);
    }

    [Fact]
    public void CalculateCost_CalculatesCorrectly_ForLargeNumberOfCalls()
    {
        // Act
        var cost = _calculator.CalculateCost(DataProviderType.AlphaVantage, 10000);

        // Assert
        Assert.Equal(20.0m, cost);
    }

    [Fact]
    public void CalculateCost_ReturnsZero_ForFreeProvider()
    {
        // Act
        var cost = _calculator.CalculateCost(DataProviderType.YahooFinance, 1000);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void GetMonthlySubscriptionCost_ReturnsZero_ForYahooFinance()
    {
        // Act
        var cost = _calculator.GetMonthlySubscriptionCost(DataProviderType.YahooFinance);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void GetMonthlySubscriptionCost_ReturnsCorrectCost_ForAlphaVantage()
    {
        // Act
        var cost = _calculator.GetMonthlySubscriptionCost(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(49.99m, cost);
    }

    [Fact]
    public void GetMonthlySubscriptionCost_ReturnsZero_ForUnconfiguredProvider()
    {
        // Arrange
        var emptySettings = new ProviderCostSettings { Providers = new Dictionary<string, ProviderCostConfig>() };
        var calculator = new ProviderCostCalculator(_mockLogger.Object, Options.Create(emptySettings));

        // Act
        var cost = calculator.GetMonthlySubscriptionCost(DataProviderType.AlphaVantage);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void GetTotalEstimatedCost_IncludesSubscriptionAndUsage()
    {
        // Act
        var cost = _calculator.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 100);

        // Assert
        // 100 calls * $0.002 + $49.99 subscription = $50.19
        Assert.Equal(50.19m, cost);
    }

    [Fact]
    public void GetTotalEstimatedCost_ReturnsOnlyUsageCost_WhenNoSubscription()
    {
        // Act
        var cost = _calculator.GetTotalEstimatedCost(DataProviderType.YahooFinance, 100);

        // Assert
        Assert.Equal(0.0m, cost);
    }

    [Fact]
    public void GetTotalEstimatedCost_ReturnsOnlySubscriptionCost_WhenNoCalls()
    {
        // Act
        var cost = _calculator.GetTotalEstimatedCost(DataProviderType.AlphaVantage, 0);

        // Assert
        Assert.Equal(49.99m, cost);
    }

    [Theory]
    [InlineData(1, 49.992)]
    [InlineData(10, 50.01)]
    [InlineData(100, 50.19)]
    [InlineData(1000, 51.99)]
    [InlineData(5000, 59.99)]
    public void GetTotalEstimatedCost_CalculatesCorrectly_ForVariousCallCounts(long calls, decimal expectedCost)
    {
        // Act
        var cost = _calculator.GetTotalEstimatedCost(DataProviderType.AlphaVantage, calls);

        // Assert
        Assert.Equal(expectedCost, cost, 3); // 3 decimal places precision
    }

    [Fact]
    public void GetCostPerCall_LogsWarning_ForUnconfiguredProvider()
    {
        // Arrange
        var emptySettings = new ProviderCostSettings { Providers = new Dictionary<string, ProviderCostConfig>() };
        var calculator = new ProviderCostCalculator(_mockLogger.Object, Options.Create(emptySettings));

        // Act
        calculator.GetCostPerCall(DataProviderType.AlphaVantage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No cost configuration found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetMonthlySubscriptionCost_LogsWarning_ForUnconfiguredProvider()
    {
        // Arrange
        var emptySettings = new ProviderCostSettings { Providers = new Dictionary<string, ProviderCostConfig>() };
        var calculator = new ProviderCostCalculator(_mockLogger.Object, Options.Create(emptySettings));

        // Act
        calculator.GetMonthlySubscriptionCost(DataProviderType.AlphaVantage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No subscription cost configuration found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
