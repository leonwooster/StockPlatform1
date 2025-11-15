namespace StockSensePro.Core.Exceptions
{
    public class SymbolNotFoundException : StockDataException
    {
        public SymbolNotFoundException() : base()
        {
            Code = ErrorCode.SymbolNotFound;
        }

        public SymbolNotFoundException(string symbol) 
            : base($"Stock symbol '{symbol}' was not found.", symbol, ErrorCode.SymbolNotFound)
        {
        }

        public SymbolNotFoundException(string symbol, Exception innerException) 
            : base($"Stock symbol '{symbol}' was not found.", symbol, ErrorCode.SymbolNotFound, innerException)
        {
        }
    }
}
