using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Contracts.Routes;
using QuickTix.Mobile.Helpers;
using QuickTix.Mobile.Services;
using QuickTix.Mobile.Views;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace QuickTix.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel de venta batch de tickets en Mobile.
    /// Permite construir líneas de venta, valida campos, registra la venta en API y gestiona logout.
    /// </summary>
    public partial class TicketsViewModel : ObservableObject
    {
        private readonly HttpJsonClient _http;
        private readonly IAppSession _session;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TicketsViewModel"/>.
        /// Prepara colecciones y valores iniciales del formulario de venta batch.
        /// </summary>
        /// <param name="http">Cliente HTTP para consumo de API.</param>
        /// <param name="session">Sesión actual (claims del JWT).</param>
        /// <param name="services">Proveedor de servicios para resolver páginas.</param>
        /// <param name="authService">Servicio de autenticación.</param>
        public TicketsViewModel(HttpJsonClient http, IAppSession session, IServiceProvider services, IAuthService authService)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            TicketTypes = new ObservableCollection<TicketType>();
            TicketContexts = new ObservableCollection<TicketContext>();

            Lines = new ObservableCollection<TicketBatchLineVM>();
            Lines.CollectionChanged += OnLinesChanged;

            NewLineQuantityText = "1";
            NewLineUnitPriceText = string.Empty;

            ClientIdText = string.Empty;
            StatusMessage = string.Empty;
            SessionInfo = string.Empty;
            LinesSummary = string.Empty;
        }

        public ObservableCollection<TicketType> TicketTypes { get; }
        public ObservableCollection<TicketContext> TicketContexts { get; }
        public ObservableCollection<TicketBatchLineVM> Lines { get; }

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string statusMessage;

        [ObservableProperty] private string sessionInfo;
        [ObservableProperty] private string linesSummary;

        [ObservableProperty] private TicketType newLineTicketType;
        [ObservableProperty] private TicketContext newLineTicketContext;

        [ObservableProperty] private string newLineQuantityText;
        [ObservableProperty] private string newLineUnitPriceText;

        [ObservableProperty] private string clientIdText;

        /// <summary>
        /// Indica si la venta requiere ClientId por existir líneas con contexto InvitadoAbonado.
        /// </summary>
        public bool RequiresClientId =>
            Lines.Any(l => l.Context == TicketContext.InvitadoAbonado) ||
            NewLineTicketContext == TicketContext.InvitadoAbonado;

        /// <summary>
        /// Indica si se puede añadir una línea con el estado actual del formulario.
        /// </summary>
        public bool CanAddLine
        {
            get
            {
                if (IsBusy) return false;
                if (!HasManagerContext()) return false;

                if (!int.TryParse(NewLineQuantityText, out var q) || q < 1 || q > 1000)
                    return false;

                if (!string.IsNullOrWhiteSpace(NewLineUnitPriceText))
                {
                    if (!TryParseDecimal(NewLineUnitPriceText, out var price) || price < 0)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Indica si se puede registrar la venta (hay líneas y, si aplica, ClientId válido).
        /// </summary>
        public bool CanSell
        {
            get
            {
                if (IsBusy) return false;
                if (!HasManagerContext()) return false;
                if (Lines.Count == 0) return false;

                if (RequiresClientId)
                {
                    if (!int.TryParse(ClientIdText, out var c) || c <= 0)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Inicializa catálogos de enums y valores por defecto del formulario.
        /// </summary>
        /// <returns>Tarea completada.</returns>
        public Task InitializeAsync()
        {
            if (TicketTypes.Count == 0)
            {
                foreach (var t in Enum.GetValues<TicketType>())
                    TicketTypes.Add(t);
            }

            if (TicketContexts.Count == 0)
            {
                foreach (var c in Enum.GetValues<TicketContext>())
                    TicketContexts.Add(c);
            }

            NewLineTicketType = TicketTypes.FirstOrDefault();
            NewLineTicketContext = TicketContexts.FirstOrDefault();

            RefreshSessionInfo();
            RefreshLinesSummary();

            OnPropertyChanged(nameof(RequiresClientId));
            OnPropertyChanged(nameof(CanAddLine));
            OnPropertyChanged(nameof(CanSell));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Actualiza resúmenes y estados derivados cuando cambia la colección de líneas.
        /// </summary>
        private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshLinesSummary();
            OnPropertyChanged(nameof(RequiresClientId));
            OnPropertyChanged(nameof(CanSell));
        }

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(CanAddLine));
            OnPropertyChanged(nameof(CanSell));
        }

        partial void OnNewLineQuantityTextChanged(string value) => OnPropertyChanged(nameof(CanAddLine));
        partial void OnNewLineUnitPriceTextChanged(string value) => OnPropertyChanged(nameof(CanAddLine));

        partial void OnNewLineTicketContextChanged(TicketContext value)
        {
            OnPropertyChanged(nameof(RequiresClientId));
            OnPropertyChanged(nameof(CanAddLine));
            OnPropertyChanged(nameof(CanSell));
        }

        partial void OnClientIdTextChanged(string value) => OnPropertyChanged(nameof(CanSell));

        /// <summary>
        /// Limpia el formulario y deja el estado listo para una nueva venta.
        /// </summary>
        /// <returns>Tarea completada.</returns>
        [RelayCommand]
        private Task ResetAsync()
        {
            Lines.Clear();

            NewLineTicketType = TicketTypes.FirstOrDefault();
            NewLineTicketContext = TicketContexts.FirstOrDefault();
            NewLineQuantityText = "1";
            NewLineUnitPriceText = string.Empty;

            ClientIdText = string.Empty;
            StatusMessage = string.Empty;

            RefreshSessionInfo();
            RefreshLinesSummary();

            OnPropertyChanged(nameof(RequiresClientId));
            OnPropertyChanged(nameof(CanAddLine));
            OnPropertyChanged(nameof(CanSell));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Añade una nueva línea a la venta con los valores actuales del formulario.
        /// </summary>
        /// <returns>Tarea completada.</returns>
        [RelayCommand]
        private Task AddLineAsync()
        {
            if (!CanAddLine)
            {
                StatusMessage = "Revisa los campos de la nueva línea.";
                return Task.CompletedTask;
            }

            var quantity = int.Parse(NewLineQuantityText);

            decimal? unitPrice = null;
            if (!string.IsNullOrWhiteSpace(NewLineUnitPriceText) &&
                TryParseDecimal(NewLineUnitPriceText, out var parsedPrice))
            {
                unitPrice = parsedPrice;
            }

            Lines.Add(new TicketBatchLineVM(
                type: NewLineTicketType,
                context: NewLineTicketContext,
                quantity: quantity,
                unitPrice: unitPrice
            ));

            NewLineQuantityText = "1";
            NewLineUnitPriceText = string.Empty;

            StatusMessage = "Línea añadida.";
            return Task.CompletedTask;
        }

        /// <summary>
        /// Elimina una línea existente de la venta.
        /// </summary>
        /// <param name="line">Línea a eliminar.</param>
        /// <returns>Tarea completada.</returns>
        [RelayCommand]
        private Task RemoveLineAsync(TicketBatchLineVM line)
        {
            Lines.Remove(line);
            StatusMessage = "Línea eliminada.";
            return Task.CompletedTask;
        }

        /// <summary>
        /// Registra la venta batch en la API usando los datos de sesión (VenueId/ManagerId) y las líneas actuales.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task SellBatchAsync()
        {
            RefreshSessionInfo();

            if (!HasManagerContext())
            {
                StatusMessage = "No se puede vender: faltan VenueId/ManagerId en la sesión (claims del JWT).";
                return;
            }

            if (!CanSell)
            {
                StatusMessage = "Revisa la venta: añade líneas y valida ClientId si aplica.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Registrando venta batch...";

            try
            {
                int? clientId = null;
                if (RequiresClientId && int.TryParse(ClientIdText, out var parsedClientId))
                    clientId = parsedClientId;

                var request = new SellTicketsBatchDTO
                {
                    VenueId = _session.VenueId,
                    ManagerId = _session.ManagerId,
                    ClientId = clientId,
                    Lines = Lines.Select(l => new SellTicketLineDTO
                    {
                        Type = l.Type,
                        Context = l.Context,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice
                    }).ToList()
                };

                var sale = await _http.PostAsync<SellTicketsBatchDTO, SaleDTO>(
                    ApiRoutes.Sale.SellTicketsBatch,
                    request
                );

                StatusMessage = $"Venta registrada correctamente. SaleId={sale.Id}";
                await ResetAsync();
            }
            catch (ApiException apiEx)
            {
                StatusMessage = $"Error API ({(int)apiEx.StatusCode}): {apiEx.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error inesperado: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Cierra la sesión actual y redirige al login.
        /// </summary>
        /// <returns>Tarea completada.</returns>
        [RelayCommand]
        private Task LogoutAsync()
        {
            _authService.Logout();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var loginPage = _services.GetRequiredService<LoginPage>();
                App.Current.MainPage = new NavigationPage(loginPage);
            });

            return Task.CompletedTask;
        }

        private bool HasManagerContext() => _session.VenueId > 0 && _session.ManagerId > 0;

        /// <summary>
        /// Actualiza la información de sesión visible en UI.
        /// </summary>
        private void RefreshSessionInfo()
        {
            SessionInfo = $"VenueId={_session.VenueId} | ManagerId={_session.ManagerId} | Role={_session.Role}";
            OnPropertyChanged(nameof(SessionInfo));
        }

        /// <summary>
        /// Recalcula el resumen de líneas (cantidad y total estimado si hay precios manuales).
        /// </summary>
        private void RefreshLinesSummary()
        {
            var totalQty = Lines.Sum(l => l.Quantity);
            var totalAmount = Lines.Sum(l => (l.UnitPrice ?? 0m) * l.Quantity);

            LinesSummary = $"Líneas: {Lines.Count} | Entradas: {totalQty} | Total (si hay precios): {totalAmount}";
            OnPropertyChanged(nameof(LinesSummary));
        }

        /// <summary>
        /// Intenta parsear un decimal en cultura actual o invariante.
        /// </summary>
        /// <param name="input">Texto del usuario.</param>
        /// <param name="value">Decimal parseado.</param>
        /// <returns>True si se pudo parsear.</returns>
        private static bool TryParseDecimal(string input, out decimal value)
        {
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>
    /// Representa una línea de venta batch para la UI.
    /// Mantiene datos de negocio y textos formateados para mostrar en la lista.
    /// </summary>
    public sealed class TicketBatchLineVM
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TicketBatchLineVM"/> y calcula textos de presentación.
        /// </summary>
        /// <param name="type">Tipo de ticket.</param>
        /// <param name="context">Contexto del ticket.</param>
        /// <param name="quantity">Cantidad.</param>
        /// <param name="unitPrice">Precio unitario manual (null para "Auto").</param>
        public TicketBatchLineVM(TicketType type, TicketContext context, int quantity, decimal? unitPrice)
        {
            Type = type;
            Context = context;
            Quantity = quantity;
            UnitPrice = unitPrice;

            var culture = CultureInfo.CurrentCulture;

            DisplayTitle = $"{quantity} x {Type} ({Context})";

            var priceText = unitPrice.HasValue ? unitPrice.Value.ToString("C", culture) : "Auto";
            DisplayInfo = $"Precio unitario: {priceText}";

            var totalText = unitPrice.HasValue ? (unitPrice.Value * quantity).ToString("C", culture) : "Auto";
            DisplayTotal = $"Total: {totalText}";
        }

        public TicketType Type { get; }
        public TicketContext Context { get; }
        public int Quantity { get; }
        public decimal? UnitPrice { get; }

        public string DisplayTitle { get; }
        public string DisplayInfo { get; }
        public string DisplayTotal { get; }
    }
}
