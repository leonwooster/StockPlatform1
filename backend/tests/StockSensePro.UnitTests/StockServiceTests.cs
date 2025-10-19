using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using StockSensePro.Application.Services;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class StockServiceTests
    {
        private readonly Mock<IStockRepository> _mockRepository;
        private readonly Mock<ILogger<StockService>> _mockLogger;
        private readonly StockService _stockService;

        public StockServiceTests()
        {
            _mockRepository = new Mock<IStockRepository>();
            _mockLogger = new Mock<ILogger<StockService>>();
            _stockService = new StockService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetStockBySymbolAsync_WhenStockExists_ReturnsStock()
        {
            // Arrange
            var symbol = "AAPL";
            var expectedStock = new Stock 
            { 
                Symbol = symbol, 
                Name = "Apple Inc.",
                CurrentPrice = 175.00m
            };
            
            _mockRepository.Setup(repo => repo.GetBySymbolAsync(symbol))
                .ReturnsAsync(expectedStock);

            // Act
            var result = await _stockService.GetStockBySymbolAsync(symbol);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(expectedStock.Name, result.Name);
            Assert.Equal(expectedStock.CurrentPrice, result.CurrentPrice);
        }

        [Fact]
        public async Task GetStockBySymbolAsync_WhenStockDoesNotExist_ReturnsNull()
        {
            // Arrange
            var symbol = "NONEXISTENT";
            _mockRepository.Setup(repo => repo.GetBySymbolAsync(symbol))
                .ReturnsAsync((Stock?)null);

            // Act
            var result = await _stockService.GetStockBySymbolAsync(symbol);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateStockAsync_CallsRepositoryAdd()
        {
            // Arrange
            var stock = new Stock 
            { 
                Symbol = "NEW", 
                Name = "New Stock" 
            };

            // Act
            var result = await _stockService.CreateStockAsync(stock);

            // Assert
            Assert.Equal(stock, result);
            _mockRepository.Verify(repo => repo.AddAsync(stock), Times.Once);
        }
    }
}
