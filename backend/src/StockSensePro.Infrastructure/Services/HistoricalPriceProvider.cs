using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services
{
    public class HistoricalPriceProvider : IHistoricalPriceProvider
    {
        private readonly IYahooFinanceService _yahooFinanceService;

        public HistoricalPriceProvider(IYahooFinanceService yahooFinanceService)
        {
            _yahooFinanceService = yahooFinanceService;
        }

        public async Task<IReadOnlyList<StockPrice>> GetHistoricalPricesAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var prices = await _yahooFinanceService.GetHistoricalPricesAsync(symbol, startDate, endDate, cancellationToken);
            return prices;
        }
    }
}
