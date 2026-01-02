using System.Net;

namespace QuickTix.Contracts.Common
{
    public class ApiResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; } = true;
        public List<string> ErrorMessages { get; set; } = new();
        public T? Result { get; set; }
        public string? TraceId { get; set; }

        public static ApiResponse<T> Ok(T result, HttpStatusCode statusCode = HttpStatusCode.OK, string? traceId = null)
            => new()
            {
                StatusCode = statusCode,
                IsSuccess = true,
                Result = result,
                TraceId = traceId
            };

        public static ApiResponse<T> Fail(HttpStatusCode statusCode, IEnumerable<string> errors, string? traceId = null)
            => new()
            {
                StatusCode = statusCode,
                IsSuccess = false,
                Result = default,
                ErrorMessages = errors?.ToList() ?? new List<string> { "Error no especificado." },
                TraceId = traceId
            };
    }
}
