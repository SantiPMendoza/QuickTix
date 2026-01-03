using System.Net;

namespace QuickTix.Mobile.Services
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? TraceId { get; }

        public ApiException(string message, HttpStatusCode statusCode, string? traceId = null)
            : base(message)
        {
            StatusCode = statusCode;
            TraceId = traceId;
        }
    }
}
