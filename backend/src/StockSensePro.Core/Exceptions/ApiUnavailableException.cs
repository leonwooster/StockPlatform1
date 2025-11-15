namespace StockSensePro.Core.Exceptions
{
    public class ApiUnavailableException : StockDataException
    {
        public ApiUnavailableException() : base()
        {
            Code = ErrorCode.ApiUnavailable;
        }

        public ApiUnavailableException(string message) 
            : base(message, string.Empty, ErrorCode.ApiUnavailable)
        {
        }

        public ApiUnavailableException(string message, Exception innerException) 
            : base(message, string.Empty, ErrorCode.ApiUnavailable, innerException)
        {
        }

        public ApiUnavailableException(string message, string symbol, Exception innerException) 
            : base(message, symbol, ErrorCode.ApiUnavailable, innerException)
        {
        }
    }
}
