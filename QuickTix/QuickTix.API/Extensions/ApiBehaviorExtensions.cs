using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using System.Net;

namespace QuickTix.API.Extensions
{
    /// <summary>
    /// Ajusta el comportamiento automático de [ApiController] al contrato de la API (ADR-003).
    /// </summary>
    public static class ApiBehaviorExtensions
    {
        /// <summary>
        /// Con [ApiController], un modelo inválido se responde solo con ProblemDetails y nunca llega al
        /// action: el cliente no encontraría "errorMessages". Esta fábrica lo devuelve en el envelope
        /// <see cref="ApiResponse{T}"/> con 400 y los mensajes del ModelState.
        /// </summary>
        public static IServiceCollection AddApiEnvelopeValidation(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                        .ToList();

                    if (errors.Count == 0)
                        errors.Add("Error de validación.");

                    var response = ApiResponse<object>.Fail(
                        HttpStatusCode.BadRequest,
                        errors,
                        context.HttpContext.TraceIdentifier);

                    return new BadRequestObjectResult(response);
                };
            });

            return services;
        }
    }
}
