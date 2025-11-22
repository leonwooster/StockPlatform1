using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using Xunit;

namespace StockSensePro.IntegrationTests
{
    public class ConfigurationIntegrationTests
    {
        [Fact]
        public void AppsettingsJson_ShouldLoadDataProviderSettings_Successfully()
        {
            // Arrange
            var configuration = BuildConfiguration("appsettings.json");
            var services = new ServiceCollection();
            services.Configure<DataProviderSettings>(configuration.GetSection(DataProviderSettings.SectionName));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<DataProviderSettings>>().Value;

            // Assert
            Assert.NotNull(options);
            Assert.Equal(DataProviderType.YahooFinance, options.PrimaryProvider);
            Assert.Null(options.FallbackProvider);
            Assert.Equal(ProviderStrategyType.Primary, options.Strategy);
            Assert.True(options.EnableAutomaticFallback);
            Assert.Equal(60, options.HealthCheckIntervalSeconds);
        }

        [Fact]
        public void AppsettingsJson_ShouldLoadAlphaVantageSettings_Successfully()
        {
            // Arrange
            var configuration = BuildConfiguration("appsettings.json");
            var services = new ServiceCollection();
            services.Configure<AlphaVantageSettings>(configuration.GetSection(AlphaVantageSettings.SectionName));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<AlphaVantageSettings>>().Value;

            // Assert
            Assert.NotNull(options);
            Assert.Equal(string.Empty, options.ApiKey);
            Assert.Equal("https://www.alphavantage.co/query", options.BaseUrl);
            Assert.Equal(10, options.Timeout);
            Assert.Equal(3, options.MaxRetries);
            Assert.False(options.Enabled);
            
            // Rate limit assertions
            Assert.NotNull(options.RateLimit);
            Assert.Equal(5, options.RateLimit.RequestsPerMinute);
            Assert.Equal(0, options.RateLimit.RequestsPerHour);
            Assert.Equal(25, options.RateLimit.RequestsPerDay);
            
            // Data enrichment assertions
            Assert.NotNull(options.DataEnrichment);
            Assert.False(options.DataEnrichment.EnableBidAskEnrichment);
            Assert.True(options.DataEnrichment.EnableCalculated52WeekRange);
            Assert.True(options.DataEnrichment.EnableCalculatedAverageVolume);
            Assert.Equal(86400, options.DataEnrichment.CalculatedFieldsCacheTTL);
        }

        [Fact]
        public void AppsettingsDevelopmentJson_ShouldLoadWithMockDefaults()
        {
            // Arrange
            var configuration = BuildConfiguration("appsettings.Development.json");
            var services = new ServiceCollection();
            services.Configure<DataProviderSettings>(configuration.GetSection(DataProviderSettings.SectionName));
            services.Configure<AlphaVantageSettings>(configuration.GetSection(AlphaVantageSettings.SectionName));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var dataProviderOptions = serviceProvider.GetRequiredService<IOptions<DataProviderSettings>>().Value;
            var alphaVantageOptions = serviceProvider.GetRequiredService<IOptions<AlphaVantageSettings>>().Value;

            // Assert - DataProvider
            Assert.NotNull(dataProviderOptions);
            Assert.Equal(DataProviderType.YahooFinance, dataProviderOptions.PrimaryProvider);
            
            // Assert - AlphaVantage should be disabled in development
            Assert.NotNull(alphaVantageOptions);
            Assert.False(alphaVantageOptions.Enabled);
        }

        [Fact]
        public void AppsettingsProductionJson_ShouldLoadWithProductionSettings()
        {
            // Arrange
            var configuration = BuildConfiguration("appsettings.Production.json");
            var services = new ServiceCollection();
            services.Configure<DataProviderSettings>(configuration.GetSection(DataProviderSettings.SectionName));
            services.Configure<AlphaVantageSettings>(configuration.GetSection(AlphaVantageSettings.SectionName));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var dataProviderOptions = serviceProvider.GetRequiredService<IOptions<DataProviderSettings>>().Value;
            var alphaVantageOptions = serviceProvider.GetRequiredService<IOptions<AlphaVantageSettings>>().Value;

            // Assert - DataProvider should use AlphaVantage as primary with fallback
            Assert.NotNull(dataProviderOptions);
            Assert.Equal(DataProviderType.AlphaVantage, dataProviderOptions.PrimaryProvider);
            Assert.Equal(DataProviderType.YahooFinance, dataProviderOptions.FallbackProvider);
            Assert.Equal(ProviderStrategyType.Fallback, dataProviderOptions.Strategy);
            Assert.True(dataProviderOptions.EnableAutomaticFallback);
            
            // Assert - AlphaVantage should be enabled in production
            Assert.NotNull(alphaVantageOptions);
            Assert.True(alphaVantageOptions.Enabled);
            Assert.True(alphaVantageOptions.DataEnrichment.EnableBidAskEnrichment);
        }

        [Fact]
        public void AllConfigurationFiles_ShouldHaveValidJsonStructure()
        {
            // Arrange & Act & Assert
            var baseConfig = BuildConfiguration("appsettings.json");
            Assert.NotNull(baseConfig);
            Assert.NotNull(baseConfig.GetSection(DataProviderSettings.SectionName));
            Assert.NotNull(baseConfig.GetSection(AlphaVantageSettings.SectionName));

            var devConfig = BuildConfiguration("appsettings.Development.json");
            Assert.NotNull(devConfig);
            Assert.NotNull(devConfig.GetSection(DataProviderSettings.SectionName));
            Assert.NotNull(devConfig.GetSection(AlphaVantageSettings.SectionName));

            var prodConfig = BuildConfiguration("appsettings.Production.json");
            Assert.NotNull(prodConfig);
            Assert.NotNull(prodConfig.GetSection(DataProviderSettings.SectionName));
            Assert.NotNull(prodConfig.GetSection(AlphaVantageSettings.SectionName));
        }

        [Fact]
        public void DataProviderSettings_ShouldBindAllProperties()
        {
            // Arrange
            var configuration = BuildConfiguration("appsettings.json");
            var services = new ServiceCollection();
            services.Configure<DataProviderSettings>(configuration.GetSection(DataProviderSettings.SectionName));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<DataProviderSettings>>().Value;

            // Assert - Verify all properties are bound with valid values
            Assert.True(Enum.IsDefined(typeof(DataProviderType), options.PrimaryProvider));
            Assert.True(Enum.IsDefined(typeof(ProviderStrategyType), options.Strategy));
            Assert.True(options.HealthCheckIntervalSeconds > 0);
        }

        [Fact]
        public void AlphaVantageSettings_RateLimitSettings_ShouldBindCorrectly()
        {
            // Arrange
            var configuration = BuildConfiguration("appsettings.json");
            var services = new ServiceCollection();
            services.Configure<AlphaVantageSettings>(configuration.GetSection(AlphaVantageSettings.SectionName));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetRequiredService<IOptions<AlphaVantageSettings>>().Value;

            // Assert
            Assert.NotNull(options.RateLimit);
            Assert.True(options.RateLimit.RequestsPerMinute > 0);
            Assert.True(options.RateLimit.RequestsPerDay > 0);
        }

        private IConfiguration BuildConfiguration(string fileName)
        {
            var configPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "..",
                "src", "StockSensePro.Api",
                fileName
            );

            return new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: false)
                .Build();
        }
    }
}
