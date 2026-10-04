using QuickTix.Contracts.DTOs.SaleDTOs;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Desktop.ViewModels.Base;
using System.ComponentModel;
using System.Net;

namespace QuickTix.Desktop.ViewModels.Sales
{
    /// <summary>
    /// Base común de los históricos de ventas (Entradas y Suscripciones): añade la acción
    /// «Anular» (solo admin, solo ventas no anuladas) con diálogo de motivo obligatorio.
    /// La venta no se borra: la API la marca como anulada y el historial la sigue mostrando.
    /// </summary>
    /// <typeparam name="T">DTO de fila del historial.</typeparam>
    public abstract partial class SalesHistoryViewModelBase<T> : BaseCrudViewModel<T, CreateSaleDTO>
        where T : class
    {
        private readonly IAuthService _authService;

        // ===== Diálogo de anulación =====
        [ObservableProperty] private bool isVoidDialogOpen;

        // Snapshot del id al abrir el diálogo: la selección puede cambiar con el popup abierto
        // (mismo motivo que PendingDeleteItem en BaseCrudViewModel).
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VoidDialogTitle))]
        private int pendingVoidSaleId;

        [ObservableProperty] private string voidReason = string.Empty;
        [ObservableProperty] private string? voidError;
        [ObservableProperty] private bool isVoiding;

        /// <summary>Id de la venta de una fila del historial.</summary>
        protected abstract int GetSaleId(T item);

        /// <summary>Indica si la fila ya está anulada.</summary>
        protected abstract bool GetIsVoided(T item);

        /// <summary>Solo el admin puede anular (la API responde 403 al resto).</summary>
        public bool IsAdmin => _authService.IsAdmin;

        /// <summary>Título del diálogo de anulación.</summary>
        public string VoidDialogTitle => $"Anular venta {PendingVoidSaleId}";

        /// <summary>Contador de longitud del motivo (se mide recortado, igual que la API).</summary>
        public string VoidReasonCounter => $"{VoidReason.Trim().Length}/{VoidSaleDTO.ReasonMaxLength}";

        /// <summary>
        /// Inicializa la base del histórico.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        /// <param name="authService">Servicio de autenticación (rol de la sesión).</param>
        protected SalesHistoryViewModelBase(HttpJsonClient httpClient, IAuthService authService)
            : base(httpClient)
        {
            _authService = authService;

            // «Anular» se habilita según la fila seleccionada y el rol de la sesión
            PropertyChanged += OnBasePropertyChanged;
            _authService.SessionChanged += () =>
            {
                OnPropertyChanged(nameof(IsAdmin));
                OpenVoidForSelectedCommand.NotifyCanExecuteChanged();
            };
        }

        private void OnBasePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
                OpenVoidForSelectedCommand.NotifyCanExecuteChanged();
        }

        partial void OnVoidReasonChanged(string value)
        {
            OnPropertyChanged(nameof(VoidReasonCounter));
            VoidError = null;
        }

        private bool CanVoidSelected() =>
            IsAdmin && SelectedItem != null && !GetIsVoided(SelectedItem);

        /// <summary>
        /// Abre el diálogo de motivo para la venta seleccionada.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanVoidSelected))]
        private void OpenVoidForSelected()
        {
            var item = SelectedItem;
            if (item == null)
                return;

            PendingVoidSaleId = GetSaleId(item);
            VoidReason = string.Empty;
            VoidError = null;
            IsVoidDialogOpen = true;
        }

        /// <summary>
        /// Cierra el diálogo de anulación sin anular.
        /// </summary>
        [RelayCommand]
        private void CloseVoidDialog() => IsVoidDialogOpen = false;

        /// <summary>
        /// Recarga el historial tras una anulación confirmada. LoadAsync deja el fallo en ErrorMessage
        /// (o lanza en subclases que lo sobrescriban): en ambos casos se avisa de que la venta SÍ se anuló.
        /// </summary>
        private async Task ReloadAfterVoidAsync()
        {
            const string title = "Venta anulada";
            const string reloadFailed = "La venta se anuló correctamente, pero no se pudo recargar la lista. Pulsa «Actualizar» para verla al día.";

            try
            {
                await LoadAsync();

                if (!string.IsNullOrEmpty(ErrorMessage))
                    ShowAlert(title, reloadFailed);
            }
            catch (Exception)
            {
                ShowAlert(title, reloadFailed);
            }
        }

        /// <summary>
        /// Valida el motivo y envía la anulación. Con éxito recarga el historial;
        /// un 400 se muestra dentro del diálogo y el resto (409, 404...) en el aviso modal.
        /// </summary>
        [RelayCommand]
        private async Task ConfirmVoidAsync()
        {
            if (IsVoiding)
                return;

            var reason = VoidReason.Trim();

            if (reason.Length == 0)
            {
                VoidError = "El motivo de la anulación es obligatorio.";
                return;
            }

            if (reason.Length > VoidSaleDTO.ReasonMaxLength)
            {
                VoidError = $"El motivo no puede superar los {VoidSaleDTO.ReasonMaxLength} caracteres ({reason.Length}).";
                return;
            }

            try
            {
                IsVoiding = true;
                VoidError = null;

                await _httpClient.PostAsync(
                    ApiRoutes.Sale.VoidBySaleId(PendingVoidSaleId),
                    new VoidSaleDTO { Reason = reason });

                // La anulación ya está confirmada por la API: a partir de aquí un fallo del refresco
                // NO es un error de anulación (reintentar daría un 409 engañoso).
                IsVoidDialogOpen = false;
                await ReloadAfterVoidAsync();
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.BadRequest)
            {
                // Mensaje en español del envelope (motivo vacío o demasiado largo): el admin lo corrige aquí mismo
                VoidError = apiEx.Message;
            }
            catch (ApiException apiEx)
            {
                IsVoidDialogOpen = false;

                var message = apiEx.StatusCode switch
                {
                    HttpStatusCode.Forbidden => "Solo un administrador puede anular ventas.",
                    HttpStatusCode.Unauthorized => "La sesión ha caducado. Vuelve a iniciar sesión.",
                    _ => apiEx.Message
                };

                ShowAlert("No se pudo anular la venta", message);

                // 409 (ya anulada) y 404: el historial en pantalla está desfasado, se refresca
                if (apiEx.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound)
                    await LoadAsync();
            }
            catch (Exception ex)
            {
                VoidError = $"Error local anulando la venta: {ex.Message}";
            }
            finally
            {
                IsVoiding = false;
            }
        }
    }
}
