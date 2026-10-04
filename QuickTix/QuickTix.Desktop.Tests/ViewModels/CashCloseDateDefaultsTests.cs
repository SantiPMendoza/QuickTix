using System.Net;
using System.Net.Http;
using System.Text;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Desktop.Services;
using QuickTix.Desktop.ViewModels;

namespace QuickTix.Desktop.Tests.ViewModels
{
    /// <summary>
    /// Fechas por defecto del cierre de caja: «hoy» solo se refresca al navegar y solo si el usuario
    /// no las ha tocado; la consulta explícita nunca las pisa y una fecha vacía da validación.
    /// </summary>
    public class CashCloseDateDefaultsTests
    {
        private static readonly DateOnly Yesterday = new(2026, 7, 14);
        private static readonly DateOnly Today = new(2026, 7, 15);
        private static DateTime D(DateOnly d) => d.ToDateTime(TimeOnly.MinValue);

        // ===== Función pura =====

        [Fact]
        public void ForNavigation_BothEmpty_SetsToday()
        {
            var r = CashCloseDateDefaults.ForNavigation(null, null, null, Today);

            Assert.Equal(D(Today), r.From);
            Assert.Equal(D(Today), r.To);
            Assert.Equal(Today, r.DefaultDay);
        }

        [Fact]
        public void ForNavigation_StaleUntouchedDefault_IsRefreshedToToday()
        {
            var r = CashCloseDateDefaults.ForNavigation(D(Yesterday), D(Yesterday), Yesterday, Today);

            Assert.Equal(D(Today), r.From);
            Assert.Equal(D(Today), r.To);
            Assert.Equal(Today, r.DefaultDay);
        }

        [Fact]
        public void ForNavigation_UserChosenPastDay_IsKept()
        {
            // El marcador es null: el usuario editó las fechas tras el valor por defecto
            var r = CashCloseDateDefaults.ForNavigation(D(Yesterday), D(Yesterday), null, Today);

            Assert.Equal(D(Yesterday), r.From);
            Assert.Equal(D(Yesterday), r.To);
            Assert.Null(r.DefaultDay);
        }

        [Fact]
        public void ForNavigation_OneDateEmpty_IsNotOverwritten()
        {
            var r = CashCloseDateDefaults.ForNavigation(D(Yesterday), null, null, Today);

            Assert.Equal(D(Yesterday), r.From);
            Assert.Null(r.To);
        }

        // ===== ViewModel =====

        private sealed class FakeAuth(bool isAdmin) : IAuthService
        {
            public bool IsAdmin { get; } = isAdmin;
            public event Action? SessionChanged { add { } remove { } }
            public Task<bool> LoginAsync(UserLoginDTO loginDto) => Task.FromResult(true);
            public string? GetToken() => null;
            public UserDTO? GetCurrentUser() => null;
            public int GetManagerId() => 0;
            public void Logout() { }
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            public int Calls { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"statusCode":500,"isSuccess":false,"errorMessages":["fallo"],"result":null,"traceId":"t"}""",
                        Encoding.UTF8, "application/json")
                });
            }
        }

        private static (CashCloseViewModel Vm, CountingHandler Handler) CreateViewModel()
        {
            var handler = new CountingHandler();
            var vm = new CashCloseViewModel(new HttpJsonClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new TokenStore()), new FakeAuth(true));
            return (vm, handler);
        }

        [Fact]
        public async Task Consult_WithUserChosenPastDay_KeepsDatesAndQueriesThatDay()
        {
            var (vm, handler) = CreateViewModel();
            vm.RefreshDefaultDates(Yesterday);          // «hoy» por defecto de ayer
            vm.FromDate = D(Yesterday.AddDays(-3));     // el usuario elige otro día
            vm.ToDate = D(Yesterday.AddDays(-3));

            await vm.LoadAsync();                       // Consultar tras pasar medianoche

            Assert.Equal(D(Yesterday.AddDays(-3)), vm.FromDate);
            Assert.Equal(D(Yesterday.AddDays(-3)), vm.ToDate);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task Consult_WithOneDateEmpty_ShowsValidationAndDoesNotQuery()
        {
            var (vm, handler) = CreateViewModel();
            vm.FromDate = D(Yesterday);
            vm.ToDate = null;

            await vm.LoadAsync();

            Assert.Equal("Indica las fechas «Desde» y «Hasta».", vm.ErrorMessage);
            Assert.Equal(D(Yesterday), vm.FromDate);
            Assert.Null(vm.ToDate);
            Assert.Equal(0, handler.Calls);
        }

        [Fact]
        public async Task Consult_WithStaleDefaultDates_DoesNotRefreshThem()
        {
            var (vm, handler) = CreateViewModel();
            vm.RefreshDefaultDates(Yesterday);

            await vm.LoadAsync();                       // Consultar nunca cambia fechas

            Assert.Equal(D(Yesterday), vm.FromDate);
            Assert.Equal(D(Yesterday), vm.ToDate);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public void Navigation_RefreshesStaleDefault_ButNotAfterUserEdit()
        {
            var (vm, _) = CreateViewModel();
            vm.RefreshDefaultDates(Yesterday);

            vm.RefreshDefaultDates(Today);              // navegar al día siguiente: se refresca
            Assert.Equal(D(Today), vm.FromDate);
            Assert.Equal(D(Today), vm.ToDate);

            vm.FromDate = D(Yesterday);                 // el usuario edita
            vm.ToDate = D(Yesterday);
            vm.RefreshDefaultDates(Today.AddDays(1));   // otro día: se respeta lo elegido

            Assert.Equal(D(Yesterday), vm.FromDate);
            Assert.Equal(D(Yesterday), vm.ToDate);
        }
    }
}
