using System.Net;
using System.Net.Http;
using System.Text;
using QuickTix.Contracts.DTOs.SaleDTOs;
using QuickTix.Desktop.Services;

namespace QuickTix.Desktop.Tests.Services
{
    /// <summary>
    /// <see cref="HttpJsonClient.PostAsync{TRequest}"/> (POST sin resultado, usado al anular ventas):
    /// un envelope de éxito con Result nulo no debe lanzar, y un error debe llegar como ApiException.
    /// </summary>
    public class HttpJsonClientTests
    {
        private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
        }

        private static HttpJsonClient CreateClient(HttpStatusCode status, string body) =>
            new(new HttpClient(new StubHandler(status, body)), new TokenStore());

        private const string Url = "http://localhost/api/Sale/1/void";

        [Fact]
        public async Task PostAsync_SuccessEnvelopeWithNullResult_DoesNotThrow()
        {
            var client = CreateClient(HttpStatusCode.OK,
                """{"statusCode":200,"isSuccess":true,"errorMessages":[],"result":null,"traceId":"t-1"}""");

            var exception = await Record.ExceptionAsync(() =>
                client.PostAsync(Url, new VoidSaleDTO { Reason = "Error de cobro" }));

            Assert.Null(exception);
        }

        [Fact]
        public async Task PostAsync_ConflictEnvelope_ThrowsApiExceptionWithMessageAndStatus()
        {
            var client = CreateClient(HttpStatusCode.Conflict,
                """{"statusCode":409,"isSuccess":false,"errorMessages":["La venta ya está anulada."],"result":null,"traceId":"t-2"}""");

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                client.PostAsync(Url, new VoidSaleDTO { Reason = "Otra vez" }));

            Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
            Assert.Equal("La venta ya está anulada.", ex.Message);
        }

        [Fact]
        public async Task PostAsync_ValidationEnvelope_ThrowsApiExceptionWithMessage()
        {
            // Forma que devuelve la API para un modelo inválido (InvalidModelStateResponseFactory, ADR-003)
            var client = CreateClient(HttpStatusCode.BadRequest,
                """{"statusCode":400,"isSuccess":false,"errorMessages":["El motivo de la anulación es obligatorio."],"result":null,"traceId":"t-3"}""");

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                client.PostAsync(Url, new VoidSaleDTO()));

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Contains("obligatorio", ex.Message);
        }
    }
}
