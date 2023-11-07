using System.Net;

namespace PlagScanAPI.Infrastructure.Exceptions
{
    public class ExceptionWithStatusCode : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ExceptionWithStatusCode(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
