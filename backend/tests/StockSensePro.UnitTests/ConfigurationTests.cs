using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using Xunit;

namespace StockSensePro.UnitTests;

public class ConfigurationTests
{
    [Fact]
    public void AlphaVantageSettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new AlphaVantageSettings();

        // Assert
        Assert.Equal("https://www.alphavantage.co/query", settings.BaseUrl);
        Assert.Equal(10, settings.Timeout);
        Assert.Equal(3, settings.MaxRetries);
        Assert.False(settings.Enabled);
        Assert.Equal(string.Empty, settings.ApiKey);
        Assert.NotNull(settings.RateLimit);
        Assert.NotNull(settings.DataEnrichment);
    }

    [Fact]
    public void AlphaVantageSettings_DataEnrichment_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new AlphaVantageSettings();

        // Assert
        Assert.False(settings.DataEnrichment.EnableBidAskEnrichment);
        Assert.True(settings.DataEnrichment.EnableCalculated52WeekRange);
        Assert.True(settings.DataEnrichment.EnableCalculatedAverageVolume);
        Assert.Equal(86400, settings.DataEnrichment.CalculatedFieldsCacheTTL);
    }

    [Fact]
    public void DataProviderSettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new DataProviderSettings();

        // Assert
        Assert.Equal(DataProviderType.YahooFinance, settings.PrimaryProvider);
        Assert.Null(settings.FallbackProvider);
        Assert.Equal(ProviderStrategyType.Primary, settings.Strategy);
        Assert.True(settings.EnableAutomaticFallback);
        Assert.Equal(60, settings.HealthCheckIntervalSeconds);
    }

    [Fact]
    public void AlphaVantageSettings_CanSetProperties()
    {
        // Arrange
        var settings = new AlphaVantageSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://custom.url",
            Timeout = 20,
            MaxRetries = 5,
            Enabled = true
        };

        // Assert
        Assert.Equal("test-api-key", settings.ApiKey);
        Assert.Equal("https://custom.url", settings.BaseUrl);
        Assert.Equal(20, settings.Timeout);
        Assert.Equal(5, settings.MaxRetries);
        Assert.True(settings.Enabled);
    }

    [Fact]
    public void DataProviderSettings_CanSetProperties()
    {
        // Arrange
        var settings = new DataProviderSettings
        {
            PrimaryProvider = DataProviderType.AlphaVantage,
            FallbackProvider = DataProviderType.YahooFinance,
            Strategy = ProviderStrategyType.Fallback,
            EnableAutomaticFallback = false,
            HealthCheckIntervalSeconds = 120
        };

        // Assert
        Assert.Equal(DataProviderType.AlphaVantage, settings.PrimaryProvider);
        Assert.Equal(DataProviderType.YahooFinance, settings.FallbackProvider);
        Assert.Equal(ProviderStrategyType.Fallback, settings.Strategy);
        Assert.False(settings.EnableAutomaticFallback);
        Assert.Equal(120, settings.HealthCheckIntervalSeconds);
    }
}
