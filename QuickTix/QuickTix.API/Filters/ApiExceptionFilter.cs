using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
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
            string message = ex.Message;

            switch (ex)
            {
                case InvalidOperationException:
                case ArgumentException:
                    statusCode = HttpStatusCode.BadRequest;
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    break;

                case DbUpdateException dbEx:
                    statusCode = HttpStatusCode.Conflict;
                    if (dbEx.InnerException?.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        message = "No se puede eliminar este registro porque tiene elementos relacionados (por ejemplo, ventas asociadas).";
                    }
                    else
                    {
                        message = "Error al actualizar o eliminar datos en la base de datos.";
                    }
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "Ha ocurrido un error inesperado en el servidor.";
                    break;
            }

            context.Result = new ObjectResult(new { message })
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;
        }
    }
}
