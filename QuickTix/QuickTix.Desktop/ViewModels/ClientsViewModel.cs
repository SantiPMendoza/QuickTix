
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;

using QuickTix.Desktop.Models.Forms;

using QuickTix.Desktop.ViewModels.Base;


namespace QuickTix.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel principal de clientes.
    /// Gestiona el CRUD de clientes y coordina la gestión/venta de suscripciones asociadas al cliente seleccionado,
    /// utilizando formularios en flyouts para creación/edición.
    /// </summary>
    public partial class ClientsViewModel : BaseCrudViewModel<ClientDTO, CreateClientDTO>
    {
        protected override string Endpoint => "Client";

        private readonly IAuthService _authService;

        // Id del manager en sesión, leído de los claims del JWT en cada acceso.
        // Es 0 cuando no hay sesión o el usuario logueado no es manager (p.ej. admin):
        // en ese caso SaveSubscription decide por rol (admin vende sin manager).
        public int CurrentManagerId => _authService.GetManagerId();

        // Rol de la sesión actual según el usuario devuelto por la API en el login.
        private bool IsAdminSession =>
            string.Equals(_authService.GetCurrentUser()?.Role, "admin", StringComparison.OrdinalIgnoreCase);

        // Estado del flyout de Cliente
        [ObservableProperty] private bool isClientFlyoutOpen;
        [ObservableProperty] private bool isEditingClient;
        [ObservableProperty] private object? activeClientForm;

        /// <summary>
        /// Módulo de suscripciones asociado a la vista de clientes.
        /// Se sincroniza con el cliente seleccionado.
        /// </summary>
        public SubscriptionsViewModel SubscriptionsVM { get; }

        // Estado del flyout de Suscripción
        [ObservableProperty] private bool isSubscriptionFlyoutOpen;
        [ObservableProperty] private bool isEditingSubscription;
        [ObservableProperty] private object? activeSubscriptionForm;

        // Estado del diálogo de confirmación de borrado de abono (fix 2b):
        // el borrado real solo se ejecuta al confirmar, nunca directamente.
        [ObservableProperty] private bool isConfirmDeleteSubscriptionOpen;
        [ObservableProperty] private SubscriptionDTO? pendingDeleteSubscription;

        // Estado del diálogo de borrado forzado (409 de la API): solo aparece
        // si el borrado normal devolvió conflicto por dependencias. Sustituye
        // al MessageBox que causaba el "segundo diálogo" tras confirmar.
        [ObservableProperty] private bool isForceDeleteSubscriptionOpen;
        [ObservableProperty] private string? forceDeleteSubscriptionMessage;
        private int _forceDeleteSubscriptionId;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ClientsViewModel"/>,
        /// crea el módulo de suscripciones y carga el listado inicial de clientes.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        /// <param name="authService">Servicio de autenticación (sesión y claims del JWT).</param>
        public ClientsViewModel(HttpJsonClient httpClient, IAuthService authService) : base(httpClient)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            SubscriptionsVM = new SubscriptionsViewModel(httpClient);
            _ = LoadAsync();
        }

        /// <summary>
        /// Abre el flyout de clientes en modo creación e inicializa el formulario.
        /// </summary>
        [RelayCommand]
        private void OpenClientFlyout()
        {
            IsEditingClient = false;
            ActiveClientForm = new CreateClientDTO();
            ErrorMessage = null;
            IsClientFlyoutOpen = true;
        }

        /// <summary>
        /// Abre el flyout de clientes en modo edición clonando el elemento seleccionado.
        /// </summary>
        [RelayCommand]
        private void EditClient()
        {
            if (SelectedItem == null)
                return;

            IsEditingClient = true;
            ErrorMessage = null;

            ActiveClientForm = new ClientDTO
            {
                Id = SelectedItem.Id,
                Name = SelectedItem.Name,
                Email = SelectedItem.Email,
                Nif = SelectedItem.Nif,
                PhoneNumber = SelectedItem.PhoneNumber
            };

            IsClientFlyoutOpen = true;
        }

        /// <summary>
        /// Guarda el formulario de cliente (alta o edición).
        /// Si hay error, mantiene el flyout abierto y conserva los datos introducidos.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task SaveClient()
        {
            if (ActiveClientForm == null)
                return;

            ErrorMessage = null;

            bool ok;
            int? idToReselect = null;

            if (!IsEditingClient)
            {
                ok = await TryAddAsync((CreateClientDTO)ActiveClientForm);
            }
            else
            {
                var dto = (ClientDTO)ActiveClientForm;
                idToReselect = dto.Id;
                ok = await TryUpdateAsync(dto.Id, dto);
            }

            if (!ok)
            {
                // Mantener abierto y conservar datos para corregir errores
                IsClientFlyoutOpen = true;
                return;
            }

            // Tras edición, se intenta re-seleccionar el cliente actualizado en el listado
            if (idToReselect.HasValue)
                SelectedItem = Items.FirstOrDefault(x => x.Id == idToReselect.Value);

            ActiveClientForm = null;
            IsEditingClient = false;
            IsClientFlyoutOpen = false;
        }

        /// <summary>
        /// Cierra el flyout de cliente y limpia el estado del formulario.
        /// </summary>
        [RelayCommand]
        private void CloseClientFlyout()
        {
            IsClientFlyoutOpen = false;
            ActiveClientForm = null;
            IsEditingClient = false;
            ErrorMessage = null;
        }

        /// <summary>
        /// Abre el flyout de suscripción en modo creación para el cliente seleccionado.
        /// Precarga recintos y establece valores por defecto del formulario.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task OpenSubscriptionFlyout()
        {
            if (SelectedItem == null)
                return;

            IsEditingSubscription = false;
            SubscriptionsVM.ErrorMessage = null;

            await SubscriptionsVM.LoadVenuesAsync();
            if (SubscriptionsVM.Venues.Count == 0)
                return;

            SubscriptionsVM.SelectedVenue = SubscriptionsVM.Venues[0];

            ActiveSubscriptionForm = new SubscriptionFormModel
            {
                VenueId = SubscriptionsVM.SelectedVenue.Id,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual,
                StartDate = DateTime.Today
            };

            IsSubscriptionFlyoutOpen = true;
        }

        /// <summary>
        /// Cierra el flyout de suscripción y limpia el estado del formulario.
        /// </summary>
        [RelayCommand]
        private void CloseSubscriptionFlyout()
        {
            IsSubscriptionFlyoutOpen = false;
            ActiveSubscriptionForm = null;
            IsEditingSubscription = false;
            SubscriptionsVM.ErrorMessage = null;
        }

        /// <summary>
        /// Registra la venta de una suscripción para el cliente seleccionado.
        /// Construye el request de venta y delega la operación al módulo de suscripciones.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task SaveSubscription()
        {
            if (SelectedItem == null)
                return;

            if (ActiveSubscriptionForm is not SubscriptionFormModel form)
                return;

            // Admin: la venta se registra sin manager (la API la muestra como "Administración").
            // Manager: se envía su id real leído del JWT. Sin rol válido: error inline.
            int? managerIdForSale;

            if (IsAdminSession)
            {
                managerIdForSale = null;
            }
            else if (CurrentManagerId > 0)
            {
                managerIdForSale = CurrentManagerId;
            }
            else
            {
                // Se mantiene el patrón de error inline del módulo de suscripciones
                SubscriptionsVM.ErrorMessage =
                    "La sesión actual no tiene un Manager asociado. Inicia sesión como manager o admin para registrar ventas.";

                IsSubscriptionFlyoutOpen = true;
                return;
            }

            SubscriptionsVM.ErrorMessage = null;

            var request = new SellSubscriptionDTO
            {
                ClientId = SelectedItem.Id,
                VenueId = form.VenueId,
                ManagerId = managerIdForSale,
                Category = form.Category,
                Duration = form.Duration,
                StartDate = form.StartDate,

                // El backend calcula el precio; si falta en el mapa, devolverá error controlado
                Price = 0m
            };

            var ok = await SubscriptionsVM.TrySellAsync(request);

            if (!ok)
            {
                // Mantener flyout y datos para corregir errores
                IsSubscriptionFlyoutOpen = true;
                return;
            }

            ActiveSubscriptionForm = null;
            IsEditingSubscription = false;
            IsSubscriptionFlyoutOpen = false;
        }

        /// <summary>
        /// Abre el diálogo de confirmación para eliminar la suscripción seleccionada.
        /// El borrado real se ejecuta en <see cref="ConfirmDeleteSubscription"/>.
        /// </summary>
        /// <remarks>
        /// Se mantiene el nombre del comando (CancelSubscriptionCommand) para no
        /// romper el binding existente del botón "Cancelar abono".
        /// </remarks>
        [RelayCommand]
        private void CancelSubscription()
        {
            if (SelectedItem == null)
                return;

            if (SubscriptionsVM.SelectedItem == null)
                return;

            PendingDeleteSubscription = SubscriptionsVM.SelectedItem;
            IsConfirmDeleteSubscriptionOpen = true;
        }

        /// <summary>
        /// Confirma y ejecuta el borrado de la suscripción pendiente de eliminar.
        /// Si la API devuelve conflicto (409, dependencias de venta), abre el
        /// diálogo de borrado forzado en lugar de un MessageBox.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task ConfirmDeleteSubscription()
        {
            if (PendingDeleteSubscription == null)
            {
                IsConfirmDeleteSubscriptionOpen = false;
                return;
            }

            var subId = PendingDeleteSubscription.Id;

            IsConfirmDeleteSubscriptionOpen = false;
            PendingDeleteSubscription = null;

            var result = await SubscriptionsVM.TryDeleteAsync(subId);

            switch (result)
            {
                case SubscriptionDeleteResult.Success:
                    SubscriptionsVM.SelectedItem = null;
                    break;

                case SubscriptionDeleteResult.Conflict:
                    // Segunda confirmación SOLO en el caso 409: borrado forzado
                    _forceDeleteSubscriptionId = subId;
                    ForceDeleteSubscriptionMessage = SubscriptionsVM.LastConflictMessage;
                    IsForceDeleteSubscriptionOpen = true;
                    break;

                case SubscriptionDeleteResult.Error:
                    ShowAlert("Error", SubscriptionsVM.ErrorMessage ?? "No se pudo eliminar el abono.");
                    break;
            }
        }

        /// <summary>
        /// Cierra el diálogo de confirmación sin eliminar nada.
        /// </summary>
        [RelayCommand]
        private void CancelDeleteSubscription()
        {
            IsConfirmDeleteSubscriptionOpen = false;
            PendingDeleteSubscription = null;
        }

        /// <summary>
        /// Confirma el borrado forzado tras el conflicto (elimina el abono
        /// junto con los ítems de venta asociados).
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task ConfirmForceDeleteSubscription()
        {
            var subId = _forceDeleteSubscriptionId;

            IsForceDeleteSubscriptionOpen = false;
            ForceDeleteSubscriptionMessage = null;
            _forceDeleteSubscriptionId = 0;

            if (subId == 0)
                return;

            var result = await SubscriptionsVM.TryDeleteAsync(subId, force: true);

            if (result == SubscriptionDeleteResult.Success)
                SubscriptionsVM.SelectedItem = null;
            else
                ShowAlert("Error", SubscriptionsVM.ErrorMessage ?? "No se pudo eliminar el abono.");
        }

        /// <summary>
        /// Cierra el diálogo de borrado forzado sin eliminar nada.
        /// </summary>
        [RelayCommand]
        private void CancelForceDeleteSubscription()
        {
            IsForceDeleteSubscriptionOpen = false;
            ForceDeleteSubscriptionMessage = null;
            _forceDeleteSubscriptionId = 0;
        }

        /// <summary>
        /// Maneja el cambio de cliente seleccionado:
        /// carga las suscripciones del cliente en el módulo de suscripciones o limpia la vista si no hay selección.
        /// </summary>
        /// <param name="value">Cliente seleccionado.</param>
        /// <returns>Tarea asíncrona.</returns>
        protected override async Task OnSelectedItemChangedAsync(ClientDTO? value)
        {
            if (value == null)
            {
                SubscriptionsVM.Items.Clear();
                SubscriptionsVM.SelectedItem = null;
                return;
            }

            await SubscriptionsVM.LoadByClientAsync(value.Id);
        }
    }
}
