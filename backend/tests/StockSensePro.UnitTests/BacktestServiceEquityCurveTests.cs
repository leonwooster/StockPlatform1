using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Models;
using StockSensePro.Application.Services;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using Xunit;

namespace StockSensePro.UnitTests
{
    public class BacktestServiceEquityCurveTests
    {
        private readonly Mock<ITradingSignalRepository> _signalsRepo = new();
        private readonly Mock<ISignalPerformanceRepository> _perfRepo = new();
        private readonly Mock<IHistoricalPriceProvider> _priceProvider = new();
        private readonly Mock<ILogger<BacktestService>> _logger = new();

        private BacktestService CreateServiceWithPerformances(IEnumerable<SignalPerformance> performances)
        {
            _perfRepo
                .Setup(r => r.GetBySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(performances.ToList());

            return new BacktestService(_signalsRepo.Object, _perfRepo.Object, _priceProvider.Object, _logger.Object);
        }

        [Fact]
        public async Task GetEquityCurve_Compounded_ComputesExpectedSeries()
        {
            // Arrange
            var symbol = "AAPL";
            var baseDate = new DateTime(2025, 1, 1);
            var perfs = new List<SignalPerformance>
            {
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(0), ActualReturn = 10m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(1), ActualReturn = -5m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(2), ActualReturn = 7m }
            };
            var svc = CreateServiceWithPerformances(perfs);

            // Act
            var curve = await svc.GetEquityCurveAsync(symbol, compounded: true);

            // Assert
            Assert.Equal(3, curve.Count);
            Assert.Equal(110.00m, curve[0].Equity);
            Assert.Equal(104.50m, curve[1].Equity);
            Assert.Equal(111.82m, curve[2].Equity); // 100 * 1.10 * 0.95 * 1.07 = 111.815 -> 111.82
        }

        [Fact]
        public async Task GetEquityCurve_Additive_ComputesExpectedSeries()
        {
            // Arrange
            var symbol = "AAPL";
            var baseDate = new DateTime(2025, 1, 1);
            var perfs = new List<SignalPerformance>
            {
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(0), ActualReturn = 10m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(1), ActualReturn = -5m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(2), ActualReturn = 7m }
            };
            var svc = CreateServiceWithPerformances(perfs);

            // Act
            var curve = await svc.GetEquityCurveAsync(symbol, compounded: false);

            // Assert
            Assert.Equal(3, curve.Count);
            Assert.Equal(110.00m, curve[0].Equity); // 100 + 10
            Assert.Equal(105.00m, curve[1].Equity); // 100 + 10 - 5
            Assert.Equal(112.00m, curve[2].Equity); // 100 + 10 - 5 + 7
        }

        [Fact]
        public async Task GetEquityCurve_WithDateFilter_FiltersSeries()
        {
            // Arrange
            var symbol = "AAPL";
            var baseDate = new DateTime(2025, 1, 1);
            var perfs = new List<SignalPerformance>
            {
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(0), ActualReturn = 1m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(1), ActualReturn = 2m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(2), ActualReturn = 3m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = baseDate.AddDays(3), ActualReturn = 4m },
            };
            var svc = CreateServiceWithPerformances(perfs);

            // Act
            var start = baseDate.AddDays(1);
            var end = baseDate.AddDays(2);
            var curve = await svc.GetEquityCurveAsync(symbol, startDate: start, endDate: end, compounded: false);

            // Assert
            Assert.Equal(2, curve.Count);
            Assert.Equal(102.00m, curve[0].Equity); // 100 + 2
            Assert.Equal(105.00m, curve[1].Equity); // 100 + 2 + 3
        }

        [Fact]
        public async Task GetEquityCurveDaily_Compounded_AppliesMultipleReturnsPerDay()
        {
            // Arrange
            var symbol = "AAPL";
            var d = new DateTime(2025, 2, 10);
            var perfs = new List<SignalPerformance>
            {
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = d.AddHours(10), ActualReturn = 5m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = d.AddHours(15), ActualReturn = 5m },
            };
            var svc = CreateServiceWithPerformances(perfs);

            // Act
            var curve = await svc.GetEquityCurveDailyAsync(symbol, d, d.AddDays(1), compounded: true);

            // Assert
            Assert.Equal(2, curve.Count);
            Assert.Equal(110.25m, curve[0].Equity); // 100 * 1.05 * 1.05
            Assert.Equal(110.25m, curve[1].Equity); // next day unchanged
        }

        [Fact]
        public async Task GetEquityCurveDaily_Additive_SumsReturnsPerDay()
        {
            // Arrange
            var symbol = "AAPL";
            var d = new DateTime(2025, 2, 10);
            var perfs = new List<SignalPerformance>
            {
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = d.AddHours(10), ActualReturn = 5m },
                new SignalPerformance { TradingSignalId = Guid.NewGuid(), EvaluatedAt = d.AddHours(15), ActualReturn = 5m },
            };
            var svc = CreateServiceWithPerformances(perfs);

            // Act
            var curve = await svc.GetEquityCurveDailyAsync(symbol, d, d.AddDays(1), compounded: false);

            // Assert
            Assert.Equal(2, curve.Count);
            Assert.Equal(110.00m, curve[0].Equity); // 100 + (5 + 5)
            Assert.Equal(110.00m, curve[1].Equity);
        }
    }
}
