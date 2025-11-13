using StockSensePro.Core.Entities;

namespace StockSensePro.Core.Interfaces
{
    public interface IHistoricalPriceProvider
    {
        Task<IReadOnlyList<StockPrice>> GetHistoricalPricesAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
