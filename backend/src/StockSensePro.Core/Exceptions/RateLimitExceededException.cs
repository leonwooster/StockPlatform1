namespace StockSensePro.Core.Exceptions
{
    public class RateLimitExceededException : StockDataException
    {
        public RateLimitExceededException() : base()
        {
            Code = ErrorCode.RateLimitExceeded;
        }

        public RateLimitExceededException(string message) 
            : base(message, string.Empty, ErrorCode.RateLimitExceeded)
        {
        }

        public RateLimitExceededException(string message, string symbol) 
            : base(message, symbol, ErrorCode.RateLimitExceeded)
        {
        }

        public RateLimitExceededException(string message, Exception innerException) 
            : base(message, string.Empty, ErrorCode.RateLimitExceeded, innerException)
        {
        }
    }
}
