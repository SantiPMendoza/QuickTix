using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuickTix.Contracts.Common;
using System.Diagnostics;
using System.Net;

namespace QuickTix.API.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;
            _logger.LogError(ex, "Unhandled exception occurred");

            HttpStatusCode statusCode;
            string message;

            switch (ex)
            {
                case InvalidOperationException:
                case ArgumentException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = ex.Message;
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = ex.Message;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = ex.Message;
                    break;

                case DbUpdateException dbEx:
                    statusCode = HttpStatusCode.Conflict;
                    if (dbEx.InnerException?.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) == true)
                        message = "No se puede eliminar este registro porque tiene elementos relacionados (por ejemplo, ventas asociadas).";
                    else
                        message = "Error al actualizar o eliminar datos en la base de datos.";
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "Ha ocurrido un error inesperado en el servidor.";
                    break;
            }

            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

            var response = ApiResponse<object>.Fail(statusCode, new[] { message }, traceId);

            context.Result = new ObjectResult(response)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;
        }
    }
}
