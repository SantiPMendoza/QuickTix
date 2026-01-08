using System.Net;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Excepción específica para errores producidos al consumir la API de QuickTix desde el cliente Desktop.
    /// Permite transportar el código HTTP y, opcionalmente, un TraceId para trazabilidad.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Código de estado HTTP devuelto por la API.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Identificador de traza correlacionable con logs del servidor.
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ApiException"/> con mensaje, código HTTP y TraceId opcional.
        /// </summary>
        /// <param name="message">Mensaje de error para el usuario o para logging.</param>
        /// <param name="statusCode">Código de estado HTTP.</param>
        /// <param name="traceId">Identificador de traza (opcional).</param>
        public ApiException(string message, HttpStatusCode statusCode, string? traceId = null)
            : base(message)
        {
            StatusCode = statusCode;
            TraceId = traceId;
        }
    }
}
