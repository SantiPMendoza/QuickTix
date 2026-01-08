using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
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
                    {
                        // 1) FK restrict / referencias
                        if (dbEx.InnerException?.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            statusCode = HttpStatusCode.Conflict;
                            message = "No se puede eliminar este registro porque tiene elementos relacionados (por ejemplo, ventas asociadas).";
                            break;
                        }

                        // 2) Duplicados por índices únicos (2601/2627)
                        var sqlEx = FindSqlException(dbEx);
                        if (sqlEx != null && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                        {
                            statusCode = HttpStatusCode.Conflict;

                            var sqlMessage = sqlEx.Message ?? string.Empty;

                            if (sqlMessage.Contains("IX_AspNetUsers_Nif", StringComparison.OrdinalIgnoreCase))
                                message = "Ya existe un usuario con ese NIF/NIE.";
                            else if (sqlMessage.Contains("IX_AspNetUsers_PhoneNumber", StringComparison.OrdinalIgnoreCase))
                                message = "Ya existe un usuario con ese número de teléfono.";
                            else
                                message = "Ya existe un registro con los mismos datos únicos.";

                            break;
                        }

                        // 3) Resto de DbUpdateException
                        statusCode = HttpStatusCode.Conflict;
                        message = "Error al actualizar o eliminar datos en la base de datos.";
                        break;
                    }


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

        private static SqlException? FindSqlException(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is SqlException sqlEx)
                    return sqlEx;

                current = current.InnerException;
            }

            return null;
        }

    }
}
